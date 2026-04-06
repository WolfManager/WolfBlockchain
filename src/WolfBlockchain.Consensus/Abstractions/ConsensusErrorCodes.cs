namespace WolfBlockchain.Consensus.Abstractions;

public static class ConsensusErrorCodes
{
    public const string HeightMismatch = "CONSENSUS_HEIGHT_MISMATCH";
    public const string InvalidRound = "CONSENSUS_INVALID_ROUND";
    public const string ProposalInvalidTimestamp = "CONSENSUS_PROPOSAL_INVALID_TIMESTAMP";
    public const string ProposalBeforeRoundStart = "CONSENSUS_PROPOSAL_BEFORE_ROUND_START";

    public const string MessageInvalidVersion = "CONSENSUS_MESSAGE_INVALID_VERSION";
    public const string MessageTypeRequired = "CONSENSUS_MESSAGE_TYPE_REQUIRED";
    public const string MessageTypeTooLarge = "CONSENSUS_MESSAGE_TYPE_TOO_LARGE";
    public const string MessagePayloadRequired = "CONSENSUS_MESSAGE_PAYLOAD_REQUIRED";
    public const string MessagePayloadTooLarge = "CONSENSUS_MESSAGE_PAYLOAD_TOO_LARGE";
}
