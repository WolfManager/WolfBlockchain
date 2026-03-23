using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Controllers;

/// <summary>AI model management endpoints</summary>
[ApiController]
[Route("api/[controller]")]
public class AIModelController : ControllerBase
{
    private readonly IAIModelService _modelService;
    private readonly IModelVersionService _versionService;
    private readonly ILogger<AIModelController> _logger;

    public AIModelController(
        IAIModelService modelService,
        IModelVersionService versionService,
        ILogger<AIModelController> logger)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Train batch of models</summary>
    [HttpPost("train/batch")]
    public async Task<IActionResult> TrainBatch([FromBody] List<TrainingDataDto> trainingData)
    {
        try
        {
            if (trainingData == null || trainingData.Count == 0)
                return BadRequest(new { error = "Training data is required" });

            _logger.LogInformation("Starting batch training with {Count} models", trainingData.Count);

            var result = await _modelService.TrainBatchAsync(trainingData);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training batch");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get model by version</summary>
    [HttpGet("{modelId}/{version}")]
    public async Task<IActionResult> GetModel(string modelId, string version)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(version))
                return BadRequest(new { error = "ModelId and version are required" });

            _logger.LogInformation("Getting model {ModelId} version {Version}", modelId, version);

            var model = await _modelService.GetModelAsync(modelId, version);
            return Ok(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Model {modelId} version {version} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get all model versions</summary>
    [HttpGet("{modelId}/versions")]
    public async Task<IActionResult> GetModelVersions(string modelId)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "ModelId is required" });

            _logger.LogInformation("Getting versions for model {ModelId}", modelId);

            var versions = await _modelService.GetModelVersionsAsync(modelId);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model versions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Make prediction</summary>
    [HttpPost("{modelId}/{version}/predict")]
    public async Task<IActionResult> Predict(string modelId, string version, [FromBody] object input)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(version))
                return BadRequest(new { error = "ModelId and version are required" });

            _logger.LogInformation("Making prediction with model {ModelId} version {Version}", modelId, version);

            var prediction = await _modelService.PredictAsync(modelId, version, input);
            return Ok(prediction);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Model {modelId} version {version} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making prediction");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Evaluate model</summary>
    [HttpGet("{modelId}/{version}/evaluate")]
    public async Task<IActionResult> Evaluate(string modelId, string version)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(version))
                return BadRequest(new { error = "ModelId and version are required" });

            _logger.LogInformation("Evaluating model {ModelId} version {Version}", modelId, version);

            var evaluation = await _modelService.EvaluateAsync(modelId, version);
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get model metrics</summary>
    [HttpGet("{modelId}/metrics")]
    public async Task<IActionResult> GetMetrics(string modelId)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "ModelId is required" });

            _logger.LogInformation("Getting metrics for model {ModelId}", modelId);

            var metrics = await _modelService.GetMetricsAsync(modelId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model metrics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Create model version</summary>
    [HttpPost("{modelId}/versions/create")]
    public async Task<IActionResult> CreateVersion(string modelId, [FromBody] ModelMetadataDto metadata)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId) || metadata == null)
                return BadRequest(new { error = "ModelId and metadata are required" });

            _logger.LogInformation("Creating version for model {ModelId}", modelId);

            var version = await _versionService.CreateVersionAsync(modelId, metadata);
            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating model version");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Promote version to environment</summary>
    [HttpPut("{modelId}/{version}/promote")]
    public async Task<IActionResult> PromoteVersion(string modelId, string version, [FromQuery] string environment)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(environment))
                return BadRequest(new { error = "ModelId, version, and environment are required" });

            _logger.LogInformation("Promoting model {ModelId} version {Version} to {Environment}", modelId, version, environment);

            await _versionService.PromoteVersionAsync(modelId, version, environment);
            return Ok(new { message = "Version promoted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting version");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Deprecate version</summary>
    [HttpPut("{modelId}/{version}/deprecate")]
    public async Task<IActionResult> DeprecateVersion(string modelId, string version)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(version))
                return BadRequest(new { error = "ModelId and version are required" });

            _logger.LogInformation("Deprecating model {ModelId} version {Version}", modelId, version);

            await _versionService.DeprecateVersionAsync(modelId, version);
            return Ok(new { message = "Version deprecated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deprecating version");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get version history</summary>
    [HttpGet("{modelId}/history")]
    public async Task<IActionResult> GetHistory(string modelId)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "ModelId is required" });

            _logger.LogInformation("Getting history for model {ModelId}", modelId);

            var history = await _versionService.GetHistoryAsync(modelId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model history");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
