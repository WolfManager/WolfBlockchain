using Microsoft.Extensions.Logging;
using WolfBlockchain.Agents.Abstractions;

namespace WolfBlockchain.Agents.Logging;

/// <summary>Structured audit logger that writes to the standard .NET logging pipeline.</summary>
public sealed class StructuredAuditLogger(ILogger<StructuredAuditLogger> logger) : IAuditLogger
{
    public void Log(AuditEventType eventType, string eventName, IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var serializedMetadata = string.Join(", ", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        logger.LogInformation("[Audit] {EventType} | {EventName} | {Metadata}", eventType, eventName, serializedMetadata);
    }
}
