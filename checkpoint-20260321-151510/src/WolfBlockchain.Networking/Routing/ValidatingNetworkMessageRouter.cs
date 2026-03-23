using WolfBlockchain.Networking.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Networking.Routing;

public sealed class ValidatingNetworkMessageRouter(
    IExternalMessageValidator validator,
    IReadOnlyDictionary<string, INetworkMessageHandler> handlers) : INetworkMessageRouter
{
    private readonly Dictionary<string, INetworkMessageHandler> _handlers =
        handlers.ToDictionary(pair => pair.Key.Trim(), pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    public ValueTask RouteAsync(PeerMessageEnvelope message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!validator.IsValid(message, out _))
        {
            return ValueTask.CompletedTask;
        }

        var normalizedType = message.MessageType.Trim();
        if (!_handlers.TryGetValue(normalizedType, out var handler))
        {
            return ValueTask.CompletedTask;
        }

        return handler.HandleAsync(message, cancellationToken);
    }
}
