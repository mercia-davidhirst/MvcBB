using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Poll;

namespace MvcBB.API.InMemory
{
    public class InMemoryPollRepository : IPollRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryPollRepository(InMemoryStore store)
        {
            _store = store;
        }

        public Poll? GetByThreadId(int threadId)
        {
            var poll = _store.Polls.FirstOrDefault(p => p.ThreadId == threadId);
            return poll == null ? null : WithOptions(poll);
        }

        public Poll? GetById(int id)
        {
            var poll = _store.Polls.FirstOrDefault(p => p.Id == id);
            return poll == null ? null : WithOptions(poll);
        }

        private Poll WithOptions(Poll poll)
        {
            poll.Options = _store.PollOptions
                .Where(o => o.PollId == poll.Id)
                .OrderBy(o => o.SortOrder)
                .ToList();
            return poll;
        }

        public Poll Add(Poll poll)
        {
            var newId = _store.Polls.Count == 0 ? 1 : _store.Polls.Max(p => p.Id) + 1;
            poll.Id = newId;
            _store.Polls.Add(poll);

            var nextOptionId = _store.PollOptions.Count == 0 ? 1 : _store.PollOptions.Max(o => o.Id) + 1;
            foreach (var option in poll.Options)
            {
                option.Id = nextOptionId++;
                option.PollId = poll.Id;
                _store.PollOptions.Add(option);
            }

            return poll;
        }

        public bool HasVoted(int pollId, string userId) =>
            _store.PollVotes.Any(v => v.PollId == pollId && v.UserId == userId);

        public void AddVote(int pollId, int pollOptionId, string userId)
        {
            if (HasVoted(pollId, userId))
            {
                throw new InvalidOperationException("User has already voted on this poll.");
            }

            var newId = _store.PollVotes.Count == 0 ? 1 : _store.PollVotes.Max(v => v.Id) + 1;
            _store.PollVotes.Add(new PollVote
            {
                Id = newId,
                PollId = pollId,
                PollOptionId = pollOptionId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        public IReadOnlyDictionary<int, int> GetVoteCounts(int pollId) =>
            _store.PollVotes
                .Where(v => v.PollId == pollId)
                .GroupBy(v => v.PollOptionId)
                .ToDictionary(g => g.Key, g => g.Count());

        public int? GetVotedOptionId(int pollId, string userId) =>
            _store.PollVotes.FirstOrDefault(v => v.PollId == pollId && v.UserId == userId)?.PollOptionId;
    }
}
