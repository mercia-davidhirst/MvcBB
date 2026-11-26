using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.ForumThread;

namespace MvcBB.API.InMemory
{
    public class InMemoryForumThreadRepository : IForumThreadRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryForumThreadRepository(InMemoryStore store)
        {
            _store = store;
        }

        public IReadOnlyList<ForumThread> GetAll() => _store.Threads.Select(Hydrate).ToList();

        public ForumThread? GetById(int id)
        {
            var thread = _store.Threads.FirstOrDefault(t => t.Id == id);
            return thread == null ? null : Hydrate(thread);
        }

        public ForumThread Add(ForumThread thread)
        {
            if (thread.Id == 0)
                thread.Id = _store.Threads.Count == 0 ? 1 : _store.Threads.Max(t => t.Id) + 1;
            _store.Threads.Add(thread);
            return Hydrate(thread);
        }

        public void Update(ForumThread thread)
        {
            var index = _store.Threads.FindIndex(t => t.Id == thread.Id);
            if (index >= 0)
                _store.Threads[index] = thread;
        }

        // Matches usp_Thread_Delete / sp_thread_delete: the thread goes with
        // its posts, likes, poll, watchers, read positions, notifications and
        // held replies, and other threads' quotes of its posts are unlinked.
        public void Remove(ForumThread thread)
        {
            var postIds = _store.Posts.Where(p => p.ThreadId == thread.Id).Select(p => p.Id).ToHashSet();
            foreach (var quoting in _store.Posts.Where(p => p.ThreadId != thread.Id && p.QuotedPostId.HasValue && postIds.Contains(p.QuotedPostId.Value)))
            {
                quoting.QuotedPostId = null;
            }
            _store.PostLikes.RemoveAll(l => postIds.Contains(l.PostId));
            _store.Posts.RemoveAll(p => p.ThreadId == thread.Id);

            var pollIds = _store.Polls.Where(p => p.ThreadId == thread.Id).Select(p => p.Id).ToHashSet();
            _store.PollVotes.RemoveAll(v => pollIds.Contains(v.PollId));
            _store.PollOptions.RemoveAll(o => pollIds.Contains(o.PollId));
            _store.Polls.RemoveAll(p => p.ThreadId == thread.Id);

            _store.ThreadSubscriptions.RemoveAll(s => s.ThreadId == thread.Id);
            _store.ThreadReads.RemoveAll(r => r.ThreadId == thread.Id);
            _store.Notifications.RemoveAll(n => n.ThreadId == thread.Id);
            _store.ModerationQueue.RemoveAll(q => q.ThreadId == thread.Id);
            _store.Threads.RemoveAll(t => t.Id == thread.Id);
        }

        public (IReadOnlyList<ForumThread> Items, int TotalCount) GetPage(int skip, int take, bool includePrivateBoards, int? boardId = null)
        {
            var visibleBoardIds = _store.Boards
                .Where(b => includePrivateBoards || !b.IsPrivate)
                .Select(b => b.Id)
                .ToHashSet();

            // Sticky only wins the sort for a single board's own thread list
            // (boardId given) - for the site-wide "latest across every
            // board" feed (boardId null), sticky is ignored so an old thread
            // pinned in one board can't crowd out genuinely recent activity
            // from every other board.
            var filtered = _store.Threads
                .Where(t => visibleBoardIds.Contains(t.BoardId))
                .Where(t => !boardId.HasValue || t.BoardId == boardId.Value)
                .OrderByDescending(t => boardId.HasValue && t.IsSticky)
                .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
                .ToList();

            var page = filtered.Skip(skip).Take(take).Select(Hydrate).ToList();
            return (page, filtered.Count);
        }

        public IReadOnlyDictionary<int, ForumThread> GetLastActiveThreadsByBoardIds(IEnumerable<int> boardIds)
        {
            var idSet = boardIds.ToHashSet();
            return _store.Threads
                .Where(t => idSet.Contains(t.BoardId))
                .GroupBy(t => t.BoardId)
                .ToDictionary(
                    g => g.Key,
                    g => Hydrate(g.OrderByDescending(t => t.LastPostAt ?? t.CreatedAt).First()));
        }

        public IReadOnlyDictionary<int, ForumThread> GetByIds(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();
            return _store.Threads.Where(t => idSet.Contains(t.Id)).Select(Hydrate).ToDictionary(t => t.Id);
        }

        public (IReadOnlyList<ForumThread> Items, int TotalCount) Search(string query, int? boardId, bool includePrivateBoards, int skip, int take)
        {
            var visibleBoardIds = _store.Boards
                .Where(b => includePrivateBoards || !b.IsPrivate)
                .Select(b => b.Id)
                .ToHashSet();

            var q = query.ToLowerInvariant();
            var filtered = _store.Threads
                .Where(t => visibleBoardIds.Contains(t.BoardId))
                .Where(t => !boardId.HasValue || t.BoardId == boardId.Value)
                .Select(Hydrate)
                .Where(t => t.Title.ToLowerInvariant().Contains(q) || t.CreatedByUsername.ToLowerInvariant().Contains(q))
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return (filtered.Skip(skip).Take(take).ToList(), filtered.Count);
        }

        // Populates the denormalized presentation fields (BoardName,
        // BoardIsPrivate, CreatedByUsername, LastPostByUsername) from the
        // current Board/User records, matching what the SQL/PostgreSQL
        // repositories get for free from vw_ThreadDetails/vw_thread_details.
        private ForumThread Hydrate(ForumThread thread)
        {
            var board = _store.Boards.FirstOrDefault(b => b.Id == thread.BoardId);
            thread.BoardName = board?.Name ?? string.Empty;
            thread.BoardIsPrivate = board?.IsPrivate ?? false;

            if (int.TryParse(thread.CreatedByUserId, out var creatorId))
            {
                thread.CreatedByUsername = _store.Users.FirstOrDefault(u => u.Id == creatorId)?.Username ?? string.Empty;
            }

            if (int.TryParse(thread.LastPostByUserId, out var lastPosterId))
            {
                thread.LastPostByUsername = _store.Users.FirstOrDefault(u => u.Id == lastPosterId)?.Username;
            }
            else
            {
                thread.LastPostByUsername = null;
            }

            return thread;
        }
    }
}
