using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed thread repository. Reads go through usp_Thread_Get*
    /// (which query dbo.vw_ThreadDetails, so the denormalized presentation
    /// fields - BoardName, BoardIsPrivate, CreatedByUsername,
    /// LastPostByUsername - come back populated, matching how SqlPostRepository
    /// already works). usp_Thread_Insert only returns the new row's Id, so
    /// Add() re-fetches the full joined row afterwards via GetById.
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
            var newId = connection.QuerySingle<int>("dbo.usp_Thread_Insert", new
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

            var row = connection.QuerySingle<ThreadRow>("dbo.usp_Thread_GetById", new { Id = newId }, commandType: CommandType.StoredProcedure);
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

        public (IReadOnlyList<ForumThread> Items, int TotalCount) GetPage(int skip, int take, bool includePrivateBoards)
        {
            using var connection = _connectionFactory.CreateConnection();
            var totalCount = connection.ExecuteScalar<int>(
                "dbo.usp_Thread_GetPageCount",
                new { IncludePrivateBoards = includePrivateBoards },
                commandType: CommandType.StoredProcedure);
            var rows = connection.Query<ThreadRow>(
                "dbo.usp_Thread_GetPage",
                new { Skip = skip, Take = take, IncludePrivateBoards = includePrivateBoards },
                commandType: CommandType.StoredProcedure);
            return (rows.Select(MapToThread).ToList(), totalCount);
        }

        public IReadOnlyDictionary<int, ForumThread> GetLastActiveThreadsByBoardIds(IEnumerable<int> boardIds)
        {
            var ids = boardIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, ForumThread>();
            }

            // A variable-length IN-list doesn't fit a stored procedure's fixed
            // parameters - this batch method goes straight against the view.
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>(
                @"SELECT Id, Title, BoardId, BoardName, BoardIsPrivate, CreatedByUserId, CreatedByUsername,
                         CreatedAt, UpdatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt,
                         LastPostByUserId, LastPostByUsername
                  FROM (
                      SELECT *, ROW_NUMBER() OVER (PARTITION BY BoardId ORDER BY COALESCE(LastPostAt, CreatedAt) DESC) AS Rn
                      FROM dbo.vw_ThreadDetails
                      WHERE BoardId IN @BoardIds
                  ) AS ranked
                  WHERE Rn = 1",
                new { BoardIds = ids });

            return rows.Select(MapToThread).ToDictionary(t => t.BoardId);
        }

        public IReadOnlyDictionary<int, ForumThread> GetByIds(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0)
            {
                return new Dictionary<int, ForumThread>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<ThreadRow>("SELECT * FROM dbo.vw_ThreadDetails WHERE Id IN @Ids", new { Ids = idList });
            return rows.Select(MapToThread).ToDictionary(t => t.Id);
        }

        public (IReadOnlyList<ForumThread> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var totalCount = connection.ExecuteScalar<int>(
                "dbo.usp_Search_ThreadsCount",
                new { Query = query, BoardId = boardId, IncludePrivateBoards = includePrivateBoards },
                commandType: CommandType.StoredProcedure);
            var rows = connection.Query<ThreadRow>(
                "dbo.usp_Search_Threads",
                new { Query = query, BoardId = boardId, IncludePrivateBoards = includePrivateBoards, Skip = skip, Take = take },
                commandType: CommandType.StoredProcedure);
            return (rows.Select(MapToThread).ToList(), totalCount);
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
        /// Matches dbo.vw_ThreadDetails' actual column types (CreatedByUserId/
        /// LastPostByUserId are INT foreign keys to Users.Id there, not strings
        /// as on the ForumThread model).
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
