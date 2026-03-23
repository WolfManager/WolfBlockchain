using System.Diagnostics;
using System.Text;

namespace WolfBlockchain.API.Middleware;

/// <summary>
/// Middleware for detailed request/response logging with latency tracking.
/// Logs all API requests and responses to help with performance monitoring.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks (too noisy)
        if (context.Request.Path.ToString().Contains("/health"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);
        
        // Store original response stream
        var originalResponseStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            stopwatch.Stop();
            var responseBody = await ReadResponseBodyAsync(memoryStream);

            // Log request/response details
            LogRequestResponse(context, requestBody, responseBody, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            await memoryStream.CopyToAsync(originalResponseStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, 
                "Request {Method} {Path} failed after {Elapsed}ms. Error: {Error}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseStream;
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.ContentLength.HasValue || request.ContentLength == 0)
            return string.Empty;

        request.EnableBuffering();
        var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0; // Reset stream position
        return body;
    }

    private async Task<string> ReadResponseBodyAsync(MemoryStream memoryStream)
    {
        memoryStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(memoryStream, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position
        return body;
    }

    private void LogRequestResponse(
        HttpContext context,
        string requestBody,
        string responseBody,
        long elapsedMilliseconds)
    {
        var request = context.Request;
        var response = context.Response;
        var method = request.Method;
        var path = request.Path.ToString();
        var statusCode = response.StatusCode;

        // Log level based on status code
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            >= 200 => LogLevel.Information,
            _ => LogLevel.Debug
        };

        // Truncate large bodies for logging
        var truncatedRequestBody = TruncateBody(requestBody, 200);
        var truncatedResponseBody = TruncateBody(responseBody, 200);

        _logger.Log(
            logLevel,
            "API Request/Response: {Method} {Path} -> {StatusCode} ({Elapsed}ms). " +
            "Request: {RequestBody} | Response: {ResponseBody}",
            method,
            path,
            statusCode,
            elapsedMilliseconds,
            truncatedRequestBody,
            truncatedResponseBody);

        // Log slow requests (> 500ms)
        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Slow API request detected: {Method} {Path} took {Elapsed}ms",
                method,
                path,
                elapsedMilliseconds);
        }
    }

    private static string TruncateBody(string body, int maxLength)
    {
        if (string.IsNullOrEmpty(body))
            return "(empty)";

        return body.Length > maxLength ? body[..maxLength] + "..." : body;
    }
}

/// <summary>
/// Extension method for registering the logging middleware.
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
