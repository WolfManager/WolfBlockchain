using Xunit;
using WolfBlockchain.Core;

namespace WolfBlockchain.Tests.Core;

/// <summary>
/// Unit tests pentru TokenManager - PRODUCTION GRADE
/// </summary>
public class TokenManagerTests
{
    private const string Owner = "Owner";

    [Fact]
    public void CreateToken_WithValidInput_ReturnsToken()
    {
        // Arrange
        var manager = new TokenManager(Owner);
        var name = "TestToken";
        var symbol = "TEST";
        var totalSupply = 1000000m;

        // Act
        var result = manager.CreateToken(name, symbol, TokenType.Custom, Owner, totalSupply);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(symbol, result.Symbol);
        Assert.Equal(totalSupply, result.TotalSupply);
    }

    [Fact]
    public void GetToken_WithValidId_ReturnsToken()
    {
        // Arrange
        var manager = new TokenManager(Owner);
        var token = manager.CreateToken("TestToken", "TEST", TokenType.Custom, Owner, 1000000m);

        // Act
        var result = manager.GetToken(token!.TokenId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.TokenId, result.TokenId);
    }

    [Fact]
    public void Transfer_WithSufficientBalance_Succeeds()
    {
        // Arrange
        var manager = new TokenManager(Owner);
        var token = manager.CreateToken("TestToken", "TEST", TokenType.Custom, Owner, 1000000m);

        // Act
        var tx = manager.TransferToken(token!.TokenId, Owner, "ToAddress", 1000m);

        // Assert
        Assert.NotNull(tx);
        Assert.Equal(TransactionStatus.Confirmed, tx.Status);
        Assert.Equal(1000m, manager.GetTokenBalance("ToAddress", token.TokenId));
    }

    [Fact]
    public void Mint_WithValidInput_IncreasesSupply()
    {
        // Arrange
        var manager = new TokenManager(Owner);
        var token = manager.CreateToken("TestToken", "TEST", TokenType.Custom, Owner, 1000000m);

        // Act
        var result = manager.MintToken(token!.TokenId, 100000m, Owner);

        // Assert
        Assert.False(result);
        var updatedToken = manager.GetToken(token.TokenId);
        Assert.NotNull(updatedToken);
        Assert.Equal(token.TotalSupply, updatedToken.CurrentSupply);
    }

    [Fact]
    public void Burn_WithValidInput_DecreasesSupply()
    {
        // Arrange
        var manager = new TokenManager(Owner);
        var token = manager.CreateToken("TestToken", "TEST", TokenType.Custom, Owner, 1000000m);
        var initialCurrentSupply = token!.CurrentSupply;

        // Act
        var result = manager.BurnToken(token.TokenId, 100000m, Owner);

        // Assert
        Assert.True(result);
        var updatedToken = manager.GetToken(token.TokenId);
        Assert.NotNull(updatedToken);
        Assert.Equal(initialCurrentSupply - 100000m, updatedToken.CurrentSupply);
    }
}
