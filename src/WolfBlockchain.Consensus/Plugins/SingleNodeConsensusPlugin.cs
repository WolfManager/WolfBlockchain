using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Consensus.Plugins;

public sealed class SingleNodeConsensusPlugin : IConsensusPlugin
{
    public string Name => "single-node";

    public ValidationResult ValidateProposal(BlockEnvelope block, ConsensusRoundContext context)
    {
        if (block.Height != context.Height)
        {
            return new ValidationResult(false, ConsensusErrorCodes.HeightMismatch, "Block height does not match consensus round height.");
        }

        if (context.Round < 0)
        {
            return new ValidationResult(false, ConsensusErrorCodes.InvalidRound, "Consensus round must be non-negative.");
        }

        if (block.ProposedAtUtc == default)
        {
            return new ValidationResult(false, ConsensusErrorCodes.ProposalInvalidTimestamp, "Block proposal timestamp is required.");
        }

        if (block.ProposedAtUtc < context.StartedAtUtc)
        {
            return new ValidationResult(false, ConsensusErrorCodes.ProposalBeforeRoundStart, "Block proposal timestamp cannot be earlier than round start.");
        }

        return new ValidationResult(true);
    }
}
