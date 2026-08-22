using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Security;

/// <summary>
/// Creates permission policies on demand from the central catalog and rejects unknown names.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!AuthorizationPolicyNames.TryGetPermission(policyName, out var permission))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
