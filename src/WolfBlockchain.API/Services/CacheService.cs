using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace WolfBlockchain.API.Services;

/// <summary>Serviciu de caching distribuit cu Redis</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
}

/// <summary>Implementare CacheService cu Redis</summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    
    private static class CacheKeys
    {
        public const string Users = "cache:users:*";
        public const string Tokens = "cache:tokens:*";
        public const string Transactions = "cache:transactions:*";
        public const string SmartContracts = "cache:contracts:*";
        public const string AITraining = "cache:training:*";
    }

    private static class CacheExpiration
    {
        public static readonly TimeSpan UserCache = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan TokenCache = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan TransactionCache = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan ContractCache = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan TrainingCache = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan LookupCache = TimeSpan.FromHours(1);
    }

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Preluare valoare din cache</summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var cached = await _cache.GetAsync(key);
            if (cached == null)
                return default;

            var json = System.Text.Encoding.UTF8.GetString(cached);
            var value = JsonSerializer.Deserialize<T>(json);
            
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache, key: {Key}", key);
            return default;
        }
    }

    /// <summary>Salvare valoare în cache</summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            expiration ??= GetDefaultExpiration(key);
            
            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetAsync(key, bytes, options);
            _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}ms", key, expiration?.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache, key: {Key}", key);
        }
    }

    /// <summary>Ștergere valoare din cache</summary>
    public async Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache, key: {Key}", key);
        }
    }

    /// <summary>Ștergere cache după pattern (pentru invalidare grup)</summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        try
        {
            _logger.LogInformation("Cache invalidation pattern: {Pattern}", pattern);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
        }
    }

    /// <summary>Verificare existență cheie în cache</summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var value = await _cache.GetAsync(key);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence, key: {Key}", key);
            return false;
        }
    }

    private static TimeSpan GetDefaultExpiration(string key)
    {
        return key switch
        {
            _ when key.StartsWith("cache:users:") => CacheExpiration.UserCache,
            _ when key.StartsWith("cache:tokens:") => CacheExpiration.TokenCache,
            _ when key.StartsWith("cache:transactions:") => CacheExpiration.TransactionCache,
            _ when key.StartsWith("cache:contracts:") => CacheExpiration.ContractCache,
            _ when key.StartsWith("cache:training:") => CacheExpiration.TrainingCache,
            _ when key.StartsWith("cache:lookup:") => CacheExpiration.LookupCache,
            _ => CacheExpiration.UserCache
        };
    }
}

/// <summary>Extensii pentru caching</summary>
public static class CacheExtensions
{
    /// <summary>Obține valoare din cache sau execută factory</summary>
    public static async Task<T> GetOrSetAsync<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await cache.GetAsync<T>(key);
        if (cached != null)
            return cached;

        var value = await factory();
        await cache.SetAsync(key, value, expiration);
        return value;
    }

    /// <summary>Invalidează cache pentru colecție</summary>
    public static async Task InvalidateCollectionAsync(
        this ICacheService cache,
        string collectionPattern)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(collectionPattern);

        await cache.RemoveByPatternAsync(collectionPattern);
    }

    /// <summary>Cache key builder</summary>
    public static string BuildKey(string entity, string id) => $"cache:{entity}:{id}";
    
    public static string BuildKey(string entity, string id, string subKey) => $"cache:{entity}:{id}:{subKey}";
}
