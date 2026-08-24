using CustSearch.Application.Authentication;
using CustSearch.Application.PlatformBilling;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.PlatformBilling;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

public sealed class PhaseNinePlatformBillingServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 23, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PlatformAdminCanCreateTrialRenewChangePlanAndScheduleCancellation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var starter = await fixture.AddPlanAsync("STARTER", 100m, 14, 5, 20, 10, 5);
        var pro = await fixture.AddPlanAsync("PRO", 500m, 0, 20, 50, 30, 20);
        fixture.UsePlatformAdmin();
        var service = fixture.CreateService();
        var created = await service.CreateSubscriptionAsync(fixture.TenantA.Id, new(starter.Id, "Monthly", Now, true));
        Assert.Equal("Trial", created.Status);
        Assert.Equal(Now.AddDays(14), created.TrialEndUtc);
        var renewed = await service.RenewSubscriptionAsync(fixture.TenantA.Id);
        Assert.Equal("Active", renewed.Status);
        Assert.Equal(Now.AddDays(14), renewed.CurrentPeriodStartUtc);
        Assert.Equal(Now.AddDays(14).AddMonths(1), renewed.CurrentPeriodEndUtc);
        var changed = await service.ChangePlanAsync(fixture.TenantA.Id, new(pro.Id, "Annual"));
        Assert.Equal(pro.Id, changed.PlanId);
        Assert.Equal("Annual", changed.BillingCycle);
        Assert.Equal(30, changed.MaxStaff);
        var cancelled = await service.CancelSubscriptionAsync(fixture.TenantA.Id, true);
        Assert.True(cancelled.CancelAtPeriodEnd);
        Assert.Equal("Active", cancelled.Status);
    }

    [Fact]
    public async Task StaffQuotaRejectsPlanBelowAuthoritativeActiveStaffUsage()
    {
        await using var fixture = await Fixture.CreateAsync();
        var constrained = await fixture.AddPlanAsync("SMALL", 100m, 0, 10, 20, 1, 10);
        await fixture.AddStaffAsync(fixture.TenantA, "S001");
        await fixture.AddStaffAsync(fixture.TenantA, "S002");
        fixture.UsePlatformAdmin();
        var exception = await Assert.ThrowsAsync<PlatformBusinessRuleException>(() => fixture.CreateService().CreateSubscriptionAsync(fixture.TenantA.Id, new(constrained.Id, "Monthly", Now, false)));
        Assert.Contains("quota", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(fixture.Db.TenantSubscriptions.Where(x => x.TenantId == fixture.TenantA.Id));
    }

    [Fact]
    public async Task TenantBillingReadsNeverReturnAnotherTenantsPlatformInvoice()
    {
        await using var fixture = await Fixture.CreateAsync();
        var plan = await fixture.AddPlanAsync("STANDARD", 250m, 0, 10, 30, 20, 10);
        fixture.UsePlatformAdmin();
        var platform = fixture.CreateService();
        await platform.CreateSubscriptionAsync(fixture.TenantA.Id, new(plan.Id, "Monthly", Now, false));
        await platform.CreateSubscriptionAsync(fixture.TenantB.Id, new(plan.Id, "Monthly", Now, false));
        var invoiceA = await platform.GenerateInvoiceAsync(fixture.TenantA.Id, new(0m, 0m, null));
        var invoiceB = await platform.GenerateInvoiceAsync(fixture.TenantB.Id, new(0m, 0m, null));
        fixture.UseTenant(fixture.TenantA);
        var tenantInvoices = await fixture.CreateService().ListTenantInvoicesAsync();
        Assert.Single(tenantInvoices);
        Assert.Equal(invoiceA.Id, tenantInvoices[0].Id);
        Assert.DoesNotContain(tenantInvoices, x => x.Id == invoiceB.Id || x.TenantId == fixture.TenantB.Id);
    }

    [Fact]
    public async Task SuccessfulPaymentCallbackIsIdempotentByTenantTransactionReference()
    {
        await using var fixture = await Fixture.CreateAsync();
        var plan = await fixture.AddPlanAsync("PAY", 100m, 0, 10, 30, 20, 10);
        fixture.UsePlatformAdmin();
        var service = fixture.CreateService();
        await service.CreateSubscriptionAsync(fixture.TenantA.Id, new(plan.Id, "Monthly", Now, false));
        var invoice = await service.GenerateInvoiceAsync(fixture.TenantA.Id, new(0m, 0m, null));
        var command = new RecordPlatformPaymentCommand(invoice.Id, "UPI", invoice.Total, "INR", "gateway-1", "txn-idempotent-1", Now, PlatformPaymentStatus.Successful);
        var first = await service.RecordPaymentAsync(command);
        var second = await service.RecordPaymentAsync(command);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await fixture.Db.PlatformPayments.CountAsync(x => x.TenantId == fixture.TenantA.Id));
        var storedInvoice = await fixture.Db.PlatformInvoices.AsNoTracking().SingleAsync(x => x.Id == invoice.Id);
        Assert.Equal(invoice.Total, storedInvoice.PaidAmount);
        Assert.Equal(PlatformInvoiceStatus.Paid, storedInvoice.Status);
    }

    [Fact]
    public async Task ConflictingDuplicatePaymentReferenceIsRejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var plan = await fixture.AddPlanAsync("PAY2", 100m, 0, 10, 30, 20, 10);
        fixture.UsePlatformAdmin();
        var service = fixture.CreateService();
        await service.CreateSubscriptionAsync(fixture.TenantA.Id, new(plan.Id, "Monthly", Now, false));
        var invoice = await service.GenerateInvoiceAsync(fixture.TenantA.Id, new(0m, 0m, null));
        await service.RecordPaymentAsync(new(invoice.Id, "UPI", 50m, "INR", null, "txn-conflict", Now, PlatformPaymentStatus.Pending));
        var exception = await Assert.ThrowsAsync<PlatformBusinessRuleException>(() => service.RecordPaymentAsync(new(invoice.Id, "UPI", 40m, "INR", null, "txn-conflict", Now, PlatformPaymentStatus.Pending)));
        Assert.Contains("different payment facts", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TenantScopedIdentityCannotManagePlatformPlanCatalog()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.UseTenant(fixture.TenantA);
        var exception = await Assert.ThrowsAsync<PlatformBusinessRuleException>(() => fixture.CreateService().ListPlansAsync());
        Assert.Contains("Platform administrator", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly MutableCurrentUserContext currentUser = new();
        private readonly FixedTimeProvider timeProvider = new(Now);
        private Fixture(SqliteConnection connection, CustSearchDbContext db, Tenant tenantA, Tenant tenantB){this.connection=connection;Db=db;TenantA=tenantA;TenantB=tenantB;}
        public CustSearchDbContext Db { get; }
        public Tenant TenantA { get; }
        public Tenant TenantB { get; }
        public static async Task<Fixture> CreateAsync()
        {
            var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();var options=new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(connection).Options;var db=new CustSearchDbContext(options);await db.Database.EnsureCreatedAsync();
            var tenantA=Tenant.Create("T-P9-A","Phase Nine A Pvt Ltd","Phase Nine A","Owner A","a@phase9.test","9000000001","IN","India Standard Time","INR",50,100,50,Now);
            var tenantB=Tenant.Create("T-P9-B","Phase Nine B Pvt Ltd","Phase Nine B","Owner B","b@phase9.test","9000000002","IN","India Standard Time","INR",50,100,50,Now);
            db.Tenants.AddRange(tenantA,tenantB);await db.SaveChangesAsync();return new Fixture(connection,db,tenantA,tenantB);
        }
        public async Task<SubscriptionPlan> AddPlanAsync(string code,decimal monthlyPrice,int trialDays,int maxStores,int maxUsers,int maxStaff,int maxCameras){var plan=SubscriptionPlan.CreatePlatform(code,code,$"{code} plan",monthlyPrice,monthlyPrice*10,"INR",trialDays,maxStores,maxUsers,maxStaff,maxCameras,null,null,null,0,Now);Db.SubscriptionPlans.Add(plan);await Db.SaveChangesAsync();return plan;}
        public async Task AddStaffAsync(Tenant tenant,string code){var user=UserAccount.CreateTenant(tenant.Id,$"user-{code}",$"{code}@phase9.test",code,"hash",Now);Db.UserAccounts.Add(user);await Db.SaveChangesAsync();Db.StaffProfiles.Add(StaffProfile.Create(tenant.Id,user.Id,code,"Staff",code,null,Now));await Db.SaveChangesAsync();}
        public void UsePlatformAdmin(){currentUser.IsPlatformAdminValue=true;currentUser.TenantIdValue=null;}
        public void UseTenant(Tenant tenant){currentUser.IsPlatformAdminValue=false;currentUser.TenantIdValue=tenant.Id;}
        public PlatformBillingService CreateService()=>new(Db,currentUser,timeProvider);
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }

    private sealed class MutableCurrentUserContext : ICurrentUserContext
    {
        public bool IsPlatformAdminValue{get;set;} public long? TenantIdValue{get;set;} public bool IsAuthenticated=>true;public long UserId=>1;public long? TenantId=>TenantIdValue;public bool IsPlatformAdmin=>IsPlatformAdminValue;public string SecurityStamp=>"phase9-test";public IReadOnlySet<string> Roles=>new HashSet<string>();public IReadOnlySet<string> Permissions=>new HashSet<string>();public IReadOnlySet<long> StoreIds=>new HashSet<long>();
    }
    private sealed class FixedTimeProvider(DateTime utc):TimeProvider{private readonly DateTimeOffset current=new(utc);public override DateTimeOffset GetUtcNow()=>current;}
}
