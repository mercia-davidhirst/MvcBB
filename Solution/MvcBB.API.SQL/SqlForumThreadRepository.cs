using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed thread repository, calling the usp_Thread_* stored
    /// procedures in Database/SQL/Stored Procedures.
    /// </summary>
    public class SqlForumThreadRepository : IForumThreadRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlForumThreadRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<ForumThread> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>("dbo.usp_Thread_GetAll", commandType: CommandType.StoredProcedure);
            return rows.Select(MapToThread).ToList();
        }

        public IReadOnlyList<ForumThread> GetByBoardId(int boardId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>("dbo.usp_Thread_GetByBoardId", new { BoardId = boardId }, commandType: CommandType.StoredProcedure);
            return rows.Select(MapToThread).ToList();
        }

        public ForumThread? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<ThreadRow>("dbo.usp_Thread_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToThread(row);
        }

        public ForumThread Add(ForumThread thread)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<ThreadRow>("dbo.usp_Thread_Insert", new
            {
                thread.Title,
                thread.BoardId,
                CreatedByUserId = int.Parse(thread.CreatedByUserId),
                thread.CreatedAt,
                thread.IsSticky,
                thread.IsLocked,
                thread.ViewCount,
                thread.PostCount,
                thread.LastPostAt,
                LastPostByUserId = ParseUserId(thread.LastPostByUserId)
            }, commandType: CommandType.StoredProcedure);
            return MapToThread(row);
        }

        public void Update(ForumThread thread)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Thread_Update", new
            {
                thread.Id,
                thread.Title,
                thread.BoardId,
                thread.UpdatedAt,
                thread.IsSticky,
                thread.IsLocked,
                thread.ViewCount,
                thread.PostCount,
                thread.LastPostAt,
                LastPostByUserId = ParseUserId(thread.LastPostByUserId)
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(ForumThread thread)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Thread_Delete", new { thread.Id }, commandType: CommandType.StoredProcedure);
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
        /// Matches dbo.Threads' actual column types (CreatedByUserId/
        /// LastPostByUserId are INT foreign keys to Users.Id there, not
        /// strings as on the ForumThread model).
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
