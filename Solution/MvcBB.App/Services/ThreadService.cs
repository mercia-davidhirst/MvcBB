using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MvcBB.App.Exceptions;
using MvcBB.App.Extensions;
using MvcBB.App.Interfaces;
using MvcBB.Shared.Models.ForumThread;
using MvcBB.Shared.Models.Poll;
using MvcBB.Shared.Models.Post;
using MvcBB.Shared.Models.ThreadSubscription;

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
                    response.EnsureSuccessOrSessionExpired();
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
                listResponse.EnsureSuccessOrSessionExpired();
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
                response.EnsureSuccessOrSessionExpired();
                
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
                response.EnsureSuccessOrSessionExpired();
                
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
                response.EnsureSuccessOrSessionExpired();
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
                response.EnsureSuccessOrSessionExpired();
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
                response.EnsureSuccessOrSessionExpired();

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
                response.EnsureSuccessOrSessionExpired();
                
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
                response.EnsureSuccessOrSessionExpired();
                
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
                response.EnsureSuccessOrSessionExpired();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException($"Failed to update post {id}", ex);
            }
        }

        /// <summary>
        /// Toggles the current user's like on a post.
        /// </summary>
        public async Task<(bool Liked, int LikeCount)> ToggleLikeAsync(int postId)
        {
            try
            {
                var token = GetCurrentUserToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/posts/{postId}/like", content: null);
                response.EnsureSuccessOrSessionExpired();

                var result = await response.Content.ReadFromJsonAsync<ToggleLikeResponse>();
                return (result?.Liked ?? false, result?.LikeCount ?? 0);
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException($"Failed to like post {postId}", ex);
            }
        }

        private class ToggleLikeResponse
        {
            public bool Liked { get; set; }
            public int LikeCount { get; set; }
        }

        /// <summary>
        /// Casts the current user's vote on a thread's poll.
        /// </summary>
        public async Task<PollResponse> VotePollAsync(int threadId, int pollOptionId)
        {
            try
            {
                var token = GetCurrentUserToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(new VotePollRequest { PollOptionId = pollOptionId });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"api/threads/{threadId}/poll/vote", content);
                await EnsureSuccessOrThrowAsync(response);

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PollResponse>(responseContent, _jsonOptions)
                    ?? throw new ServiceException("Failed to cast vote");
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException("Failed to cast vote", ex);
            }
        }

        /// <summary>
        /// Subscribes the current user to a thread, or updates their notify
        /// preferences if already subscribed (the API treats PUT as an upsert).
        /// </summary>
        public async Task SubscribeToThreadAsync(int threadId, bool notifyViaMessage, bool notifyViaEmail)
        {
            try
            {
                var token = GetCurrentUserToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(new SubscribeToThreadRequest
                {
                    NotifyViaMessage = notifyViaMessage,
                    NotifyViaEmail = notifyViaEmail
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/threads/{threadId}/subscription", content);
                response.EnsureSuccessOrSessionExpired();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException("Failed to subscribe to thread", ex);
            }
        }

        public async Task UnsubscribeFromThreadAsync(int threadId)
        {
            try
            {
                var token = GetCurrentUserToken();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"api/threads/{threadId}/subscription");
                response.EnsureSuccessOrSessionExpired();
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceException("Failed to unsubscribe from thread", ex);
            }
        }

        // The vote endpoint returns a plain { message } body on failure (already
        // voted, invalid option) rather than a ValidationProblemDetails payload -
        // extract it so the caller gets that specific text instead of a generic failure.
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new SessionExpiredException();
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrEmpty(message))
                    {
                        throw new ServiceException(message);
                    }
                }
            }
            catch (JsonException)
            {
                // Body wasn't the expected shape - fall through to the generic exception below.
            }

            response.EnsureSuccessOrSessionExpired();
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