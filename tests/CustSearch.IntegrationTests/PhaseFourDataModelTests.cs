using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CustSearch.IntegrationTests;

/// <summary>
/// Verifies the EF model stays aligned with the versioned Phase 4 SQL schema.
/// </summary>
public sealed class PhaseFourDataModelTests
{
    [Fact]
    public void TenantManagementEntitiesUseExpectedTablesKeysAndConcurrencyTokens()
    {
        using var context = CreateContext();
        var model = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var tenant = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(model.FindEntityType(typeof(Tenant)));
        var plan = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(model.FindEntityType(typeof(SubscriptionPlan)));
        var subscription = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(model.FindEntityType(typeof(TenantSubscription)));
        var usage = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(model.FindEntityType(typeof(TenantUsageSnapshot)));
        var quota = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(model.FindEntityType(typeof(TenantQuotaOverride)));
        var audit = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(model.FindEntityType(typeof(AuditLog)));

        Assert.Equal("Tenants", tenant.GetTableName());
        Assert.True(tenant.FindProperty(nameof(Tenant.UpdatedUtc))!.IsConcurrencyToken);
        Assert.True(tenant.FindProperty(nameof(Tenant.RowVersion))!.IsConcurrencyToken);
        Assert.Equal(16, tenant.FindProperty(nameof(Tenant.RowVersion))!.GetMaxLength());

        Assert.Equal("SubscriptionPlans", plan.GetTableName());
        Assert.True(plan.FindProperty(nameof(SubscriptionPlan.UpdatedUtc))!.IsConcurrencyToken);
        Assert.True(plan.GetIndexes().Single(index =>
            index.Properties.Count == 1 && index.Properties[0].Name == nameof(SubscriptionPlan.PlanCode)).IsUnique);

        Assert.Equal("TenantSubscriptions", subscription.GetTableName());
        Assert.Equal("TenantUsageSnapshots", usage.GetTableName());
        Assert.True(usage.GetIndexes().Single(index => index.Properties.Select(item => item.Name).SequenceEqual(
            [nameof(TenantUsageSnapshot.TenantId), nameof(TenantUsageSnapshot.PeriodStartUtc), nameof(TenantUsageSnapshot.PeriodEndUtc)])).IsUnique);
        Assert.Equal("TenantQuotaOverrides", quota.GetTableName());
        Assert.Equal("AuditLogs", audit.GetTableName());
        Assert.Equal(4000, audit.FindProperty(nameof(AuditLog.BeforeJson))!.GetMaxLength());
    }

    private static CustSearchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustSearchDbContext>()
            .UseSqlServer("Server=(local);Database=CustSearch_AI;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new CustSearchDbContext(options);
    }
}
