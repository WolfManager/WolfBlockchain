using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.Engine;

public sealed class DeterministicBlockchainEngine(
    IBlockValidator blockValidator,
    IStateTransitionExecutor stateTransitionExecutor,
    IStateUpdater stateUpdater) : IBlockchainEngine
{
    public ValidationResult TryAcceptBlock(BlockEnvelope block)
    {
        var blockValidation = blockValidator.Validate(block);
        if (!blockValidation.IsValid)
        {
            return blockValidation;
        }

        var transitionContext = new StateTransitionContext(
            block.BlockHash,
            block.Height,
            block.ProposedAtUtc);

        var executionResult = stateTransitionExecutor.Execute(block, transitionContext);
        if (!executionResult.IsValid)
        {
            return executionResult;
        }

        return stateUpdater.Apply(block, transitionContext);
    }
}
