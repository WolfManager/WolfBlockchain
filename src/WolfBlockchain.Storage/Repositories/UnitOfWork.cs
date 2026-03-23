using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.Storage.Repositories;

/// <summary>
/// Generic Repository Interface
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveChangesAsync();
}

/// <summary>
/// Generic Repository Implementation
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly WolfBlockchainDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(WolfBlockchainDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate)
    {
        return await Task.FromResult(_dbSet.Where(predicate).ToList());
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Repository pentru Blocks
/// </summary>
public interface IBlockRepository : IRepository<BlockEntity>
{
    Task<BlockEntity?> GetByHashAsync(string hash);
    Task<BlockEntity?> GetByIndexAsync(int index);
    Task<IEnumerable<BlockEntity>> GetByStatusAsync(string status);
    Task<IEnumerable<BlockEntity>> GetRecentBlocksAsync(int count);
}

public class BlockRepository : Repository<BlockEntity>, IBlockRepository
{
    public BlockRepository(WolfBlockchainDbContext context) : base(context) { }

    public async Task<BlockEntity?> GetByHashAsync(string hash)
    {
        return await _dbSet.FirstOrDefaultAsync(b => b.Hash == hash);
    }

    public async Task<BlockEntity?> GetByIndexAsync(int index)
    {
        return await _dbSet.FirstOrDefaultAsync(b => b.Index == index);
    }

    public async Task<IEnumerable<BlockEntity>> GetByStatusAsync(string status)
    {
        return await _dbSet.Where(b => b.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<BlockEntity>> GetRecentBlocksAsync(int count)
    {
        return await _dbSet.OrderByDescending(b => b.Timestamp).Take(count).ToListAsync();
    }
}

/// <summary>
/// Repository pentru Transactions
/// </summary>
public interface ITransactionRepository : IRepository<TransactionEntity>
{
    Task<TransactionEntity?> GetByTransactionIdAsync(string transactionId);
    Task<IEnumerable<TransactionEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<TransactionEntity>> GetByStatusAsync(string status);
    Task<IEnumerable<TransactionEntity>> GetByBlockIdAsync(int blockId);
}

public class TransactionRepository : Repository<TransactionEntity>, ITransactionRepository
{
    public TransactionRepository(WolfBlockchainDbContext context) : base(context) { }

    public async Task<TransactionEntity?> GetByTransactionIdAsync(string transactionId)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TransactionId == transactionId);
    }

    public async Task<IEnumerable<TransactionEntity>> GetByAddressAsync(string address)
    {
        return await _dbSet.Where(t => t.FromAddress == address || t.ToAddress == address).ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetByStatusAsync(string status)
    {
        return await _dbSet.Where(t => t.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<TransactionEntity>> GetByBlockIdAsync(int blockId)
    {
        return await _dbSet.Where(t => t.BlockId == blockId).ToListAsync();
    }
}

/// <summary>
/// Repository pentru Wallets
/// </summary>
public interface IWalletRepository : IRepository<WalletEntity>
{
    Task<WalletEntity?> GetByAddressAsync(string address);
    Task<IEnumerable<WalletEntity>> GetActiveWalletsAsync();
}

public class WalletRepository : Repository<WalletEntity>, IWalletRepository
{
    public WalletRepository(WolfBlockchainDbContext context) : base(context) { }

    public async Task<WalletEntity?> GetByAddressAsync(string address)
    {
        return await _dbSet.Include(w => w.TokenBalances).FirstOrDefaultAsync(w => w.Address == address);
    }

    public async Task<IEnumerable<WalletEntity>> GetActiveWalletsAsync()
    {
        return await _dbSet.Where(w => w.IsActive).ToListAsync();
    }
}

/// <summary>
/// Repository pentru Users
/// </summary>
public interface IUserRepository : IRepository<UserEntity>
{
    Task<UserEntity?> GetByUsernameAsync(string username);
    Task<UserEntity?> GetByAddressAsync(string address);
    Task<IEnumerable<UserEntity>> GetByRoleAsync(string role);
}

public class UserRepository : Repository<UserEntity>, IUserRepository
{
    public UserRepository(WolfBlockchainDbContext context) : base(context) { }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserEntity?> GetByAddressAsync(string address)
    {
        return await _dbSet.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Address == address);
    }

    public async Task<IEnumerable<UserEntity>> GetByRoleAsync(string role)
    {
        return await _dbSet.Where(u => u.Role == role).ToListAsync();
    }
}

/// <summary>
/// Repository pentru Tokens
/// </summary>
public interface ITokenRepository : IRepository<TokenEntity>
{
    Task<TokenEntity?> GetByTokenIdAsync(string tokenId);
    Task<TokenEntity?> GetBySymbolAsync(string symbol);
    Task<IEnumerable<TokenEntity>> GetByTypeAsync(string tokenType);
}

public class TokenRepository : Repository<TokenEntity>, ITokenRepository
{
    public TokenRepository(WolfBlockchainDbContext context) : base(context) { }

    public async Task<TokenEntity?> GetByTokenIdAsync(string tokenId)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TokenId == tokenId);
    }

    public async Task<TokenEntity?> GetBySymbolAsync(string symbol)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Symbol == symbol);
    }

    public async Task<IEnumerable<TokenEntity>> GetByTypeAsync(string tokenType)
    {
        return await _dbSet.Where(t => t.TokenType == tokenType).ToListAsync();
    }
}

/// <summary>
/// Repository pentru Token Transactions
/// </summary>
public interface ITokenTransactionRepository : IRepository<TokenTransactionEntity>
{
    Task<TokenTransactionEntity?> GetByTransactionIdAsync(string transactionId);
    Task<IEnumerable<TokenTransactionEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<TokenTransactionEntity>> GetByTokenIdAsync(int tokenId);
}

public class TokenTransactionRepository : Repository<TokenTransactionEntity>, ITokenTransactionRepository
{
    public TokenTransactionRepository(WolfBlockchainDbContext context) : base(context) { }

    public async Task<TokenTransactionEntity?> GetByTransactionIdAsync(string transactionId)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TransactionId == transactionId);
    }

    public async Task<IEnumerable<TokenTransactionEntity>> GetByAddressAsync(string address)
    {
        return await _dbSet.Where(t => t.FromAddress == address || t.ToAddress == address).ToListAsync();
    }

    public async Task<IEnumerable<TokenTransactionEntity>> GetByTokenIdAsync(int tokenId)
    {
        return await _dbSet.Where(t => t.TokenId == tokenId).ToListAsync();
    }
}

/// <summary>
/// Unit of Work Pattern
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IBlockRepository Blocks { get; }
    ITransactionRepository Transactions { get; }
    IWalletRepository Wallets { get; }
    IUserRepository Users { get; }
    ITokenRepository Tokens { get; }
    ITokenTransactionRepository TokenTransactions { get; }
    Task SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly WolfBlockchainDbContext _context;

    public IBlockRepository Blocks { get; }
    public ITransactionRepository Transactions { get; }
    public IWalletRepository Wallets { get; }
    public IUserRepository Users { get; }
    public ITokenRepository Tokens { get; }
    public ITokenTransactionRepository TokenTransactions { get; }

    public UnitOfWork(WolfBlockchainDbContext context)
    {
        _context = context;
        Blocks = new BlockRepository(context);
        Transactions = new TransactionRepository(context);
        Wallets = new WalletRepository(context);
        Users = new UserRepository(context);
        Tokens = new TokenRepository(context);
        TokenTransactions = new TokenTransactionRepository(context);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
