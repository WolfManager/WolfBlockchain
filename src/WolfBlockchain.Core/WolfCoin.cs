namespace WolfBlockchain.Core;

/// <summary>
/// Wolf Coin - Token nativ al Wolf Blockchain
/// </summary>
public class WolfCoin
{
    /// <summary>ID-ul tokenului Wolf Coin</summary>
    public const string WOLF_TOKEN_ID = "WOLFTOKEN_000001";
    
    /// <summary>Simbolul tokenului</summary>
    public const string WOLF_SYMBOL = "WOLF";
    
    /// <summary>Numele tokenului</summary>
    public const string WOLF_NAME = "Wolf Coin";
    
    /// <summary>Supply total - 1 miliard WOLF</summary>
    public const decimal TOTAL_SUPPLY = 1_000_000_000m;
    
    /// <summary>Decimale - 18 (standard)</summary>
    public const int DECIMALS = 18;
    
    /// <summary>Reward pentru mining</summary>
    public const decimal MINING_REWARD = 50m;
    
    /// <summary>Valoarea minima a unei tranzactii</summary>
    public const decimal MIN_TRANSACTION_VALUE = 0.000001m;
    
    /// <summary>Taxa minima de tranzactie</summary>
    public const decimal MIN_TRANSACTION_FEE = 0.00001m;

    /// <summary>Instanta Wolf Coin token</summary>
    public static Token CreateWolfCoin(string creatorAddress)
    {
        var wolfCoin = new Token(
            WOLF_NAME,
            WOLF_SYMBOL,
            TokenType.Wolf,
            creatorAddress,
            TOTAL_SUPPLY,
            DECIMALS
        );
        
        return wolfCoin;
    }

    /// <summary>Calculeaza taxa pentru o tranzactie</summary>
    public static decimal CalculateTransactionFee(decimal amount)
    {
        // Taxa este 0.1% din suma, minim MIN_TRANSACTION_FEE
        var fee = amount * 0.001m;
        return fee < MIN_TRANSACTION_FEE ? MIN_TRANSACTION_FEE : fee;
    }

    /// <summary>Calculeaza recompensa blocului cu dificultate</summary>
    public static decimal CalculateMiningReward(int blockNumber, int difficulty)
    {
        // Halving la fiecare 210000 blocuri (similar Bitcoin)
        int halvingCount = blockNumber / 210000;
        decimal reward = MINING_REWARD;

        for (int i = 0; i < halvingCount; i++)
        {
            reward /= 2;
        }

        // Multiplicatorul de dificultate
        reward *= (1 + (difficulty * 0.05m));

        return reward;
    }

    /// <summary>Verifica daca o valoare este valida pentru tranzactie</summary>
    public static bool IsValidTransactionAmount(decimal amount)
    {
        return amount >= MIN_TRANSACTION_VALUE;
    }

    /// <summary>Verifica daca o taxa este valida</summary>
    public static bool IsValidTransactionFee(decimal fee)
    {
        return fee >= MIN_TRANSACTION_FEE;
    }
}

/// <summary>
/// Distribuitor initial de Wolf Coin
/// </summary>
public class WolfCoinDistribution
{
    /// <summary>Procentul pentru pool de mining</summary>
    public const decimal MINING_POOL_PERCENTAGE = 0.40m; // 40%
    
    /// <summary>Procentul pentru ecosystem</summary>
    public const decimal ECOSYSTEM_PERCENTAGE = 0.20m; // 20%
    
    /// <summary>Procentul pentru development</summary>
    public const decimal DEVELOPMENT_PERCENTAGE = 0.15m; // 15%
    
    /// <summary>Procentul pentru community</summary>
    public const decimal COMMUNITY_PERCENTAGE = 0.15m; // 15%
    
    /// <summary>Procentul pentru founders</summary>
    public const decimal FOUNDERS_PERCENTAGE = 0.10m; // 10%

    /// <summary>Calculeaza distributia initiala</summary>
    public static Dictionary<string, decimal> GetInitialDistribution()
    {
        return new Dictionary<string, decimal>
        {
            { "MiningPool", WolfCoin.TOTAL_SUPPLY * MINING_POOL_PERCENTAGE },
            { "Ecosystem", WolfCoin.TOTAL_SUPPLY * ECOSYSTEM_PERCENTAGE },
            { "Development", WolfCoin.TOTAL_SUPPLY * DEVELOPMENT_PERCENTAGE },
            { "Community", WolfCoin.TOTAL_SUPPLY * COMMUNITY_PERCENTAGE },
            { "Founders", WolfCoin.TOTAL_SUPPLY * FOUNDERS_PERCENTAGE }
        };
    }
}

/// <summary>
/// Staking manager pentru Wolf Coin
/// Utilizatorii pot stake WOLF pentru a obtine rewards
/// </summary>
public class WolfCoinStaking
{
    /// <summary>APY (Annual Percentage Yield) pentru staking - 10%</summary>
    public const decimal STAKING_APY = 0.10m;
    
    /// <summary>Minimum pentru staking</summary>
    public const decimal MINIMUM_STAKE = 100m;
    
    /// <summary>Perioada de staking (zile)</summary>
    public const int STAKING_PERIOD_DAYS = 30;

    public class StakeRecord
    {
        public string StakerId { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime StakeStartDate { get; set; }
        public DateTime? StakeEndDate { get; set; }
        public decimal RewardEarned { get; set; }
        public bool IsActive { get; set; }

        public decimal CalculateReward(DateTime currentDate)
        {
            if (!IsActive)
                return RewardEarned;

            var daysStaked = (currentDate - StakeStartDate).Days;
            var yearsStaked = daysStaked / 365m;
            var dailyReward = (Amount * STAKING_APY) / 365m;
            var totalReward = dailyReward * daysStaked;

            return totalReward;
        }
    }

    private Dictionary<string, List<StakeRecord>> _stakes = new();

    /// <summary>Stake WOLF coins</summary>
    public StakeRecord? Stake(string stakerId, decimal amount)
    {
        if (amount < MINIMUM_STAKE)
            return null;

        var stake = new StakeRecord
        {
            StakerId = stakerId,
            Amount = amount,
            StakeStartDate = DateTime.UtcNow,
            RewardEarned = 0,
            IsActive = true
        };

        if (!_stakes.ContainsKey(stakerId))
            _stakes[stakerId] = new List<StakeRecord>();

        _stakes[stakerId].Add(stake);
        return stake;
    }

    /// <summary>Obtine rewardurile pentru stakeholder</summary>
    public decimal GetStakingRewards(string stakerId)
    {
        if (!_stakes.ContainsKey(stakerId))
            return 0;

        var totalRewards = 0m;
        foreach (var stake in _stakes[stakerId])
        {
            totalRewards += stake.CalculateReward(DateTime.UtcNow);
        }

        return totalRewards;
    }

    /// <summary>Unstake si obtine rewarduri</summary>
    public decimal? Unstake(string stakerId)
    {
        if (!_stakes.ContainsKey(stakerId) || _stakes[stakerId].Count == 0)
            return null;

        var totalAmount = 0m;
        foreach (var stake in _stakes[stakerId])
        {
            if (stake.IsActive)
            {
                stake.IsActive = false;
                stake.StakeEndDate = DateTime.UtcNow;
                stake.RewardEarned = stake.CalculateReward(DateTime.UtcNow);
                totalAmount += stake.Amount + stake.RewardEarned;
            }
        }

        return totalAmount > 0 ? totalAmount : null;
    }
}
