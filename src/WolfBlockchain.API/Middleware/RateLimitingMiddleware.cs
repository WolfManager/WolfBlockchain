namespace WolfBlockchain.API.Validation;

/// <summary>
/// Rate Limiting Middleware - Protecție contra DDoS și abuse - PRODUCTION GRADE
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly Dictionary<string, RateLimitBucket> _buckets = new();
    private static readonly object _lockObject = new();

    // Configuration
    private const int MaxRequestsPerMinute = 100;
    private const int MaxRequestsPerHour = 5000;
    private const int CleanupIntervalSeconds = 300; // 5 minutes
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);

        if (IsBypassedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!IsRequestAllowed(clientId))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." });
            return;
        }

        // Cleanup old entries periodically
        CleanupOldEntries();

        await _next(context);
    }

    private static bool IsBypassedPath(PathString path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/metrics", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica daca request-ul e permis
    /// </summary>
    private bool IsRequestAllowed(string clientId)
    {
        lock (_lockObject)
        {
            if (!_buckets.ContainsKey(clientId))
            {
                _buckets[clientId] = new RateLimitBucket();
            }

            var bucket = _buckets[clientId];
            var now = DateTime.UtcNow;

            // Reset minute counter
            if ((now - bucket.MinuteResetTime).TotalMinutes >= 1)
            {
                bucket.RequestsThisMinute = 0;
                bucket.MinuteResetTime = now;
            }

            // Reset hour counter
            if ((now - bucket.HourResetTime).TotalHours >= 1)
            {
                bucket.RequestsThisHour = 0;
                bucket.HourResetTime = now;
            }

            // Check limits
            if (bucket.RequestsThisMinute >= MaxRequestsPerMinute)
            {
                return false;
            }

            if (bucket.RequestsThisHour >= MaxRequestsPerHour)
            {
                return false;
            }

            // Increment counters
            bucket.RequestsThisMinute++;
            bucket.RequestsThisHour++;
            bucket.LastRequestTime = now;

            return true;
        }
    }

    /// <summary>
    /// Obtine client identifier (IP address)
    /// </summary>
    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get from X-Forwarded-For header (for proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        // Fall back to remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Curata intrari vechi
    /// </summary>
    private void CleanupOldEntries()
    {
        lock (_lockObject)
        {
            if ((DateTime.UtcNow - _lastCleanup).TotalSeconds < CleanupIntervalSeconds)
                return;

            var expiredKeys = _buckets
                .Where(x => (DateTime.UtcNow - x.Value.LastRequestTime).TotalHours > 1)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _buckets.Remove(key);
            }

            _lastCleanup = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Rate limit bucket pentru track request-uri
    /// </summary>
    private class RateLimitBucket
    {
        public int RequestsThisMinute { get; set; }
        public int RequestsThisHour { get; set; }
        public DateTime MinuteResetTime { get; set; } = DateTime.UtcNow;
        public DateTime HourResetTime { get; set; } = DateTime.UtcNow;
        public DateTime LastRequestTime { get; set; } = DateTime.UtcNow;
    }
}
