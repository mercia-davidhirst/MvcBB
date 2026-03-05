using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MvcBB.App.Exceptions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.Authorization;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Service for managing forum threads and their posts
    /// </summary>
    public class ThreadService : IThreadService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ThreadService(
            IHttpClientFactory httpClientFactory, 
            IAuthService authService,
            IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _authService = authService;
            _configuration = configuration;
        }

        /// <summary>
        /// Gets all threads, optionally filtered by board ID
        /// </summary>
        public async Task<IEnumerable<ThreadResponse>> GetThreadsAsync(int? boardId = null)
        {
            try
            {
                var url = boardId.HasValue ? $"api/threads?boardId={boardId}" : "api/threads";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ThreadResponse>>(content, _jsonOptions) 
                    ?? Array.Empty<ThreadResponse>();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException("Failed to retrieve threads", ex);
            }
            catch (JsonException ex)
            {
                throw new ServiceException("Failed to parse thread data", ex);
            }
        }

        /// <summary>
        /// Gets a specific thread by ID
        /// </summary>
        public async Task<ThreadResponse> GetThreadAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/threads/{id}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ThreadResponse>(content, _jsonOptions)
                    ?? throw new ServiceException($"Thread with ID {id} not found");
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException($"Failed to retrieve thread {id}", ex);
            }
            catch (JsonException ex)
            {
                throw new ServiceException($"Failed to parse thread {id} data", ex);
            }
        }

        /// <summary>
        /// Creates a new thread
        /// </summary>
        public async Task<ThreadResponse> CreateThreadAsync(CreateForumThreadRequest request)
        {
            try
            {
                var token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/threads", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ThreadResponse>(responseContent, _jsonOptions)
                    ?? throw new ServiceException("Failed to create thread");
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException("Failed to create thread", ex);
            }
            catch (JsonException ex)
            {
                throw new ServiceException("Failed to parse created thread data", ex);
            }
        }

        /// <summary>
        /// Updates an existing thread
        /// </summary>
        public async Task UpdateThreadAsync(int id, UpdateForumThreadRequest request)
        {
            try
            {
                var token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/threads/{id}", content);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException($"Failed to update thread {id}", ex);
            }
        }

        /// <summary>
        /// Deletes a thread
        /// </summary>
        public async Task DeleteThreadAsync(int id)
        {
            try
            {
                var token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"api/threads/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException($"Failed to delete thread {id}", ex);
            }
        }

        /// <summary>
        /// Gets all posts for a specific thread
        /// </summary>
        public async Task<IEnumerable<PostResponse>> GetThreadPostsAsync(int threadId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/threads/{threadId}/posts");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<PostResponse>>(content, _jsonOptions)
                    ?? Array.Empty<PostResponse>();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException($"Failed to retrieve posts for thread {threadId}", ex);
            }
            catch (JsonException ex)
            {
                throw new ServiceException($"Failed to parse posts data for thread {threadId}", ex);
            }
        }

        /// <summary>
        /// Creates a new post in a thread
        /// </summary>
        public async Task<PostResponse> CreatePostAsync(int threadId, CreatePostRequest request)
        {
            try
            {
                var token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                request.ThreadId = threadId;
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("api/posts", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PostResponse>(responseContent, _jsonOptions)
                    ?? throw new ServiceException("Failed to create post");
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
        /// Gets a specific post by ID
        /// </summary>
        public async Task<PostResponse> GetPostAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/posts/{id}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PostResponse>(content, _jsonOptions)
                    ?? throw new ServiceException($"Post with ID {id} not found");
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
        /// Updates an existing post
        /// </summary>
        public async Task UpdatePostAsync(int id, UpdatePostRequest request)
        {
            try
            {
                var token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
    }
} 