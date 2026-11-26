using Microsoft.Extensions.Logging.Abstractions;
using MvcBB.API.InMemory;
using MvcBB.API.Services;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.Moderation;
using MvcBB.Shared.Models.Notification;
using MvcBB.Shared.Models.ThreadSubscription;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Services;

namespace MvcBB.Tests;

/// <summary>
/// ForumPostingService (holding new members' posts, creating threads and
/// replies) and NotificationService (who gets told about what), against the
/// in-memory repositories.
/// </summary>
public class PostingAndNotificationTests
{
    private sealed class NullEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body) => Task.CompletedTask;
    }

    private sealed class Harness
    {
        public InMemoryStore Store { get; } = new();
        public InMemoryUserRepository Users { get; }
        public InMemoryNotificationRepository Notifications { get; }
        public InMemoryModerationSettingsRepository Settings { get; }
        public InMemoryModerationQueueRepository Queue { get; }
        public InMemoryThreadSubscriptionRepository Subscriptions { get; }
        public ForumPostingService Posting { get; }
        public NotificationService NotificationService { get; }
        public Board Board { get; }

        public Harness()
        {
            Users = new InMemoryUserRepository(Store);
            Notifications = new InMemoryNotificationRepository(Store);
            Settings = new InMemoryModerationSettingsRepository(Store);
            Queue = new InMemoryModerationQueueRepository(Store);
            Subscriptions = new InMemoryThreadSubscriptionRepository(Store);
            var boards = new InMemoryBoardRepository(Store);
            var posts = new InMemoryPostRepository(Store);

            NotificationService = new NotificationService(Notifications, Users, boards, posts, Subscriptions,
                new CoreBBCodeService(), NullLogger<NotificationService>.Instance);
            Posting = new ForumPostingService(new InMemoryForumThreadRepository(Store), boards, posts,
                new InMemoryPollRepository(Store), Users, new InMemoryWordFilterRepository(Store), Subscriptions,
                new InMemoryMessageRepository(Store), Settings, Queue, new NullEmailSender(), NotificationService);

            Board = boards.Add(new Board { Name = "General", IsActive = true });
        }

        public User AddUser(string name, UserRole role = UserRole.User) =>
            Users.Add(new User { Username = name, Email = name + "@example.com", Role = role, CreatedAt = DateTime.UtcNow });

        public IReadOnlyList<Notification> For(User user) => Notifications.GetByUser(user.Id, 0, 100).Items;
    }

    [Fact]
    public void ShouldHold_OnlyNewNonStaffMembers_WhenSwitchedOn()
    {
        var h = new Harness();
        var member = h.AddUser("newbie");

        Assert.False(h.Posting.ShouldHold(member.Id, isStaff: false)); // off by default

        h.Settings.Update(new ModerationSettings { HoldNewMemberPosts = true, NewMemberPostThreshold = 1 });
        Assert.True(h.Posting.ShouldHold(member.Id, isStaff: false));
        Assert.False(h.Posting.ShouldHold(member.Id, isStaff: true));

        h.Posting.CreateThread(member.Id, h.Board, "Hello", "First post", null);
        Assert.False(h.Posting.ShouldHold(member.Id, isStaff: false)); // has 1 live post now
    }

    [Fact]
    public void ShouldHold_WhileAPostIsAlreadyWaiting()
    {
        var h = new Harness();
        var member = h.AddUser("regular");
        h.Posting.CreateThread(member.Id, h.Board, "Hello", "First post", null);
        h.Settings.Update(new ModerationSettings { HoldNewMemberPosts = true, NewMemberPostThreshold = 1 });
        h.Queue.Add(new ModerationQueueItem { Kind = QueuedPostKind.NewThread, UserId = member.Id, BoardId = h.Board.Id, Title = "x", Content = "y" });

        Assert.True(h.Posting.ShouldHold(member.Id, isStaff: false));
    }

    [Fact]
    public async Task Reply_NotifiesStarterWatcherMentionAndQuote_OncePerMember()
    {
        var h = new Harness();
        var starter = h.AddUser("starter");
        var watcher = h.AddUser("watcher");
        var mentioned = h.AddUser("mentioned");
        var replier = h.AddUser("replier");

        var (thread, firstPost) = h.Posting.CreateThread(starter.Id, h.Board, "Topic", "Opening post", null);
        h.Subscriptions.Add(new ThreadSubscriptionModel { ThreadId = thread.Id, UserId = watcher.Id.ToString() });

        // Quotes the starter and mentions both the starter and another member:
        // the starter still gets only one notification (the mention wins).
        await h.Posting.CreateReplyAsync(replier.Id, thread, "@starter and @mentioned, see this", firstPost.Id, null);

        Assert.Equal(NotificationType.Mention, Assert.Single(h.For(starter)).Type);
        Assert.Equal(NotificationType.Mention, Assert.Single(h.For(mentioned)).Type);
        Assert.Equal(NotificationType.Reply, Assert.Single(h.For(watcher)).Type);
        Assert.Empty(h.For(replier));
    }

    [Fact]
    public async Task Mention_OnInactiveBoard_OnlyReachesAdmins()
    {
        var h = new Harness();
        h.Board.IsActive = false;
        var admin = h.AddUser("theadmin", UserRole.Administrator);
        var member = h.AddUser("member");
        var author = h.AddUser("author", UserRole.Administrator);

        var (thread, _) = h.Posting.CreateThread(author.Id, h.Board, "Hidden", "Hi @theadmin and @member", null);
        await Task.CompletedTask;

        Assert.Single(h.For(admin));
        Assert.Empty(h.For(member));
        Assert.NotNull(thread);
    }

    [Fact]
    public void Like_NotifiesAuthorOncePerLiker()
    {
        var h = new Harness();
        var author = h.AddUser("author");
        var liker = h.AddUser("liker");
        var (_, post) = h.Posting.CreateThread(author.Id, h.Board, "Topic", "Likeable", null);

        h.NotificationService.NotifyLike(post, liker.Id);
        h.NotificationService.NotifyLike(post, liker.Id); // unlike + like again
        h.NotificationService.NotifyLike(post, author.Id); // own post

        Assert.Equal(NotificationType.Like, Assert.Single(h.For(author)).Type);
    }

    [Fact]
    public void EditedPost_OnlyNotifiesNewlyMentionedMembers()
    {
        var h = new Harness();
        var author = h.AddUser("author");
        var first = h.AddUser("first");
        var second = h.AddUser("second");
        var (thread, post) = h.Posting.CreateThread(author.Id, h.Board, "Topic", "Hi @first", null);

        post.Content = "Hi @first and @second";
        h.NotificationService.NotifyEditedPost(thread, post);

        Assert.Single(h.For(first));
        Assert.Single(h.For(second));
    }

    [Fact]
    public async Task RemoveThread_TakesItsPostsLikesPollAndNotificationsWithIt()
    {
        var h = new Harness();
        var author = h.AddUser("author");
        var other = h.AddUser("other");
        var (thread, first) = h.Posting.CreateThread(author.Id, h.Board, "Doomed", "Hi @other", new MvcBB.Shared.Models.Poll.CreatePollRequest { Question = "Q", Options = new() { "a", "b" } });
        await h.Posting.CreateReplyAsync(other.Id, thread, "Reply", first.Id, null);
        new InMemoryPostLikeRepository(h.Store).AddLike(first.Id, other.Id.ToString());

        // A post elsewhere quoting the doomed thread keeps its text, loses the link.
        var (elsewhere, _) = h.Posting.CreateThread(other.Id, h.Board, "Elsewhere", "x", null);
        var quoting = await h.Posting.CreateReplyAsync(other.Id, elsewhere, "Quoting", first.Id, null);

        new InMemoryForumThreadRepository(h.Store).Remove(thread);

        Assert.DoesNotContain(h.Store.Threads, t => t.Id == thread.Id);
        Assert.DoesNotContain(h.Store.Posts, p => p.ThreadId == thread.Id);
        Assert.Empty(h.Store.PostLikes);
        Assert.Empty(h.Store.Polls);
        Assert.DoesNotContain(h.Store.Notifications, n => n.ThreadId == thread.Id);
        Assert.Null(h.Store.Posts.Single(p => p.Id == quoting.Id).QuotedPostId);
    }

    [Fact]
    public async Task ThreadPaging_PagesInOrderAndLocatesPostsAndFirstUnread()
    {
        var h = new Harness();
        var author = h.AddUser("author");
        var reader = h.AddUser("reader");
        var (thread, first) = h.Posting.CreateThread(author.Id, h.Board, "Long", "post 0", null);
        var ids = new List<int> { first.Id };
        for (var i = 1; i < 12; i++)
        {
            // Same timestamp for several posts: ties are ordered by id.
            var post = await h.Posting.CreateReplyAsync(i % 3 == 0 ? reader.Id : author.Id, thread, $"post {i}", null, null);
            post.CreatedAt = first.CreatedAt.AddSeconds(i / 2);
            h.Store.Posts.Single(p => p.Id == post.Id).CreatedAt = post.CreatedAt;
            ids.Add(post.Id);
        }

        var posts = new InMemoryPostRepository(h.Store);
        Assert.Equal(ids.Skip(5).Take(5), posts.GetThreadPage(thread.Id, 5, 5).Select(p => p.Id));

        var at7 = posts.GetThreadLocation(thread.Id, postId: ids[7]);
        Assert.Equal(12, at7.TotalCount);
        Assert.Equal(7, at7.Position);

        Assert.Null(posts.GetThreadLocation(thread.Id, postId: 999999).Position);

        // First post after post 4's time not written by the reader: post 6 is
        // the reader's own (6 % 3 == 0), so it's post 7.
        var unread = posts.GetThreadLocation(thread.Id, unreadAfter: h.Store.Posts.Single(p => p.Id == ids[5]).CreatedAt, excludeUserId: reader.Id);
        Assert.Equal(ids[7], unread.PostId);
        Assert.Equal(7, unread.Position);
    }

    [Fact]
    public void Excerpt_HidesSpoilersAndShortensLongText()
    {
        var h = new Harness();
        var excerpt = h.NotificationService.Excerpt("[b]Big[/b] news [spoiler]the ending[/spoiler] " + new string('x', 300), 50);

        Assert.StartsWith("Big news (spoiler) ", excerpt);
        Assert.DoesNotContain("ending", excerpt);
        Assert.True(excerpt.Length <= 50);
    }
}
