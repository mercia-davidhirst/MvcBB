using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Moderation;

namespace MvcBB.API.SQL
{
    public class SqlModerationQueueRepository : IModerationQueueRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlModerationQueueRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ModerationQueueItem Add(ModerationQueueItem item)
        {
            using var connection = _connectionFactory.CreateConnection();
            var id = connection.QuerySingle<int>(
                "dbo.usp_ModerationQueue_Insert",
                new
                {
                    Kind = (byte)item.Kind,
                    item.UserId,
                    item.BoardId,
                    item.ThreadId,
                    item.Title,
                    item.Content,
                    item.QuotedPostId,
                    item.ImageUrl,
                    item.PollJson
                },
                commandType: CommandType.StoredProcedure);
            return GetById(id) ?? item;
        }

        public ModerationQueueItem? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<QueueRow>("dbo.usp_ModerationQueue_Get", new { Id = id }, commandType: CommandType.StoredProcedure);
            return row == null ? null : Map(row);
        }

        public IReadOnlyList<ModerationQueueItem> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.Query<QueueRow>("dbo.usp_ModerationQueue_Get", commandType: CommandType.StoredProcedure)
                .Select(Map)
                .ToList();
        }

        public void Remove(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_ModerationQueue_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public int Count()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_ModerationQueue_Count", commandType: CommandType.StoredProcedure);
        }

        public int CountByUser(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_ModerationQueue_Count", new { UserId = userId }, commandType: CommandType.StoredProcedure);
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
            public byte Kind { get; set; }
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

    public class SqlModerationLogRepository : IModerationLogRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlModerationLogRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Add(ModerationLogEntry entry)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_ModerationLog_Insert",
                new { entry.ActorUserId, entry.Action, entry.TargetType, entry.TargetId, entry.TargetUserId, entry.Summary, entry.Reason },
                commandType: CommandType.StoredProcedure);
        }

        public (IReadOnlyList<ModerationLogEntry> Items, int TotalCount) GetPage(int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<LogRow>(
                "dbo.usp_ModerationLog_GetPage",
                new { Skip = Math.Max(0, skip), Take = Math.Max(1, take) },
                commandType: CommandType.StoredProcedure).ToList();
            var total = rows.Count > 0
                ? rows[0].TotalCount
                : skip <= 0 ? 0 : connection.QuerySingle<int>("SELECT COUNT(*) FROM dbo.ModerationLog");
            return (rows.Select(r => (ModerationLogEntry)r).ToList(), total);
        }

        private class LogRow : ModerationLogEntry
        {
            public int TotalCount { get; set; }
        }
    }

    public class SqlModerationSettingsRepository : IModerationSettingsRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlModerationSettingsRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ModerationSettings Get()
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<ModerationSettings>("dbo.usp_ModerationSettings_Get", commandType: CommandType.StoredProcedure)
                ?? new ModerationSettings();
        }

        public void Update(ModerationSettings settings)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_ModerationSettings_Update",
                new { settings.HoldNewMemberPosts, settings.NewMemberPostThreshold },
                commandType: CommandType.StoredProcedure);
        }
    }
}
