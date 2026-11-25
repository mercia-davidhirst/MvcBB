using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ThreadRead;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlThreadReadRepository : IThreadReadRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlThreadReadRepository(IPostgreSqlConnectionFactory connectionFactory)
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
                "SELECT * FROM public.fn_thread_read_get_by_thread_and_user(@ThreadId, @UserId)",
                new { ThreadId = threadId, UserId = parsedUserId });
            return row == null ? null : MapToModel(row);
        }

        public void MarkRead(int threadId, string userId, int postId, DateTime postCreatedAt)
        {
            if (!int.TryParse(userId, out var parsedUserId))
            {
                return;
            }

            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_thread_read_mark_read(@ThreadId, @UserId, @LastReadPostId, @LastReadPostAt)",
                new { ThreadId = threadId, UserId = parsedUserId, LastReadPostId = postId, LastReadPostAt = postCreatedAt });
        }

        private static ThreadReadModel MapToModel(ThreadReadRow row) => new()
        {
            ThreadId = row.ThreadId,
            UserId = row.UserId.ToString(),
            LastReadPostId = row.LastReadPostId,
            LastReadPostAt = row.LastReadPostAt,
            UpdatedAt = row.UpdatedAt
        };

        /// <summary>Matches public.thread_reads' column types (user_id is an INTEGER there).</summary>
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
