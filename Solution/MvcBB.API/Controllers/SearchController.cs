using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.Search;
using MvcBB.Shared.Models.User;
using MvcBB.Shared.Interfaces;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        // "all" has no single ranked index across threads/posts/users, so a
        // correct merged "newest first" page needs enough rows from EACH
        // type to cover everything before the end of the requested page,
        // sorted the same way, then merged and re-sliced in MergeAndPage.
        // Capped so a very deep page number can't force an unbounded amount
        // of work on any one type's search.
        private const int MaxCombinedTake = 2000;

        private readonly IForumThreadRepository _threadRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;

        public SearchController(
            IForumThreadRepository threadRepository,
            IPostRepository postRepository,
            IUserRepository userRepository)
        {
            _threadRepository = threadRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public ActionResult<SearchResponse> Search([FromQuery] SearchRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = request.Query.Trim();
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Max(1, request.PageSize);
            var skip = (page - 1) * pageSize;

            SearchResponse response;
            switch (request.Type?.ToLower())
            {
                case "threads":
                {
                    var (items, total) = _threadRepository.Search(query, request.BoardId, isAuthenticated, skip, pageSize);
                    response = BuildResponse(MapThreads(items).ToList(), total, page, pageSize);
                    break;
                }
                case "posts":
                {
                    var (items, total) = _postRepository.Search(query, request.BoardId, isAuthenticated, skip, pageSize);
                    response = BuildResponse(MapPosts(items), total, page, pageSize);
                    break;
                }
                case "users":
                {
                    var (items, total) = _userRepository.Search(query, skip, pageSize);
                    response = BuildResponse(MapUsers(items).ToList(), total, page, pageSize);
                    break;
                }
                default:
                    response = SearchAll(query, request.BoardId, isAuthenticated, page, pageSize);
                    break;
            }

            return Ok(response);
        }

        /// <summary>
        /// Combines threads/posts/users search, each already filtered,
        /// sorted and limited to the data layer's best effort at "enough to
        /// cover this page" (see MaxCombinedTake) - only the final merge and
        /// page slice happens here, over at most a few thousand rows rather
        /// than every thread/post/user in the database.
        /// </summary>
        private SearchResponse SearchAll(string query, int? boardId, bool isAuthenticated, int page, int pageSize)
        {
            var combinedTake = Math.Min(page * pageSize, MaxCombinedTake);

            var (threads, threadCount) = _threadRepository.Search(query, boardId, isAuthenticated, 0, combinedTake);
            var (posts, postCount) = _postRepository.Search(query, boardId, isAuthenticated, 0, combinedTake);
            var (users, userCount) = _userRepository.Search(query, 0, combinedTake);

            var merged = new List<SearchResult>(threads.Count + posts.Count + users.Count);
            merged.AddRange(MapThreads(threads));
            merged.AddRange(MapPosts(posts));
            merged.AddRange(MapUsers(users));
            merged = merged.OrderByDescending(r => r.CreatedAt).ToList();

            var skip = (page - 1) * pageSize;
            var totalResults = threadCount + postCount + userCount;
            return BuildResponse(merged.Skip(skip).Take(pageSize).ToList(), totalResults, page, pageSize);
        }

        private static SearchResponse BuildResponse(List<SearchResult> results, int totalResults, int page, int pageSize) => new()
        {
            Results = results,
            TotalResults = totalResults,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalResults / (double)pageSize)
        };

        private static IEnumerable<SearchResult> MapThreads(IReadOnlyList<ForumThread> threads) =>
            threads.Select(t => new SearchResult
            {
                Type = "thread",
                Id = t.Id,
                Title = t.Title,
                Author = t.CreatedByUsername,
                CreatedAt = t.CreatedAt,
                BoardId = t.BoardId,
                BoardName = t.BoardName
            });

        /// <summary>
        /// Posts don't carry BoardId/BoardName themselves - this batch-
        /// resolves them via each post's thread (which already has them
        /// denormalized), one query for the whole page instead of one
        /// thread+board lookup per post.
        /// </summary>
        private List<SearchResult> MapPosts(IReadOnlyList<Post> posts)
        {
            if (posts.Count == 0)
            {
                return new List<SearchResult>();
            }

            var threads = _threadRepository.GetByIds(posts.Select(p => p.ThreadId).Distinct());

            return posts.Select(p =>
            {
                threads.TryGetValue(p.ThreadId, out var thread);
                return new SearchResult
                {
                    Type = "post",
                    Id = p.Id,
                    Content = p.Content,
                    Author = p.CreatedByUsername,
                    CreatedAt = p.CreatedAt,
                    ThreadId = p.ThreadId,
                    ThreadTitle = p.ThreadTitle,
                    BoardId = thread?.BoardId,
                    BoardName = thread?.BoardName
                };
            }).ToList();
        }

        private static IEnumerable<SearchResult> MapUsers(IReadOnlyList<User> users) =>
            users.Select(u => new SearchResult
            {
                Type = "user",
                Id = u.Id,
                Title = u.Username,
                Content = u.Email,
                Author = u.Username,
                CreatedAt = u.CreatedAt
            });
    }
}
