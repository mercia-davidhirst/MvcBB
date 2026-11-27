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
            deleted_at, delete_reason, quoted_post_id, image_url,
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

        public IReadOnlyList<Post> GetThreadPage(int threadId, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>(
                $"SELECT {SelectColumns} FROM public.fn_post_get_thread_page(@ThreadId, @Skip, @Take)",
                new { ThreadId = threadId, Skip = Math.Max(0, skip), Take = Math.Max(1, take) });
            return rows.Select(MapToPost).ToList();
        }

        public ThreadPostLocation GetThreadLocation(int threadId, int? postId = null, DateTime? unreadAfter = null, int? excludeUserId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<ThreadPostLocation>(
                "SELECT total_count, post_id, post_position AS position FROM public.fn_post_get_thread_location(@ThreadId, @PostId, @UnreadAfter, @ExcludeUserId)",
                new
                {
                    ThreadId = threadId,
                    PostId = postId,
                    UnreadAfter = unreadAfter.HasValue ? DateTime.SpecifyKind(unreadAfter.Value, DateTimeKind.Utc) : (DateTime?)null,
                    ExcludeUserId = excludeUserId
                });
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
                "CALL public.sp_post_insert(@Content, @ThreadId, @CreatedByUserId, @QuotedPostId, @ImageUrl, NULL)",
                new
                {
                    post.Content,
                    post.ThreadId,
                    CreatedByUserId = int.Parse(post.CreatedByUserId),
                    post.QuotedPostId,
                    post.ImageUrl
                });

            var row = connection.QuerySingle<PostRow>($"SELECT {SelectColumns} FROM public.fn_post_get_by_id(@Id)", new { Id = newId });
            return MapToPost(row);
        }

        public void Update(Post post)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_post_update(@Id, @Content, @IsEdited, @EditReason, @UpdatedByUserId, @IsDeleted, @DeletedByUserId, @DeletedAt, @DeleteReason, @QuotedPostId, @ImageUrl)",
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
                    post.QuotedPostId,
                    post.ImageUrl
                });
        }


        public bool HasPostInThread(int threadId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var exists = connection.ExecuteScalar<int?>(
                "SELECT 1 FROM public.posts WHERE thread_id = @ThreadId AND created_by_user_id = @UserId LIMIT 1",
                new { ThreadId = threadId, UserId = ParseUserId(userId) });
            return exists.HasValue;
        }

        public IReadOnlyDictionary<int, Post> GetByIds(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToArray();
            if (idList.Length == 0)
            {
                return new Dictionary<int, Post>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>($"SELECT {SelectColumns} FROM public.vw_post_details WHERE id = ANY(@Ids)", new { Ids = idList });
            return rows.Select(MapToPost).ToDictionary(p => p.Id);
        }

        public (IReadOnlyList<Post> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take)
        {
            using var connection = _connectionFactory.CreateConnection();
            var totalCount = connection.ExecuteScalar<long>(
                "SELECT public.fn_search_posts_count(@Query, @BoardId, @IncludePrivateBoards)",
                new { Query = query, BoardId = boardId, IncludePrivateBoards = includePrivateBoards });
            var rows = connection.Query<PostRow>(
                $"SELECT {SelectColumns} FROM public.fn_search_posts(@Query, @BoardId, @IncludePrivateBoards, @Skip, @Take)",
                new { Query = query, BoardId = boardId, IncludePrivateBoards = includePrivateBoards, Skip = skip, Take = take });
            return (rows.Select(MapToPost).ToList(), (int)totalCount);
        }

        public IReadOnlyList<Post> GetByUserId(int userId, int take, bool includePrivateBoards, bool includeInactiveBoards)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PostRow>(
                $"SELECT {SelectColumns} FROM public.fn_post_get_by_user_id(@UserId, @Take, @IncludePrivateBoards, @IncludeInactiveBoards)",
                new { UserId = userId, Take = take, IncludePrivateBoards = includePrivateBoards, IncludeInactiveBoards = includeInactiveBoards });
            return rows.Select(MapToPost).ToList();
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
            ImageUrl = row.ImageUrl,
            IsImportedData = row.IsImportedData,
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
            public string? ImageUrl { get; set; }
            public bool IsImportedData { get; set; }
            public string? UserAvatar { get; set; }
            public string? UserSignature { get; set; }
            public int UserPostCount { get; set; }
            public DateTime UserJoinedAt { get; set; }
            public string? UserRole { get; set; }
        }
    }
}
