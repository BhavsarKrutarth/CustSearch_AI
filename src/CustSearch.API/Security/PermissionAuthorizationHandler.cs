using CustSearch.Application.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace CustSearch.API.Security;

/// <summary>
/// Grants a permission policy only when the validated JWT contains the exact permission claim.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(CustomClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
