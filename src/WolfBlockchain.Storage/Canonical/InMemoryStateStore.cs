using WolfBlockchain.Storage.Abstractions;

namespace WolfBlockchain.Storage.Canonical;

public sealed class InMemoryStateStore : IStateStore
{
    private readonly object _sync = new();
    private readonly Dictionary<long, string> _stateRootByHeight = new();

    public ValueTask SaveStateRootAsync(long height, string stateRootHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(stateRootHash))
        {
            throw new ArgumentException("State root hash is required.", nameof(stateRootHash));
        }

        lock (_sync)
        {
            _stateRootByHeight[height] = stateRootHash;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> GetStateRootAsync(long height, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");
        }

        lock (_sync)
        {
            _stateRootByHeight.TryGetValue(height, out var stateRootHash);
            return ValueTask.FromResult(stateRootHash);
        }
    }
}
