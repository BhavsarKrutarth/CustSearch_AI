using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CustSearch.API.Security;

/// <summary>
/// Marks an endpoint with one exact permission from the shared permission catalog.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        if (!PermissionCatalog.All.Contains(permission))
        {
            throw new ArgumentOutOfRangeException(nameof(permission), permission, "Permission is not in the catalog.");
        }

        Policy = AuthorizationPolicyNames.ForPermission(permission);
    }
}
