using WolfBlockchain.Agents.Abstractions;

namespace WolfBlockchain.Agents.Gateway;

public sealed class SafeAgentActionGateway : IAgentActionGateway
{
    public ValueTask<AgentActionResult> HandleAsync(AgentActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.ActionType == AgentActionType.SubmitTransaction)
        {
            var isAuthorizedCommand = request.Parameters.TryGetValue("authorizedCommand", out var marker) &&
                                      string.Equals(marker, "true", StringComparison.OrdinalIgnoreCase);

            if (!isAuthorizedCommand)
            {
                return ValueTask.FromResult(new AgentActionResult(false, "blocked-direct-chain-mutation"));
            }
        }

        return ValueTask.FromResult(new AgentActionResult(true, "gateway-accepted"));
    }
}
