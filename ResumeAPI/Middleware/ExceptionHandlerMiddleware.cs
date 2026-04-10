using System.Net;
using System.Text.Json;

namespace ResumeAPI.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches unhandled exceptions anywhere in the pipeline and returns
/// a consistent JSON error response instead of leaking stack traces.
/// 
/// Registered in Program.cs via: app.UseMiddleware&lt;ExceptionHandlerMiddleware&gt;()
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass through to the next middleware/controller
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Map exception types to HTTP status codes
        var (statusCode, message) = ex switch
        {
            ArgumentException      => (HttpStatusCode.BadRequest,           ex.Message),
            KeyNotFoundException   => (HttpStatusCode.NotFound,             ex.Message),
            InvalidOperationException => (HttpStatusCode.ServiceUnavailable, ex.Message),
            _                      => (HttpStatusCode.InternalServerError,  "An unexpected error occurred. Please try again.")
        };

        // Serialize a clean error envelope
        var response = JsonSerializer.Serialize(new
        {
            error      = message,
            statusCode = (int)statusCode,
            timestamp  = DateTime.UtcNow
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        return context.Response.WriteAsync(response);
    }
}
