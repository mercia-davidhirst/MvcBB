using MvcBB.Shared.Models.Rank;

namespace MvcBB.Shared.Services
{
    /// <summary>
    /// Pure computation, no repository access - callers fetch the active
    /// ranks once (e.g. per request) and reuse this against as many post
    /// counts as they need.
    /// </summary>
    public static class RankCalculator
    {
        /// <summary>
        /// The highest-threshold active rank the given post count qualifies
        /// for, or null if none do (e.g. a brand new poster below the lowest
        /// MinPosts).
        /// </summary>
        public static RankModel? GetRank(IEnumerable<RankModel> ranks, int postCount)
        {
            return ranks
                .Where(r => r.IsActive && r.MinPosts <= postCount)
                .OrderByDescending(r => r.MinPosts)
                .FirstOrDefault();
        }
    }
}
