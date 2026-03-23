using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Core;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WolfCoinController : ControllerBase
{
    private static WolfCoinManager _wolfCoinManager = new WolfCoinManager("WOLFADMIN");
    private static bool _initialized = false;

    public WolfCoinController()
    {
        // Initializeaza Wolf Coin doar o data
        if (!_initialized)
        {
            _wolfCoinManager.InitializeWolfCoin();
            _initialized = true;
        }
    }

    /// <summary>
    /// Obtine informatii despre Wolf Coin
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetWolfCoinInfo()
    {
        var token = _wolfCoinManager.GetWolfCoinToken();
        if (token == null)
            return BadRequest("Wolf Coin not initialized");

        return Ok(new
        {
            name = token.Name,
            symbol = token.Symbol,
            tokenId = token.TokenId,
            totalSupply = token.TotalSupply,
            currentSupply = token.CurrentSupply,
            decimals = token.Decimals,
            type = token.Type.ToString(),
            createdAt = token.CreatedAt
        });
    }

    /// <summary>
    /// Obtine balanța de Wolf Coin pentru o adresa
    /// </summary>
    [HttpGet("balance/{address}")]
    public IActionResult GetWolfCoinBalance(string address)
    {
        var balance = _wolfCoinManager.GetWolfCoinBalance(address);

        return Ok(new
        {
            address = address,
            balance = balance,
            symbol = WolfCoin.WOLF_SYMBOL,
            decimals = WolfCoin.DECIMALS
        });
    }

    /// <summary>
    /// Transfer Wolf Coin intre adrese
    /// </summary>
    [HttpPost("transfer")]
    public IActionResult TransferWolfCoin([FromBody] TransferWolfCoinRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var success = _wolfCoinManager.TransferWolfCoin(request.FromAddress, request.ToAddress, request.Amount);
        if (!success)
            return BadRequest("Transfer failed");

        return Ok(new
        {
            success = true,
            from = request.FromAddress,
            to = request.ToAddress,
            amount = request.Amount,
            symbol = WolfCoin.WOLF_SYMBOL
        });
    }

    /// <summary>
    /// Aplica taxa de tranzactie
    /// </summary>
    [HttpPost("apply-fee")]
    public IActionResult ApplyTransactionFee([FromBody] ApplyFeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var fee = WolfCoin.CalculateTransactionFee(request.Amount);
        var success = _wolfCoinManager.ApplyTransactionFee(request.PayerAddress, request.Amount, request.TreasuryAddress);

        if (!success)
            return BadRequest("Failed to apply fee");

        return Ok(new
        {
            success = true,
            amount = request.Amount,
            fee = fee,
            feePercentage = "0.1%"
        });
    }

    /// <summary>
    /// Acorda recompensa mining
    /// </summary>
    [HttpPost("reward-miner")]
    public IActionResult RewardMiner([FromBody] RewardMinerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var reward = WolfCoin.CalculateMiningReward(request.BlockNumber, request.Difficulty);
        var success = _wolfCoinManager.RewardMiner(request.MinerAddress, request.BlockNumber, request.Difficulty);

        if (!success)
            return BadRequest("Failed to reward miner");

        return Ok(new
        {
            success = true,
            minerAddress = request.MinerAddress,
            reward = reward,
            blockNumber = request.BlockNumber,
            difficulty = request.Difficulty
        });
    }

    /// <summary>
    /// Stake Wolf Coin
    /// </summary>
    [HttpPost("stake")]
    public IActionResult StakeWolfCoin([FromBody] StakeWolfCoinRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var success = _wolfCoinManager.StakeWolfCoin(request.StakerId, request.Amount);
        if (!success)
            return BadRequest($"Failed to stake. Minimum stake: {WolfCoinStaking.MINIMUM_STAKE} WOLF");

        return Ok(new
        {
            success = true,
            stakerId = request.StakerId,
            amount = request.Amount,
            apy = $"{WolfCoinStaking.STAKING_APY * 100}%",
            period = $"{WolfCoinStaking.STAKING_PERIOD_DAYS} days"
        });
    }

    /// <summary>
    /// Unstake Wolf Coin si obtine rewarduri
    /// </summary>
    [HttpPost("unstake")]
    public IActionResult UnstakeWolfCoin([FromBody] UnstakeWolfCoinRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var totalWithRewards = _wolfCoinManager.UnstakeWolfCoin(request.StakerId);
        if (totalWithRewards == null)
            return BadRequest("Failed to unstake or no active stakes");

        return Ok(new
        {
            success = true,
            stakerId = request.StakerId,
            totalAmount = totalWithRewards,
            message = "Unstaked successfully with rewards"
        });
    }

    /// <summary>
    /// Obtine rewardurile de staking
    /// </summary>
    [HttpGet("staking-rewards/{stakerId}")]
    public IActionResult GetStakingRewards(string stakerId)
    {
        var rewards = _wolfCoinManager.GetStakingRewards(stakerId);

        return Ok(new
        {
            stakerId = stakerId,
            rewards = rewards,
            apy = $"{WolfCoinStaking.STAKING_APY * 100}%"
        });
    }

    /// <summary>
    /// Obtine circulating supply
    /// </summary>
    [HttpGet("circulating-supply")]
    public IActionResult GetCirculatingSupply()
    {
        var supply = _wolfCoinManager.GetCirculatingSupply();

        return Ok(new
        {
            circulatingSupply = supply,
            totalSupply = WolfCoin.TOTAL_SUPPLY,
            symbol = WolfCoin.WOLF_SYMBOL,
            percentage = $"{(supply / WolfCoin.TOTAL_SUPPLY * 100):F2}%"
        });
    }

    /// <summary>
    /// Obtine statistici blockchain
    /// </summary>
    [HttpGet("statistics")]
    public IActionResult GetWolfCoinStatistics()
    {
        var stats = _wolfCoinManager.GetWolfCoinStatistics();

        return Ok(stats);
    }

    /// <summary>
    /// Calculeaza taxa pentru o suma
    /// </summary>
    [HttpGet("calculate-fee/{amount}")]
    public IActionResult CalculateFee(decimal amount)
    {
        if (amount <= 0)
            return BadRequest("Amount must be greater than 0");

        var fee = WolfCoin.CalculateTransactionFee(amount);

        return Ok(new
        {
            amount = amount,
            fee = fee,
            feePercentage = "0.1%",
            total = amount + fee
        });
    }

    /// <summary>
    /// Calculeaza recompensa mining
    /// </summary>
    [HttpGet("calculate-mining-reward/{blockNumber}/{difficulty}")]
    public IActionResult CalculateMiningReward(int blockNumber, int difficulty)
    {
        if (blockNumber < 0 || difficulty < 1)
            return BadRequest("Invalid block number or difficulty");

        var reward = WolfCoin.CalculateMiningReward(blockNumber, difficulty);

        return Ok(new
        {
            blockNumber = blockNumber,
            difficulty = difficulty,
            reward = reward,
            baseReward = WolfCoin.MINING_REWARD
        });
    }
}

/// <summary>Request pentru transfer Wolf Coin</summary>
public class TransferWolfCoinRequest
{
    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>Request pentru aplicare taxa</summary>
public class ApplyFeeRequest
{
    public string PayerAddress { get; set; } = "";
    public string TreasuryAddress { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>Request pentru recompensa mining</summary>
public class RewardMinerRequest
{
    public string MinerAddress { get; set; } = "";
    public int BlockNumber { get; set; }
    public int Difficulty { get; set; }
}

/// <summary>Request pentru staking</summary>
public class StakeWolfCoinRequest
{
    public string StakerId { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>Request pentru unstaking</summary>
public class UnstakeWolfCoinRequest
{
    public string StakerId { get; set; } = "";
}
