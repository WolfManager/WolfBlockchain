namespace WolfBlockchain.Agents.Abstractions;

public enum AgentActionType
{
    ReadChainData,
    DraftTransaction,
    RequestSignature,
    SubmitTransaction,
    QueryAnalytics
}

public sealed record AgentActionRequest(string AgentId, AgentActionType ActionType, IReadOnlyDictionary<string, string> Parameters);

public sealed record AgentPolicyContext(string Environment, IReadOnlyCollection<string> Roles, string RequestId);

public sealed record AgentActionResult(bool Allowed, string Outcome, IReadOnlyDictionary<string, string>? Metadata = null);

public interface IAgentPolicyEvaluator
{
    ValueTask<bool> IsAllowedAsync(AgentActionRequest request, CancellationToken cancellationToken);
}

public interface IAgentPolicyEngine
{
    ValueTask<AgentActionResult> EvaluateAsync(AgentActionRequest request, AgentPolicyContext context, CancellationToken cancellationToken);
}

public interface IProviderAdapter
{
    ValueTask<string> GenerateAsync(string promptVersion, string prompt, CancellationToken cancellationToken);
}

public interface IAgentOrchestrator
{
    ValueTask<AgentActionResult> ExecuteAsync(AgentActionRequest request, CancellationToken cancellationToken);
}

public interface IAgentActionGateway
{
    ValueTask<AgentActionResult> HandleAsync(AgentActionRequest request, CancellationToken cancellationToken);
}
