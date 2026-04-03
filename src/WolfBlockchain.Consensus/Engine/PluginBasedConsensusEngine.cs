using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Consensus.Engine;

public sealed class PluginBasedConsensusEngine(
    IConsensusPlugin plugin,
    IBlockValidator blockValidator) : IConsensusEngine
{
    private readonly object _sync = new();
    private ConsensusRoundContext? _currentRound;

    public ValueTask StartRoundAsync(ConsensusRoundContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), "Consensus height must be non-negative.");
        }

        if (context.Round < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), "Consensus round must be non-negative.");
        }

        if (context.StartedAtUtc == default)
        {
            throw new ArgumentException("Consensus round start timestamp is required.", nameof(context));
        }

        lock (_sync)
        {
            _currentRound = context;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryCommitBlockAsync(BlockEnvelope block, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ConsensusRoundContext? round;
        lock (_sync)
        {
            round = _currentRound;
        }

        if (round is null)
        {
            return ValueTask.FromResult(false);
        }

        var blockValidation = blockValidator.Validate(block);
        if (!blockValidation.IsValid)
        {
            return ValueTask.FromResult(false);
        }

        var consensusValidation = plugin.ValidateProposal(block, round);
        if (!consensusValidation.IsValid)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }
}
