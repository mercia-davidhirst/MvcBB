using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.API.SQL
{
    public class SqlMessageRepository : IMessageRepository
    {
        public IReadOnlyList<Message> GetAll() => throw new NotImplementedException("Implement with EF Core or Dapper");
        public IReadOnlyList<Message> GetByRecipientUserId(string userId, bool unreadOnly = false) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public IReadOnlyList<Message> GetByRecipientOrSenderUserId(string userId) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Message? GetById(int id) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public Message Add(Message message) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Update(Message message) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public void Remove(Message message) => throw new NotImplementedException("Implement with EF Core or Dapper");
        public int GetUnreadCountByRecipientUserId(string userId) => throw new NotImplementedException("Implement with EF Core or Dapper");
    }
}
