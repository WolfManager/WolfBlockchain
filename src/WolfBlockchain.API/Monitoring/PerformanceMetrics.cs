namespace WolfBlockchain.API.Monitoring;

/// <summary>
/// Performance metrics collection - PRODUCTION GRADE
/// </summary>
public interface IPerformanceMetrics
{
    void RecordRequestMetric(string endpoint, long durationMs, int statusCode);
    void RecordSlowQuery(string query, long durationMs);
    void RecordMemoryUsage(long memoryMB);
    PerformanceStatistics GetStatistics();
    List<RequestMetric> GetSlowRequests(int topCount = 10);
    List<QueryMetric> GetSlowQueries(int topCount = 10);
}

/// <summary>
/// Request metric
/// </summary>
public class RequestMetric
{
    public string Endpoint { get; set; } = "";
    public long DurationMs { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Query metric
/// </summary>
public class QueryMetric
{
    public string Query { get; set; } = "";
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Performance statistics
/// </summary>
public class PerformanceStatistics
{
    public int TotalRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long MaxResponseTimeMs { get; set; }
    public long MinResponseTimeMs { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRatePercent { get; set; }
    public int SlowQueryCount { get; set; }
    public long AverageMemoryMB { get; set; }
    public long MaxMemoryMB { get; set; }
    public DateTime CollectionTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Implementare Performance Metrics
/// </summary>
public class PerformanceMetrics : IPerformanceMetrics
{
    private readonly List<RequestMetric> _requestMetrics = new();
    private readonly List<QueryMetric> _queryMetrics = new();
    private readonly List<long> _memoryUsage = new();
    private readonly object _lockObject = new();
    private const int MaxMetricsCount = 1000;
    private const long SlowQueryThresholdMs = 100;
    private const long SlowRequestThresholdMs = 1000;

    public void RecordRequestMetric(string endpoint, long durationMs, int statusCode)
    {
        lock (_lockObject)
        {
            _requestMetrics.Add(new RequestMetric
            {
                Endpoint = endpoint,
                DurationMs = durationMs,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            });

            // Keep only recent metrics
            if (_requestMetrics.Count > MaxMetricsCount)
            {
                _requestMetrics.RemoveRange(0, _requestMetrics.Count - MaxMetricsCount);
            }
        }
    }

    public void RecordSlowQuery(string query, long durationMs)
    {
        if (durationMs < SlowQueryThresholdMs)
            return;

        lock (_lockObject)
        {
            _queryMetrics.Add(new QueryMetric
            {
                Query = query,
                DurationMs = durationMs,
                Timestamp = DateTime.UtcNow
            });

            if (_queryMetrics.Count > MaxMetricsCount)
            {
                _queryMetrics.RemoveRange(0, _queryMetrics.Count - MaxMetricsCount);
            }
        }
    }

    public void RecordMemoryUsage(long memoryMB)
    {
        lock (_lockObject)
        {
            _memoryUsage.Add(memoryMB);

            if (_memoryUsage.Count > MaxMetricsCount)
            {
                _memoryUsage.RemoveRange(0, _memoryUsage.Count - MaxMetricsCount);
            }
        }
    }

    public PerformanceStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            var stats = new PerformanceStatistics
            {
                TotalRequests = _requestMetrics.Count,
                AverageResponseTimeMs = _requestMetrics.Count > 0 
                    ? _requestMetrics.Average(r => r.DurationMs) 
                    : 0,
                MaxResponseTimeMs = _requestMetrics.Count > 0 
                    ? _requestMetrics.Max(r => r.DurationMs) 
                    : 0,
                MinResponseTimeMs = _requestMetrics.Count > 0 
                    ? _requestMetrics.Min(r => r.DurationMs) 
                    : 0,
                ErrorCount = _requestMetrics.Count(r => r.StatusCode >= 400),
                ErrorRatePercent = _requestMetrics.Count > 0 
                    ? (_requestMetrics.Count(r => r.StatusCode >= 400) * 100.0 / _requestMetrics.Count) 
                    : 0,
                SlowQueryCount = _queryMetrics.Count,
                AverageMemoryMB = _memoryUsage.Count > 0 
                    ? (long)_memoryUsage.Average() 
                    : 0,
                MaxMemoryMB = _memoryUsage.Count > 0 
                    ? _memoryUsage.Max() 
                    : 0
            };

            return stats;
        }
    }

    public List<RequestMetric> GetSlowRequests(int topCount = 10)
    {
        lock (_lockObject)
        {
            return _requestMetrics
                .Where(r => r.DurationMs > SlowRequestThresholdMs)
                .OrderByDescending(r => r.DurationMs)
                .Take(topCount)
                .ToList();
        }
    }

    public List<QueryMetric> GetSlowQueries(int topCount = 10)
    {
        lock (_lockObject)
        {
            return _queryMetrics
                .OrderByDescending(q => q.DurationMs)
                .Take(topCount)
                .ToList();
        }
    }
}
