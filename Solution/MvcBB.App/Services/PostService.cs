using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;
using MvcBB.App.Models;
using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.Authorization;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Services;

/// <summary>
/// Service for managing forum posts
/// </summary>
public class PostService : IPostService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IBBCodeManagementService _bbCodeService;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostService(
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        IConfiguration configuration,
        IBBCodeManagementService bbCodeService)
    {
        _httpClient = httpClientFactory.CreateClient("MvcBBApi");
        _authService = authService;
        _configuration = configuration;
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
            var token = await GetTokenAsync();
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
            var token = await GetTokenAsync();
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
            var token = await GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/posts/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new ServiceException($"Failed to delete post {id}", ex);
        }
    }

    private async Task<string> GetTokenAsync()
    {
        var credentials = new ApiCredentials
        {
            ClientId = _configuration["Api:ClientId"] ?? "mvc_client",
            ClientSecret = _configuration["Api:ClientSecret"] 
                ?? throw new ServiceException("API client secret not configured")
        };
        var response = await _authService.GetTokenAsync(credentials);
        return response.Token;
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