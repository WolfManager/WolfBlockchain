using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WolfBlockchain.API.Controllers;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.Tests.Controllers;

/// <summary>Unit tests for ChatbotController</summary>
public class ChatbotControllerTests
{
    private readonly Mock<IOllamaService> _ollamaMock;
    private readonly Mock<IChatSessionStore> _sessionStoreMock;
    private readonly Mock<ILogger<ChatbotController>> _loggerMock;
    private readonly ChatbotController _controller;

    public ChatbotControllerTests()
    {
        _ollamaMock       = new Mock<IOllamaService>();
        _sessionStoreMock = new Mock<IChatSessionStore>();
        _loggerMock       = new Mock<ILogger<ChatbotController>>();
        _controller = new ChatbotController(
            _ollamaMock.Object,
            _sessionStoreMock.Object,
            _loggerMock.Object);
    }

    // ============= POST api/chatbot/chat =============

    [Fact]
    public async Task Chat_NullRequest_ShouldReturnBadRequest()
    {
        var result = await _controller.Chat(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_EmptyMessage_ShouldReturnBadRequest()
    {
        var result = await _controller.Chat(new ChatRequest { Message = "   " }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Chat_ValidRequest_OllamaSuccess_ShouldReturnOk()
    {
        _ollamaMock
            .Setup(s => s.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                Success   = true,
                Reply     = "Hello from Ollama",
                SessionId = "test-session-id",
                Model     = "llama3"
            });

        var result = await _controller.Chat(new ChatRequest { Message = "Hi" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Chat_ValidRequest_OllamaFails_ShouldReturn503()
    {
        _ollamaMock
            .Setup(s => s.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                Success   = false,
                Error     = "Backend unavailable",
                SessionId = "test-session-id"
            });

        var result = await _controller.Chat(new ChatRequest { Message = "Hi" }, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusResult.StatusCode);
    }

    // ============= GET api/chatbot/sessions/{id}/history =============

    [Fact]
    public void GetSessionHistory_EmptyId_ShouldReturnBadRequest()
    {
        var result = _controller.GetSessionHistory("  ");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetSessionHistory_NonExistingSession_ShouldReturnNotFound()
    {
        _sessionStoreMock
            .Setup(s => s.GetSession(It.IsAny<string>()))
            .Returns((ChatSession?)null);

        var result = _controller.GetSessionHistory("some-id");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void GetSessionHistory_ExistingSession_ShouldReturnOk()
    {
        var session = new ChatSession();
        session.Messages.Add(new ChatMessage { Role = "user", Content = "Hello" });

        _sessionStoreMock
            .Setup(s => s.GetSession(session.SessionId))
            .Returns(session);

        var result = _controller.GetSessionHistory(session.SessionId);
        Assert.IsType<OkObjectResult>(result);
    }

    // ============= DELETE api/chatbot/sessions/{id} =============

    [Fact]
    public void DeleteSession_EmptyId_ShouldReturnBadRequest()
    {
        var result = _controller.DeleteSession(string.Empty);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void DeleteSession_NonExistingSession_ShouldReturnNotFound()
    {
        _sessionStoreMock
            .Setup(s => s.DeleteSession(It.IsAny<string>()))
            .Returns(false);

        var result = _controller.DeleteSession("missing-id");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DeleteSession_ExistingSession_ShouldReturnOk()
    {
        _sessionStoreMock
            .Setup(s => s.DeleteSession("session-1"))
            .Returns(true);

        var result = _controller.DeleteSession("session-1");
        Assert.IsType<OkObjectResult>(result);
    }

    // ============= GET api/chatbot/status =============

    [Fact]
    public async Task Status_OllamaAvailable_ShouldReturnOkWithModels()
    {
        _ollamaMock
            .Setup(s => s.BackendName)
            .Returns("ollama");

        _ollamaMock
            .Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _ollamaMock
            .Setup(s => s.ListModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "llama3", "mistral" });

        _sessionStoreMock
            .Setup(s => s.ActiveSessionCount)
            .Returns(2);

        var result = await _controller.Status(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Status_OllamaUnavailable_ShouldReturnOkWithEmptyModels()
    {
        _ollamaMock
            .Setup(s => s.BackendName)
            .Returns("mock");

        _ollamaMock
            .Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sessionStoreMock
            .Setup(s => s.ActiveSessionCount)
            .Returns(0);

        var result = await _controller.Status(CancellationToken.None);

        // Should still return 200 OK (just with available=false)
        Assert.IsType<OkObjectResult>(result);
    }
}

/// <summary>Unit tests for ChatController</summary>
public class ChatControllerTests
{
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly Mock<ILogger<ChatController>> _loggerMock;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _chatServiceMock = new Mock<IChatService>();
        _loggerMock      = new Mock<ILogger<ChatController>>();
        _controller      = new ChatController(_chatServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendMessage_NullRequest_ShouldReturnBadRequest()
    {
        var result = await _controller.SendMessage(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_EmptyMessage_ShouldReturnBadRequest()
    {
        var result = await _controller.SendMessage(new ChatRequest { Message = "" }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_Success_ShouldReturnOk()
    {
        _chatServiceMock
            .Setup(s => s.BackendName)
            .Returns("mock");

        _chatServiceMock
            .Setup(s => s.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                Success   = true,
                Reply     = "test reply",
                SessionId = "sid",
                Model     = "mock"
            });

        var result = await _controller.SendMessage(new ChatRequest { Message = "Hello" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_Failure_ShouldReturn503()
    {
        _chatServiceMock
            .Setup(s => s.BackendName)
            .Returns("mock");

        _chatServiceMock
            .Setup(s => s.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { Success = false, Error = "error" });

        var result = await _controller.SendMessage(new ChatRequest { Message = "Hello" }, CancellationToken.None);
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }

    [Fact]
    public void GetBackend_ShouldReturnBackendName()
    {
        _chatServiceMock.Setup(s => s.BackendName).Returns("ollama");
        var result = _controller.GetBackend();
        Assert.IsType<OkObjectResult>(result);
    }
}
