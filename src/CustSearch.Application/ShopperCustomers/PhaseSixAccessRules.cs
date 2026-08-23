namespace CustSearch.Application.ShopperCustomers;

/// <summary>
/// Phase 6G pure authorization helpers used by service logic and regression tests. Tenant-wide roles can see all
/// customers in their tenant; all other tenant users are constrained to authoritative StoreIds from the session.
/// </summary>
public static class PhaseSixAccessRules
{
    private static readonly HashSet<string> TenantWideRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "TenantAdmin",
        "TenantOwner",
        "ShopOwner",
    };

    public static bool IsTenantWide(IReadOnlySet<string> roles) => roles.Any(TenantWideRoles.Contains);

    public static bool CanAccessStore(long storeId, IReadOnlySet<long> allowedStoreIds, bool tenantWide) =>
        tenantWide || allowedStoreIds.Contains(storeId);

    public static bool HasCustomerVisibility(IEnumerable<long> customerStoreIds, IReadOnlySet<long> allowedStoreIds, bool tenantWide) =>
        tenantWide || customerStoreIds.Any(allowedStoreIds.Contains);

    public static bool RequestedStoresWithinScope(IEnumerable<long> requestedStoreIds, IReadOnlySet<long> allowedStoreIds, bool tenantWide) =>
        tenantWide || requestedStoreIds.All(allowedStoreIds.Contains);

    public static (int PageNumber, int PageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 25;
        return (pageNumber, Math.Min(pageSize, 100));
    }
}
