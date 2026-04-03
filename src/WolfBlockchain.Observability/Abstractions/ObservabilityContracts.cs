namespace WolfBlockchain.Observability.Abstractions;

public enum AuditEventType
{
    Security,
    Consensus,
    StateTransition,
    FeeAccounting,
    RewardDistribution,
    SupplyChange,
    AgentAction,
    ApiAccess
}

public interface IAuditLogger
{
    void Log(AuditEventType eventType, string eventName, IReadOnlyDictionary<string, string> metadata);
}

public interface IMetricsSink
{
    void Increment(string metricName, double value = 1);
    void Observe(string metricName, double value);
}

public interface IHealthReporter
{
    void ReportHealthy(string component, string details);
    void ReportUnhealthy(string component, string details);
}

public interface ITraceHook
{
    void Trace(string component, string operation, IReadOnlyDictionary<string, string> metadata);
}
