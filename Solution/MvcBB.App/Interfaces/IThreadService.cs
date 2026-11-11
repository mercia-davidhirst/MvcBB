using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Poll;
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

        /// <summary>
        /// Toggles the current user's like on a post. Returns the post's new
        /// like state and total count.
        /// </summary>
        Task<(bool Liked, int LikeCount)> ToggleLikeAsync(int postId);

        /// <summary>
        /// Casts the current user's vote on a thread's poll. Throws
        /// ServiceException (with a user-facing message) if they've already
        /// voted or the option is invalid.
        /// </summary>
        Task<PollResponse> VotePollAsync(int threadId, int pollOptionId);

        /// <summary>Subscribes the current user to a thread, or updates their notify preferences if already subscribed.</summary>
        Task SubscribeToThreadAsync(int threadId, bool notifyViaMessage, bool notifyViaEmail);

        Task UnsubscribeFromThreadAsync(int threadId);
    }
} 