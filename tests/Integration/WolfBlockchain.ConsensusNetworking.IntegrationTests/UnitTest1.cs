using WolfBlockchain.Consensus.Abstractions;
using WolfBlockchain.Networking.Abstractions;
using WolfBlockchain.Protocol.Abstractions;

namespace WolfBlockchain.ConsensusNetworking.IntegrationTests;

public class ConsensusNetworkingContractTests
{
    [Fact]
    public void ConsensusHandler_And_NetworkRouter_ShouldSharePeerMessageEnvelope()
    {
        var consensusMethod = typeof(IConsensusMessageHandler).GetMethod(nameof(IConsensusMessageHandler.HandleAsync));
        var routerMethod = typeof(INetworkMessageRouter).GetMethod(nameof(INetworkMessageRouter.RouteAsync));

        Assert.NotNull(consensusMethod);
        Assert.NotNull(routerMethod);
        Assert.Equal(typeof(PeerMessageEnvelope), consensusMethod!.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(PeerMessageEnvelope), routerMethod!.GetParameters()[0].ParameterType);
    }
}
