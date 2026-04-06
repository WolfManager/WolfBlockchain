using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Chatbot endpoints — powered by the configured IChatService provider.
/// Supports Option A (Ollama), Option B (OpenAI), or Option C (Mock).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send a message to the AI chatbot and receive a reply.
    /// </summary>
    /// <remarks>
    /// POST /api/chat/message
    /// {
    ///   "message": "What is the current block height?",
    ///   "sessionId": "optional-session-id",
    ///   "model": "llama3"
    /// }
    /// </remarks>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            _logger.LogInformation("Chat request received via provider {Provider}", _chatService.ProviderName);

            var response = await _chatService.SendMessageAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid chat request");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Chat provider {Provider} is unreachable", _chatService.ProviderName);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = $"AI provider '{_chatService.ProviderName}' is currently unavailable." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing chat message");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// List AI models available from the configured chat provider.
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> ListModels(CancellationToken cancellationToken)
    {
        try
        {
            var models = await _chatService.ListModelsAsync(cancellationToken);
            return Ok(new
            {
                provider = _chatService.ProviderName,
                models
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing models from provider {Provider}", _chatService.ProviderName);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = $"Could not retrieve models from '{_chatService.ProviderName}'." });
        }
    }

    /// <summary>
    /// Check whether the configured AI provider is reachable.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var available = await _chatService.IsAvailableAsync(cancellationToken);
        return Ok(new
        {
            provider = _chatService.ProviderName,
            available
        });
    }

    /// <summary>
    /// Technical analysis: returns metadata about ALL available AI provider options
    /// so that the administrator can compare and choose the best fit.
    /// </summary>
    [HttpGet("options")]
    public IActionResult GetProviderOptions()
    {
        var options = ChatProviderRegistry.GetAllOptions();
        return Ok(new
        {
            description = "All available AI provider options for the chatbot feature.",
            activeProvider = _chatService.ProviderName,
            options
        });
    }
}
