using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Message;

namespace MvcBB.API.InMemory
{
    public class InMemoryMessageRepository : IMessageRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryMessageRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<Message> GetAll() => _store.Messages.ToList();

        public IReadOnlyList<Message> GetByRecipientUserId(string userId, bool unreadOnly = false)
        {
            var query = unreadOnly
                ? _store.Messages.Where(m => m.RecipientUserId == userId && !m.ReadAt.HasValue)
                : _store.Messages.Where(m => m.RecipientUserId == userId);
            return query.OrderByDescending(m => m.CreatedAt).ToList();
        }

        public IReadOnlyList<Message> GetByRecipientOrSenderUserId(string userId) =>
            _store.Messages
                .Where(m => m.RecipientUserId == userId || m.SenderUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

        public Message? GetById(int id) => _store.Messages.FirstOrDefault(m => m.Id == id);

        public Message Add(Message message)
        {
            if (message.Id == 0)
                message.Id = _store.Messages.Count == 0 ? 1 : _store.Messages.Max(m => m.Id) + 1;
            _store.Messages.Add(message);
            return message;
        }

        public void Update(Message message)
        {
            var index = _store.Messages.FindIndex(m => m.Id == message.Id);
            if (index >= 0)
                _store.Messages[index] = message;
        }

        public void Remove(Message message) => _store.Messages.Remove(message);

        public int GetUnreadCountByRecipientUserId(string userId) =>
            _store.Messages.Count(m => m.RecipientUserId == userId && !m.ReadAt.HasValue);
    }
}
