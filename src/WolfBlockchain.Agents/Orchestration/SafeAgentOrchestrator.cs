using WolfBlockchain.Agents.Abstractions;

namespace WolfBlockchain.Agents.Orchestration;

public sealed class SafeAgentOrchestrator(
    IAgentPolicyEngine policyEngine,
    IAgentActionGateway gateway,
    IAuditLogger auditLogger) : IAgentOrchestrator
{
    public async ValueTask<AgentActionResult> ExecuteAsync(AgentActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = new AgentPolicyContext("runtime", new[] { "agent-runtime" }, Guid.NewGuid().ToString("N"));
        var policy = await policyEngine.EvaluateAsync(request, context, cancellationToken).ConfigureAwait(false);

        if (!policy.Allowed)
        {
            auditLogger.Log(AuditEventType.AgentAction, "agent.policy.denied", new Dictionary<string, string>
            {
                ["agentId"] = request.AgentId,
                ["actionType"] = request.ActionType.ToString()
            });

            return policy;
        }

        var gatewayResult = await gateway.HandleAsync(request, cancellationToken).ConfigureAwait(false);

        auditLogger.Log(AuditEventType.AgentAction, "agent.action.executed", new Dictionary<string, string>
        {
            ["agentId"] = request.AgentId,
            ["actionType"] = request.ActionType.ToString(),
            ["allowed"] = gatewayResult.Allowed.ToString()
        });

        return gatewayResult;
    }
}
