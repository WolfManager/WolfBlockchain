using Xunit;
using WolfBlockchain.Core;

namespace WolfBlockchain.Tests.Core;

/// <summary>
/// Unit tests for Blockchain.GetHistory - verifies the blockchain history (commit log) feature.
/// </summary>
public class BlockchainHistoryTests
{
    [Fact]
    public void GetHistory_ReturnsGenesisBlock_WhenNoBlocksMined()
    {
        // Arrange
        var blockchain = new Blockchain();

        // Act
        var history = blockchain.GetHistory();

        // Assert
        Assert.Single(history);
        Assert.Equal(0, history[0].Index);
        Assert.Equal("0", history[0].PreviousHash);
        Assert.NotEmpty(history[0].Hash);
    }

    [Fact]
    public void GetHistory_ReturnsAllBlocks_AfterMining()
    {
        // Arrange
        var blockchain = new Blockchain();
        blockchain.AddTransaction(new Transaction("Alice", "Bob", 10m));
        blockchain.MinePendingTransactions("Miner");

        // Act
        var history = blockchain.GetHistory();

        // Assert
        Assert.Equal(2, history.Count);
        Assert.Equal(0, history[0].Index);
        Assert.Equal(1, history[1].Index);
        Assert.Equal(history[0].Hash, history[1].PreviousHash);
    }

    [Fact]
    public void GetHistory_RespectsSkipAndTake()
    {
        // Arrange
        var blockchain = new Blockchain();
        blockchain.AddTransaction(new Transaction("Alice", "Bob", 1m));
        blockchain.MinePendingTransactions("Miner");
        blockchain.AddTransaction(new Transaction("Bob", "Carol", 2m));
        blockchain.MinePendingTransactions("Miner");

        // Act - skip the genesis block, take 1
        var history = blockchain.GetHistory(skip: 1, take: 1);

        // Assert
        Assert.Single(history);
        Assert.Equal(1, history[0].Index);
    }

    [Fact]
    public void GetHistory_ReturnsCorrectTransactionCount()
    {
        // Arrange
        var blockchain = new Blockchain();
        blockchain.AddTransaction(new Transaction("Alice", "Bob", 5m));
        blockchain.AddTransaction(new Transaction("Bob", "Carol", 3m));
        blockchain.MinePendingTransactions("Miner");

        // Act
        var history = blockchain.GetHistory();
        var minedBlock = history[1]; // index 1 is the first mined block

        // Assert - 3 transactions: 2 user + 1 mining reward
        Assert.Equal(3, minedBlock.TransactionCount);
    }

    [Fact]
    public void GetHistory_HashesAreLinked()
    {
        // Arrange
        var blockchain = new Blockchain();
        blockchain.AddTransaction(new Transaction("Alice", "Bob", 10m));
        blockchain.MinePendingTransactions("Miner");

        // Act
        var history = blockchain.GetHistory();

        // Assert - each block's PreviousHash equals the prior block's Hash
        for (int i = 1; i < history.Count; i++)
        {
            Assert.Equal(history[i - 1].Hash, history[i].PreviousHash);
        }
    }

    [Fact]
    public void GetHistory_ReturnsEmpty_WhenSkipExceedsChainLength()
    {
        // Arrange
        var blockchain = new Blockchain();

        // Act
        var history = blockchain.GetHistory(skip: 100);

        // Assert
        Assert.Empty(history);
    }
}
