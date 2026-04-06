using WolfBlockchain.Wallet.Abstractions;

namespace WolfBlockchain.Wallet.Services;

public sealed class WalletService(IKeyStore keyStore, ISigner signer) : IWalletService
{
    public async ValueTask<byte[]> SignTransactionAsync(string accountId, byte[] transactionPayload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var account = await keyStore.GetAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            throw new InvalidOperationException("Account does not exist.");
        }

        return await signer.SignAsync(accountId, transactionPayload, cancellationToken).ConfigureAwait(false);
    }
}
