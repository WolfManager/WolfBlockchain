using System.Diagnostics;
using System.Collections.Concurrent;

namespace WolfBlockchain.API.Services;

/// <summary>Batch smart contract executor</summary>
public interface IBatchContractExecutor
{
    /// <summary>Execute multiple contract calls in batch</summary>
    Task<BatchExecutionResultDto> ExecuteBatchAsync(
        List<ContractCallDto> calls,
        CancellationToken ct = default);

    /// <summary>Execute with parallelism control</summary>
    Task<BatchExecutionResultDto> ExecuteParallelAsync(
        List<ContractCallDto> calls,
        int maxDegreeOfParallelism = 5,
        CancellationToken ct = default);

    /// <summary>Get batch execution statistics</summary>
    Task<BatchExecutionStatsDto> GetStatsAsync();
}

/// <summary>Implementation of batch contract executor</summary>
public class BatchContractExecutor : IBatchContractExecutor
{
    private readonly IContractCacheService _contractCache;
    private readonly ILogger<BatchContractExecutor> _logger;
    private readonly BatchExecutionMetrics _metrics;

    public BatchContractExecutor(
        IContractCacheService contractCache,
        ILogger<BatchContractExecutor> logger)
    {
        _contractCache = contractCache ?? throw new ArgumentNullException(nameof(contractCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new BatchExecutionMetrics();
    }

    /// <summary>Execute batch of contract calls</summary>
    public async Task<BatchExecutionResultDto> ExecuteBatchAsync(
        List<ContractCallDto> calls,
        CancellationToken ct = default)
    {
        return await ExecuteParallelAsync(calls, maxDegreeOfParallelism: 3, ct);
    }

    /// <summary>Execute batch with parallelism control</summary>
    public async Task<BatchExecutionResultDto> ExecuteParallelAsync(
        List<ContractCallDto> calls,
        int maxDegreeOfParallelism = 5,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(calls);

        if (calls.Count == 0)
        {
            return new BatchExecutionResultDto
            {
                Results = new List<ContractExecutionItemDto>(),
                TotalCount = 0,
                SuccessCount = 0,
                FailureCount = 0,
                ExecutionTimeMs = 0
            };
        }

        _logger.LogInformation("Starting batch execution of {Count} contract calls", calls.Count);

        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<ContractExecutionItemDto>();
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

        try
        {
            var tasks = calls.Select(async call =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var itemStopwatch = Stopwatch.StartNew();
                    var result = await ExecuteContractCallAsync(call, ct);
                    itemStopwatch.Stop();

                    result.ExecutionTimeMs = itemStopwatch.ElapsedMilliseconds;
                    results.Add(result);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var resultList = results.ToList();
            var successCount = resultList.Count(r => r.Success);
            var failureCount = resultList.Count(r => !r.Success);

            _logger.LogInformation(
                "Batch execution completed: {Total} total, {Success} success, {Failure} failure in {Ms}ms",
                resultList.Count,
                successCount,
                failureCount,
                stopwatch.ElapsedMilliseconds);

            RecordMetrics(resultList.Count, successCount, stopwatch.ElapsedMilliseconds);

            return new BatchExecutionResultDto
            {
                Results = resultList,
                TotalCount = resultList.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Batch execution cancelled: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch execution");
            throw;
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    /// <summary>Execute single contract call</summary>
    private async Task<ContractExecutionItemDto> ExecuteContractCallAsync(
        ContractCallDto call,
        CancellationToken ct)
    {
        try
        {
            // Try to get from cache first
            var cachedResult = await _contractCache.GetExecutionResultAsync(call.ContractId, call.MethodName);
            if (cachedResult != null && !call.BypassCache)
            {
                return new ContractExecutionItemDto
                {
                    ContractId = call.ContractId,
                    MethodName = call.MethodName,
                    Success = cachedResult.Success,
                    Result = cachedResult.Result,
                    ErrorMessage = cachedResult.ErrorMessage,
                    CachedResult = true
                };
            }

            // Execute contract call (simulated)
            var executionResult = new ExecutionResultDto
            {
                ContractId = call.ContractId,
                MethodName = call.MethodName,
                Success = true,
                Result = $"Executed {call.MethodName} on {call.ContractId}",
                ExecutedUtc = DateTime.UtcNow
            };

            // Cache the result
            if (!call.BypassCache)
            {
                await _contractCache.CacheExecutionResultAsync(
                    call.ContractId,
                    call.MethodName,
                    executionResult);
            }

            return new ContractExecutionItemDto
            {
                ContractId = call.ContractId,
                MethodName = call.MethodName,
                Success = executionResult.Success,
                Result = executionResult.Result,
                CachedResult = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing contract call: {ContractId}:{Method}",
                call.ContractId,
                call.MethodName);

            return new ContractExecutionItemDto
            {
                ContractId = call.ContractId,
                MethodName = call.MethodName,
                Success = false,
                ErrorMessage = ex.Message,
                CachedResult = false
            };
        }
    }

    /// <summary>Get batch statistics</summary>
    public async Task<BatchExecutionStatsDto> GetStatsAsync()
    {
        return new BatchExecutionStatsDto
        {
            TotalBatches = _metrics.TotalBatches,
            TotalCalls = _metrics.TotalCalls,
            TotalSuccesses = _metrics.TotalSuccesses,
            TotalFailures = _metrics.TotalFailures,
            AverageExecutionMs = _metrics.TotalBatches > 0
                ? _metrics.TotalExecutionTime / _metrics.TotalBatches
                : 0
        };
    }

    /// <summary>Record metrics</summary>
    private void RecordMetrics(int total, int successes, long executionTimeMs)
    {
        _metrics.TotalBatches++;
        _metrics.TotalCalls += total;
        _metrics.TotalSuccesses += successes;
        _metrics.TotalFailures += (total - successes);
        _metrics.TotalExecutionTime += executionTimeMs;
    }
}

/// <summary>Batch execution metrics</summary>
public class BatchExecutionMetrics
{
    public int TotalBatches { get; set; }
    public int TotalCalls { get; set; }
    public int TotalSuccesses { get; set; }
    public int TotalFailures { get; set; }
    public long TotalExecutionTime { get; set; }
}

/// <summary>Contract call DTO</summary>
public record ContractCallDto
{
    public string ContractId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public bool BypassCache { get; set; }
}

/// <summary>Single execution item result</summary>
public record ContractExecutionItemDto
{
    public string ContractId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CachedResult { get; set; }
    public long ExecutionTimeMs { get; set; }
}

/// <summary>Batch execution result</summary>
public record BatchExecutionResultDto
{
    public List<ContractExecutionItemDto> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long ExecutionTimeMs { get; set; }
}

/// <summary>Batch execution statistics</summary>
public record BatchExecutionStatsDto
{
    public int TotalBatches { get; set; }
    public int TotalCalls { get; set; }
    public int TotalSuccesses { get; set; }
    public int TotalFailures { get; set; }
    public double AverageExecutionMs { get; set; }
}
