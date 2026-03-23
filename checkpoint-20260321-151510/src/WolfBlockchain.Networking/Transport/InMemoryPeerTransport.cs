using System.Threading.Channels;
using WolfBlockchain.Networking.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Networking.Transport;

public sealed class InMemoryPeerTransport : IPeerTransport
{
    private readonly Channel<PeerMessageEnvelope> _inbound = Channel.CreateUnbounded<PeerMessageEnvelope>();

    public ValueTask SendAsync(string peerId, PeerMessageEnvelope message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(peerId, message.SourcePeerId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Peer id mismatch between session and message source.");
        }

        return _inbound.Writer.WriteAsync(message, cancellationToken);
    }

    public async IAsyncEnumerable<PeerMessageEnvelope> ReceiveAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _inbound.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_inbound.Reader.TryRead(out var message))
            {
                yield return message;
            }
        }
    }
}
