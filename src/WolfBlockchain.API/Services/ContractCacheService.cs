using System.Diagnostics;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Services;

/// <summary>Smart contract state caching service</summary>
public interface IContractCacheService
{
    /// <summary>Get cached contract state</summary>
    Task<ContractStateDto?> GetStateAsync(string contractId);

    /// <summary>Cache contract state</summary>
    Task CacheStateAsync(string contractId, ContractStateDto state, TimeSpan? expiration = null);

    /// <summary>Invalidate contract state cache</summary>
    Task InvalidateStateAsync(string contractId);

    /// <summary>Get execution result from cache</summary>
    Task<ExecutionResultDto?> GetExecutionResultAsync(string contractId, string methodName);

    /// <summary>Cache execution result</summary>
    Task CacheExecutionResultAsync(
        string contractId,
        string methodName,
        ExecutionResultDto result,
        TimeSpan? expiration = null);

    /// <summary>Get contract performance metrics</summary>
    Task<ContractPerformanceDto> GetPerformanceAsync(string contractId);

    /// <summary>Get cache statistics</summary>
    Task<ContractCacheStatsDto> GetStatsAsync();
}

/// <summary>Implementation of contract caching service</summary>
public class ContractCacheService : IContractCacheService
{
    private readonly ICacheService _cache;
    private readonly IQueryCacheService _queryCache;
    private readonly ILogger<ContractCacheService> _logger;
    private readonly Dictionary<string, ContractMetrics> _metrics;

    public ContractCacheService(
        ICacheService cache,
        IQueryCacheService queryCache,
        ILogger<ContractCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new Dictionary<string, ContractMetrics>();
    }

    /// <summary>Get cached contract state</summary>
    public async Task<ContractStateDto?> GetStateAsync(string contractId)
    {
        ArgumentNullException.ThrowIfNull(contractId);

        var key = $"contract:state:{contractId}";
        var state = await _cache.GetAsync<ContractStateDto>(key);

        if (state != null)
        {
            RecordMetric(contractId, hit: true);
            _logger.LogDebug("Contract state cache hit: {ContractId}", contractId);
        }
        else
        {
            RecordMetric(contractId, hit: false);
        }

        return state;
    }

    /// <summary>Cache contract state</summary>
    public async Task CacheStateAsync(string contractId, ContractStateDto state, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(contractId);
        ArgumentNullException.ThrowIfNull(state);

        var key = $"contract:state:{contractId}";
        var ttl = expiration ?? TimeSpan.FromHours(1);

        await _cache.SetAsync(key, state, ttl);
        _logger.LogDebug("Cached contract state: {ContractId}", contractId);
    }

    /// <summary>Invalidate contract state</summary>
    public async Task InvalidateStateAsync(string contractId)
    {
        ArgumentNullException.ThrowIfNull(contractId);

        var key = $"contract:state:{contractId}";
        await _cache.RemoveAsync(key);

        _logger.LogInformation("Invalidated contract state: {ContractId}", contractId);
    }

    /// <summary>Get execution result from cache</summary>
    public async Task<ExecutionResultDto?> GetExecutionResultAsync(string contractId, string methodName)
    {
        ArgumentNullException.ThrowIfNull(contractId);
        ArgumentNullException.ThrowIfNull(methodName);

        var key = $"contract:exec:{contractId}:{methodName}";
        var result = await _cache.GetAsync<ExecutionResultDto>(key);

        if (result != null)
        {
            RecordMetric(contractId, hit: true);
            _logger.LogDebug("Execution result cache hit: {ContractId}:{Method}", contractId, methodName);
        }
        else
        {
            RecordMetric(contractId, hit: false);
        }

        return result;
    }

    /// <summary>Cache execution result</summary>
    public async Task CacheExecutionResultAsync(
        string contractId,
        string methodName,
        ExecutionResultDto result,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(contractId);
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(result);

        var key = $"contract:exec:{contractId}:{methodName}";
        var ttl = expiration ?? TimeSpan.FromMinutes(30);

        await _cache.SetAsync(key, result, ttl);
        _logger.LogDebug("Cached execution result: {ContractId}:{Method}", contractId, methodName);
    }

    /// <summary>Get performance metrics for contract</summary>
    public async Task<ContractPerformanceDto> GetPerformanceAsync(string contractId)
    {
        ArgumentNullException.ThrowIfNull(contractId);

        if (!_metrics.TryGetValue(contractId, out var metric))
        {
            return new ContractPerformanceDto
            {
                ContractId = contractId,
                CacheHits = 0,
                CacheMisses = 0,
                HitRate = 0
            };
        }

        var total = metric.Hits + metric.Misses;
        var hitRate = total > 0 ? (double)metric.Hits / total * 100 : 0;

        return new ContractPerformanceDto
        {
            ContractId = contractId,
            CacheHits = metric.Hits,
            CacheMisses = metric.Misses,
            HitRate = hitRate,
            AverageExecutionMs = metric.AverageExecutionTime,
            LastAccessedUtc = metric.LastAccessedUtc
        };
    }

    /// <summary>Get cache statistics</summary>
    public async Task<ContractCacheStatsDto> GetStatsAsync()
    {
        var totalHits = _metrics.Values.Sum(m => m.Hits);
        var totalMisses = _metrics.Values.Sum(m => m.Misses);
        var total = totalHits + totalMisses;

        var stats = new ContractCacheStatsDto
        {
            TotalContracts = _metrics.Count,
            TotalCacheHits = totalHits,
            TotalCacheMisses = totalMisses,
            OverallHitRate = total > 0 ? (double)totalHits / total * 100 : 0,
            AverageExecutionMs = _metrics.Values.Average(m => m.AverageExecutionTime)
        };

        return stats;
    }

    /// <summary>Record cache hit/miss metric</summary>
    private void RecordMetric(string contractId, bool hit)
    {
        if (!_metrics.TryGetValue(contractId, out var metric))
        {
            metric = new ContractMetrics();
            _metrics[contractId] = metric;
        }

        if (hit)
            metric.Hits++;
        else
            metric.Misses++;

        metric.LastAccessedUtc = DateTime.UtcNow;
    }
}

/// <summary>Contract metrics</summary>
public class ContractMetrics
{
    public int Hits { get; set; }
    public int Misses { get; set; }
    public double AverageExecutionTime { get; set; }
    public DateTime LastAccessedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Contract state DTO</summary>
public record ContractStateDto
{
    public string ContractId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public Dictionary<string, object> StateVariables { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Execution result DTO</summary>
public record ExecutionResultDto
{
    public string ContractId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public object? Result { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Contract performance DTO</summary>
public record ContractPerformanceDto
{
    public string ContractId { get; set; } = string.Empty;
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double HitRate { get; set; }
    public double AverageExecutionMs { get; set; }
    public DateTime LastAccessedUtc { get; set; }
}

/// <summary>Contract cache statistics DTO</summary>
public record ContractCacheStatsDto
{
    public int TotalContracts { get; set; }
    public int TotalCacheHits { get; set; }
    public int TotalCacheMisses { get; set; }
    public double OverallHitRate { get; set; }
    public double AverageExecutionMs { get; set; }
}
