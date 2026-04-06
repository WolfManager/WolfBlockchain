namespace WolfBlockchain.Core.Economics;

public sealed class DeterministicSupplyPolicy(decimal initialTotalSupply, decimal initialCirculatingSupply = 0m) : ISupplyPolicy
{
    private readonly object _sync = new();
    private SupplySnapshot _snapshot = new(initialTotalSupply, initialCirculatingSupply, BurnedSupply: 0m);

    public SupplySnapshot GetCurrentSupply()
    {
        lock (_sync)
        {
            return _snapshot;
        }
    }

    public SupplySnapshot ApplyIssuance(decimal amount, string reason)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Issuance amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Issuance reason is required.", nameof(reason));
        }

        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                TotalSupply = _snapshot.TotalSupply + amount,
                CirculatingSupply = _snapshot.CirculatingSupply + amount
            };

            return _snapshot;
        }
    }

    public SupplySnapshot ApplyBurn(decimal amount, string reason)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Burn amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Burn reason is required.", nameof(reason));
        }

        lock (_sync)
        {
            if (_snapshot.CirculatingSupply < amount)
            {
                throw new InvalidOperationException("Burn amount exceeds circulating supply.");
            }

            _snapshot = _snapshot with
            {
                CirculatingSupply = _snapshot.CirculatingSupply - amount,
                BurnedSupply = _snapshot.BurnedSupply + amount,
                TotalSupply = _snapshot.TotalSupply - amount
            };

            return _snapshot;
        }
    }
}
