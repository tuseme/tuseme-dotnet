using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Tuseme
{
    // ── Exception Types ────────────────────────────────────────

    public class TusemeException : Exception
    {
        public int? StatusCode { get; }
        public Dictionary<string, object>? Response { get; }

        public TusemeException(string message, int? statusCode = null,
            Dictionary<string, object>? response = null) : base(message)
        {
            StatusCode = statusCode;
            Response = response;
        }
    }

    public class AuthenticationException : TusemeException
    {
        public AuthenticationException(string message, int? statusCode = null,
            Dictionary<string, object>? response = null) : base(message, statusCode, response) { }
    }

    public class ValidationException : TusemeException
    {
        public ValidationException(string message, int? statusCode = null,
            Dictionary<string, object>? response = null) : base(message, statusCode, response) { }
    }

    public class RateLimitException : TusemeException
    {
        public int RetryAfter { get; }

        public RateLimitException(string message, int retryAfter, int? statusCode = null,
            Dictionary<string, object>? response = null) : base(message, statusCode, response)
        {
            RetryAfter = retryAfter;
        }
    }

    // ── Models ─────────────────────────────────────────────────

    public class Recipient
    {
        [JsonPropertyName("msisdn")]
        public string Msisdn { get; set; } = "";

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
    }

    public class SendRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("sender_id")]
        public string SenderId { get; set; } = "TUSEME-LTD";

        [JsonPropertyName("recipients")]
        public List<Recipient> Recipients { get; set; } = new();

        [JsonPropertyName("type")]
        public string Type { get; set; } = "promotional";

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "MEDIUM";

        [JsonPropertyName("group_ids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? GroupIds { get; set; }

        [JsonPropertyName("contact_ids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ContactIds { get; set; }

        [JsonPropertyName("scheduled_for")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ScheduledFor { get; set; }

        [JsonPropertyName("timezone")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Timezone { get; set; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class SendResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = "";

        [JsonPropertyName("batch_id")]
        public string BatchId { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("estimated_cost")]
        public double? EstimatedCost { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "KES";

        [JsonPropertyName("selected_provider")]
        public string? SelectedProvider { get; set; }

        [JsonPropertyName("recipient_count")]
        public int RecipientCount { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";
    }

    public class MessageStatus
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("recipient")]
        public string Recipient { get; set; } = "";

        [JsonPropertyName("sender_id")]
        public string SenderId { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("cost")]
        public double? Cost { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "KES";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";

        [JsonPropertyName("delivered_at")]
        public string? DeliveredAt { get; set; }
    }

    // ── HTTP Client ────────────────────────────────────────────

    internal class AuthResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;
    }

    internal class TusemeHttpClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _baseUrl;
        private readonly int _maxRetries;
        private readonly HttpClient _http;
        private string? _accessToken;
        private DateTime _tokenExpiresAt = DateTime.MinValue;
        private readonly SemaphoreSlim _authLock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public TusemeHttpClient(string apiKey, string apiSecret, string baseUrl,
            int timeoutSeconds, int maxRetries)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _baseUrl = baseUrl.TrimEnd('/');
            _maxRetries = maxRetries;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("tuseme-dotnet/1.0.0");
        }

        public string ApiKey => _apiKey;

        private async Task AuthenticateAsync()
        {
            var body = JsonSerializer.Serialize(new { api_key = _apiKey, api_secret = _apiSecret });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_baseUrl}/auth/login", content);

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new AuthenticationException("Invalid API credentials.", 401);

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var auth = JsonSerializer.Deserialize<AuthResponse>(json)!;

            _accessToken = auth.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(auth.ExpiresIn - 60);
        }

        private async Task EnsureAuthAsync()
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt)
                return;

            await _authLock.WaitAsync();
            try
            {
                if (_accessToken == null || DateTime.UtcNow >= _tokenExpiresAt)
                    await AuthenticateAsync();
            }
            finally
            {
                _authLock.Release();
            }
        }

        public async Task<T> RequestAsync<T>(HttpMethod method, string path,
            object? body = null, Dictionary<string, string>? queryParams = null)
        {
            await EnsureAuthAsync();

            var url = _baseUrl + path;
            if (queryParams is { Count: > 0 })
            {
                var qs = string.Join("&", queryParams.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                url += "?" + qs;
            }

            Exception? lastError = null;
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    var request = new HttpRequestMessage(method, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                    if (body != null)
                    {
                        var json = JsonSerializer.Serialize(body, JsonOpts);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    var resp = await _http.SendAsync(request);
                    var responseBody = await resp.Content.ReadAsStringAsync();

                    if ((int)resp.StatusCode == 429)
                    {
                        var retryAfter = resp.Headers.RetryAfter?.Delta?.Seconds ?? 5;
                        var ex = new RateLimitException("Rate limit exceeded", retryAfter, 429);
                        if (attempt < _maxRetries)
                        {
                            await Task.Delay(retryAfter * 1000);
                            lastError = ex;
                            continue;
                        }
                        throw ex;
                    }

                    if ((int)resp.StatusCode >= 500)
                    {
                        var ex = new TusemeException($"Server error: {resp.StatusCode}", (int)resp.StatusCode);
                        if (attempt < _maxRetries)
                        {
                            await Task.Delay((int)(500 * Math.Pow(2, attempt - 1)));
                            lastError = ex;
                            continue;
                        }
                        throw ex;
                    }

                    if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (attempt == 1)
                        {
                            _accessToken = null;
                            await EnsureAuthAsync();
                            continue;
                        }
                        throw new AuthenticationException("Authentication failed", 401);
                    }

                    if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        throw new ValidationException($"Validation error: {responseBody}", 400);

                    if (!resp.IsSuccessStatusCode)
                        throw new TusemeException($"API error: {resp.StatusCode}", (int)resp.StatusCode);

                    return JsonSerializer.Deserialize<T>(responseBody)!;
                }
                catch (HttpRequestException ex)
                {
                    lastError = new TusemeException($"Network error: {ex.Message}");
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay((int)(500 * Math.Pow(2, attempt - 1)));
                        continue;
                    }
                }
            }

            throw lastError ?? new TusemeException("Request failed after retries");
        }
    }

    // ── Messages Resource ──────────────────────────────────────

    public class Messages
    {
        private readonly TusemeHttpClient _http;

        internal Messages(TusemeHttpClient http) => _http = http;

        /// <summary>Send an SMS to one or more recipients.</summary>
        public Task<SendResponse> SendAsync(SendRequest request)
            => _http.RequestAsync<SendResponse>(HttpMethod.Post, "/messages/send", request);

        /// <summary>Get the delivery status of a message.</summary>
        public Task<MessageStatus> GetAsync(string messageId)
            => _http.RequestAsync<MessageStatus>(HttpMethod.Get, $"/messages/{messageId}");

        /// <summary>List sent messages with pagination.</summary>
        public Task<JsonElement> ListAsync(int page = 1, int pageSize = 20, string? status = null)
        {
            var p = new Dictionary<string, string>
            {
                ["page"] = page.ToString(),
                ["page_size"] = pageSize.ToString(),
            };
            if (status != null) p["status"] = status;
            return _http.RequestAsync<JsonElement>(HttpMethod.Get, "/messages", queryParams: p);
        }
    }

    // ── Main Client ────────────────────────────────────────────

    public class TusemeClient
    {
        private readonly TusemeHttpClient _http;

        /// <summary>Messages API resource.</summary>
        public Messages Messages { get; }

        /// <summary>Create a new Tuseme API client.</summary>
        /// <param name="apiKey">Your API Key (tk_test_ or tk_live_).</param>
        /// <param name="apiSecret">Your API Secret (sk_test_ or sk_live_).</param>
        /// <param name="baseUrl">API base URL.</param>
        /// <param name="timeoutSeconds">Request timeout in seconds.</param>
        /// <param name="maxRetries">Maximum retry attempts.</param>
        public TusemeClient(
            string apiKey,
            string apiSecret,
            string baseUrl = "https://api.tuseme.co.ke/api/v1",
            int timeoutSeconds = 30,
            int maxRetries = 3)
        {
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentException("apiKey is required");
            if (string.IsNullOrEmpty(apiSecret)) throw new ArgumentException("apiSecret is required");

            _http = new TusemeHttpClient(apiKey, apiSecret, baseUrl, timeoutSeconds, maxRetries);
            Messages = new Messages(_http);
        }

        /// <summary>Whether this client uses sandbox credentials.</summary>
        public bool IsSandbox => _http.ApiKey.StartsWith("tk_test_");

        /// <summary>Whether this client uses production credentials.</summary>
        public bool IsProduction => _http.ApiKey.StartsWith("tk_live_");
    }
}
