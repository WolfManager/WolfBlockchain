using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WolfBlockchain.API.Controllers;
using WolfBlockchain.API.Services;
using Xunit;

namespace WolfBlockchain.Tests.Services;

/// <summary>Unit tests for InMemoryChatSessionStore</summary>
public class InMemoryChatSessionStoreTests
{
    private readonly InMemoryChatSessionStore _store = new();

    [Fact]
    public void GetHistory_UnknownSession_ReturnsEmptyList()
    {
        var history = _store.GetHistory("unknown-session");
        Assert.Empty(history);
    }

    [Fact]
    public void AddMessage_NewSession_CreatesSessionAndStoresMessage()
    {
        var msg = new ChatMessage("user", "Hello");
        _store.AddMessage("session1", msg);

        var history = _store.GetHistory("session1");
        Assert.Single(history);
        Assert.Equal("user", history[0].Role);
        Assert.Equal("Hello", history[0].Content);
    }

    [Fact]
    public void AddMessage_MultipleMessages_PreservesOrder()
    {
        _store.AddMessage("s", new ChatMessage("user", "Hi"));
        _store.AddMessage("s", new ChatMessage("assistant", "Hello!"));
        _store.AddMessage("s", new ChatMessage("user", "How are you?"));

        var history = _store.GetHistory("s");
        Assert.Equal(3, history.Count);
        Assert.Equal("user", history[0].Role);
        Assert.Equal("assistant", history[1].Role);
        Assert.Equal("user", history[2].Role);
    }

    [Fact]
    public void SessionExists_AfterAdd_ReturnsTrue()
    {
        _store.AddMessage("existing", new ChatMessage("user", "test"));
        Assert.True(_store.SessionExists("existing"));
    }

    [Fact]
    public void SessionExists_NoSession_ReturnsFalse()
    {
        Assert.False(_store.SessionExists("does-not-exist"));
    }

    [Fact]
    public void DeleteSession_ExistingSession_ReturnsTrueAndRemoves()
    {
        _store.AddMessage("toDelete", new ChatMessage("user", "x"));
        var result = _store.DeleteSession("toDelete");

        Assert.True(result);
        Assert.False(_store.SessionExists("toDelete"));
        Assert.Empty(_store.GetHistory("toDelete"));
    }

    [Fact]
    public void DeleteSession_UnknownSession_ReturnsFalse()
    {
        var result = _store.DeleteSession("ghost");
        Assert.False(result);
    }

    [Fact]
    public void AddMessage_NullSessionId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _store.AddMessage(string.Empty, new ChatMessage("user", "x")));
    }

    [Fact]
    public void GetHistory_NullSessionId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _store.GetHistory(string.Empty));
    }
}

/// <summary>Unit tests for ChatbotController</summary>
public class ChatbotControllerTests
{
    private readonly Mock<IOllamaService> _ollamaMock = new();
    private readonly InMemoryChatSessionStore _sessionStore = new();
    private readonly Mock<ILogger<ChatbotController>> _loggerMock = new();
    private readonly ChatbotController _controller;

    public ChatbotControllerTests()
    {
        _controller = new ChatbotController(_ollamaMock.Object, _sessionStore, _loggerMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ---- Chat endpoint ----

    [Fact]
    public async Task Chat_NullRequest_ReturnsBadRequest()
    {
        var result = await _controller.Chat(null!, CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task Chat_EmptyMessage_ReturnsBadRequest()
    {
        var result = await _controller.Chat(new ChatRequest("  ", null), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_ValidRequest_ReturnsOkWithSessionIdAndReply()
    {
        _ollamaMock
            .Setup(s => s.ChatAsync(It.IsAny<string>(), "Hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hi there!");

        var result = await _controller.Chat(new ChatRequest("Hello", null), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(response.SessionId));
        Assert.Equal("Hi there!", response.Reply);
    }

    [Fact]
    public async Task Chat_WithExistingSessionId_UsesProvidedSessionId()
    {
        _ollamaMock
            .Setup(s => s.ChatAsync("my-session", "Hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Reply");

        var result = await _controller.Chat(new ChatRequest("Hello", "my-session"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.Equal("my-session", response.SessionId);
    }

    [Fact]
    public async Task Chat_OllamaUnavailable_Returns503()
    {
        _ollamaMock
            .Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ollama backend is unavailable."));

        var result = await _controller.Chat(new ChatRequest("Hi", null), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }

    // ---- GetHistory endpoint ----

    [Fact]
    public void GetHistory_ExistingSession_ReturnsMessages()
    {
        _sessionStore.AddMessage("sess1", new ChatMessage("user", "hello"));
        _sessionStore.AddMessage("sess1", new ChatMessage("assistant", "world"));

        var result = _controller.GetHistory("sess1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetHistory_UnknownSession_Returns404()
    {
        var result = _controller.GetHistory("no-such-session");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void GetHistory_EmptySessionId_ReturnsBadRequest()
    {
        var result = _controller.GetHistory("  ");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---- DeleteSession endpoint ----

    [Fact]
    public void DeleteSession_ExistingSession_Returns204()
    {
        _sessionStore.AddMessage("toDelete", new ChatMessage("user", "x"));

        var result = _controller.DeleteSession("toDelete");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void DeleteSession_UnknownSession_Returns404()
    {
        var result = _controller.DeleteSession("ghost");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---- GetStatus endpoint ----

    [Fact]
    public async Task GetStatus_OllamaOnline_ReturnsOnline()
    {
        _ollamaMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetStatus_OllamaOffline_ReturnsOffline()
    {
        _ollamaMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
