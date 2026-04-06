using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WolfBlockchain.API.Services;
using Xunit;

namespace WolfBlockchain.Tests.Services;

/// <summary>Unit tests for <see cref="OllamaService"/>.</summary>
public class OllamaServiceTests
{
    private static OllamaService CreateService(
        HttpMessageHandler handler,
        string baseUrl = "http://localhost:11434",
        string defaultModel = "llama3",
        int timeoutSeconds = 30)
    {
        var options = new OllamaOptions
        {
            BaseUrl = baseUrl,
            DefaultModel = defaultModel,
            TimeoutSeconds = timeoutSeconds
        };
        // Simulate what Program.cs AddHttpClient configuration does at registration time.
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds))
        };
        var logger = Mock.Of<ILogger<OllamaService>>();
        return new OllamaService(client, Options.Create(options), logger);
    }

    private static HttpMessageHandler CreateHandler(HttpStatusCode statusCode, string jsonBody)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            });
        return mock.Object;
    }

    // ============= IsAvailableAsync =============

    [Fact]
    public async Task IsAvailableAsync_WhenServerReachable_ReturnsTrue()
    {
        var json = JsonSerializer.Serialize(new { models = Array.Empty<object>() });
        var service = CreateService(CreateHandler(HttpStatusCode.OK, json));

        var result = await service.IsAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServerUnreachable_ReturnsFalse()
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var service = CreateService(mock.Object);

        var result = await service.IsAvailableAsync();

        Assert.False(result);
    }

    // ============= ListModelsAsync =============

    [Fact]
    public async Task ListModelsAsync_WhenServerReturnsModels_ReturnsList()
    {
        var models = new[]
        {
            new { name = "llama3", modified_at = "2024-01-01", size = 1000L },
            new { name = "mistral", modified_at = "2024-01-02", size = 2000L }
        };
        var json = JsonSerializer.Serialize(new { models });
        var service = CreateService(CreateHandler(HttpStatusCode.OK, json));

        var result = await service.ListModelsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("llama3", result[0].Name);
        Assert.Equal("mistral", result[1].Name);
    }

    [Fact]
    public async Task ListModelsAsync_WhenServerReturnsEmptyList_ReturnsEmpty()
    {
        var json = JsonSerializer.Serialize(new { models = Array.Empty<object>() });
        var service = CreateService(CreateHandler(HttpStatusCode.OK, json));

        var result = await service.ListModelsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListModelsAsync_WhenServerReturnsError_Throws()
    {
        var service = CreateService(CreateHandler(HttpStatusCode.InternalServerError, "{}"));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.ListModelsAsync());
    }

    // ============= GenerateAsync =============

    [Fact]
    public async Task GenerateAsync_WithValidPrompt_ReturnsResponse()
    {
        var json = JsonSerializer.Serialize(new
        {
            model = "llama3",
            response = "Generated text",
            done = true,
            total_duration = 1000L
        });
        var service = CreateService(CreateHandler(HttpStatusCode.OK, json));

        var result = await service.GenerateAsync("Tell me about blockchain");

        Assert.NotNull(result);
        Assert.Equal("llama3", result.Model);
        Assert.Equal("Generated text", result.Response);
        Assert.True(result.Done);
    }

    [Fact]
    public async Task GenerateAsync_WithExplicitModel_UsesSpecifiedModel()
    {
        var capturedRequest = (HttpRequestMessage?)null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { model = "mistral", response = "ok", done = true, total_duration = 0L }),
                    Encoding.UTF8, "application/json")
            });

        var service = CreateService(mock.Object);

        var result = await service.GenerateAsync("Hello", model: "mistral");

        Assert.Equal("mistral", result.Model);
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("mistral", body);
    }

    [Fact]
    public async Task GenerateAsync_WithNullOrWhitespacePrompt_Throws()
    {
        var service = CreateService(CreateHandler(HttpStatusCode.OK, "{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateAsync("   "));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateAsync(string.Empty));
    }

    [Fact]
    public async Task GenerateAsync_WhenServerReturnsError_Throws()
    {
        var service = CreateService(CreateHandler(HttpStatusCode.ServiceUnavailable, "error"));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GenerateAsync("Hello"));
    }

    // ============= ChatAsync =============

    [Fact]
    public async Task ChatAsync_WithValidMessages_ReturnsResponse()
    {
        var json = JsonSerializer.Serialize(new
        {
            model = "llama3",
            message = new { role = "assistant", content = "Sure, here is the info." },
            done = true,
            total_duration = 500L
        });
        var service = CreateService(CreateHandler(HttpStatusCode.OK, json));

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "user", Content = "What is a blockchain?" }
        };

        var result = await service.ChatAsync(messages);

        Assert.NotNull(result);
        Assert.Equal("llama3", result.Model);
        Assert.NotNull(result.Message);
        Assert.Equal("assistant", result.Message!.Role);
        Assert.Equal("Sure, here is the info.", result.Message.Content);
    }

    [Fact]
    public async Task ChatAsync_WithNullMessages_Throws()
    {
        var service = CreateService(CreateHandler(HttpStatusCode.OK, "{}"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ChatAsync(null!));
    }

    [Fact]
    public async Task ChatAsync_WithEmptyMessages_Throws()
    {
        var service = CreateService(CreateHandler(HttpStatusCode.OK, "{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.ChatAsync(new List<OllamaChatMessage>()));
    }

    [Fact]
    public async Task ChatAsync_WhenServerReturnsError_Throws()
    {
        var service = CreateService(CreateHandler(HttpStatusCode.ServiceUnavailable, "error"));

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "user", Content = "Hello" }
        };

        await Assert.ThrowsAsync<HttpRequestException>(() => service.ChatAsync(messages));
    }

    // ============= Constructor guard clauses =============

    [Fact]
    public void Constructor_WithNullHttpClient_Throws()
    {
        var options = Options.Create(new OllamaOptions());
        var logger = Mock.Of<ILogger<OllamaService>>();

        Assert.Throws<ArgumentNullException>(() => new OllamaService(null!, options, logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_Throws()
    {
        var client = new HttpClient();
        var logger = Mock.Of<ILogger<OllamaService>>();

        Assert.Throws<ArgumentNullException>(() => new OllamaService(client, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var client = new HttpClient();
        var options = Options.Create(new OllamaOptions());

        Assert.Throws<ArgumentNullException>(() => new OllamaService(client, options, null!));
    }
}
