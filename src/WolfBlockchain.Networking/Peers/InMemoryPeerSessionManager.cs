using WolfBlockchain.Networking.Abstractions;

namespace WolfBlockchain.Networking.Peers;

public sealed class InMemoryPeerSessionManager : IPeerSessionManager
{
    private readonly object _sync = new();
    private readonly Dictionary<string, PeerIdentity> _connectedPeers = new(StringComparer.Ordinal);

    public ValueTask<bool> TryConnectAsync(PeerIdentity peer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(peer.PeerId) || string.IsNullOrWhiteSpace(peer.Endpoint))
        {
            return ValueTask.FromResult(false);
        }

        lock (_sync)
        {
            if (_connectedPeers.ContainsKey(peer.PeerId))
            {
                return ValueTask.FromResult(false);
            }

            _connectedPeers[peer.PeerId] = peer;
            return ValueTask.FromResult(true);
        }
    }

    public ValueTask DisconnectAsync(string peerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _connectedPeers.Remove(peerId);
        }

        return ValueTask.CompletedTask;
    }

    public IReadOnlyCollection<PeerIdentity> GetConnectedPeers()
    {
        lock (_sync)
        {
            return _connectedPeers.Values.ToArray();
        }
    }
}
