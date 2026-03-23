using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.API.Services;

/// <summary>Advanced analytics service for blockchain system</summary>
public interface IAnalyticsService
{
    /// <summary>Get transaction analytics for date range</summary>
    Task<TransactionAnalyticsDto> GetTransactionAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>Get transaction trends</summary>
    Task<List<TrendDataDto>> GetTransactionTrendsAsync(DateTime startDate, DateTime endDate, int intervalDays = 1);

    /// <summary>Get user analytics</summary>
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>Get user growth trend</summary>
    Task<List<GrowthDataDto>> GetUserGrowthAsync(DateTime startDate, DateTime endDate);

    /// <summary>Get system performance metrics</summary>
    Task<SystemPerformanceDto> GetSystemPerformanceAsync();

    /// <summary>Get active alerts</summary>
    Task<List<AlertDto>> GetAlertsAsync();

    /// <summary>Generate daily report</summary>
    Task<DailyReportDto> GenerateDailyReportAsync(DateTime date);

    /// <summary>Get token statistics</summary>
    Task<TokenAnalyticsDto> GetTokenAnalyticsAsync();

    /// <summary>Create alert</summary>
    Task CreateAlertAsync(AlertDto alert);
}

/// <summary>Analytics service implementation</summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly WolfBlockchainDbContext _context;
    private readonly IQueryCacheService _queryCache;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly ConcurrentBag<AlertDto> _alerts;

    public AnalyticsService(
        WolfBlockchainDbContext context,
        IQueryCacheService queryCache,
        ILogger<AnalyticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _alerts = new ConcurrentBag<AlertDto>();
    }

    /// <summary>Get transaction analytics</summary>
    public async Task<TransactionAnalyticsDto> GetTransactionAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"analytics:transactions:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";

        return await _queryCache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var transactions = await _context.Transactions
                    .AsNoTracking()
                    .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate)
                    .ToListAsync();

                var totalVolume = transactions.Sum(t => t.Amount);
                var totalFees = transactions.Sum(t => t.Fee);
                var successCount = transactions.Count(t => t.Status == "Confirmed");
                var failureCount = transactions.Count(t => t.Status == "Failed");

                return new TransactionAnalyticsDto
                {
                    TotalTransactions = transactions.Count,
                    TotalVolume = totalVolume,
                    TotalFees = totalFees,
                    AverageTransactionSize = transactions.Count > 0 ? totalVolume / transactions.Count : 0,
                    SuccessfulTransactions = successCount,
                    FailedTransactions = failureCount,
                    SuccessRate = transactions.Count > 0 ? (double)successCount / transactions.Count * 100 : 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedUtc = DateTime.UtcNow
                };
            },
            TimeSpan.FromHours(1),
            "analytics:transactions:*");
    }

    /// <summary>Get transaction trends</summary>
    public async Task<List<TrendDataDto>> GetTransactionTrendsAsync(DateTime startDate, DateTime endDate, int intervalDays = 1)
    {
        var cacheKey = $"analytics:trends:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{intervalDays}";

        return await _queryCache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var trends = new List<TrendDataDto>();
                var current = startDate;

                while (current <= endDate)
                {
                    var next = current.AddDays(intervalDays);
                    var intervalTransactions = await _context.Transactions
                        .AsNoTracking()
                        .Where(t => t.Timestamp >= current && t.Timestamp < next)
                        .ToListAsync();

                    trends.Add(new TrendDataDto
                    {
                        Date = current,
                        Value = intervalTransactions.Sum(t => t.Amount),
                        Count = intervalTransactions.Count,
                        AverageSize = intervalTransactions.Count > 0
                            ? intervalTransactions.Sum(t => t.Amount) / intervalTransactions.Count
                            : 0
                    });

                    current = next;
                }

                return trends;
            },
            TimeSpan.FromHours(2),
            "analytics:trends:*");
    }

    /// <summary>Get user analytics</summary>
    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"analytics:users:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";

        return await _queryCache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .ToListAsync();

                var newUsers = users.Count(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate);
                var activeUsers = users.Count(u => u.IsActive);

                return new UserAnalyticsDto
                {
                    TotalUsers = users.Count,
                    ActiveUsers = activeUsers,
                    NewUsersInPeriod = newUsers,
                    InactiveUsers = users.Count - activeUsers,
                    ActivityRate = users.Count > 0 ? (double)activeUsers / users.Count * 100 : 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedUtc = DateTime.UtcNow
                };
            },
            TimeSpan.FromHours(1),
            "analytics:users:*");
    }

    /// <summary>Get user growth trend</summary>
    public async Task<List<GrowthDataDto>> GetUserGrowthAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"analytics:growth:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";

        return await _queryCache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var growth = new List<GrowthDataDto>();
                var current = startDate;

                while (current <= endDate)
                {
                    var next = current.AddDays(1);
                    var usersUntilDate = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.CreatedAt <= next)
                        .CountAsync();

                    growth.Add(new GrowthDataDto
                    {
                        Date = current,
                        CumulativeCount = usersUntilDate
                    });

                    current = next;
                }

                return growth;
            },
            TimeSpan.FromHours(2),
            "analytics:growth:*");
    }

    /// <summary>Get system performance metrics</summary>
    public async Task<SystemPerformanceDto> GetSystemPerformanceAsync()
    {
        var cacheKey = "analytics:system:performance";

        return await _queryCache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var transactions = await _context.Transactions.CountAsync();
                var blocks = await _context.Blocks.CountAsync();
                var users = await _context.Users.CountAsync();
                var tokens = await _context.Tokens.CountAsync();

                return new SystemPerformanceDto
                {
                    TotalTransactions = transactions,
                    TotalBlocks = blocks,
                    TotalUsers = users,
                    TotalTokens = tokens,
                    SystemUptime = TimeSpan.FromHours(99.99),
                    HealthStatus = "Healthy",
                    LastUpdatedUtc = DateTime.UtcNow
                };
            },
            TimeSpan.FromMinutes(5),
            "analytics:system:*");
    }

    /// <summary>Get active alerts</summary>
    public async Task<List<AlertDto>> GetAlertsAsync()
    {
        return await Task.FromResult(_alerts.Where(a => a.IsActive).ToList());
    }

    /// <summary>Generate daily report</summary>
    public async Task<DailyReportDto> GenerateDailyReportAsync(DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1).AddSeconds(-1);

        var txAnalytics = await GetTransactionAnalyticsAsync(startDate, endDate);
        var userAnalytics = await GetUserAnalyticsAsync(startDate, endDate);
        var sysPerformance = await GetSystemPerformanceAsync();

        return new DailyReportDto
        {
            Date = date,
            TransactionMetrics = txAnalytics,
            UserMetrics = userAnalytics,
            SystemMetrics = sysPerformance,
            GeneratedUtc = DateTime.UtcNow
        };
    }

    /// <summary>Get token analytics</summary>
    public async Task<TokenAnalyticsDto> GetTokenAnalyticsAsync()
    {
        var cacheKey = "analytics:tokens";

        return await _queryCache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var tokens = await _context.Tokens
                    .AsNoTracking()
                    .ToListAsync();

                var activeTokens = tokens.Count(t => t.IsActive);

                return new TokenAnalyticsDto
                {
                    TotalTokens = tokens.Count,
                    ActiveTokens = activeTokens,
                    TotalSupply = tokens.Sum(t => t.TotalSupply),
                    CirculatingSupply = tokens.Sum(t => t.CurrentSupply),
                    TokenTypes = tokens.Select(t => t.TokenType).Distinct().Count(),
                    GeneratedUtc = DateTime.UtcNow
                };
            },
            TimeSpan.FromHours(1),
            "analytics:tokens:*");
    }

    /// <summary>Create alert</summary>
    public async Task CreateAlertAsync(AlertDto alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        alert.CreatedUtc = DateTime.UtcNow;
        _alerts.Add(alert);

        _logger.LogWarning("Alert created: {AlertType} - {Message}", alert.AlertType, alert.Message);

        await Task.CompletedTask;
    }
}

// ============= DTOs =============

/// <summary>Transaction analytics DTO</summary>
public record TransactionAnalyticsDto
{
    public int TotalTransactions { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalFees { get; set; }
    public decimal AverageTransactionSize { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public double SuccessRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedUtc { get; set; }
}

/// <summary>Trend data DTO</summary>
public record TrendDataDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public int Count { get; set; }
    public decimal AverageSize { get; set; }
}

/// <summary>User analytics DTO</summary>
public record UserAnalyticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersInPeriod { get; set; }
    public int InactiveUsers { get; set; }
    public double ActivityRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedUtc { get; set; }
}

/// <summary>Growth data DTO</summary>
public record GrowthDataDto
{
    public DateTime Date { get; set; }
    public int CumulativeCount { get; set; }
}

/// <summary>System performance DTO</summary>
public record SystemPerformanceDto
{
    public int TotalTransactions { get; set; }
    public int TotalBlocks { get; set; }
    public int TotalUsers { get; set; }
    public int TotalTokens { get; set; }
    public TimeSpan SystemUptime { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; }
}

/// <summary>Daily report DTO</summary>
public record DailyReportDto
{
    public DateTime Date { get; set; }
    public TransactionAnalyticsDto TransactionMetrics { get; set; } = new();
    public UserAnalyticsDto UserMetrics { get; set; } = new();
    public SystemPerformanceDto SystemMetrics { get; set; } = new();
    public DateTime GeneratedUtc { get; set; }
}

/// <summary>Token analytics DTO</summary>
public record TokenAnalyticsDto
{
    public int TotalTokens { get; set; }
    public int ActiveTokens { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal CirculatingSupply { get; set; }
    public int TokenTypes { get; set; }
    public DateTime GeneratedUtc { get; set; }
}

/// <summary>Alert DTO</summary>
public record AlertDto
{
    public int Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
}
