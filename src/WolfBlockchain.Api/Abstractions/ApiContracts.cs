namespace WolfBlockchain.Api.Abstractions;

public sealed record ApiRequestContext(string RequestId, string CallerId, string? IpAddress);

public sealed record ApiResult<T>(bool Success, T? Data, string? ErrorCode = null, string? ErrorMessage = null);

public sealed record ChainStatusDto(long CurrentHeight, int PendingTransactions, string? LastBlockHash);

public interface IPublicApiService
{
    ValueTask<ApiResult<string>> SubmitTransactionAsync(byte[] transactionPayload, ApiRequestContext context, CancellationToken cancellationToken);
    ValueTask<ApiResult<byte[]>> GetBlockByHashAsync(string blockHash, ApiRequestContext context, CancellationToken cancellationToken);
    ValueTask<ApiResult<ChainStatusDto>> GetChainStatusAsync(ApiRequestContext context, CancellationToken cancellationToken);
}

public interface IAdminAuthorizationService
{
    bool IsAuthorized(IReadOnlyCollection<string> roles, string requiredRole);
}

public interface IAdminApiService
{
    ValueTask<ApiResult<string>> GetNodeStatusAsync(ApiRequestContext context, CancellationToken cancellationToken);
    ValueTask<ApiResult<string>> GetConsensusStatusAsync(ApiRequestContext context, CancellationToken cancellationToken);
}
