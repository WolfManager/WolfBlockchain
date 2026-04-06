using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Core.Engine;
using WolfBlockchain.Core.Mempool;
using WolfBlockchain.Core.State;
using WolfBlockchain.Core.Validation;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Core.UnitTests;

public class DeterministicCoreBehaviorTests
{
    [Fact]
    public void BlockValidatorReturnsSameResultForSameInput()
    {
        var txValidator = new DeterministicTransactionValidator();
        var blockValidator = new DeterministicBlockValidator(txValidator);
        var block = CreateValidBlock();

        var first = blockValidator.Validate(block);
        var second = blockValidator.Validate(block);

        Assert.Equal(first.IsValid, second.IsValid);
        Assert.Equal(first.ErrorCode, second.ErrorCode);
    }

    [Fact]
    public void MempoolRejectsDuplicateTransaction()
    {
        var txValidator = new DeterministicTransactionValidator();
        var mempool = new SafeMempoolService(txValidator, 100);
        var tx = CreateValidTransaction("tx-dup");

        var first = mempool.TryAcceptTransaction(tx);
        var second = mempool.TryAcceptTransaction(tx);

        Assert.True(first.IsValid);
        Assert.False(second.IsValid);
        Assert.Equal(CoreErrorCodes.MempoolDuplicateTransaction, second.ErrorCode);
    }

    [Fact]
    public void BlockchainEngineAcceptsSequentialBlock()
    {
        var txValidator = new DeterministicTransactionValidator();
        var blockValidator = new DeterministicBlockValidator(txValidator);
        var transition = new DeterministicStateTransitionExecutor();
        var updater = new InMemoryStateUpdater();
        var engine = new DeterministicBlockchainEngine(blockValidator, transition, updater);

        var accepted = engine.TryAcceptBlock(CreateValidBlock());

        Assert.True(accepted.IsValid);
    }

    private static BlockEnvelope CreateValidBlock()
    {
        return new BlockEnvelope(
            new ProtocolVersion(1, 0),
            0,
            "block-0",
            string.Empty,
            new[] { CreateValidTransaction("tx-1") },
            DateTimeOffset.UtcNow);
    }

    private static TransactionEnvelope CreateValidTransaction(string id)
    {
        return new TransactionEnvelope(
            new ProtocolVersion(1, 0),
            id,
            "transfer",
            new byte[] { 0x01 },
            new byte[] { 0xAA });
    }
}
