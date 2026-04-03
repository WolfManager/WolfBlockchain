using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Storage.Abstractions;

namespace WolfBlockchain.Api.PublicApi;

public sealed class SingleNodeBlockCommitOrchestrator(
    IChainReadRepository chainReadRepository,
    IBlockProposalFactory blockProposalFactory,
    IConsensusEngine consensusEngine,
    IBlockchainEngine blockchainEngine,
    IBlockStore blockStore,
    IStateStore stateStore) : ISingleNodeBlockCommitOrchestrator
{
    public async ValueTask<BlockCommitResult> TryCommitPendingBlockAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentHeight = await chainReadRepository.GetCurrentHeightAsync(cancellationToken).ConfigureAwait(false);
        var nextHeight = currentHeight + 1;

        var context = new ConsensusRoundContext(nextHeight, 0, DateTimeOffset.UtcNow);
        await consensusEngine.StartRoundAsync(context, cancellationToken).ConfigureAwait(false);

        var proposal = await blockProposalFactory.CreateProposalAsync(context, cancellationToken).ConfigureAwait(false);
        if (proposal.Transactions.Count == 0)
        {
            return new BlockCommitResult(false, ErrorCode: ApiErrorCodes.CommitNoTransactions, ErrorMessage: "No pending transactions to commit.");
        }

        var consensusCommitted = await consensusEngine.TryCommitBlockAsync(proposal, cancellationToken).ConfigureAwait(false);
        if (!consensusCommitted)
        {
            return new BlockCommitResult(false, proposal.BlockHash, proposal.Height, ApiErrorCodes.CommitConsensusRejected, "Consensus rejected the block proposal.");
        }

        var accepted = blockchainEngine.TryAcceptBlock(proposal);
        if (!accepted.IsValid)
        {
            return new BlockCommitResult(false, proposal.BlockHash, proposal.Height, accepted.ErrorCode, accepted.ErrorMessage);
        }

        await blockStore.SaveBlockAsync(proposal, cancellationToken).ConfigureAwait(false);
        await stateStore.SaveStateRootAsync(proposal.Height, $"state-{proposal.BlockHash}", cancellationToken).ConfigureAwait(false);

        return new BlockCommitResult(true, proposal.BlockHash, proposal.Height);
    }
}
