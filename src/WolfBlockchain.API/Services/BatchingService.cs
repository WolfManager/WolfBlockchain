using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WolfBlockchain.Storage.Context;
using WolfBlockchain.Storage.Models;

namespace WolfBlockchain.API.Services;

/// <summary>Request batching service for bulk operations</summary>
public interface IBatchingService
{
    Task<List<T>> GetByIdsAsync<T>(List<int> ids, int maxBatch = 100) where T : class;
    Task<List<UserEntity>> GetUsersByIdsAsync(List<int> ids);
    Task<List<TokenEntity>> GetTokensByIdsAsync(List<int> ids);
    Task<List<TransactionEntity>> GetTransactionsByIdsAsync(List<int> ids);
}

/// <summary>Implementation with efficient batch processing</summary>
public class BatchingService : IBatchingService
{
    private readonly WolfBlockchainDbContext _context;
    private readonly ILogger<BatchingService> _logger;

    public BatchingService(WolfBlockchainDbContext context, ILogger<BatchingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Generic batch retrieval with safety limits</summary>
    public async Task<List<T>> GetByIdsAsync<T>(List<int> ids, int maxBatch = 100) where T : class
    {
        if (ids == null || ids.Count == 0)
            return new List<T>();

        // Safety check - never exceed max batch size
        if (ids.Count > maxBatch)
        {
            _logger.LogWarning(
                "Batch request exceeds max size: requested {Requested}, allowed {Max}",
                ids.Count,
                maxBatch);
            ids = ids.Take(maxBatch).ToList();
        }

        // Remove duplicates
        var uniqueIds = ids.Distinct().ToList();

        _logger.LogInformation(
            "Batching {Count} items of type {Type}",
            uniqueIds.Count,
            typeof(T).Name);

        try
        {
            var set = _context.Set<T>();
            
            // Execute single query instead of N queries
            var results = await set
                .AsNoTracking()
                .Where(CreateIdPredicate<T>(uniqueIds))
                .ToListAsync();

            _logger.LogInformation(
                "Batch retrieval completed: requested {Requested}, returned {Returned}",
                uniqueIds.Count,
                results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch retrieval of type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>Batch retrieve users</summary>
    public async Task<List<UserEntity>> GetUsersByIdsAsync(List<int> ids)
    {
        return await GetByIdsAsync<UserEntity>(ids, maxBatch: 100);
    }

    /// <summary>Batch retrieve tokens</summary>
    public async Task<List<TokenEntity>> GetTokensByIdsAsync(List<int> ids)
    {
        return await GetByIdsAsync<TokenEntity>(ids, maxBatch: 100);
    }

    /// <summary>Batch retrieve transactions</summary>
    public async Task<List<TransactionEntity>> GetTransactionsByIdsAsync(List<int> ids)
    {
        return await GetByIdsAsync<TransactionEntity>(ids, maxBatch: 100);
    }

    /// <summary>Create predicate for ID filtering</summary>
    private static Expression<Func<T, bool>> CreateIdPredicate<T>(List<int> ids) where T : class
    {
        // Dynamic predicate: WHERE Id IN (id1, id2, id3, ...)
        var parameter = Expression.Parameter(typeof(T), "e");
        var idProperty = Expression.Property(parameter, "Id");
        var idConstant = Expression.Constant(ids);
        
        var containsMethod = typeof(List<int>).GetMethod("Contains", new[] { typeof(int) });
        var call = Expression.Call(idConstant, containsMethod, idProperty);
        
        return Expression.Lambda<Func<T, bool>>(call, parameter);
    }
}

/// <summary>Batch request builder for fluent API</summary>
public class BatchRequestBuilder
{
    private readonly List<int> _ids = new();
    private int _maxSize = 100;

    public BatchRequestBuilder AddIds(params int[] ids)
    {
        _ids.AddRange(ids);
        return this;
    }

    public BatchRequestBuilder AddIds(List<int> ids)
    {
        _ids.AddRange(ids);
        return this;
    }

    public BatchRequestBuilder SetMaxSize(int maxSize)
    {
        if (maxSize > 0 && maxSize <= 1000)
            _maxSize = maxSize;
        return this;
    }

    public BatchRequestDto Build()
    {
        return new BatchRequestDto
        {
            Ids = _ids.Take(_maxSize).ToList(),
            MaxIds = _maxSize
        };
    }
}

/// <summary>Batch request DTO</summary>
public record BatchRequestDto
{
    public List<int> Ids { get; set; } = new();
    public int MaxIds { get; set; }
}
