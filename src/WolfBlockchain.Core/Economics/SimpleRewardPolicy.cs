namespace WolfBlockchain.Core.Economics;

public sealed class SimpleRewardPolicy : IRewardPolicy
{
    public IReadOnlyList<RewardAllocation> Distribute(RewardDistributionInput input)
    {
        if (input.BlockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Block height must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(input.ProposerAccountId))
        {
            throw new ArgumentException("Proposer account id is required.", nameof(input));
        }

        if (input.CollectedFees < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Collected fees must be non-negative.");
        }

        return new[]
        {
            new RewardAllocation(input.ProposerAccountId, input.CollectedFees, "block-proposal")
        };
    }
}
