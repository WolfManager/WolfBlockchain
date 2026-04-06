using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Simplified chat API that works with any IChatService backend
/// (Ollama, OpenAI, or Mock).  Use this endpoint for stateless, single-turn
/// interactions where session management is not required.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send a single message and receive an AI reply.
    /// An optional session ID can be included to maintain conversation context.
    /// </summary>
    /// <param name="request">Chat request with a user message and optional session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, error = "Message is required." });

        _logger.LogInformation(
            "Chat request via {Backend} – session {SessionId}",
            _chatService.BackendName,
            LogSanitizer.SanitizeForLog(request.SessionId));

        var response = await _chatService.ChatAsync(request, cancellationToken);

        if (!response.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                error   = response.Error,
                backend = _chatService.BackendName
            });
        }

        return Ok(new
        {
            success   = true,
            reply     = response.Reply,
            sessionId = response.SessionId,
            model     = response.Model,
            backend   = _chatService.BackendName
        });
    }

    /// <summary>
    /// Returns the name of the active chat backend.
    /// </summary>
    [HttpGet("backend")]
    [AllowAnonymous]
    public IActionResult GetBackend()
    {
        return Ok(new { backend = _chatService.BackendName });
    }
}
