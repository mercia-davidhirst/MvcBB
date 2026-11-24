using Microsoft.Extensions.Options;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Sends queued admin emails in the background, one email at a time, in
    /// batches of AdminEmail:BatchSize with an AdminEmail:BatchDelaySeconds
    /// pause between batches, saving progress to the send log after each
    /// batch. When the host's sending limits are set (Admin &gt; Settings, or
    /// AdminEmail:MaxPerHour / MaxPerDay) it also waits whenever the next
    /// email would go over them (see AdminEmailRateLimit).
    /// Recipients are worked out when sending starts, so anyone who opts out
    /// while an email waits in the queue isn't sent it. Each member reached is
    /// recorded, so a resumed email skips them.
    /// </summary>
    public class AdminEmailDispatcher : BackgroundService
    {
        private readonly AdminEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<AdminEmailOptions> _options;
        private readonly EmailSendCounter _sendCounter;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AdminEmailDispatcher> _logger;

        public AdminEmailDispatcher(
            AdminEmailQueue queue,
            IServiceScopeFactory scopeFactory,
            IEmailSender emailSender,
            IConfiguration configuration,
            IOptionsMonitor<AdminEmailOptions> options,
            EmailSendCounter sendCounter,
            TimeProvider timeProvider,
            ILogger<AdminEmailDispatcher> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _emailSender = emailSender;
            _configuration = configuration;
            _options = options;
            _sendCounter = sendCounter;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MarkUnfinishedInterrupted();
            SeedSendCounter();

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
        // Sending was cut off by a restart. Not resumed automatically: an admin
        // decides whether the rest still need it (Resume on the details page).
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

        /// <summary>
        /// Refills the send-limit count from the send log's last 24 hours, so
        /// a restart doesn't hand out the whole limit again. Public so tests
        /// can drive it.
        /// </summary>
        public void SeedSendCounter()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sendLog = scope.ServiceProvider.GetRequiredService<IEmailSendLogRepository>();
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                _sendCounter.Seed(sendLog.GetSentTimesSince(now - EmailSendCounter.Window));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't load the email send log for the sending limits at startup");
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

            // A resumed email skips everyone it already reached, and tries again
            // for anyone it failed for. The counts cover the whole email, not
            // just this run: SentCount = members reached, FailedCount = members
            // it failed for who haven't been reached since.
            var delivered = repository.GetDeliveredUserIds(email.Id).ToHashSet();
            var failed = repository.GetFailures(email.Id).Select(f => f.UserId).Where(u => !delivered.Contains(u)).ToHashSet();
            var recipients = repository.GetRecipients(email.Role, email.UserId, includeOptedOut: email.IsImportant)
                .Where(r => !delivered.Contains(r.UserId))
                .ToList();
            var remainingIds = recipients.Select(r => r.UserId).ToHashSet();

            email.Status = AdminEmailStatus.Sending;
            email.CompletedAt = null;
            email.PausedUntil = null;
            email.SentCount = delivered.Count;
            email.FailedCount = failed.Count;
            // Earlier failures no longer in the audience (e.g. they've opted out
            // since) aren't retried, but still count towards the total.
            email.RecipientCount = delivered.Count + recipients.Count + failed.Count(u => !remainingIds.Contains(u));
            repository.UpdateProgress(email);

            // The limits come from Admin > Settings (else appsettings.json), read
            // again each batch and while waiting, so a change applies part way.
            SendLimits ReadLimits() => SendLimits.Resolve(_options.CurrentValue, displaySettings.GetDisplaySettings());

            var first = true;
            foreach (var batch in AdminEmailComposer.Batch(recipients, options.BatchSize))
            {
                if (!first && options.BatchDelaySeconds > 0)
                {
                    await WaitAsync(TimeSpan.FromSeconds(options.BatchDelaySeconds), cancellationToken);
                }
                first = false;
                var limits = ReadLimits();

                foreach (var recipient in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    limits = await WaitForSendLimitAsync(email, repository, limits, ReadLimits, cancellationToken);
                    try
                    {
                        await _emailSender.SendAsync(AdminEmailComposer.BuildMessage(email, recipient, siteName, profileUrl));
                    }
                    catch (Exception ex)
                    {
                        if (failed.Add(recipient.UserId))
                        {
                            email.FailedCount++;
                        }
                        _logger.LogWarning(ex, "Admin email {AdminEmailId} couldn't be sent to user {UserId}", email.Id, recipient.UserId);
                        RecordFailure(repository, email.Id, recipient, ex);
                        continue;
                    }

                    email.SentCount++;
                    if (failed.Remove(recipient.UserId))
                    {
                        email.FailedCount--;
                    }
                    RecordDelivery(repository, email.Id, recipient);
                }

                repository.UpdateProgress(email);
            }

            email.Status = email.FailedCount > 0 ? AdminEmailStatus.CompletedWithErrors : AdminEmailStatus.Completed;
            email.CompletedAt = DateTime.UtcNow;
            email.PausedUntil = null;
            repository.UpdateProgress(email);

            _logger.LogInformation(
                "Admin email {AdminEmailId} \"{Subject}\" finished: {Sent} sent, {Failed} failed",
                email.Id, email.Subject, email.SentCount, email.FailedCount);
        }

        // Holds the next admin email back while the host's sending limits (less
        // the share kept for the site's own emails) are used up. Shows when it
        // expects to carry on via PausedUntil, and re-checks at least every
        // AdminEmailRateLimit.MaxWait, re-reading the limits in case they're
        // changed meanwhile. Returns the limits last read.
        private async Task<SendLimits> WaitForSendLimitAsync(
            AdminEmail email, IAdminEmailRepository repository, SendLimits limits, Func<SendLimits> readLimits, CancellationToken cancellationToken)
        {
            while (true)
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var allowance = AdminEmailRateLimit.Allowance(limits, _sendCounter, now);
                if (allowance == null || allowance > 0)
                {
                    break;
                }

                var resumeAt = AdminEmailRateLimit.ResumeTime(limits, _sendCounter, now);
                if (email.PausedUntil != resumeAt)
                {
                    email.PausedUntil = resumeAt;
                    repository.UpdateProgress(email);
                }

                var wait = resumeAt - now;
                await WaitAsync(wait > AdminEmailRateLimit.MaxWait ? AdminEmailRateLimit.MaxWait : wait, cancellationToken);
                limits = readLimits();
            }

            if (email.PausedUntil != null)
            {
                email.PausedUntil = null;
                repository.UpdateProgress(email);
            }
            return limits;
        }

        /// <summary>Every pause goes through here (on the TimeProvider), so tests can skip the waiting.</summary>
        protected virtual Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            Task.Delay(delay, _timeProvider, cancellationToken);

        // Saves who the email reached, so a resume skips them. A problem saving
        // it is only logged: it mustn't stop the rest sending.
        private void RecordDelivery(IAdminEmailRepository repository, int adminEmailId, AdminEmailRecipient recipient)
        {
            try
            {
                repository.AddDelivery(adminEmailId, recipient.UserId, _timeProvider.GetUtcNow().UtcDateTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't record that admin email {AdminEmailId} reached user {UserId}", adminEmailId, recipient.UserId);
            }
        }

        // Saves who an email couldn't be sent to, for the details page. A
        // problem saving it is only logged: it mustn't stop the rest sending.
        private void RecordFailure(IAdminEmailRepository repository, int adminEmailId, AdminEmailRecipient recipient, Exception error)
        {
            try
            {
                repository.AddFailure(new AdminEmailFailure
                {
                    AdminEmailId = adminEmailId,
                    UserId = recipient.UserId,
                    Username = recipient.Username,
                    Email = recipient.Email,
                    Reason = AdminEmailComposer.FailureReason(error),
                    FailedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't record that admin email {AdminEmailId} failed for user {UserId}", adminEmailId, recipient.UserId);
            }
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
