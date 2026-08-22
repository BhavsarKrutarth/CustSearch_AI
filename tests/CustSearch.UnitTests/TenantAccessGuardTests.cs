using CustSearch.Application.Tenancy;

namespace CustSearch.UnitTests;

public sealed class TenantAccessGuardTests
{
    [Fact]
    public void TenantUserCanAccessOnlyMatchingTenant()
    {
        TenantAccessGuard.EnsureAccess(12, 12, new HashSet<string>());

        Assert.Throws<UnauthorizedAccessException>(() =>
            TenantAccessGuard.EnsureAccess(12, 13, new HashSet<string>()));
        Assert.Throws<UnauthorizedAccessException>(() =>
            TenantAccessGuard.EnsureAccess(12, null, new HashSet<string>()));
    }

    [Fact]
    public void PlatformUserNeedsExplicitSupportPermissionForCrossTenantAccess()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            TenantAccessGuard.EnsureAccess(12, null, new HashSet<string> { "PlatformBilling.View" }));

        TenantAccessGuard.EnsureAccess(
            12,
            null,
            new HashSet<string> { "PlatformSupport.AccessTenant" });
    }
}
