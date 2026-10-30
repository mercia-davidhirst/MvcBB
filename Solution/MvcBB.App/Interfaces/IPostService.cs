using MvcBB.App.Models;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Interfaces;

/// <summary>
/// Interface for managing forum posts
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Gets a post by ID
    /// </summary>
    Task<PostDto> GetPostAsync(int id);

    /// <summary>
    /// Creates a new post
    /// </summary>
    Task<PostDto> CreatePostAsync(CreatePostRequest request);

    /// <summary>
    /// Updates an existing post
    /// </summary>
    Task UpdatePostAsync(int id, UpdatePostRequest request);

    /// <summary>
    /// Deletes a post
    /// </summary>
    Task DeletePostAsync(int id);

    /// <summary>
    /// Gets a user's most recent posts, for display on their profile
    /// </summary>
    Task<IEnumerable<PostDto>> GetPostsByUserAsync(int userId, int take = 5);
} 