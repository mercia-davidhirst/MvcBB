using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed thread repository, calling the fn_thread_*/sp_thread_*
    /// functions/procedures in Database/PostGresSql.
    /// </summary>
    public class PostgreSqlForumThreadRepository : IForumThreadRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlForumThreadRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<ForumThread> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>("SELECT * FROM public.fn_thread_get_all()");
            return rows.Select(MapToThread).ToList();
        }

        public IReadOnlyList<ForumThread> GetByBoardId(int boardId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>("SELECT * FROM public.fn_thread_get_by_board_id(@BoardId)", new { BoardId = boardId });
            return rows.Select(MapToThread).ToList();
        }

        public ForumThread? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<ThreadRow>("SELECT * FROM public.fn_thread_get_by_id(@Id)", new { Id = id });
            return row == null ? null : MapToThread(row);
        }

        public ForumThread Add(ForumThread thread)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_thread_insert(@Title, @BoardId, @CreatedByUserId, @IsSticky, @IsLocked, @ViewCount, @PostCount, @LastPostAt, @LastPostByUserId, NULL)",
                new
                {
                    thread.Title,
                    thread.BoardId,
                    CreatedByUserId = int.Parse(thread.CreatedByUserId),
                    thread.IsSticky,
                    thread.IsLocked,
                    thread.ViewCount,
                    thread.PostCount,
                    thread.LastPostAt,
                    LastPostByUserId = ParseUserId(thread.LastPostByUserId)
                });

            var row = connection.QuerySingle<ThreadRow>("SELECT * FROM public.fn_thread_get_by_id(@Id)", new { Id = newId });
            return MapToThread(row);
        }

        public void Update(ForumThread thread)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_thread_update(@Id, @Title, @BoardId, @IsSticky, @IsLocked, @ViewCount, @PostCount, @LastPostAt, @LastPostByUserId)",
                new
                {
                    thread.Id,
                    thread.Title,
                    thread.BoardId,
                    thread.IsSticky,
                    thread.IsLocked,
                    thread.ViewCount,
                    thread.PostCount,
                    thread.LastPostAt,
                    LastPostByUserId = ParseUserId(thread.LastPostByUserId)
                });
        }

        public void Remove(ForumThread thread)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_thread_delete(@Id)", new { thread.Id });
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static ForumThread MapToThread(ThreadRow row) => new()
        {
            Id = row.Id,
            Title = row.Title,
            BoardId = row.BoardId,
            CreatedByUserId = row.CreatedByUserId.ToString(),
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            IsSticky = row.IsSticky,
            IsLocked = row.IsLocked,
            ViewCount = row.ViewCount,
            PostCount = row.PostCount,
            LastPostAt = row.LastPostAt,
            LastPostByUserId = row.LastPostByUserId?.ToString()
        };

        /// <summary>
        /// Matches public.threads' actual column types (created_by_user_id/
        /// last_post_by_user_id are INTEGER foreign keys to users.id there,
        /// not strings as on the ForumThread model).
        /// </summary>
        private class ThreadRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public int BoardId { get; set; }
            public int CreatedByUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsSticky { get; set; }
            public bool IsLocked { get; set; }
            public int ViewCount { get; set; }
            public int PostCount { get; set; }
            public DateTime? LastPostAt { get; set; }
            public int? LastPostByUserId { get; set; }
        }
    }
}
