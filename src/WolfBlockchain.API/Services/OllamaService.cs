using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WolfBlockchain.API.Services;

/// <summary>Chat message in a conversation</summary>
public sealed record ChatMessage(string Role, string Content);

/// <summary>Request DTO for chatbot endpoint</summary>
public sealed record ChatRequest(string Message, string? SessionId = null);

/// <summary>Response DTO from chatbot endpoint</summary>
public sealed record ChatResponse(string SessionId, string Reply, int TokensUsed);

/// <summary>Interface for Ollama AI service</summary>
public interface IOllamaService
{
    /// <summary>Send a message and receive a reply</summary>
    Task<string> ChatAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>Check whether the Ollama backend is reachable</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>Interface for chat session storage</summary>
public interface IChatSessionStore
{
    /// <summary>Return the message history for a session (oldest first)</summary>
    IReadOnlyList<ChatMessage> GetHistory(string sessionId);

    /// <summary>Append a message to a session</summary>
    void AddMessage(string sessionId, ChatMessage message);

    /// <summary>Delete a session and all its messages</summary>
    bool DeleteSession(string sessionId);

    /// <summary>Return true if the session exists</summary>
    bool SessionExists(string sessionId);
}

/// <summary>Thread-safe in-memory chat session store</summary>
public sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private readonly Dictionary<string, List<ChatMessage>> _sessions = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    public IReadOnlyList<ChatMessage> GetHistory(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var messages)
                ? messages.AsReadOnly()
                : Array.Empty<ChatMessage>();
        }
    }

    public void AddMessage(string sessionId, ChatMessage message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(message);
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var list))
            {
                list = new List<ChatMessage>();
                _sessions[sessionId] = list;
            }
            list.Add(message);
        }
    }

    public bool DeleteSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_lock)
        {
            return _sessions.Remove(sessionId);
        }
    }

    public bool SessionExists(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_lock)
        {
            return _sessions.ContainsKey(sessionId);
        }
    }
}

/// <summary>Ollama API chat service — calls the local Ollama /api/chat endpoint</summary>
public sealed class OllamaService : IOllamaService
{
    // -----------------------------------------------------------------------
    // Private Ollama API request/response shapes
    // -----------------------------------------------------------------------
    private sealed record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IEnumerable<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream = false);

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message);

    // -----------------------------------------------------------------------
    private readonly HttpClient _httpClient;
    private readonly IChatSessionStore _sessionStore;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _model;
    private readonly string _systemPrompt;

    private static string SanitizeForLog(string value) =>
        string.Concat(value.Select(c => c < 0x20 || c == 0x7F ? ' ' : c));

    public OllamaService(
        HttpClient httpClient,
        IChatSessionStore sessionStore,
        IConfiguration configuration,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var section = configuration.GetSection("Ollama");
        var baseUrl = section["BaseUrl"] ?? "http://localhost:11434";
        _model = section["Model"] ?? "llama3";
        _systemPrompt = section["SystemPrompt"] ?? "You are WolfBot, a helpful AI assistant for the Wolf Blockchain platform.";

        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        var timeoutSeconds = section.GetValue<int>("TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        _logger.LogInformation("ChatAsync session={SessionId}", SanitizeForLog(sessionId));

        // Build message list: system prompt + history + new user message
        var messages = new List<OllamaMessage>
        {
            new("system", _systemPrompt)
        };

        foreach (var h in _sessionStore.GetHistory(sessionId))
        {
            messages.Add(new OllamaMessage(h.Role, h.Content));
        }

        messages.Add(new OllamaMessage("user", userMessage));

        var requestBody = new OllamaChatRequest(_model, messages);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("api/chat", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Ollama backend unavailable for session {SessionId}", SanitizeForLog(sessionId));
            throw new InvalidOperationException("Ollama backend is unavailable. Please ensure Ollama is running.", ex);
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
        var reply = ollamaResponse?.Message?.Content ?? string.Empty;

        // Persist the exchange
        _sessionStore.AddMessage(sessionId, new ChatMessage("user", userMessage));
        _sessionStore.AddMessage(sessionId, new ChatMessage("assistant", reply));

        return reply;
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var response = await _httpClient.GetAsync("api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
