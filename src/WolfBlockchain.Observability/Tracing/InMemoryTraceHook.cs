using WolfBlockchain.Observability.Abstractions;

namespace WolfBlockchain.Observability.Tracing;

public sealed class InMemoryTraceHook : ITraceHook
{
    private readonly object _sync = new();
    private readonly List<string> _traces = new();

    public void Trace(string component, string operation, IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var serializedMetadata = string.Join(",", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var line = $"{DateTimeOffset.UtcNow:O}|{component}|{operation}|{serializedMetadata}";

        lock (_sync)
        {
            _traces.Add(line);
        }
    }

    public IReadOnlyList<string> GetRecent(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<string>();
        }

        lock (_sync)
        {
            return _traces.TakeLast(limit).ToArray();
        }
    }
}
