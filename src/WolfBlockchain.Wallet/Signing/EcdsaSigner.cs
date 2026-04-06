using System.Security.Cryptography;
using WolfBlockchain.Wallet.Abstractions;
using WolfBlockchain.Wallet.Keys;

namespace WolfBlockchain.Wallet.Signing;

public sealed class EcdsaSigner(InMemoryKeyStore keyStore) : ISigner
{
    public ValueTask<byte[]> SignAsync(string accountId, byte[] payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (payload is null || payload.Length == 0)
        {
            throw new ArgumentException("Payload is required.", nameof(payload));
        }

        if (!keyStore.TryGetSigner(accountId, out var signer) || signer is null)
        {
            throw new InvalidOperationException("Unknown account for signing.");
        }

        return ValueTask.FromResult(signer.SignData(payload, HashAlgorithmName.SHA256));
    }

    public ValueTask<bool> VerifyAsync(string publicKey, byte[] payload, byte[] signature, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(publicKey))
        {
            return ValueTask.FromResult(false);
        }

        if (payload is null || payload.Length == 0 || signature is null || signature.Length == 0)
        {
            return ValueTask.FromResult(false);
        }

        using var verifier = ECDsa.Create();
        verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        return ValueTask.FromResult(verifier.VerifyData(payload, signature, HashAlgorithmName.SHA256));
    }
}
