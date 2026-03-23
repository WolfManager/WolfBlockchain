using Xunit;
using WolfBlockchain.Core;

namespace WolfBlockchain.Tests.Core;

/// <summary>
/// Unit tests pentru WolfCoinManager - PRODUCTION GRADE
/// </summary>
public class WolfCoinManagerTests
{
    private const string Owner = "Founders";

    [Fact]
    public void GetBalance_WithNewAddress_ReturnsZero()
    {
        var manager = new WolfCoinManager(Owner);

        var balance = manager.GetWolfCoinBalance("NewAddress");

        Assert.Equal(0, balance);
    }

    [Fact]
    public void Transfer_WithSufficientBalance_Succeeds()
    {
        var manager = new WolfCoinManager(Owner);
        manager.InitializeWolfCoin();

        var result = manager.TransferWolfCoin(Owner, "Address2", 100m);

        Assert.True(result);
        var balance = manager.GetWolfCoinBalance("Address2");
        Assert.Equal(100m, balance);
    }

    [Fact]
    public void Stake_WithValidAmount_Succeeds()
    {
        var manager = new WolfCoinManager(Owner);
        manager.InitializeWolfCoin();

        var result = manager.StakeWolfCoin(Owner, 1000m);

        Assert.True(result);
    }

    [Fact]
    public void Unstake_WithValidAmount_Succeeds()
    {
        var manager = new WolfCoinManager(Owner);
        manager.InitializeWolfCoin();
        manager.StakeWolfCoin(Owner, 1000m);

        var result = manager.UnstakeWolfCoin(Owner);

        Assert.NotNull(result);
        Assert.True(result > 0);
    }

    [Fact]
    public void GetStakingRewards_WithActiveStake_ReturnsRewards()
    {
        var manager = new WolfCoinManager(Owner);
        manager.InitializeWolfCoin();
        manager.StakeWolfCoin(Owner, 1000m);

        var rewards = manager.GetStakingRewards(Owner);

        Assert.True(rewards >= 0);
    }

    [Fact]
    public void GetCirculatingSupply_ReturnsValidAmount()
    {
        var manager = new WolfCoinManager(Owner);
        manager.InitializeWolfCoin();

        var supply = manager.GetCirculatingSupply();

        Assert.True(supply > 0);
        Assert.True(supply <= WolfCoin.TOTAL_SUPPLY);
    }
}
