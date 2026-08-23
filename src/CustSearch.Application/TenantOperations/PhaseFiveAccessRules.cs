namespace CustSearch.Application.TenantOperations;

/// <summary>Pure Phase 5 authorization/quota rules shared by the infrastructure decorator and regression tests.</summary>
public static class PhaseFiveAccessRules
{
    private static readonly HashSet<string> TenantWideRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "TenantAdmin", "TenantOwner", "ShopOwner",
    };

    public static bool HasStoreOverlap(IEnumerable<long> callerStoreIds, IEnumerable<long> targetStoreIds)
    {
        var caller = callerStoreIds as IReadOnlySet<long> ?? callerStoreIds.ToHashSet();
        return targetStoreIds.Any(caller.Contains);
    }

    public static bool ContainsTenantWideRole(IEnumerable<string> roles) => roles.Any(TenantWideRoles.Contains);

    public static bool RequestedStoresAreWithinScope(IEnumerable<long> callerStoreIds, IReadOnlyCollection<long> requestedStoreIds)
    {
        if (requestedStoreIds.Count == 0) return false;
        var caller = callerStoreIds as IReadOnlySet<long> ?? callerStoreIds.ToHashSet();
        return requestedStoreIds.All(caller.Contains);
    }

    public static bool ReactivationWouldExceedQuota(bool isCurrentlyActive, bool requestedActive, int activeUsers, int maxUsers) =>
        !isCurrentlyActive && requestedActive && activeUsers >= maxUsers;
}
