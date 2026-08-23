using CustSearch.Application.TenantOperations;

namespace CustSearch.UnitTests;

/// <summary>Regression tests for Phase 5 store isolation, privilege boundaries and account reactivation quotas.</summary>
public sealed class PhaseFiveAccessRulesTests
{
    [Fact]
    public void StoreScopeRequiresAtLeastOneSharedStore()
    {
        Assert.True(PhaseFiveAccessRules.HasStoreOverlap([10, 11], [11, 12]));
        Assert.False(PhaseFiveAccessRules.HasStoreOverlap([10, 11], [12, 13]));
        Assert.False(PhaseFiveAccessRules.HasStoreOverlap([10, 11], []));
    }

    [Fact]
    public void StoreScopedAssignmentMustStayInsideCallerStores()
    {
        Assert.True(PhaseFiveAccessRules.RequestedStoresAreWithinScope([10, 11], [10, 11]));
        Assert.True(PhaseFiveAccessRules.RequestedStoresAreWithinScope([10, 11], [10]));
        Assert.False(PhaseFiveAccessRules.RequestedStoresAreWithinScope([10, 11], [10, 12]));
        Assert.False(PhaseFiveAccessRules.RequestedStoresAreWithinScope([10, 11], []));
    }

    [Theory]
    [InlineData("TenantAdmin")]
    [InlineData("TenantOwner")]
    [InlineData("ShopOwner")]
    public void TenantWideRolesAreRecognizedAsPrivilegeBoundary(string role)
    {
        Assert.True(PhaseFiveAccessRules.ContainsTenantWideRole(["SalesStaff", role]));
    }

    [Fact]
    public void ReactivationAtQuotaIsRejectedButOrdinaryUpdateIsNot()
    {
        Assert.True(PhaseFiveAccessRules.ReactivationWouldExceedQuota(false, true, 5, 5));
        Assert.False(PhaseFiveAccessRules.ReactivationWouldExceedQuota(true, true, 5, 5));
        Assert.False(PhaseFiveAccessRules.ReactivationWouldExceedQuota(false, false, 5, 5));
        Assert.False(PhaseFiveAccessRules.ReactivationWouldExceedQuota(false, true, 4, 5));
    }
}
