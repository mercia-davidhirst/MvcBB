using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Poll;

namespace MvcBB.API.PostgreSql
{
    /// <summary>
    /// PostgreSQL-backed poll repository, calling fn_poll_*/sp_poll_*/fn_poll_option_*
    /// functions/procedures. These (and the polls/poll_options/poll_votes tables they
    /// act on) are NOT checked into this repo - like every other PostgreSQL
    /// repository here, the schema/function source lives externally and needs to be
    /// created to match this call shape before this repository can actually be used.
    /// </summary>
    public class PostgreSqlPollRepository : IPollRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlPollRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Poll? GetByThreadId(int threadId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var poll = connection.QuerySingleOrDefault<Poll>(
                "SELECT * FROM public.fn_poll_get_by_thread_id(@ThreadId)",
                new { ThreadId = threadId });
            return poll == null ? null : WithOptions(connection, poll);
        }

        public Poll? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var poll = connection.QuerySingleOrDefault<Poll>(
                "SELECT * FROM public.fn_poll_get_by_id(@Id)",
                new { Id = id });
            return poll == null ? null : WithOptions(connection, poll);
        }

        private static Poll WithOptions(IDbConnection connection, Poll poll)
        {
            poll.Options = connection.Query<PollOption>(
                "SELECT * FROM public.fn_poll_option_get_by_poll_id(@PollId)",
                new { PollId = poll.Id }).ToList();
            return poll;
        }

        public Poll Add(Poll poll)
        {
            using var connection = _connectionFactory.CreateConnection();
            var pollId = connection.QuerySingle<int>(
                "SELECT public.fn_poll_create(@ThreadId, @Question)",
                new { poll.ThreadId, poll.Question });
            poll.Id = pollId;

            foreach (var option in poll.Options)
            {
                option.Id = connection.QuerySingle<int>(
                    "SELECT public.fn_poll_option_add(@PollId, @Text, @SortOrder)",
                    new { PollId = pollId, option.Text, option.SortOrder });
                option.PollId = pollId;
            }

            return poll;
        }

        public bool HasVoted(int pollId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "SELECT public.fn_poll_has_voted(@PollId, @UserId)",
                new { PollId = pollId, UserId = ParseUserId(userId) });
        }

        public void AddVote(int pollId, int pollOptionId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_poll_vote_add(@PollId, @PollOptionId, @UserId)",
                new { PollId = pollId, PollOptionId = pollOptionId, UserId = ParseUserId(userId) });
        }

        public IReadOnlyDictionary<int, int> GetVoteCounts(int pollId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<VoteCountRow>(
                "SELECT * FROM public.fn_poll_get_vote_counts(@PollId)",
                new { PollId = pollId });
            return rows.ToDictionary(r => r.PollOptionId, r => r.VoteCount);
        }

        public int? GetVotedOptionId(int pollId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<int?>(
                "SELECT public.fn_poll_get_voted_option_id(@PollId, @UserId)",
                new { PollId = pollId, UserId = ParseUserId(userId) });
        }

        public IReadOnlyDictionary<int, Poll> GetByThreadIds(IEnumerable<int> threadIds)
        {
            var ids = threadIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<int, Poll>();
            }

            // Variable-length IN-lists don't fit a stored function's fixed
            // parameters - these batch methods go straight against the tables
            // using = ANY(@array) instead of an fn_*/sp_* function.
            using var connection = _connectionFactory.CreateConnection();
            var polls = connection.Query<Poll>(
                "SELECT id, thread_id, question, created_at FROM public.polls WHERE thread_id = ANY(@ThreadIds)",
                new { ThreadIds = ids }).ToList();

            if (polls.Count == 0)
            {
                return new Dictionary<int, Poll>();
            }

            var options = connection.Query<PollOption>(
                "SELECT id, poll_id, text, sort_order FROM public.poll_options WHERE poll_id = ANY(@PollIds)",
                new { PollIds = polls.Select(p => p.Id).ToArray() }).ToList();
            var optionsByPoll = options.GroupBy(o => o.PollId).ToDictionary(g => g.Key, g => g.OrderBy(o => o.SortOrder).ToList());

            foreach (var poll in polls)
            {
                poll.Options = optionsByPoll.TryGetValue(poll.Id, out var opts) ? opts : new List<PollOption>();
            }

            return polls.ToDictionary(p => p.ThreadId);
        }

        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, int>> GetVoteCountsByPollIds(IEnumerable<int> pollIds)
        {
            var ids = pollIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<int, IReadOnlyDictionary<int, int>>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PollOptionVoteCountRow>(
                "SELECT poll_id, poll_option_id, COUNT(*) AS vote_count FROM public.poll_votes WHERE poll_id = ANY(@PollIds) GROUP BY poll_id, poll_option_id",
                new { PollIds = ids });

            return rows
                .GroupBy(r => r.PollId)
                .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<int, int>)g.ToDictionary(r => r.PollOptionId, r => r.VoteCount));
        }

        private class PollOptionVoteCountRow
        {
            public int PollId { get; set; }
            public int PollOptionId { get; set; }
            public int VoteCount { get; set; }
        }

        public IReadOnlyDictionary<int, int> GetVotedOptionIdsByPollIds(IEnumerable<int> pollIds, string userId)
        {
            var ids = pollIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<int, int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PollVoteOptionRow>(
                "SELECT poll_id, poll_option_id FROM public.poll_votes WHERE poll_id = ANY(@PollIds) AND user_id = @UserId",
                new { PollIds = ids, UserId = ParseUserId(userId) });
            return rows.ToDictionary(r => r.PollId, r => r.PollOptionId);
        }

        private class PollVoteOptionRow
        {
            public int PollId { get; set; }
            public int PollOptionId { get; set; }
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private class VoteCountRow
        {
            public int PollOptionId { get; set; }
            public int VoteCount { get; set; }
        }
    }
}
