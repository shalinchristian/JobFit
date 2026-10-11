using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using JobFit.Services;

namespace JobFit.Middleware;

/// <summary>
/// Limits for JSON API bodies: 10 MB per request (checked from Content-Length before anything is read,
/// and enforced by Kestrel for chunked bodies), returned as a JSON 413 instead of a reset connection.
/// </summary>
public class ApiBodyLimitMiddleware
{
    public const long MaxApiBodyBytes = 10L * 1024 * 1024;

    /// <summary>The one 413 message used everywhere (body size, field length, Kestrel limit).</summary>
    public static readonly string TooLargeMessage =
        $"Request is too large. API bodies can be up to 10 MB and each text field up to {DocumentTextExtractor.MaxTextChars:N0} characters.";
    private readonly RequestDelegate _next;

    public ApiBodyLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (context.Request.ContentLength > MaxApiBodyBytes)
            {
                await TooLarge(context);
                return;
            }

            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (feature is { IsReadOnly: false })
                feature.MaxRequestBodySize = MaxApiBodyBytes;
        }

        await _next(context);
    }

    public static Task TooLarge(HttpContext context, string? field = null)
    {
        var message = field is null ? TooLargeMessage : $"{TooLargeMessage} '{field}' is over the limit.";
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return context.Response.WriteAsJsonAsync(new { error = message, statusCode = 413 });
    }
}

/// <summary>Rejects any API string field longer than <see cref="DocumentTextExtractor.MaxTextChars"/> with 413.</summary>
public class TextLengthLimitFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            foreach (var prop in arg.GetType().GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanRead))
            {
                if (prop.GetValue(arg) is string s && s.Length > DocumentTextExtractor.MaxTextChars)
                {
                    context.Result = new ObjectResult(new
                    {
                        error = $"{ApiBodyLimitMiddleware.TooLargeMessage} '{prop.Name}' is over the limit.",
                        statusCode = 413
                    }) { StatusCode = StatusCodes.Status413PayloadTooLarge };
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
