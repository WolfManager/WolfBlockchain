using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace WolfBlockchain.API.Services;

// ============= OPTIONS =============

/// <summary>Configuration options for the Ollama local AI provider.</summary>
public sealed class OllamaOptions
{
    /// <summary>Base URL of the Ollama API server. Defaults to http://localhost:11434.</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Default model name to use when none is specified.</summary>
    public string DefaultModel { get; set; } = "llama3";

    /// <summary>HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}

// ============= DTOs =============

/// <summary>Request payload for text generation.</summary>
public sealed record OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;
}

/// <summary>Response from the Ollama generate endpoint.</summary>
public sealed record OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; init; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("total_duration")]
    public long TotalDurationNs { get; init; }
}

/// <summary>A single message in a chat conversation.</summary>
public sealed record OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}

/// <summary>Request payload for chat completion.</summary>
public sealed record OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("messages")]
    public IList<OllamaChatMessage> Messages { get; init; } = new List<OllamaChatMessage>();

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;
}

/// <summary>Response from the Ollama chat endpoint.</summary>
public sealed record OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("total_duration")]
    public long TotalDurationNs { get; init; }
}

/// <summary>Metadata about a locally available Ollama model.</summary>
public sealed record OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }
}

/// <summary>List of locally available models returned by /api/tags.</summary>
internal sealed record OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public IList<OllamaModelInfo> Models { get; init; } = new List<OllamaModelInfo>();
}

// ============= INTERFACE =============

/// <summary>Service for interacting with a local Ollama AI inference server.</summary>
public interface IOllamaService
{
    /// <summary>Generate a text completion for the given prompt.</summary>
    Task<OllamaGenerateResponse> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default);

    /// <summary>Send a chat request and receive a response.</summary>
    Task<OllamaChatResponse> ChatAsync(IList<OllamaChatMessage> messages, string? model = null, CancellationToken cancellationToken = default);

    /// <summary>List all models available on the local Ollama server.</summary>
    Task<IList<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>Check whether the Ollama server is reachable.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

// ============= IMPLEMENTATION =============

/// <summary>
/// Communicates with a locally running Ollama server using its HTTP REST API.
/// Configure the server address and default model via <see cref="OllamaOptions"/>.
/// </summary>
public sealed class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OllamaService(HttpClient httpClient, IOptions<OllamaOptions> options, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OllamaGenerateResponse> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var effectiveModel = ResolveModel(model);
        _logger.LogInformation("Ollama generate: model={Model}, promptLength={Length}", SanitizeForLog(effectiveModel), prompt.Length);

        var request = new OllamaGenerateRequest
        {
            Model = effectiveModel,
            Prompt = prompt,
            Stream = false
        };

        using var response = await _httpClient
            .PostAsJsonAsync("api/generate", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaGenerateResponse>(_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Ollama returned an empty generate response.");
    }

    /// <inheritdoc/>
    public async Task<OllamaChatResponse> ChatAsync(IList<OllamaChatMessage> messages, string? model = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(messages));

        var effectiveModel = ResolveModel(model);
        _logger.LogInformation("Ollama chat: model={Model}, messages={Count}", SanitizeForLog(effectiveModel), messages.Count);

        var request = new OllamaChatRequest
        {
            Model = effectiveModel,
            Messages = messages,
            Stream = false
        };

        using var response = await _httpClient
            .PostAsJsonAsync("api/chat", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaChatResponse>(_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Ollama returned an empty chat response.");
    }

    /// <inheritdoc/>
    public async Task<IList<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ollama: listing available models");

        using var response = await _httpClient
            .GetAsync("api/tags", cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaTagsResponse>(_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return result?.Models ?? new List<OllamaModelInfo>();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient
                .GetAsync("api/tags", cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning("Ollama server is not available at {BaseUrl}: {Error}", _options.BaseUrl, ex.Message);
            return false;
        }
    }

    private string ResolveModel(string? model) =>
        !string.IsNullOrWhiteSpace(model) ? model : _options.DefaultModel;

    /// <summary>Strips all ASCII control characters (0–31 and 127) to prevent log-injection attacks.</summary>
    private static string SanitizeForLog(string value) =>
        string.Concat(value.Select(c => c < 0x20 || c == 0x7F ? ' ' : c));
}
