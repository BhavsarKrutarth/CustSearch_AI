namespace CustSearch.Application.Authorization;

/// <summary>
/// Builds and parses dynamic policy names without scattering security string formats.
/// </summary>
public static class AuthorizationPolicyNames
{
    public const string PermissionPrefix = "Permission:";
    public const string PlatformScope = "Scope:Platform";
    public const string TenantScope = "Scope:Tenant";

    public static string ForPermission(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return $"{PermissionPrefix}{permission}";
    }

    public static bool TryGetPermission(string policyName, out string permission)
    {
        permission = policyName.StartsWith(PermissionPrefix, StringComparison.Ordinal)
            ? policyName[PermissionPrefix.Length..]
            : string.Empty;
        return PermissionCatalog.All.Contains(permission);
    }
}
