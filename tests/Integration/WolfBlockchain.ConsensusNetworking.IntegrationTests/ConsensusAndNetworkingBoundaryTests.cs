using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Consensus.Engine;
using WolfBlockchain.Consensus.Messaging;
using WolfBlockchain.Consensus.Plugins;
using WolfBlockchain.Core.Mempool;
using WolfBlockchain.Core.Validation;
using WolfBlockchain.Networking.Routing;
using WolfBlockchain.Networking.Validation;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.ConsensusNetworking.IntegrationTests;

public class ConsensusAndNetworkingBoundaryTests
{
    [Fact]
    public async Task ConsensusEngineCommitsValidProposalFromCoreValidator()
    {
        var txValidator = new DeterministicTransactionValidator();
        var blockValidator = new DeterministicBlockValidator(txValidator);
        var engine = new PluginBasedConsensusEngine(new SingleNodeConsensusPlugin(), blockValidator);

        await engine.StartRoundAsync(new ConsensusRoundContext(0, 0, DateTimeOffset.UtcNow), CancellationToken.None);

        var block = new BlockEnvelope(new ProtocolVersion(1, 0), 0, "block-0", string.Empty, new[]
        {
            new TransactionEnvelope(new ProtocolVersion(1,0), "tx-1", "transfer", new byte[] {0x01}, new byte[] {0x02})
        }, DateTimeOffset.UtcNow);

        var committed = await engine.TryCommitBlockAsync(block, CancellationToken.None);

        Assert.True(committed);
    }

    [Fact]
    public async Task ConsensusEngineRejectsInvalidRoundContextOnStart()
    {
        var txValidator = new DeterministicTransactionValidator();
        var blockValidator = new DeterministicBlockValidator(txValidator);
        var engine = new PluginBasedConsensusEngine(new SingleNodeConsensusPlugin(), blockValidator);

        var invalidContext = new ConsensusRoundContext(0, -1, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await engine.StartRoundAsync(invalidContext, CancellationToken.None));
    }

    [Fact]
    public async Task ConsensusEngineRejectsMissingRoundStartTimestampOnStart()
    {
        var txValidator = new DeterministicTransactionValidator();
        var blockValidator = new DeterministicBlockValidator(txValidator);
        var engine = new PluginBasedConsensusEngine(new SingleNodeConsensusPlugin(), blockValidator);

        var invalidContext = new ConsensusRoundContext(0, 0, default);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await engine.StartRoundAsync(invalidContext, CancellationToken.None));
    }

    [Fact]
    public async Task NetworkingRouterDropsInvalidExternalMessageBeforeHandler()
    {
        var called = false;
        var handlers = new Dictionary<string, WolfBlockchain.Networking.Abstractions.INetworkMessageHandler>
        {
            ["tx"] = new DelegateHandler(_ =>
            {
                called = true;
                return ValueTask.CompletedTask;
            })
        };

        var router = new ValidatingNetworkMessageRouter(new StrictExternalMessageValidator(), handlers);
        var invalid = new PeerMessageEnvelope(new ProtocolVersion(1, 0), "tx", Array.Empty<byte>(), "peer-1", DateTimeOffset.UtcNow);

        await router.RouteAsync(invalid, CancellationToken.None);

        Assert.False(called);
    }

    [Fact]
    public async Task NetworkingRouterMatchesHandlerCaseInsensitively()
    {
        var called = false;
        var handlers = new Dictionary<string, WolfBlockchain.Networking.Abstractions.INetworkMessageHandler>
        {
            ["TX"] = new DelegateHandler(_ =>
            {
                called = true;
                return ValueTask.CompletedTask;
            })
        };

        var router = new ValidatingNetworkMessageRouter(new StrictExternalMessageValidator(), handlers);
        var valid = new PeerMessageEnvelope(new ProtocolVersion(1, 0), " tx ", new byte[] { 0x01 }, "peer-1", DateTimeOffset.UtcNow);

        await router.RouteAsync(valid, CancellationToken.None);

        Assert.True(called);
    }

    [Fact]
    public async Task ConsensusMessageHandlerRejectsOversizedMessageType()
    {
        var handler = new DefaultConsensusMessageHandler();
        var messageType = new string('m', 65);
        var message = new PeerMessageEnvelope(new ProtocolVersion(1, 0), messageType, new byte[] { 0x01 }, "peer-1", DateTimeOffset.UtcNow);

        var result = await handler.HandleAsync(message, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal(ConsensusErrorCodes.MessageTypeTooLarge, result.ErrorCode);
    }

    [Fact]
    public async Task ConsensusMessageHandlerRejectsOversizedPayload()
    {
        var handler = new DefaultConsensusMessageHandler();
        var oversizedPayload = new byte[128 * 1024 + 1];
        var message = new PeerMessageEnvelope(new ProtocolVersion(1, 0), "vote", oversizedPayload, "peer-1", DateTimeOffset.UtcNow);

        var result = await handler.HandleAsync(message, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal(ConsensusErrorCodes.MessagePayloadTooLarge, result.ErrorCode);
    }

    [Fact]
    public void ConsensusPluginRejectsProposalWithMissingTimestamp()
    {
        var plugin = new SingleNodeConsensusPlugin();
        var context = new ConsensusRoundContext(0, 0, DateTimeOffset.UtcNow);
        var block = new BlockEnvelope(
            new ProtocolVersion(1, 0),
            0,
            "block-0",
            string.Empty,
            Array.Empty<TransactionEnvelope>(),
            default);

        var result = plugin.ValidateProposal(block, context);

        Assert.False(result.IsValid);
        Assert.Equal(ConsensusErrorCodes.ProposalInvalidTimestamp, result.ErrorCode);
    }

    [Fact]
    public void ConsensusPluginRejectsProposalBeforeRoundStart()
    {
        var plugin = new SingleNodeConsensusPlugin();
        var roundStart = DateTimeOffset.UtcNow;
        var context = new ConsensusRoundContext(1, 0, roundStart);
        var block = new BlockEnvelope(
            new ProtocolVersion(1, 0),
            1,
            "block-1",
            "block-0",
            Array.Empty<TransactionEnvelope>(),
            roundStart.AddSeconds(-1));

        var result = plugin.ValidateProposal(block, context);

        Assert.False(result.IsValid);
        Assert.Equal(ConsensusErrorCodes.ProposalBeforeRoundStart, result.ErrorCode);
    }

    private sealed class DelegateHandler(Func<PeerMessageEnvelope, ValueTask> callback) : WolfBlockchain.Networking.Abstractions.INetworkMessageHandler
    {
        public ValueTask HandleAsync(PeerMessageEnvelope message, CancellationToken cancellationToken) => callback(message);
    }
}
