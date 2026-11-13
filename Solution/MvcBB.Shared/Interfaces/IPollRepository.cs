using MvcBB.Shared.Models.Poll;

namespace MvcBB.Shared.Interfaces
{
    public interface IPollRepository
    {
        Poll? GetByThreadId(int threadId);
        Poll? GetById(int id);

        /// <summary>
        /// Persists the poll and every option in poll.Options together,
        /// returning the poll with all Ids populated.
        /// </summary>
        Poll Add(Poll poll);

        bool HasVoted(int pollId, string userId);

        /// <summary>
        /// Records a vote. Throws if the user already voted on this poll -
        /// callers should check HasVoted first, but this is the real guard.
        /// </summary>
        void AddVote(int pollId, int pollOptionId, string userId);

        /// <summary>Vote counts keyed by PollOptionId.</summary>
        IReadOnlyDictionary<int, int> GetVoteCounts(int pollId);

        /// <summary>Which option (if any) this user voted for.</summary>
        int? GetVotedOptionId(int pollId, string userId);

        /// <summary>The poll for each of the given threads, in one query - absent if a thread has no poll.</summary>
        IReadOnlyDictionary<int, Poll> GetByThreadIds(IEnumerable<int> threadIds);

        /// <summary>Vote counts (keyed by PollOptionId) for several polls at once, keyed by PollId.</summary>
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, int>> GetVoteCountsByPollIds(IEnumerable<int> pollIds);

        /// <summary>Which option (if any) this user voted for, for several polls at once, keyed by PollId.</summary>
        IReadOnlyDictionary<int, int> GetVotedOptionIdsByPollIds(IEnumerable<int> pollIds, string userId);
    }
}
