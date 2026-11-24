using Microsoft.Extensions.Options;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Deletes send log rows older than AdminEmail:SendLogRetentionDays (at
    /// least 1), once at startup and then once a day.
    /// </summary>
    public class EmailSendLogPruner : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<AdminEmailOptions> _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<EmailSendLogPruner> _logger;

        public EmailSendLogPruner(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<AdminEmailOptions> options,
            TimeProvider timeProvider,
            ILogger<EmailSendLogPruner> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Prune();
                try
                {
                    await Task.Delay(Interval, _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>Deletes the old rows now. Public so tests can drive it.</summary>
        public int Prune()
        {
            try
            {
                var days = Math.Max(1, _options.CurrentValue.SendLogRetentionDays);
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IEmailSendLogRepository>();
                var removed = repository.Prune(_timeProvider.GetUtcNow().UtcDateTime.AddDays(-days));
                if (removed > 0)
                {
                    _logger.LogInformation("Removed {Count} email send log row(s) older than {Days} days", removed, days);
                }
                return removed;
            }
            catch (Exception ex)
            {
                // E.g. the database isn't reachable yet - try again tomorrow.
                _logger.LogError(ex, "Couldn't prune the email send log");
                return 0;
            }
        }
    }
}
