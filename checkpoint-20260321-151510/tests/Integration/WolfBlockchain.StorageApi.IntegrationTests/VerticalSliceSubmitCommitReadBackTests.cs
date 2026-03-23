using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Api.PublicApi;
using WolfBlockchain.Consensus.Engine;
using WolfBlockchain.Consensus.Plugins;
using WolfBlockchain.Consensus.Proposal;
using WolfBlockchain.Core.Engine;
using WolfBlockchain.Core.Mempool;
using WolfBlockchain.Core.State;
using WolfBlockchain.Core.Validation;
using WolfBlockchain.Storage.Canonical;

namespace WolfBlockchain.StorageApi.IntegrationTests;

public class VerticalSliceSubmitCommitReadBackTests
{
    [Fact]
    public async Task SubmitCommitReadBack_FlowShouldPersistAndReturnBlock()
    {
        var txValidator = new DeterministicTransactionValidator();
        var blockValidator = new DeterministicBlockValidator(txValidator);
        var mempool = new SafeMempoolService(txValidator);

        var proposalFactory = new DeterministicBlockProposalFactory(mempool);
        var consensusEngine = new PluginBasedConsensusEngine(new SingleNodeConsensusPlugin(), blockValidator);
        var blockchainEngine = new DeterministicBlockchainEngine(blockValidator, new DeterministicStateTransitionExecutor(), new InMemoryStateUpdater());

        var blockStore = new InMemoryBlockStore();
        var stateStore = new InMemoryStateStore();
        var orchestrator = new SingleNodeBlockCommitOrchestrator(blockStore, proposalFactory, consensusEngine, blockchainEngine, blockStore, stateStore);

        var publicApi = new PublicApiService(mempool, orchestrator, blockStore, blockStore);
        var requestContext = new ApiRequestContext("req-vs-1", "integration", "127.0.0.1");

        var submit = await publicApi.SubmitTransactionAsync(new byte[] { 0x01, 0x02 }, requestContext, CancellationToken.None);
        var status = await publicApi.GetChainStatusAsync(requestContext, CancellationToken.None);

        Assert.True(submit.Success);
        Assert.True(status.Success);
        Assert.Equal(0, status.Data!.CurrentHeight);
        Assert.False(string.IsNullOrWhiteSpace(status.Data.LastBlockHash));

        var readBack = await publicApi.GetBlockByHashAsync(status.Data.LastBlockHash!, requestContext, CancellationToken.None);
        Assert.True(readBack.Success);
        Assert.NotNull(readBack.Data);
        Assert.NotEmpty(readBack.Data!);
    }
}
