using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Consensus.Proposal;

public sealed class DeterministicBlockProposalFactory(IMempoolService mempoolService) : IBlockProposalFactory
{
    public ValueTask<BlockEnvelope> CreateProposalAsync(ConsensusRoundContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var transactions = mempoolService.GetPendingTransactions(1_000);
        var blockHash = $"block-{context.Height}-{context.Round}";
        var previousHash = context.Height == 0 ? string.Empty : $"block-{context.Height - 1}-{context.Round}";

        var proposal = new BlockEnvelope(
            new ProtocolVersion(1, 0),
            context.Height,
            blockHash,
            previousHash,
            transactions,
            context.StartedAtUtc);

        return ValueTask.FromResult(proposal);
    }
}
