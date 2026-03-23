using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WolfBlockchain.Storage.Context;

namespace WolfBlockchain.API.Services;

/// <summary>Serviciu pentru optimizarea performanței database și queriilor</summary>
public interface IPerformanceOptimizationService
{
    Task<PerformanceMetrics> MeasureQueryPerformanceAsync(string queryName, Func<Task> query);
    Task<T> MeasureQueryPerformanceAsync<T>(string queryName, Func<Task<T>> query);
    Task LogSlowQueryAsync(string query, long durationMs);
    Task<DatabaseHealthReport> GetDatabaseHealthAsync();
}

public class PerformanceOptimizationService : IPerformanceOptimizationService
{
    private readonly WolfBlockchainDbContext _context;
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private readonly ICacheService _cacheService;

    private const long SlowQueryThresholdMs = 200;

    public PerformanceOptimizationService(
        WolfBlockchainDbContext context,
        ILogger<PerformanceOptimizationService> logger,
        ICacheService cacheService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>Măsoară performanța unei query fără retur</summary>
    public async Task<PerformanceMetrics> MeasureQueryPerformanceAsync(string queryName, Func<Task> query)
    {
        ArgumentNullException.ThrowIfNull(queryName);
        ArgumentNullException.ThrowIfNull(query);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await query();
            stopwatch.Stop();

            var metrics = new PerformanceMetrics
            {
                QueryName = queryName,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Success = true,
                Timestamp = DateTime.UtcNow
            };

            if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                await LogSlowQueryAsync(queryName, stopwatch.ElapsedMilliseconds);
            }

            _logger.LogInformation("Query '{QueryName}' executed in {Duration}ms", queryName, stopwatch.ElapsedMilliseconds);
            return metrics;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query '{QueryName}' failed after {Duration}ms", queryName, stopwatch.ElapsedMilliseconds);
            
            return new PerformanceMetrics
            {
                QueryName = queryName,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Success = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>Măsoară performanța unei query cu retur</summary>
    public async Task<T> MeasureQueryPerformanceAsync<T>(string queryName, Func<Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(queryName);
        ArgumentNullException.ThrowIfNull(query);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await query();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                await LogSlowQueryAsync(queryName, stopwatch.ElapsedMilliseconds);
            }

            _logger.LogInformation("Query '{QueryName}' executed in {Duration}ms", queryName, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query '{QueryName}' failed after {Duration}ms", queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>Loghează query-uri lente</summary>
    public async Task LogSlowQueryAsync(string query, long durationMs)
    {
        ArgumentNullException.ThrowIfNull(query);

        var logEntry = new SlowQueryLog
        {
            Query = query,
            DurationMs = durationMs,
            LoggedAt = DateTime.UtcNow,
            Threshold = SlowQueryThresholdMs
        };

        var cacheKey = CacheExtensions.BuildKey("slow-queries", DateTime.UtcNow.ToString("yyyyMMdd-HH"));
        await _cacheService.SetAsync(cacheKey, logEntry, TimeSpan.FromHours(24));

        _logger.LogWarning(
            "[SLOW QUERY] {Query} - Duration: {Duration}ms (Threshold: {Threshold}ms)",
            query,
            durationMs,
            SlowQueryThresholdMs);
    }

    /// <summary>Raport de sănătate al bazei de date</summary>
    public async Task<DatabaseHealthReport> GetDatabaseHealthAsync()
    {
        var report = new DatabaseHealthReport
        {
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            // Test database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            report.IsConnected = canConnect;

            if (!canConnect)
            {
                _logger.LogError("Database connection failed");
                return report;
            }

            // Get connection state
            report.ConnectionState = _context.Database.GetConnectionString() != null ? "Connected" : "Not Connected";

            // Count records by entity
            report.UserCount = await _context.Users.CountAsync();
            report.TokenCount = await _context.Tokens.CountAsync();
            report.TransactionCount = await _context.Transactions.CountAsync();

            // Check for pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            report.HasPendingMigrations = pendingMigrations.Any();
            report.PendingMigrationCount = pendingMigrations.Count();

            _logger.LogInformation("Database health check completed: {@Report}", report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database health");
            report.Error = ex.Message;
        }

        return report;
    }
}

/// <summary>Metrici de performanță pentru query</summary>
public record PerformanceMetrics
{
    public string QueryName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>Log pentru query-uri lente</summary>
public record SlowQueryLog
{
    public string Query { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public long Threshold { get; set; }
    public DateTime LoggedAt { get; set; }
}

/// <summary>Raport de sănătate al bazei de date</summary>
public record DatabaseHealthReport
{
    public DateTime CheckedAt { get; set; }
    public bool IsConnected { get; set; }
    public string ConnectionState { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int TokenCount { get; set; }
    public int TransactionCount { get; set; }
    public bool HasPendingMigrations { get; set; }
    public int PendingMigrationCount { get; set; }
    public string? Error { get; set; }
}
