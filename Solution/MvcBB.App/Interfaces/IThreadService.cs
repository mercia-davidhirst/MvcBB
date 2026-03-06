using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Interfaces
{
    public interface IThreadService
    {
        Task<ThreadListResponse> GetThreadsAsync(int? boardId = null, int page = 1, int? pageSize = null);
        Task<ThreadResponse> GetThreadAsync(int id);
        Task<ThreadResponse> CreateThreadAsync(CreateForumThreadRequest request);
        Task UpdateThreadAsync(int id, UpdateForumThreadRequest request);
        Task DeleteThreadAsync(int id);
        Task<IEnumerable<PostResponse>> GetThreadPostsAsync(int threadId);
        Task<PostResponse> CreatePostAsync(int threadId, CreatePostRequest request);
        Task<PostResponse> GetPostAsync(int id);
        Task UpdatePostAsync(int id, UpdatePostRequest request);
    }
} 