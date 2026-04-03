using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.Core;

namespace WolfBlockchain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly TokenManager _tokenManager;

    public TokenController(TokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    /// <summary>
    /// Creeaza un nou token
    /// </summary>
    [HttpPost("create")]
    public IActionResult CreateToken([FromBody] CreateTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var token = _tokenManager.CreateToken(
            request.Name,
            request.Symbol,
            request.Type,
            request.CreatorAddress,
            request.TotalSupply,
            request.Decimals
        );

        if (token == null)
            return BadRequest("Failed to create token");

        return Ok(new
        {
            success = true,
            tokenId = token.TokenId,
            name = token.Name,
            symbol = token.Symbol,
            totalSupply = token.TotalSupply,
            type = token.Type.ToString()
        });
    }

    /// <summary>
    /// Obtine informatii despre un token
    /// </summary>
    [HttpGet("{tokenId}")]
    public IActionResult GetToken(string tokenId)
    {
        var token = _tokenManager.GetToken(tokenId);
        if (token == null)
            return NotFound("Token not found");

        return Ok(new
        {
            tokenId = token.TokenId,
            name = token.Name,
            symbol = token.Symbol,
            type = token.Type.ToString(),
            creatorAddress = token.CreatorAddress,
            totalSupply = token.TotalSupply,
            currentSupply = token.CurrentSupply,
            decimals = token.Decimals,
            isActive = token.IsActive,
            createdAt = token.CreatedAt
        });
    }

    /// <summary>
    /// Lista toti tokenii
    /// </summary>
    [HttpGet("")]
    public IActionResult GetAllTokens()
    {
        var tokens = _tokenManager.GetAllTokens();
        var tokenList = tokens.Select(t => new
        {
            tokenId = t.TokenId,
            name = t.Name,
            symbol = t.Symbol,
            type = t.Type.ToString(),
            totalSupply = t.TotalSupply,
            currentSupply = t.CurrentSupply
        });

        return Ok(tokenList);
    }

    /// <summary>
    /// Transfer token intre adrese
    /// </summary>
    [HttpPost("transfer")]
    public IActionResult TransferToken([FromBody] TransferTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var transaction = _tokenManager.TransferToken(
            request.TokenId,
            request.FromAddress,
            request.ToAddress,
            request.Amount,
            request.Fee
        );

        if (transaction == null)
            return BadRequest("Transfer failed");

        return Ok(new
        {
            success = true,
            transactionId = transaction.TransactionId,
            from = transaction.FromAddress,
            to = transaction.ToAddress,
            amount = transaction.Amount,
            fee = transaction.Fee,
            status = transaction.Status.ToString(),
            timestamp = transaction.Timestamp
        });
    }

    /// <summary>
    /// Obtine balanța de token pentru o adresa
    /// </summary>
    [HttpGet("balance/{address}/{tokenId}")]
    public IActionResult GetTokenBalance(string address, string tokenId)
    {
        var balance = _tokenManager.GetTokenBalance(address, tokenId);
        var token = _tokenManager.GetToken(tokenId);

        if (token == null)
            return NotFound("Token not found");

        return Ok(new
        {
            address = address,
            tokenId = tokenId,
            symbol = token.Symbol,
            balance = balance,
            decimals = token.Decimals
        });
    }

    /// <summary>
    /// Obtine toate balanțele de tokeni pentru o adresa
    /// </summary>
    [HttpGet("balances/{address}")]
    public IActionResult GetAllTokenBalances(string address)
    {
        var balances = _tokenManager.GetAllTokenBalances(address);
        var result = balances.Select(kvp => new
        {
            tokenId = kvp.Key,
            amount = kvp.Value,
            symbol = _tokenManager.GetToken(kvp.Key)?.Symbol ?? "UNKNOWN"
        });

        return Ok(result);
    }

    /// <summary>
    /// Obtine istoricul tranzactiilor de token
    /// </summary>
    [HttpGet("history/{address}")]
    public IActionResult GetTokenTransactionHistory(string address)
    {
        var transactions = _tokenManager.GetTokenTransactionHistory(address);
        var result = transactions.Select(t => new
        {
            transactionId = t.TransactionId,
            tokenId = t.TokenId,
            from = t.FromAddress,
            to = t.ToAddress,
            amount = t.Amount,
            fee = t.Fee,
            status = t.Status.ToString(),
            timestamp = t.Timestamp
        });

        return Ok(result);
    }

    /// <summary>
    /// Mint tokeni (doar owner)
    /// </summary>
    [HttpPost("mint")]
    public IActionResult MintToken([FromBody] MintTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var success = _tokenManager.MintToken(request.TokenId, request.Amount, request.TargetAddress);

        if (!success)
            return BadRequest("Mint failed. Check if you are the owner.");

        return Ok(new { success = true, message = "Tokens minted successfully" });
    }

    /// <summary>
    /// Burn tokeni
    /// </summary>
    [HttpPost("burn")]
    public IActionResult BurnToken([FromBody] BurnTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request");

        var success = _tokenManager.BurnToken(request.TokenId, request.Amount, request.FromAddress);

        if (!success)
            return BadRequest("Burn failed");

        return Ok(new { success = true, message = "Tokens burned successfully" });
    }
}

/// <summary>Request pentru crearea de token</summary>
public class CreateTokenRequest
{
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public TokenType Type { get; set; }
    public string CreatorAddress { get; set; } = "";
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; } = 18;
}

/// <summary>Request pentru transfer de token</summary>
public class TransferTokenRequest
{
    public string TokenId { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Fee { get; set; } = 0;
}

/// <summary>Request pentru mint</summary>
public class MintTokenRequest
{
    public string TokenId { get; set; } = "";
    public decimal Amount { get; set; }
    public string TargetAddress { get; set; } = "";
}

/// <summary>Request pentru burn</summary>
public class BurnTokenRequest
{
    public string TokenId { get; set; } = "";
    public decimal Amount { get; set; }
    public string FromAddress { get; set; } = "";
}
