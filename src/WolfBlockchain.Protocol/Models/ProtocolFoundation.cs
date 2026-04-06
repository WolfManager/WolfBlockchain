using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.Protocol.Models;

public static class ProtocolVersions
{
    public static readonly ProtocolVersion V1 = new(1, 0);
}

public enum ProtocolErrorCode
{
    None = 0,
    UnsupportedProtocolVersion = 1000,
    InvalidMessageType = 1001,
    InvalidPayload = 1002,
    InvalidBlockHeader = 2000,
    InvalidBlockBody = 2001,
    InvalidTransactionHeader = 3000,
    InvalidTransactionBody = 3001,
    InvalidSignature = 3002
}

public sealed record ProtocolError(
    ProtocolErrorCode Code,
    string Message,
    bool IsRetryable = false);

public sealed record TransactionHeader(
    ProtocolVersion Version,
    string TransactionId,
    DateTimeOffset CreatedAtUtc,
    string SenderAccountId,
    long Nonce,
    string PayloadType);

public sealed record Transaction(
    TransactionHeader Header,
    byte[] Payload,
    byte[] Signature);

public sealed record BlockHeader(
    ProtocolVersion Version,
    long Height,
    string BlockHash,
    string PreviousBlockHash,
    DateTimeOffset ProposedAtUtc,
    string ProposerId,
    string TransactionsRootHash,
    string StateRootHash);

public sealed record Block(
    BlockHeader Header,
    IReadOnlyList<Transaction> Transactions);

public enum ProtocolMessageType
{
    Unknown = 0,
    Handshake = 1,
    Ping = 2,
    NewTransaction = 3,
    NewBlockProposal = 4,
    BlockVote = 5,
    BlockCommit = 6
}

public sealed record ProtocolMessageHeader(
    ProtocolVersion Version,
    ProtocolMessageType MessageType,
    string CorrelationId,
    DateTimeOffset SentAtUtc,
    string SourcePeerId);

public sealed record ProtocolMessage(
    ProtocolMessageHeader Header,
    byte[] Payload);
