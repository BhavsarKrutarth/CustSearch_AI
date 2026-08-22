namespace CustSearch.Application.Tenancy;

/// <summary>
/// Allows tenant ownership access or an explicitly granted and auditable platform support permission.
/// </summary>
public static class TenantAccessGuard
{
    public static void EnsureAccess(
        long resourceTenantId,
        long? authenticatedTenantId,
        IReadOnlySet<string> permissions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resourceTenantId);
        ArgumentNullException.ThrowIfNull(permissions);

        if (authenticatedTenantId == resourceTenantId)
        {
            return;
        }

        // Platform scope alone never grants tenant-data access. Callers must also audit the
        // target tenant and business reason whenever this explicit support permission is used.
        if (authenticatedTenantId is null
            && permissions.Contains(Authorization.PermissionCatalog.Platform.SupportAccessTenant))
        {
            return;
        }

        throw new UnauthorizedAccessException("The requested resource is outside the authenticated tenant.");
    }
}
