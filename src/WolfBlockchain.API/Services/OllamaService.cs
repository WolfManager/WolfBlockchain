using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WolfBlockchain.API.Validation;

namespace WolfBlockchain.API.Services;

/// <summary>Chat message sent by the user or assistant.</summary>
public record ChatMessage
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>Request to send a chat message.</summary>
public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public string? Model { get; init; }
}

/// <summary>Response from the chat service.</summary>
public record ChatResponse
{
    public string Reply { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public long LatencyMs { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>Chat provider options for technical analysis.</summary>
public record ChatProviderInfo
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string EstimatedSetupTime { get; init; } = string.Empty;
    public bool RequiresApiKey { get; init; }
    public bool RunsLocally { get; init; }
    public string[] SupportedModels { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Chat service abstraction supporting multiple AI provider backends.
/// Option A: Ollama (local, open-source) — 3-5 days
/// Option B: OpenAI API (cloud, paid)    — 5-7 days
/// Option C: Mock/Stub (for testing)     — immediate
/// </summary>
public interface IChatService
{
    /// <summary>Send a chat message and receive a reply.</summary>
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>List available models for this provider.</summary>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Provider name (e.g. "Ollama", "OpenAI", "Mock").</summary>
    string ProviderName { get; }

    /// <summary>Whether the provider is reachable.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

// ============= OPTION A: OLLAMA (LOCAL AI) =============

/// <summary>
/// Ollama service — Option A for local, offline AI integration.
/// Connects to a locally running Ollama instance at the configured base URL.
/// </summary>
public class OllamaService : IChatService
{
    private const string DefaultModel = "llama3";

    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly IInputSanitizer _sanitizer;
    private readonly string _defaultModel;

    public string ProviderName => "Ollama";

    public OllamaService(
        HttpClient httpClient,
        ILogger<OllamaService> logger,
        IInputSanitizer sanitizer,
        IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        _defaultModel = configuration["Ollama:DefaultModel"] ?? DefaultModel;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sanitizedMessage = _sanitizer.SanitizeString(request.Message, maxLength: 2000);
        if (string.IsNullOrWhiteSpace(sanitizedMessage))
            throw new ArgumentException("Message cannot be empty after sanitization.", nameof(request));

        var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : _sanitizer.SanitizeString(request.Model, maxLength: 100);
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString()
            : _sanitizer.SanitizeString(request.SessionId, maxLength: 64);

        var safeMessageForLog = SanitizeForLog(sanitizedMessage);
        _logger.LogInformation("Sending chat message to Ollama model {Model} (session {SessionId}): {Message}",
            model, sessionId, safeMessageForLog);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var ollamaRequest = new OllamaGenerateRequest
        {
            Model = model,
            Prompt = sanitizedMessage,
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/generate", ollamaRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Received null response from Ollama.");

        sw.Stop();

        _logger.LogInformation("Ollama responded in {LatencyMs}ms", sw.ElapsedMilliseconds);

        return new ChatResponse
        {
            Reply = ollamaResponse.Response,
            SessionId = sessionId,
            Provider = ProviderName,
            Model = model,
            LatencyMs = sw.ElapsedMilliseconds,
            TimestampUtc = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
        response.EnsureSuccessStatusCode();

        var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken: cancellationToken);
        return tagsResponse?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama is not available");
            return false;
        }
    }

    /// <summary>Strips ASCII control characters from a value before writing to logs.</summary>
    private static string SanitizeForLog(string value) =>
        string.Concat(value.Select(c => c < 0x20 || c == 0x7F ? ' ' : c));

    // ============= PRIVATE DTOs =============

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo> Models { get; set; } = new();
    }

    private sealed class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

// ============= OPTION C: MOCK/STUB (TESTING & DEVELOPMENT) =============

/// <summary>
/// Mock chat service — Option C for development, testing, and CI environments
/// where a real LLM backend is not available.
/// </summary>
public class MockChatService : IChatService
{
    private readonly ILogger<MockChatService> _logger;

    public string ProviderName => "Mock";

    public MockChatService(ILogger<MockChatService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("MockChatService: echoing message (no real LLM backend configured)");

        return Task.FromResult(new ChatResponse
        {
            Reply = $"[Mock] Received: {request.Message}",
            SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
            Provider = ProviderName,
            Model = "mock",
            LatencyMs = 0,
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(new[] { "mock" });

    /// <inheritdoc/>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

// ============= PROVIDER REGISTRY =============

/// <summary>
/// Provides metadata about all available chat providers for the technical analysis endpoint.
/// </summary>
public static class ChatProviderRegistry
{
    /// <summary>Returns info about all supported chat provider options.</summary>
    public static IReadOnlyList<ChatProviderInfo> GetAllOptions() => new List<ChatProviderInfo>
    {
        new()
        {
            Name = "Ollama",
            Description = "Local, open-source LLM runtime. Runs entirely on-premise — no data leaves the server. Supports Llama 3, Mistral, Phi-3 and more.",
            EstimatedSetupTime = "3-5 days",
            RequiresApiKey = false,
            RunsLocally = true,
            SupportedModels = new[] { "llama3", "mistral", "phi3", "gemma2" }
        },
        new()
        {
            Name = "OpenAI",
            Description = "Cloud-based GPT-4o / GPT-4 Turbo API. High quality, requires an API key and sends data to OpenAI servers.",
            EstimatedSetupTime = "5-7 days",
            RequiresApiKey = true,
            RunsLocally = false,
            SupportedModels = new[] { "gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo" }
        },
        new()
        {
            Name = "Mock",
            Description = "No-op stub used in development and CI environments. Returns an echo of the input — no external dependencies.",
            EstimatedSetupTime = "immediate",
            RequiresApiKey = false,
            RunsLocally = true,
            SupportedModels = new[] { "mock" }
        }
    };
}
