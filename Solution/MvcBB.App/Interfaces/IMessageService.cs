using MvcBB.Shared.Models.Message;

namespace MvcBB.App.Interfaces
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageResponse>> GetMessagesAsync(string folder = "inbox");
        Task<MessageResponse> GetMessageAsync(int id);
        Task<MessageResponse> CreateMessageAsync(CreateMessageRequest request);
        Task DeleteMessageAsync(int id);
    }
} 