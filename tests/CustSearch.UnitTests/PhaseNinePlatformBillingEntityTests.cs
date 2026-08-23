using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseNinePlatformBillingEntityTests
{
    private static readonly DateTime Now = new(2026, 8, 23, 17, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void PlatformPlanKeepsCommercialMetadataAndStaffQuota()
    {
        var plan = SubscriptionPlan.CreatePlatform(
            " pro ", "Pro", "Platform plan", 999m, 9999m, "inr", 14,
            5, 20, 12, 8, 10000, 50000, "{\"exports\":true}", 2, Now);

        Assert.Equal("PRO", plan.PlanCode);
        Assert.Equal("INR", plan.Currency);
        Assert.Equal(14, plan.TrialDays);
        Assert.Equal(12, plan.MaxStaff);
        Assert.Equal(2, plan.DisplayOrder);
        Assert.DoesNotContain("Retail", plan.GetType().Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrialSubscriptionUsesServerAuthoritativePeriod()
    {
        var trialEnd = Now.AddDays(14);
        var subscription = TenantSubscription.Create(10, 20, BillingCycle.Monthly, SubscriptionStatus.Trial, Now, trialEnd, true, Now);
        subscription.ConfigureAuthoritativePeriod(trialEnd, Now, trialEnd, false, Now);

        Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
        Assert.Equal(trialEnd, subscription.TrialEndUtc);
        Assert.Equal(Now, subscription.CurrentPeriodStartUtc);
        Assert.Equal(trialEnd, subscription.CurrentPeriodEndUtc);
        Assert.False(subscription.CancelAtPeriodEnd);
    }

    [Fact]
    public void RenewalMovesSubscriptionToActivePeriod()
    {
        var firstEnd = Now.AddMonths(1);
        var subscription = TenantSubscription.Create(10, 20, BillingCycle.Monthly, SubscriptionStatus.Active, Now, firstEnd, true, Now);
        var renewalEnd = firstEnd.AddMonths(1);

        subscription.Renew(firstEnd, renewalEnd, firstEnd);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(firstEnd, subscription.CurrentPeriodStartUtc);
        Assert.Equal(renewalEnd, subscription.CurrentPeriodEndUtc);
        Assert.False(subscription.CancelAtPeriodEnd);
    }

    [Fact]
    public void PlanChangeUpdatesPlanAndBillingCycleWithoutRetailMutation()
    {
        var firstEnd = Now.AddMonths(1);
        var subscription = TenantSubscription.Create(10, 20, BillingCycle.Monthly, SubscriptionStatus.Active, Now, firstEnd, true, Now);
        var changeUtc = Now.AddDays(3);
        var annualEnd = changeUtc.AddYears(1);

        subscription.ChangePlan(30, BillingCycle.Annual, changeUtc, annualEnd, changeUtc);

        Assert.Equal(30, subscription.SubscriptionPlanId);
        Assert.Equal(BillingCycle.Annual, subscription.BillingCycle);
        Assert.Equal(annualEnd, subscription.CurrentPeriodEndUtc);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void CancellationCanBeDeferredToPeriodEnd()
    {
        var periodEnd = Now.AddMonths(1);
        var subscription = TenantSubscription.Create(10, 20, BillingCycle.Monthly, SubscriptionStatus.Active, Now, periodEnd, true, Now);

        subscription.Cancel(true, Now.AddDays(1));

        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.False(subscription.AutoRenew);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.CancelledUtc);
    }

    [Fact]
    public void ImmediateCancellationIsAuditable()
    {
        var periodEnd = Now.AddMonths(1);
        var subscription = TenantSubscription.Create(10, 20, BillingCycle.Monthly, SubscriptionStatus.Active, Now, periodEnd, true, Now);
        var cancelledUtc = Now.AddDays(1);

        subscription.Cancel(false, cancelledUtc);

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.Equal(cancelledUtc, subscription.CancelledUtc);
        Assert.Equal(cancelledUtc, subscription.EndsUtc);
    }

    [Fact]
    public void PlatformInvoiceCalculatesTotalsServerSide()
    {
        var invoice = PlatformInvoice.Create(10, 99, "plat-10-1", "inr", Now, Now.AddDays(7), 1000m, 100m, 162m, Now);

        Assert.Equal(1062m, invoice.Total);
        Assert.Equal(0m, invoice.PaidAmount);
        Assert.Equal(PlatformInvoiceStatus.Open, invoice.Status);
    }

    [Fact]
    public void PlatformInvoiceRejectsOverpayment()
    {
        var invoice = PlatformInvoice.Create(10, 99, "plat-10-2", "INR", Now, Now.AddDays(7), 1000m, 0m, 0m, Now);

        Assert.Throws<InvalidOperationException>(() => invoice.ApplySuccessfulPayment(1000.01m, Now.AddMinutes(1)));
    }

    [Fact]
    public void PlatformInvoiceItemPreservesPlanSnapshot()
    {
        var item = PlatformInvoiceItem.Create(10, 55, 20, "Pro 2026", "Annual plan", 1m, 12000m, 1000m, 1980m, Now);

        Assert.Equal("Pro 2026", item.PlanName);
        Assert.Equal("Annual plan", item.Description);
        Assert.Equal(12000m, item.Subtotal);
        Assert.Equal(12980m, item.Total);
    }

    [Fact]
    public void PlatformPaymentCallbackIdentityIsIdempotentAndFactual()
    {
        var payment = PlatformPayment.Create(10, 55, "UPI", 500m, "INR", "gw-1", "txn-1", Now, PlatformPaymentStatus.Successful, Now);

        Assert.True(payment.MatchesCallback(55, 500m, "inr", "txn-1"));
        Assert.False(payment.MatchesCallback(55, 501m, "INR", "txn-1"));
        Assert.Equal(PlatformPaymentStatus.Successful, payment.Status);
    }

    [Fact]
    public void PlatformBillingEntitiesNeverReferenceRetailInvoiceTypes()
    {
        var types = new[] { typeof(PlatformInvoice), typeof(PlatformInvoiceItem), typeof(PlatformPayment) };
        foreach (var type in types)
        {
            var propertyTypes = type.GetProperties().Select(x => x.PropertyType.Name).ToArray();
            Assert.DoesNotContain(propertyTypes, name => name.Contains("RetailInvoice", StringComparison.Ordinal));
            Assert.DoesNotContain(propertyTypes, name => name.Contains("RetailPayment", StringComparison.Ordinal));
        }
    }
}
