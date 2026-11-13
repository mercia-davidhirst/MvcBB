using Dapper;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ThreadSubscription;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlThreadSubscriptionRepository : IThreadSubscriptionRepository
    {
        private readonly IPostgreSqlConnectionFactory _connectionFactory;

        public PostgreSqlThreadSubscriptionRepository(IPostgreSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ThreadSubscriptionModel? GetByThreadAndUser(int threadId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var row = connection.QuerySingleOrDefault<SubscriptionRow>(
                "SELECT * FROM public.fn_thread_subscription_get_by_thread_and_user(@ThreadId, @UserId)",
                new { ThreadId = threadId, UserId = ParseUserId(userId) });
            return row == null ? null : MapToModel(row);
        }

        public IReadOnlyList<ThreadSubscriptionModel> GetByThreadId(int threadId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<SubscriptionRow>(
                "SELECT * FROM public.fn_thread_subscription_get_by_thread_id(@ThreadId)",
                new { ThreadId = threadId });
            return rows.Select(MapToModel).ToList();
        }

        public ThreadSubscriptionModel Add(ThreadSubscriptionModel subscription)
        {
            using var connection = _connectionFactory.CreateConnection();
            var newId = connection.QuerySingle<int>(
                "CALL public.sp_thread_subscription_insert(@ThreadId, @UserId, @NotifyViaMessage, @NotifyViaEmail, NULL)",
                new
                {
                    subscription.ThreadId,
                    UserId = ParseUserId(subscription.UserId),
                    subscription.NotifyViaMessage,
                    subscription.NotifyViaEmail
                });

            var row = connection.QuerySingle<SubscriptionRow>(
                "SELECT * FROM public.fn_thread_subscription_get_by_id(@Id)",
                new { Id = newId });
            return MapToModel(row);
        }

        public void Update(ThreadSubscriptionModel subscription)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_thread_subscription_update(@Id, @NotifyViaMessage, @NotifyViaEmail)",
                new { subscription.Id, subscription.NotifyViaMessage, subscription.NotifyViaEmail });
        }

        public void Remove(int threadId, string userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Execute(
                "CALL public.sp_thread_subscription_delete(@ThreadId, @UserId)",
                new { ThreadId = threadId, UserId = ParseUserId(userId) });
        }

        public IReadOnlyDictionary<int, ThreadSubscriptionModel> GetByThreadIdsAndUser(IEnumerable<int> threadIds, string userId)
        {
            var ids = threadIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<int, ThreadSubscriptionModel>();
            }

            // A variable-length IN-list doesn't fit a stored function's fixed
            // parameters - this batch method goes straight against the table.
            using var connection = _connectionFactory.CreateConnection();
            var rows = connection.Query<SubscriptionRow>(
                "SELECT * FROM public.thread_subscriptions WHERE thread_id = ANY(@ThreadIds) AND user_id = @UserId",
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
        /// Matches public.thread_subscriptions' actual column types (user_id
        /// is an INTEGER foreign key to users.id there, not a string as on
        /// the model).
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
