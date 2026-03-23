using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using WolfBlockchain.API.Services;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.Tests.Services;

/// <summary>Analytics service tests</summary>
public class AnalyticsServiceTests : IAsyncLifetime
{
    private readonly WolfBlockchainDbContext _context;
    private readonly IQueryCacheService _queryCache;
    private readonly Mock<ILogger<AnalyticsService>> _loggerMock;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<WolfBlockchainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WolfBlockchainDbContext(options);
        _queryCache = new PassthroughQueryCacheService();
        _loggerMock = new Mock<ILogger<AnalyticsService>>();

        _analyticsService = new AnalyticsService(_context, _queryCache, _loggerMock.Object);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = Enumerable.Range(1, 10)
            .Select(i => new UserEntity
            {
                UserId = $"user_{i}",
                Username = $"user{i}",
                Email = $"user{i}@test.com",
                Address = $"0x{i:D40}",
                Role = "User",
                IsActive = i % 2 == 0 // Half active
            })
            .ToList();

        _context.Users.AddRange(users);

        // Create test transactions
        var transactions = Enumerable.Range(1, 20)
            .Select(i => new TransactionEntity
            {
                TransactionId = $"tx_{i}",
                FromAddress = users[i % users.Count].Address,
                ToAddress = users[(i + 1) % users.Count].Address,
                Amount = 100m + i,
                Fee = 0.5m,
                Status = i % 3 == 0 ? "Failed" : "Confirmed",
                Timestamp = DateTime.UtcNow.AddHours(-i)
            })
            .ToList();

        _context.Transactions.AddRange(transactions);

        // Create test tokens
        var tokens = Enumerable.Range(1, 5)
            .Select(i => new TokenEntity
            {
                TokenId = $"token_{i}",
                Name = $"Token {i}",
                Symbol = $"TK{i}",
                TokenType = "Standard",
                TotalSupply = 1000000,
                CurrentSupply = 500000,
                IsActive = true
            })
            .ToList();

        _context.Tokens.AddRange(tokens);

        await _context.SaveChangesAsync();
    }

    // ============= TRANSACTION ANALYTICS TESTS =============

    [Fact]
    public async Task GetTransactionAnalyticsAsync_ShouldReturnValidMetrics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var analytics = await _analyticsService.GetTransactionAnalyticsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(analytics);
        Assert.True(analytics.TotalTransactions > 0);
        Assert.True(analytics.TotalVolume > 0);
        Assert.True(analytics.SuccessRate >= 0 && analytics.SuccessRate <= 100);
    }

    [Fact]
    public async Task GetTransactionAnalyticsAsync_ShouldCalculateSuccessRate()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var analytics = await _analyticsService.GetTransactionAnalyticsAsync(startDate, endDate);

        // Assert
        var expectedSuccessRate = (double)analytics.SuccessfulTransactions / analytics.TotalTransactions * 100;
        Assert.Equal(expectedSuccessRate, analytics.SuccessRate, 2);
    }

    [Fact]
    public async Task GetTransactionTrendsAsync_ShouldReturnTrendData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act
        var trends = await _analyticsService.GetTransactionTrendsAsync(startDate, endDate, 1);

        // Assert
        Assert.NotNull(trends);
        Assert.NotEmpty(trends);
        Assert.All(trends, t => Assert.True(t.Value >= 0));
    }

    // ============= USER ANALYTICS TESTS =============

    [Fact]
    public async Task GetUserAnalyticsAsync_ShouldReturnValidMetrics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var analytics = await _analyticsService.GetUserAnalyticsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(analytics);
        Assert.True(analytics.TotalUsers > 0);
        Assert.True(analytics.ActiveUsers > 0);
        Assert.True(analytics.ActivityRate >= 0 && analytics.ActivityRate <= 100);
    }

    [Fact]
    public async Task GetUserAnalyticsAsync_ShouldCalculateActivityRate()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var analytics = await _analyticsService.GetUserAnalyticsAsync(startDate, endDate);

        // Assert
        var expectedRate = (double)analytics.ActiveUsers / analytics.TotalUsers * 100;
        Assert.Equal(expectedRate, analytics.ActivityRate, 2);
    }

    [Fact]
    public async Task GetUserGrowthAsync_ShouldReturnGrowthData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act
        var growth = await _analyticsService.GetUserGrowthAsync(startDate, endDate);

        // Assert
        Assert.NotNull(growth);
        Assert.NotEmpty(growth);
        Assert.True(growth.All(g => g.CumulativeCount >= 0));
    }

    // ============= SYSTEM PERFORMANCE TESTS =============

    [Fact]
    public async Task GetSystemPerformanceAsync_ShouldReturnMetrics()
    {
        // Act
        var performance = await _analyticsService.GetSystemPerformanceAsync();

        // Assert
        Assert.NotNull(performance);
        Assert.True(performance.TotalTransactions >= 0);
        Assert.True(performance.TotalUsers >= 0);
        Assert.NotEmpty(performance.HealthStatus);
    }

    // ============= TOKEN ANALYTICS TESTS =============

    [Fact]
    public async Task GetTokenAnalyticsAsync_ShouldReturnTokenMetrics()
    {
        // Act
        var analytics = await _analyticsService.GetTokenAnalyticsAsync();

        // Assert
        Assert.NotNull(analytics);
        Assert.True(analytics.TotalTokens > 0);
        Assert.True(analytics.ActiveTokens > 0);
        Assert.True(analytics.TotalSupply > 0);
    }

    // ============= ALERTS TESTS =============

    [Fact]
    public async Task CreateAlertAsync_ShouldAddAlert()
    {
        // Arrange
        var alert = new AlertDto
        {
            AlertType = "Test",
            Message = "Test alert",
            Severity = "Info",
            IsActive = true
        };

        // Act
        await _analyticsService.CreateAlertAsync(alert);
        var alerts = await _analyticsService.GetAlertsAsync();

        // Assert
        Assert.NotEmpty(alerts);
        Assert.Contains(alerts, a => a.AlertType == "Test");
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldOnlyReturnActiveAlerts()
    {
        // Arrange
        var activeAlert = new AlertDto { AlertType = "Active", Message = "Active", IsActive = true };
        var inactiveAlert = new AlertDto { AlertType = "Inactive", Message = "Inactive", IsActive = false };

        await _analyticsService.CreateAlertAsync(activeAlert);
        await _analyticsService.CreateAlertAsync(inactiveAlert);

        // Act
        var alerts = await _analyticsService.GetAlertsAsync();

        // Assert
        Assert.All(alerts, a => Assert.True(a.IsActive));
        Assert.Single(alerts);
    }

    // ============= REPORTING TESTS =============

    [Fact]
    public async Task GenerateDailyReportAsync_ShouldReturnCompleteReport()
    {
        // Arrange
        var date = DateTime.UtcNow.AddDays(-1);

        // Act
        var report = await _analyticsService.GenerateDailyReportAsync(date);

        // Assert
        Assert.NotNull(report);
        Assert.NotNull(report.TransactionMetrics);
        Assert.NotNull(report.UserMetrics);
        Assert.NotNull(report.SystemMetrics);
    }

    // ============= EDGE CASES =============

    [Fact]
    public async Task GetTransactionAnalyticsAsync_WithNoData_ShouldReturnZeros()
    {
        // Arrange
        var futureStart = DateTime.UtcNow.AddDays(100);
        var futureEnd = DateTime.UtcNow.AddDays(101);

        // Act
        var analytics = await _analyticsService.GetTransactionAnalyticsAsync(futureStart, futureEnd);

        // Assert
        Assert.NotNull(analytics);
        Assert.Equal(0, analytics.TotalTransactions);
    }

    [Fact]
    public async Task GetTransactionTrendsAsync_WithMultiDayInterval_ShouldGroupCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act
        var trends = await _analyticsService.GetTransactionTrendsAsync(startDate, endDate, 2);

        // Assert
        Assert.NotNull(trends);
        Assert.True(trends.Count > 0);
        Assert.True(trends.Count <= 4); // 7 days / 2 day interval
    }

    private sealed class PassthroughQueryCacheService : IQueryCacheService
    {
        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, params string[] invalidationPatterns) where T : class
            => factory();

        public Task InvalidateAsync(string pattern) => Task.CompletedTask;

        public Task<CacheStatsDto> GetStatsAsync() => Task.FromResult(new CacheStatsDto());

        public Task ClearAsync() => Task.CompletedTask;

        public Task<KeyStatsDto> GetKeyStatsAsync(string key) => Task.FromResult(new KeyStatsDto { Key = key });
    }
}
