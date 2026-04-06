using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;
using WolfBlockchain.Observability.Abstractions;

namespace WolfBlockchain.Storage.Abstractions;

public enum AuditCategory
{
    Security,
    Economic,
    Administrative
}

public sealed record AuditLogEntry(
    DateTimeOffset OccurredAtUtc,
    AuditCategory Category,
    AuditEventType EventType,
    string EventName,
    IReadOnlyDictionary<string, string> Metadata,
    string ActorId);

public interface IBlockStore
{
    ValueTask SaveBlockAsync(BlockEnvelope block, CancellationToken cancellationToken);
    ValueTask<BlockEnvelope?> GetBlockByHashAsync(string blockHash, CancellationToken cancellationToken);
}

public interface IStateStore
{
    ValueTask SaveStateRootAsync(long height, string stateRootHash, CancellationToken cancellationToken);
    ValueTask<string?> GetStateRootAsync(long height, CancellationToken cancellationToken);
}

public interface IAuditLogStore
{
    ValueTask AppendAsync(AuditEventType eventType, string eventName, IReadOnlyDictionary<string, string> metadata, CancellationToken cancellationToken);
    ValueTask AppendAsync(AuditLogEntry entry, CancellationToken cancellationToken);
    ValueTask<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);
    ValueTask<IReadOnlyList<AuditLogEntry>> GetByCategoryAsync(AuditCategory category, int limit, CancellationToken cancellationToken);
}

public interface IChainReadRepository
{
    ValueTask<long> GetCurrentHeightAsync(CancellationToken cancellationToken);
    ValueTask<string?> GetLastBlockHashAsync(CancellationToken cancellationToken);
    ValueTask<bool> HasTransactionAsync(string transactionId, CancellationToken cancellationToken);
}
