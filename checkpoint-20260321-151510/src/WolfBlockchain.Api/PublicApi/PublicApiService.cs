using System.Security.Cryptography;
using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Protocol.Abstractions;
using WolfBlockchain.Storage.Abstractions;

namespace WolfBlockchain.Api.PublicApi;

public sealed class PublicApiService(
    IMempoolService mempoolService,
    ISingleNodeBlockCommitOrchestrator commitOrchestrator,
    IBlockStore blockStore,
    IChainReadRepository chainReadRepository) : IPublicApiService
{
    private const int MaxTransactionPayloadBytes = 256 * 1024;

    public async ValueTask<ApiResult<string>> SubmitTransactionAsync(byte[] transactionPayload, ApiRequestContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (transactionPayload is null || transactionPayload.Length == 0)
        {
            return new ApiResult<string>(false, null, ApiErrorCodes.InvalidTransaction, "Transaction payload is required.");
        }

        if (transactionPayload.Length > MaxTransactionPayloadBytes)
        {
            return new ApiResult<string>(false, null, ApiErrorCodes.TransactionTooLarge, $"Transaction payload exceeds maximum size of {MaxTransactionPayloadBytes} bytes.");
        }

        var transactionId = Convert.ToHexString(SHA256.HashData(transactionPayload));
        var envelope = new TransactionEnvelope(
            new ProtocolVersion(1, 0),
            transactionId,
            "raw",
            transactionPayload,
            Signature: new byte[] { 0x01 });

        var result = mempoolService.TryAcceptTransaction(envelope);
        if (!result.IsValid)
        {
            return new ApiResult<string>(false, null, result.ErrorCode, result.ErrorMessage);
        }

        var commitResult = await commitOrchestrator.TryCommitPendingBlockAsync(cancellationToken).ConfigureAwait(false);
        if (!commitResult.Committed)
        {
            return new ApiResult<string>(false, null, commitResult.ErrorCode, commitResult.ErrorMessage);
        }

        return new ApiResult<string>(true, transactionId);
    }

    public async ValueTask<ApiResult<byte[]>> GetBlockByHashAsync(string blockHash, ApiRequestContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(blockHash))
        {
            return new ApiResult<byte[]>(false, null, ApiErrorCodes.InvalidBlockHash, "Block hash is required.");
        }

        var block = await blockStore.GetBlockByHashAsync(blockHash, cancellationToken).ConfigureAwait(false);
        if (block is null)
        {
            return new ApiResult<byte[]>(false, null, ApiErrorCodes.NotFound, "Block was not found.");
        }

        var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(block);
        return new ApiResult<byte[]>(true, payload);
    }

    public async ValueTask<ApiResult<ChainStatusDto>> GetChainStatusAsync(ApiRequestContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pendingCount = mempoolService.GetPendingTransactions(10_000).Count;
        var height = await chainReadRepository.GetCurrentHeightAsync(cancellationToken).ConfigureAwait(false);
        var lastBlockHash = await chainReadRepository.GetLastBlockHashAsync(cancellationToken).ConfigureAwait(false);
        var status = new ChainStatusDto(CurrentHeight: height, PendingTransactions: pendingCount, LastBlockHash: lastBlockHash);
        return new ApiResult<ChainStatusDto>(true, status);
    }
}
