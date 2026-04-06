using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Consensus.Messaging;

public sealed class DefaultConsensusMessageHandler : IConsensusMessageHandler
{
    private const int MaxMessageTypeLength = 64;
    private const int MaxPayloadBytes = 128 * 1024;

    public ValueTask<ValidationResult> HandleAsync(PeerMessageEnvelope message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message.Version.Major <= 0)
        {
            return ValueTask.FromResult(new ValidationResult(false, ConsensusErrorCodes.MessageInvalidVersion, "Unsupported message version."));
        }

        if (string.IsNullOrWhiteSpace(message.MessageType))
        {
            return ValueTask.FromResult(new ValidationResult(false, ConsensusErrorCodes.MessageTypeRequired, "Message type is required."));
        }

        if (message.MessageType.Length > MaxMessageTypeLength)
        {
            return ValueTask.FromResult(new ValidationResult(false, ConsensusErrorCodes.MessageTypeTooLarge, $"Message type exceeds maximum length of {MaxMessageTypeLength} characters."));
        }

        if (message.Payload is null || message.Payload.Length == 0)
        {
            return ValueTask.FromResult(new ValidationResult(false, ConsensusErrorCodes.MessagePayloadRequired, "Message payload is required."));
        }

        if (message.Payload.Length > MaxPayloadBytes)
        {
            return ValueTask.FromResult(new ValidationResult(false, ConsensusErrorCodes.MessagePayloadTooLarge, $"Message payload exceeds maximum size of {MaxPayloadBytes} bytes."));
        }

        return ValueTask.FromResult(new ValidationResult(true));
    }
}
