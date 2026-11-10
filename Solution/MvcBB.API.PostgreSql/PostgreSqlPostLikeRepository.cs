using Dapper;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed post-like repository, calling fn_post_like_*/sp_post_like_*
    /// functions/procedures. These (and the post_likes table they act on) are NOT
    /// checked into this repo - like every other PostgreSQL repository here, the
    /// schema/function source lives externally and needs to be created to match
    /// this call shape before this repository can actually be used.
    /// </summary>
    public class PostgreSqlPostLikeRepository : IPostLikeRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlPostLikeRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool HasLiked(int postId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "SELECT public.fn_post_like_has_liked(@PostId, @UserId)",
                new { PostId = postId, UserId = ParseUserId(userId) });
        }

        public int GetLikeCount(int postId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>(
                "SELECT public.fn_post_like_get_count(@PostId)",
                new { PostId = postId });
        }

        public IReadOnlyDictionary<int, int> GetLikeCounts(IEnumerable<int> postIds)
        {
            // No batch function for this yet - fall back to one call per post.
            return postIds.Distinct().ToDictionary(id => id, GetLikeCount);
        }

        public void AddLike(int postId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_post_like_add(@PostId, @UserId)",
                new { PostId = postId, UserId = ParseUserId(userId) });
        }

        public void RemoveLike(int postId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_post_like_remove(@PostId, @UserId)",
                new { PostId = postId, UserId = ParseUserId(userId) });
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;
    }
}
