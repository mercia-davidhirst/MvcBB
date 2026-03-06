using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Models.Board;
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
        private readonly IBoardRepository _boardRepository;
        private readonly IForumThreadRepository _threadRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;

        public SearchController(
            IBoardRepository boardRepository,
            IForumThreadRepository threadRepository,
            IPostRepository postRepository,
            IUserRepository userRepository)
        {
            _boardRepository = boardRepository;
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

            var query = request.Query.ToLower();
            var results = new List<SearchResult>();

            switch (request.Type?.ToLower())
            {
                case "threads":
                    results.AddRange(SearchThreads(query, request.BoardId));
                    break;
                case "posts":
                    results.AddRange(SearchPosts(query, request.BoardId));
                    break;
                case "users":
                    results.AddRange(SearchUsers(query));
                    break;
                default:
                    results.AddRange(SearchThreads(query, request.BoardId));
                    results.AddRange(SearchPosts(query, request.BoardId));
                    results.AddRange(SearchUsers(query));
                    break;
            }

            results = results.OrderByDescending(r => r.CreatedAt).ToList();

            var totalResults = results.Count;
            var pageSize = Math.Max(1, request.PageSize);
            var page = Math.Max(1, request.Page);
            var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);
            var skip = (page - 1) * pageSize;

            return Ok(new SearchResponse
            {
                Results = results.Skip(skip).Take(pageSize).ToList(),
                TotalResults = totalResults,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        private IEnumerable<SearchResult> SearchThreads(string query, int? boardId)
        {
            var threads = _threadRepository.GetAll()
                .Where(t => !boardId.HasValue || t.BoardId == boardId.Value)
                .Where(t => t.Title.ToLower().Contains(query) || t.CreatedByUserId.ToLower().Contains(query));

            foreach (var t in threads)
            {
                var board = _boardRepository.GetById(t.BoardId);
                yield return new SearchResult
                {
                    Type = "thread",
                    Id = t.Id,
                    Title = t.Title,
                    Author = t.CreatedByUserId,
                    CreatedAt = t.CreatedAt,
                    BoardId = t.BoardId,
                    BoardName = board?.Name
                };
            }
        }

        private IEnumerable<SearchResult> SearchPosts(string query, int? boardId)
        {
            var posts = _postRepository.GetAll()
                .Where(p => p.Content.ToLower().Contains(query) || p.CreatedByUserId.ToLower().Contains(query));

            foreach (var p in posts)
            {
                var thread = _threadRepository.GetById(p.ThreadId);
                var board = thread != null ? _boardRepository.GetById(thread.BoardId) : null;

                if (boardId.HasValue && board?.Id != boardId.Value)
                    continue;

                yield return new SearchResult
                {
                    Type = "post",
                    Id = p.Id,
                    Content = p.Content,
                    Author = p.CreatedByUserId,
                    CreatedAt = p.CreatedAt,
                    ThreadId = p.ThreadId,
                    ThreadTitle = thread?.Title,
                    BoardId = board?.Id,
                    BoardName = board?.Name
                };
            }
        }

        private IEnumerable<SearchResult> SearchUsers(string query)
        {
            return _userRepository.GetAll()
                .Where(u => u.Username.ToLower().Contains(query) || u.Email.ToLower().Contains(query))
                .Select(u => new SearchResult
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
}
