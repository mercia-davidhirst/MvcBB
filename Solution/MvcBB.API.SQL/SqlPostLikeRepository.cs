using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed post-like repository, calling the usp_PostLike_* stored
    /// procedures. These procedures (and the PostLikes table they act on) are NOT
    /// checked into this repo - like every other SQL Server repository here, the
    /// schema/proc source lives externally and needs to be created to match this
    /// call shape before this repository can actually be used.
    /// </summary>
    public class SqlPostLikeRepository : IPostLikeRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlPostLikeRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool HasLiked(int postId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "dbo.usp_PostLike_HasLiked",
                new { PostId = postId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        public int GetLikeCount(int postId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<int>(
                "dbo.usp_PostLike_GetCount",
                new { PostId = postId },
                commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyDictionary<int, int> GetLikeCounts(IEnumerable<int> postIds)
        {
            // No batch stored proc for this yet - fall back to one call per post.
            return postIds.Distinct().ToDictionary(id => id, GetLikeCount);
        }

        public void AddLike(int postId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_PostLike_Add",
                new { PostId = postId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        public void RemoveLike(int postId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_PostLike_Remove",
                new { PostId = postId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;
    }
}
