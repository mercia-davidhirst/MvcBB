using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Services;

/// <summary>
/// Service for managing forum posts
/// </summary>
public class PostService : IPostService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("MvcBBApi");
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets a post by ID
    /// </summary>
    public async Task<PostDto> GetPostAsync(int id)
    {
        try
        {
            AttachCurrentUserTokenIfPresent();
            var response = await _httpClient.GetAsync($"api/posts/{id}");
            response.EnsureSuccessOrSessionExpired();
            
            var content = await response.Content.ReadAsStringAsync();
            var post = JsonSerializer.Deserialize<Post>(content, _jsonOptions);
            return post != null 
                ? MapToPostDto(post) 
                : throw new ServiceException($"Post with ID {id} not found");
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceException($"Failed to retrieve post {id}", ex);
        }
        catch (JsonException ex)
        {
            throw new ServiceException($"Failed to parse post {id} data", ex);
        }
    }

    /// <summary>
    /// Gets a user's most recent posts, for display on their profile
    /// </summary>
    public async Task<IEnumerable<PostDto>> GetPostsByUserAsync(int userId, int take = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/posts/by-user/{userId}?take={take}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var posts = JsonSerializer.Deserialize<List<Post>>(content, _jsonOptions) ?? new List<Post>();
            return posts.Select(MapToPostDto);
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceException($"Failed to retrieve posts for user {userId}", ex);
        }
        catch (JsonException ex)
        {
            throw new ServiceException($"Failed to parse posts data for user {userId}", ex);
        }
    }

    // GET calls must keep working for anonymous visitors, but the API still
    // needs to know whether THIS caller is signed in to decide whether to
    // include a post from a private board - attach the token only when one exists.
    private void AttachCurrentUserTokenIfPresent()
    {
        var token = _httpContextAccessor.GetApiTokenOrNull();
        if (token != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static PostDto MapToPostDto(Post post) => new()
    {
        Id = post.Id,
        Content = post.Content,
        ThreadId = post.ThreadId,
        CreatedByUserId = post.CreatedByUserId,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt,
        IsEdited = post.IsEdited,
        EditReason = post.EditReason ?? string.Empty,
        QuotedPostId = post.QuotedPostId,
        QuotedPost = post.QuotedPost != null ? MapToPostDto(post.QuotedPost) : null
    };
} 