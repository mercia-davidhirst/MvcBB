using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed post repository. Reads go through usp_Post_Get* (which
    /// query dbo.vw_PostDetails, so the denormalized presentation fields -
    /// ThreadTitle, CreatedByUsername, UserRole, etc. - come back populated).
    /// usp_Post_Insert only returns the new row's Id, so Add() re-fetches the
    /// full joined row afterwards via GetById.
    /// </summary>
    public class SqlPostRepository : IPostRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlPostRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Post> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>("dbo.usp_Post_GetAll", commandType: CommandType.StoredProcedure);
            return rows.Select(MapToPost).ToList();
        }

        public IReadOnlyList<Post> GetByThreadId(int threadId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>("dbo.usp_Post_GetByThreadId", new { ThreadId = threadId }, commandType: CommandType.StoredProcedure);
            return rows.Select(MapToPost).ToList();
        }

        public Post? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<PostRow>("dbo.usp_Post_GetById", new { Id = id }, commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToPost(row);
        }

        public Post Add(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>("dbo.usp_Post_Insert", new
            {
                post.Content,
                post.ThreadId,
                CreatedByUserId = int.Parse(post.CreatedByUserId),
                post.CreatedAt,
                post.QuotedPostId
            }, commandType: CommandType.StoredProcedure);

            var row = connection.QuerySingle<PostRow>("dbo.usp_Post_GetById", new { Id = newId }, commandType: CommandType.StoredProcedure);
            return MapToPost(row);
        }

        public void Update(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Post_Update", new
            {
                post.Id,
                post.Content,
                post.IsEdited,
                post.EditReason,
                post.UpdatedAt,
                UpdatedByUserId = ParseUserId(post.UpdatedByUserId),
                post.IsDeleted,
                DeletedByUserId = ParseUserId(post.DeletedByUserId),
                post.DeletedAt,
                post.DeleteReason,
                post.QuotedPostId
            }, commandType: CommandType.StoredProcedure);
        }

        public void Remove(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("dbo.usp_Post_Delete", new { post.Id }, commandType: CommandType.StoredProcedure);
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static Post MapToPost(PostRow row) => new()
        {
            Id = row.Id,
            Content = row.Content,
            ThreadId = row.ThreadId,
            ThreadTitle = row.ThreadTitle,
            CreatedByUserId = row.CreatedByUserId.ToString(),
            CreatedByUsername = row.CreatedByUsername,
            CreatedAt = row.CreatedAt,
            IsEdited = row.IsEdited,
            EditReason = row.EditReason,
            UpdatedAt = row.UpdatedAt,
            UpdatedByUserId = row.UpdatedByUserId?.ToString(),
            UpdatedByUsername = row.UpdatedByUsername,
            IsDeleted = row.IsDeleted,
            DeletedByUserId = row.DeletedByUserId?.ToString(),
            DeletedByUsername = row.DeletedByUsername,
            DeletedAt = row.DeletedAt,
            DeleteReason = row.DeleteReason,
            QuotedPostId = row.QuotedPostId,
            UserAvatar = row.UserAvatar,
            UserSignature = row.UserSignature,
            UserPostCount = row.UserPostCount,
            UserJoinedAt = row.UserJoinedAt,
            UserRole = row.UserRole
        };

        /// <summary>
        /// Matches dbo.vw_PostDetails' actual column types (the *UserId columns
        /// are INT foreign keys to Users.Id there, not strings as on the Post
        /// model).
        /// </summary>
        private class PostRow
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public int ThreadId { get; set; }
            public string ThreadTitle { get; set; } = string.Empty;
            public int CreatedByUserId { get; set; }
            public string CreatedByUsername { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsEdited { get; set; }
            public string? EditReason { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public int? UpdatedByUserId { get; set; }
            public string? UpdatedByUsername { get; set; }
            public bool IsDeleted { get; set; }
            public int? DeletedByUserId { get; set; }
            public string? DeletedByUsername { get; set; }
            public DateTime? DeletedAt { get; set; }
            public string? DeleteReason { get; set; }
            public int? QuotedPostId { get; set; }
            public string? UserAvatar { get; set; }
            public string? UserSignature { get; set; }
            public int UserPostCount { get; set; }
            public DateTime UserJoinedAt { get; set; }
            public string? UserRole { get; set; }
        }
    }
}
