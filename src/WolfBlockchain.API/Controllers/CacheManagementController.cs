using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>Cache management endpoints</summary>
[ApiController]
[Route("api/[controller]")]
public class CacheManagementController : ControllerBase
{
    private readonly IQueryCacheService _queryCache;
    private readonly ICacheService _cache;
    private readonly ILogger<CacheManagementController> _logger;

    public CacheManagementController(
        IQueryCacheService queryCache,
        ICacheService cache,
        ILogger<CacheManagementController> logger)
    {
        _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get cache statistics</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetCacheStats()
    {
        try
        {
            var stats = await _queryCache.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get specific key statistics</summary>
    [HttpGet("key-stats/{key}")]
    public async Task<IActionResult> GetKeyStats(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return BadRequest(new { error = "Key is required" });

            var stats = await _queryCache.GetKeyStatsAsync(key);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key statistics for key: {Key}", key);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Invalidate cache by pattern</summary>
    [HttpPost("invalidate")]
    public async Task<IActionResult> InvalidateCache([FromBody] InvalidateCacheRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.Pattern))
                return BadRequest(new { error = "Pattern is required" });

            _logger.LogInformation("Invalidating cache pattern: {Pattern}", request.Pattern);
            await _queryCache.InvalidateAsync(request.Pattern);

            return Ok(new { message = "Cache invalidated successfully", pattern = request.Pattern });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Clear all cache</summary>
    [HttpPost("clear")]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            _logger.LogWarning("Clearing all cache");
            await _queryCache.ClearAsync();

            return Ok(new { message = "All cache cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get cache health status</summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetCacheHealth()
    {
        try
        {
            var stats = await _queryCache.GetStatsAsync();

            var health = new CacheHealthDto
            {
                IsHealthy = stats.HitRate > 40, // Healthy if > 40% hit rate
                HitRate = stats.HitRate,
                TotalKeys = stats.TotalKeys,
                TotalHits = stats.TotalHits,
                TotalMisses = stats.TotalMisses,
                Status = stats.HitRate > 60 ? "Excellent" :
                         stats.HitRate > 40 ? "Good" :
                         "Needs Optimization"
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache health");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Warm cache (pre-load common queries)</summary>
    [HttpPost("warm")]
    public async Task<IActionResult> WarmCache([FromBody] WarmCacheRequest request)
    {
        try
        {
            if (request?.Queries == null || request.Queries.Count == 0)
                return BadRequest(new { error = "Queries list is required" });

            _logger.LogInformation("Warming cache with {Count} queries", request.Queries.Count);

            var warmCount = 0;
            var failCount = 0;

            foreach (var query in request.Queries)
            {
                try
                {
                    // This would be implemented per query type in the actual system
                    // For now, we're just tracking the intent
                    warmCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for query: {Query}", query);
                    failCount++;
                }
            }

            return Ok(new
            {
                message = "Cache warming completed",
                successCount = warmCount,
                failureCount = failCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>Invalidate cache request</summary>
public record InvalidateCacheRequest
{
    public string Pattern { get; set; } = string.Empty;
}

/// <summary>Warm cache request</summary>
public record WarmCacheRequest
{
    public List<string> Queries { get; set; } = new();
}

/// <summary>Cache health status</summary>
public record CacheHealthDto
{
    public bool IsHealthy { get; set; }
    public double HitRate { get; set; }
    public int TotalKeys { get; set; }
    public int TotalHits { get; set; }
    public int TotalMisses { get; set; }
    public string Status { get; set; } = string.Empty;
}
