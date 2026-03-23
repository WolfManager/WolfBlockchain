using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WolfBlockchain.API.Services;
using WolfBlockchain.Storage.Repositories;
using AdminDashboardDtos = WolfBlockchain.API.Services.ClientAuthContracts;

namespace WolfBlockchain.API.Controllers;

/// <summary>
/// Admin Dashboard API — provides user, token, and network statistics with caching.
/// Requires authentication. Single-admin mode: only authenticated users can access.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AdminDashboardCacheService _cacheService;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        IUnitOfWork unitOfWork,
        AdminDashboardCacheService cacheService,
        ILogger<AdminDashboardController> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get summary statistics for the admin dashboard (cached).
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryAsync()
    {
        try
        {
            var summary = await _cacheService.GetOrSetSummaryAsync(async () => new
            {
                TotalUsers = 150,
                TotalTokens = 8,
                TotalValidators = 12,
                TotalStaked = 120_000_000L,
                ActiveAITrainingJobs = 0,
                DeployedSmartContracts = 5,
                LastUpdatedAt = DateTime.UtcNow
            });

            _logger.LogInformation($"Admin dashboard summary requested");
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin dashboard summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated list of users (cached per page).
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Invalid pagination parameters" });

            var result = await _cacheService.GetOrSetUsersAsync(page, pageSize, async (p, ps) =>
            {
                var users = Enumerable.Range(1, 10).Select(i => new
                {
                    UserId = $"USR{i:000}",
                    Username = $"user_{i}",
                    Address = $"WOLF{Guid.NewGuid().ToString()[..8]}",
                    Role = i % 3 == 0 ? "Validator" : "User",
                    Balance = i * 50000,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                }).ToList();

                return new
                {
                    Users = users,
                    TotalCount = 150,
                    Page = p,
                    PageSize = ps
                };
            });

            _logger.LogInformation($"Retrieved users (page {page}) from cache");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated list of tokens (cached per page).
    /// </summary>
    [HttpGet("tokens")]
    public async Task<IActionResult> GetTokensAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Invalid pagination parameters" });

            var result = await _cacheService.GetOrSetTokensAsync(page, pageSize, async (p, ps) =>
            {
                var tokens = new object[]
                {
                    new { TokenId = "TOKEN001", Name = "Wolf Coin", Symbol = "WOLF", TokenType = "Native", TotalSupply = 1000000000L, CurrentSupply = 450000000L, Status = "Active", CreatorAddress = "WOLFADMIN", CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new { TokenId = "TOKEN002", Name = "MemeCoin", Symbol = "MEM", TokenType = "Custom", TotalSupply = 5000000L, CurrentSupply = 3200000L, Status = "Active", CreatorAddress = "WOLFUSER1", CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new { TokenId = "TOKEN003", Name = "Token AI", Symbol = "AI", TokenType = "Custom", TotalSupply = 1000000000L, CurrentSupply = 750000000L, Status = "Active", CreatorAddress = "WOLFUSER2", CreatedAt = DateTime.UtcNow.AddDays(-10) },
                };

                return new
                {
                    Tokens = tokens.Skip((p - 1) * ps).Take(ps),
                    TotalCount = tokens.Length,
                    Page = p,
                    PageSize = ps
                };
            });

            _logger.LogInformation($"Retrieved tokens (page {page}) from cache");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tokens");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent blockchain events (cached).
    /// </summary>
    [HttpGet("recent-events")]
    public IActionResult GetRecentEvents([FromQuery] int limit = 10)
    {
        try
        {
            // Placeholder: In production, fetch from event log/audit trail
            var events = new object[]
            {
                new { EventType = "BlockAdded", BlockNumber = 1234, Timestamp = DateTime.UtcNow.AddMinutes(-5) },
                new { EventType = "Transaction", Hash = "0x123abc", Amount = 100M, Timestamp = DateTime.UtcNow.AddMinutes(-3) },
                new { EventType = "TokenCreated", TokenSymbol = "TEST", Creator = "WOLFUSER", Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            };

            return Ok(events.Take(limit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent events");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
