using CustSearch.Application.Authentication;
using CustSearch.Application.PlatformBilling;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.PlatformBilling;

/// <summary>
/// Phase 9 subscription billing service. This service intentionally has no dependency on Phase 8 retail billing types.
/// Tenant-facing reads derive TenantId exclusively from the authenticated server context.
/// </summary>
public sealed class PlatformBillingService(
    CustSearchDbContext db,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : IPlatformBillingService
{
    public async Task<IReadOnlyList<PlatformPlanView>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var rows = await db.SubscriptionPlans.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.PlanName)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(MapPlan).ToArray();
    }

    public async Task<PlatformPlanView> CreatePlanAsync(
        SavePlatformPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        ValidatePlan(command);
        var code = command.PlanCode.Trim().ToUpperInvariant();
        if (await db.SubscriptionPlans.AnyAsync(x => x.PlanCode == code, cancellationToken).ConfigureAwait(false))
            throw new PlatformBusinessRuleException("Plan code already exists.");

        SubscriptionPlan plan;
        try
        {
            plan = SubscriptionPlan.CreatePlatform(
                command.PlanCode, command.Name, command.Description, command.MonthlyPrice, command.AnnualPrice,
                command.Currency, command.TrialDays, command.MaxStores, command.MaxUsers, command.MaxStaff,
                command.MaxCameras, command.MaxMonthlyRecognitions, command.MaxMonthlyApiCalls,
                command.FeatureLimitsJson, command.DisplayOrder, UtcNow());
            plan.SetActive(command.IsActive, UtcNow());
        }
        catch (ArgumentException exception)
        {
            throw new PlatformBusinessRuleException(exception.Message);
        }

        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapPlan(plan);
    }

    public async Task<PlatformPlanView> UpdatePlanAsync(
        long planId,
        SavePlatformPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        ValidatePlan(command);
        var plan = await db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == planId, cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Subscription plan");
        if (!string.Equals(plan.PlanCode, command.PlanCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new PlatformBusinessRuleException("Plan code is immutable.");

        try
        {
            plan.UpdatePlatform(
                command.Name, command.Description, command.MonthlyPrice, command.AnnualPrice, command.Currency,
                command.TrialDays, command.MaxStores, command.MaxUsers, command.MaxStaff, command.MaxCameras,
                command.MaxMonthlyRecognitions, command.MaxMonthlyApiCalls, command.FeatureLimitsJson,
                command.DisplayOrder, command.IsActive, UtcNow());
        }
        catch (ArgumentException exception)
        {
            throw new PlatformBusinessRuleException(exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapPlan(plan);
    }

    public async Task<IReadOnlyList<PlatformSubscriptionView>> ListSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var rows = await db.TenantSubscriptions.AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Include(x => x.Tenant)
            .OrderByDescending(x => x.StartsUtc)
            .Take(1000)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(MapSubscription).ToArray();
    }

    public async Task<PlatformSubscriptionView> CreateSubscriptionAsync(
        long tenantId,
        CreatePlatformSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var plan = await RequireActivePlanAsync(command.PlanId, cancellationToken).ConfigureAwait(false);
        var cycle = ParseCycle(command.BillingCycle);
        var start = RequireUtc(command.StartUtc, nameof(command.StartUtc));
        if (await CurrentSubscriptionQuery(tenantId).AnyAsync(cancellationToken).ConfigureAwait(false))
            throw new PlatformBusinessRuleException("Tenant already has a current subscription. Use renew or change-plan.");

        await ValidateQuotasAsync(tenant, plan, cancellationToken).ConfigureAwait(false);
        DateTime? trialEnd = command.UseTrial && plan.TrialDays > 0 ? start.AddDays(plan.TrialDays) : null;
        var status = trialEnd.HasValue ? SubscriptionStatus.Trial : SubscriptionStatus.Active;
        var periodEnd = trialEnd ?? PeriodEnd(start, cycle);
        var now = UtcNow();
        var subscription = TenantSubscription.Create(tenant.Id, plan.Id, cycle, status, start, periodEnd, true, now);
        subscription.ConfigureAuthoritativePeriod(trialEnd, start, periodEnd, false, now);
        db.TenantSubscriptions.Add(subscription);
        tenant.SetQuotas(plan.MaxStores, plan.MaxUsers, plan.MaxStaff, plan.MaxCameras, now);
        tenant.ConfigureSubscription(
            plan.Id,
            status,
            status == SubscriptionStatus.Trial ? start : null,
            trialEnd,
            status == SubscriptionStatus.Trial ? null : start,
            status == SubscriptionStatus.Trial ? null : periodEnd,
            now);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await db.Entry(subscription).Reference(x => x.SubscriptionPlan).LoadAsync(cancellationToken).ConfigureAwait(false);
        await db.Entry(subscription).Reference(x => x.Tenant).LoadAsync(cancellationToken).ConfigureAwait(false);
        return MapSubscription(subscription);
    }

    public async Task<PlatformSubscriptionView> RenewSubscriptionAsync(
        long tenantId,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var subscription = await RequireCurrentSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        await db.Entry(subscription).Reference(x => x.SubscriptionPlan).LoadAsync(cancellationToken).ConfigureAwait(false);
        var plan = subscription.SubscriptionPlan;
        await ValidateQuotasAsync(tenant, plan, cancellationToken).ConfigureAwait(false);
        var now = UtcNow();
        var start = subscription.CurrentPeriodEndUtc is { } currentEnd && currentEnd > now ? currentEnd : now;
        var end = PeriodEnd(start, subscription.BillingCycle);
        subscription.Renew(start, end, now);
        tenant.SetQuotas(plan.MaxStores, plan.MaxUsers, plan.MaxStaff, plan.MaxCameras, now);
        tenant.ConfigureSubscription(plan.Id, SubscriptionStatus.Active, null, null, start, end, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapSubscription(subscription);
    }

    public async Task<PlatformSubscriptionView> ChangePlanAsync(
        long tenantId,
        ChangePlatformPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var subscription = await RequireCurrentSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var plan = await RequireActivePlanAsync(command.PlanId, cancellationToken).ConfigureAwait(false);
        var cycle = ParseCycle(command.BillingCycle);
        await ValidateQuotasAsync(tenant, plan, cancellationToken).ConfigureAwait(false);
        var start = UtcNow();
        var end = PeriodEnd(start, cycle);
        subscription.ChangePlan(plan.Id, cycle, start, end, start);
        tenant.SetQuotas(plan.MaxStores, plan.MaxUsers, plan.MaxStaff, plan.MaxCameras, start);
        tenant.ConfigureSubscription(plan.Id, SubscriptionStatus.Active, null, null, start, end, start);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await db.Entry(subscription).Reference(x => x.SubscriptionPlan).LoadAsync(cancellationToken).ConfigureAwait(false);
        return MapSubscription(subscription);
    }

    public async Task<PlatformSubscriptionView> CancelSubscriptionAsync(
        long tenantId,
        bool atPeriodEnd,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var subscription = await RequireCurrentSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var now = UtcNow();
        subscription.Cancel(atPeriodEnd, now);
        if (!atPeriodEnd)
            tenant.ConfigureSubscription(subscription.SubscriptionPlanId, SubscriptionStatus.Cancelled, null, null, tenant.SubscriptionStartsUtc, now, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await db.Entry(subscription).Reference(x => x.SubscriptionPlan).LoadAsync(cancellationToken).ConfigureAwait(false);
        return MapSubscription(subscription);
    }

    public async Task<IReadOnlyList<PlatformInvoiceView>> ListInvoicesAsync(CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        return await MapInvoicesAsync(
            db.PlatformInvoices.AsNoTracking().OrderByDescending(x => x.InvoiceUtc).Take(1000),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlatformInvoiceView> GenerateInvoiceAsync(
        long tenantId,
        GeneratePlatformInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var subscription = await RequireCurrentSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        await db.Entry(subscription).Reference(x => x.SubscriptionPlan).LoadAsync(cancellationToken).ConfigureAwait(false);
        var plan = subscription.SubscriptionPlan;
        if (subscription.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            throw new PlatformBusinessRuleException("Cancelled or expired subscriptions cannot be invoiced.");

        var rate = subscription.BillingCycle == BillingCycle.Annual
            ? plan.AnnualPrice ?? plan.MonthlyPrice * 12
            : plan.MonthlyPrice;
        ArgumentOutOfRangeException.ThrowIfNegative(command.DiscountAmount);
        ArgumentOutOfRangeException.ThrowIfNegative(command.TaxAmount);
        if (command.DiscountAmount > rate + command.TaxAmount)
            throw new PlatformBusinessRuleException("Invoice discount exceeds billable value.");
        var now = UtcNow();
        var due = command.DueUtc.HasValue ? RequireUtc(command.DueUtc.Value, nameof(command.DueUtc)) : now.AddDays(7);
        var invoiceNumber = $"PLAT-{tenant.Id}-{now:yyyyMMddHHmmssfff}";

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var invoice = PlatformInvoice.Create(
            tenant.Id, subscription.Id, invoiceNumber, plan.Currency, now, due,
            rate, command.DiscountAmount, command.TaxAmount, now);
        db.PlatformInvoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var item = PlatformInvoiceItem.Create(
            tenant.Id, invoice.Id, plan.Id, plan.PlanName, plan.Description, 1, rate,
            command.DiscountAmount, command.TaxAmount, now);
        db.PlatformInvoiceItems.Add(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return MapInvoice(invoice, [item]);
    }

    public async Task<IReadOnlyList<PlatformPaymentView>> ListPaymentsAsync(CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var rows = await db.PlatformPayments.AsNoTracking()
            .OrderByDescending(x => x.PaymentUtc)
            .Take(1000)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(MapPayment).ToArray();
    }

    public async Task<PlatformPaymentView> RecordPaymentAsync(
        RecordPlatformPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var invoice = await db.PlatformInvoices.SingleOrDefaultAsync(
            x => x.Id == command.PlatformInvoiceId,
            cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Platform invoice");
        if (command.Amount <= 0)
            throw new PlatformBusinessRuleException("Payment amount must be positive.");
        if (!string.Equals(invoice.Currency, command.Currency, StringComparison.OrdinalIgnoreCase))
            throw new PlatformBusinessRuleException("Payment currency must match the platform invoice.");
        var reference = command.TransactionReference.Trim();
        if (reference.Length == 0)
            throw new PlatformBusinessRuleException("Transaction reference is required.");

        var existing = await db.PlatformPayments.SingleOrDefaultAsync(
            x => x.TenantId == invoice.TenantId && x.TransactionReference == reference,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!existing.MatchesCallback(invoice.Id, command.Amount, command.Currency, reference))
                throw new PlatformBusinessRuleException("Transaction reference was already used with different payment facts.");
            if (existing.Status == command.Status)
                return MapPayment(existing);
            if (existing.Status == PlatformPaymentStatus.Refunded)
                throw new PlatformBusinessRuleException("A refunded platform payment cannot transition to another status.");
            if (existing.Status == PlatformPaymentStatus.Successful && command.Status != PlatformPaymentStatus.Refunded)
                throw new PlatformBusinessRuleException("A successful platform payment may only remain successful or become refunded.");

            var now = UtcNow();
            if (existing.Status != PlatformPaymentStatus.Successful && command.Status == PlatformPaymentStatus.Successful)
                invoice.ApplySuccessfulPayment(command.Amount, now);
            else if (existing.Status == PlatformPaymentStatus.Successful && command.Status == PlatformPaymentStatus.Refunded)
                invoice.ReverseSuccessfulPayment(command.Amount, now);

            existing.UpdateStatus(command.Status, now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return MapPayment(existing);
        }

        if (command.Status == PlatformPaymentStatus.Refunded)
            throw new PlatformBusinessRuleException("A payment cannot be created directly in Refunded status without a prior successful callback.");
        if (command.Status == PlatformPaymentStatus.Successful)
            invoice.ApplySuccessfulPayment(command.Amount, UtcNow());
        var payment = PlatformPayment.Create(
            invoice.TenantId,
            invoice.Id,
            command.PaymentMethod,
            command.Amount,
            command.Currency,
            command.GatewayReference,
            reference,
            RequireUtc(command.PaymentUtc, nameof(command.PaymentUtc)),
            command.Status,
            UtcNow());
        db.PlatformPayments.Add(payment);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapPayment(payment);
    }

    public async Task<TenantPlatformBillingSummary> GetTenantSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var tenant = await db.Tenants.AsNoTracking().SingleAsync(x => x.Id == tenantId, cancellationToken).ConfigureAwait(false);
        var subscription = await GetTenantSubscriptionInternalAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var latestPayment = await db.PlatformPayments.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.PaymentUtc)
            .Select(x => (PlatformPaymentStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var count = await db.PlatformInvoices.CountAsync(x => x.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
        return new TenantPlatformBillingSummary(
            subscription, subscription?.PlanName, tenant.MaxStores, tenant.MaxUsers, tenant.MaxStaff, tenant.MaxCameras,
            subscription?.CurrentPeriodEndUtc, latestPayment?.ToString(), count);
    }

    public Task<PlatformSubscriptionView?> GetTenantSubscriptionAsync(CancellationToken cancellationToken = default) =>
        GetTenantSubscriptionInternalAsync(RequireTenantId(), cancellationToken);

    public Task<IReadOnlyList<PlatformInvoiceView>> ListTenantInvoicesAsync(CancellationToken cancellationToken = default) =>
        MapInvoicesAsync(
            db.PlatformInvoices.AsNoTracking().Where(x => x.TenantId == RequireTenantId()).OrderByDescending(x => x.InvoiceUtc),
            cancellationToken);

    public async Task<IReadOnlyList<PlatformPaymentView>> ListTenantPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var rows = await db.PlatformPayments.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.PaymentUtc)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(MapPayment).ToArray();
    }

    private async Task<PlatformSubscriptionView?> GetTenantSubscriptionInternalAsync(long tenantId, CancellationToken cancellationToken)
    {
        var row = await db.TenantSubscriptions.AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Include(x => x.Tenant)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.StartsUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : MapSubscription(row);
    }

    private async Task<IReadOnlyList<PlatformInvoiceView>> MapInvoicesAsync(
        IQueryable<PlatformInvoice> query,
        CancellationToken cancellationToken)
    {
        var invoices = await query.ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if (invoices.Length == 0)
            return [];
        var ids = invoices.Select(x => x.Id).ToArray();
        var items = await db.PlatformInvoiceItems.AsNoTracking()
            .Where(x => ids.Contains(x.PlatformInvoiceId))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return invoices.Select(invoice =>
            MapInvoice(invoice, items.Where(x => x.PlatformInvoiceId == invoice.Id).ToArray())).ToArray();
    }

    private static PlatformInvoiceView MapInvoice(PlatformInvoice invoice, IReadOnlyList<PlatformInvoiceItem> items) =>
        new(
            invoice.Id, invoice.TenantId, invoice.TenantSubscriptionId, invoice.InvoiceNumber, invoice.Currency,
            invoice.InvoiceUtc, invoice.DueUtc, invoice.Status.ToString(), invoice.Subtotal, invoice.DiscountAmount,
            invoice.TaxAmount, invoice.Total, invoice.PaidAmount,
            items.Select(item => new PlatformInvoiceItemView(
                item.Id, item.SubscriptionPlanId, item.PlanName, item.Description, item.Quantity, item.Rate,
                item.DiscountAmount, item.TaxAmount, item.Subtotal, item.Total)).ToArray());

    private static PlatformPaymentView MapPayment(PlatformPayment payment) =>
        new(
            payment.Id, payment.TenantId, payment.PlatformInvoiceId, payment.PaymentMethod, payment.Amount,
            payment.Currency, payment.GatewayReference, payment.TransactionReference, payment.PaymentUtc,
            payment.Status.ToString());

    private static PlatformPlanView MapPlan(SubscriptionPlan plan) =>
        new(
            plan.Id, plan.PlanCode, plan.PlanName, plan.Description, plan.MonthlyPrice, plan.AnnualPrice,
            plan.Currency, plan.TrialDays, plan.MaxStores, plan.MaxUsers, plan.MaxStaff, plan.MaxCameras,
            plan.MaxMonthlyRecognitions, plan.MaxMonthlyApiCalls, plan.FeatureLimitsJson, plan.IsActive,
            plan.DisplayOrder, plan.CreatedUtc, plan.UpdatedUtc);

    private static PlatformSubscriptionView MapSubscription(TenantSubscription subscription) =>
        new(
            subscription.Id, subscription.TenantId, subscription.SubscriptionPlanId,
            subscription.SubscriptionPlan.PlanCode, subscription.SubscriptionPlan.PlanName,
            subscription.BillingCycle.ToString(), subscription.Status.ToString(), subscription.StartsUtc,
            subscription.TrialEndUtc, subscription.CurrentPeriodStartUtc, subscription.CurrentPeriodEndUtc,
            subscription.CancelAtPeriodEnd, subscription.CancelledUtc,
            subscription.Tenant.MaxStores, subscription.Tenant.MaxUsers, subscription.Tenant.MaxStaff,
            subscription.Tenant.MaxCameras);

    private async Task<Tenant> RequireTenantAsync(long tenantId, CancellationToken cancellationToken) =>
        await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken).ConfigureAwait(false)
        ?? throw new PlatformResourceNotFoundException("Tenant");

    private async Task<SubscriptionPlan> RequireActivePlanAsync(long planId, CancellationToken cancellationToken) =>
        await db.SubscriptionPlans.SingleOrDefaultAsync(
            x => x.Id == planId && x.IsActive,
            cancellationToken).ConfigureAwait(false)
        ?? throw new PlatformBusinessRuleException("Selected plan is inactive or unavailable.");

    private async Task<TenantSubscription> RequireCurrentSubscriptionAsync(long tenantId, CancellationToken cancellationToken) =>
        await CurrentSubscriptionQuery(tenantId)
            .Include(x => x.Tenant)
            .OrderByDescending(x => x.StartsUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? throw new PlatformResourceNotFoundException("Current tenant subscription");

    private IQueryable<TenantSubscription> CurrentSubscriptionQuery(long tenantId) =>
        db.TenantSubscriptions.Where(x =>
            x.TenantId == tenantId
            && (x.Status == SubscriptionStatus.Trial
                || x.Status == SubscriptionStatus.Active
                || x.Status == SubscriptionStatus.PastDue
                || x.Status == SubscriptionStatus.Suspended));

    private async Task ValidateQuotasAsync(Tenant tenant, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        var stores = await db.Stores.CountAsync(x => x.TenantId == tenant.Id && x.IsActive, cancellationToken).ConfigureAwait(false);
        var users = await db.UserAccounts.CountAsync(x => x.TenantId == tenant.Id && x.IsActive, cancellationToken).ConfigureAwait(false);
        var staff = await db.StaffProfiles.CountAsync(x => x.TenantId == tenant.Id && x.IsActive, cancellationToken).ConfigureAwait(false);
        var cameras = await db.TenantUsageSnapshots.AsNoTracking()
            .Where(x => x.TenantId == tenant.Id)
            .OrderByDescending(x => x.PeriodEndUtc)
            .Select(x => (int?)x.CameraCount)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? 0;
        if (stores > plan.MaxStores || users > plan.MaxUsers || staff > plan.MaxStaff || cameras > plan.MaxCameras)
            throw new PlatformBusinessRuleException("Selected plan quota is below current authoritative tenant usage.");
    }

    private static BillingCycle ParseCycle(string value) =>
        Enum.TryParse<BillingCycle>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new PlatformBusinessRuleException("Billing cycle must be Monthly or Annual.");

    private static DateTime PeriodEnd(DateTime start, BillingCycle cycle) =>
        cycle == BillingCycle.Annual ? start.AddYears(1) : start.AddMonths(1);

    private static DateTime RequireUtc(DateTime value, string name) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new PlatformBusinessRuleException($"{name} must be UTC.");

    private static void ValidatePlan(SavePlatformPlanCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.PlanCode)
            || string.IsNullOrWhiteSpace(command.Name)
            || string.IsNullOrWhiteSpace(command.Currency))
            throw new PlatformBusinessRuleException("Plan code, name and currency are required.");
    }

    private long RequireTenantId()
    {
        if (currentUser.IsPlatformAdmin || currentUser.TenantId is not { } tenantId)
            throw new PlatformBusinessRuleException("A tenant-scoped authenticated identity is required.");
        return tenantId;
    }

    private void RequirePlatformAdmin()
    {
        if (!currentUser.IsPlatformAdmin)
            throw new PlatformBusinessRuleException("Platform administrator access is required.");
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
