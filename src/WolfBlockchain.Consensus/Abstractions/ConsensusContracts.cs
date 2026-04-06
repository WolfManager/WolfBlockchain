using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Consensus.Abstractions;

public sealed record ConsensusRoundContext(long Height, int Round, DateTimeOffset StartedAtUtc);

public interface IConsensusPlugin
{
    string Name { get; }
    ValidationResult ValidateProposal(BlockEnvelope block, ConsensusRoundContext context);
}

public interface IConsensusEngine
{
    ValueTask StartRoundAsync(ConsensusRoundContext context, CancellationToken cancellationToken);
    ValueTask<bool> TryCommitBlockAsync(BlockEnvelope block, CancellationToken cancellationToken);
}

public interface IBlockProposalFactory
{
    ValueTask<BlockEnvelope> CreateProposalAsync(ConsensusRoundContext context, CancellationToken cancellationToken);
}

public interface IConsensusMessageHandler
{
    ValueTask<ValidationResult> HandleAsync(PeerMessageEnvelope message, CancellationToken cancellationToken);
}
