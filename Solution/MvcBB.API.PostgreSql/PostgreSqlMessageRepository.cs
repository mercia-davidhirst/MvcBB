using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed message repository, calling the fn_message_*/sp_message_*
    /// functions/procedures in Database/PostGresSql.
    /// </summary>
    public class PostgreSqlMessageRepository : IMessageRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlMessageRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Message> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<MessageRow>("SELECT * FROM public.fn_message_get_all()");
            return rows.Select(MapToMessage).ToList();
        }

        public IReadOnlyList<Message> GetByRecipientUserId(string userId, bool unreadOnly = false)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<MessageRow>(
                "SELECT * FROM public.fn_message_get_by_recipient_user_id(@RecipientUserId, @UnreadOnly)",
                new { RecipientUserId = int.Parse(userId), UnreadOnly = unreadOnly });
            return rows.Select(MapToMessage).ToList();
        }

        public IReadOnlyList<Message> GetByRecipientOrSenderUserId(string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<MessageRow>("SELECT * FROM public.fn_message_get_by_recipient_or_sender_user_id(@UserId)", new { UserId = int.Parse(userId) });
            return rows.Select(MapToMessage).ToList();
        }

        public Message? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<MessageRow>("SELECT * FROM public.fn_message_get_by_id(@Id)", new { Id = id });
            return row == null ? null : MapToMessage(row);
        }

        public Message Add(Message message)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_message_insert(@Content, @SenderUserId, @RecipientUserId, @Subject, NULL)",
                new
                {
                    message.Content,
                    SenderUserId = int.Parse(message.SenderUserId),
                    RecipientUserId = int.Parse(message.RecipientUserId),
                    message.Subject
                });

            var row = connection.QuerySingle<MessageRow>("SELECT * FROM public.fn_message_get_by_id(@Id)", new { Id = newId });
            return MapToMessage(row);
        }

        public void Update(Message message)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_message_update(@Id, @Subject, @Content, @ReadAt)",
                new { message.Id, message.Subject, message.Content, message.ReadAt });
        }

        public void Remove(Message message)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_message_delete(@Id)", new { message.Id });
        }

        public int GetUnreadCountByRecipientUserId(string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("SELECT public.fn_message_get_unread_count_by_recipient_user_id(@RecipientUserId)", new { RecipientUserId = int.Parse(userId) });
        }

        private static Message MapToMessage(MessageRow row) => new()
        {
            Id = row.Id,
            Subject = row.Subject,
            Content = row.Content,
            SenderUserId = row.SenderUserId.ToString(),
            RecipientUserId = row.RecipientUserId.ToString(),
            CreatedAt = row.CreatedAt,
            ReadAt = row.ReadAt
        };

        /// <summary>
        /// Matches public.messages' actual column types (sender_user_id/
        /// recipient_user_id are INTEGER foreign keys to users.id there, not
        /// strings as on the Message model).
        /// </summary>
        private class MessageRow
        {
            public int Id { get; set; }
            public string Subject { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public int SenderUserId { get; set; }
            public int RecipientUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ReadAt { get; set; }
        }
    }
}
