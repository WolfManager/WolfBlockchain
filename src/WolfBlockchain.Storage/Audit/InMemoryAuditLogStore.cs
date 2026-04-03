using WolfBlockchain.Observability.Abstractions;
using WolfBlockchain.Storage.Abstractions;

namespace WolfBlockchain.Storage.Audit;

public sealed class InMemoryAuditLogStore : IAuditLogStore
{
    private readonly object _sync = new();
    private readonly List<AuditLogEntry> _entries = new();

    public ValueTask AppendAsync(AuditEventType eventType, string eventName, IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var category = MapCategory(eventType);
        var entry = new AuditLogEntry(
            DateTimeOffset.UtcNow,
            category,
            eventType,
            eventName,
            metadata,
            ActorId: "system");

        return AppendAsync(entry, cancellationToken);
    }

    public ValueTask AppendAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(entry.EventName))
        {
            throw new ArgumentException("Audit event name is required.", nameof(entry));
        }

        lock (_sync)
        {
            _entries.Add(entry);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (limit <= 0)
        {
            return ValueTask.FromResult<IReadOnlyList<AuditLogEntry>>(Array.Empty<AuditLogEntry>());
        }

        lock (_sync)
        {
            var result = _entries
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(limit)
                .ToArray();

            return ValueTask.FromResult<IReadOnlyList<AuditLogEntry>>(result);
        }
    }

    public ValueTask<IReadOnlyList<AuditLogEntry>> GetByCategoryAsync(AuditCategory category, int limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (limit <= 0)
        {
            return ValueTask.FromResult<IReadOnlyList<AuditLogEntry>>(Array.Empty<AuditLogEntry>());
        }

        lock (_sync)
        {
            var result = _entries
                .Where(x => x.Category == category)
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(limit)
                .ToArray();

            return ValueTask.FromResult<IReadOnlyList<AuditLogEntry>>(result);
        }
    }

    private static AuditCategory MapCategory(AuditEventType eventType)
    {
        return eventType switch
        {
            AuditEventType.Security => AuditCategory.Security,
            AuditEventType.FeeAccounting or AuditEventType.RewardDistribution or AuditEventType.SupplyChange => AuditCategory.Economic,
            _ => AuditCategory.Administrative
        };
    }
}
