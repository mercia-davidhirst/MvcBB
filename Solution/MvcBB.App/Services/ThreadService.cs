using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Post;

namespace MvcBB.App.Services
{
    /// <summary>
    /// Service for managing forum threads and their posts
    /// </summary>
    public class ThreadService : IThreadService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ThreadService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("MvcBBApi");
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets threads, optionally filtered by board ID. When boardId is null, returns a paginated result from api/threads.
        /// </summary>
        public async Task<ThreadListResponse> GetThreadsAsync(int? boardId = null, int page = 1, int? pageSize = null)
        {
            try
            {
                AttachCurrentUserTokenIfPresent();

                if (boardId.HasValue)
                {
                    var response = await _httpClient.GetAsync($"api/threads/board/{boardId}");
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    var threads = JsonSerializer.Deserialize<List<ThreadResponse>>(content, _jsonOptions) ?? new List<ThreadResponse>();
                    return new ThreadListResponse
                    {
                        Threads = threads,
                        TotalThreads = threads.Count,
                        Page = 1,
                        PageSize = threads.Count,
                        TotalPages = threads.Count == 0 ? 1 : 1
                    };
                }

                var query = new List<string> { $"page={page}" };
                if (pageSize.HasValue)
                    query.Add($"pageSize={pageSize.Value}");
                var url = "api/threads?" + string.Join("&", query);
                var listResponse = await _httpClient.GetAsync(url);
                listResponse.EnsureSuccessStatusCode();
                var listContent = await listResponse.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ThreadListResponse>(listContent, _jsonOptions)
                    ?? new ThreadListResponse();
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
                AttachCurrentUserTokenIfPresent();
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
                var token = GetCurrentUserToken();
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
                var token = GetCurrentUserToken();
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
                var token = GetCurrentUserToken();
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
                AttachCurrentUserTokenIfPresent();
                var response = await _httpClient.GetAsync($"api/posts/thread/{threadId}");
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
                var token = GetCurrentUserToken();
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
                AttachCurrentUserTokenIfPresent();
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
                var token = GetCurrentUserToken();
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

        // Creating/editing threads and posts, and deleting threads, all require the
        // caller's own identity on the API side (NameIdentifier for ownership, or a
        // real Administrator/Moderator role for delete) - the generic mvc_client
        // service token can't satisfy either.
        private string GetCurrentUserToken()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst("api_token")?.Value
                ?? throw new SessionExpiredException();
        }

        // GET calls must keep working for anonymous visitors, but the API still
        // needs to know whether THIS caller is signed in to decide whether to
        // include private boards' threads/posts - attach the token only when one exists.
        private void AttachCurrentUserTokenIfPresent()
        {
            var token = _httpContextAccessor.GetApiTokenOrNull();
            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
} 