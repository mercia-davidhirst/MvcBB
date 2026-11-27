using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Deletes "Remember me" refresh tokens that have expired, and used or
    /// revoked ones more than a week old, once at startup and then once a
    /// day. Spent tokens are kept for that week so a copied token is still
    /// recognised (and its sign-in revoked) if it turns up again.
    /// </summary>
    public class RefreshTokenPruner : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan KeepSpentFor = TimeSpan.FromDays(7);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<RefreshTokenPruner> _logger;

        public RefreshTokenPruner(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<RefreshTokenPruner> logger)
        {
            _scopeFactory = scopeFactory;
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
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                var removed = repository.Prune(now, now - KeepSpentFor);
                if (removed > 0)
                {
                    _logger.LogInformation("Removed {Count} expired or spent refresh token(s)", removed);
                }
                return removed;
            }
            catch (Exception ex)
            {
                // E.g. the database isn't reachable yet (or not upgraded) - try again tomorrow.
                _logger.LogError(ex, "Couldn't prune old refresh tokens");
                return 0;
            }
        }
    }
}
