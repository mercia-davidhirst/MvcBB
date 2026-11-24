using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MvcBB.API.Controllers;
using MvcBB.API.InMemory;
using MvcBB.API.Services;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.User;

namespace MvcBB.Tests;

public class AdminEmailComposerTests
{
    [Fact]
    public void NoReply_UsesSiteAddressAndName_WithNoReplyTo()
    {
        var sender = AdminEmailComposer.ResolveSender(AdminEmailSenderMode.NoReply, "dave", "dave@gmail.com", "Forum", "noreply@forum.test", allowAdminFromAddress: true);

        Assert.Equal("noreply@forum.test", sender.FromAddress);
        Assert.Equal("Forum", sender.FromName);
        Assert.Null(sender.ReplyToAddress);
    }

    [Fact]
    public void Admin_WithoutSwitch_SendsFromSiteWithAdminAsReplyTo()
    {
        var sender = AdminEmailComposer.ResolveSender(AdminEmailSenderMode.Admin, "dave", "dave@gmail.com", "Forum", "noreply@forum.test", allowAdminFromAddress: false);

        Assert.Equal("noreply@forum.test", sender.FromAddress);
        Assert.Equal("dave via Forum", sender.FromName);
        Assert.Equal("dave@gmail.com", sender.ReplyToAddress);
        Assert.True(sender.AdminAddressMovedToReplyTo);
    }

    [Fact]
    public void Admin_WithSwitch_SendsFromAdminAddress()
    {
        var sender = AdminEmailComposer.ResolveSender(AdminEmailSenderMode.Admin, "dave", "dave@forum.test", "Forum", "noreply@forum.test", allowAdminFromAddress: true);

        Assert.Equal("dave@forum.test", sender.FromAddress);
        Assert.Equal("dave", sender.FromName);
        Assert.Null(sender.ReplyToAddress);
        Assert.False(sender.AdminAddressMovedToReplyTo);
    }

    [Fact]
    public void BuildBody_FillsUsername_AndAddsOptOutFooter()
    {
        var body = AdminEmailComposer.BuildBody("Hi {Username},\nWe're down Sunday.", "alice", "Forum", "https://forum.test/Account/Profile", isImportant: false);

        Assert.StartsWith("Hi alice,\nWe're down Sunday.", body);
        Assert.Contains(AdminEmailText.OptOutLabel, body);
        Assert.Contains("https://forum.test/Account/Profile", body);
    }

    [Fact]
    public void BuildBody_ImportantAlert_ExplainsWhyItArrived()
    {
        var body = AdminEmailComposer.BuildBody("Security notice", "alice", "Forum", "https://forum.test/Account/Profile", isImportant: true);

        Assert.Contains("important service notice", body);
        Assert.DoesNotContain(AdminEmailText.OptOutLabel, body);
    }

    [Fact]
    public void Batch_SplitsIntoChunks()
    {
        var batches = AdminEmailComposer.Batch(Enumerable.Range(1, 5).ToList(), 2).ToList();

        Assert.Equal(new[] { 2, 2, 1 }, batches.Select(b => b.Count));
    }
}

public class AdminEmailRepositoryTests
{
    private static (InMemoryStore Store, InMemoryAdminEmailRepository Repository) Create()
    {
        var store = new InMemoryStore();
        store.Users.Add(new User { Id = 1, Username = "admin", Email = "admin@forum.test", Role = UserRole.Administrator });
        store.Users.Add(new User { Id = 2, Username = "alice", Email = "alice@forum.test", Role = UserRole.User });
        store.Users.Add(new User { Id = 3, Username = "bob", Email = "bob@forum.test", Role = UserRole.User });
        store.Users.Add(new User { Id = 4, Username = "Deleted User 4", Email = "deleted4@invalid", Role = UserRole.Deleted });
        store.Users.Add(new User { Id = 5, Username = "mod", Email = "mod@forum.test", Role = UserRole.Moderator });
        return (store, new InMemoryAdminEmailRepository(store));
    }

    [Fact]
    public void Recipients_SkipDeletedAndOptedOut()
    {
        var (_, repository) = Create();
        repository.SetOptedOut(3, true);

        var ids = repository.GetRecipients(null, null, includeOptedOut: false).Select(r => r.UserId);

        Assert.Equal(new[] { 1, 2, 5 }, ids);
    }

    [Fact]
    public void ImportantAlert_IncludesOptedOut_ButNeverDeleted()
    {
        var (_, repository) = Create();
        repository.SetOptedOut(3, true);

        var ids = repository.GetRecipients(null, null, includeOptedOut: true).Select(r => r.UserId);

        Assert.Equal(new[] { 1, 2, 3, 5 }, ids);
    }

    [Fact]
    public void Recipients_FilterByRoleAndUser()
    {
        var (_, repository) = Create();

        Assert.Equal(new[] { 2, 3 }, repository.GetRecipients(UserRole.User, null, false).Select(r => r.UserId));
        Assert.Equal(new[] { 5 }, repository.GetRecipients(null, 5, false).Select(r => r.UserId));
    }

    [Fact]
    public void CountRecipients_ReportsOptedOut()
    {
        var (_, repository) = Create();
        repository.SetOptedOut(2, true);

        Assert.Equal((2, 1), repository.CountRecipients(UserRole.User, null));
    }

    [Fact]
    public void OptOut_CanBeTurnedBackOn()
    {
        var (_, repository) = Create();
        repository.SetOptedOut(2, true);
        repository.SetOptedOut(2, false);

        Assert.False(repository.IsOptedOut(2));
    }

    [Fact]
    public void MarkUnfinishedInterrupted_OnlyTouchesQueuedAndSending()
    {
        var (_, repository) = Create();
        var queued = repository.Add(new AdminEmail { Subject = "a", Status = AdminEmailStatus.Queued });
        var done = repository.Add(new AdminEmail { Subject = "b", Status = AdminEmailStatus.Completed });

        Assert.Equal(1, repository.MarkUnfinishedInterrupted());
        Assert.Equal(AdminEmailStatus.Interrupted, repository.GetById(queued.Id)!.Status);
        Assert.Equal(AdminEmailStatus.Completed, repository.GetById(done.Id)!.Status);
    }
}

public class AdminEmailSendingTests
{
    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = new();
        public string? FailFor { get; set; }

        public Task SendAsync(string toEmail, string subject, string body) =>
            SendAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });

        public Task SendAsync(EmailMessage message)
        {
            if (message.To == FailFor)
                throw new InvalidOperationException("relay refused");
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class Harness
    {
        public required ServiceProvider Services { get; init; }
        public required InMemoryStore Store { get; init; }
        public required RecordingEmailSender Sender { get; init; }
        public required AdminEmailQueue Queue { get; init; }
        public IAdminEmailRepository Repository => Services.GetRequiredService<IAdminEmailRepository>();

        public AdminEmailsController Controller(int callerUserId)
        {
            var scope = Services.CreateScope();
            return new AdminEmailsController(
                scope.ServiceProvider.GetRequiredService<IAdminEmailRepository>(),
                scope.ServiceProvider.GetRequiredService<IUserRepository>(),
                scope.ServiceProvider.GetRequiredService<IDisplaySettingsService>(),
                Sender,
                Queue,
                scope.ServiceProvider.GetRequiredService<IConfiguration>(),
                scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AdminEmailOptions>>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, callerUserId.ToString()),
                            new Claim(ClaimTypes.Role, "Administrator")
                        }, "Test"))
                    }
                }
            };
        }

        public AdminEmailDispatcher Dispatcher() => new(
            Queue,
            Services.GetRequiredService<IServiceScopeFactory>(),
            Sender,
            Services.GetRequiredService<IConfiguration>(),
            Services.GetRequiredService<IOptionsMonitor<AdminEmailOptions>>(),
            NullLogger<AdminEmailDispatcher>.Instance);
    }

    private static Harness Create()
    {
        var store = new InMemoryStore();
        store.Users.Add(new User { Id = 1, Username = "admin", Email = "admin@gmail.com", Role = UserRole.Administrator });
        store.Users.Add(new User { Id = 2, Username = "alice", Email = "alice@forum.test", Role = UserRole.User });
        store.Users.Add(new User { Id = 3, Username = "bob", Email = "bob@forum.test", Role = UserRole.User });
        store.Users.Add(new User { Id = 4, Username = "carol", Email = "carol@forum.test", Role = UserRole.User });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:FromEmail"] = "noreply@forum.test",
                ["App:BaseUrl"] = "https://forum.test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(store);
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IAdminEmailRepository, InMemoryAdminEmailRepository>();
        services.AddSingleton<MvcBB.Shared.Interfaces.IDisplaySettingsRepository, InMemoryDisplaySettingsRepository>();
        services.AddScoped<IDisplaySettingsService, DisplaySettingsService>();
        services.AddOptions();
        services.Configure<AdminEmailOptions>(o =>
        {
            o.BatchSize = 2;
            o.BatchDelaySeconds = 0;
        });

        return new Harness
        {
            Services = services.BuildServiceProvider(),
            Store = store,
            Sender = new RecordingEmailSender(),
            Queue = new AdminEmailQueue()
        };
    }

    private static SendAdminEmailRequest Request(AdminEmailAudience audience = AdminEmailAudience.AllUsers) => new()
    {
        Subject = "Maintenance on Sunday",
        Body = "Hi {username}, the forum is down 2-4am.",
        Audience = audience,
        SenderMode = AdminEmailSenderMode.Admin
    };

    [Fact]
    public async Task Send_QueuesThenSendsToEveryoneExceptOptedOut_WithAdminAsReplyTo()
    {
        var harness = Create();
        harness.Repository.SetOptedOut(3, true);

        var result = harness.Controller(1).Send(Request());
        var queued = Assert.IsType<AdminEmail>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal(AdminEmailStatus.Queued, queued.Status);
        Assert.Equal(3, queued.RecipientCount);

        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(new[] { "admin@gmail.com", "alice@forum.test", "carol@forum.test" }, harness.Sender.Sent.Select(m => m.To));
        var toAlice = harness.Sender.Sent.Single(m => m.To == "alice@forum.test");
        Assert.Equal("noreply@forum.test", toAlice.FromAddress);
        Assert.Equal("admin@gmail.com", toAlice.ReplyToAddress);
        Assert.StartsWith("Hi alice,", toAlice.Body);

        var finished = harness.Repository.GetById(queued.Id)!;
        Assert.Equal(AdminEmailStatus.Completed, finished.Status);
        Assert.Equal(3, finished.SentCount);
        Assert.NotNull(finished.CompletedAt);
    }

    [Fact]
    public async Task ImportantAlert_ReachesOptedOutMembers()
    {
        var harness = Create();
        harness.Repository.SetOptedOut(3, true);
        var request = Request();
        request.IsImportant = true;

        var queued = Assert.IsType<AdminEmail>(Assert.IsType<CreatedAtActionResult>(harness.Controller(1).Send(request).Result).Value);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Contains(harness.Sender.Sent, m => m.To == "bob@forum.test");
    }

    [Fact]
    public async Task OneFailedRecipient_DoesNotStopTheRest()
    {
        var harness = Create();
        harness.Sender.FailFor = "alice@forum.test";

        var queued = Assert.IsType<AdminEmail>(Assert.IsType<CreatedAtActionResult>(harness.Controller(1).Send(Request()).Result).Value);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        var finished = harness.Repository.GetById(queued.Id)!;
        Assert.Equal(AdminEmailStatus.CompletedWithErrors, finished.Status);
        Assert.Equal(3, finished.SentCount);
        Assert.Equal(1, finished.FailedCount);
    }

    [Fact]
    public void Send_ToSingleOptedOutMember_IsRefused()
    {
        var harness = Create();
        harness.Repository.SetOptedOut(2, true);
        var request = Request(AdminEmailAudience.SingleUser);
        request.UserId = 2;

        var result = harness.Controller(1).Send(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(harness.Repository.GetRecent(10));
    }

    [Fact]
    public void Send_RoleAudienceWithoutRole_IsRefused()
    {
        var harness = Create();

        var result = harness.Controller(1).Send(Request(AdminEmailAudience.Role));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void Preview_CountsWithoutSending()
    {
        var harness = Create();
        harness.Repository.SetOptedOut(4, true);
        var request = Request(AdminEmailAudience.Role);
        request.Role = UserRole.User;

        var preview = harness.Controller(1).Preview(request).Value!;

        Assert.Equal(2, preview.RecipientCount);
        Assert.Equal(1, preview.OptedOutCount);
        Assert.Equal("admin via MvcBB <noreply@forum.test>", preview.From);
        Assert.True(preview.AdminAddressMovedToReplyTo);
        Assert.Empty(harness.Repository.GetRecent(10));
        Assert.Empty(harness.Sender.Sent);
    }

    [Fact]
    public async Task SendTest_GoesOnlyToTheAdmin()
    {
        var harness = Create();

        var result = await harness.Controller(1).SendTest(Request());

        Assert.IsType<OkObjectResult>(result);
        var message = Assert.Single(harness.Sender.Sent);
        Assert.Equal("admin@gmail.com", message.To);
        Assert.StartsWith("[Test] ", message.Subject);
    }
}
