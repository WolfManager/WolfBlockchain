using WolfBlockchain.Agents.Abstractions;

namespace WolfBlockchain.Agents.Providers;

public sealed class NullProviderAdapter : IProviderAdapter
{
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

        return ValueTask.FromResult("provider-not-configured");
    }
}
