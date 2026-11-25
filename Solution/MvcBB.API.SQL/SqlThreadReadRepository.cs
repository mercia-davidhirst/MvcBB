using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ThreadRead;

namespace MvcBB.API.SQL
{
    public class SqlThreadReadRepository : IThreadReadRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlThreadReadRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ThreadReadModel? GetByThreadAndUser(int threadId, string userId)
        {
            if (!int.TryParse(userId, out var parsedUserId))
            {
                return null;
            }

            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<ThreadReadRow>(
                "dbo.usp_ThreadRead_GetByThreadAndUser",
                new { ThreadId = threadId, UserId = parsedUserId },
                commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToModel(row);
        }

        public void MarkRead(int threadId, string userId, int postId, DateTime postCreatedAt)
        {
            if (!int.TryParse(userId, out var parsedUserId))
            {
                return;
            }

            // Sent as DATETIME2, not Dapper's default DATETIME: DATETIME rounds
            // to 1/300 of a second, so the saved time could land just before
            // the post's own CreatedAt and leave that post looking unread.
            var parameters = new DynamicParameters();
            parameters.Add("ThreadId", threadId);
            parameters.Add("UserId", parsedUserId);
            parameters.Add("LastReadPostId", postId);
            parameters.Add("LastReadPostAt", postCreatedAt, DbType.DateTime2);

            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_ThreadRead_MarkRead", parameters, commandType: CommandType.StoredProcedure);
        }

        private static ThreadReadModel MapToModel(ThreadReadRow row) => new()
        {
            ThreadId = row.ThreadId,
            UserId = row.UserId.ToString(),
            LastReadPostId = row.LastReadPostId,
            LastReadPostAt = row.LastReadPostAt,
            UpdatedAt = row.UpdatedAt
        };

        /// <summary>Matches dbo.ThreadReads' column types (UserId is an INT there).</summary>
        private class ThreadReadRow
        {
            public int ThreadId { get; set; }
            public int UserId { get; set; }
            public int LastReadPostId { get; set; }
            public DateTime LastReadPostAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
