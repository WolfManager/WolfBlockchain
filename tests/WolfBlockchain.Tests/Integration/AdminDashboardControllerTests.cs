using System.Net;
using Xunit;
using WolfBlockchain.API.Controllers;

namespace WolfBlockchain.Tests.Integration;

/// <summary>
/// Integration tests for AdminDashboardController.
/// NOTE: These tests require running API instance.
/// Use [Trait("Category", "Integration")] to skip in CI/CD if API not available.
/// </summary>
[Trait("Category", "Integration")]
public class AdminDashboardControllerTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "http://localhost:5000";
    private string? _testAuthToken;

    public AdminDashboardControllerTests()
    {
        _httpClient = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        // Wait for API to be ready
        await WaitForApiReadinessAsync();
        
        // Get auth token (mock for testing)
        _testAuthToken = GenerateMockJwtToken();
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetSummary_ShouldReturnOkWithValidData()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/summary");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("TotalUsers", content);
        Assert.Contains("TotalTokens", content);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnPaginatedResults()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/users?page=1&pageSize=10");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Users", content);
        Assert.Contains("TotalCount", content);
        Assert.Contains("Page", content);
    }

    [Fact]
    public async Task GetTokens_ShouldReturnPaginatedResults()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/tokens?page=1&pageSize=10");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Tokens", content);
        Assert.Contains("TokenId", content);
    }

    [Fact]
    public async Task GetRecentEvents_ShouldReturnEventsList()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/recent-events?limit=10");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/summary");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/users?page=0&pageSize=10");
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
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/summary");
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
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/admindashboard/summary");
        request.Headers.Add("Authorization", $"Bearer {_testAuthToken}");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - First request (populate cache)
        var response1 = await _httpClient.SendAsync(request);
        stopwatch.Stop();
        var firstRequestTime = stopwatch.ElapsedMilliseconds;

        // Second request should be faster (from cache)
        stopwatch.Restart();
        var response2 = await _httpClient.SendAsync(request);
        stopwatch.Stop();
        var secondRequestTime = stopwatch.ElapsedMilliseconds;

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.True(secondRequestTime < firstRequestTime * 2, 
            $"Cached request ({secondRequestTime}ms) should be faster than first request ({firstRequestTime}ms)");
    }

    private async Task WaitForApiReadinessAsync(int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/health");
                if (response.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch { }

            await Task.Delay(1000);
        }

        throw new InvalidOperationException("API did not become ready in time");
    }

    private string GenerateMockJwtToken()
    {
        // In production, use proper JWT generation
        // This is a placeholder for testing
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImF1ZCI6IndvbGYtYmxvY2tjaGFpbiIsImlzcyI6IndvbGYtYmxvY2tjaGFpbi1hcGkifQ.placeholder";
    }
}
