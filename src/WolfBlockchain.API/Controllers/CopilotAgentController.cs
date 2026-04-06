using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Agents.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Copilot Coding Agent endpoints - a development tool for code generation,
/// analysis, debugging and architectural decisions. Completely independent of Ollama.
/// </summary>
[ApiController]
[Route("api/copilot-agent")]
public class CopilotAgentController : ControllerBase
{
    private readonly ICodingAgentService _codingAgent;
    private readonly ILogger<CopilotAgentController> _logger;

    public CopilotAgentController(ICodingAgentService codingAgent, ILogger<CopilotAgentController> logger)
    {
        _codingAgent = codingAgent ?? throw new ArgumentNullException(nameof(codingAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Get information about the active coding agent provider.</summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            provider = _codingAgent.ProviderName,
            independent = true,
            capabilities = new[] { "code-generation", "code-analysis", "debugging", "architecture-advice" }
        });
    }

    /// <summary>Generate code based on a description.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateCode([FromBody] CodeGenerationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Description))
            return BadRequest(new { error = "Description is required." });

        try
        {
            _logger.LogInformation("CopilotAgent: generate code request");
            var response = await _codingAgent.GenerateCodeAsync(request.Description, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CopilotAgent: error generating code");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Analyze code for quality, patterns and issues.</summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeCode([FromBody] CodeAnalysisRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
            return BadRequest(new { error = "Code is required." });

        try
        {
            _logger.LogInformation("CopilotAgent: analyze code request");
            var response = await _codingAgent.AnalyzeCodeAsync(request.Code, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CopilotAgent: error analyzing code");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Debug code and suggest a fix for the described issue.</summary>
    [HttpPost("debug")]
    public async Task<IActionResult> DebugCode([FromBody] CodeDebugRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
            return BadRequest(new { error = "Code is required." });

        if (string.IsNullOrWhiteSpace(request.Issue))
            return BadRequest(new { error = "Issue description is required." });

        try
        {
            _logger.LogInformation("CopilotAgent: debug code request");
            var response = await _codingAgent.DebugCodeAsync(request.Code, request.Issue, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CopilotAgent: error debugging code");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get architectural recommendations for the described system.</summary>
    [HttpPost("architecture")]
    public async Task<IActionResult> AdviseArchitecture([FromBody] ArchitectureRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Description))
            return BadRequest(new { error = "Description is required." });

        try
        {
            _logger.LogInformation("CopilotAgent: architecture advice request");
            var response = await _codingAgent.AdviseArchitectureAsync(request.Description, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CopilotAgent: error providing architecture advice");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public sealed record CodeGenerationRequest(string Description);
public sealed record CodeAnalysisRequest(string Code);
public sealed record CodeDebugRequest(string Code, string Issue);
public sealed record ArchitectureRequest(string Description);
