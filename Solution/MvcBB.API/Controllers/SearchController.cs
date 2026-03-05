using Microsoft.AspNetCore.Mvc;
using MvcBB.Shared.Models.Board;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.Search;
using MvcBB.Shared.Models.User;

namespace MvcBB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private static readonly List<Board> _boards = new();
        private static readonly List<ForumThread> _threads = new();
        private static readonly List<Post> _posts = new();
        private static readonly List<User> _users = new();

        [HttpGet]
        public ActionResult<SearchResponse> Search([FromQuery] SearchRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = request.Query.ToLower();
            var results = new List<SearchResult>();

            // Search based on type
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
                default: // "all" or null
                    results.AddRange(SearchThreads(query, request.BoardId));
                    results.AddRange(SearchPosts(query, request.BoardId));
                    results.AddRange(SearchUsers(query));
                    break;
            }

            // Sort results by relevance (newest first)
            results = results.OrderByDescending(r => r.CreatedAt).ToList();

            // Calculate pagination
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
            return _threads
                .Where(t => !boardId.HasValue || t.BoardId == boardId.Value)
                .Where(t => t.Title.ToLower().Contains(query) || 
                           t.CreatedByUserId.ToLower().Contains(query))
                .Select(t =>
                {
                    var board = _boards.FirstOrDefault(b => b.Id == t.BoardId);
                    return new SearchResult
                    {
                        Type = "thread",
                        Id = t.Id,
                        Title = t.Title,
                        Author = t.CreatedByUserId,
                        CreatedAt = t.CreatedAt,
                        BoardId = t.BoardId,
                        BoardName = board?.Name
                    };
                });
        }

        private IEnumerable<SearchResult> SearchPosts(string query, int? boardId)
        {
            return _posts
                .Where(p => p.Content.ToLower().Contains(query) || 
                           p.CreatedByUserId.ToLower().Contains(query))
                .Select(p =>
                {
                    var thread = _threads.FirstOrDefault(t => t.Id == p.ThreadId);
                    var board = thread != null ? _boards.FirstOrDefault(b => b.Id == thread.BoardId) : null;

                    if (boardId.HasValue && board?.Id != boardId.Value)
                    {
                        return null;
                    }

                    return new SearchResult
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
                })
                .Where(r => r != null);
        }

        private IEnumerable<SearchResult> SearchUsers(string query)
        {
            return _users
                .Where(u => u.Username.ToLower().Contains(query) || 
                           u.Email.ToLower().Contains(query))
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