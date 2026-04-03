using WolfBlockchain.Agents.Abstractions;
using WolfBlockchain.Agents.Gateway;
using WolfBlockchain.Agents.Orchestration;
using WolfBlockchain.Agents.Policies;
using WolfBlockchain.Observability.Logging;

namespace WolfBlockchain.Agents.UnitTests;

public class AgentPolicyEngineTests
{
    [Fact]
    public async Task PolicyEngineDeniesUnapprovedTransactionSubmission()
    {
        var policyEngine = new DefaultAgentPolicyEngine();
        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction, new Dictionary<string, string>());

        var result = await policyEngine.EvaluateAsync(request, new AgentPolicyContext("test", new[] { "agent-runtime" }, "req-1"), CancellationToken.None);

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task OrchestratorAllowsAuthorizedGatewayAction()
    {
        var orchestrator = new SafeAgentOrchestrator(
            new DefaultAgentPolicyEngine(),
            new SafeAgentActionGateway(),
            new StructuredAuditLogger());

        var request = new AgentActionRequest("agent-1", AgentActionType.SubmitTransaction, new Dictionary<string, string>
        {
            ["authorizedCommand"] = "true"
        });

        var result = await orchestrator.ExecuteAsync(request, CancellationToken.None);

        Assert.True(result.Allowed);
    }
}
