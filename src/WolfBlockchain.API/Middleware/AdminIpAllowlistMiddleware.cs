namespace WolfBlockchain.API.Middleware;

/// <summary>
/// Restricționează accesul la aplicație pentru IP-urile permise în single-admin mode.
/// Includes IP rate limiting, failover logging, and security metrics.
/// </summary>
public class AdminIpAllowlistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminIpAllowlistMiddleware> _logger;
    private readonly bool _singleAdminMode;
    private readonly HashSet<string> _allowedIps;
    private readonly HashSet<string> _blockedIps;
    private readonly Dictionary<string, (int attempts, DateTime lastAttempt)> _failedAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _blockDuration;
    private readonly object _lockObject = new();

    public AdminIpAllowlistMiddleware(RequestDelegate next, ILogger<AdminIpAllowlistMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _singleAdminMode = configuration.GetValue<bool>("Security:SingleAdminMode", true);
        
        _allowedIps = configuration.GetSection("Security:AdminAllowedIps").Get<string[]>()?
            .Select(ip => ip.Trim())
            .Where(ip => !string.IsNullOrWhiteSpace(ip))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _blockedIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _maxFailedAttempts = configuration.GetValue<int>("Security:MaxFailedAttempts", 5);
        _blockDuration = TimeSpan.FromMinutes(configuration.GetValue<int>("Security:BlockDurationMinutes", 15));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_singleAdminMode)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (IsInfrastructureBypassPath(path))
        {
            await _next(context);
            return;
        }

        var remoteIp = GetClientIp(context);

        // Check if IP is temporarily blocked due to failed attempts
        if (IsIpTemporarilyBlocked(remoteIp))
        {
            _logger.LogWarning("Blocked request from temporarily blocked IP: {Ip}, Path: {Path}", remoteIp, path);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Too many failed attempts. Try again later." });
            return;
        }

        // Verify allowlist is not empty
        if (_allowedIps.Count == 0 && !_allowedIps.Contains("*"))
        {
            _logger.LogCritical("Single-admin mode active, but no IP allowlist configured. Blocking all access for path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Access blocked. Admin IP allowlist is empty or not properly configured." });
            return;
        }

        // Check if IP is in allowlist
        var isAllowed = _allowedIps.Contains(remoteIp) || _allowedIps.Contains("*");
        
        if (!isAllowed)
        {
            RecordFailedAttempt(remoteIp);
            _logger.LogWarning("Blocked request from non-allowlisted IP: {Ip}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}", 
                remoteIp, path, context.Request.Method, context.Request.Headers.UserAgent);
            
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Access denied for this IP." });
            return;
        }

        // Reset failed attempts for successful authentication
        ResetFailedAttempts(remoteIp);
        _logger.LogInformation("Authorized request from IP: {Ip}, Path: {Path}, Method: {Method}", 
            remoteIp, path, context.Request.Method);

        await _next(context);
    }

    /// <summary>
    /// Keeps infrastructure endpoints accessible for probes and metrics scraping.
    /// </summary>
    private static bool IsInfrastructureBypassPath(string path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/ready", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/metrics", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts client IP from headers or connection info.
    /// Supports X-Forwarded-For, X-Real-IP, and direct RemoteIpAddress.
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        // Check X-Forwarded-For header (proxy/load balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
        {
            var forwardedIp = xForwardedFor.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(forwardedIp) && IsValidIp(forwardedIp))
                return forwardedIp;
        }

        // Check X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var xRealIp))
        {
            var realIp = xRealIp.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(realIp) && IsValidIp(realIp))
                return realIp;
        }

        // Use connection remote IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return IsValidIp(remoteIp) ? remoteIp : "unknown";
    }

    /// <summary>
    /// Validates if string is a valid IP address.
    /// </summary>
    private static bool IsValidIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || ip == "unknown")
            return false;
        
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    /// <summary>
    /// Records a failed attempt for an IP.
    /// </summary>
    private void RecordFailedAttempt(string ip)
    {
        lock (_lockObject)
        {
            if (_failedAttempts.TryGetValue(ip, out var attempt))
            {
                var newCount = attempt.attempts + 1;
                _failedAttempts[ip] = (newCount, DateTime.UtcNow);

                if (newCount >= _maxFailedAttempts)
                {
                    _blockedIps.Add(ip);
                    _logger.LogError("IP blocked due to excessive failed attempts: {Ip}, Attempts: {Count}", ip, newCount);
                }
            }
            else
            {
                _failedAttempts[ip] = (1, DateTime.UtcNow);
            }
        }
    }

    /// <summary>
    /// Resets failed attempts for an IP.
    /// </summary>
    private void ResetFailedAttempts(string ip)
    {
        lock (_lockObject)
        {
            if (_failedAttempts.ContainsKey(ip))
            {
                _failedAttempts.Remove(ip);
            }

            if (_blockedIps.Contains(ip))
            {
                _blockedIps.Remove(ip);
                _logger.LogInformation("IP unblocked after successful authentication: {Ip}", ip);
            }
        }
    }

    /// <summary>
    /// Checks if IP is temporarily blocked.
    /// </summary>
    private bool IsIpTemporarilyBlocked(string ip)
    {
        lock (_lockObject)
        {
            if (!_blockedIps.Contains(ip))
                return false;

            if (!_failedAttempts.TryGetValue(ip, out var attempt))
                return true;

            var timeSinceLastAttempt = DateTime.UtcNow - attempt.lastAttempt;
            if (timeSinceLastAttempt > _blockDuration)
            {
                _blockedIps.Remove(ip);
                _failedAttempts.Remove(ip);
                _logger.LogInformation("IP unblocked after block duration expired: {Ip}", ip);
                return false;
            }

            return true;
        }
    }
}
