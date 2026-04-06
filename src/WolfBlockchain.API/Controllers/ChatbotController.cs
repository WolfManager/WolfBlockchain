using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// AI Chatbot controller – powered by Ollama.
/// Provides conversation endpoints (send message, get history, clear session, status check).
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IOllamaService _ollama;
    private readonly IChatSessionStore _sessionStore;
    private readonly ILogger<ChatbotController> _logger;

    private const int MaxMessageLength = 4000;
    private const int MaxSessionIdLength = 128;

    public ChatbotController(
        IOllamaService ollama,
        IChatSessionStore sessionStore,
        ILogger<ChatbotController> logger)
    {
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send a message to the AI chatbot and receive a response.
    /// The conversation is persisted per sessionId so follow-up questions work correctly.
    /// </summary>
    /// <param name="request">Chat request with sessionId and message</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid request" });

        if (string.IsNullOrWhiteSpace(request.SessionId) || request.SessionId.Length > MaxSessionIdLength)
            return BadRequest(new { error = $"SessionId must be between 1 and {MaxSessionIdLength} characters" });

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxMessageLength)
            return BadRequest(new { error = $"Message must be between 1 and {MaxMessageLength} characters" });

        _logger.LogInformation("Chatbot request sessionId={SessionId} messageLength={Length}",
            SanitizeForLog(request.SessionId), request.Message.Length);

        try
        {
            var history = _sessionStore.GetHistory(request.SessionId);

            var reply = await _ollama.ChatAsync(request.Message, history, request.SystemPrompt, ct);

            // Persist both the user message and the assistant reply
            _sessionStore.AddMessage(request.SessionId, ChatRole.User, request.Message);
            _sessionStore.AddMessage(request.SessionId, ChatRole.Assistant, reply);

            return Ok(new ChatResponse
            {
                SessionId = request.SessionId,
                Message = reply,
                Model = _ollama.ModelName,
                TimestampUtc = DateTime.UtcNow,
                Success = true
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama service unavailable sessionId={SessionId}",
                SanitizeForLog(request.SessionId));
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "AI service is currently unavailable. Please ensure Ollama is running.",
                detail = ex.Message
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Ollama request timed out sessionId={SessionId}",
                SanitizeForLog(request.SessionId));
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                error = "AI service request timed out. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in chatbot sessionId={SessionId}",
                SanitizeForLog(request.SessionId));
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An unexpected error occurred."
            });
        }
    }

    /// <summary>
    /// Retrieve the full conversation history for a session (oldest message first).
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    [HttpGet("sessions/{sessionId}/history")]
    public IActionResult GetHistory(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > MaxSessionIdLength)
            return BadRequest(new { error = $"SessionId must be between 1 and {MaxSessionIdLength} characters" });

        _logger.LogInformation("Retrieving chat history sessionId={SessionId}",
            SanitizeForLog(sessionId));

        var history = _sessionStore.GetHistory(sessionId);

        return Ok(new
        {
            sessionId,
            messageCount = history.Count,
            messages = history.Select(m => new
            {
                role = m.Role.ToString().ToLowerInvariant(),
                content = m.Content,
                timestampUtc = m.TimestampUtc
            })
        });
    }

    /// <summary>
    /// Clear all messages for a session (start fresh).
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    [HttpDelete("sessions/{sessionId}")]
    public IActionResult ClearSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > MaxSessionIdLength)
            return BadRequest(new { error = $"SessionId must be between 1 and {MaxSessionIdLength} characters" });

        _logger.LogInformation("Clearing chat session sessionId={SessionId}",
            SanitizeForLog(sessionId));

        _sessionStore.ClearSession(sessionId);

        return Ok(new { success = true, message = "Session cleared", sessionId });
    }

    /// <summary>
    /// Check whether the Ollama backend is reachable and the configured model is available.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        _logger.LogInformation("Checking Ollama service status");

        var available = await _ollama.IsAvailableAsync(ct);

        return Ok(new
        {
            ollamaAvailable = available,
            model = _ollama.ModelName,
            checkedAtUtc = DateTime.UtcNow,
            status = available ? "ready" : "unavailable"
        });
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Strip ASCII control characters (0-31 and 127) from a user-supplied string
    /// before writing it to a log entry, preventing log-injection attacks.
    /// </summary>
    private static string SanitizeForLog(string value) =>
        string.Concat(value.Select(c => c < 0x20 || c == 0x7F ? ' ' : c));
}
