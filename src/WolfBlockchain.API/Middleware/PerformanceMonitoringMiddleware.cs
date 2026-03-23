using WolfBlockchain.API.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace WolfBlockchain.API.Middleware;

/// <summary>
/// Performance Monitoring Middleware - Track slow requests - PRODUCTION GRADE
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly IPerformanceMetrics _performanceMetrics;
    private const long SlowRequestThresholdMs = 1000;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        IPerformanceMetrics performanceMetrics)
    {
        _next = next;
        _logger = logger;
        _performanceMetrics = performanceMetrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var startTicks = Environment.TickCount64;

        // Capture original body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);
        }
        finally
        {
            var endTicks = Environment.TickCount64;
            var durationMs = endTicks - startTicks;
            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            var statusCode = context.Response.StatusCode;

            // Record metric
            _performanceMetrics.RecordRequestMetric(endpoint, durationMs, statusCode);

            // Log slow requests
            if (durationMs > SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Endpoint} took {DurationMs}ms with status {StatusCode}",
                    endpoint, durationMs, statusCode);
            }

            // Log all requests with timing
            var logLevel = statusCode >= 500 ? LogLevel.Error
                         : statusCode >= 400 ? LogLevel.Warning
                         : LogLevel.Information;

            _logger.Log(logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms",
                context.Request.Method, context.Request.Path, statusCode, durationMs);

            // Track memory
            var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            _performanceMetrics.RecordMemoryUsage(memoryMB);
        }
    }
}
