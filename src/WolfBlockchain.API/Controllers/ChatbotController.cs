using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Chatbot controller — exposes Ollama-powered conversational AI endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IOllamaService _ollamaService;
    private readonly IChatSessionStore _sessionStore;
    private readonly ILogger<ChatbotController> _logger;

    private static string SanitizeForLog(string value) =>
        string.Concat(value.Select(c => c < 0x20 || c == 0x7F ? ' ' : c));

    public ChatbotController(
        IOllamaService ollamaService,
        IChatSessionStore sessionStore,
        ILogger<ChatbotController> logger)
    {
        _ollamaService = ollamaService ?? throw new ArgumentNullException(nameof(ollamaService));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send a chat message and receive an AI reply.
    /// If <c>SessionId</c> is null or empty a new session UUID is created automatically.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId;

        _logger.LogInformation("Chat request session={SessionId}", SanitizeForLog(sessionId));

        try
        {
            var reply = await _ollamaService.ChatAsync(sessionId, request.Message, cancellationToken);

            var historyCount = _sessionStore.GetHistory(sessionId).Count;
            var tokensUsed = historyCount; // approximation: one unit per stored message

            return Ok(new ChatResponse(sessionId, reply, tokensUsed));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve the message history for a session.
    /// </summary>
    [HttpGet("sessions/{sessionId}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetHistory(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { error = "SessionId is required." });

        if (!_sessionStore.SessionExists(sessionId))
            return NotFound(new { error = "Session not found." });

        var history = _sessionStore.GetHistory(sessionId);
        return Ok(new { sessionId, messages = history });
    }

    /// <summary>
    /// Delete a chat session and its history.
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { error = "SessionId is required." });

        var deleted = _sessionStore.DeleteSession(sessionId);
        if (!deleted)
            return NotFound(new { error = "Session not found." });

        _logger.LogInformation("Session deleted: {SessionId}", SanitizeForLog(sessionId));
        return NoContent();
    }

    /// <summary>
    /// Get the current status of the Ollama backend.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var available = await _ollamaService.IsAvailableAsync(cancellationToken);
        return Ok(new
        {
            ollamaAvailable = available,
            status = available ? "online" : "offline"
        });
    }
}
