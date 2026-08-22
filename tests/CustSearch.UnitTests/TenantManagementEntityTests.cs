using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

/// <summary>
/// Verifies Phase 4 tenant lifecycle, quota and subscription rules before persistence.
/// </summary>
public sealed class TenantManagementEntityTests
{
    private static readonly DateTime CreatedUtc = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TenantProfileAndLifecycleChangesAdvanceConcurrencyVersion()
    {
        var tenant = CreateTenant();
        var originalVersion = tenant.RowVersion.ToArray();

        tenant.Suspend("Billing review", CreatedUtc.AddMinutes(1));

        Assert.True(tenant.IsSuspended);
        Assert.Equal("Billing review", tenant.SuspensionReason);
        Assert.NotEqual(originalVersion, tenant.RowVersion);
    }

    [Fact]
    public void SubscriptionPlanRejectsInvalidQuotaAndCanBeReactivated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SubscriptionPlan.Create(
            "INVALID", "Invalid", 0, null, 0, 1, 1, null, null, CreatedUtc));

        var plan = SubscriptionPlan.Create("PRO", "Professional", 100, 1000, 5, 25, 50, 100000, 200000, CreatedUtc);
        plan.Deactivate(CreatedUtc.AddMinutes(1));
        plan.Activate(CreatedUtc.AddMinutes(2));

        Assert.True(plan.IsActive);
    }

    [Fact]
    public void UsageSnapshotRejectsAnInvalidReportingPeriod()
    {
        Assert.Throws<ArgumentException>(() => TenantUsageSnapshot.Capture(
            10,
            CreatedUtc.AddDays(1),
            CreatedUtc,
            0,
            0,
            0,
            0,
            0,
            CreatedUtc.AddDays(1)));
    }

    [Fact]
    public void QuotaOverrideRequiresAtLeastOnePositiveLimitAndReason()
    {
        Assert.Throws<ArgumentException>(() => TenantQuotaOverride.Create(
            10, null, null, null, null, null, "Capacity exception", 1, CreatedUtc, null));
    }

    private static Tenant CreateTenant() => Tenant.Create(
        "TEN-000001",
        "Example Retail Private Limited",
        "Example Retail",
        "Asha Patel",
        "asha@example.test",
        "+910000000000",
        "IN",
        "Asia/Calcutta",
        "INR",
        2,
        10,
        20,
        CreatedUtc);
}
