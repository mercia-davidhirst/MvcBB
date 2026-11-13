using System.Data;
using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ThreadSubscription;

namespace MvcBB.API.SQL
{
    public class SqlThreadSubscriptionRepository : IThreadSubscriptionRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlThreadSubscriptionRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ThreadSubscriptionModel? GetByThreadAndUser(int threadId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<SubscriptionRow>(
                "dbo.usp_ThreadSubscription_GetByThreadAndUser",
                new { ThreadId = threadId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
            return row == null ? null : MapToModel(row);
        }

        public IReadOnlyList<ThreadSubscriptionModel> GetByThreadId(int threadId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<SubscriptionRow>(
                "dbo.usp_ThreadSubscription_GetByThreadId",
                new { ThreadId = threadId },
                commandType: CommandType.StoredProcedure);
            return rows.Select(MapToModel).ToList();
        }

        public ThreadSubscriptionModel Add(ThreadSubscriptionModel subscription)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingle<SubscriptionRow>(
                "dbo.usp_ThreadSubscription_Insert",
                new
                {
                    subscription.ThreadId,
                    UserId = ParseUserId(subscription.UserId),
                    subscription.NotifyViaMessage,
                    subscription.NotifyViaEmail,
                    subscription.CreatedAt
                },
                commandType: CommandType.StoredProcedure);
            return MapToModel(row);
        }

        public void Update(ThreadSubscriptionModel subscription)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_ThreadSubscription_Update",
                new { subscription.Id, subscription.NotifyViaMessage, subscription.NotifyViaEmail },
                commandType: CommandType.StoredProcedure);
        }

        public void Remove(int threadId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "dbo.usp_ThreadSubscription_Delete",
                new { ThreadId = threadId, UserId = ParseUserId(userId) },
                commandType: CommandType.StoredProcedure);
        }

        public IReadOnlyDictionary<int, ThreadSubscriptionModel> GetByThreadIdsAndUser(IEnumerable<int> threadIds, string userId)
        {
            var ids = threadIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, ThreadSubscriptionModel>();
            }

            // A variable-length IN-list doesn't fit a stored procedure's fixed
            // parameters - this batch method goes straight against the table.
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<SubscriptionRow>(
                "SELECT * FROM dbo.ThreadSubscriptions WHERE ThreadId IN @ThreadIds AND UserId = @UserId",
                new { ThreadIds = ids, UserId = ParseUserId(userId) });
            return rows.Select(MapToModel).ToDictionary(s => s.ThreadId);
        }

        private static int? ParseUserId(string? userId) => int.TryParse(userId, out var id) ? id : null;

        private static ThreadSubscriptionModel MapToModel(SubscriptionRow row) => new()
        {
            Id = row.Id,
            ThreadId = row.ThreadId,
            UserId = row.UserId.ToString(),
            NotifyViaMessage = row.NotifyViaMessage,
            NotifyViaEmail = row.NotifyViaEmail,
            CreatedAt = row.CreatedAt
        };

        /// <summary>
        /// Matches dbo.ThreadSubscriptions' actual column types (UserId is an
        /// INT foreign key to Users.Id there, not a string as on the model).
        /// </summary>
        private class SubscriptionRow
        {
            public int Id { get; set; }
            public int ThreadId { get; set; }
            public int UserId { get; set; }
            public bool NotifyViaMessage { get; set; }
            public bool NotifyViaEmail { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
