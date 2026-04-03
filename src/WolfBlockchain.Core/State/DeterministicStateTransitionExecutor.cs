using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.State;

public sealed class DeterministicStateTransitionExecutor : IStateTransitionExecutor
{
    public ValidationResult Execute(BlockEnvelope block, StateTransitionContext context)
    {
        if (!string.Equals(block.BlockHash, context.BlockHash, StringComparison.Ordinal))
        {
            return new ValidationResult(false, CoreErrorCodes.StateBlockHashMismatch, "State transition context does not match block hash.");
        }

        if (block.Height != context.Height)
        {
            return new ValidationResult(false, CoreErrorCodes.StateHeightMismatch, "State transition context does not match block height.");
        }

        if (context.TimestampUtc == default)
        {
            return new ValidationResult(false, CoreErrorCodes.StateInvalidTimestamp, "State transition timestamp is required.");
        }

        return new ValidationResult(true);
    }
}
