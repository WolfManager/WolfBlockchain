using System;
using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WolfBlockchain.API.Middleware;

/// <summary>Middleware pentru compresie GZIP automată a răspunsurilor</summary>
public class ResponseCompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCompressionMiddleware> _logger;
    private const int MinimumSizeToCompress = 1024; // 1KB - compress responses larger than this

    public ResponseCompressionMiddleware(RequestDelegate next, ILogger<ResponseCompressionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store original response stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Wrap response stream with compression if needed
            var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString();

            if (acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Content-Encoding"] = "gzip";
                context.Response.Headers["Vary"] = "Accept-Encoding";

                using var compressedStream = new GZipStream(originalBodyStream, CompressionMode.Compress, leaveOpen: true);
                context.Response.Body = compressedStream;

                await _next(context);

                _logger.LogDebug("Response compressed with GZIP for {Path}", context.Request.Path);
            }
            else if (acceptEncoding.Contains("deflate", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Content-Encoding"] = "deflate";
                context.Response.Headers["Vary"] = "Accept-Encoding";

                using var compressedStream = new DeflateStream(originalBodyStream, CompressionMode.Compress, leaveOpen: true);
                context.Response.Body = compressedStream;

                await _next(context);

                _logger.LogDebug("Response compressed with Deflate for {Path}", context.Request.Path);
            }
            else
            {
                // No compression - proceed normally
                await _next(context);
            }
        }
        finally
        {
            // Restore original response stream
            context.Response.Body = originalBodyStream;
        }
    }
}

/// <summary>Middleware pentru HTTP caching headers</summary>
public class HttpCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpCachingMiddleware> _logger;

    // Cache policies per path pattern
    private static readonly Dictionary<string, CachePolicy> CachePolicies = new(StringComparer.OrdinalIgnoreCase)
    {
        // Static content - 30 days
        { "/css/", new CachePolicy { MaxAge = 2592000, Public = true, Immutable = true } },
        { "/js/", new CachePolicy { MaxAge = 2592000, Public = true, Immutable = true } },
        { "/images/", new CachePolicy { MaxAge = 2592000, Public = true } },
        { "/fonts/", new CachePolicy { MaxAge = 2592000, Public = true } },

        // API endpoints - 5 minutes (with validation)
        { "/api/users", new CachePolicy { MaxAge = 300, Public = false, MustRevalidate = true } },
        { "/api/tokens", new CachePolicy { MaxAge = 300, Public = false, MustRevalidate = true } },
        { "/api/blockchain", new CachePolicy { MaxAge = 60, Public = false, MustRevalidate = true } },

        // Health check - no cache
        { "/health", new CachePolicy { MaxAge = 0, NoCache = true } },

        // Sensitive endpoints - no cache
        { "/api/security", new CachePolicy { MaxAge = 0, NoCache = true, NoStore = true } },
        { "/api/admin", new CachePolicy { MaxAge = 0, NoCache = true, NoStore = true } },
    };

    public HttpCachingMiddleware(RequestDelegate next, ILogger<HttpCachingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var policy = GetCachePolicy(path);

        // Add cache headers based on policy
        ApplyCachePolicy(context.Response, policy);

        // Add ETag support
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            // Generate ETag if content was modified
            if (context.Response.StatusCode == 200 && policy.UseETag)
            {
                var content = memoryStream.ToArray();
                var etag = GenerateETag(content);
                context.Response.Headers["ETag"] = $"\"{etag}\"";

                // Check If-None-Match header
                if (context.Request.Headers.TryGetValue("If-None-Match", out var clientETag))
                {
                    if (clientETag.ToString() == $"\"{etag}\"")
                    {
                        context.Response.StatusCode = 304; // Not Modified
                        context.Response.ContentLength = 0;
                        _logger.LogDebug("304 Not Modified returned for {Path} with ETag", path);
                        return;
                    }
                }
            }

            // Copy response to original stream
            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static CachePolicy GetCachePolicy(string path)
    {
        foreach (var (pattern, policy) in CachePolicies)
        {
            if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return policy;
            }
        }

        // Default: no cache for unknown paths
        return new CachePolicy { MaxAge = 0, NoCache = true };
    }

    private static void ApplyCachePolicy(HttpResponse response, CachePolicy policy)
    {
        var cacheControlParts = new List<string>();

        if (policy.NoStore)
        {
            cacheControlParts.Add("no-store");
        }
        else if (policy.NoCache)
        {
            cacheControlParts.Add("no-cache");
            cacheControlParts.Add("must-revalidate");
        }
        else if (policy.MaxAge > 0)
        {
            cacheControlParts.Add($"max-age={policy.MaxAge}");

            if (policy.Public)
                cacheControlParts.Add("public");
            else
                cacheControlParts.Add("private");

            if (policy.MustRevalidate)
                cacheControlParts.Add("must-revalidate");

            if (policy.Immutable)
                cacheControlParts.Add("immutable");
        }
        else
        {
            cacheControlParts.Add("no-cache");
        }

        response.Headers["Cache-Control"] = string.Join(", ", cacheControlParts);
        
        if (!policy.NoStore && !policy.NoCache)
        {
            response.Headers["Pragma"] = "cache";
        }
        else
        {
            response.Headers["Pragma"] = "no-cache";
        }

        // Vary header for cache validation
        if (!response.Headers.ContainsKey("Vary"))
        {
            response.Headers["Vary"] = "Accept-Encoding";
        }
    }

    private static string GenerateETag(byte[] content)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(content);
        return Convert.ToHexString(hash).ToLowerInvariant()[..16]; // First 16 chars of hash
    }
}

/// <summary>Cache policy configuration</summary>
public record CachePolicy
{
    public int MaxAge { get; set; } = 0;
    public bool Public { get; set; } = false;
    public bool Private { get; set; } = true;
    public bool NoCache { get; set; } = false;
    public bool NoStore { get; set; } = false;
    public bool MustRevalidate { get; set; } = false;
    public bool Immutable { get; set; } = false;
    public bool UseETag { get; set; } = true;
}
