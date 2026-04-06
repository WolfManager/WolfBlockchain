namespace WolfBlockchain.Agents.Abstractions;

public enum AgentActionType
{
    ReadChainData,
    DraftTransaction,
    RequestSignature,
    SubmitTransaction,
    QueryAnalytics
}

public enum AuditEventType
{
    Security,
    Consensus,
    StateTransition,
    AgentAction,
    ApiAccess
}

public sealed record AgentActionRequest(string AgentId, AgentActionType ActionType, IReadOnlyDictionary<string, string> Parameters);

public sealed record AgentPolicyContext(string Environment, IReadOnlyCollection<string> Roles, string RequestId);

public sealed record AgentActionResult(bool Allowed, string Outcome, IReadOnlyDictionary<string, string>? Metadata = null);

public interface IAuditLogger
{
    void Log(AuditEventType eventType, string eventName, IReadOnlyDictionary<string, string> metadata);
}

public interface IAgentPolicyEvaluator
{
    ValueTask<bool> IsAllowedAsync(AgentActionRequest request, CancellationToken cancellationToken);
}

public interface IAgentPolicyEngine
{
    ValueTask<AgentActionResult> EvaluateAsync(AgentActionRequest request, AgentPolicyContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Adapter interface for AI provider integrations. Implementations must be self-contained
/// and must not depend on Ollama or any other specific AI runtime.
/// </summary>
public interface IProviderAdapter
{
    string ProviderName { get; }
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
