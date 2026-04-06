using WolfBlockchain.Protocol.Abstractions;
using WolfBlockchain.Storage.Abstractions;

namespace WolfBlockchain.Storage.Canonical;

public sealed class InMemoryBlockStore : IBlockStore, IChainReadRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, BlockEnvelope> _blocksByHash = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownTransactionIds = new(StringComparer.Ordinal);
    private long _currentHeight = -1;
    private string? _lastBlockHash;

    public ValueTask SaveBlockAsync(BlockEnvelope block, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(block.BlockHash))
        {
            throw new ArgumentException("Block hash is required.", nameof(block));
        }

        lock (_sync)
        {
            _blocksByHash[block.BlockHash] = block;

            foreach (var tx in block.Transactions)
            {
                _knownTransactionIds.Add(tx.TransactionId);
            }

            if (block.Height > _currentHeight)
            {
                _currentHeight = block.Height;
                _lastBlockHash = block.BlockHash;
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<BlockEnvelope?> GetBlockByHashAsync(string blockHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(blockHash))
        {
            throw new ArgumentException("Block hash is required.", nameof(blockHash));
        }

        lock (_sync)
        {
            _blocksByHash.TryGetValue(blockHash, out var block);
            return ValueTask.FromResult(block);
        }
    }

    public ValueTask<long> GetCurrentHeightAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return ValueTask.FromResult(_currentHeight);
        }
    }

    public ValueTask<string?> GetLastBlockHashAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return ValueTask.FromResult(_lastBlockHash);
        }
    }

    public ValueTask<bool> HasTransactionAsync(string transactionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        }

        lock (_sync)
        {
            return ValueTask.FromResult(_knownTransactionIds.Contains(transactionId));
        }
    }
}
