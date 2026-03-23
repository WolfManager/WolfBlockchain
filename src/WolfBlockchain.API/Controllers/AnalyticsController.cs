using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>Analytics endpoints for system monitoring and reporting</summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analytics,
        ILogger<AnalyticsController> logger)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get transaction analytics for date range</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactionAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            _logger.LogInformation("Getting transaction analytics from {Start} to {End}", start, end);

            var analytics = await _analytics.GetTransactionAnalyticsAsync(start, end);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get transaction trends</summary>
    [HttpGet("transactions/trends")]
    public async Task<IActionResult> GetTransactionTrends([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int intervalDays = 1)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            _logger.LogInformation("Getting transaction trends from {Start} to {End} with interval {Interval}", start, end, intervalDays);

            var trends = await _analytics.GetTransactionTrendsAsync(start, end, intervalDays);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction trends");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get user analytics</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUserAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            _logger.LogInformation("Getting user analytics from {Start} to {End}", start, end);

            var analytics = await _analytics.GetUserAnalyticsAsync(start, end);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get user growth trend</summary>
    [HttpGet("users/growth")]
    public async Task<IActionResult> GetUserGrowth([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            _logger.LogInformation("Getting user growth from {Start} to {End}", start, end);

            var growth = await _analytics.GetUserGrowthAsync(start, end);
            return Ok(growth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user growth");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get system performance metrics</summary>
    [HttpGet("system/performance")]
    public async Task<IActionResult> GetSystemPerformance()
    {
        try
        {
            _logger.LogInformation("Getting system performance metrics");

            var performance = await _analytics.GetSystemPerformanceAsync();
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system performance");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get active alerts</summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        try
        {
            _logger.LogInformation("Getting active alerts");

            var alerts = await _analytics.GetAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Create alert</summary>
    [HttpPost("alerts")]
    public async Task<IActionResult> CreateAlert([FromBody] AlertDto alert)
    {
        try
        {
            if (alert == null)
                return BadRequest(new { error = "Alert is required" });

            _logger.LogInformation("Creating alert: {Type}", alert.AlertType);

            await _analytics.CreateAlertAsync(alert);
            return Ok(new { message = "Alert created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Generate daily report</summary>
    [HttpGet("reports/daily")]
    public async Task<IActionResult> GenerateDailyReport([FromQuery] DateTime? date)
    {
        try
        {
            var reportDate = date ?? DateTime.UtcNow.AddDays(-1);

            _logger.LogInformation("Generating daily report for {Date}", reportDate);

            var report = await _analytics.GenerateDailyReportAsync(reportDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get token analytics</summary>
    [HttpGet("tokens")]
    public async Task<IActionResult> GetTokenAnalytics()
    {
        try
        {
            _logger.LogInformation("Getting token analytics");

            var analytics = await _analytics.GetTokenAnalyticsAsync();
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token analytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get comprehensive dashboard data</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardData()
    {
        try
        {
            _logger.LogInformation("Getting dashboard data");

            var systemPerf = await _analytics.GetSystemPerformanceAsync();
            var txAnalytics = await _analytics.GetTransactionAnalyticsAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
            var userAnalytics = await _analytics.GetUserAnalyticsAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
            var tokenAnalytics = await _analytics.GetTokenAnalyticsAsync();

            return Ok(new
            {
                system = systemPerf,
                transactions = txAnalytics,
                users = userAnalytics,
                tokens = tokenAnalytics,
                generatedUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
