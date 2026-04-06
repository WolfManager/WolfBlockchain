namespace WolfBlockchain.Core.Economics;

public sealed record FeeCalculationInput(string TransactionType, long PayloadSizeBytes, decimal Amount);

public sealed record FeeCalculationResult(decimal BaseFee, decimal PriorityFee, decimal TotalFee);

public sealed record RewardDistributionInput(long BlockHeight, decimal CollectedFees, string ProposerAccountId);

public sealed record RewardAllocation(string AccountId, decimal Amount, string Reason);

public sealed record SupplySnapshot(decimal TotalSupply, decimal CirculatingSupply, decimal BurnedSupply);

public interface IFeePolicy
{
    FeeCalculationResult CalculateFee(FeeCalculationInput input);
}

public interface IRewardPolicy
{
    IReadOnlyList<RewardAllocation> Distribute(RewardDistributionInput input);
}

public interface ISupplyPolicy
{
    SupplySnapshot GetCurrentSupply();
    SupplySnapshot ApplyIssuance(decimal amount, string reason);
    SupplySnapshot ApplyBurn(decimal amount, string reason);
}
