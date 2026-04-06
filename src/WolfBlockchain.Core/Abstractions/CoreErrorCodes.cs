namespace WolfBlockchain.Core.Abstractions;

public static class CoreErrorCodes
{
    public const string TxInvalidVersion = "TX_INVALID_VERSION";
    public const string TxMissingId = "TX_MISSING_ID";
    public const string TxMissingPayloadType = "TX_MISSING_PAYLOAD_TYPE";
    public const string TxEmptyPayload = "TX_EMPTY_PAYLOAD";
    public const string TxMissingSignature = "TX_MISSING_SIGNATURE";

    public const string BlockInvalidVersion = "BLOCK_INVALID_VERSION";
    public const string BlockInvalidHeight = "BLOCK_INVALID_HEIGHT";
    public const string BlockMissingHash = "BLOCK_MISSING_HASH";
    public const string BlockMissingPreviousHash = "BLOCK_MISSING_PREVIOUS_HASH";
    public const string BlockMissingTransactions = "BLOCK_MISSING_TRANSACTIONS";
    public const string BlockInvalidTransaction = "BLOCK_INVALID_TRANSACTION";
    public const string BlockDuplicateTransaction = "BLOCK_DUPLICATE_TRANSACTION";

    public const string StateBlockHashMismatch = "STATE_BLOCK_HASH_MISMATCH";
    public const string StateHeightMismatch = "STATE_HEIGHT_MISMATCH";
    public const string StateInvalidTimestamp = "STATE_INVALID_TIMESTAMP";
    public const string StateNonSequentialHeight = "STATE_NON_SEQUENTIAL_HEIGHT";
    public const string StatePreviousHashMismatch = "STATE_PREVIOUS_HASH_MISMATCH";

    public const string MempoolDuplicateTransaction = "MEMPOOL_DUPLICATE_TRANSACTION";
    public const string MempoolFull = "MEMPOOL_FULL";
}
