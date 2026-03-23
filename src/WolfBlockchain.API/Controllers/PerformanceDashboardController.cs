using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WolfBlockchain.API.Services;
using WolfBlockchain.Storage.Context;

namespace WolfBlockchain.API.Controllers;

/// <summary>Optimized base controller with async/await best practices</summary>
[ApiController]
[Route("api/[controller]")]
public abstract class OptimizedApiControllerBase : ControllerBase
{
    protected readonly WolfBlockchainDbContext Context;
    protected readonly ICacheService CacheService;
    protected readonly IPerformanceOptimizationService PerfService;
    protected readonly ILogger Logger;

    protected OptimizedApiControllerBase(
        WolfBlockchainDbContext context,
        ICacheService cacheService,
        IPerformanceOptimizationService perfService,
        ILogger logger)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        CacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        PerfService = perfService ?? throw new ArgumentNullException(nameof(perfService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Wrap API call with performance tracking</summary>
    protected async Task<IActionResult> TrackPerformanceAsync(
        string operationName,
        Func<Task<IActionResult>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 200)
            {
                Logger.LogWarning(
                    "[SLOW_API] {Operation} took {Duration}ms",
                    operationName,
                    stopwatch.ElapsedMilliseconds);
            }

            Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "[API_ERROR] {Operation} failed after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>NEVER use synchronous blocking calls - always await</summary>
    protected async Task<T> GetAsync<T>(Func<Task<T>> operation) where T : class
    {
        return await operation().ConfigureAwait(false);
    }
}

/// <summary>Performance dashboard controller</summary>
[ApiController]
[Route("api/[controller]")]
public class PerformanceDashboardController : ControllerBase
{
    private readonly IPerformanceOptimizationService _perfService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PerformanceDashboardController> _logger;

    public PerformanceDashboardController(
        IPerformanceOptimizationService perfService,
        ICacheService cacheService,
        ILogger<PerformanceDashboardController> logger)
    {
        _perfService = perfService ?? throw new ArgumentNullException(nameof(perfService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get current performance metrics</summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        try
        {
            var cacheKey = "cache:perf:metrics";
            var cached = await _cacheService.GetAsync<PerformanceMetricsDto>(cacheKey);
            
            if (cached != null)
                return Ok(cached);

            var dbHealth = await _perfService.GetDatabaseHealthAsync();
            var metrics = new PerformanceMetricsDto
            {
                Timestamp = DateTime.UtcNow,
                IsConnected = dbHealth.IsConnected,
                UserCount = dbHealth.UserCount,
                TokenCount = dbHealth.TokenCount,
                TransactionCount = dbHealth.TransactionCount,
                HasPendingMigrations = dbHealth.HasPendingMigrations,
                ResponseTimeMs = 0
            };

            // Cache for 30 seconds
            await _cacheService.SetAsync(cacheKey, metrics, TimeSpan.FromSeconds(30));

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get database health</summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            var health = await _perfService.GetDatabaseHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database health");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get performance statistics</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetPerformanceStats()
    {
        try
        {
            var stats = new PerformanceStatsDto
            {
                Timestamp = DateTime.UtcNow,
                CacheEnabled = true,
                CompressionEnabled = true,
                AsyncAwaitUsed = true,
                ConnectionPooling = 100,
                RecommendedActions = GetRecommendations()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance stats");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private List<string> GetRecommendations()
    {
        return new()
        {
            "Ensure all DB queries use AsNoTracking() for read-only operations",
            "Use pagination for large datasets (default: 20 items per page)",
            "Enable caching for frequently accessed data",
            "Monitor slow queries (>200ms)",
            "Use async/await for all I/O operations",
            "Implement request batching for bulk operations"
        };
    }
}

/// <summary>Performance metrics DTO</summary>
public record PerformanceMetricsDto
{
    public DateTime Timestamp { get; set; }
    public bool IsConnected { get; set; }
    public int UserCount { get; set; }
    public int TokenCount { get; set; }
    public int TransactionCount { get; set; }
    public bool HasPendingMigrations { get; set; }
    public long ResponseTimeMs { get; set; }
}

/// <summary>Performance statistics DTO</summary>
public record PerformanceStatsDto
{
    public DateTime Timestamp { get; set; }
    public bool CacheEnabled { get; set; }
    public bool CompressionEnabled { get; set; }
    public bool AsyncAwaitUsed { get; set; }
    public int ConnectionPooling { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>Request batch DTO for bulk operations</summary>
public record BatchRequest
{
    public List<int> Ids { get; set; } = new();
    public int MaxIds { get; set; } = 100; // Safety limit
}

/// <summary>Batch response DTO</summary>
public record BatchResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Processed { get; set; }
}
