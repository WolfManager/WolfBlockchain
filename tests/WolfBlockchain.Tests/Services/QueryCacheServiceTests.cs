using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.Tests.Services;

/// <summary>Query cache service tests</summary>
public class QueryCacheServiceTests
{
    private readonly Mock<ICacheService> _baseCacheMock;
    private readonly Mock<ILogger<QueryCacheService>> _loggerMock;
    private readonly QueryCacheService _queryCacheService;

    public QueryCacheServiceTests()
    {
        _baseCacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<QueryCacheService>>();
        _queryCacheService = new QueryCacheService(_baseCacheMock.Object, _loggerMock.Object);
    }

    // ============= CACHE HIT TESTS =============

    [Fact]
    public async Task GetOrSetAsync_WhenCached_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test:key";
        var cachedValue = new TestData { Id = 1, Name = "Cached" };
        
        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync(cachedValue);

        var factoryCalled = false;
        Func<Task<TestData>> factory = async () =>
        {
            factoryCalled = true;
            return new TestData { Id = 2, Name = "Fresh" };
        };

        // Act
        var result = await _queryCacheService.GetOrSetAsync(key, factory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedValue.Id, result.Id);
        Assert.Equal("Cached", result.Name);
        Assert.False(factoryCalled, "Factory should not be called when cache hit");
    }

    // ============= CACHE MISS TESTS =============

    [Fact]
    public async Task GetOrSetAsync_WhenNotCached_ShouldExecuteFactory()
    {
        // Arrange
        var key = "test:key:miss";
        var freshValue = new TestData { Id = 2, Name = "Fresh" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null);

        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        Func<Task<TestData>> factory = async () => freshValue;

        // Act
        var result = await _queryCacheService.GetOrSetAsync(key, factory);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(freshValue.Id, result.Id);
        _baseCacheMock.Verify(c => c.SetAsync(key, freshValue, It.IsAny<TimeSpan?>()), Times.Once);
    }

    // ============= INVALIDATION TESTS =============

    [Fact]
    public async Task InvalidateAsync_ShouldInvalidateMatchingPatterns()
    {
        // Arrange
        var key = "users:1";
        var pattern = "users:*";
        var value = new TestData { Id = 1, Name = "User 1" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null);

        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        _baseCacheMock
            .Setup(c => c.RemoveAsync(key))
            .Returns(Task.CompletedTask);

        // preload metadata
        await _queryCacheService.GetOrSetAsync(key, () => Task.FromResult(value));

        // Act
        await _queryCacheService.InvalidateAsync(pattern);

        // Assert
        _baseCacheMock.Verify(c => c.RemoveAsync(key), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_WithSimpleKey_ShouldInvalidate()
    {
        // Arrange
        var key = "users:123";
        var freshValue = new TestData { Id = 3, Name = "Test" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null);

        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        _baseCacheMock
            .Setup(c => c.RemoveAsync(key))
            .Returns(Task.CompletedTask);

        await _queryCacheService.GetOrSetAsync(key, async () => freshValue);

        // Act
        await _queryCacheService.InvalidateAsync("users:*");

        // Assert
        _baseCacheMock.Verify(c => c.RemoveAsync(key), Times.Once);
    }

    // ============= STATISTICS TESTS =============

    [Fact]
    public async Task GetStatsAsync_ShouldReturnValidStatistics()
    {
        // Arrange
        var key1 = "test:key1";
        var key2 = "test:key2";
        var testValue = new TestData { Id = 1, Name = "Test" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(It.IsAny<string>()))
            .ReturnsAsync((TestData?)null);

        _baseCacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        // Act - Create cache entries
        await _queryCacheService.GetOrSetAsync(key1, async () => testValue);
        await _queryCacheService.GetOrSetAsync(key2, async () => testValue);

        var stats = await _queryCacheService.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalKeys >= 2);
        Assert.True(stats.TotalMisses >= 2);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateHitRate()
    {
        // Arrange
        var key = "test:hitrate";
        var testValue = new TestData { Id = 1, Name = "Test" };

        _baseCacheMock
            .SetupSequence(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null)
            .ReturnsAsync(testValue);

        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queryCacheService.GetOrSetAsync(key, async () => testValue);
        await _queryCacheService.GetOrSetAsync(key, async () => testValue);

        var stats = await _queryCacheService.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.HitRate >= 0);
        Assert.True(stats.HitRate <= 100);
    }

    // ============= CLEAR TESTS =============

    [Fact]
    public async Task ClearAsync_ShouldClearAllMetadata()
    {
        // Arrange
        var key = "test:clear";
        var testValue = new TestData { Id = 1, Name = "Test" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null);

        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        // Load something into cache
        await _queryCacheService.GetOrSetAsync(key, async () => testValue);

        // Act
        await _queryCacheService.ClearAsync();

        var stats = await _queryCacheService.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalKeys);
    }

    // ============= KEY STATS TESTS =============

    [Fact]
    public async Task GetKeyStatsAsync_ShouldReturnAccurateStats()
    {
        // Arrange
        var key = "test:keystats";
        var testValue = new TestData { Id = 1, Name = "Test" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null);

        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queryCacheService.GetOrSetAsync(key, async () => testValue);
        var stats = await _queryCacheService.GetKeyStatsAsync(key);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(key, stats.Key);
        Assert.Equal(1, stats.MissCount);
        Assert.Equal(0, stats.HitCount);
    }

    // ============= EXPIRATION TESTS =============

    [Fact]
    public async Task GetOrSetAsync_ShouldUseCustomExpiration()
    {
        // Arrange
        var key = "test:expiration";
        var expiration = TimeSpan.FromHours(1);
        var testValue = new TestData { Id = 1, Name = "Test" };

        _baseCacheMock
            .Setup(c => c.GetAsync<TestData>(key))
            .ReturnsAsync((TestData?)null);

        TimeSpan? actualExpiration = null;
        _baseCacheMock
            .Setup(c => c.SetAsync(key, It.IsAny<TestData>(), It.IsAny<TimeSpan?>()))
            .Callback<string, TestData, TimeSpan?>((k, v, e) => actualExpiration = e)
            .Returns(Task.CompletedTask);

        // Act
        await _queryCacheService.GetOrSetAsync(key, async () => testValue, expiration);

        // Assert
        Assert.NotNull(actualExpiration);
        Assert.Equal(expiration, actualExpiration);
    }

    // ============= NULL TESTS =============

    [Fact]
    public async Task GetOrSetAsync_WithNullKey_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _queryCacheService.GetOrSetAsync<TestData>(null!, () => Task.FromResult(new TestData())));
    }

    [Fact]
    public async Task InvalidateAsync_WithNullPattern_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _queryCacheService.InvalidateAsync(null!));
    }
}

/// <summary>Test data class</summary>
public class TestData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
