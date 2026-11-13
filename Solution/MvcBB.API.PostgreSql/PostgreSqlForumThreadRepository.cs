using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed thread repository. Reads go through fn_thread_get_*
    /// (which query public.vw_thread_details, so the denormalized presentation
    /// fields - board_name, board_is_private, created_by_username,
    /// last_post_by_username - come back populated, matching how
    /// PostgreSqlPostRepository already works). sp_thread_insert only returns
    /// the new row's id, so Add() re-fetches the full joined row afterwards
    /// via GetById.
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

        public (IReadOnlyList<ForumThread> Items, int TotalCount) GetPage(int skip, int take, bool includePrivateBoards)
        {
            using var connection = _connectionFactory.CreateConnection();
            var totalCount = connection.ExecuteScalar<long>(
                "SELECT public.fn_thread_get_page_count(@IncludePrivateBoards)",
                new { IncludePrivateBoards = includePrivateBoards });
            var rows = connection.Query<ThreadRow>(
                "SELECT * FROM public.fn_thread_get_page(@Skip, @Take, @IncludePrivateBoards)",
                new { Skip = skip, Take = take, IncludePrivateBoards = includePrivateBoards });
            return (rows.Select(MapToThread).ToList(), (int)totalCount);
        }

        public IReadOnlyDictionary<int, ForumThread> GetLastActiveThreadsByBoardIds(IEnumerable<int> boardIds)
        {
            var ids = boardIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<int, ForumThread>();
            }

            // A variable-length IN-list doesn't fit a stored function's fixed
            // parameters - this batch method goes straight against the view.
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>(
                @"SELECT DISTINCT ON (board_id) *
                  FROM public.vw_thread_details
                  WHERE board_id = ANY(@BoardIds)
                  ORDER BY board_id, COALESCE(last_post_at, created_at) DESC",
                new { BoardIds = ids });

            return rows.Select(MapToThread).ToDictionary(t => t.BoardId);
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static ForumThread MapToThread(ThreadRow row) => new()
        {
            Id = row.Id,
            Title = row.Title,
            BoardId = row.BoardId,
            BoardName = row.BoardName,
            BoardIsPrivate = row.BoardIsPrivate,
            CreatedByUserId = row.CreatedByUserId.ToString(),
            CreatedByUsername = row.CreatedByUsername,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            IsSticky = row.IsSticky,
            IsLocked = row.IsLocked,
            ViewCount = row.ViewCount,
            PostCount = row.PostCount,
            LastPostAt = row.LastPostAt,
            LastPostByUserId = row.LastPostByUserId?.ToString(),
            LastPostByUsername = row.LastPostByUsername
        };

        /// <summary>
        /// Matches public.vw_thread_details' actual column types
        /// (created_by_user_id/last_post_by_user_id are INTEGER foreign keys
        /// to users.id there, not strings as on the ForumThread model).
        /// </summary>
        private class ThreadRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public int BoardId { get; set; }
            public string BoardName { get; set; } = string.Empty;
            public bool BoardIsPrivate { get; set; }
            public int CreatedByUserId { get; set; }
            public string CreatedByUsername { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsSticky { get; set; }
            public bool IsLocked { get; set; }
            public int ViewCount { get; set; }
            public int PostCount { get; set; }
            public DateTime? LastPostAt { get; set; }
            public int? LastPostByUserId { get; set; }
            public string? LastPostByUsername { get; set; }
        }
    }
}
