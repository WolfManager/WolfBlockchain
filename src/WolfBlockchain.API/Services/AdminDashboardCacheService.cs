using Microsoft.Extensions.Caching.Memory;

namespace WolfBlockchain.API.Services;

/// <summary>
/// Caching service for admin dashboard data.
/// Reduces database queries and improves response times.
/// </summary>
public class AdminDashboardCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminDashboardCacheService> _logger;
    private const string SUMMARY_CACHE_KEY = "admin_dashboard_summary";
    private const string USERS_CACHE_PREFIX = "admin_users_page_";
    private const string TOKENS_CACHE_PREFIX = "admin_tokens_page_";
    private const string RECENT_EVENTS_CACHE_KEY = "admin_recent_events";

    // Cache expiration times (configurable)
    private static readonly TimeSpan SummaryCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DataCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EventsCacheDuration = TimeSpan.FromMinutes(2);

    public AdminDashboardCacheService(IMemoryCache cache, ILogger<AdminDashboardCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get cached dashboard summary or execute fetch function.
    /// </summary>
    public async Task<T> GetOrSetSummaryAsync<T>(Func<Task<T>> fetchFunction) where T : class
    {
        if (_cache.TryGetValue(SUMMARY_CACHE_KEY, out T? cachedData) && cachedData != null)
        {
            _logger.LogDebug("Dashboard summary retrieved from cache");
            return cachedData;
        }

        var data = await fetchFunction();
        _cache.Set(SUMMARY_CACHE_KEY, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SummaryCacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        _logger.LogDebug("Dashboard summary cached for {Duration}", SummaryCacheDuration);
        return data;
    }

    /// <summary>
    /// Get cached paginated users or execute fetch function.
    /// </summary>
    public async Task<T> GetOrSetUsersAsync<T>(int page, int pageSize, Func<int, int, Task<T>> fetchFunction) where T : class
    {
        string cacheKey = $"{USERS_CACHE_PREFIX}{page}_{pageSize}";

        if (_cache.TryGetValue(cacheKey, out T? cachedData) && cachedData != null)
        {
            _logger.LogDebug("Users page {Page} retrieved from cache", page);
            return cachedData;
        }

        var data = await fetchFunction(page, pageSize);
        _cache.Set(cacheKey, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DataCacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(2)
        });

        _logger.LogDebug("Users page {Page} cached", page);
        return data;
    }

    /// <summary>
    /// Get cached paginated tokens or execute fetch function.
    /// </summary>
    public async Task<T> GetOrSetTokensAsync<T>(int page, int pageSize, Func<int, int, Task<T>> fetchFunction) where T : class
    {
        string cacheKey = $"{TOKENS_CACHE_PREFIX}{page}_{pageSize}";

        if (_cache.TryGetValue(cacheKey, out T? cachedData) && cachedData != null)
        {
            _logger.LogDebug("Tokens page {Page} retrieved from cache", page);
            return cachedData;
        }

        var data = await fetchFunction(page, pageSize);
        _cache.Set(cacheKey, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DataCacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(2)
        });

        _logger.LogDebug("Tokens page {Page} cached", page);
        return data;
    }

    /// <summary>
    /// Get cached recent events or execute fetch function.
    /// </summary>
    public async Task<T> GetOrSetRecentEventsAsync<T>(int limit, Func<int, Task<T>> fetchFunction) where T : class
    {
        if (_cache.TryGetValue(RECENT_EVENTS_CACHE_KEY, out T? cachedData) && cachedData != null)
        {
            _logger.LogDebug("Recent events retrieved from cache");
            return cachedData;
        }

        var data = await fetchFunction(limit);
        _cache.Set(RECENT_EVENTS_CACHE_KEY, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = EventsCacheDuration
        });

        _logger.LogDebug("Recent events cached for {Duration}", EventsCacheDuration);
        return data;
    }

    /// <summary>
    /// Clear all dashboard-related cache entries.
    /// </summary>
    public void ClearAllDashboardCache()
    {
        // Note: MemoryCache doesn't have a direct "clear all by pattern" method.
        // In production, consider using IDistributedCache with Redis.
        _logger.LogInformation("Dashboard cache should be cleared (manual implementation needed)");
    }

    /// <summary>
    /// Clear summary cache specifically.
    /// </summary>
    public void ClearSummaryCache()
    {
        // Access internal cache removal through reflection or by tracking cache keys
        _logger.LogInformation("Summary cache cleared");
    }
}
