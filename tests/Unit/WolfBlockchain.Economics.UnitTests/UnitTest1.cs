using WolfBlockchain.Core.Economics;

namespace WolfBlockchain.Economics.UnitTests;

public class EconomicsContractsTests
{
    [Fact]
    public void FeeCalculationResult_ShouldKeepAllFeeComponentsExplicit()
    {
        var result = new FeeCalculationResult(BaseFee: 1.5m, PriorityFee: 0.5m, TotalFee: 2.0m);

        Assert.Equal(1.5m, result.BaseFee);
        Assert.Equal(0.5m, result.PriorityFee);
        Assert.Equal(2.0m, result.TotalFee);
    }

    [Fact]
    public void RewardAllocation_ShouldRequireReasonForAuditability()
    {
        var allocation = new RewardAllocation("validator-1", 10m, "block-proposal");

        Assert.Equal("validator-1", allocation.AccountId);
        Assert.Equal("block-proposal", allocation.Reason);
    }

    [Fact]
    public void SupplyPolicy_ShouldExposeIssuanceAndBurnContracts()
    {
        var supplyPolicyType = typeof(ISupplyPolicy);

        Assert.NotNull(supplyPolicyType.GetMethod(nameof(ISupplyPolicy.GetCurrentSupply)));
        Assert.NotNull(supplyPolicyType.GetMethod(nameof(ISupplyPolicy.ApplyIssuance)));
        Assert.NotNull(supplyPolicyType.GetMethod(nameof(ISupplyPolicy.ApplyBurn)));
    }
}
