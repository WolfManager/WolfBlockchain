using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Networking.Abstractions;

public sealed record PeerIdentity(string PeerId, string Endpoint, string NetworkVersion);

public interface IExternalMessageValidator
{
    bool IsValid(PeerMessageEnvelope message, out string? reason);
}

public interface INetworkMessageHandler
{
    ValueTask HandleAsync(PeerMessageEnvelope message, CancellationToken cancellationToken);
}

public interface IPeerSessionManager
{
    ValueTask<bool> TryConnectAsync(PeerIdentity peer, CancellationToken cancellationToken);
    ValueTask DisconnectAsync(string peerId, CancellationToken cancellationToken);
    IReadOnlyCollection<PeerIdentity> GetConnectedPeers();
}

public interface IPeerTransport
{
    ValueTask SendAsync(string peerId, PeerMessageEnvelope message, CancellationToken cancellationToken);
    IAsyncEnumerable<PeerMessageEnvelope> ReceiveAsync(CancellationToken cancellationToken);
}

public interface INetworkMessageRouter
{
    ValueTask RouteAsync(PeerMessageEnvelope message, CancellationToken cancellationToken);
}
