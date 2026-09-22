using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.API.PostgreSql
{
    public class PostgreSqlMessageRepository : IMessageRepository
    {
        public IReadOnlyList<Message> GetAll() => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public IReadOnlyList<Message> GetByRecipientUserId(string userId, bool unreadOnly = false) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public IReadOnlyList<Message> GetByRecipientOrSenderUserId(string userId) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Message? GetById(int id) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public Message Add(Message message) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Update(Message message) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public void Remove(Message message) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
        public int GetUnreadCountByRecipientUserId(string userId) => throw new NotImplementedException("Implement with Npgsql or EF Core (Npgsql provider)");
    }
}
