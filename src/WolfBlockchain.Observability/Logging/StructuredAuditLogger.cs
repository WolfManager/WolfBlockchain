using WolfBlockchain.Observability.Abstractions;

namespace WolfBlockchain.Observability.Logging;

public sealed class StructuredAuditLogger : IAuditLogger
{
    private readonly object _sync = new();
    private readonly List<string> _events = new();

    public void Log(AuditEventType eventType, string eventName, IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var serializedMetadata = string.Join(",", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var line = $"{DateTimeOffset.UtcNow:O}|{eventType}|{eventName}|{serializedMetadata}";

        lock (_sync)
        {
            _events.Add(line);
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
            return _events.TakeLast(limit).ToArray();
        }
    }
}
