using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Moderation;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlModerationQueueRepository : IModerationQueueRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlModerationQueueRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ModerationQueueItem Add(ModerationQueueItem item)
        {
            using var connection = _connectionFactory.CreateConnection();
            var id = connection.QuerySingle<int>(
                "CALL public.sp_moderation_queue_insert(@Kind, @UserId, @BoardId, @ThreadId, @Title, @Content, @QuotedPostId, @ImageUrl, @PollJson, NULL)",
                new
                {
                    Kind = (short)item.Kind,
                    item.UserId,
                    item.BoardId,
                    item.ThreadId,
                    item.Title,
                    item.Content,
                    item.QuotedPostId,
                    item.ImageUrl,
                    item.PollJson
                });
            return GetById(id) ?? item;
        }

        public ModerationQueueItem? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<QueueRow>("SELECT * FROM public.fn_moderation_queue_get(@Id)", new { Id = id });
            return row == null ? null : Map(row);
        }

        public IReadOnlyList<ModerationQueueItem> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<QueueRow>("SELECT * FROM public.fn_moderation_queue_get(NULL)").Select(Map).ToList();
        }

        public void Remove(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_moderation_queue_delete(@Id)", new { Id = id });
        }

        public int Count()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_moderation_queue_count(NULL)");
        }

        public int CountByUser(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_moderation_queue_count(@UserId)", new { UserId = userId });
        }

        private static ModerationQueueItem Map(QueueRow row) => new()
        {
            Id = row.Id,
            Kind = (QueuedPostKind)row.Kind,
            UserId = row.UserId,
            Username = row.Username,
            BoardId = row.BoardId,
            BoardName = row.BoardName,
            ThreadId = row.ThreadId,
            ThreadTitle = row.ThreadTitle,
            Title = row.Title,
            Content = row.Content,
            QuotedPostId = row.QuotedPostId,
            ImageUrl = row.ImageUrl,
            PollJson = row.PollJson,
            CreatedAt = row.CreatedAt
        };

        private class QueueRow
        {
            public int Id { get; set; }
            public short Kind { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public int BoardId { get; set; }
            public string? BoardName { get; set; }
            public int? ThreadId { get; set; }
            public string? ThreadTitle { get; set; }
            public string? Title { get; set; }
            public string Content { get; set; } = string.Empty;
            public int? QuotedPostId { get; set; }
            public string? ImageUrl { get; set; }
            public string? PollJson { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

    public class PostgreSqlModerationLogRepository : IModerationLogRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlModerationLogRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Add(ModerationLogEntry entry)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_moderation_log_insert(@ActorUserId, @Action, @TargetType, @TargetId, @TargetUserId, @Summary, @Reason)",
                new { entry.ActorUserId, entry.Action, entry.TargetType, entry.TargetId, entry.TargetUserId, entry.Summary, entry.Reason });
        }

        public (IReadOnlyList<ModerationLogEntry> Items, int TotalCount) GetPage(int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<LogRow>(
                "SELECT * FROM public.fn_moderation_log_get_page(@Skip, @Take)",
                new { Skip = Math.Max(0, skip), Take = Math.Max(1, take) }).ToList();
            var total = rows.Count > 0
                ? (int)rows[0].TotalCount
                : skip <= 0 ? 0 : connection.QuerySingle<int>("SELECT COUNT(*)::integer FROM public.moderation_log");
            return (rows.Select(r => (ModerationLogEntry)r).ToList(), total);
        }

        private class LogRow : ModerationLogEntry
        {
            public long TotalCount { get; set; }
        }
    }

    public class PostgreSqlModerationSettingsRepository : IModerationSettingsRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlModerationSettingsRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ModerationSettings Get()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<ModerationSettings>("SELECT hold_new_member_posts, new_member_post_threshold FROM public.fn_moderation_settings_get()")
                ?? new ModerationSettings();
        }

        public void Update(ModerationSettings settings)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_moderation_settings_update(@HoldNewMemberPosts, @NewMemberPostThreshold)",
                new { settings.HoldNewMemberPosts, settings.NewMemberPostThreshold });
        }
    }
}
