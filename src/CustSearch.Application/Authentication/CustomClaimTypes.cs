namespace CustSearch.Application.Authentication;

/// <summary>
/// Defines the server-issued claim names used for tenant and permission authorization.
/// </summary>
public static class CustomClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string UserScope = "user_scope";
    public const string SecurityStamp = "security_stamp";
    public const string Permission = "permission";
    public const string StoreId = "store_id";
}
