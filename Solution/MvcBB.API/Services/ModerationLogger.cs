using System.Security.Claims;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Moderation;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Writes moderator actions to the moderation log (see
    /// ModerationActions). Only actions by Administrators and Moderators are
    /// logged, and a failure to write the entry never undoes or blocks the
    /// action itself.
    /// </summary>
    public interface IModerationLogger
    {
        void Log(string action, string targetType, int? targetId, int? targetUserId, string summary, string? reason = null);
    }

    public class ModerationLogger : IModerationLogger
    {
        private readonly IModerationLogRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ModerationLogger> _logger;

        public ModerationLogger(IModerationLogRepository repository, IHttpContextAccessor httpContextAccessor, ILogger<ModerationLogger> logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void Log(string action, string targetType, int? targetId, int? targetUserId, string summary, string? reason = null)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null
                || !(user.IsInRole("Administrator") || user.IsInRole("Moderator"))
                || !int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actorId))
            {
                return;
            }

            try
            {
                _repository.Add(new ModerationLogEntry
                {
                    ActorUserId = actorId,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId,
                    TargetUserId = targetUserId,
                    Summary = Trim(summary, 500) ?? string.Empty,
                    Reason = Trim(string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), 500)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't write moderation log entry {Action} for {TargetType} {TargetId}", action, targetType, targetId);
            }
        }

        private static string? Trim(string? value, int max) =>
            value == null || value.Length <= max ? value : value[..(max - 1)] + "…";

        /// <summary>The numeric user id stored as a string on posts, threads and users, or null.</summary>
        public static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;
    }
}
