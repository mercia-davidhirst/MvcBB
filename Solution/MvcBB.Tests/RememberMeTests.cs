using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MvcBB.API.InMemory;
using MvcBB.API.Services;
using MvcBB.App.Services;
using MvcBB.Shared.Models.User;

namespace MvcBB.Tests;

public class RememberMeTests
{
    private sealed record Rig(RefreshTokenService Service, InMemoryStore Store, InMemoryUserRepository Users, ManualClock Clock, JwtTokenService Jwt, User Member);

    private static Rig Create()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "a-test-secret-key-that-is-long-enough-for-hmac",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            })
            .Build();

        var store = new InMemoryStore();
        var users = new InMemoryUserRepository(store);
        var member = users.Add(new User
        {
            Username = "Remembered",
            Email = "remembered@example.com",
            PasswordHash = "hash-1",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        });

        var clock = new ManualClock();
        var jwt = new JwtTokenService(configuration);
        var service = new RefreshTokenService(
            new InMemoryRefreshTokenRepository(store), users, jwt, configuration, clock, NullLogger<RefreshTokenService>.Instance);
        return new Rig(service, store, users, clock, jwt, member);
    }

    [Fact]
    public void Issue_StoresOnlyAHashOfTheToken()
    {
        var rig = Create();

        var issued = rig.Service.Issue(rig.Member, "10.0.0.1");

        var stored = Assert.Single(rig.Store.RefreshTokens);
        Assert.NotEqual(issued.Token, stored.TokenHash);
        Assert.Equal(RefreshTokenService.Hash(issued.Token), stored.TokenHash);
        Assert.Equal(rig.Clock.GetUtcNow().UtcDateTime.AddDays(30), issued.ExpiresAt);
        Assert.True(issued.Token.Length >= 43);
    }

    [Fact]
    public void Rotate_SpendsTheTokenAndIssuesAReplacementInTheSameFamily()
    {
        var rig = Create();
        var first = rig.Service.Issue(rig.Member, null);

        var result = rig.Service.Rotate(first.Token, null);

        Assert.Equal(RefreshOutcome.Success, result.Outcome);
        Assert.Equal(rig.Member.Id, result.User!.Id);
        Assert.NotEqual(first.Token, result.Token!.Token);
        Assert.Equal(2, rig.Store.RefreshTokens.Count);
        Assert.NotNull(rig.Store.RefreshTokens[0].UsedAt);
        Assert.Equal(rig.Store.RefreshTokens[0].FamilyId, rig.Store.RefreshTokens[1].FamilyId);
    }

    [Fact]
    public void Rotate_WithASpentToken_RevokesTheWholeSignIn()
    {
        var rig = Create();
        var stolen = rig.Service.Issue(rig.Member, null);
        var legitimate = rig.Service.Rotate(stolen.Token, null).Token!;
        rig.Clock.Advance(TimeSpan.FromMinutes(5));

        var replay = rig.Service.Rotate(stolen.Token, null);

        Assert.Equal(RefreshOutcome.Reused, replay.Outcome);
        Assert.Equal(RefreshOutcome.Invalid, rig.Service.Rotate(legitimate.Token, null).Outcome);
    }

    [Fact]
    public void Rotate_WithATokenSpentAMomentAgo_IsAConflictNotATheft()
    {
        var rig = Create();
        var first = rig.Service.Issue(rig.Member, null);
        var next = rig.Service.Rotate(first.Token, null).Token!;
        rig.Clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(RefreshOutcome.Conflict, rig.Service.Rotate(first.Token, null).Outcome);
        Assert.Equal(RefreshOutcome.Success, rig.Service.Rotate(next.Token, null).Outcome);
    }

    [Fact]
    public void Rotate_AfterExpiry_IsRefused()
    {
        var rig = Create();
        var issued = rig.Service.Issue(rig.Member, null);
        rig.Clock.Advance(TimeSpan.FromDays(31));

        Assert.Equal(RefreshOutcome.Expired, rig.Service.Rotate(issued.Token, null).Outcome);
    }

    [Fact]
    public void Rotate_AfterPasswordChange_EndsTheSession()
    {
        var rig = Create();
        var issued = rig.Service.Issue(rig.Member, null);
        rig.Member.PasswordHash = "hash-2";
        rig.Users.Update(rig.Member);

        Assert.Equal(RefreshOutcome.SessionEnded, rig.Service.Rotate(issued.Token, null).Outcome);
        Assert.NotNull(Assert.Single(rig.Store.RefreshTokens).RevokedAt);
    }

    [Fact]
    public void Rotate_ForALockedMember_EndsTheSession()
    {
        var rig = Create();
        var issued = rig.Service.Issue(rig.Member, null);
        rig.Member.LockedUntil = rig.Clock.GetUtcNow().UtcDateTime.AddDays(1);
        rig.Users.Update(rig.Member);

        Assert.Equal(RefreshOutcome.SessionEnded, rig.Service.Rotate(issued.Token, null).Outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    public void Rotate_WithAnUnknownToken_IsInvalid(string? token)
    {
        var rig = Create();
        rig.Service.Issue(rig.Member, null);

        Assert.Equal(RefreshOutcome.Invalid, rig.Service.Rotate(token, null).Outcome);
    }

    [Fact]
    public void Revoke_EndsOnlyThatDevice()
    {
        var rig = Create();
        var laptop = rig.Service.Issue(rig.Member, null);
        var phone = rig.Service.Issue(rig.Member, null);

        rig.Service.Revoke(laptop.Token);

        Assert.Equal(RefreshOutcome.Invalid, rig.Service.Rotate(laptop.Token, null).Outcome);
        Assert.Equal(RefreshOutcome.Success, rig.Service.Rotate(phone.Token, null).Outcome);
    }

    [Fact]
    public void RevokeAllForUser_EndsEveryDevice()
    {
        var rig = Create();
        var laptop = rig.Service.Issue(rig.Member, null);
        var phone = rig.Service.Issue(rig.Member, null);

        rig.Service.RevokeAllForUser(rig.Member.Id);

        Assert.Equal(RefreshOutcome.Invalid, rig.Service.Rotate(laptop.Token, null).Outcome);
        Assert.Equal(RefreshOutcome.Invalid, rig.Service.Rotate(phone.Token, null).Outcome);
    }

    [Fact]
    public void Prune_RemovesExpiredAndLongSpentTokens()
    {
        var rig = Create();
        var repository = new InMemoryRefreshTokenRepository(rig.Store);
        var now = rig.Clock.GetUtcNow().UtcDateTime;
        repository.Add(new RefreshToken { TokenHash = "expired", ExpiresAt = now.AddMinutes(-1) });
        repository.Add(new RefreshToken { TokenHash = "spent-long-ago", ExpiresAt = now.AddDays(20), UsedAt = now.AddDays(-8) });
        repository.Add(new RefreshToken { TokenHash = "spent-recently", ExpiresAt = now.AddDays(20), UsedAt = now.AddDays(-1) });
        repository.Add(new RefreshToken { TokenHash = "live", ExpiresAt = now.AddDays(20) });

        Assert.Equal(2, repository.Prune(now, now.AddDays(-7)));
        Assert.Equal(new[] { "spent-recently", "live" }, rig.Store.RefreshTokens.Select(t => t.TokenHash));
    }

    [Fact]
    public void SessionCookie_RememberedSessionsPersistUntilTheRefreshTokenRunsOut()
    {
        var refreshExpires = DateTime.UtcNow.AddDays(30);
        var remembered = SessionCookie.CreateProperties(new SessionTokens("jwt", DateTime.UtcNow.AddHours(1), "refresh", refreshExpires));
        var plain = SessionCookie.CreateProperties(new SessionTokens("jwt", DateTime.UtcNow.AddHours(1), null, null));

        Assert.True(remembered.IsPersistent);
        Assert.Equal(refreshExpires, remembered.ExpiresUtc!.Value.UtcDateTime, TimeSpan.FromSeconds(1));
        Assert.False(plain.IsPersistent);
        Assert.True(plain.ExpiresUtc < DateTimeOffset.UtcNow.AddHours(2));
    }

    [Fact]
    public void SessionCookie_WithTokens_SwapsTokensAndKeepsTheRefreshTokenWhenNoneIsGiven()
    {
        var principal = SessionCookie.CreatePrincipal(new UserAuthResponse
        {
            Token = "old-jwt",
            ExpiresAt = DateTime.UtcNow.AddMinutes(1),
            RefreshToken = "refresh-1",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30),
            User = new UserResponse { Id = 7, Username = "Remembered", Email = "r@example.com", Role = UserRole.User, MustChangePassword = true }
        });

        var expires = DateTime.UtcNow.AddHours(1);
        var updated = SessionCookie.WithTokens(principal, new SessionTokens("new-jwt", expires, null, null), removeMustChangePassword: true);

        Assert.Equal("new-jwt", updated.FindFirst(SessionCookie.ApiTokenClaim)!.Value);
        Assert.Equal("refresh-1", SessionCookie.GetRefreshToken(updated));
        Assert.Null(updated.FindFirst(SessionCookie.MustChangePasswordClaim));
        Assert.Equal(expires, SessionCookie.GetApiTokenExpiry(updated)!.Value, TimeSpan.FromMilliseconds(1));
        Assert.Single(updated.FindAll(SessionCookie.ApiTokenClaim));
    }
}
