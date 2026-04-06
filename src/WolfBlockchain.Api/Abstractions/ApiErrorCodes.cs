namespace WolfBlockchain.Api.Abstractions;

public static class ApiErrorCodes
{
    public const string InvalidTransaction = "API_INVALID_TRANSACTION";
    public const string TransactionTooLarge = "API_TRANSACTION_TOO_LARGE";
    public const string InvalidBlockHash = "API_INVALID_BLOCK_HASH";
    public const string NotFound = "API_NOT_FOUND";

    public const string CommitNoTransactions = "COMMIT_NO_TRANSACTIONS";
    public const string CommitConsensusRejected = "COMMIT_CONSENSUS_REJECTED";
    public const string CommitSimulatedFailure = "COMMIT_SIMULATED_FAILURE";
}
