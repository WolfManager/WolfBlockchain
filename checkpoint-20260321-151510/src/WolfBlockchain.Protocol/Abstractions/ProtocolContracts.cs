namespace WolfBlockchain.Protocol.Abstractions;

public readonly record struct ProtocolVersion(int Major, int Minor);

public sealed record TransactionEnvelope(
    ProtocolVersion Version,
    string TransactionId,
    string PayloadType,
    byte[] Payload,
    byte[] Signature);

public sealed record BlockEnvelope(
    ProtocolVersion Version,
    long Height,
    string BlockHash,
    string PreviousBlockHash,
    IReadOnlyList<TransactionEnvelope> Transactions,
    DateTimeOffset ProposedAtUtc);

public sealed record PeerMessageEnvelope(
    ProtocolVersion Version,
    string MessageType,
    byte[] Payload,
    string SourcePeerId,
    DateTimeOffset ReceivedAtUtc);

public interface IProtocolVersionProvider
{
    ProtocolVersion Current { get; }
    bool IsSupported(ProtocolVersion version);
}

public interface IProtocolSerializer
{
    byte[] Serialize<T>(T message);
    T Deserialize<T>(byte[] payload);
}
