using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.UnitTests;

public class ValidationAndStateContractsTests
{
    [Fact]
    public void TransactionEnvelope_ShouldKeepVersionedDataExplicit()
    {
        var tx = new TransactionEnvelope(
            new ProtocolVersion(1, 0),
            "tx-1",
            "transfer",
            new byte[] { 0x01 },
            new byte[] { 0xAB });

        Assert.Equal("tx-1", tx.TransactionId);
        Assert.Equal("transfer", tx.PayloadType);
        Assert.Equal(1, tx.Version.Major);
    }

    [Fact]
    public void ValidatorContracts_ShouldExposeDeterministicSignatures()
    {
        var txValidate = typeof(ITransactionValidator).GetMethod(nameof(ITransactionValidator.Validate));
        var blockValidate = typeof(IBlockValidator).GetMethod(nameof(IBlockValidator.Validate));

        Assert.NotNull(txValidate);
        Assert.NotNull(blockValidate);
        Assert.Equal(typeof(ValidationResult), txValidate!.ReturnType);
        Assert.Equal(typeof(ValidationResult), blockValidate!.ReturnType);
    }

    [Fact]
    public void StateTransitionContext_ShouldBeExplicit()
    {
        var context = new StateTransitionContext("block-hash", 42, DateTimeOffset.UtcNow);

        Assert.Equal("block-hash", context.BlockHash);
        Assert.Equal(42, context.Height);
    }
}
