using WolfBlockchain.Observability.Abstractions;

namespace WolfBlockchain.Observability.Metrics;

public sealed class InMemoryMetricsSink : IMetricsSink
{
    private readonly object _sync = new();
    private readonly Dictionary<string, double> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<double>> _observations = new(StringComparer.Ordinal);

    public void Increment(string metricName, double value = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);

        lock (_sync)
        {
            _counters.TryGetValue(metricName, out var current);
            _counters[metricName] = current + value;
        }
    }

    public void Observe(string metricName, double value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);

        lock (_sync)
        {
            if (!_observations.TryGetValue(metricName, out var list))
            {
                list = new List<double>();
                _observations[metricName] = list;
            }

            list.Add(value);
        }
    }

    public double GetCounter(string metricName)
    {
        lock (_sync)
        {
            return _counters.TryGetValue(metricName, out var value) ? value : 0;
        }
    }
}
