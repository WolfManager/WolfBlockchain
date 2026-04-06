using WolfBlockchain.Agents.Abstractions;
using WolfBlockchain.Agents.Memory;

namespace WolfBlockchain.Agents.Services;

/// <summary>
/// Service for the Copilot Coding Agent - a development tool independent of Ollama.
/// Provides code generation, analysis, debugging and architectural decision support.
/// </summary>
public interface ICodingAgentService
{
    /// <summary>Generate code based on the given description.</summary>
    Task<CodingAgentResponse> GenerateCodeAsync(string description, CancellationToken cancellationToken = default);

    /// <summary>Analyze the provided code for quality, patterns and issues.</summary>
    Task<CodingAgentResponse> AnalyzeCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Debug the provided code and suggest a fix for the described issue.</summary>
    Task<CodingAgentResponse> DebugCodeAsync(string code, string issue, CancellationToken cancellationToken = default);

    /// <summary>Provide architectural recommendations for the described system.</summary>
    Task<CodingAgentResponse> AdviseArchitectureAsync(string description, CancellationToken cancellationToken = default);

    /// <summary>Returns the name of the active provider backing this agent.</summary>
    string ProviderName { get; }
}

public sealed record CodingAgentResponse(
    string Content,
    string ProviderName,
    DateTimeOffset Timestamp,
    bool Success);

/// <summary>
/// Implementation of <see cref="ICodingAgentService"/> backed by an <see cref="IProviderAdapter"/>.
/// Completely independent of Ollama.
/// </summary>
public sealed class CopilotCodingAgentService(
    IProviderAdapter provider,
    InMemoryAgentMemoryStore memory) : ICodingAgentService
{
    private const string PromptVersion = "1.0";

    public string ProviderName => provider.ProviderName;

    public async Task<CodingAgentResponse> GenerateCodeAsync(string description, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var prompt = $"generate code: {description}";
        var content = await provider.GenerateAsync(PromptVersion, prompt, cancellationToken).ConfigureAwait(false);
        memory.Save($"last-generate-{DateTimeOffset.UtcNow.Ticks}", content);
        return new CodingAgentResponse(content, provider.ProviderName, DateTimeOffset.UtcNow, true);
    }

    public async Task<CodingAgentResponse> AnalyzeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var prompt = $"analyze code: {code}";
        var content = await provider.GenerateAsync(PromptVersion, prompt, cancellationToken).ConfigureAwait(false);
        memory.Save($"last-analyze-{DateTimeOffset.UtcNow.Ticks}", content);
        return new CodingAgentResponse(content, provider.ProviderName, DateTimeOffset.UtcNow, true);
    }

    public async Task<CodingAgentResponse> DebugCodeAsync(string code, string issue, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(issue);

        var prompt = $"debug error: {issue} in code: {code}";
        var content = await provider.GenerateAsync(PromptVersion, prompt, cancellationToken).ConfigureAwait(false);
        memory.Save($"last-debug-{DateTimeOffset.UtcNow.Ticks}", content);
        return new CodingAgentResponse(content, provider.ProviderName, DateTimeOffset.UtcNow, true);
    }

    public async Task<CodingAgentResponse> AdviseArchitectureAsync(string description, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var prompt = $"architecture design: {description}";
        var content = await provider.GenerateAsync(PromptVersion, prompt, cancellationToken).ConfigureAwait(false);
        memory.Save($"last-architecture-{DateTimeOffset.UtcNow.Ticks}", content);
        return new CodingAgentResponse(content, provider.ProviderName, DateTimeOffset.UtcNow, true);
    }
}
