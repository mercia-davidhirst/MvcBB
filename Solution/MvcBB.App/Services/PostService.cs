using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Services;

/// <summary>
/// Service for managing forum posts
/// </summary>
public class PostService : IPostService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBBCodeService _bbCodeService;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IBBCodeService bbCodeService)
    {
        _httpClient = httpClientFactory.CreateClient("MvcBBApi");
        _httpContextAccessor = httpContextAccessor;
        _bbCodeService = bbCodeService;
    }

    /// <summary>
    /// Gets a post by ID
    /// </summary>
    public async Task<PostDto> GetPostAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/posts/{id}");
            response.EnsureSuccessStatusCode();
            
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
    /// Creates a new post
    /// </summary>
    public async Task<PostDto> CreatePostAsync(CreatePostRequest request)
    {
        try
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Validate BBCode in content
            if (!_bbCodeService.ValidateBBCode(request.Content))
            {
                throw new ServiceException("Invalid BBCode in post content");
            }

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/posts", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var post = JsonSerializer.Deserialize<Post>(responseContent, _jsonOptions);
            return post != null 
                ? MapToPostDto(post) 
                : throw new ServiceException("Failed to create post");
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceException("Failed to create post", ex);
        }
        catch (JsonException ex)
        {
            throw new ServiceException("Failed to parse created post data", ex);
        }
    }

    /// <summary>
    /// Updates an existing post
    /// </summary>
    public async Task UpdatePostAsync(int id, UpdatePostRequest request)
    {
        try
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Validate BBCode in content
            if (!_bbCodeService.ValidateBBCode(request.Content))
            {
                throw new ServiceException("Invalid BBCode in post content");
            }

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/posts/{id}", content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceException($"Failed to update post {id}", ex);
        }
    }

    /// <summary>
    /// Deletes a post
    /// </summary>
    public async Task DeletePostAsync(int id)
    {
        try
        {
            var token = GetCurrentUserToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/posts/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceException($"Failed to delete post {id}", ex);
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

    // Creating/editing/deleting a post all require the caller's own identity on the
    // API side (NameIdentifier for ownership, or a real Administrator/Moderator role
    // for delete) - the generic mvc_client service token can't satisfy either.
    private string GetCurrentUserToken()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
            ?? throw new SessionExpiredException();
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