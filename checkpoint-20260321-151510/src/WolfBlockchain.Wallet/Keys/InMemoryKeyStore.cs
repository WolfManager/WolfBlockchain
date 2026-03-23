using System.Security.Cryptography;
using WolfBlockchain.Wallet.Abstractions;

namespace WolfBlockchain.Wallet.Keys;

public sealed class InMemoryKeyStore : IKeyStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, WalletAccount> _accounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ECDsa> _privateKeys = new(StringComparer.Ordinal);

    public ValueTask<WalletAccount> CreateAccountAsync(string algorithm, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(algorithm, "ECDSA_P256", StringComparison.Ordinal))
        {
            throw new NotSupportedException("Only ECDSA_P256 is supported in the minimal foundation.");
        }

        var accountId = Guid.NewGuid().ToString("N");
        var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo());
        var account = new WalletAccount(accountId, publicKey, algorithm);

        lock (_sync)
        {
            _accounts[accountId] = account;
            _privateKeys[accountId] = signer;
        }

        return ValueTask.FromResult(account);
    }

    public ValueTask<WalletAccount?> GetAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _accounts.TryGetValue(accountId, out var account);
            return ValueTask.FromResult(account);
        }
    }

    internal bool TryGetSigner(string accountId, out ECDsa? signer)
    {
        lock (_sync)
        {
            return _privateKeys.TryGetValue(accountId, out signer);
        }
    }
}
