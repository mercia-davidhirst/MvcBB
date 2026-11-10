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

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;
    }
}
