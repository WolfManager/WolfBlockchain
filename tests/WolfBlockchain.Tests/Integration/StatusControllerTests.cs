using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Controllers;
using Xunit;

namespace WolfBlockchain.Tests.Integration;

/// <summary>
/// Unit tests for StatusController.
/// </summary>
public class StatusControllerTests
{
    private readonly StatusController _controller;

    public StatusControllerTests()
    {
        _controller = new StatusController();
    }

    [Fact]
    public void GetStatus_ReturnsOkResult()
    {
        // Act
        var result = _controller.GetStatus();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetStatus_ReturnsCorrectProjectName()
    {
        // Act
        var result = _controller.GetStatus() as OkObjectResult;

        // Assert
        Assert.NotNull(result?.Value);
        var value = result.Value!;
        var project = value.GetType().GetProperty("project")?.GetValue(value) as string;
        Assert.Equal("WolfBlockchain", project);
    }

    [Fact]
    public void GetStatus_ReturnsCorrectVersion()
    {
        // Act
        var result = _controller.GetStatus() as OkObjectResult;

        // Assert
        Assert.NotNull(result?.Value);
        var value = result.Value!;
        var version = value.GetType().GetProperty("version")?.GetValue(value) as string;
        Assert.Equal("v2.0.0", version);
    }

    [Fact]
    public void GetStatus_ReturnsProductionReadyStatus()
    {
        // Act
        var result = _controller.GetStatus() as OkObjectResult;

        // Assert
        Assert.NotNull(result?.Value);
        var value = result.Value!;
        var status = value.GetType().GetProperty("status")?.GetValue(value) as string;
        Assert.Equal("Production Ready", status);
    }

    [Fact]
    public void GetStatus_ReturnsNonEmptyFeatures()
    {
        // Act
        var result = _controller.GetStatus() as OkObjectResult;

        // Assert
        Assert.NotNull(result?.Value);
        var value = result.Value!;
        var features = value.GetType().GetProperty("features")?.GetValue(value) as string[];
        Assert.NotNull(features);
        Assert.NotEmpty(features);
    }

    [Fact]
    public void GetStatus_ReturnsNonEmptyComponents()
    {
        // Act
        var result = _controller.GetStatus() as OkObjectResult;

        // Assert
        Assert.NotNull(result?.Value);
        var value = result.Value!;
        var components = value.GetType().GetProperty("components")?.GetValue(value) as string[];
        Assert.NotNull(components);
        Assert.NotEmpty(components);
    }

    [Fact]
    public void GetStatus_ReturnsTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var result = _controller.GetStatus() as OkObjectResult;
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result?.Value);
        var value = result.Value!;
        var timestamp = (DateTime)value.GetType().GetProperty("timestamp")!.GetValue(value)!;
        Assert.InRange(timestamp, before, after);
    }
}
