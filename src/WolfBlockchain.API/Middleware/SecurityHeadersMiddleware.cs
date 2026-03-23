namespace WolfBlockchain.API.Middleware;

/// <summary>
/// Middleware pentru adaugarea security headers - PRODUCTION GRADE
/// Implements OWASP security best practices
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Previne MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Previne clickjacking
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // Previne XSS
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Content Security Policy - STRICT for maximum security
            var csp = "default-src 'none'; " +
                      "script-src 'self'; " +
                      "style-src 'self'; " +
                      "img-src 'self' data: https:; " +
                      "font-src 'self'; " +
                      "connect-src 'self'; " +
                      "frame-ancestors 'none'; " +
                      "base-uri 'self'; " +
                      "form-action 'self'";
            
            context.Response.Headers["Content-Security-Policy"] = csp;

            // HSTS - Force HTTPS with 1 year expiration
            context.Response.Headers["Strict-Transport-Security"] = 
                "max-age=31536000; includeSubDomains; preload";

            // Referrer Policy - Don't leak referrer information
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

            // Permissions Policy - Disable dangerous APIs
            context.Response.Headers["Permissions-Policy"] = 
                "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=()";

            // Additional security headers
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
            context.Response.Headers["X-Mobile-Web-App-Capable"] = "no";
            context.Response.Headers["X-Apple-Mobile-Web-App-Status-Bar-Style"] = "black-translucent";

            // Disable caching for sensitive endpoints
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("security", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("api/tokens", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }

            _logger.LogDebug("Security headers added for path: {Path}", path);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding security headers");
            throw;
        }
    }
}
