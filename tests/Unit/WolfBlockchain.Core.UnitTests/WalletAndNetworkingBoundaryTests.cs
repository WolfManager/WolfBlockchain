using WolfBlockchain.Networking.Validation;
using WolfBlockchain.Protocol.Abstractions;
using WolfBlockchain.Wallet.Keys;
using WolfBlockchain.Wallet.Services;
using WolfBlockchain.Wallet.Signing;

namespace WolfBlockchain.Core.UnitTests;

public class WalletAndNetworkingBoundaryTests
{
    [Fact]
    public async Task WalletSignerDoesNotExposePrivateKeyMaterial()
    {
        var keyStore = new InMemoryKeyStore();
        var account = await keyStore.CreateAccountAsync("ECDSA_P256", CancellationToken.None);
        var signer = new EcdsaSigner(keyStore);
        var walletService = new WalletService(keyStore, signer);

        var signature = await walletService.SignTransactionAsync(account.AccountId, new byte[] { 0x02 }, CancellationToken.None);

        Assert.NotEmpty(signature);
        Assert.NotNull(account.PublicKey);
    }

    [Fact]
    public void ExternalMessageValidatorRejectsMalformedInput()
    {
        var validator = new StrictExternalMessageValidator();
        var message = new PeerMessageEnvelope(new ProtocolVersion(1, 0), string.Empty, Array.Empty<byte>(), string.Empty, DateTimeOffset.UtcNow);

        var isValid = validator.IsValid(message, out var reason);

        Assert.False(isValid);
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void ExternalMessageValidatorRejectsOversizedPayload()
    {
        var validator = new StrictExternalMessageValidator();
        var oversized = new byte[128 * 1024 + 1];
        var message = new PeerMessageEnvelope(new ProtocolVersion(1, 0), "tx", oversized, "peer-1", DateTimeOffset.UtcNow);

        var isValid = validator.IsValid(message, out var reason);

        Assert.False(isValid);
        Assert.Contains("Payload too large", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalMessageValidatorRejectsOversizedPeerId()
    {
        var validator = new StrictExternalMessageValidator();
        var peerId = new string('p', 129);
        var message = new PeerMessageEnvelope(new ProtocolVersion(1, 0), "tx", new byte[] { 0x01 }, peerId, DateTimeOffset.UtcNow);

        var isValid = validator.IsValid(message, out var reason);

        Assert.False(isValid);
        Assert.Contains("Source peer id exceeds maximum length", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalMessageValidatorRejectsOversizedMessageType()
    {
        var validator = new StrictExternalMessageValidator();
        var messageType = new string('t', 65);
        var message = new PeerMessageEnvelope(new ProtocolVersion(1, 0), messageType, new byte[] { 0x01 }, "peer-1", DateTimeOffset.UtcNow);

        var isValid = validator.IsValid(message, out var reason);

        Assert.False(isValid);
        Assert.Contains("Message type exceeds maximum length", reason, StringComparison.Ordinal);
    }
}
