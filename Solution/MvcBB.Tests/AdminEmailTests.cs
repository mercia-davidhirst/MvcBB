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
using MvcBB.App.Models;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.Settings;
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
    public void FailureReason_UsesInnermostMessage_TrimmedAndCut()
    {
        var wrapped = new AggregateException(new InvalidOperationException("  mailbox unavailable  "));
        Assert.Equal("mailbox unavailable", AdminEmailComposer.FailureReason(wrapped));

        var longReason = AdminEmailComposer.FailureReason(new Exception(new string('x', 1500)));
        Assert.Equal(AdminEmailComposer.MaxFailureReasonLength, longReason.Length);
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

    [Fact]
    public void Failures_RoundTrip_FilteredByEmail_OldestFirst()
    {
        var (_, repository) = Create();
        var first = repository.Add(new AdminEmail { Subject = "a" });
        var second = repository.Add(new AdminEmail { Subject = "b" });
        var now = DateTime.UtcNow;

        repository.AddFailure(new AdminEmailFailure { AdminEmailId = first.Id, UserId = 3, Username = "bob", Email = "bob@forum.test", Reason = "later", FailedAt = now.AddMinutes(1) });
        repository.AddFailure(new AdminEmailFailure { AdminEmailId = second.Id, UserId = 2, Username = "alice", Email = "alice@forum.test", Reason = "other email" });
        repository.AddFailure(new AdminEmailFailure { AdminEmailId = first.Id, UserId = 2, Username = "alice", Email = "alice@forum.test", Reason = "earlier", FailedAt = now });

        var failures = repository.GetFailures(first.Id);

        Assert.Equal(new[] { "earlier", "later" }, failures.Select(f => f.Reason));
        Assert.Equal(new[] { 2, 3 }, failures.Select(f => f.UserId));
        Assert.All(failures, f => Assert.True(f.Id > 0));
        Assert.Equal("alice@forum.test", failures[0].Email);
        Assert.Single(repository.GetFailures(second.Id));
        Assert.Empty(repository.GetFailures(999));
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

    // AddDelivery always fails; everything else goes to the real repository.
    private sealed class FailingDeliveryRepository : IAdminEmailRepository
    {
        private readonly IAdminEmailRepository _inner;
        public FailingDeliveryRepository(InMemoryStore store) => _inner = new InMemoryAdminEmailRepository(store);

        public AdminEmail Add(AdminEmail email) => _inner.Add(email);
        public void UpdateProgress(AdminEmail email) => _inner.UpdateProgress(email);
        public AdminEmail? GetById(int id) => _inner.GetById(id);
        public IReadOnlyList<AdminEmail> GetRecent(int take) => _inner.GetRecent(take);
        public int MarkUnfinishedInterrupted() => _inner.MarkUnfinishedInterrupted();
        public void AddFailure(AdminEmailFailure failure) => _inner.AddFailure(failure);
        public IReadOnlyList<AdminEmailFailure> GetFailures(int adminEmailId) => _inner.GetFailures(adminEmailId);
        public void AddDelivery(int adminEmailId, int userId, DateTime sentAt) => throw new InvalidOperationException("database unavailable");
        public IReadOnlyList<int> GetDeliveredUserIds(int adminEmailId) => _inner.GetDeliveredUserIds(adminEmailId);
        public bool IsOptedOut(int userId) => _inner.IsOptedOut(userId);
        public void SetOptedOut(int userId, bool optedOut) => _inner.SetOptedOut(userId, optedOut);
        public IReadOnlyList<AdminEmailRecipient> GetRecipients(UserRole? role, int? userId, bool includeOptedOut) => _inner.GetRecipients(role, userId, includeOptedOut);
        public (int Total, int OptedOut) CountRecipients(UserRole? role, int? userId) => _inner.CountRecipients(role, userId);
    }

    // A send log that's down.
    private sealed class FailingSendLogRepository : IEmailSendLogRepository
    {
        public void Add(DateTime sentAtUtc, EmailKind kind) => throw new InvalidOperationException("database unavailable");
        public IReadOnlyList<EmailSendCount> GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes) => throw new InvalidOperationException("database unavailable");
        public IReadOnlyList<DateTime> GetSentTimesSince(DateTime sinceUtc) => throw new InvalidOperationException("database unavailable");
        public int Prune(DateTime beforeUtc) => throw new InvalidOperationException("database unavailable");
    }

    // The dispatcher with its pauses skipped: each wait just moves the clock on.
    private sealed class InstantDispatcher : AdminEmailDispatcher
    {
        private readonly Harness _harness;

        public InstantDispatcher(Harness harness) : base(
            harness.Queue,
            harness.Services.GetRequiredService<IServiceScopeFactory>(),
            harness.CountingSender,
            harness.Services.GetRequiredService<IConfiguration>(),
            harness.Services.GetRequiredService<IOptionsMonitor<AdminEmailOptions>>(),
            harness.Counter,
            harness.Clock,
            NullLogger<AdminEmailDispatcher>.Instance)
        {
            _harness = harness;
        }

        protected override Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            _harness.Waits.Add((delay, _harness.Sender.Sent.Count, _harness.Store.AdminEmails.Select(e => e.PausedUntil).FirstOrDefault(p => p != null)));
            _harness.Clock.Advance(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class Harness
    {
        public required ServiceProvider Services { get; init; }
        public required InMemoryStore Store { get; init; }
        public required RecordingEmailSender Sender { get; init; }
        public required AdminEmailQueue Queue { get; init; }
        public required ManualClock Clock { get; init; }
        public required EmailSendCounter Counter { get; init; }

        /// <summary>The recording sender wrapped the way Program.cs wraps the real one: counted and logged.</summary>
        public required CountingEmailSender CountingSender { get; init; }

        /// <summary>Each pause the dispatcher took: how long, how many emails had gone, and the PausedUntil shown.</summary>
        public List<(TimeSpan Delay, int SentSoFar, DateTime? PausedUntil)> Waits { get; } = new();

        public IAdminEmailRepository Repository => Services.GetRequiredService<IAdminEmailRepository>();
        public IEmailSendLogRepository SendLog => Services.GetRequiredService<IEmailSendLogRepository>();

        public AdminEmailsController Controller(int callerUserId)
        {
            var scope = Services.CreateScope();
            return new AdminEmailsController(
                scope.ServiceProvider.GetRequiredService<IAdminEmailRepository>(),
                scope.ServiceProvider.GetRequiredService<IUserRepository>(),
                scope.ServiceProvider.GetRequiredService<IDisplaySettingsService>(),
                CountingSender,
                Queue,
                scope.ServiceProvider.GetRequiredService<IConfiguration>(),
                scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AdminEmailOptions>>(),
                scope.ServiceProvider.GetRequiredService<IEmailSendLogRepository>(),
                Counter,
                Clock)
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

        public AdminEmailDispatcher Dispatcher() => new InstantDispatcher(this);
    }

    private static Harness Create(Action<AdminEmailOptions>? configure = null, bool failDeliveryRecording = false, bool failSendLog = false)
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
        if (failDeliveryRecording)
            services.AddSingleton<IAdminEmailRepository>(new FailingDeliveryRepository(store));
        else
            services.AddSingleton<IAdminEmailRepository, InMemoryAdminEmailRepository>();
        if (failSendLog)
            services.AddSingleton<IEmailSendLogRepository>(new FailingSendLogRepository());
        else
            services.AddSingleton<IEmailSendLogRepository, InMemoryEmailSendLogRepository>();
        services.AddSingleton<MvcBB.Shared.Interfaces.IDisplaySettingsRepository, InMemoryDisplaySettingsRepository>();
        services.AddScoped<IDisplaySettingsService, DisplaySettingsService>();
        services.AddOptions();
        services.Configure<AdminEmailOptions>(o =>
        {
            o.BatchSize = 2;
            o.BatchDelaySeconds = 0;
            configure?.Invoke(o);
        });

        var clock = new ManualClock();
        var provider = services.BuildServiceProvider();
        var sender = new RecordingEmailSender();
        var counter = new EmailSendCounter(clock);
        return new Harness
        {
            Services = provider,
            Store = store,
            Sender = sender,
            Queue = new AdminEmailQueue(),
            Clock = clock,
            Counter = counter,
            CountingSender = new CountingEmailSender(sender, counter, provider.GetRequiredService<IServiceScopeFactory>(), clock, NullLogger<CountingEmailSender>.Instance)
        };
    }

    private static AdminEmail QueueEmail(Harness harness, SendAdminEmailRequest? request = null) =>
        Assert.IsType<AdminEmail>(Assert.IsType<CreatedAtActionResult>(harness.Controller(1).Send(request ?? Request()).Result).Value);

    // What a restart does to an email that was part way through.
    private static void Interrupt(Harness harness, int id)
    {
        var email = harness.Repository.GetById(id)!;
        email.Status = AdminEmailStatus.Interrupted;
        harness.Repository.UpdateProgress(email);
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
    public async Task FailedRecipient_IsRecordedWithReason_AndListedByTheApi()
    {
        var harness = Create();
        harness.Sender.FailFor = "alice@forum.test";

        var queued = Assert.IsType<AdminEmail>(Assert.IsType<CreatedAtActionResult>(harness.Controller(1).Send(Request()).Result).Value);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(new[] { "admin@gmail.com", "bob@forum.test", "carol@forum.test" }, harness.Sender.Sent.Select(m => m.To));
        var failure = Assert.Single(harness.Repository.GetFailures(queued.Id));
        Assert.Equal(2, failure.UserId);
        Assert.Equal("alice", failure.Username);
        Assert.Equal("alice@forum.test", failure.Email);
        Assert.Equal("relay refused", failure.Reason);

        var listed = Assert.IsAssignableFrom<IEnumerable<AdminEmailFailure>>(Assert.IsType<OkObjectResult>(harness.Controller(1).GetFailures(queued.Id).Result).Value);
        Assert.Equal("alice", Assert.Single(listed).Username);
        Assert.IsType<NotFoundObjectResult>(harness.Controller(1).GetFailures(999).Result);
    }

    [Fact]
    public async Task NoLimits_SendsEverythingWithoutWaiting()
    {
        var harness = Create();

        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(4, harness.Sender.Sent.Count);
        Assert.Empty(harness.Waits);
        Assert.Null(harness.Repository.GetById(queued.Id)!.PausedUntil);
    }

    [Fact]
    public async Task HourlyLimit_PausesWithReserve_ThenResumesWhenTheWindowPasses()
    {
        // 4 an hour, 1 of which is kept for the site's own emails: 3 go, then it
        // waits for the first of them to be an hour old.
        var harness = Create(o => o.MaxPerHour = 4);
        var start = harness.Clock.GetUtcNow().UtcDateTime;

        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        var firstWait = harness.Waits.First();
        Assert.Equal(3, firstWait.SentSoFar);
        Assert.Equal(start.AddHours(1), firstWait.PausedUntil);
        Assert.All(harness.Waits, w => Assert.True(w.Delay <= AdminEmailRateLimit.MaxWait));
        Assert.True(harness.Clock.GetUtcNow().UtcDateTime >= start.AddHours(1));

        Assert.Equal(4, harness.Sender.Sent.Count);
        var finished = harness.Repository.GetById(queued.Id)!;
        Assert.Equal(AdminEmailStatus.Completed, finished.Status);
        Assert.Null(finished.PausedUntil);
    }

    [Fact]
    public async Task SiteEmails_AreCountedButNeverHeldUp_AdminEmailsWaitForThem()
    {
        var harness = Create(o => o.MaxPerHour = 4);
        for (var i = 0; i < 5; i++)
        {
            await harness.CountingSender.SendAsync(new EmailMessage { To = "someone@forum.test", Subject = "Reset your password", Body = "link", Kind = EmailKind.PasswordReset });
        }

        Assert.Equal(5, harness.Sender.Sent.Count);
        Assert.Equal(5, harness.Counter.CountSince(DateTime.MinValue));

        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(5, harness.Waits.First().SentSoFar);
        Assert.Equal(9, harness.Sender.Sent.Count);
    }

    [Fact]
    public void RateLimit_KeepsFivePercentForSiteEmails()
    {
        Assert.Equal(12, AdminEmailRateLimit.Reserve(250));
        Assert.Equal(238, AdminEmailRateLimit.AdminShare(250));
        Assert.Equal(1, AdminEmailRateLimit.Reserve(10));
        Assert.Equal(0, AdminEmailRateLimit.Reserve(1));
        Assert.Null(AdminEmailRateLimit.Allowance(new SendLimits(0, 0), new EmailSendCounter(new ManualClock()), DateTime.UtcNow));
        Assert.Null(AdminEmailRateLimit.EstimatedHours(new SendLimits(250, 0), 238));
        Assert.Equal(2, AdminEmailRateLimit.EstimatedHours(new SendLimits(250, 0), 500));
    }

    [Fact]
    public void Limits_FromSettingsWin_ZeroFallsBackToAppSettings()
    {
        var options = new AdminEmailOptions { MaxPerHour = 100, MaxPerDay = 1000 };

        Assert.Equal(new SendLimits(250, 1000), SendLimits.Resolve(options, new DisplaySettings { MaxEmailsPerHour = 250 }));
        Assert.Equal(new SendLimits(100, 2000), SendLimits.Resolve(options, new DisplaySettings { MaxEmailsPerDay = 2000 }));
        Assert.Equal(new SendLimits(100, 1000), SendLimits.Resolve(options, new DisplaySettings()));
        Assert.Equal(new SendLimits(0, 0), SendLimits.Resolve(new AdminEmailOptions(), null));
    }

    [Fact]
    public async Task LimitSetInSettings_IsHonouredByTheSender()
    {
        var harness = Create();
        harness.Store.DisplaySettings.MaxEmailsPerHour = 4;

        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(3, harness.Waits.First().SentSoFar);
        Assert.Equal(4, harness.Sender.Sent.Count);
    }

    [Fact]
    public void Preview_ReportsLimitsAndEstimate()
    {
        var harness = Create(o => o.MaxPerHour = 3);

        var preview = Assert.IsType<AdminEmailPreview>(harness.Controller(1).Preview(Request()).Value);

        // 4 recipients, 2 an hour after the reserve: 2 now, the rest in about 1 hour.
        Assert.Equal(3, preview.MaxPerHour);
        Assert.Equal(2, preview.CanSendNow);
        Assert.Equal(1, preview.EstimatedHours);
    }

    [Fact]
    public void Preview_EstimateCountsWhatsAlreadyBeenSent()
    {
        // 5 an hour (4 after the reserve) fits all 4 members - unless 3 have
        // just gone out, when only 1 can go now and the rest wait an hour.
        var harness = Create(o => o.MaxPerHour = 5);
        Assert.Null(Assert.IsType<AdminEmailPreview>(harness.Controller(1).Preview(Request()).Value).EstimatedHours);

        for (var i = 0; i < 3; i++)
            harness.Counter.Record();
        var preview = Assert.IsType<AdminEmailPreview>(harness.Controller(1).Preview(Request()).Value);

        Assert.Equal(1, preview.CanSendNow);
        Assert.Equal(1, preview.EstimatedHours);
    }

    [Fact]
    public void Estimate_ReplaysTheRollingWindow()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var limits = new SendLimits(4, 0); // 3 an hour for admin emails

        // One sent 30 minutes ago: 2 go now, 1 when that one is an hour old,
        // and the last 2 when the first batch is.
        var estimate = AdminEmailRateLimit.Estimate(limits, new[] { now.AddMinutes(-30) }, now, 5);
        Assert.Equal(2, estimate.CanSendNow);
        Assert.Equal(1, estimate.Hours);

        Assert.Equal(new SendEstimate(3, null), AdminEmailRateLimit.Estimate(limits, Array.Empty<DateTime>(), now, 3));
        Assert.Equal(new SendEstimate(10, null), AdminEmailRateLimit.Estimate(new SendLimits(0, 0), new[] { now }, now, 10));

        // A daily limit that's used up waits for the day's oldest send.
        var daily = AdminEmailRateLimit.Estimate(new SendLimits(0, 21), new[] { now.AddHours(-20) }.Concat(Enumerable.Repeat(now.AddHours(-1), 19)).ToArray(), now, 1);
        Assert.Equal(0, daily.CanSendNow);
        Assert.Equal(4, daily.Hours);
    }

    [Fact]
    public async Task Resume_SkipsMembersAlreadyReached()
    {
        var harness = Create();
        var queued = QueueEmail(harness);
        harness.Repository.AddDelivery(queued.Id, 1, DateTime.UtcNow);
        harness.Repository.AddDelivery(queued.Id, 2, DateTime.UtcNow);
        Interrupt(harness, queued.Id);

        Assert.Equal(2, Assert.IsType<OkObjectResult>(harness.Controller(1).GetResumeCount(queued.Id).Result).Value);
        var resumed = Assert.IsType<AdminEmail>(Assert.IsType<OkObjectResult>(harness.Controller(1).Resume(queued.Id).Result).Value);
        Assert.Equal(AdminEmailStatus.Queued, resumed.Status);

        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(new[] { "bob@forum.test", "carol@forum.test" }, harness.Sender.Sent.Select(m => m.To));
        var finished = harness.Repository.GetById(queued.Id)!;
        Assert.Equal(AdminEmailStatus.Completed, finished.Status);
        Assert.Equal(4, finished.SentCount);
        Assert.Equal(4, finished.RecipientCount);
        Assert.Equal(new[] { 1, 2, 3, 4 }, harness.Repository.GetDeliveredUserIds(queued.Id).OrderBy(u => u));
    }

    [Fact]
    public async Task Resume_RetriesEarlierFailures_AndClearsThemOnceReached()
    {
        var harness = Create();
        harness.Sender.FailFor = "alice@forum.test";
        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);
        Assert.Equal(1, harness.Repository.GetById(queued.Id)!.FailedCount);

        Interrupt(harness, queued.Id);
        harness.Sender.FailFor = null;
        harness.Sender.Sent.Clear();
        Assert.IsType<OkObjectResult>(harness.Controller(1).Resume(queued.Id).Result);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(new[] { "alice@forum.test" }, harness.Sender.Sent.Select(m => m.To));
        var finished = harness.Repository.GetById(queued.Id)!;
        Assert.Equal(AdminEmailStatus.Completed, finished.Status);
        Assert.Equal(4, finished.SentCount);
        Assert.Equal(0, finished.FailedCount);
        var listed = Assert.IsAssignableFrom<IEnumerable<AdminEmailFailure>>(Assert.IsType<OkObjectResult>(harness.Controller(1).GetFailures(queued.Id).Result).Value);
        Assert.Empty(listed);
    }

    [Fact]
    public void Resume_OnlyForInterruptedEmails()
    {
        var harness = Create();
        var queued = QueueEmail(harness);

        Assert.IsType<BadRequestObjectResult>(harness.Controller(1).Resume(queued.Id).Result);
        Assert.IsType<NotFoundObjectResult>(harness.Controller(1).Resume(999).Result);
    }

    [Fact]
    public async Task DeliveryRecordingFailure_DoesNotStopSending()
    {
        var harness = Create(failDeliveryRecording: true);

        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(4, harness.Sender.Sent.Count);
        var finished = harness.Repository.GetById(queued.Id)!;
        Assert.Equal(AdminEmailStatus.Completed, finished.Status);
        Assert.Equal(4, finished.SentCount);
    }

    [Fact]
    public void SendCounter_IsSeededFromTheSendLog_SiteEmailsIncluded()
    {
        var harness = Create();
        var now = harness.Clock.GetUtcNow().UtcDateTime;
        harness.SendLog.Add(now.AddMinutes(-30), EmailKind.AdminEmail);
        harness.SendLog.Add(now.AddMinutes(-20), EmailKind.PasswordReset);
        harness.SendLog.Add(now.AddHours(-2), EmailKind.Notification);
        harness.SendLog.Add(now.AddHours(-25), EmailKind.AdminEmail);

        harness.Dispatcher().SeedSendCounter();

        Assert.Equal(2, harness.Counter.CountSince(now.AddHours(-1)));
        Assert.Equal(3, harness.Counter.CountSince(now.AddHours(-24)));
    }

    [Fact]
    public async Task EverySend_IsLoggedWithItsKind()
    {
        var harness = Create();
        await harness.CountingSender.SendAsync(new EmailMessage { To = "a@forum.test", Kind = EmailKind.PasswordReset });
        await harness.CountingSender.SendAsync(new EmailMessage { To = "b@forum.test", Kind = EmailKind.Notification });
        await harness.CountingSender.SendAsync("c@forum.test", "Hello", "From a mod");

        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        var now = harness.Clock.GetUtcNow().UtcDateTime;
        var counts = harness.SendLog.GetCounts(now.AddHours(-1), now.AddHours(1), 120);
        Assert.Equal(4, counts.Single(c => c.Kind == EmailKind.AdminEmail).Count);
        Assert.Equal(1, counts.Single(c => c.Kind == EmailKind.PasswordReset).Count);
        Assert.Equal(1, counts.Single(c => c.Kind == EmailKind.Notification).Count);
        Assert.Equal(1, counts.Single(c => c.Kind == EmailKind.Other).Count);
        Assert.Equal(7, harness.Counter.CountSince(DateTime.MinValue));
    }

    [Fact]
    public async Task SendLogFailure_NeverStopsAnEmail()
    {
        var harness = Create(failSendLog: true);

        await harness.CountingSender.SendAsync(new EmailMessage { To = "a@forum.test", Kind = EmailKind.PasswordReset });
        var queued = QueueEmail(harness);
        await harness.Dispatcher().SendQueuedAsync(queued.Id, CancellationToken.None);

        Assert.Equal(5, harness.Sender.Sent.Count);
        Assert.Equal(5, harness.Counter.CountSince(DateTime.MinValue));
        Assert.Equal(AdminEmailStatus.Completed, harness.Repository.GetById(queued.Id)!.Status);
    }

    [Fact]
    public void ActivityEndpoint_ReturnsBucketsLimitsAndUsage()
    {
        var harness = Create(o => o.MaxPerHour = 100);
        var now = harness.Clock.GetUtcNow().UtcDateTime;
        for (var i = 0; i < 3; i++)
        {
            harness.Counter.Record();
            harness.SendLog.Add(now, EmailKind.AdminEmail);
        }
        harness.SendLog.Add(now.AddDays(-3), EmailKind.PasswordReset);

        var hourly = Assert.IsType<EmailActivity>(Assert.IsType<OkObjectResult>(harness.Controller(1).GetActivity("24h").Result).Value);
        Assert.True(hourly.Hourly);
        Assert.Equal(24, hourly.Buckets.Count);
        Assert.Equal(3, hourly.Buckets.Sum(b => b.Total));
        Assert.Equal(3, hourly.RollingHour.Last().Count);
        Assert.Equal(100, hourly.Usage.LimitPerHour);
        Assert.Equal(5, hourly.Usage.ReservePerHour);
        Assert.Equal(3, hourly.Usage.SentLastHour);
        Assert.Equal(92, hourly.Usage.AdminAllowanceNow);

        var weekly = Assert.IsType<EmailActivity>(Assert.IsType<OkObjectResult>(harness.Controller(1).GetActivity("7d").Result).Value);
        Assert.False(weekly.Hourly);
        Assert.Equal(7, weekly.Buckets.Count);
        Assert.Equal(3, weekly.Buckets[^1].AdminEmail);
        Assert.Equal(1, weekly.Buckets[^4].PasswordReset);

        var usage = Assert.IsType<EmailSendUsage>(Assert.IsType<OkObjectResult>(harness.Controller(1).GetUsage().Result).Value);
        Assert.Equal(3, usage.SentLastHour);
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

public class EmailSendCounterTests
{
    [Fact]
    public void CountsARollingWindow_AndForgetsAfterADay()
    {
        var clock = new ManualClock();
        var counter = new EmailSendCounter(clock);
        var start = clock.GetUtcNow().UtcDateTime;

        counter.Record();
        counter.Record();
        clock.Advance(TimeSpan.FromMinutes(30));
        counter.Record();

        var now = clock.GetUtcNow().UtcDateTime;
        Assert.Equal(3, counter.CountSince(now.AddHours(-1)));
        Assert.Equal(start, counter.OldestSince(now.AddHours(-1)));

        clock.Advance(TimeSpan.FromMinutes(45));
        now = clock.GetUtcNow().UtcDateTime;
        Assert.Equal(1, counter.CountSince(now.AddHours(-1)));
        Assert.Equal(start.AddMinutes(30), counter.OldestSince(now.AddHours(-1)));
        Assert.Equal(3, counter.CountSince(now.AddHours(-24)));

        clock.Advance(TimeSpan.FromHours(24));
        Assert.Equal(0, counter.CountSince(DateTime.MinValue));
        Assert.Null(counter.OldestSince(DateTime.MinValue));
    }

    [Fact]
    public void Seed_MergesInTimeOrder_AndDropsAnythingOlderThanADay()
    {
        var clock = new ManualClock();
        var counter = new EmailSendCounter(clock);
        var now = clock.GetUtcNow().UtcDateTime;
        counter.Record();

        counter.Seed(new[] { now.AddMinutes(-10), now.AddHours(-30), now.AddHours(-3) });

        Assert.Equal(3, counter.CountSince(DateTime.MinValue));
        Assert.Equal(now.AddHours(-3), counter.OldestSince(DateTime.MinValue));
    }
}

public class EmailSendLogTests
{
    private static readonly DateTime Noon = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetCounts_GroupsBySlotAndKind()
    {
        var log = new InMemoryEmailSendLogRepository(new InMemoryStore());
        log.Add(Noon.AddMinutes(1), EmailKind.AdminEmail);
        log.Add(Noon.AddMinutes(9), EmailKind.AdminEmail);
        log.Add(Noon.AddMinutes(9), EmailKind.PasswordReset);
        log.Add(Noon.AddMinutes(10), EmailKind.AdminEmail);
        log.Add(Noon.AddMinutes(75), EmailKind.Notification);
        log.Add(Noon.AddMinutes(-1), EmailKind.Other);

        var tenMinutes = log.GetCounts(Noon, Noon.AddHours(2), 10);
        Assert.Equal(
            new[] { (Noon, EmailKind.AdminEmail, 2), (Noon, EmailKind.PasswordReset, 1), (Noon.AddMinutes(10), EmailKind.AdminEmail, 1), (Noon.AddMinutes(70), EmailKind.Notification, 1) },
            tenMinutes.Select(c => (c.BucketStart, c.Kind, c.Count)));

        var daily = log.GetCounts(Noon.AddHours(-12), Noon.AddHours(12), 1440);
        Assert.Equal(6, daily.Sum(c => c.Count));
        Assert.All(daily, c => Assert.Equal(Noon.AddHours(-12), c.BucketStart));
    }

    [Fact]
    public void Prune_RemovesOnlyOlderRows()
    {
        var log = new InMemoryEmailSendLogRepository(new InMemoryStore());
        log.Add(Noon.AddDays(-31), EmailKind.AdminEmail);
        log.Add(Noon.AddDays(-29), EmailKind.AdminEmail);
        log.Add(Noon, EmailKind.Other);

        Assert.Equal(1, log.Prune(Noon.AddDays(-30)));
        Assert.Equal(2, log.GetSentTimesSince(DateTime.MinValue).Count);
    }

    [Fact]
    public void Pruner_UsesTheRetentionSetting()
    {
        var store = new InMemoryStore();
        var services = new ServiceCollection();
        services.AddSingleton<IEmailSendLogRepository>(new InMemoryEmailSendLogRepository(store));
        services.AddOptions();
        services.Configure<AdminEmailOptions>(o => o.SendLogRetentionDays = 7);
        var provider = services.BuildServiceProvider();
        var clock = new ManualClock();
        var now = clock.GetUtcNow().UtcDateTime;
        var log = provider.GetRequiredService<IEmailSendLogRepository>();
        log.Add(now.AddDays(-8), EmailKind.AdminEmail);
        log.Add(now.AddDays(-6), EmailKind.AdminEmail);

        var pruner = new EmailSendLogPruner(provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IOptionsMonitor<AdminEmailOptions>>(), clock, NullLogger<EmailSendLogPruner>.Instance);

        Assert.Equal(1, pruner.Prune());
        Assert.Single(log.GetSentTimesSince(DateTime.MinValue));
    }

    [Fact]
    public void Chart_DrawsTheRulerAndMarksBarsOverTheLimit()
    {
        var activity = new EmailActivity
        {
            Hourly = true,
            Usage = new EmailSendUsage { LimitPerHour = 100, ReservePerHour = 5 },
            Buckets =
            {
                new EmailActivityBucket { StartUtc = Noon, EndUtc = Noon.AddHours(1), AdminEmail = 90, PasswordReset = 30 },
                new EmailActivityBucket { StartUtc = Noon.AddHours(1), EndUtc = Noon.AddHours(2), Notification = 10 }
            },
            RollingHour = { new EmailRollingPoint { AtUtc = Noon.AddMinutes(60), Count = 120 } }
        };

        var chart = new EmailActivityChart(activity, new[] { "12:00", "13:00" });

        Assert.Equal(100, chart.Limit);
        Assert.Equal(95, chart.AdminShare);
        Assert.True(chart.YMax >= 120);
        Assert.NotNull(chart.LimitY);
        Assert.True(chart.AdminShareY > chart.LimitY);
        Assert.True(chart.Bars[0].Over);
        Assert.NotNull(chart.Bars[0].OverSegment);
        Assert.Equal(2, chart.Bars[0].Segments.Count);
        Assert.False(chart.Bars[1].Over);
        Assert.True(Assert.Single(chart.RollingPoints).Over);
        Assert.Contains("over the host limit", chart.Bars[0].Describe("12:00-13:00"));
    }
}

/// <summary>A clock that only moves when told to.</summary>
public sealed class ManualClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
