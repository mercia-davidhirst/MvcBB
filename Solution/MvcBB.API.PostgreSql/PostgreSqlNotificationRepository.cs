using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Notification;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed notification repository (fn_notification_* /
    /// sp_notification_*). type is a SMALLINT column, so it's passed as a
    /// short for PostgreSQL to resolve the function/procedure call.
    /// </summary>
    public class PostgreSqlNotificationRepository : INotificationRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlNotificationRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Notification Add(Notification notification)
        {
            using var connection = _connectionFactory.CreateConnection();
            notification.Id = connection.QuerySingle<int>(
                "CALL public.sp_notification_insert(@UserId, @Type, @ActorUserId, @ThreadId, @PostId, @MessageId, @Excerpt, NULL)",
                new
                {
                    notification.UserId,
                    Type = (short)notification.Type,
                    notification.ActorUserId,
                    notification.ThreadId,
                    notification.PostId,
                    notification.MessageId,
                    Excerpt = Truncate(notification.Excerpt, 300)
                });
            return notification;
        }

        public Notification? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<NotificationRow>(
                "SELECT * FROM public.fn_notification_get_by_user(NULL, 0, 1, @Id)",
                new { Id = id });
            return row == null ? null : Map(row);
        }

        public (IReadOnlyList<Notification> Items, int TotalCount) GetByUser(int userId, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<NotificationRow>(
                "SELECT * FROM public.fn_notification_get_by_user(@UserId, @Skip, @Take, NULL)",
                new { UserId = userId, Skip = Math.Max(0, skip), Take = Math.Max(1, take) }).ToList();
            var total = rows.Count > 0
                ? (int)rows[0].TotalCount
                : skip <= 0 ? 0 : connection.QuerySingle<int>("SELECT COUNT(*)::integer FROM public.notifications WHERE user_id = @UserId", new { UserId = userId });
            return (rows.Select(Map).ToList(), total);
        }

        public int CountUnread(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_notification_count_unread(@UserId)", new { UserId = userId });
        }

        public bool Exists(int userId, NotificationType type, int postId, int? actorUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "SELECT public.fn_notification_exists(@UserId, @Type, @PostId, @ActorUserId)",
                new { UserId = userId, Type = (short)type, PostId = postId, ActorUserId = actorUserId });
        }

        public void MarkRead(int id, int userId) => MarkReadWhere(userId, id: id);
        public void MarkAllRead(int userId) => MarkReadWhere(userId);
        public void MarkReadForThread(int userId, int threadId) => MarkReadWhere(userId, threadId: threadId);
        public void MarkReadForMessage(int userId, int messageId) => MarkReadWhere(userId, messageId: messageId);

        private void MarkReadWhere(int userId, int? id = null, int? threadId = null, int? messageId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_notification_mark_read(@UserId, @Id, @ThreadId, @MessageId)",
                new { UserId = userId, Id = id, ThreadId = threadId, MessageId = messageId });
        }

        public int PruneRead(DateTime createdBefore)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>(
                "SELECT public.fn_notification_prune_read(@CreatedBefore)",
                new { CreatedBefore = DateTime.SpecifyKind(createdBefore, DateTimeKind.Utc) });
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
            public short Type { get; set; }
            public int? ActorUserId { get; set; }
            public string? ActorUsername { get; set; }
            public int? ThreadId { get; set; }
            public string? ThreadTitle { get; set; }
            public int? PostId { get; set; }
            public int? MessageId { get; set; }
            public string? Excerpt { get; set; }
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
            public long TotalCount { get; set; }
        }
    }
}
