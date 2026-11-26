using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Deletes read notifications older than Notifications:RetentionDays
    /// (appsettings.json, default 90, at least 7), once at startup and then
    /// once a day. Unread ones are kept however old they are.
    /// </summary>
    public class NotificationPruner : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<NotificationPruner> _logger;

        public NotificationPruner(IServiceScopeFactory scopeFactory, IConfiguration configuration, TimeProvider timeProvider, ILogger<NotificationPruner> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
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

        public int Prune()
        {
            try
            {
                var days = Math.Max(7, _configuration.GetValue("Notifications:RetentionDays", 90));
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var removed = repository.PruneRead(_timeProvider.GetUtcNow().UtcDateTime.AddDays(-days));
                if (removed > 0)
                {
                    _logger.LogInformation("Removed {Count} read notification(s) older than {Days} days", removed, days);
                }
                return removed;
            }
            catch (Exception ex)
            {
                // E.g. the database isn't reachable yet (or not upgraded) - try again tomorrow.
                _logger.LogError(ex, "Couldn't prune old notifications");
                return 0;
            }
        }
    }
}
