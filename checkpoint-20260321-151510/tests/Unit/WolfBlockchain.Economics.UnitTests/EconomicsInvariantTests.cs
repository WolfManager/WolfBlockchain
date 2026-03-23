using WolfBlockchain.Core.Economics;

namespace WolfBlockchain.Economics.UnitTests;

public class EconomicsInvariantTests
{
    [Fact]
    public void FeePolicyMaintainsFeeSumInvariant()
    {
        var policy = new DeterministicFeePolicy();
        var result = policy.CalculateFee(new FeeCalculationInput("transfer", 2048, 10m));

        Assert.Equal(result.BaseFee + result.PriorityFee, result.TotalFee);
    }

    [Fact]
    public void SupplyPolicyIssuanceAndBurnAreDeterministic()
    {
        var supply = new DeterministicSupplyPolicy(initialTotalSupply: 1_000m, initialCirculatingSupply: 1_000m);

        var afterIssuance = supply.ApplyIssuance(100m, "genesis-adjustment");
        var afterBurn = supply.ApplyBurn(20m, "governance-burn");

        Assert.Equal(1_100m, afterIssuance.TotalSupply);
        Assert.Equal(1_080m, afterBurn.TotalSupply);
        Assert.Equal(20m, afterBurn.BurnedSupply);
    }

    [Fact]
    public void RewardPolicyDistributesCollectedFeesToProposer()
    {
        var policy = new SimpleRewardPolicy();

        var allocations = policy.Distribute(new RewardDistributionInput(10, 5m, "validator-a"));

        Assert.Single(allocations);
        Assert.Equal("validator-a", allocations[0].AccountId);
        Assert.Equal(5m, allocations[0].Amount);
    }
}
