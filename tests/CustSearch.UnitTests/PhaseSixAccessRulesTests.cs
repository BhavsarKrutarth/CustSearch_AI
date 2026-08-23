using CustSearch.Application.ShopperCustomers;

namespace CustSearch.UnitTests;

public sealed class PhaseSixAccessRulesTests
{
    [Theory]
    [InlineData("TenantAdmin", true)]
    [InlineData("TenantOwner", true)]
    [InlineData("ShopOwner", true)]
    [InlineData("StoreManager", false)]
    [InlineData("CRMStaff", false)]
    public void TenantWideRoleRecognitionIsExplicit(string role, bool expected)
    {
        IReadOnlySet<string> roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { role };
        Assert.Equal(expected, PhaseSixAccessRules.IsTenantWide(roles));
    }

    [Fact]
    public void StoreScopedCustomerRequiresOverlap()
    {
        IReadOnlySet<long> allowed = new HashSet<long> { 10, 20 };
        Assert.True(PhaseSixAccessRules.HasCustomerVisibility(new long[] { 20, 30 }, allowed, false));
        Assert.False(PhaseSixAccessRules.HasCustomerVisibility(new long[] { 30, 40 }, allowed, false));
    }

    [Fact]
    public void RequestedStoresCannotEscapeCallerScope()
    {
        IReadOnlySet<long> allowed = new HashSet<long> { 10, 20 };
        Assert.True(PhaseSixAccessRules.RequestedStoresWithinScope(new long[] { 10, 20 }, allowed, false));
        Assert.False(PhaseSixAccessRules.RequestedStoresWithinScope(new long[] { 10, 99 }, allowed, false));
        Assert.True(PhaseSixAccessRules.RequestedStoresWithinScope(new long[] { 99 }, allowed, true));
    }

    [Theory]
    [InlineData(0, 0, 1, 25)]
    [InlineData(-1, 500, 1, 100)]
    [InlineData(3, 50, 3, 50)]
    public void PagingIsNormalizedAndCapped(int page, int size, int expectedPage, int expectedSize)
    {
        Assert.Equal((expectedPage, expectedSize), PhaseSixAccessRules.NormalizePaging(page, size));
    }
}
