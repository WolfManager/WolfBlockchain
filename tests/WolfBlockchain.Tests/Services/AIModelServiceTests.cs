using Xunit;
using Moq;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.Tests.Services;

/// <summary>AI model service tests</summary>
public class AIModelServiceTests
{
    private readonly Mock<ILogger<AIModelService>> _loggerMock;
    private readonly AIModelService _service;

    public AIModelServiceTests()
    {
        _loggerMock = new Mock<ILogger<AIModelService>>();
        _service = new AIModelService(_loggerMock.Object);
    }

    // ============= BATCH TRAINING TESTS =============

    [Fact]
    public async Task TrainBatchAsync_WithValidData_ShouldTrainSuccessfully()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1", Epochs = 10, LearningRate = 0.001 },
            new() { ModelId = "model2", ModelName = "Model 2", Epochs = 10, LearningRate = 0.001 }
        };

        // Act
        var result = await _service.TrainBatchAsync(trainingData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalModels);
        Assert.True(result.SuccessfulModels > 0);
    }

    [Fact]
    public async Task TrainBatchAsync_ShouldGenerateAccuracyMetrics()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        // Act
        var result = await _service.TrainBatchAsync(trainingData);

        // Assert
        Assert.NotNull(result.Results);
        Assert.True(result.Results[0].Accuracy > 0);
        Assert.True(result.Results[0].Accuracy <= 1);
    }

    [Fact]
    public async Task TrainBatchAsync_ShouldTrackTrainingTime()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        // Act
        var result = await _service.TrainBatchAsync(trainingData);

        // Assert
        Assert.True(result.TotalTrainingTimeMs >= 0);
    }

    // ============= GET MODEL TESTS =============

    [Fact]
    public async Task GetModelAsync_AfterTraining_ShouldReturnModel()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        await _service.TrainBatchAsync(trainingData);

        // Act & Assert - Should not throw
        var versions = await _service.GetModelVersionsAsync("model1");
        Assert.NotEmpty(versions);
    }

    [Fact]
    public async Task GetModelAsync_WithInvalidVersion_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetModelAsync("invalid", "v1"));
    }

    // ============= VERSION TESTS =============

    [Fact]
    public async Task GetModelVersionsAsync_ShouldReturnAllVersions()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        await _service.TrainBatchAsync(trainingData);

        // Act
        var versions = await _service.GetModelVersionsAsync("model1");

        // Assert
        Assert.NotEmpty(versions);
        Assert.True(versions.Count > 0);
    }

    // ============= PREDICTION TESTS =============

    [Fact]
    public async Task PredictAsync_ShouldReturnPrediction()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        var trainResult = await _service.TrainBatchAsync(trainingData);
        var version = trainResult.Results[0].Version!;

        // Act
        var prediction = await _service.PredictAsync("model1", version, new { test = "data" });

        // Assert
        Assert.NotNull(prediction);
        Assert.Equal("model1", prediction.ModelId);
        Assert.True(prediction.Confidence > 0);
    }

    [Fact]
    public async Task PredictAsync_WithInvalidModel_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.PredictAsync("invalid", "v1", new { }));
    }

    // ============= EVALUATION TESTS =============

    [Fact]
    public async Task EvaluateAsync_ShouldReturnEvaluation()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        var trainResult = await _service.TrainBatchAsync(trainingData);
        var version = trainResult.Results[0].Version!;

        // Act
        var evaluation = await _service.EvaluateAsync("model1", version);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.Accuracy > 0);
        Assert.True(evaluation.Precision > 0);
    }

    // ============= METRICS TESTS =============

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        var trainingData = new List<TrainingDataDto>
        {
            new() { ModelId = "model1", ModelName = "Model 1" }
        };

        await _service.TrainBatchAsync(trainingData);

        // Act
        var metrics = await _service.GetMetricsAsync("model1");

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal("model1", metrics.ModelId);
        Assert.True(metrics.TotalVersions > 0);
    }

    // ============= NULL HANDLING TESTS =============

    [Fact]
    public async Task TrainBatchAsync_WithNull_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.TrainBatchAsync(null!));
    }

    [Fact]
    public async Task GetModelAsync_WithNullId_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GetModelAsync(null!, "v1"));
    }
}

/// <summary>Model version service tests</summary>
public class ModelVersionServiceTests
{
    private readonly Mock<IAIModelService> _modelServiceMock;
    private readonly Mock<ILogger<ModelVersionService>> _loggerMock;
    private readonly ModelVersionService _service;

    public ModelVersionServiceTests()
    {
        _modelServiceMock = new Mock<IAIModelService>();
        _loggerMock = new Mock<ILogger<ModelVersionService>>();
        _service = new ModelVersionService(_modelServiceMock.Object, _loggerMock.Object);
    }

    // ============= CREATE VERSION TESTS =============

    [Fact]
    public async Task CreateVersionAsync_ShouldCreateVersion()
    {
        // Arrange
        var metadata = new ModelMetadataDto
        {
            Name = "Test Model",
            Description = "Test Description",
            ModelType = "Classification"
        };

        // Act
        var version = await _service.CreateVersionAsync("model1", metadata);

        // Assert
        Assert.NotNull(version);
        Assert.Equal("model1", version.ModelId);
        Assert.NotEmpty(version.Version);
    }

    // ============= PROMOTE VERSION TESTS =============

    [Fact]
    public async Task PromoteVersionAsync_ShouldPromoteVersion()
    {
        // Arrange
        var metadata = new ModelMetadataDto { Name = "Test Model" };
        var createdVersion = await _service.CreateVersionAsync("model1", metadata);

        // Act
        await _service.PromoteVersionAsync("model1", createdVersion.Version, "Production");

        // Assert - Should not throw
        var history = await _service.GetHistoryAsync("model1");
        Assert.Contains(history, h => h.Action.Contains("Production"));
    }

    // ============= DEPRECATE VERSION TESTS =============

    [Fact]
    public async Task DeprecateVersionAsync_ShouldDeprecateVersion()
    {
        // Arrange
        var metadata = new ModelMetadataDto { Name = "Test Model" };
        var createdVersion = await _service.CreateVersionAsync("model1", metadata);

        // Act
        await _service.DeprecateVersionAsync("model1", createdVersion.Version);

        // Assert
        var history = await _service.GetHistoryAsync("model1");
        Assert.Contains(history, h => h.Action == "Deprecated");
    }

    // ============= HISTORY TESTS =============

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAllActions()
    {
        // Arrange
        var metadata = new ModelMetadataDto { Name = "Test Model" };
        var version = await _service.CreateVersionAsync("model1", metadata);
        await _service.PromoteVersionAsync("model1", version.Version, "Staging");
        await _service.DeprecateVersionAsync("model1", version.Version);

        // Act
        var history = await _service.GetHistoryAsync("model1");

        // Assert
        Assert.NotEmpty(history);
        Assert.True(history.Count >= 3); // Created, Promoted, Deprecated
    }

    // ============= NULL HANDLING TESTS =============

    [Fact]
    public async Task CreateVersionAsync_WithNull_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateVersionAsync(null!, new ModelMetadataDto()));
    }

    [Fact]
    public async Task PromoteVersionAsync_WithNullEnvironment_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.PromoteVersionAsync("model1", "v1", null!));
    }
}
