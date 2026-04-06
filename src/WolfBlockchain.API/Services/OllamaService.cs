using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WolfBlockchain.API.Services;

// ============= OPTIONS =============

/// <summary>Configuration options for the Ollama integration</summary>
public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3";
    public int TimeoutSeconds { get; set; } = 120;
    public string SystemPrompt { get; set; } =
        "You are WolfBot, an AI assistant for the WolfBlockchain platform. " +
        "Help users with blockchain, tokens, smart contracts, and platform questions. " +
        "Be concise, accurate, and helpful.";
}

// ============= DTOs =============

/// <summary>A single chat message in a conversation</summary>
public sealed record ChatMessage
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>A chat session with message history</summary>
public sealed class ChatSession
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public List<ChatMessage> Messages { get; } = new();
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Request payload for a chat message</summary>
public sealed class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

/// <summary>Response from a chat request</summary>
public sealed class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Model { get; set; } = string.Empty;
}

// ============= INTERFACES =============

/// <summary>Generic chat service abstraction – supports Ollama, OpenAI, and Mock backends</summary>
public interface IChatService
{
    /// <summary>Send a message and get a reply (session is managed by the implementation)</summary>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>Backend identifier (e.g. "ollama", "openai", "mock")</summary>
    string BackendName { get; }
}

/// <summary>Ollama-specific service operations</summary>
public interface IOllamaService : IChatService
{
    /// <summary>Check whether the Ollama backend is reachable</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>List models available on the Ollama server</summary>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Session-scoped chat history storage</summary>
public interface IChatSessionStore
{
    /// <summary>Create a new session and return its ID</summary>
    string CreateSession();

    /// <summary>Get an existing session (returns null if not found)</summary>
    ChatSession? GetSession(string sessionId);

    /// <summary>Get-or-create: returns existing session or creates a new one</summary>
    ChatSession GetOrCreateSession(string? sessionId, out bool wasCreated);

    /// <summary>Append a message to an existing session</summary>
    void AppendMessage(string sessionId, ChatMessage message);

    /// <summary>Delete a session</summary>
    bool DeleteSession(string sessionId);

    /// <summary>Count of live sessions</summary>
    int ActiveSessionCount { get; }
}

// ============= SESSION STORE =============

/// <summary>In-memory implementation of IChatSessionStore</summary>
public sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private readonly Dictionary<string, ChatSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(2);

    /// <inheritdoc/>
    public string CreateSession()
    {
        var session = new ChatSession();
        lock (_lock)
        {
            _sessions[session.SessionId] = session;
            PurgeExpired();
        }
        return session.SessionId;
    }

    /// <inheritdoc/>
    public ChatSession? GetSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var s) ? s : null;
        }
    }

    /// <inheritdoc/>
    public ChatSession GetOrCreateSession(string? sessionId, out bool wasCreated)
    {
        lock (_lock)
        {
            PurgeExpired();
            if (!string.IsNullOrWhiteSpace(sessionId) && _sessions.TryGetValue(sessionId, out var existing))
            {
                wasCreated = false;
                return existing;
            }

            var newSession = new ChatSession();
            _sessions[newSession.SessionId] = newSession;
            wasCreated = true;
            return newSession;
        }
    }

    /// <inheritdoc/>
    public void AppendMessage(string sessionId, ChatMessage message)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");
            session.Messages.Add(message);
            session.LastActivityUtc = DateTime.UtcNow;
        }
    }

    /// <inheritdoc/>
    public bool DeleteSession(string sessionId)
    {
        lock (_lock)
        {
            return _sessions.Remove(sessionId);
        }
    }

    /// <inheritdoc/>
    public int ActiveSessionCount
    {
        get { lock (_lock) { return _sessions.Count; } }
    }

    private void PurgeExpired()
    {
        var cutoff = DateTime.UtcNow - SessionTtl;
        var toRemove = _sessions.Values
            .Where(s => s.LastActivityUtc < cutoff)
            .Select(s => s.SessionId)
            .ToList();
        foreach (var id in toRemove)
            _sessions.Remove(id);
    }
}

// ============= OLLAMA IMPLEMENTATION =============

/// <summary>Ollama chat service – communicates with a local Ollama server</summary>
public sealed class OllamaChatService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly IChatSessionStore _sessionStore;
    private readonly ILogger<OllamaChatService> _logger;

    public string BackendName => "ollama";

    public OllamaChatService(
        HttpClient httpClient,
        OllamaOptions options,
        IChatSessionStore sessionStore,
        ILogger<OllamaChatService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
            return new ChatResponse { Success = false, Error = "Message cannot be empty.", SessionId = string.Empty };

        var session = _sessionStore.GetOrCreateSession(request.SessionId, out _);

        _logger.LogInformation("Ollama chat request – session {SessionId}, model {Model}",
            session.SessionId, _options.Model);

        var messages = BuildMessageList(session.Messages, request.Message);

        try
        {
            var payload = new
            {
                model = _options.Model,
                messages,
                stream = false
            };

            var httpResponse = await _httpClient.PostAsJsonAsync("api/chat", payload, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var ollamaResp = await httpResponse.Content.ReadFromJsonAsync<OllamaChatApiResponse>(
                cancellationToken: cancellationToken);

            var replyContent = ollamaResp?.Message?.Content ?? string.Empty;

            _sessionStore.AppendMessage(session.SessionId, new ChatMessage { Role = "user",      Content = request.Message });
            _sessionStore.AppendMessage(session.SessionId, new ChatMessage { Role = "assistant", Content = replyContent });

            return new ChatResponse
            {
                Success = true,
                Reply = replyContent,
                SessionId = session.SessionId,
                Model = _options.Model
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama HTTP error for session {SessionId}", session.SessionId);
            return new ChatResponse { Success = false, Error = "Ollama backend unavailable.", SessionId = session.SessionId };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama request timed out for session {SessionId}", session.SessionId);
            return new ChatResponse { Success = false, Error = "Request timed out.", SessionId = session.SessionId };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync("api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>("api/tags", cancellationToken);
            var names = resp?.Models?.Select(m => m.Name ?? string.Empty).Where(n => n.Length > 0).ToList();
            return names is not null ? names : Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list Ollama models");
            return Array.Empty<string>();
        }
    }

    private List<object> BuildMessageList(IReadOnlyList<ChatMessage> history, string newUserMessage)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(_options.SystemPrompt))
            messages.Add(new { role = "system", content = _options.SystemPrompt });

        foreach (var m in history.TakeLast(20))
            messages.Add(new { role = m.Role, content = m.Content });

        messages.Add(new { role = "user", content = newUserMessage });
        return messages;
    }

    // Internal Ollama API response shapes
    private sealed class OllamaChatApiResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelEntry>? Models { get; set; }
    }

    private sealed class OllamaModelEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}

// ============= MOCK IMPLEMENTATION =============

/// <summary>Mock chat service – used when Ollama is not configured or not reachable</summary>
public sealed class MockChatService : IChatService
{
    private readonly IChatSessionStore _sessionStore;

    public string BackendName => "mock";

    public MockChatService(IChatSessionStore sessionStore)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <inheritdoc/>
    public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = _sessionStore.GetOrCreateSession(request.SessionId, out _);

        var reply = $"[Mock] Echo: {request.Message}";

        _sessionStore.AppendMessage(session.SessionId, new ChatMessage { Role = "user",      Content = request.Message });
        _sessionStore.AppendMessage(session.SessionId, new ChatMessage { Role = "assistant", Content = reply });

        return Task.FromResult(new ChatResponse
        {
            Success = true,
            Reply = reply,
            SessionId = session.SessionId,
            Model = "mock"
        });
    }
}

// ============= LOG SANITIZER =============

/// <summary>Helpers to sanitise user-controlled strings before logging (prevents log-injection)</summary>
public static class LogSanitizer
{
    /// <summary>
    /// Strips ASCII control chars (0–31, 127) and C1 control chars (0x80–0x9F)
    /// from a user-supplied value before it is passed to a logger.
    /// </summary>
    public static string SanitizeForLog(string? value)
    {
        if (value is null) return string.Empty;
        return string.Concat(value.Select(c =>
            c < 0x20 || c == 0x7F || (c >= 0x80 && c <= 0x9F) ? ' ' : c));
    }
}
