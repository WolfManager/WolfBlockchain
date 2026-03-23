using System.Net;
using System.Text.Json;

namespace WolfBlockchain.API.Middleware;

/// <summary>
/// Global exception handler middleware - PRODUCTION GRADE
/// Centralizează error handling și nu expune stack traces
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}, Method: {Method}", 
                context.Request.Path, context.Request.Method);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = "An error occurred processing your request",
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case ArgumentNullException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Required parameter is missing";
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = argEx.Message;
                break;

            case InvalidOperationException invOpEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = invOpEx.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Unauthorized access";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "Resource not found";
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "Internal server error";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Standard error response
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = "";
    public string TraceId { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
