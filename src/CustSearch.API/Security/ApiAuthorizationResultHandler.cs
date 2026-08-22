using CustSearch.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace CustSearch.API.Security;

/// <summary>
/// Returns consistent JSON for unauthenticated (401) and unauthorized (403) API requests.
/// </summary>
public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged || authorizeResult.Forbidden)
        {
            var isForbidden = authorizeResult.Forbidden;
            context.Response.StatusCode = isForbidden
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;
            if (!isForbidden)
            {
                context.Response.Headers.WWWAuthenticate = "Bearer";
            }

            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
                isForbidden ? "PermissionDenied" : "AuthenticationRequired",
                isForbidden
                    ? "The authenticated user does not have permission for this operation."
                    : "A valid authenticated session is required.",
                context.TraceIdentifier)).ConfigureAwait(false);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
    }
}
