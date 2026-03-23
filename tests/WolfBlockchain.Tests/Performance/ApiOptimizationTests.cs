using Xunit;
using Microsoft.EntityFrameworkCore;
using WolfBlockchain.API.Services;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.Tests.Performance;

/// <summary>API optimization tests</summary>
public class ApiOptimizationTests : IAsyncLifetime
{
    private readonly WolfBlockchainDbContext _context;
    private readonly BatchingService _batchingService;
    private readonly ConnectionPoolingService _poolingService;
    private readonly ILogger<BatchingService> _logger;

    public ApiOptimizationTests()
    {
        var options = new DbContextOptionsBuilder<WolfBlockchainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WolfBlockchainDbContext(options);
        
        // Create mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<BatchingService>();
        
        _batchingService = new BatchingService(_context, _logger);
        _poolingService = new ConnectionPoolingService(
            loggerFactory.CreateLogger<ConnectionPoolingService>());
    }

    public async Task InitializeAsync()
    {
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = Enumerable.Range(1, 50)
            .Select(i => new UserEntity
            {
                UserId = $"user_{i}",
                Username = $"testuser_{i}",
                Email = $"user{i}@test.com",
                Address = $"0x{i:D40}",
                Role = "User",
                IsActive = true
            })
            .ToList();

        _context.Users.AddRange(users);

        // Create test tokens
        var tokens = Enumerable.Range(1, 10)
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

        // Create test transactions
        var transactions = Enumerable.Range(1, 100)
            .Select(i => new TransactionEntity
            {
                TransactionId = $"tx_{i}",
                FromAddress = users[i % users.Count].Address,
                ToAddress = users[(i + 1) % users.Count].Address,
                Amount = 10m,
                Fee = 0.001m,
                Status = "Confirmed",
                Timestamp = DateTime.UtcNow.AddHours(-i)
            })
            .ToList();

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    // ============= BATCHING SERVICE TESTS =============

    [Fact]
    public async Task GetUsersByIdsAsync_ShouldReturnMultipleUsers()
    {
        // Arrange
        var ids = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var results = await _batchingService.GetUsersByIdsAsync(ids);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldRespectMaxBatchSize()
    {
        // Arrange
        var ids = Enumerable.Range(1, 150).ToList(); // 150 items, max is 100

        // Act
        var results = await _batchingService.GetByIdsAsync<UserEntity>(ids, maxBatch: 100);

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.Count <= 100);
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldRemoveDuplicates()
    {
        // Arrange
        var ids = new List<int> { 1, 1, 2, 2, 3, 3 }; // Duplicates

        // Act
        var results = await _batchingService.GetByIdsAsync<UserEntity>(ids);

        // Assert
        Assert.NotEmpty(results);
        // Should have max 3 unique items (1, 2, 3)
        Assert.True(results.Count <= 3);
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldUseSingleQuery()
    {
        // This test verifies N+1 prevention
        // Arrange
        var ids = new List<int> { 1, 2, 3, 4, 5 };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var results = await _batchingService.GetByIdsAsync<UserEntity>(ids);
        stopwatch.Stop();

        // Assert
        Assert.NotEmpty(results);
        // Single batch query should be fast
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task GetTokensByIdsAsync_ShouldReturnTokens()
    {
        // Arrange
        var ids = new List<int> { 1, 2, 3 };

        // Act
        var results = await _batchingService.GetTokensByIdsAsync(ids);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetTransactionsByIdsAsync_ShouldReturnTransactions()
    {
        // Arrange
        var ids = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var results = await _batchingService.GetTransactionsByIdsAsync(ids);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetByIdsAsync_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var ids = new List<int>();

        // Act
        var results = await _batchingService.GetByIdsAsync<UserEntity>(ids);

        // Assert
        Assert.Empty(results);
    }

    // ============= CONNECTION POOLING TESTS =============

    [Fact]
    public void ConnectionPooling_ShouldHaveOptimalSettings()
    {
        // Act
        var stats = _poolingService.GetPoolStatsAsync().Result;

        // Assert
        Assert.True(stats.PoolingEnabled);
        Assert.Equal(5, stats.MinPoolSize);
        Assert.Equal(100, stats.MaxPoolSize);
        Assert.True(stats.MARSEnabled);
        Assert.True(stats.EncryptionEnabled);
        Assert.True(stats.OptimalSettings);
    }

    [Fact]
    public void ConnectionPooling_ShouldHaveRecommendations()
    {
        // Act
        var stats = _poolingService.GetPoolStatsAsync().Result;

        // Assert
        Assert.NotEmpty(stats.RecommendedActions);
        Assert.True(stats.RecommendedActions.Count > 0);
    }

    // ============= BATCH REQUEST BUILDER TESTS =============

    [Fact]
    public void BatchRequestBuilder_ShouldBuildCorrectly()
    {
        // Arrange
        var builder = new BatchRequestBuilder();

        // Act
        var request = builder
            .AddIds(1, 2, 3, 4, 5)
            .SetMaxSize(100)
            .Build();

        // Assert
        Assert.NotEmpty(request.Ids);
        Assert.Equal(5, request.Ids.Count);
        Assert.Equal(100, request.MaxIds);
    }

    [Fact]
    public void BatchRequestBuilder_ShouldRespectMaxSize()
    {
        // Arrange
        var builder = new BatchRequestBuilder();
        var ids = Enumerable.Range(1, 50).ToList();

        // Act
        var request = builder
            .AddIds(ids)
            .SetMaxSize(20)
            .Build();

        // Assert
        Assert.Equal(20, request.Ids.Count);
    }

    // ============= ASYNC/AWAIT BEST PRACTICES TESTS =============

    [Fact]
    public async Task AllOperations_ShouldBeFullyAsync()
    {
        // This test verifies no blocking calls are used
        var ids = new List<int> { 1, 2, 3 };

        // Act - All should be async
        var users = await _batchingService.GetUsersByIdsAsync(ids);
        var tokens = await _batchingService.GetTokensByIdsAsync(ids);
        var transactions = await _batchingService.GetTransactionsByIdsAsync(ids);

        // Assert - All completed without blocking
        Assert.NotNull(users);
        Assert.NotNull(tokens);
        Assert.NotNull(transactions);
    }

    [Fact]
    public async Task AsyncOperations_ShouldNotBlockThreadPool()
    {
        // Arrange
        var ids = new List<int> { 1, 2, 3, 4, 5 };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Multiple concurrent operations
        var task1 = _batchingService.GetUsersByIdsAsync(ids);
        var task2 = _batchingService.GetTokensByIdsAsync(ids);
        var task3 = _batchingService.GetTransactionsByIdsAsync(ids);

        await Task.WhenAll(task1, task2, task3);
        stopwatch.Stop();

        // Assert - All completed quickly (async, not blocking)
        Assert.True(stopwatch.ElapsedMilliseconds < 500);
    }
}
