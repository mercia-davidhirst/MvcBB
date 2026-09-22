using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed post repository. Reads go through fn_post_get_* (which
    /// query public.vw_post_details, so the denormalized presentation fields -
    /// thread_title, created_by_username, user_role, etc. - come back
    /// populated). The three *_username columns are derived from users.username
    /// (CITEXT), so they're explicitly cast to text here, same reasoning as
    /// PostgreSqlUserRepository. sp_post_insert only returns the new row's id,
    /// so Add() re-fetches the full joined row afterwards via GetById.
    /// </summary>
    public class PostgreSqlPostRepository : IPostRepository
    {
        private const string SelectColumns = @"
            id, content, thread_id, thread_title,
            created_by_user_id, created_by_username::text AS created_by_username,
            created_at, is_edited, edit_reason, updated_at,
            updated_by_user_id, updated_by_username::text AS updated_by_username,
            is_deleted, deleted_by_user_id, deleted_by_username::text AS deleted_by_username,
            deleted_at, delete_reason, quoted_post_id,
            user_avatar, user_signature, user_post_count, user_joined_at, user_role";

        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlPostRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IReadOnlyList<Post> GetAll()
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>($"SELECT {SelectColumns} FROM public.fn_post_get_all()");
            return rows.Select(MapToPost).ToList();
        }

        public IReadOnlyList<Post> GetByThreadId(int threadId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>($"SELECT {SelectColumns} FROM public.fn_post_get_by_thread_id(@ThreadId)", new { ThreadId = threadId });
            return rows.Select(MapToPost).ToList();
        }

        public Post? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<PostRow>($"SELECT {SelectColumns} FROM public.fn_post_get_by_id(@Id)", new { Id = id });
            return row == null ? null : MapToPost(row);
        }

        public Post Add(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_post_insert(@Content, @ThreadId, @CreatedByUserId, @QuotedPostId, NULL)",
                new
                {
                    post.Content,
                    post.ThreadId,
                    CreatedByUserId = int.Parse(post.CreatedByUserId),
                    post.QuotedPostId
                });

            var row = connection.QuerySingle<PostRow>($"SELECT {SelectColumns} FROM public.fn_post_get_by_id(@Id)", new { Id = newId });
            return MapToPost(row);
        }

        public void Update(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_post_update(@Id, @Content, @IsEdited, @EditReason, @UpdatedByUserId, @IsDeleted, @DeletedByUserId, @DeletedAt, @DeleteReason, @QuotedPostId)",
                new
                {
                    post.Id,
                    post.Content,
                    post.IsEdited,
                    post.EditReason,
                    UpdatedByUserId = ParseUserId(post.UpdatedByUserId),
                    post.IsDeleted,
                    DeletedByUserId = ParseUserId(post.DeletedByUserId),
                    post.DeletedAt,
                    post.DeleteReason,
                    post.QuotedPostId
                });
        }

        public void Remove(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute("CALL public.sp_post_delete(@Id)", new { post.Id });
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
        /// Matches public.vw_post_details' actual column types (the *_user_id
        /// columns are INTEGER foreign keys to users.id there, not strings as
        /// on the Post model).
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
