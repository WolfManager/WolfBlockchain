using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WolfBlockchain.API.Controllers;
using WolfBlockchain.API.Services;
using System.Net;
using System.Net.Http;

namespace WolfBlockchain.Tests.Services;

// ============================================================
// InMemoryChatSessionStore tests
// ============================================================

/// <summary>Unit tests for InMemoryChatSessionStore</summary>
public class ChatSessionStoreTests
{
    private readonly InMemoryChatSessionStore _store = new();

    [Fact]
    public void AddMessage_ThenGetHistory_ReturnsMessages()
    {
        _store.AddMessage("s1", ChatRole.User, "Hello");
        _store.AddMessage("s1", ChatRole.Assistant, "Hi there!");

        var history = _store.GetHistory("s1");

        Assert.Equal(2, history.Count);
        Assert.Equal(ChatRole.User, history[0].Role);
        Assert.Equal("Hello", history[0].Content);
        Assert.Equal(ChatRole.Assistant, history[1].Role);
    }

    [Fact]
    public void GetHistory_UnknownSession_ReturnsEmpty()
    {
        var history = _store.GetHistory("does-not-exist");
        Assert.Empty(history);
    }

    [Fact]
    public void ClearSession_RemovesAllMessages()
    {
        _store.AddMessage("s2", ChatRole.User, "Question");
        _store.ClearSession("s2");

        Assert.Empty(_store.GetHistory("s2"));
        Assert.False(_store.SessionExists("s2"));
    }

    [Fact]
    public void SessionExists_ReturnsTrueAfterAddMessage()
    {
        _store.AddMessage("s3", ChatRole.User, "Ping");
        Assert.True(_store.SessionExists("s3"));
    }

    [Fact]
    public void SessionExists_ReturnsFalseForUnknownSession()
    {
        Assert.False(_store.SessionExists("unknown-xyz"));
    }

    [Fact]
    public void AddMessage_NullSessionId_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => _store.AddMessage(null!, ChatRole.User, "msg"));
    }

    [Fact]
    public void AddMessage_EmptyContent_Throws()
    {
        Assert.Throws<ArgumentException>(() => _store.AddMessage("s4", ChatRole.User, ""));
    }

    [Fact]
    public void MultipleSessionsAreIsolated()
    {
        _store.AddMessage("a", ChatRole.User, "From A");
        _store.AddMessage("b", ChatRole.User, "From B");

        Assert.Single(_store.GetHistory("a"));
        Assert.Single(_store.GetHistory("b"));
        Assert.Equal("From A", _store.GetHistory("a")[0].Content);
    }

    [Fact]
    public void ClearSession_UnknownSession_DoesNotThrow()
    {
        // Should be a no-op
        var ex = Record.Exception(() => _store.ClearSession("non-existent"));
        Assert.Null(ex);
    }
}

// ============================================================
// ChatbotController tests
// ============================================================

/// <summary>Unit tests for ChatbotController</summary>
public class ChatbotControllerTests
{
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly Mock<IChatSessionStore> _storeMock;
    private readonly Mock<ILogger<ChatbotController>> _loggerMock;
    private readonly ChatbotController _controller;

    public ChatbotControllerTests()
    {
        _ollamaMock = new Mock<IOllamaService>();
        _storeMock = new Mock<IChatSessionStore>();
        _loggerMock = new Mock<ILogger<ChatbotController>>();

        _ollamaMock.SetupGet(o => o.ModelName).Returns("llama3");
        _storeMock.Setup(s => s.GetHistory(It.IsAny<string>()))
                  .Returns(Array.Empty<ChatMessageDto>());

        _controller = new ChatbotController(_ollamaMock.Object, _storeMock.Object, _loggerMock.Object);
    }

    // ─── Chat ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Chat_ValidRequest_ReturnsOkWithReply()
    {
        _ollamaMock.Setup(o => o.ChatAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("I'm doing great, thank you!");

        var result = await _controller.Chat(new ChatRequest
        {
            SessionId = "session-1",
            Message = "How are you?"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("I'm doing great, thank you!", response.Message);
        Assert.Equal("session-1", response.SessionId);
        Assert.Equal("llama3", response.Model);
    }

    [Fact]
    public async Task Chat_SavesUserAndAssistantMessagesToStore()
    {
        _ollamaMock.Setup(o => o.ChatAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Blockchain is a distributed ledger.");

        await _controller.Chat(new ChatRequest
        {
            SessionId = "sess-save",
            Message = "What is blockchain?"
        }, CancellationToken.None);

        _storeMock.Verify(s => s.AddMessage("sess-save", ChatRole.User, "What is blockchain?"), Times.Once);
        _storeMock.Verify(s => s.AddMessage("sess-save", ChatRole.Assistant, "Blockchain is a distributed ledger."), Times.Once);
    }

    [Fact]
    public async Task Chat_EmptySessionId_ReturnsBadRequest()
    {
        var result = await _controller.Chat(new ChatRequest
        {
            SessionId = "",
            Message = "Hello"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_EmptyMessage_ReturnsBadRequest()
    {
        var result = await _controller.Chat(new ChatRequest
        {
            SessionId = "session-1",
            Message = ""
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_MessageTooLong_ReturnsBadRequest()
    {
        var result = await _controller.Chat(new ChatRequest
        {
            SessionId = "session-1",
            Message = new string('x', 4001)
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_OllamaUnavailable_Returns503()
    {
        _ollamaMock.Setup(o => o.ChatAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _controller.Chat(new ChatRequest
        {
            SessionId = "session-1",
            Message = "Hello"
        }, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusResult.StatusCode);
    }

    [Fact]
    public async Task Chat_Timeout_Returns504()
    {
        _ollamaMock.Setup(o => o.ChatAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Request timed out"));

        var result = await _controller.Chat(new ChatRequest
        {
            SessionId = "session-1",
            Message = "Hello"
        }, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(504, statusResult.StatusCode);
    }

    // ─── GetHistory ──────────────────────────────────────────────────────────

    [Fact]
    public void GetHistory_ExistingSession_ReturnsMessages()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = ChatRole.User, Content = "Hi" },
            new() { Role = ChatRole.Assistant, Content = "Hello!" }
        };

        _storeMock.Setup(s => s.GetHistory("session-h")).Returns(history);

        var result = _controller.GetHistory("session-h");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public void GetHistory_EmptySessionId_ReturnsBadRequest()
    {
        var result = _controller.GetHistory("");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetHistory_SessionIdTooLong_ReturnsBadRequest()
    {
        var result = _controller.GetHistory(new string('x', 129));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── ClearSession ────────────────────────────────────────────────────────

    [Fact]
    public void ClearSession_ValidSessionId_ClearsAndReturnsOk()
    {
        var result = _controller.ClearSession("session-c");

        _storeMock.Verify(s => s.ClearSession("session-c"), Times.Once);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public void ClearSession_EmptySessionId_ReturnsBadRequest()
    {
        var result = _controller.ClearSession("");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── GetStatus ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_WhenOllamaAvailable_ReturnsReadyStatus()
    {
        _ollamaMock.Setup(o => o.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetStatus_WhenOllamaUnavailable_ReturnsUnavailableStatus()
    {
        _ollamaMock.Setup(o => o.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
