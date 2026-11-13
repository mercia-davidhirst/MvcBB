using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Poll;

namespace MvcBB.API.SQL
{
    /// <summary>
    /// SQL Server-backed poll repository, calling the usp_Poll_*/usp_PollOption_*/
    /// usp_PollVote_* stored procedures. These procedures (and the Polls/PollOptions/
    /// PollVotes tables they act on) are NOT checked into this repo - like every
    /// other SQL Server repository here, the schema/proc source lives externally
    /// and needs to be created to match this call shape before this repository can
    /// actually be used.
    /// </summary>
    public class SqlPollRepository : IPollRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlPollRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Poll? GetByThreadId(int threadId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var poll = connection.QuerySingleOrDefault<Poll>(
                "dbo.usp_Poll_GetByThreadId",
                new { ThreadId = threadId },
                commandType: CommandType.StoredProcedure);
            return poll == null ? null : WithOptions(connection, poll);
        }

        public Poll? GetById(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var poll = connection.QuerySingleOrDefault<Poll>(
                "dbo.usp_Poll_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
            return poll == null ? null : WithOptions(connection, poll);
        }

        private static Poll WithOptions(IDbConnection connection, Poll poll)
        {
            poll.Options = connection.Query<PollOption>(
                "dbo.usp_PollOption_GetByPollId",
                new { PollId = poll.Id },
                commandType: CommandType.StoredProcedure).ToList();
            return poll;
        }

        public Poll Add(Poll poll)
        {
            using var connection = _connectionFactory.CreateConnection();
            var pollId = connection.QuerySingle<int>(
                "dbo.usp_Poll_Create",
                new { poll.ThreadId, poll.Question },
                commandType: CommandType.StoredProcedure);
            poll.Id = pollId;

            foreach (var option in poll.Options)
            {
                option.Id = connection.QuerySingle<int>(
                    "dbo.usp_PollOption_Add",
                    new { PollId = pollId, option.Text, option.SortOrder },
                    commandType: CommandType.StoredProcedure);
                option.PollId = pollId;
            }

            return poll;
        }

        public bool HasVoted(int pollId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingle<bool>(
                "dbo.usp_Poll_HasVoted",
                new { PollId = pollId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        public void AddVote(int pollId, int pollOptionId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_PollVote_Add",
                new { PollId = pollId, PollOptionId = pollOptionId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyDictionary<int, int> GetVoteCounts(int pollId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<VoteCountRow>(
                "dbo.usp_Poll_GetVoteCounts",
                new { PollId = pollId },
                commandType: CommandType.StoredProcedure);
            return rows.ToDictionary(r => r.PollOptionId, r => r.VoteCount);
        }

        private class VoteCountRow
        {
            public int PollOptionId { get; set; }
            public int VoteCount { get; set; }
        }

        public int? GetVotedOptionId(int pollId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.QuerySingleOrDefault<int?>(
                "dbo.usp_Poll_GetVotedOptionId",
                new { PollId = pollId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyDictionary<int, Poll> GetByThreadIds(IEnumerable<int> threadIds)
        {
            var ids = threadIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, Poll>();
            }

            // Variable-length IN-lists don't fit a stored procedure's fixed
            // parameters - Dapper expands @ThreadIds/@PollIds into IN (...)
            // itself, so these batch methods go straight against the tables.
            using var connection = _connectionFactory.CreateConnection();
            var polls = connection.Query<Poll>(
                "SELECT Id, ThreadId, Question, CreatedAt FROM dbo.Polls WHERE ThreadId IN @ThreadIds",
                new { ThreadIds = ids }).ToList();

            if (polls.Count == 0)
            {
                return new Dictionary<int, Poll>();
            }

            var options = connection.Query<PollOption>(
                "SELECT Id, PollId, Text, SortOrder FROM dbo.PollOptions WHERE PollId IN @PollIds",
                new { PollIds = polls.Select(p => p.Id).ToList() }).ToList();
            var optionsByPoll = options.GroupBy(o => o.PollId).ToDictionary(g => g.Key, g => g.OrderBy(o => o.SortOrder).ToList());

            foreach (var poll in polls)
            {
                poll.Options = optionsByPoll.TryGetValue(poll.Id, out var opts) ? opts : new List<PollOption>();
            }

            return polls.ToDictionary(p => p.ThreadId);
        }

        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, int>> GetVoteCountsByPollIds(IEnumerable<int> pollIds)
        {
            var ids = pollIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, IReadOnlyDictionary<int, int>>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PollOptionVoteCountRow>(
                "SELECT PollId, PollOptionId, COUNT(*) AS VoteCount FROM dbo.PollVotes WHERE PollId IN @PollIds GROUP BY PollId, PollOptionId",
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
            var ids = pollIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<PollVoteOptionRow>(
                "SELECT PollId, PollOptionId FROM dbo.PollVotes WHERE PollId IN @PollIds AND UserId = @UserId",
                new { PollIds = ids, UserId = ParseUserId(userId) });
            return rows.ToDictionary(r => r.PollId, r => r.PollOptionId);
        }

        private class PollVoteOptionRow
        {
            public int PollId { get; set; }
            public int PollOptionId { get; set; }
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;
    }
}
