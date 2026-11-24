using Microsoft.Extensions.Options;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Sends queued admin emails in the background, one email at a time, in
    /// batches of AdminEmail:BatchSize with an AdminEmail:BatchDelaySeconds
    /// pause between batches, saving progress to the send log after each
    /// batch. Recipients are worked out when sending starts, so anyone who
    /// opts out while an email waits in the queue isn't sent it.
    /// </summary>
    public class AdminEmailDispatcher : BackgroundService
    {
        private readonly AdminEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<AdminEmailOptions> _options;
        private readonly ILogger<AdminEmailDispatcher> _logger;

        public AdminEmailDispatcher(
            AdminEmailQueue queue,
            IServiceScopeFactory scopeFactory,
            IEmailSender emailSender,
            IConfiguration configuration,
            IOptionsMonitor<AdminEmailOptions> options,
            ILogger<AdminEmailDispatcher> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _emailSender = emailSender;
            _configuration = configuration;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MarkUnfinishedInterrupted();

            await foreach (var id in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await SendQueuedAsync(id, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Shutting down mid-send: the row stays Sending and the next
                    // startup marks it Interrupted.
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Admin email {AdminEmailId} failed", id);
                }
            }
        }

        // Nothing can still be sending at startup, so anything left Queued or
        // Sending was cut off by a restart. Not resumed: with no per-recipient
        // record, resuming could email the same members twice.
        private void MarkUnfinishedInterrupted()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAdminEmailRepository>();
                var count = repository.MarkUnfinishedInterrupted();
                if (count > 0)
                {
                    _logger.LogWarning("{Count} admin email(s) were cut off by a restart and marked Interrupted", count);
                }
            }
            catch (Exception ex)
            {
                // E.g. the database isn't reachable yet, or the schema hasn't
                // been updated - don't take the whole API down over it.
                _logger.LogError(ex, "Couldn't check for interrupted admin emails at startup");
            }
        }

        /// <summary>Sends one queued email (public so tests can drive it without the hosted loop).</summary>
        public async Task SendQueuedAsync(int id, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAdminEmailRepository>();
            var displaySettings = scope.ServiceProvider.GetRequiredService<IDisplaySettingsService>();

            var email = repository.GetById(id);
            if (email == null || email.Status != AdminEmailStatus.Queued)
            {
                return;
            }

            var siteName = displaySettings.GetDisplaySettings().SiteName;
            var profileUrl = AdminEmailLinks.ProfileUrl(_configuration);
            var options = _options.CurrentValue;

            var recipients = repository.GetRecipients(email.Role, email.UserId, includeOptedOut: email.IsImportant);
            email.Status = AdminEmailStatus.Sending;
            email.RecipientCount = recipients.Count;
            repository.UpdateProgress(email);

            var first = true;
            foreach (var batch in AdminEmailComposer.Batch(recipients, options.BatchSize))
            {
                if (!first && options.BatchDelaySeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(options.BatchDelaySeconds), cancellationToken);
                }
                first = false;

                foreach (var recipient in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await _emailSender.SendAsync(AdminEmailComposer.BuildMessage(email, recipient, siteName, profileUrl));
                        email.SentCount++;
                    }
                    catch (Exception ex)
                    {
                        email.FailedCount++;
                        _logger.LogWarning(ex, "Admin email {AdminEmailId} couldn't be sent to user {UserId}", email.Id, recipient.UserId);
                    }
                }

                repository.UpdateProgress(email);
            }

            email.Status = email.FailedCount > 0 ? AdminEmailStatus.CompletedWithErrors : AdminEmailStatus.Completed;
            email.CompletedAt = DateTime.UtcNow;
            repository.UpdateProgress(email);

            _logger.LogInformation(
                "Admin email {AdminEmailId} \"{Subject}\" finished: {Sent} sent, {Failed} failed",
                email.Id, email.Subject, email.SentCount, email.FailedCount);
        }
    }

    /// <summary>Links put into admin emails.</summary>
    public static class AdminEmailLinks
    {
        /// <summary>The member's own profile page, where the opt-out tick box is.</summary>
        public static string ProfileUrl(IConfiguration configuration) =>
            $"{configuration["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty}/Account/Profile";
    }
}
