using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.AdminEmail;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Message;
using MvcBB.Shared.Models.Poll;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Services;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Creates new discussions and replies, with everything that goes with
    /// them (word filter, thread/board counters, poll, watcher emails/PMs and
    /// notifications). Used both when a member posts and when a moderator
    /// approves a held post, so an approved post ends up exactly as if it had
    /// gone straight through. Callers check permissions first.
    /// </summary>
    public interface IForumPostingService
    {
        /// <summary>
        /// Whether this member's next post should be held for approval:
        /// holding is on, they aren't staff, and they have fewer live posts
        /// than the threshold (or already have a post waiting).
        /// </summary>
        bool ShouldHold(int userId, bool isStaff);

        (ForumThread Thread, Post FirstPost) CreateThread(int userId, Board board, string title, string content, CreatePollRequest? poll);

        Task<Post> CreateReplyAsync(int userId, ForumThread thread, string content, int? quotedPostId, string? imageUrl);
    }

    public class ForumPostingService : IForumPostingService
    {
        private readonly IForumThreadRepository _threadRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IPostRepository _postRepository;
        private readonly IPollRepository _pollRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWordFilterRepository _wordFilterRepository;
        private readonly IThreadSubscriptionRepository _subscriptionRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IModerationSettingsRepository _moderationSettings;
        private readonly IModerationQueueRepository _moderationQueue;
        private readonly IEmailSender _emailSender;
        private readonly INotificationService _notifications;

        public ForumPostingService(
            IForumThreadRepository threadRepository,
            IBoardRepository boardRepository,
            IPostRepository postRepository,
            IPollRepository pollRepository,
            IUserRepository userRepository,
            IWordFilterRepository wordFilterRepository,
            IThreadSubscriptionRepository subscriptionRepository,
            IMessageRepository messageRepository,
            IModerationSettingsRepository moderationSettings,
            IModerationQueueRepository moderationQueue,
            IEmailSender emailSender,
            INotificationService notifications)
        {
            _threadRepository = threadRepository;
            _boardRepository = boardRepository;
            _postRepository = postRepository;
            _pollRepository = pollRepository;
            _userRepository = userRepository;
            _wordFilterRepository = wordFilterRepository;
            _subscriptionRepository = subscriptionRepository;
            _messageRepository = messageRepository;
            _moderationSettings = moderationSettings;
            _moderationQueue = moderationQueue;
            _emailSender = emailSender;
            _notifications = notifications;
        }

        public bool ShouldHold(int userId, bool isStaff)
        {
            if (isStaff)
            {
                return false;
            }

            var settings = _moderationSettings.Get();
            if (!settings.HoldNewMemberPosts)
            {
                return false;
            }

            // Anything already waiting keeps the rest of their posts in order
            // behind it, rather than letting a later post jump the queue.
            if (_moderationQueue.CountByUser(userId) > 0)
            {
                return true;
            }

            var user = _userRepository.GetById(userId);
            if (user == null)
            {
                return true;
            }

            return _userRepository.CountPostsByUsername(user.Username) < Math.Max(1, settings.NewMemberPostThreshold);
        }

        public (ForumThread Thread, Post FirstPost) CreateThread(int userId, Board board, string title, string content, CreatePollRequest? poll)
        {
            var userIdText = userId.ToString();
            var wordFilters = _wordFilterRepository.GetAll();
            var now = DateTime.UtcNow;
            var thread = new ForumThread
            {
                Title = WordFilterApplier.Apply(title, wordFilters),
                BoardId = board.Id,
                CreatedByUserId = userIdText,
                CreatedAt = now,
                LastPostAt = now,
                LastPostByUserId = userIdText,
                PostCount = 1,
                ParticipantCount = 1
            };

            thread = _threadRepository.Add(thread);

            // The thread's own Content is its first post - without this the thread
            // would have zero posts and nothing for GetThreadPosts to return.
            var firstPost = _postRepository.Add(new Post
            {
                Content = WordFilterApplier.Apply(content, wordFilters),
                ThreadId = thread.Id,
                CreatedByUserId = userIdText,
                CreatedAt = now
            });

            board.ThreadCount++;
            board.PostCount++;
            board.LastPostAt = now;
            board.LastPostByUserId = userIdText;
            _boardRepository.Update(board);

            if (poll != null)
            {
                _pollRepository.Add(new Poll
                {
                    ThreadId = thread.Id,
                    Question = poll.Question,
                    CreatedAt = now,
                    Options = poll.Options
                        .Select((text, index) => new PollOption { Text = text, SortOrder = index })
                        .ToList()
                });
            }

            _notifications.NotifyNewPost(thread, firstPost, isFirstPost: true);

            return (thread, firstPost);
        }

        public async Task<Post> CreateReplyAsync(int userId, ForumThread thread, string content, int? quotedPostId, string? imageUrl)
        {
            var userIdText = userId.ToString();

            // Checked before adding the post itself, otherwise this post
            // would always "find" itself and never count as a new participant.
            var isNewParticipant = !_postRepository.HasPostInThread(thread.Id, userIdText);

            var post = _postRepository.Add(new Post
            {
                Content = WordFilterApplier.Apply(content, _wordFilterRepository.GetAll()),
                ThreadId = thread.Id,
                CreatedByUserId = userIdText,
                CreatedAt = DateTime.UtcNow,
                QuotedPostId = quotedPostId,
                ImageUrl = imageUrl
            });

            thread.LastPostAt = post.CreatedAt;
            thread.LastPostByUserId = userIdText;
            thread.PostCount++;
            if (isNewParticipant)
            {
                thread.ParticipantCount++;
            }
            _threadRepository.Update(thread);

            var board = _boardRepository.GetById(thread.BoardId);
            if (board != null)
            {
                board.PostCount++;
                board.LastPostAt = post.CreatedAt;
                board.LastPostByUserId = userIdText;
                _boardRepository.Update(board);
            }

            await NotifySubscribersAsync(thread, post, userIdText);
            _notifications.NotifyNewPost(thread, post, isFirstPost: false);

            return post;
        }

        // Fans out to everyone watching this thread except the replier
        // themselves - a subscription's two flags are independent, so a
        // watcher can get a PM, an email, both or (harmlessly) neither.
        // (Every watcher also gets a Reply notification - see NotificationService.)
        private async Task NotifySubscribersAsync(ForumThread thread, Post post, string replierUserId)
        {
            var subscribers = _subscriptionRepository.GetByThreadId(thread.Id)
                .Where(s => s.UserId != replierUserId)
                .ToList();
            if (subscribers.Count == 0)
            {
                return;
            }

            var replierUsername = int.TryParse(replierUserId, out var replierId)
                ? _userRepository.GetById(replierId)?.Username ?? string.Empty
                : string.Empty;
            var subject = $"New reply in \"{thread.Title}\"";
            var body = $"{replierUsername} replied to a thread you're watching:\n\n{post.Content}";
            string? systemUserId = null;

            foreach (var subscription in subscribers)
            {
                if (subscription.NotifyViaMessage)
                {
                    systemUserId ??= SystemUserProvider.GetOrCreateSystemUserId(_userRepository);
                    _messageRepository.Add(new Message
                    {
                        Subject = subject,
                        Content = body,
                        SenderUserId = systemUserId,
                        RecipientUserId = subscription.UserId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (subscription.NotifyViaEmail && int.TryParse(subscription.UserId, out var recipientId))
                {
                    var recipient = _userRepository.GetById(recipientId);
                    if (recipient != null && !string.IsNullOrEmpty(recipient.Email))
                    {
                        await _emailSender.SendAsync(new EmailMessage { To = recipient.Email, Subject = subject, Body = body, Kind = EmailKind.Notification });
                    }
                }
            }
        }
    }
}
