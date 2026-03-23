using Xunit;
using WolfBlockchain.API.Validation;

namespace WolfBlockchain.Tests;

public class InputValidatorTests
{
    [Fact]
    public void ValidateTokenName_WithValidName_ShouldPass()
    {
        var (isValid, error) = BlazorInputValidator.ValidateTokenName("TestToken");
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateTokenName_WithMaliciousInput_ShouldFail()
    {
        var (isValid, _) = BlazorInputValidator.ValidateTokenName("<script>alert('xss')</script>");
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateTokenSymbol_WithValidSymbol_ShouldPass()
    {
        var (isValid, error) = BlazorInputValidator.ValidateTokenSymbol("WOLF");
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateTokenSupply_WithValidAmount_ShouldPass()
    {
        var (isValid, error) = BlazorInputValidator.ValidateTokenSupply(1_000_000);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateTokenSupply_WithZero_ShouldFail()
    {
        var (isValid, _) = BlazorInputValidator.ValidateTokenSupply(0);
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateDatasetUrl_WithValidUrl_ShouldPass()
    {
        var (isValid, error) = BlazorInputValidator.ValidateDatasetUrl("https://example.com/data");
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateDatasetUrl_WithInvalidUrl_ShouldFail()
    {
        var (isValid, _) = BlazorInputValidator.ValidateDatasetUrl("not-a-url");
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateJsonParameters_WithValidJson_ShouldPass()
    {
        var (isValid, error) = BlazorInputValidator.ValidateJsonParameters(@"{ ""key"": ""value"" }");
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateJsonParameters_WithInvalidJson_ShouldFail()
    {
        var (isValid, _) = BlazorInputValidator.ValidateJsonParameters("not json");
        Assert.False(isValid);
    }
}
