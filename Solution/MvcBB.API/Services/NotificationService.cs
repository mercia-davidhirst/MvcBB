using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Models.Notification;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Services;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Fills members' notification centres. Each member gets at most one
    /// notification per post, picked in this order: a @mention, then a quote
    /// of their post, then a reply in a discussion they started or watch. A
    /// member is never notified about their own post, and never about a
    /// board they can't see.
    ///
    /// Nothing here may stop the post, like or message itself: every method
    /// logs and carries on if the notification can't be written.
    /// </summary>
    public interface INotificationService
    {
        void NotifyNewPost(ForumThread thread, Post post, bool isFirstPost);

        /// <summary>Notifies anyone newly @mentioned by an edit (not those the post already mentioned).</summary>
        void NotifyEditedPost(ForumThread thread, Post post);

        void NotifyLike(Post post, int likerUserId);
        void NotifyMessage(Message message);

        /// <summary>Tells a member what a moderator decided about their held post.</summary>
        void NotifyModeration(int userId, int moderatorUserId, string text, int? threadId, int? postId);

        /// <summary>A short plain-text extract of BBCode content.</summary>
        string Excerpt(string? content, int maxLength = 140);
    }

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notifications;
        private readonly IUserRepository _users;
        private readonly IBoardRepository _boards;
        private readonly IPostRepository _posts;
        private readonly IThreadSubscriptionRepository _subscriptions;
        private readonly ICoreBBCodeService _bbCode;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notifications,
            IUserRepository users,
            IBoardRepository boards,
            IPostRepository posts,
            IThreadSubscriptionRepository subscriptions,
            ICoreBBCodeService bbCode,
            ILogger<NotificationService> logger)
        {
            _notifications = notifications;
            _users = users;
            _boards = boards;
            _posts = posts;
            _subscriptions = subscriptions;
            _bbCode = bbCode;
            _logger = logger;
        }

        public void NotifyNewPost(ForumThread thread, Post post, bool isFirstPost)
        {
            Safely("a new post", () =>
            {
                if (!int.TryParse(post.CreatedByUserId, out var authorId))
                {
                    return;
                }

                var board = _boards.GetById(thread.BoardId);
                var excerpt = Excerpt(post.Content);
                var notified = new HashSet<int> { authorId };

                // 1. @mentions.
                foreach (var userId in ResolveMentions(post.Content))
                {
                    if (notified.Add(userId) && CanSee(board, userId))
                    {
                        Add(userId, NotificationType.Mention, authorId, thread.Id, post.Id, excerpt);
                    }
                }

                // 2. The author of a quoted post.
                if (post.QuotedPostId.HasValue
                    && _posts.GetById(post.QuotedPostId.Value) is { } quoted
                    && int.TryParse(quoted.CreatedByUserId, out var quotedAuthorId)
                    && notified.Add(quotedAuthorId)
                    && CanSee(board, quotedAuthorId))
                {
                    Add(quotedAuthorId, NotificationType.Quote, authorId, thread.Id, post.Id, excerpt);
                }

                if (isFirstPost)
                {
                    return;
                }

                // 3. A reply: the discussion's starter and everyone watching it.
                var recipients = new List<int>();
                if (int.TryParse(thread.CreatedByUserId, out var starterId))
                {
                    recipients.Add(starterId);
                }
                recipients.AddRange(_subscriptions.GetByThreadId(thread.Id)
                    .Select(s => int.TryParse(s.UserId, out var id) ? id : 0)
                    .Where(id => id > 0));

                foreach (var userId in recipients)
                {
                    if (notified.Add(userId) && CanSee(board, userId))
                    {
                        Add(userId, NotificationType.Reply, authorId, thread.Id, post.Id, excerpt);
                    }
                }
            });
        }

        public void NotifyEditedPost(ForumThread thread, Post post)
        {
            Safely("an edited post", () =>
            {
                if (!int.TryParse(post.CreatedByUserId, out var authorId))
                {
                    return;
                }

                var board = _boards.GetById(thread.BoardId);
                string? excerpt = null;
                foreach (var userId in ResolveMentions(post.Content))
                {
                    if (userId != authorId
                        && !_notifications.Exists(userId, NotificationType.Mention, post.Id)
                        && CanSee(board, userId))
                    {
                        excerpt ??= Excerpt(post.Content);
                        Add(userId, NotificationType.Mention, authorId, thread.Id, post.Id, excerpt);
                    }
                }
            });
        }

        public void NotifyLike(Post post, int likerUserId)
        {
            Safely("a like", () =>
            {
                if (!int.TryParse(post.CreatedByUserId, out var authorId)
                    || authorId == likerUserId
                    || _notifications.Exists(authorId, NotificationType.Like, post.Id, likerUserId))
                {
                    return;
                }

                Add(authorId, NotificationType.Like, likerUserId, post.ThreadId, post.Id, Excerpt(post.Content));
            });
        }

        public void NotifyMessage(Message message)
        {
            Safely("a private message", () =>
            {
                if (!int.TryParse(message.RecipientUserId, out var recipientId))
                {
                    return;
                }

                int.TryParse(message.SenderUserId, out var senderId);

                // Thread-watch emails sent as a PM come from the System
                // account; the reply itself already has its own notification.
                if (senderId > 0 && _users.GetById(senderId)?.Username == SystemUserProvider.Username)
                {
                    return;
                }

                _notifications.Add(new Notification
                {
                    UserId = recipientId,
                    Type = NotificationType.Message,
                    ActorUserId = senderId > 0 ? senderId : null,
                    MessageId = message.Id,
                    Excerpt = message.Subject
                });
            });
        }

        public void NotifyModeration(int userId, int moderatorUserId, string text, int? threadId, int? postId)
        {
            Safely("a moderation decision", () => _notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.Moderation,
                ActorUserId = moderatorUserId,
                ThreadId = threadId,
                PostId = postId,
                Excerpt = text
            }));
        }

        public string Excerpt(string? content, int maxLength = 140)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var text = System.Text.RegularExpressions.Regex.Replace(_bbCode.StripBBCode(content), @"\s+", " ").Trim();
            return text.Length <= maxLength ? text : text[..(maxLength - 1)].TrimEnd() + "…";
        }

        // Members only, never the System account or an anonymized one.
        private IEnumerable<int> ResolveMentions(string content)
        {
            var names = MentionParser.Extract(content);
            if (names.Count == 0)
            {
                return Array.Empty<int>();
            }

            return _users.GetIdsByUsernames(names)
                .Where(kv => !string.Equals(kv.Key, SystemUserProvider.Username, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value)
                .ToList();
        }

        private readonly Dictionary<int, User?> _userCache = new();

        private bool CanSee(Board? board, int userId)
        {
            if (board == null)
            {
                return false;
            }

            if (!_userCache.TryGetValue(userId, out var user))
            {
                user = _users.GetById(userId);
                _userCache[userId] = user;
            }

            return user != null
                && user.Role != UserRole.Deleted
                && user.Username != SystemUserProvider.Username
                && board.IsVisibleTo(isAuthenticated: true, isAdmin: user.Role == UserRole.Administrator);
        }

        private void Add(int userId, NotificationType type, int actorId, int threadId, int postId, string excerpt) =>
            _notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                ActorUserId = actorId,
                ThreadId = threadId,
                PostId = postId,
                Excerpt = excerpt
            });

        private void Safely(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't create notifications for {What}", what);
            }
        }
    }
}
