using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using WolfBlockchain.API.Services;
using WolfBlockchain.API.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace WolfBlockchain.Tests.Services;

/// <summary>Unit tests for OllamaService (Option A: local AI chat provider).</summary>
public class OllamaServiceTests
{
    private readonly Mock<ILogger<OllamaService>> _loggerMock = new();
    private readonly Mock<IInputSanitizer> _sanitizerMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    public OllamaServiceTests()
    {
        // Default: sanitizer returns input unchanged (pass-through for happy-path tests)
        _sanitizerMock
            .Setup(s => s.SanitizeString(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((input, _) => input);

        _configMock
            .Setup(c => c["Ollama:DefaultModel"])
            .Returns("llama3");
    }

    private OllamaService CreateService(HttpClient httpClient) =>
        new OllamaService(httpClient, _loggerMock.Object, _sanitizerMock.Object, _configMock.Object);

    // ============= SEND MESSAGE =============

    [Fact]
    public async Task SendMessageAsync_WithValidMessage_ReturnsChatResponse()
    {
        // Arrange
        var fakeJson = """{"response":"Hello from Llama!","model":"llama3","done":true}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, fakeJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        var request = new ChatRequest { Message = "Hello", Model = "llama3" };

        // Act
        var result = await service.SendMessageAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello from Llama!", result.Reply);
        Assert.Equal("Ollama", result.Provider);
        Assert.Equal("llama3", result.Model);
        Assert.False(string.IsNullOrEmpty(result.SessionId));
        Assert.True(result.LatencyMs >= 0);
    }

    [Fact]
    public async Task SendMessageAsync_PreservesProvidedSessionId()
    {
        // Arrange
        var fakeJson = """{"response":"ok","model":"llama3","done":true}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, fakeJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        var request = new ChatRequest { Message = "Hi", SessionId = "session-abc" };

        // Act
        var result = await service.SendMessageAsync(request);

        // Assert
        Assert.Equal("session-abc", result.SessionId);
    }

    [Fact]
    public async Task SendMessageAsync_UsesDefaultModelWhenNoneSpecified()
    {
        // Arrange
        var fakeJson = """{"response":"ok","model":"llama3","done":true}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, fakeJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        var request = new ChatRequest { Message = "Hi" };  // no Model

        // Act
        var result = await service.SendMessageAsync(request);

        // Assert - model comes from configuration default "llama3"
        Assert.Equal("llama3", result.Model);
    }

    [Fact]
    public async Task SendMessageAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendMessageAsync(null!));
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessageAfterSanitization_ThrowsArgumentException()
    {
        // Arrange - sanitizer strips everything
        _sanitizerMock
            .Setup(s => s.SanitizeString(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(string.Empty);

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SendMessageAsync(new ChatRequest { Message = "   " }));
    }

    [Fact]
    public async Task SendMessageAsync_WhenOllamaReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SendMessageAsync(new ChatRequest { Message = "Hello" }));
    }

    // ============= LIST MODELS =============

    [Fact]
    public async Task ListModelsAsync_ReturnsModelNames()
    {
        // Arrange
        var fakeJson = """{"models":[{"name":"llama3"},{"name":"mistral"}]}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, fakeJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act
        var models = await service.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Equal(2, models.Count);
        Assert.Contains("llama3", models);
        Assert.Contains("mistral", models);
    }

    [Fact]
    public async Task ListModelsAsync_WhenOllamaUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "down");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.ListModelsAsync());
    }

    // ============= IS AVAILABLE =============

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaResponds_ReturnsTrue()
    {
        // Arrange
        var fakeJson = """{"models":[]}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, fakeJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act
        var available = await service.IsAvailableAsync();

        // Assert
        Assert.True(available);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaUnreachable_ReturnsFalse()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Act
        var available = await service.IsAvailableAsync();

        // Assert
        Assert.False(available);
    }

    // ============= PROVIDER NAME =============

    [Fact]
    public void ProviderName_ShouldBeOllama()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var service = CreateService(httpClient);

        // Assert
        Assert.Equal("Ollama", service.ProviderName);
    }
}

/// <summary>Unit tests for MockChatService (Option C).</summary>
public class MockChatServiceTests
{
    private readonly Mock<ILogger<MockChatService>> _loggerMock = new();

    [Fact]
    public async Task SendMessageAsync_ReturnsEchoOfMessage()
    {
        // Arrange
        var service = new MockChatService(_loggerMock.Object);
        var request = new ChatRequest { Message = "Hello world", SessionId = "s1" };

        // Act
        var result = await service.SendMessageAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Hello world", result.Reply);
        Assert.Equal("Mock", result.Provider);
        Assert.Equal("s1", result.SessionId);
    }

    [Fact]
    public async Task SendMessageAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new MockChatService(_loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SendMessageAsync(null!));
    }

    [Fact]
    public async Task SendMessageAsync_WithoutSessionId_GeneratesNewSessionId()
    {
        // Arrange
        var service = new MockChatService(_loggerMock.Object);

        // Act
        var result = await service.SendMessageAsync(new ChatRequest { Message = "Hi" });

        // Assert
        Assert.False(string.IsNullOrEmpty(result.SessionId));
    }

    [Fact]
    public async Task ListModelsAsync_ReturnsMockModel()
    {
        // Arrange
        var service = new MockChatService(_loggerMock.Object);

        // Act
        var models = await service.ListModelsAsync();

        // Assert
        Assert.Contains("mock", models);
    }

    [Fact]
    public async Task IsAvailableAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var service = new MockChatService(_loggerMock.Object);

        // Act
        var available = await service.IsAvailableAsync();

        // Assert
        Assert.True(available);
    }

    [Fact]
    public void ProviderName_ShouldBeMock()
    {
        // Arrange
        var service = new MockChatService(_loggerMock.Object);

        // Assert
        Assert.Equal("Mock", service.ProviderName);
    }
}

/// <summary>Unit tests for ChatProviderRegistry.</summary>
public class ChatProviderRegistryTests
{
    [Fact]
    public void GetAllOptions_ReturnsAtLeastTwoProviders()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        Assert.True(options.Count >= 2);
    }

    [Fact]
    public void GetAllOptions_ContainsOllama()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        Assert.Contains(options, o => o.Name == "Ollama");
    }

    [Fact]
    public void GetAllOptions_ContainsMock()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        Assert.Contains(options, o => o.Name == "Mock");
    }

    [Fact]
    public void GetAllOptions_OllamaRunsLocally()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        var ollama = options.Single(o => o.Name == "Ollama");
        Assert.True(ollama.RunsLocally);
        Assert.False(ollama.RequiresApiKey);
    }

    [Fact]
    public void GetAllOptions_AllOptionsHaveDescriptions()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        Assert.All(options, o => Assert.False(string.IsNullOrEmpty(o.Description)));
    }

    [Fact]
    public void GetAllOptions_AllOptionsHaveSetupTime()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        Assert.All(options, o => Assert.False(string.IsNullOrEmpty(o.EstimatedSetupTime)));
    }
}

// ============= TEST HELPERS =============

/// <summary>Fake HttpMessageHandler for unit testing HttpClient calls.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;
    private readonly Exception? _exception;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public FakeHttpMessageHandler(Exception exception)
    {
        _exception = exception;
        _statusCode = HttpStatusCode.OK;
        _content = string.Empty;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_exception is not null)
            throw _exception;

        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
