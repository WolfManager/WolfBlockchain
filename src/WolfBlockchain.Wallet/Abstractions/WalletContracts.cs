namespace WolfBlockchain.Wallet.Abstractions;

public sealed record WalletAccount(string AccountId, string PublicKey, string Algorithm);

public interface IKeyStore
{
    ValueTask<WalletAccount> CreateAccountAsync(string algorithm, CancellationToken cancellationToken);
    ValueTask<WalletAccount?> GetAccountAsync(string accountId, CancellationToken cancellationToken);
}

public interface ISigner
{
    ValueTask<byte[]> SignAsync(string accountId, byte[] payload, CancellationToken cancellationToken);
    ValueTask<bool> VerifyAsync(string publicKey, byte[] payload, byte[] signature, CancellationToken cancellationToken);
}

public interface IWalletService
{
    ValueTask<byte[]> SignTransactionAsync(string accountId, byte[] transactionPayload, CancellationToken cancellationToken);
}
