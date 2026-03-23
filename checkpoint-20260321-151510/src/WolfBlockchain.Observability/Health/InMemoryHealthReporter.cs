using WolfBlockchain.Observability.Abstractions;

namespace WolfBlockchain.Observability.Health;

public sealed class InMemoryHealthReporter : IHealthReporter
{
    private readonly object _sync = new();
    private readonly Dictionary<string, (bool IsHealthy, string Details)> _statusByComponent = new(StringComparer.Ordinal);

    public void ReportHealthy(string component, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);

        lock (_sync)
        {
            _statusByComponent[component] = (true, details);
        }
    }

    public void ReportUnhealthy(string component, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);

        lock (_sync)
        {
            _statusByComponent[component] = (false, details);
        }
    }

    public IReadOnlyDictionary<string, (bool IsHealthy, string Details)> Snapshot()
    {
        lock (_sync)
        {
            return new Dictionary<string, (bool IsHealthy, string Details)>(_statusByComponent);
        }
    }
}
