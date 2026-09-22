using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed message repository, calling the usp_Message_* stored
    /// procedures in Database/SQL/Stored Procedures.
    /// </summary>
    public class SqlMessageRepository : IMessageRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlMessageRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Message> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<MessageRow>("dbo.usp_Message_GetAll", commandType: CommandType.StoredProcedure);
            return rows.Select(MapToMessage).ToList();
        }

        public IReadOnlyList<Message> GetByRecipientUserId(string userId, bool unreadOnly = false)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<MessageRow>("dbo.usp_Message_GetByRecipientUserId", new
            {
                RecipientUserId = int.Parse(userId),
                UnreadOnly = unreadOnly
            }, commandType: CommandType.StoredProcedure);
            return rows.Select(MapToMessage).ToList();
        }

        public IReadOnlyList<Message> GetByRecipientOrSenderUserId(string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<MessageRow>("dbo.usp_Message_GetByRecipientOrSenderUserId", new { UserId = int.Parse(userId) }, commandType: CommandType.StoredProcedure);
            return rows.Select(MapToMessage).ToList();
        }

        public Message? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<MessageRow>("dbo.usp_Message_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToMessage(row);
        }

        public Message Add(Message message)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<MessageRow>("dbo.usp_Message_Insert", new
            {
                message.Subject,
                message.Content,
                SenderUserId = int.Parse(message.SenderUserId),
                RecipientUserId = int.Parse(message.RecipientUserId),
                message.CreatedAt
            }, commandType: CommandType.StoredProcedure);
            return MapToMessage(row);
        }

        public void Update(Message message)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Message_Update", new
            {
                message.Id,
                message.Subject,
                message.Content,
                message.ReadAt
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(Message message)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Message_Delete", new { message.Id }, commandType: CommandType.StoredProcedure);
        }

        public int GetUnreadCountByRecipientUserId(string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>("dbo.usp_Message_GetUnreadCountByRecipientUserId", new { RecipientUserId = int.Parse(userId) }, commandType: CommandType.StoredProcedure);
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
        /// Matches dbo.Messages' actual column types (SenderUserId/
        /// RecipientUserId are INT foreign keys to Users.Id there, not
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
