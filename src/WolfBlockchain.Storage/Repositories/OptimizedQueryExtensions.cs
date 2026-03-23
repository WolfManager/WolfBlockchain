using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.Storage.Repositories;

/// <summary>Optimized query extensions for performance</summary>
public static class OptimizedQueryExtensions
{
    // ============= USER QUERIES =============
    
    /// <summary>Get user by ID with related data (optimized for caching)</summary>
    public static async Task<UserEntity?> GetUserByIdOptimizedAsync(this DbSet<UserEntity> users, int id)
    {
        return await users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>Get user by username (frequently accessed)</summary>
    public static async Task<UserEntity?> GetUserByUsernameOptimizedAsync(this DbSet<UserEntity> users, string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        
        return await users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>Get all active users with pagination</summary>
    public static async Task<(List<UserEntity> items, int total)> GetActiveUsersOptimizedAsync(
        this DbSet<UserEntity> users, 
        int page = 1, 
        int pageSize = 20)
    {
        var query = users.AsNoTracking().Where(u => u.IsActive);
        var total = await query.CountAsync();
        
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // ============= TOKEN QUERIES =============
    
    /// <summary>Get token by ID with projection (smaller payload)</summary>
    public static async Task<TokenDto?> GetTokenDtoByIdOptimizedAsync(this DbSet<TokenEntity> tokens, int id)
    {
        return await tokens
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TokenDto
            {
                Id = t.Id,
                TokenId = t.TokenId,
                Name = t.Name,
                Symbol = t.Symbol,
                TotalSupply = t.TotalSupply,
                CurrentSupply = t.CurrentSupply,
                IsActive = t.IsActive
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>Get tokens by symbol (frequently used)</summary>
    public static async Task<List<TokenEntity>> GetTokensBySymbolOptimizedAsync(this DbSet<TokenEntity> tokens, string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        
        return await tokens
            .AsNoTracking()
            .Where(t => t.Symbol == symbol && t.IsActive)
            .ToListAsync();
    }

    /// <summary>Get tokens with pagination and filtering</summary>
    public static async Task<(List<TokenEntity> items, int total)> GetTokensOptimizedAsync(
        this DbSet<TokenEntity> tokens,
        string? typeFilter = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = tokens.AsNoTracking().Where(t => t.IsActive);

        if (!string.IsNullOrEmpty(typeFilter))
            query = query.Where(t => t.TokenType == typeFilter);

        var total = await query.CountAsync();
        
        var items = await query
            .OrderBy(t => t.Symbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // ============= TRANSACTION QUERIES =============
    
    /// <summary>Get recent transactions with pagination</summary>
    public static async Task<(List<TransactionEntity> items, int total)> GetRecentTransactionsOptimizedAsync(
        this DbSet<TransactionEntity> transactions,
        int page = 1,
        int pageSize = 50)
    {
        var query = transactions.AsNoTracking();
        var total = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>Get transactions by address (both from and to)</summary>
    public static async Task<List<TransactionEntity>> GetTransactionsByAddressOptimizedAsync(
        this DbSet<TransactionEntity> transactions,
        string address,
        int limit = 100)
    {
        ArgumentNullException.ThrowIfNull(address);
        
        return await transactions
            .AsNoTracking()
            .Where(t => t.FromAddress == address || t.ToAddress == address)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>Get transaction count by status</summary>
    public static async Task<Dictionary<string, int>> GetTransactionCountsByStatusOptimizedAsync(
        this DbSet<TransactionEntity> transactions)
    {
        return await transactions
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    // ============= WALLET QUERIES =============
    
    /// <summary>Get wallet with token balances</summary>
    public static async Task<WalletEntity?> GetWalletWithBalancesOptimizedAsync(
        this DbSet<WalletEntity> wallets,
        string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        
        return await wallets
            .AsNoTracking()
            .Include(w => w.TokenBalances)
            .ThenInclude(tb => tb.Token)
            .FirstOrDefaultAsync(w => w.Address == address);
    }

    /// <summary>Get active wallets count</summary>
    public static async Task<int> GetActiveWalletsCountOptimizedAsync(this DbSet<WalletEntity> wallets)
    {
        return await wallets
            .AsNoTracking()
            .Where(w => w.IsActive)
            .CountAsync();
    }

    // ============= BLOCK QUERIES =============
    
    /// <summary>Get latest blocks</summary>
    public static async Task<List<BlockEntity>> GetLatestBlocksOptimizedAsync(
        this DbSet<BlockEntity> blocks,
        int limit = 20)
    {
        return await blocks
            .AsNoTracking()
            .OrderByDescending(b => b.Index)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>Get block by index (blockchain specific)</summary>
    public static async Task<BlockEntity?> GetBlockByIndexOptimizedAsync(
        this DbSet<BlockEntity> blocks,
        long index)
    {
        return await blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Index == index);
    }

    // ============= TOKEN BALANCE QUERIES =============
    
    /// <summary>Get token balance for wallet</summary>
    public static async Task<decimal> GetTokenBalanceOptimizedAsync(
        this DbSet<TokenBalanceEntity> balances,
        int walletId,
        int tokenId)
    {
        var balance = await balances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WalletId == walletId && b.TokenId == tokenId);

        return balance?.Balance ?? 0;
    }

    /// <summary>Get all token balances for wallet</summary>
    public static async Task<List<TokenBalanceEntity>> GetWalletTokenBalancesOptimizedAsync(
        this DbSet<TokenBalanceEntity> balances,
        int walletId)
    {
        return await balances
            .AsNoTracking()
            .Include(b => b.Token)
            .Where(b => b.WalletId == walletId)
            .OrderBy(b => b.Token!.Symbol)
            .ToListAsync();
    }
}

// ============= DATA TRANSFER OBJECTS =============

/// <summary>Token DTO for smaller payload</summary>
public record TokenDto
{
    public int Id { get; set; }
    public string TokenId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal TotalSupply { get; set; }
    public decimal CurrentSupply { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>User DTO for API responses</summary>
public record UserDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Transaction DTO for API responses</summary>
public record TransactionDto
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>Wallet DTO with balance summary</summary>
public record WalletSummaryDto
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal WolfBalance { get; set; }
    public int TokenCount { get; set; }
    public bool IsActive { get; set; }
}
