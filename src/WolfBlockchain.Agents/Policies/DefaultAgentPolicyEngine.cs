using WolfBlockchain.Agents.Abstractions;

namespace WolfBlockchain.Agents.Policies;

public sealed class DefaultAgentPolicyEngine : IAgentPolicyEngine, IAgentPolicyEvaluator
{
    public ValueTask<bool> IsAllowedAsync(AgentActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var denied = request.ActionType == AgentActionType.SubmitTransaction &&
                     (!request.Parameters.TryGetValue("authorizedCommand", out var marker) ||
                      !string.Equals(marker, "true", StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult(!denied);
    }

    public async ValueTask<AgentActionResult> EvaluateAsync(AgentActionRequest request, AgentPolicyContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.AgentId))
        {
            return new AgentActionResult(false, "missing-agent-id");
        }

        if (context.Roles is null || context.Roles.Count == 0)
        {
            return new AgentActionResult(false, "missing-roles");
        }

        var allowed = await IsAllowedAsync(request, cancellationToken).ConfigureAwait(false);
        if (!allowed)
        {
            return new AgentActionResult(false, "policy-denied", new Dictionary<string, string>
            {
                ["actionType"] = request.ActionType.ToString()
            });
        }

        return new AgentActionResult(true, "policy-approved", new Dictionary<string, string>
        {
            ["environment"] = context.Environment,
            ["requestId"] = context.RequestId
        });
    }
}
