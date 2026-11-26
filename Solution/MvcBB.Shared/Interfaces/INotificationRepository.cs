using MvcBB.Shared.Models.Notification;

namespace MvcBB.Shared.Interfaces
{
    public interface INotificationRepository
    {
        Notification Add(Notification notification);
        Notification? GetById(int id);

        /// <summary>A page of the member's notifications, newest first, with ActorUsername and ThreadTitle filled in.</summary>
        (IReadOnlyList<Notification> Items, int TotalCount) GetByUser(int userId, int skip, int take);

        int CountUnread(int userId);

        /// <summary>
        /// Whether the member already has a notification of this type for
        /// this post (from this actor, when actorUserId is given) - so editing
        /// a post doesn't re-notify everyone it mentions, and liking a post
        /// again after unliking it doesn't notify its author twice.
        /// </summary>
        bool Exists(int userId, NotificationType type, int postId, int? actorUserId = null);

        void MarkRead(int id, int userId);
        void MarkAllRead(int userId);

        /// <summary>Marks the member's notifications about this thread as read (they've just opened it).</summary>
        void MarkReadForThread(int userId, int threadId);

        /// <summary>Marks the member's notification about this private message as read (they've just opened it).</summary>
        void MarkReadForMessage(int userId, int messageId);

        /// <summary>Deletes read notifications created before the cutoff. Returns how many went.</summary>
        int PruneRead(DateTime createdBefore);
    }
}
