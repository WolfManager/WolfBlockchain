using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Api.PublicApi;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Core.Mempool;
using WolfBlockchain.Core.Validation;
using WolfBlockchain.Storage.Canonical;

namespace WolfBlockchain.StorageApi.IntegrationTests;

public class VerticalSliceNegativePathTests
{
    [Fact]
    public async Task SubmitWithEmptyPayload_ShouldFailAndNotMutateChainStatus()
    {
        var txValidator = new DeterministicTransactionValidator();
        var mempool = new SafeMempoolService(txValidator);
        var blockStore = new InMemoryBlockStore();
        var api = new PublicApiService(mempool, new FailingCommitOrchestrator(), blockStore, blockStore);
        var context = new ApiRequestContext("req-neg-1", "integration", null);

        var submit = await api.SubmitTransactionAsync(Array.Empty<byte>(), context, CancellationToken.None);
        var status = await api.GetChainStatusAsync(context, CancellationToken.None);

        Assert.False(submit.Success);
        Assert.Equal(-1, status.Data!.CurrentHeight);
    }

    [Fact]
    public async Task SubmitWithOversizedPayload_ShouldFailBeforeCommit()
    {
        var txValidator = new DeterministicTransactionValidator();
        var mempool = new SafeMempoolService(txValidator);
        var blockStore = new InMemoryBlockStore();
        var api = new PublicApiService(mempool, new FailingCommitOrchestrator(), blockStore, blockStore);
        var context = new ApiRequestContext("req-neg-oversize", "integration", null);

        var oversizedPayload = new byte[256 * 1024 + 1];
        var submit = await api.SubmitTransactionAsync(oversizedPayload, context, CancellationToken.None);
        var status = await api.GetChainStatusAsync(context, CancellationToken.None);

        Assert.False(submit.Success);
        Assert.Equal(ApiErrorCodes.TransactionTooLarge, submit.ErrorCode);
        Assert.Equal(-1, status.Data!.CurrentHeight);
        Assert.Null(status.Data.LastBlockHash);
    }

    [Fact]
    public async Task CommitFailure_ShouldReturnErrorAndNotPersistBlock()
    {
        var txValidator = new DeterministicTransactionValidator();
        var mempool = new SafeMempoolService(txValidator);
        var blockStore = new InMemoryBlockStore();
        var api = new PublicApiService(mempool, new FailingCommitOrchestrator(), blockStore, blockStore);
        var context = new ApiRequestContext("req-neg-2", "integration", null);

        var submit = await api.SubmitTransactionAsync(new byte[] { 0x01 }, context, CancellationToken.None);
        var status = await api.GetChainStatusAsync(context, CancellationToken.None);

        Assert.False(submit.Success);
        Assert.Equal(ApiErrorCodes.CommitSimulatedFailure, submit.ErrorCode);
        Assert.Equal(-1, status.Data!.CurrentHeight);
        Assert.Null(status.Data.LastBlockHash);
    }

    [Fact]
    public async Task ReadMissingBlock_ShouldReturnNotFound()
    {
        var txValidator = new DeterministicTransactionValidator();
        var mempool = new SafeMempoolService(txValidator);
        var blockStore = new InMemoryBlockStore();
        var api = new PublicApiService(mempool, new FailingCommitOrchestrator(), blockStore, blockStore);
        var context = new ApiRequestContext("req-neg-3", "integration", null);

        var readBack = await api.GetBlockByHashAsync("missing-block", context, CancellationToken.None);

        Assert.False(readBack.Success);
        Assert.Equal(ApiErrorCodes.NotFound, readBack.ErrorCode);
    }

    private sealed class FailingCommitOrchestrator : ISingleNodeBlockCommitOrchestrator
    {
        public ValueTask<BlockCommitResult> TryCommitPendingBlockAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(new BlockCommitResult(false, ErrorCode: ApiErrorCodes.CommitSimulatedFailure, ErrorMessage: "Commit intentionally failed for negative-path test."));
    }
}
