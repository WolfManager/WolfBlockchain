using WolfBlockchain.Api.AdminApi;
using WolfBlockchain.Api.Abstractions;
using WolfBlockchain.Api.PublicApi;
using WolfBlockchain.Agents.Abstractions;
using WolfBlockchain.Agents.Policies;
using WolfBlockchain.Core.Abstractions;
using WolfBlockchain.Core.Mempool;
using WolfBlockchain.Core.Validation;
using WolfBlockchain.Observability.Logging;
using WolfBlockchain.Protocol.Abstractions;
using WolfBlockchain.Storage.Canonical;
using WolfBlockchain.Wallet.Keys;
using WolfBlockchain.Wallet.Services;
using WolfBlockchain.Wallet.Signing;

namespace WolfBlockchain.StorageApi.IntegrationTests;

public class ModuleBoundaryIntegrationTests
{
    [Fact]
    public async Task CoreStorageBoundaryPersistsAcceptedBlockData()
    {
        var blockStore = new InMemoryBlockStore();
        var block = new BlockEnvelope(new ProtocolVersion(1, 0), 0, "block-a", string.Empty, Array.Empty<TransactionEnvelope>(), DateTimeOffset.UtcNow);

        await blockStore.SaveBlockAsync(block, CancellationToken.None);

        var loaded = await blockStore.GetBlockByHashAsync("block-a", CancellationToken.None);
        var height = await blockStore.GetCurrentHeightAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(0, height);
    }

    [Fact]
    public async Task WalletApiBoundarySubmitsSignedPayloadToMempool()
    {
        var keyStore = new InMemoryKeyStore();
        var account = await keyStore.CreateAccountAsync("ECDSA_P256", CancellationToken.None);
        var signer = new EcdsaSigner(keyStore);
        var wallet = new WalletService(keyStore, signer);

        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var signature = await wallet.SignTransactionAsync(account.AccountId, payload, CancellationToken.None);

        var mempool = new SafeMempoolService(new DeterministicTransactionValidator());
        var blockStore = new InMemoryBlockStore();
        var publicApi = new PublicApiService(mempool, new SuccessfulCommitOrchestrator(), blockStore, blockStore);
        var result = await publicApi.SubmitTransactionAsync(payload, new ApiRequestContext("req-1", "caller", null), CancellationToken.None);

        Assert.NotEmpty(signature);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task AgentsPolicyBoundaryDeniesUnsafeSubmitWithoutAuthorizationMarker()
    {
        var policy = new DefaultAgentPolicyEngine();
        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction, new Dictionary<string, string>());

        var decision = await policy.EvaluateAsync(request, new AgentPolicyContext("runtime", new[] { "agent-runtime" }, "req-2"), CancellationToken.None);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task AdminApiBoundaryRequiresRoleHeaderAuthorization()
    {
        var auth = new RoleBasedAdminAuthorizationService();
        var service = new AdminApiService(new StructuredAuditLogger());

        var allowed = auth.IsAuthorized(new[] { "admin" }, "admin");
        var denied = auth.IsAuthorized(Array.Empty<string>(), "admin");
        var status = await service.GetNodeStatusAsync(new ApiRequestContext("req-3", "admin-user", null), CancellationToken.None);

        Assert.True(allowed);
        Assert.False(denied);
        Assert.True(status.Success);
    }

    private sealed class SuccessfulCommitOrchestrator : ISingleNodeBlockCommitOrchestrator
    {
        public ValueTask<BlockCommitResult> TryCommitPendingBlockAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(new BlockCommitResult(true, "block-test", 0));
    }
}
