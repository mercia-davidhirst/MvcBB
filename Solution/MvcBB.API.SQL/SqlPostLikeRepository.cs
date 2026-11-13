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
            var ids = postIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            // A variable-length IN-list doesn't fit a stored procedure's fixed
            // parameters - this batch method goes straight against the table
            // instead of through a usp_PostLike_* procedure.
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<LikeCountRow>(
                "SELECT PostId, COUNT(*) AS LikeCount FROM dbo.PostLikes WHERE PostId IN @PostIds GROUP BY PostId",
                new { PostIds = ids });
            return rows.ToDictionary(r => r.PostId, r => r.LikeCount);
        }

        private class LikeCountRow
        {
            public int PostId { get; set; }
            public int LikeCount { get; set; }
        }

        public ISet<int> GetLikedPostIds(IEnumerable<int> postIds, string userId)
        {
            var ids = postIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new HashSet<int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var likedPostIds = connection.Query<int>(
                "SELECT PostId FROM dbo.PostLikes WHERE PostId IN @PostIds AND UserId = @UserId",
                new { PostIds = ids, UserId = ParseUserId(userId) });
            return likedPostIds.ToHashSet();
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
