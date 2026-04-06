using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Core;
using WolfBlockchain.Storage;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlockchainController : ControllerBase
{
    private static Blockchain _blockchain = new Blockchain();
    private static BlockchainStorage _storage = new BlockchainStorage();

    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            TotalBlocks = _blockchain.Chain.Count,
            Difficulty = _blockchain.Difficulty,
            MiningReward = _blockchain.MiningReward,
            PendingTransactions = _blockchain.PendingTransactions.Count,
            IsValid = _blockchain.IsChainValid(),
            LatestBlock = new
            {
                _blockchain.GetLatestBlock().Index,
                _blockchain.GetLatestBlock().Hash,
                _blockchain.GetLatestBlock().Timestamp,
                TransactionCount = _blockchain.GetLatestBlock().Transactions.Count
            }
        });
    }

    [HttpGet("blocks")]
    public IActionResult GetBlocks()
    {
        return Ok(_blockchain.Chain);
    }

    [HttpGet("blocks/{index}")]
    public IActionResult GetBlock(int index)
    {
        if (index < 0 || index >= _blockchain.Chain.Count)
        {
            return NotFound("Block not found");
        }
        return Ok(_blockchain.Chain[index]);
    }

    [HttpGet("balance/{address}")]
    public IActionResult GetBalance(string address)
    {
        var balance = _blockchain.GetBalance(address);
        return Ok(new { Address = address, Balance = balance });
    }

    [HttpPost("transaction")]
    public IActionResult CreateTransaction([FromBody] TransactionRequest request)
    {
        try
        {
            var transaction = new Transaction(request.From, request.To, request.Amount, request.Fee);
            _blockchain.AddTransaction(transaction);
            return Ok(new { Message = "Transaction added to pending pool", TransactionId = transaction.TransactionId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("mine")]
    public IActionResult MineBlock([FromBody] MineRequest request)
    {
        try
        {
            _blockchain.MinePendingTransactions(request.MinerAddress);
            _storage.SaveBlockchain(_blockchain);
            return Ok(new
            {
                Message = "Block mined successfully",
                Reward = _blockchain.MiningReward,
                NewBalance = _blockchain.GetBalance(request.MinerAddress),
                BlockHash = _blockchain.GetLatestBlock().Hash
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("history")]
    public IActionResult GetHistory([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        if (skip < 0) skip = 0;
        if (take < 1 || take > 200) take = 50;

        var entries = _blockchain.GetHistory(skip, take);
        return Ok(new
        {
            TotalBlocks = _blockchain.Chain.Count,
            Skip = skip,
            Take = take,
            History = entries
        });
    }

    [HttpGet("history/{index}")]
    public IActionResult GetHistoryEntry(int index)
    {
        if (index < 0 || index >= _blockchain.Chain.Count)
        {
            return NotFound("Block not found");
        }

        return Ok(_blockchain.Chain[index].ToHistoryEntry());
    }

    [HttpPost("save")]
    public IActionResult SaveBlockchain()
    {
        _storage.SaveBlockchain(_blockchain);
        return Ok(new { Message = "Blockchain saved successfully" });
    }

    [HttpPost("load")]
    public IActionResult LoadBlockchain()
    {
        var loaded = _storage.LoadBlockchain();
        if (loaded != null)
        {
            _blockchain = loaded;
            return Ok(new { Message = "Blockchain loaded successfully", TotalBlocks = _blockchain.Chain.Count });
        }
        return NotFound("No saved blockchain found");
    }
}

public class TransactionRequest
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; } = 0.1m;
}

public class MineRequest
{
    public string MinerAddress { get; set; } = string.Empty;
}