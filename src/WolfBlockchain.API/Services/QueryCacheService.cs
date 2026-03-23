using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using WolfBlockchain.API.Services;

namespace WolfBlockchain.API.Services;

/// <summary>Advanced query result caching service</summary>
public interface IQueryCacheService
{
    /// <summary>Get cached value or execute factory if not cached</summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        params string[] invalidationPatterns) where T : class;

    /// <summary>Invalidate cache by pattern</summary>
    Task InvalidateAsync(string pattern);

    /// <summary>Get cache statistics</summary>
    Task<CacheStatsDto> GetStatsAsync();

    /// <summary>Clear all cache</summary>
    Task ClearAsync();

    /// <summary>Get specific cache key stats</summary>
    Task<KeyStatsDto> GetKeyStatsAsync(string key);
}

/// <summary>Implementation of advanced query caching</summary>
public class QueryCacheService : IQueryCacheService
{
    private readonly ICacheService _baseCache;
    private readonly ILogger<QueryCacheService> _logger;
    private readonly ConcurrentDictionary<string, CacheKeyMetadata> _metadata;

    public QueryCacheService(
        ICacheService baseCache,
        ILogger<QueryCacheService> logger)
    {
        _baseCache = baseCache ?? throw new ArgumentNullException(nameof(baseCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metadata = new ConcurrentDictionary<string, CacheKeyMetadata>();
    }

    /// <summary>Get or set cached value with automatic expiration</summary>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        params string[] invalidationPatterns) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        // Try to get from cache
        var cached = await _baseCache.GetAsync<T>(key);
        if (cached != null)
        {
            RecordCacheHit(key);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cached;
        }

        // Cache miss - execute factory
        _logger.LogDebug("Cache miss for key: {Key}, executing factory", key);
        var value = await factory();

        if (value != null)
        {
            // Set expiration (default: 10 minutes)
            var ttl = expiration ?? TimeSpan.FromMinutes(10);
            await _baseCache.SetAsync(key, value, ttl);

            // Track metadata
            RecordCacheMiss(key, invalidationPatterns);
        }

        return value;
    }

    /// <summary>Invalidate cache entries by pattern</summary>
    public async Task InvalidateAsync(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        _logger.LogInformation("Invalidating cache entries matching pattern: {Pattern}", pattern);

        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase);

        var keysToInvalidate = _metadata
            .Keys
            .Where(k => regex.IsMatch(k))
            .ToList();

        foreach (var key in keysToInvalidate)
        {
            await _baseCache.RemoveAsync(key);
            _metadata.TryRemove(key, out _);
            _logger.LogDebug("Invalidated cache key: {Key}", key);
        }

        _logger.LogInformation("Invalidated {Count} cache entries", keysToInvalidate.Count);
    }

    /// <summary>Get cache statistics</summary>
    public async Task<CacheStatsDto> GetStatsAsync()
    {
        var totalKeys = _metadata.Count;
        var totalHits = _metadata.Values.Sum(m => m.HitCount);
        var totalMisses = _metadata.Values.Sum(m => m.MissCount);
        var totalRequests = totalHits + totalMisses;

        var hitRate = totalRequests > 0
            ? (double)totalHits / totalRequests * 100
            : 0;

        return new CacheStatsDto
        {
            TotalKeys = totalKeys,
            TotalHits = totalHits,
            TotalMisses = totalMisses,
            HitRate = hitRate,
            AverageHitsPerKey = totalKeys > 0 ? (double)totalHits / totalKeys : 0,
            LastClearedUtc = DateTime.UtcNow
        };
    }

    /// <summary>Get statistics for specific key</summary>
    public async Task<KeyStatsDto> GetKeyStatsAsync(string key)
    {
        if (!_metadata.TryGetValue(key, out var metadata))
        {
            return new KeyStatsDto
            {
                Key = key,
                HitCount = 0,
                MissCount = 0,
                InvalidationPatterns = Array.Empty<string>()
            };
        }

        return new KeyStatsDto
        {
            Key = key,
            HitCount = metadata.HitCount,
            MissCount = metadata.MissCount,
            InvalidationPatterns = metadata.InvalidationPatterns,
            FirstSeenUtc = metadata.FirstSeenUtc,
            LastAccessedUtc = metadata.LastAccessedUtc
        };
    }

    /// <summary>Clear all cache</summary>
    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all query cache");

        _metadata.Clear();

        _logger.LogInformation("Query cache cleared");
    }

    /// <summary>Record cache hit</summary>
    private void RecordCacheHit(string key)
    {
        _metadata.AddOrUpdate(
            key,
            new CacheKeyMetadata { HitCount = 1 },
            (k, existing) =>
            {
                existing.HitCount++;
                existing.LastAccessedUtc = DateTime.UtcNow;
                return existing;
            });
    }

    /// <summary>Record cache miss</summary>
    private void RecordCacheMiss(string key, string[] invalidationPatterns)
    {
        _metadata.AddOrUpdate(
            key,
            new CacheKeyMetadata
            {
                MissCount = 1,
                InvalidationPatterns = invalidationPatterns,
                FirstSeenUtc = DateTime.UtcNow,
                LastAccessedUtc = DateTime.UtcNow
            },
            (k, existing) =>
            {
                existing.MissCount++;
                existing.LastAccessedUtc = DateTime.UtcNow;
                if (invalidationPatterns.Length > 0)
                    existing.InvalidationPatterns = invalidationPatterns;
                return existing;
            });
    }
}

/// <summary>Cache key metadata</summary>
public class CacheKeyMetadata
{
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public string[] InvalidationPatterns { get; set; } = Array.Empty<string>();
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Cache statistics DTO</summary>
public record CacheStatsDto
{
    public int TotalKeys { get; set; }
    public int TotalHits { get; set; }
    public int TotalMisses { get; set; }
    public double HitRate { get; set; }
    public double AverageHitsPerKey { get; set; }
    public DateTime LastClearedUtc { get; set; }
}

/// <summary>Key-specific statistics DTO</summary>
public record KeyStatsDto
{
    public string Key { get; set; } = string.Empty;
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public string[] InvalidationPatterns { get; set; } = Array.Empty<string>();
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastAccessedUtc { get; set; }
}
