using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using WolfBlockchain.API.Controllers;
using WolfBlockchain.API.Services;
using WolfBlockchain.Storage.Repositories;

namespace WolfBlockchain.Tests.Integration;

/// <summary>
/// Unit tests for AdminDashboardController that test controller logic directly
/// without requiring a running API instance.
/// </summary>
public class AdminDashboardControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AdminDashboardCacheService _cacheService;
    private readonly AdminDashboardController _controller;

    public AdminDashboardControllerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheLogger = LoggerFactory.Create(b => b.AddConsole())
            .CreateLogger<AdminDashboardCacheService>();
        _cacheService = new AdminDashboardCacheService(memoryCache, cacheLogger);

        var controllerLogger = LoggerFactory.Create(b => b.AddConsole())
            .CreateLogger<AdminDashboardController>();
        _controller = new AdminDashboardController(_unitOfWorkMock.Object, _cacheService, controllerLogger);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnOkWithValidData()
    {
        // Act
        var result = await _controller.GetSummaryAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("TotalUsers", json);
        Assert.Contains("TotalTokens", json);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPaginatedResults()
    {
        // Act
        var result = await _controller.GetUsersAsync(page: 1, pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("Users", json);
        Assert.Contains("TotalCount", json);
        Assert.Contains("Page", json);
    }

    [Fact]
    public async Task GetTokens_ShouldReturnPaginatedResults()
    {
        // Act
        var result = await _controller.GetTokensAsync(page: 1, pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("Tokens", json);
        Assert.Contains("TokenId", json);
    }

    [Fact]
    public void GetRecentEvents_ShouldReturnEventsList()
    {
        // Act
        var result = _controller.GetRecentEvents(limit: 10);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Endpoints_ShouldRequireAuthentication()
    {
        // Verify that the controller class is decorated with [Authorize]
        var authorizeAttr = typeof(AdminDashboardController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
        Assert.NotEmpty(authorizeAttr);
    }

    [Fact]
    public async Task GetUsers_WithInvalidPage_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetUsersAsync(page: 0, pageSize: 10);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleMultipleClients()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _controller.GetSummaryAsync());

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.IsType<OkObjectResult>(r));
    }

    [Fact]
    public async Task CachedEndpoint_ShouldReturnFastResponse()
    {
        // Act - First request (populates cache)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result1 = await _controller.GetSummaryAsync();
        stopwatch.Stop();
        var firstRequestTime = stopwatch.ElapsedMilliseconds;

        // Second request should be served from cache and therefore faster
        stopwatch.Restart();
        var result2 = await _controller.GetSummaryAsync();
        stopwatch.Stop();
        var secondRequestTime = stopwatch.ElapsedMilliseconds;

        // Assert both return OK
        Assert.IsType<OkObjectResult>(result1);
        Assert.IsType<OkObjectResult>(result2);
        // Cached response must be strictly faster (or equal for sub-ms runs)
        Assert.True(secondRequestTime <= firstRequestTime,
            $"Cached request ({secondRequestTime}ms) should be faster than or equal to first request ({firstRequestTime}ms)");
    }
}
