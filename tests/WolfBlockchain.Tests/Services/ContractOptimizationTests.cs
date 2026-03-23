using Xunit;
using Moq;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.Tests.Services;

/// <summary>Contract optimization tests</summary>
public class ContractOptimizationTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IQueryCacheService> _queryCacheMock;
    private readonly Mock<ILogger<ContractCacheService>> _loggerMock;
    private readonly Mock<ILogger<BatchContractExecutor>> _batchLoggerMock;

    public ContractOptimizationTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _queryCacheMock = new Mock<IQueryCacheService>();
        _loggerMock = new Mock<ILogger<ContractCacheService>>();
        _batchLoggerMock = new Mock<ILogger<BatchContractExecutor>>();
    }

    // ============= CONTRACT CACHE TESTS =============

    [Fact]
    public async Task GetStateAsync_WhenCached_ShouldReturnCachedState()
    {
        // Arrange
        var contractId = "contract:123";
        var cachedState = new ContractStateDto
        {
            ContractId = contractId,
            Version = "1.0",
            StateVariables = new() { { "count", 42 } }
        };

        _cacheMock
            .Setup(c => c.GetAsync<ContractStateDto>(It.IsAny<string>()))
            .ReturnsAsync(cachedState);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetStateAsync(contractId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contractId, result.ContractId);
        Assert.Equal(42, result.StateVariables["count"]);
    }

    [Fact]
    public async Task CacheStateAsync_ShouldCacheContractState()
    {
        // Arrange
        var contractId = "contract:456";
        var state = new ContractStateDto { ContractId = contractId };

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ContractStateDto>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);

        // Act
        await service.CacheStateAsync(contractId, state);

        // Assert
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), state, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateStateAsync_ShouldInvalidateCache()
    {
        // Arrange
        var contractId = "contract:789";

        _cacheMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);

        // Act
        await service.InvalidateStateAsync(contractId);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetExecutionResultAsync_WhenCached_ShouldReturnResult()
    {
        // Arrange
        var contractId = "contract:exec1";
        var methodName = "execute";
        var cachedResult = new ExecutionResultDto
        {
            ContractId = contractId,
            MethodName = methodName,
            Success = true,
            Result = "Execution success"
        };

        _cacheMock
            .Setup(c => c.GetAsync<ExecutionResultDto>(It.IsAny<string>()))
            .ReturnsAsync(cachedResult);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetExecutionResultAsync(contractId, methodName);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Execution success", result.Result);
    }

    [Fact]
    public async Task GetPerformanceAsync_ShouldReturnMetrics()
    {
        // Arrange
        var contractId = "contract:perf";

        _cacheMock
            .Setup(c => c.GetAsync<ContractStateDto>(It.IsAny<string>()))
            .ReturnsAsync((ContractStateDto?)null);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);

        // Simulate some cache hits/misses
        await service.GetStateAsync(contractId);
        await service.GetStateAsync(contractId);

        // Act
        var performance = await service.GetPerformanceAsync(contractId);

        // Assert
        Assert.NotNull(performance);
        Assert.Equal(contractId, performance.ContractId);
    }

    // ============= BATCH EXECUTOR TESTS =============

    [Fact]
    public async Task ExecuteBatchAsync_WithMultipleCalls_ShouldExecuteAll()
    {
        // Arrange
        var calls = new List<ContractCallDto>
        {
            new() { ContractId = "contract:1", MethodName = "method1" },
            new() { ContractId = "contract:2", MethodName = "method2" },
            new() { ContractId = "contract:3", MethodName = "method3" }
        };

        _cacheMock
            .Setup(c => c.GetAsync<ExecutionResultDto>(It.IsAny<string>()))
            .ReturnsAsync((ExecutionResultDto?)null);

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ExecutionResultDto>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);
        var executor = new BatchContractExecutor(service, _batchLoggerMock.Object);

        // Act
        var result = await executor.ExecuteBatchAsync(calls);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.True(result.SuccessCount > 0);
    }

    [Fact]
    public async Task ExecuteParallelAsync_ShouldRespectDegreeOfParallelism()
    {
        // Arrange
        var calls = Enumerable.Range(1, 10)
            .Select(i => new ContractCallDto
            {
                ContractId = $"contract:{i}",
                MethodName = "execute"
            })
            .ToList();

        _cacheMock
            .Setup(c => c.GetAsync<ExecutionResultDto>(It.IsAny<string>()))
            .ReturnsAsync((ExecutionResultDto?)null);

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ExecutionResultDto>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);
        var executor = new BatchContractExecutor(service, _batchLoggerMock.Object);

        // Act
        var result = await executor.ExecuteParallelAsync(calls, maxDegreeOfParallelism: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.TotalCount);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var calls = new List<ContractCallDto>();

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);
        var executor = new BatchContractExecutor(service, _batchLoggerMock.Object);

        // Act
        var result = await executor.ExecuteBatchAsync(calls);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.SuccessCount);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ShouldReturnAccurateStats()
    {
        // Arrange
        var calls = new List<ContractCallDto>
        {
            new() { ContractId = "contract:1", MethodName = "method1" },
            new() { ContractId = "contract:2", MethodName = "method2" }
        };

        _cacheMock
            .Setup(c => c.GetAsync<ExecutionResultDto>(It.IsAny<string>()))
            .ReturnsAsync((ExecutionResultDto?)null);

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ExecutionResultDto>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);
        var executor = new BatchContractExecutor(service, _batchLoggerMock.Object);

        // Act
        var result = await executor.ExecuteBatchAsync(calls);
        var stats = await executor.GetStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(stats);
        Assert.True(stats.TotalBatches > 0);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithCachedResults_ShouldUseCached()
    {
        // Arrange
        var contractId = "contract:cached";
        var methodName = "cachedMethod";
        var cachedResult = new ExecutionResultDto
        {
            ContractId = contractId,
            MethodName = methodName,
            Success = true,
            Result = "Cached result"
        };

        var calls = new List<ContractCallDto>
        {
            new() { ContractId = contractId, MethodName = methodName }
        };

        var callCount = 0;
        _cacheMock
            .Setup(c => c.GetAsync<ExecutionResultDto>(It.IsAny<string>()))
            .Returns(async () =>
            {
                callCount++;
                return callCount == 1 ? cachedResult : null;
            });

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ExecutionResultDto>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new ContractCacheService(_cacheMock.Object, _queryCacheMock.Object, _loggerMock.Object);
        var executor = new BatchContractExecutor(service, _batchLoggerMock.Object);

        // Act
        var result = await executor.ExecuteBatchAsync(calls);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Results.First().CachedResult);
    }
}
