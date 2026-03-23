namespace WolfBlockchain.API.Middleware;

/// <summary>
/// Request Size Limiting Middleware - Protecție contra upload-uri mari - PRODUCTION GRADE
/// </summary>
public class RequestSizeLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSizeLimitingMiddleware> _logger;

    // Limits in bytes
    private const long MaxRequestBodySize = 10_485_760; // 10 MB
    private const long MaxFileUploadSize = 104_857_600; // 100 MB

    public RequestSizeLimitingMiddleware(RequestDelegate next, ILogger<RequestSizeLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var contentLength = context.Request.ContentLength ?? 0;

        // Check file upload endpoints
        if (context.Request.Path.StartsWithSegments("/api/upload"))
        {
            if (contentLength > MaxFileUploadSize)
            {
                _logger.LogWarning("File upload exceeds size limit: {ContentLength} bytes", contentLength);
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = $"File size exceeds maximum allowed ({MaxFileUploadSize / 1024 / 1024} MB)" 
                });
                return;
            }
        }
        else
        {
            // Check regular request body size
            if (contentLength > MaxRequestBodySize)
            {
                _logger.LogWarning("Request body exceeds size limit: {ContentLength} bytes", contentLength);
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = $"Request body exceeds maximum allowed ({MaxRequestBodySize / 1024 / 1024} MB)" 
                });
                return;
            }
        }

        await _next(context);
    }
}
