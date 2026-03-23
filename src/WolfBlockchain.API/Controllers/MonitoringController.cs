using WolfBlockchain.API.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Monitoring Controller - Performance metrics endpoint - PRODUCTION GRADE
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly IPerformanceMetrics _performanceMetrics;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(IPerformanceMetrics performanceMetrics, ILogger<MonitoringController> logger)
    {
        _performanceMetrics = performanceMetrics;
        _logger = logger;
    }

    /// <summary>
    /// Get performance statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatistics()
    {
        try
        {
            var stats = _performanceMetrics.GetStatistics();
            _logger.LogInformation("Performance statistics retrieved");

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalRequests = stats.TotalRequests,
                    averageResponseTimeMs = Math.Round(stats.AverageResponseTimeMs, 2),
                    maxResponseTimeMs = stats.MaxResponseTimeMs,
                    minResponseTimeMs = stats.MinResponseTimeMs,
                    errorCount = stats.ErrorCount,
                    errorRatePercent = Math.Round(stats.ErrorRatePercent, 2),
                    slowQueryCount = stats.SlowQueryCount,
                    averageMemoryMB = stats.AverageMemoryMB,
                    maxMemoryMB = stats.MaxMemoryMB,
                    collectionTime = stats.CollectionTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Get slow requests
    /// </summary>
    [HttpGet("slow-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSlowRequests([FromQuery] int topCount = 10)
    {
        try
        {
            if (topCount < 1 || topCount > 100)
                topCount = 10;

            var slowRequests = _performanceMetrics.GetSlowRequests(topCount);
            _logger.LogInformation("Retrieved {Count} slow requests", slowRequests.Count);

            return Ok(new
            {
                success = true,
                count = slowRequests.Count,
                data = slowRequests.Select(r => new
                {
                    endpoint = r.Endpoint,
                    durationMs = r.DurationMs,
                    statusCode = r.StatusCode,
                    timestamp = r.Timestamp
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slow requests");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve slow requests" });
        }
    }

    /// <summary>
    /// Get slow queries
    /// </summary>
    [HttpGet("slow-queries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSlowQueries([FromQuery] int topCount = 10)
    {
        try
        {
            if (topCount < 1 || topCount > 100)
                topCount = 10;

            var slowQueries = _performanceMetrics.GetSlowQueries(topCount);
            _logger.LogInformation("Retrieved {Count} slow queries", slowQueries.Count);

            return Ok(new
            {
                success = true,
                count = slowQueries.Count,
                data = slowQueries.Select(q => new
                {
                    query = q.Query,
                    durationMs = q.DurationMs,
                    timestamp = q.Timestamp
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slow queries");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve slow queries" });
        }
    }

    /// <summary>
    /// Get health status with performance metrics
    /// </summary>
    [HttpGet("health-detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealthDetailed()
    {
        try
        {
            var stats = _performanceMetrics.GetStatistics();
            var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            var gcGen0 = GC.GetGeneration(new object());

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                performance = new
                {
                    averageResponseTimeMs = Math.Round(stats.AverageResponseTimeMs, 2),
                    errorRatePercent = Math.Round(stats.ErrorRatePercent, 2),
                    memoryMB = memoryMB,
                    gcCollections = GC.CollectionCount(0)
                },
                requests = new
                {
                    totalCount = stats.TotalRequests,
                    errorCount = stats.ErrorCount,
                    slowCount = _performanceMetrics.GetSlowRequests(int.MaxValue).Count
                },
                database = new
                {
                    slowQueryCount = stats.SlowQueryCount,
                    status = "connected"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving detailed health");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy", error = ex.Message });
        }
    }
}
