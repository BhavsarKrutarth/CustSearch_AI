using Serilog.Context;

namespace CustSearch.API.Middleware;

/// <summary>
/// Validates or creates the request correlation ID, returns it to the client and enriches logs.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValid(candidate) ? candidate! : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString() ?? string.Empty))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Restricts external correlation IDs to a compact log-safe character set.
    /// </summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 64
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}
