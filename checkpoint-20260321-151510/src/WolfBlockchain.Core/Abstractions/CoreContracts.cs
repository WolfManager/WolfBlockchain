using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.Abstractions;

public sealed record ValidationResult(bool IsValid, string? ErrorCode = null, string? ErrorMessage = null);

public sealed record StateTransitionContext(string BlockHash, long Height, DateTimeOffset TimestampUtc);

public sealed record BlockCommitResult(bool Committed, string? BlockHash = null, long? Height = null, string? ErrorCode = null, string? ErrorMessage = null);

public interface ITransactionValidator
{
    ValidationResult Validate(TransactionEnvelope transaction);
}

public interface IBlockValidator
{
    ValidationResult Validate(BlockEnvelope block);
}

public interface IStateTransitionExecutor
{
    ValidationResult Execute(BlockEnvelope block, StateTransitionContext context);
}

public interface IStateUpdater
{
    ValidationResult Apply(BlockEnvelope block, StateTransitionContext context);
}

public interface IBlockchainEngine
{
    ValidationResult TryAcceptBlock(BlockEnvelope block);
}

public interface IMempoolService
{
    ValidationResult TryAcceptTransaction(TransactionEnvelope transaction);
    IReadOnlyList<TransactionEnvelope> GetPendingTransactions(int limit);
}

public interface ISingleNodeBlockCommitOrchestrator
{
    ValueTask<BlockCommitResult> TryCommitPendingBlockAsync(CancellationToken cancellationToken);
}
