namespace WolfBlockchain.Core;

/// <summary>
/// Manager pentru Wolf Coin si economia blockchain-ului
/// </summary>
public class WolfCoinManager
{
    private Token? _wolfCoinToken;
    private Dictionary<string, decimal> _wolfCoinBalances;
    private WolfCoinStaking _staking;
    private List<WolfCoinStaking.StakeRecord> _allStakes;
    
    public string OwnerAddress { get; set; }

    public WolfCoinManager(string ownerAddress)
    {
        OwnerAddress = ownerAddress;
        _wolfCoinBalances = new Dictionary<string, decimal>();
        _staking = new WolfCoinStaking();
        _allStakes = new List<WolfCoinStaking.StakeRecord>();
    }

    /// <summary>Initializeaza Wolf Coin cu distributia initiala</summary>
    public bool InitializeWolfCoin()
    {
        _wolfCoinToken = WolfCoin.CreateWolfCoin(OwnerAddress);

        if (_wolfCoinToken == null)
            return false;

        // Seteaza balanțele initiale pentru distributie
        var distribution = WolfCoinDistribution.GetInitialDistribution();
        foreach (var kvp in distribution)
        {
            _wolfCoinBalances[kvp.Key] = kvp.Value;
        }

        Console.WriteLine("Wolf Coin initialized with distribution:");
        foreach (var kvp in distribution)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} WOLF");
        }

        return true;
    }

    /// <summary>Obtine tokenul Wolf Coin</summary>
    public Token? GetWolfCoinToken()
    {
        return _wolfCoinToken;
    }

    /// <summary>Obtine balanța de WOLF pentru o adresa</summary>
    public decimal GetWolfCoinBalance(string address)
    {
        return _wolfCoinBalances.ContainsKey(address) ? _wolfCoinBalances[address] : 0;
    }

    /// <summary>Transfer WOLF coins intre adrese</summary>
    public bool TransferWolfCoin(string from, string to, decimal amount)
    {
        if (_wolfCoinToken == null)
        {
            Console.WriteLine("Wolf Coin not initialized");
            return false;
        }

        var fromBalance = GetWolfCoinBalance(from);
        if (fromBalance < amount)
        {
            Console.WriteLine($"Insufficient WOLF balance. Have: {fromBalance}, Need: {amount}");
            return false;
        }

        _wolfCoinBalances[from] -= amount;
        if (!_wolfCoinBalances.ContainsKey(to))
            _wolfCoinBalances[to] = 0;
        _wolfCoinBalances[to] += amount;

        Console.WriteLine($"Transferred {amount} WOLF from {from} to {to}");
        return true;
    }

    /// <summary>Seteaza taxa de tranzactie si o plateste catre treasury</summary>
    public bool ApplyTransactionFee(string payer, decimal amount, string treasuryAddress)
    {
        var fee = WolfCoin.CalculateTransactionFee(amount);
        
        var payerBalance = GetWolfCoinBalance(payer);
        if (payerBalance < fee)
        {
            Console.WriteLine($"Insufficient balance for transaction fee");
            return false;
        }

        _wolfCoinBalances[payer] -= fee;
        if (!_wolfCoinBalances.ContainsKey(treasuryAddress))
            _wolfCoinBalances[treasuryAddress] = 0;
        _wolfCoinBalances[treasuryAddress] += fee;

        Console.WriteLine($"Transaction fee ({fee} WOLF) applied and sent to treasury");
        return true;
    }

    /// <summary>Acordare recompense mining</summary>
    public bool RewardMiner(string minerAddress, int blockNumber, int difficulty)
    {
        if (_wolfCoinToken == null)
            return false;

        var reward = WolfCoin.CalculateMiningReward(blockNumber, difficulty);

        if (!_wolfCoinBalances.ContainsKey(minerAddress))
            _wolfCoinBalances[minerAddress] = 0;

        _wolfCoinBalances[minerAddress] += reward;

        Console.WriteLine($"Mining reward ({reward} WOLF) sent to {minerAddress}");
        return true;
    }

    /// <summary>Stake WOLF coins</summary>
    public bool StakeWolfCoin(string stakerId, decimal amount)
    {
        var balance = GetWolfCoinBalance(stakerId);
        if (balance < amount)
        {
            Console.WriteLine($"Insufficient WOLF balance for staking");
            return false;
        }

        var stakeRecord = _staking.Stake(stakerId, amount);
        if (stakeRecord == null)
            return false;

        _allStakes.Add(stakeRecord);

        // Deduca din balanța activa
        _wolfCoinBalances[stakerId] -= amount;

        Console.WriteLine($"Staked {amount} WOLF for {stakerId}");
        return true;
    }

    /// <summary>Unstake si obtine rewarduri</summary>
    public decimal? UnstakeWolfCoin(string stakerId)
    {
        var totalWithRewards = _staking.Unstake(stakerId);
        if (totalWithRewards == null)
            return null;

        if (!_wolfCoinBalances.ContainsKey(stakerId))
            _wolfCoinBalances[stakerId] = 0;

        _wolfCoinBalances[stakerId] += totalWithRewards.Value;

        Console.WriteLine($"Unstaked {totalWithRewards} WOLF for {stakerId} (including rewards)");
        return totalWithRewards;
    }

    /// <summary>Obtine rewardurile de staking</summary>
    public decimal GetStakingRewards(string stakerId)
    {
        return _staking.GetStakingRewards(stakerId);
    }

    /// <summary>Obtine circulating supply (total - locked)</summary>
    public decimal GetCirculatingSupply()
    {
        var total = 0m;
        foreach (var balance in _wolfCoinBalances.Values)
        {
            total += balance;
        }
        return total;
    }

    /// <summary>Obtine statistica blockchain</summary>
    public Dictionary<string, object> GetWolfCoinStatistics()
    {
        return new Dictionary<string, object>
        {
            { "TotalSupply", WolfCoin.TOTAL_SUPPLY },
            { "CirculatingSupply", GetCirculatingSupply() },
            { "Symbol", WolfCoin.WOLF_SYMBOL },
            { "Name", WolfCoin.WOLF_NAME },
            { "Decimals", WolfCoin.DECIMALS },
            { "MiningReward", WolfCoin.MINING_REWARD },
            { "StakingAPY", WolfCoinStaking.STAKING_APY },
            { "MinimumStake", WolfCoinStaking.MINIMUM_STAKE },
            { "TokenId", WolfCoin.WOLF_TOKEN_ID }
        };
    }
}
