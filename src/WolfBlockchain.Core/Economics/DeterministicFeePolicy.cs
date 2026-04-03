namespace WolfBlockchain.Core.Economics;

public sealed class DeterministicFeePolicy(
    decimal baseFeePerTransaction = 0.01m,
    decimal payloadFeePerKilobyte = 0.001m) : IFeePolicy
{
    public FeeCalculationResult CalculateFee(FeeCalculationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.TransactionType))
        {
            throw new ArgumentException("Transaction type is required.", nameof(input));
        }

        if (input.PayloadSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Payload size must be non-negative.");
        }

        if (input.Amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Amount must be non-negative.");
        }

        var payloadUnits = input.PayloadSizeBytes / 1024m;
        var baseFee = decimal.Round(baseFeePerTransaction + (payloadUnits * payloadFeePerKilobyte), 8, MidpointRounding.ToZero);
        var priorityFee = 0m;
        var totalFee = baseFee + priorityFee;

        return new FeeCalculationResult(baseFee, priorityFee, totalFee);
    }
}
