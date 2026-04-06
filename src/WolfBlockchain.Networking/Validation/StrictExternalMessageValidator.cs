using WolfBlockchain.Networking.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Networking.Validation;

public sealed class StrictExternalMessageValidator : IExternalMessageValidator
{
    private const int MaxPayloadBytes = 128 * 1024;
    private const int MaxPeerIdLength = 128;
    private const int MaxMessageTypeLength = 64;

    public bool IsValid(PeerMessageEnvelope message, out string? reason)
    {
        if (message.Version.Major <= 0)
        {
            reason = "Invalid protocol version.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.SourcePeerId))
        {
            reason = "Missing source peer id.";
            return false;
        }

        if (message.SourcePeerId.Length > MaxPeerIdLength)
        {
            reason = $"Source peer id exceeds maximum length of {MaxPeerIdLength} characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.MessageType))
        {
            reason = "Missing message type.";
            return false;
        }

        if (message.MessageType.Length > MaxMessageTypeLength)
        {
            reason = $"Message type exceeds maximum length of {MaxMessageTypeLength} characters.";
            return false;
        }

        if (message.Payload is null || message.Payload.Length == 0)
        {
            reason = "Empty message payload.";
            return false;
        }

        if (message.Payload.Length > MaxPayloadBytes)
        {
            reason = $"Payload too large. Maximum supported size is {MaxPayloadBytes} bytes.";
            return false;
        }

        reason = null;
        return true;
    }
}
