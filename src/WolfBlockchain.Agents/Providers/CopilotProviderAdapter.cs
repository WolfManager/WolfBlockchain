using WolfBlockchain.Agents.Abstractions;

namespace WolfBlockchain.Agents.Providers;

/// <summary>
/// Copilot Coding Agent provider - a development tool for code generation, analysis,
/// debugging and architectural decisions. Completely independent of Ollama.
/// </summary>
public sealed class CopilotProviderAdapter : IProviderAdapter
{
    public string ProviderName => "copilot-coding-agent";

    public ValueTask<string> GenerateAsync(string promptVersion, string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(promptVersion))
        {
            throw new ArgumentException("Prompt version is required.", nameof(promptVersion));
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        var response = RespondToPrompt(promptVersion, prompt);
        return ValueTask.FromResult(response);
    }

    private static string RespondToPrompt(string promptVersion, string prompt)
    {
        var lowerPrompt = prompt.ToLowerInvariant();

        if (lowerPrompt.Contains("generate") || lowerPrompt.Contains("create") || lowerPrompt.Contains("write"))
        {
            return $"[CopilotAgent v{promptVersion}] Code generation: Analyzing requirements and generating implementation...";
        }

        if (lowerPrompt.Contains("analyz") || lowerPrompt.Contains("review") || lowerPrompt.Contains("inspect"))
        {
            return $"[CopilotAgent v{promptVersion}] Code analysis: Examining code structure, patterns and quality...";
        }

        if (lowerPrompt.Contains("debug") || lowerPrompt.Contains("fix") || lowerPrompt.Contains("error") || lowerPrompt.Contains("bug"))
        {
            return $"[CopilotAgent v{promptVersion}] Debugging: Tracing execution path and identifying root cause...";
        }

        if (lowerPrompt.Contains("architect") || lowerPrompt.Contains("design") || lowerPrompt.Contains("structur"))
        {
            return $"[CopilotAgent v{promptVersion}] Architecture: Evaluating design patterns and recommending structure...";
        }

        return $"[CopilotAgent v{promptVersion}] Processing: {prompt}";
    }
}
