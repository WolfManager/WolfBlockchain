using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>Endpoints for interacting with the local Ollama AI provider.</summary>
[ApiController]
[Route("api/[controller]")]
public class OllamaController : ControllerBase
{
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<OllamaController> _logger;

    public OllamaController(IOllamaService ollamaService, ILogger<OllamaController> logger)
    {
        _ollamaService = ollamaService ?? throw new ArgumentNullException(nameof(ollamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Check whether the Ollama server is reachable.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var available = await _ollamaService.IsAvailableAsync(cancellationToken);
        return Ok(new { available });
    }

    /// <summary>List all models available on the local Ollama server.</summary>
    [HttpGet("models")]
    public async Task<IActionResult> ListModels(CancellationToken cancellationToken)
    {
        try
        {
            var models = await _ollamaService.ListModelsAsync(cancellationToken);
            return Ok(models);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list Ollama models");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Ollama server is unavailable." });
        }
    }

    /// <summary>Generate a text completion for the given prompt.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] OllamaGenerateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
            return BadRequest(new { error = "Prompt is required." });

        try
        {
            var result = await _ollamaService.GenerateAsync(request.Prompt, request.Model, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama generate request failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Ollama server is unavailable." });
        }
    }

    /// <summary>Send a chat request and receive a response.</summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] OllamaChatRequest request, CancellationToken cancellationToken)
    {
        if (request?.Messages == null || request.Messages.Count == 0)
            return BadRequest(new { error = "At least one message is required." });

        try
        {
            var result = await _ollamaService.ChatAsync(request.Messages, request.Model, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama chat request failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Ollama server is unavailable." });
        }
    }
}
