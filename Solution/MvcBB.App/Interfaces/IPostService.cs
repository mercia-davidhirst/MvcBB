using MvcBB.App.Models;

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
    /// Gets a user's most recent posts, for display on their profile
    /// </summary>
    Task<IEnumerable<PostDto>> GetPostsByUserAsync(int userId, int take = 5);
} 