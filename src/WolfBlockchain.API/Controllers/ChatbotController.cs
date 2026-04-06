using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Chatbot API – exposes the Ollama-backed (or mock) conversational AI assistant.
/// All endpoints except /status are protected by JWT authentication.
/// </summary>
[ApiController]
[Route("api/chatbot")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IOllamaService _ollamaService;
    private readonly IChatSessionStore _sessionStore;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        IOllamaService ollamaService,
        IChatSessionStore sessionStore,
        ILogger<ChatbotController> logger)
    {
        _ollamaService = ollamaService ?? throw new ArgumentNullException(nameof(ollamaService));
        _sessionStore  = sessionStore  ?? throw new ArgumentNullException(nameof(sessionStore));
        _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
    }

    // ============= ENDPOINTS =============

    /// <summary>
    /// Send a chat message to the AI assistant and receive a reply.
    /// A session ID can be provided to continue an existing conversation;
    /// if omitted a new session is created automatically.
    /// </summary>
    /// <param name="request">The chat request containing the user message and optional session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, error = "Message is required." });

        _logger.LogInformation(
            "Chatbot chat request – session {SessionId}",
            LogSanitizer.SanitizeForLog(request.SessionId));

        var response = await _ollamaService.ChatAsync(request, cancellationToken);

        if (!response.Success)
        {
            _logger.LogWarning(
                "Chatbot chat failed – session {SessionId} error {Error}",
                LogSanitizer.SanitizeForLog(response.SessionId),
                LogSanitizer.SanitizeForLog(response.Error));

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                error = response.Error,
                sessionId = response.SessionId
            });
        }

        return Ok(new
        {
            success = true,
            reply = response.Reply,
            sessionId = response.SessionId,
            model = response.Model
        });
    }

    /// <summary>
    /// Retrieve the full message history for a session.
    /// </summary>
    /// <param name="id">Session identifier.</param>
    [HttpGet("sessions/{id}/history")]
    public IActionResult GetSessionHistory(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { success = false, error = "Session ID is required." });

        var session = _sessionStore.GetSession(id);
        if (session == null)
            return NotFound(new { success = false, error = "Session not found." });

        return Ok(new
        {
            sessionId = session.SessionId,
            createdAtUtc = session.CreatedAtUtc,
            lastActivityUtc = session.LastActivityUtc,
            messageCount = session.Messages.Count,
            messages = session.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content,
                timestampUtc = m.TimestampUtc
            })
        });
    }

    /// <summary>
    /// Delete a chat session and its history.
    /// </summary>
    /// <param name="id">Session identifier.</param>
    [HttpDelete("sessions/{id}")]
    public IActionResult DeleteSession(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { success = false, error = "Session ID is required." });

        _logger.LogInformation("Chatbot: deleting session {SessionId}", LogSanitizer.SanitizeForLog(id));

        var deleted = _sessionStore.DeleteSession(id);
        if (!deleted)
            return NotFound(new { success = false, error = "Session not found." });

        return Ok(new { success = true, message = "Session deleted." });
    }

    /// <summary>
    /// Returns Ollama backend availability and session statistics.
    /// This endpoint is anonymous to allow health-check tooling.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var available = await _ollamaService.IsAvailableAsync(cancellationToken);
        var models    = available
            ? await _ollamaService.ListModelsAsync(cancellationToken)
            : Array.Empty<string>();

        return Ok(new
        {
            backend        = _ollamaService.BackendName,
            ollamaAvailable = available,
            activeSessions = _sessionStore.ActiveSessionCount,
            models
        });
    }
}
