using MvcBB.Shared.Models.Message;

namespace MvcBB.Shared.Interfaces
{
    public interface IMessageRepository
    {
        IReadOnlyList<Message> GetAll();
        IReadOnlyList<Message> GetByRecipientUserId(string userId, bool unreadOnly = false);
        IReadOnlyList<Message> GetByRecipientOrSenderUserId(string userId);
        Message? GetById(int id);
        Message Add(Message message);
        void Update(Message message);
        void Remove(Message message);
        int GetUnreadCountByRecipientUserId(string userId);
    }
}
