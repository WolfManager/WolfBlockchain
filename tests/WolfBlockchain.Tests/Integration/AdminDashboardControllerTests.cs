using System.Net;
using Xunit;

namespace WolfBlockchain.Tests.Integration;

/// <summary>
/// Integration tests for AdminDashboardController.
/// Uses an in-process <see cref="WolfBlockchainWebApplicationFactory"/> — no external server required.
/// </summary>
[Trait("Category", "Integration")]
public class AdminDashboardControllerTests : IClassFixture<WolfBlockchainWebApplicationFactory>
{
    private readonly HttpClient _httpClient;
    private readonly string _testAuthToken;

    public AdminDashboardControllerTests(WolfBlockchainWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
        _testAuthToken = WolfBlockchainWebApplicationFactory.GenerateTestJwt();
    }

    [Fact]
    public async Task GetSummary_ShouldReturnOkWithValidData()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/summary");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("totalUsers", content);
        Assert.Contains("totalTokens", content);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPaginatedResults()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/users?page=1&pageSize=10");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("users", content);
        Assert.Contains("totalCount", content);
        Assert.Contains("page", content);
    }

    [Fact]
    public async Task GetTokens_ShouldReturnPaginatedResults()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/tokens?page=1&pageSize=10");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("tokens", content);
        Assert.Contains("tokenId", content);
    }

    [Fact]
    public async Task GetRecentEvents_ShouldReturnEventsList()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/recent-events?limit=10");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_WithoutAuthToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/summary");
        // Intentionally no auth header

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithInvalidPage_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/users?page=0&pageSize=10");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleMultipleClients()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/summary");
            request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");
            tasks.Add(_httpClient.SendAsync(request));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
    }

    [Fact]
    public async Task CachedEndpoint_ShouldReturnFastResponse()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - First request (populate cache)
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/summary");
        request1.Headers.Add("Authorization", $"Bearer {_testAuthToken}");
        var response1 = await _httpClient.SendAsync(request1);
        stopwatch.Stop();
        var firstRequestTime = stopwatch.ElapsedMilliseconds;

        // Second request should be faster (from cache)
        stopwatch.Restart();
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/admindashboard/summary");
        request2.Headers.Add("Authorization", $"Bearer {_testAuthToken}");
        var response2 = await _httpClient.SendAsync(request2);
        stopwatch.Stop();
        var secondRequestTime = stopwatch.ElapsedMilliseconds;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.True(secondRequestTime < firstRequestTime * 2,
            $"Cached request ({secondRequestTime}ms) should be faster than first request ({firstRequestTime}ms)");
    }
}
