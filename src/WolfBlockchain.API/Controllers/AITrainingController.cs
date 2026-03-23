using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Core;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AITrainingController : ControllerBase
{
    private static AITrainingService _aiService = new AITrainingService();

    /// <summary>
    /// Creeaza un nou model AI
    /// </summary>
    [HttpPost("models/create")]
    public IActionResult CreateModel([FromBody] CreateAIModelRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        if (!Enum.TryParse<AIModelType>(request.Type, out var modelType))
            return BadRequest("Invalid model type");

        var model = _aiService.CreateModel(request.Name, request.Description, modelType, request.OwnerAddress);

        if (model == null)
            return BadRequest("Failed to create model");

        return Ok(new
        {
            success = true,
            modelId = model.ModelId,
            name = model.Name,
            type = model.Type.ToString(),
            createdAt = model.CreatedAt
        });
    }

    /// <summary>
    /// Obtine informatii despre model
    /// </summary>
    [HttpGet("models/{modelId}")]
    public IActionResult GetModel(string modelId)
    {
        var model = _aiService.GetModel(modelId);
        if (model == null)
            return NotFound("Model not found");

        return Ok(new
        {
            modelId = model.ModelId,
            name = model.Name,
            description = model.Description,
            type = model.Type.ToString(),
            ownerAddress = model.OwnerAddress,
            version = model.Version,
            status = model.Status,
            accuracy = model.Accuracy,
            loss = model.Loss,
            createdAt = model.CreatedAt,
            updatedAt = model.UpdatedAt
        });
    }

    /// <summary>
    /// Lista modelele unui utilizator
    /// </summary>
    [HttpGet("models/user/{address}")]
    public IActionResult GetUserModels(string address)
    {
        var models = _aiService.GetUserModels(address);
        var result = models.Select(m => new
        {
            modelId = m.ModelId,
            name = m.Name,
            type = m.Type.ToString(),
            accuracy = m.Accuracy,
            createdAt = m.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Creeaza dataset pentru antrenament
    /// </summary>
    [HttpPost("datasets/create")]
    public IActionResult CreateDataset([FromBody] CreateDatasetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var dataset = _aiService.CreateDataset(request.Name, request.Description, request.OwnerAddress, 
            request.SampleCount, request.FeatureCount);

        if (dataset == null)
            return BadRequest("Failed to create dataset");

        return Ok(new
        {
            success = true,
            datasetId = dataset.DatasetId,
            name = dataset.Name,
            sampleCount = dataset.SampleCount,
            featureCount = dataset.FeatureCount,
            createdAt = dataset.CreatedAt
        });
    }

    /// <summary>
    /// Obtine informatii despre dataset
    /// </summary>
    [HttpGet("datasets/{datasetId}")]
    public IActionResult GetDataset(string datasetId)
    {
        var dataset = _aiService.GetDataset(datasetId);
        if (dataset == null)
            return NotFound("Dataset not found");

        return Ok(new
        {
            datasetId = dataset.DatasetId,
            name = dataset.Name,
            description = dataset.Description,
            ownerAddress = dataset.OwnerAddress,
            sampleCount = dataset.SampleCount,
            featureCount = dataset.FeatureCount,
            sizeMB = dataset.SizeMB,
            format = dataset.Format,
            createdAt = dataset.CreatedAt
        });
    }

    /// <summary>
    /// Lanseaza un job de antrenament
    /// </summary>
    [HttpPost("jobs/start")]
    public IActionResult StartTrainingJob([FromBody] StartTrainingJobRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var job = _aiService.StartTrainingJob(request.ModelId, request.UserAddress, request.DatasetId,
            request.Epochs, request.BatchSize, request.LearningRate);

        if (job == null)
            return BadRequest("Failed to start training job");

        return Ok(new
        {
            success = true,
            jobId = job.JobId,
            modelId = job.ModelId,
            status = job.Status.ToString(),
            estimatedCost = job.TrainingFee,
            epochs = job.Epochs,
            startedAt = job.StartedAt
        });
    }

    /// <summary>
    /// Obtine status job
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public IActionResult GetJob(string jobId)
    {
        var job = _aiService.GetJob(jobId);
        if (job == null)
            return NotFound("Job not found");

        return Ok(new
        {
            jobId = job.JobId,
            modelId = job.ModelId,
            status = job.Status.ToString(),
            progressPercent = job.ProgressPercent,
            epochs = job.Epochs,
            batchSize = job.BatchSize,
            learningRate = job.LearningRate,
            datasetSize = job.DatasetSize,
            trainingFee = job.TrainingFee,
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt,
            durationSeconds = job.DurationSeconds,
            results = job.Results,
            errorMessage = job.ErrorMessage
        });
    }

    /// <summary>
    /// Lista joburi ale unui utilizator
    /// </summary>
    [HttpGet("jobs/user/{address}")]
    public IActionResult GetUserJobs(string address)
    {
        var jobs = _aiService.GetUserJobs(address);
        var result = jobs.Select(j => new
        {
            jobId = j.JobId,
            modelId = j.ModelId,
            status = j.Status.ToString(),
            progressPercent = j.ProgressPercent,
            startedAt = j.StartedAt,
            completedAt = j.CompletedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Actualizeaza progresul jobului
    /// </summary>
    [HttpPut("jobs/{jobId}/progress")]
    public IActionResult UpdateJobProgress(string jobId, [FromBody] UpdateProgressRequest request)
    {
        if (request.ProgressPercent < 0 || request.ProgressPercent > 100)
            return BadRequest("Progress must be between 0 and 100");

        _aiService.UpdateJobProgress(jobId, request.ProgressPercent);
        var job = _aiService.GetJob(jobId);

        return Ok(new
        {
            success = true,
            jobId = jobId,
            progressPercent = job?.ProgressPercent ?? 0,
            status = job?.Status.ToString() ?? "Unknown"
        });
    }

    /// <summary>
    /// Inregistreaza metrici de antrenament
    /// </summary>
    [HttpPost("jobs/{jobId}/metrics")]
    public IActionResult LogMetric(string jobId, [FromBody] LogMetricRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        _aiService.LogMetric(jobId, request.Epoch, request.TrainingLoss, request.ValidationLoss,
            request.TrainingAccuracy, request.ValidationAccuracy);

        return Ok(new { success = true, message = "Metric logged successfully" });
    }

    /// <summary>
    /// Obtine metrici unui job
    /// </summary>
    [HttpGet("jobs/{jobId}/metrics")]
    public IActionResult GetJobMetrics(string jobId)
    {
        var metrics = _aiService.GetJobMetrics(jobId);
        var result = metrics.Select(m => new
        {
            metricId = m.MetricId,
            epoch = m.Epoch,
            trainingLoss = m.TrainingLoss,
            validationLoss = m.ValidationLoss,
            trainingAccuracy = m.TrainingAccuracy,
            validationAccuracy = m.ValidationAccuracy,
            timestamp = m.Timestamp
        });

        return Ok(result);
    }

    /// <summary>
    /// Anuleaza un job
    /// </summary>
    [HttpPost("jobs/{jobId}/cancel")]
    public IActionResult CancelJob(string jobId)
    {
        var success = _aiService.CancelJob(jobId);
        if (!success)
            return BadRequest("Failed to cancel job");

        return Ok(new { success = true, message = "Job cancelled" });
    }

    /// <summary>
    /// Pauzează un job
    /// </summary>
    [HttpPost("jobs/{jobId}/pause")]
    public IActionResult PauseJob(string jobId)
    {
        var success = _aiService.PauseJob(jobId);
        if (!success)
            return BadRequest("Failed to pause job");

        return Ok(new { success = true, message = "Job paused" });
    }

    /// <summary>
    /// Reia un job pausat
    /// </summary>
    [HttpPost("jobs/{jobId}/resume")]
    public IActionResult ResumeJob(string jobId)
    {
        var success = _aiService.ResumeJob(jobId);
        if (!success)
            return BadRequest("Failed to resume job");

        return Ok(new { success = true, message = "Job resumed" });
    }

    /// <summary>
    /// Obtine statistici de antrenament
    /// </summary>
    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        var stats = _aiService.GetTrainingStatistics();
        return Ok(stats);
    }
}

// Request DTOs
public class CreateAIModelRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string OwnerAddress { get; set; } = "";
}

public class CreateDatasetRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string OwnerAddress { get; set; } = "";
    public int SampleCount { get; set; }
    public int FeatureCount { get; set; }
}

public class StartTrainingJobRequest
{
    public string ModelId { get; set; } = "";
    public string UserAddress { get; set; } = "";
    public string DatasetId { get; set; } = "";
    public int Epochs { get; set; } = 10;
    public int BatchSize { get; set; } = 32;
    public decimal LearningRate { get; set; } = 0.001m;
}

public class UpdateProgressRequest
{
    public int ProgressPercent { get; set; }
}

public class LogMetricRequest
{
    public int Epoch { get; set; }
    public decimal TrainingLoss { get; set; }
    public decimal ValidationLoss { get; set; }
    public decimal TrainingAccuracy { get; set; }
    public decimal ValidationAccuracy { get; set; }
}
