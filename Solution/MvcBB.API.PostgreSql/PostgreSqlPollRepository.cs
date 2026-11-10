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

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private class VoteCountRow
        {
            public int PollOptionId { get; set; }
            public int VoteCount { get; set; }
        }
    }
}
