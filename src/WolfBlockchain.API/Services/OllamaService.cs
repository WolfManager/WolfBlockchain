using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WolfBlockchain.API.Services;

// ============= CHATBOT DTOs =============

/// <summary>Role of a participant in a chat conversation</summary>
public enum ChatRole
{
    User,
    Assistant,
    System
}

/// <summary>A single message in a chat conversation</summary>
public record ChatMessageDto
{
    public ChatRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>Request payload for the chatbot</summary>
public record ChatRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    /// <summary>Optional system prompt override for this session</summary>
    public string? SystemPrompt { get; init; }
}

/// <summary>Response from the chatbot</summary>
public record ChatResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public bool Success { get; init; }
    public string? Error { get; init; }
}

// ============= OLLAMA API CONTRACTS (internal) =============

internal sealed record OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}

internal sealed record OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; init; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;
}

internal sealed record OllamaChatResponseMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}

internal sealed record OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaChatResponseMessage? Message { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }
}

internal sealed record OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; init; } = new();
}

internal sealed record OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

// ============= INTERFACES =============

/// <summary>Service for interacting with the Ollama LLM backend</summary>
public interface IOllamaService
{
    /// <summary>Send a chat message to Ollama and receive a response</summary>
    Task<string> ChatAsync(string message, IReadOnlyList<ChatMessageDto> history, string? systemPrompt, CancellationToken ct = default);

    /// <summary>Check whether the Ollama service is reachable and the configured model is available</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Get the name of the currently configured model</summary>
    string ModelName { get; }
}

/// <summary>Thread-safe, in-memory store for per-session chat history</summary>
public interface IChatSessionStore
{
    /// <summary>Append a message to the session history</summary>
    void AddMessage(string sessionId, ChatRole role, string content);

    /// <summary>Retrieve the full message history for a session (oldest first)</summary>
    IReadOnlyList<ChatMessageDto> GetHistory(string sessionId);

    /// <summary>Remove all messages for a session</summary>
    void ClearSession(string sessionId);

    /// <summary>Returns true if the session exists</summary>
    bool SessionExists(string sessionId);
}

// ============= IMPLEMENTATIONS =============

/// <summary>
/// OllamaService - connects to the local Ollama REST API (/api/chat).
/// Configuration keys (appsettings.json section "Ollama"):
///   BaseUrl   – default http://localhost:11434
///   Model     – default llama3
///   TimeoutSeconds – default 60
///   SystemPrompt – optional system-level prompt for every session
/// </summary>
public sealed class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _model;
    private readonly string _defaultSystemPrompt;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 60);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        _model = configuration["Ollama:Model"] ?? "llama3";
        _defaultSystemPrompt = configuration["Ollama:SystemPrompt"]
            ?? "You are a helpful assistant for the Wolf Blockchain platform. Answer questions about blockchain, smart contracts, and cryptocurrency concisely and accurately.";
    }

    /// <inheritdoc/>
    public string ModelName => _model;

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        string? systemPrompt,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var messages = new List<OllamaChatMessage>();

        // Add system prompt first
        var system = systemPrompt ?? _defaultSystemPrompt;
        if (!string.IsNullOrWhiteSpace(system))
        {
            messages.Add(new OllamaChatMessage { Role = "system", Content = system });
        }

        // Add conversation history
        foreach (var msg in history)
        {
            messages.Add(new OllamaChatMessage
            {
                Role = msg.Role == ChatRole.User ? "user" : "assistant",
                Content = msg.Content
            });
        }

        // Add current user message
        messages.Add(new OllamaChatMessage { Role = "user", Content = message });

        var request = new OllamaChatRequest
        {
            Model = _model,
            Messages = messages,
            Stream = false
        };

        _logger.LogInformation("Sending chat request to Ollama model={Model} historyLength={Length}",
            _model, history.Count);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat", request, _jsonOptions, ct);
            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(_jsonOptions, ct);

            var content = chatResponse?.Message?.Content ?? string.Empty;

            _logger.LogInformation("Ollama responded successfully model={Model} responseLength={Length}",
                _model, content.Length);

            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error communicating with Ollama model={Model}", _model);
            throw;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Ollama request timed out model={Model}", _model);
            throw new TimeoutException($"Ollama request timed out after {_httpClient.Timeout.TotalSeconds}s", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tags", ct);
            if (!response.IsSuccessStatusCode)
                return false;

            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(_jsonOptions, ct);
            var available = tags?.Models.Any(m => m.Name.StartsWith(_model, StringComparison.OrdinalIgnoreCase)) == true;

            _logger.LogInformation("Ollama availability check: available={Available} model={Model}", available, _model);
            return available;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama availability check failed");
            return false;
        }
    }
}

/// <summary>Thread-safe in-memory chat session store</summary>
public sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private readonly Dictionary<string, List<ChatMessageDto>> _sessions = new();
    private readonly Lock _lock = new();

    /// <inheritdoc/>
    public void AddMessage(string sessionId, ChatRole role, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var messages))
            {
                messages = new List<ChatMessageDto>();
                _sessions[sessionId] = messages;
            }

            messages.Add(new ChatMessageDto { Role = role, Content = content });
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ChatMessageDto> GetHistory(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var messages)
                ? messages.AsReadOnly()
                : Array.Empty<ChatMessageDto>();
        }
    }

    /// <inheritdoc/>
    public void ClearSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            _sessions.Remove(sessionId);
        }
    }

    /// <inheritdoc/>
    public bool SessionExists(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_lock)
        {
            return _sessions.ContainsKey(sessionId);
        }
    }
}
