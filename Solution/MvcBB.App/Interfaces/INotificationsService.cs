using MvcBB.Shared.Models.Notification;

namespace MvcBB.App.Interfaces
{
    /// <summary>The signed-in member's notification centre (api/notifications).</summary>
    public interface INotificationsService
    {
        Task<NotificationPage> GetPageAsync(int page = 1, int pageSize = 25);
        Task<int> GetUnreadCountAsync();

        /// <summary>Marks one notification read and returns it (null if it isn't theirs or has gone).</summary>
        Task<Notification?> MarkReadAsync(int id);

        Task MarkAllReadAsync();

        /// <summary>Usernames for the @mention autocomplete.</summary>
        Task<IReadOnlyList<string>> GetMentionSuggestionsAsync(string query);
    }
}
