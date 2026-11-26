using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Notification;

namespace MvcBB.API.SQL
{
    public class SqlNotificationRepository : INotificationRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlNotificationRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Notification Add(Notification notification)
        {
            using var connection = _connectionFactory.CreateConnection();
            notification.Id = connection.QuerySingle<int>(
                "dbo.usp_Notification_Insert",
                new
                {
                    notification.UserId,
                    Type = (byte)notification.Type,
                    notification.ActorUserId,
                    notification.ThreadId,
                    notification.PostId,
                    notification.MessageId,
                    Excerpt = Truncate(notification.Excerpt, 300)
                },
                commandType: CommandType.StoredProcedure);
            return notification;
        }

        public Notification? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<NotificationRow>(
                "dbo.usp_Notification_GetByUser",
                new { Id = id, Skip = 0, Take = 1 },
                commandType: CommandType.StoredProcedure);
            return row == null ? null : Map(row);
        }

        public (IReadOnlyList<Notification> Items, int TotalCount) GetByUser(int userId, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<NotificationRow>(
                "dbo.usp_Notification_GetByUser",
                new { UserId = userId, Skip = Math.Max(0, skip), Take = Math.Max(1, take) },
                commandType: CommandType.StoredProcedure).ToList();
            var total = rows.Count > 0 ? rows[0].TotalCount : CountAll(connection, userId, skip);
            return (rows.Select(Map).ToList(), total);
        }

        // A page past the end has no rows to carry the total; only then is it worth a second query.
        private static int CountAll(IDbConnection connection, int userId, int skip) =>
            skip <= 0 ? 0 : connection.QuerySingle<int>("SELECT COUNT(*) FROM dbo.Notifications WHERE UserId = @UserId", new { UserId = userId });

        public int CountUnread(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_Notification_CountUnread", new { UserId = userId }, commandType: CommandType.StoredProcedure);
        }

        public bool Exists(int userId, NotificationType type, int postId, int? actorUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "dbo.usp_Notification_Exists",
                new { UserId = userId, Type = (byte)type, PostId = postId, ActorUserId = actorUserId },
                commandType: CommandType.StoredProcedure);
        }

        public void MarkRead(int id, int userId) => MarkReadWhere(userId, id: id);
        public void MarkAllRead(int userId) => MarkReadWhere(userId);
        public void MarkReadForThread(int userId, int threadId) => MarkReadWhere(userId, threadId: threadId);
        public void MarkReadForMessage(int userId, int messageId) => MarkReadWhere(userId, messageId: messageId);

        private void MarkReadWhere(int userId, int? id = null, int? threadId = null, int? messageId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_Notification_MarkRead",
                new { UserId = userId, Id = id, ThreadId = threadId, MessageId = messageId },
                commandType: CommandType.StoredProcedure);
        }

        public int PruneRead(DateTime createdBefore)
        {
            var parameters = new DynamicParameters();
            parameters.Add("CreatedBefore", createdBefore, DbType.DateTime2);

            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_Notification_PruneRead", parameters, commandType: CommandType.StoredProcedure);
        }

        private static string? Truncate(string? value, int max) =>
            value == null || value.Length <= max ? value : value[..(max - 1)] + "…";

        private static Notification Map(NotificationRow row) => new()
        {
            Id = row.Id,
            UserId = row.UserId,
            Type = (NotificationType)row.Type,
            ActorUserId = row.ActorUserId,
            ActorUsername = row.ActorUsername,
            ThreadId = row.ThreadId,
            ThreadTitle = row.ThreadTitle,
            PostId = row.PostId,
            MessageId = row.MessageId,
            Excerpt = row.Excerpt,
            IsRead = row.IsRead,
            CreatedAt = row.CreatedAt
        };

        private class NotificationRow
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public byte Type { get; set; }
            public int? ActorUserId { get; set; }
            public string? ActorUsername { get; set; }
            public int? ThreadId { get; set; }
            public string? ThreadTitle { get; set; }
            public int? PostId { get; set; }
            public int? MessageId { get; set; }
            public string? Excerpt { get; set; }
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
            public int TotalCount { get; set; }
        }
    }
}
