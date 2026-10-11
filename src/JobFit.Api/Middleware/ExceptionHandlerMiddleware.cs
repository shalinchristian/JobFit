using System.Net;
using System.Text.Json;

namespace JobFit.Middleware;

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

            // Antiforgery turns an over-limit form body into a bare 400. For browser form posts, send the
            // user back to the form with a friendly message instead.
            if (context.Response.StatusCode == StatusCodes.Status400BadRequest
                && !context.Response.HasStarted
                && !context.Request.Path.StartsWithSegments("/api")
                && context.Request.ContentLength > JobFit.Services.DocumentTextExtractor.MaxRequestBytes)
            {
                context.Response.Redirect("/?error=too-large");
            }
        }
        catch (Exception ex) when (IsTooLarge(ex) && !context.Response.HasStarted)
        {
            // A browser form that blew past the request limit: send the user back with a friendly message
            // (API callers get a JSON 413).
            _logger.LogWarning("Request too large for {Path}", context.Request.Path);
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await ApiBodyLimitMiddleware.TooLarge(context);
            }
            else
            {
                context.Response.Redirect("/?error=too-large");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static bool IsTooLarge(Exception ex) =>
        ex is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
        || ex is InvalidDataException { Message: var m } && m.Contains("limit", StringComparison.OrdinalIgnoreCase)
        || ex.InnerException is not null && IsTooLarge(ex.InnerException);

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
