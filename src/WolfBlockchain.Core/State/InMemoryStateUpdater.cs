using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.State;

public sealed class InMemoryStateUpdater : IStateUpdater
{
    private readonly object _sync = new();
    private long _currentHeight = -1;
    private string? _lastBlockHash;

    public ValidationResult Apply(BlockEnvelope block, StateTransitionContext context)
    {
        lock (_sync)
        {
            if (_currentHeight >= 0 && context.Height != _currentHeight + 1)
            {
                return new ValidationResult(false, CoreErrorCodes.StateNonSequentialHeight, "Block height must be sequential.");
            }

            if (_currentHeight >= 0 && !string.Equals(block.PreviousBlockHash, _lastBlockHash, StringComparison.Ordinal))
            {
                return new ValidationResult(false, CoreErrorCodes.StatePreviousHashMismatch, "Previous block hash does not match current chain tip.");
            }

            _currentHeight = context.Height;
            _lastBlockHash = context.BlockHash;

            return new ValidationResult(true);
        }
    }
}
