using Moq;
using Xunit;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.Tests.Services;

/// <summary>Unit tests for InMemoryChatSessionStore and related OllamaService types</summary>
public class OllamaServiceTests
{
    // ============= InMemoryChatSessionStore =============

    [Fact]
    public void CreateSession_ShouldReturnNonEmptyId()
    {
        var store = new InMemoryChatSessionStore();
        var id = store.CreateSession();
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Fact]
    public void CreateSession_MultipleSessions_ShouldHaveUniqueIds()
    {
        var store = new InMemoryChatSessionStore();
        var id1 = store.CreateSession();
        var id2 = store.CreateSession();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GetSession_ExistingSession_ShouldReturnSession()
    {
        var store = new InMemoryChatSessionStore();
        var id = store.CreateSession();
        var session = store.GetSession(id);
        Assert.NotNull(session);
        Assert.Equal(id, session.SessionId);
    }

    [Fact]
    public void GetSession_NonExistingSession_ShouldReturnNull()
    {
        var store = new InMemoryChatSessionStore();
        var session = store.GetSession("non-existing-id");
        Assert.Null(session);
    }

    [Fact]
    public void GetSession_NullOrEmpty_ShouldReturnNull()
    {
        var store = new InMemoryChatSessionStore();
        Assert.Null(store.GetSession(null!));
        Assert.Null(store.GetSession(string.Empty));
        Assert.Null(store.GetSession("  "));
    }

    [Fact]
    public void GetOrCreateSession_NewSession_ShouldCreateAndSetWasCreated()
    {
        var store = new InMemoryChatSessionStore();
        var session = store.GetOrCreateSession(null, out var wasCreated);
        Assert.NotNull(session);
        Assert.True(wasCreated);
    }

    [Fact]
    public void GetOrCreateSession_ExistingSession_ShouldReturnExistingAndNotSetWasCreated()
    {
        var store = new InMemoryChatSessionStore();
        var id = store.CreateSession();
        var session = store.GetOrCreateSession(id, out var wasCreated);
        Assert.NotNull(session);
        Assert.Equal(id, session.SessionId);
        Assert.False(wasCreated);
    }

    [Fact]
    public void AppendMessage_ShouldAddMessageToSession()
    {
        var store = new InMemoryChatSessionStore();
        var id = store.CreateSession();
        store.AppendMessage(id, new ChatMessage { Role = "user", Content = "Hello" });
        var session = store.GetSession(id);
        Assert.NotNull(session);
        Assert.Single(session.Messages);
        Assert.Equal("user", session.Messages[0].Role);
        Assert.Equal("Hello", session.Messages[0].Content);
    }

    [Fact]
    public void AppendMessage_InvalidSession_ShouldThrowKeyNotFoundException()
    {
        var store = new InMemoryChatSessionStore();
        Assert.Throws<KeyNotFoundException>(() =>
            store.AppendMessage("bad-id", new ChatMessage { Role = "user", Content = "Hi" }));
    }

    [Fact]
    public void DeleteSession_ExistingSession_ShouldReturnTrue()
    {
        var store = new InMemoryChatSessionStore();
        var id = store.CreateSession();
        var result = store.DeleteSession(id);
        Assert.True(result);
        Assert.Null(store.GetSession(id));
    }

    [Fact]
    public void DeleteSession_NonExistingSession_ShouldReturnFalse()
    {
        var store = new InMemoryChatSessionStore();
        var result = store.DeleteSession("does-not-exist");
        Assert.False(result);
    }

    [Fact]
    public void ActiveSessionCount_ShouldReflectCurrentSessions()
    {
        var store = new InMemoryChatSessionStore();
        Assert.Equal(0, store.ActiveSessionCount);
        store.CreateSession();
        store.CreateSession();
        Assert.Equal(2, store.ActiveSessionCount);
    }

    // ============= MockChatService =============

    [Fact]
    public async Task MockChatService_Chat_ShouldReturnEchoReply()
    {
        var store = new InMemoryChatSessionStore();
        var mock = new MockChatService(store);

        var response = await mock.ChatAsync(new ChatRequest { Message = "Hello World" });

        Assert.True(response.Success);
        Assert.Contains("Hello World", response.Reply);
        Assert.Equal("mock", response.Model);
        Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
    }

    [Fact]
    public async Task MockChatService_Chat_ShouldReuseExistingSession()
    {
        var store = new InMemoryChatSessionStore();
        var mock = new MockChatService(store);

        var r1 = await mock.ChatAsync(new ChatRequest { Message = "Msg 1" });
        var r2 = await mock.ChatAsync(new ChatRequest { Message = "Msg 2", SessionId = r1.SessionId });

        Assert.Equal(r1.SessionId, r2.SessionId);

        var session = store.GetSession(r1.SessionId);
        Assert.NotNull(session);
        Assert.Equal(4, session.Messages.Count); // 2 user + 2 assistant
    }

    [Fact]
    public void MockChatService_BackendName_ShouldBeMock()
    {
        var store = new InMemoryChatSessionStore();
        var mock = new MockChatService(store);
        Assert.Equal("mock", mock.BackendName);
    }

    // ============= LogSanitizer =============

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("hello", "hello")]
    [InlineData("hello\nworld", "hello world")]
    [InlineData("hello\rworld", "hello world")]
    [InlineData("hello\tworld", "hello world")] // tab is control char < 0x20
    [InlineData("safe text", "safe text")]
    public void SanitizeForLog_ShouldStripControlChars(string? input, string expected)
    {
        var result = LogSanitizer.SanitizeForLog(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeForLog_C1ControlChars_ShouldBeStripped()
    {
        // C1 range: 0x80–0x9F
        var input = "test\u0085value"; // NEXT LINE (0x85) is in C1 range
        var result = LogSanitizer.SanitizeForLog(input);
        Assert.Equal("test value", result);
    }

    [Fact]
    public void SanitizeForLog_Del_ShouldBeStripped()
    {
        var input = "test\u007Fvalue"; // DEL (0x7F)
        var result = LogSanitizer.SanitizeForLog(input);
        Assert.Equal("test value", result);
    }

    // ============= ChatMessage DTO =============

    [Fact]
    public void ChatMessage_Defaults_ShouldBeValid()
    {
        var msg = new ChatMessage { Role = "user", Content = "hi" };
        Assert.Equal("user", msg.Role);
        Assert.Equal("hi", msg.Content);
        Assert.NotEqual(default, msg.TimestampUtc);
    }

    // ============= ChatSession DTO =============

    [Fact]
    public void ChatSession_Defaults_ShouldBeValid()
    {
        var session = new ChatSession();
        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.Empty(session.Messages);
        Assert.NotEqual(default, session.CreatedAtUtc);
    }

    // ============= OllamaOptions =============

    [Fact]
    public void OllamaOptions_Defaults_ShouldBePopulated()
    {
        var opts = new OllamaOptions();
        Assert.False(string.IsNullOrWhiteSpace(opts.BaseUrl));
        Assert.False(string.IsNullOrWhiteSpace(opts.Model));
        Assert.True(opts.TimeoutSeconds > 0);
        Assert.False(string.IsNullOrWhiteSpace(opts.SystemPrompt));
    }
}
