using MvcBB.Shared.Models.Moderation;

namespace MvcBB.Shared.Interfaces
{
    /// <summary>Posts held for a moderator's approval.</summary>
    public interface IModerationQueueRepository
    {
        ModerationQueueItem Add(ModerationQueueItem item);
        ModerationQueueItem? GetById(int id);

        /// <summary>Everything waiting, oldest first, with Username, BoardName and ThreadTitle filled in.</summary>
        IReadOnlyList<ModerationQueueItem> GetAll();

        void Remove(int id);
        int Count();

        /// <summary>How many of this member's posts are waiting.</summary>
        int CountByUser(int userId);
    }

    /// <summary>The moderation log (moderator actions, newest first).</summary>
    public interface IModerationLogRepository
    {
        void Add(ModerationLogEntry entry);

        /// <summary>A page of the log, newest first, with ActorUsername and TargetUsername filled in.</summary>
        (IReadOnlyList<ModerationLogEntry> Items, int TotalCount) GetPage(int skip, int take);
    }

    /// <summary>The single ModerationSettings row.</summary>
    public interface IModerationSettingsRepository
    {
        ModerationSettings Get();
        void Update(ModerationSettings settings);
    }
}
