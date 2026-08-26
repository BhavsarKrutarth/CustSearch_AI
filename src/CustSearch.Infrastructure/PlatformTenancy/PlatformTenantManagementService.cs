using System.Globalization;
using System.Text.Json;
using CustSearch.Application.Authorization;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.PlatformTenancy;

/// <summary>
/// Implements cross-tenant platform management with optimistic concurrency, safe audit data and transactional lifecycle changes.
/// </summary>
public sealed class PlatformTenantManagementService(
    CustSearchDbContext dbContext,
    TimeProvider timeProvider,
    IPasswordHasher<UserAccount> passwordHasher) : IPlatformTenantManagementService
{
    private static readonly string[] DefaultTenantRoleNames =
    [
        "TenantAdmin", "StoreAdmin", "Manager", "CRMStaff", "BillingStaff",
        "CameraOperator", "IntegrationAdmin", "Auditor",
    ];

    public async Task<PlatformDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = UtcNow();
        var tenants = dbContext.Tenants.AsNoTracking();
        var total = await tenants.CountAsync(cancellationToken).ConfigureAwait(false);
        var active = await tenants.CountAsync(
            tenant => tenant.IsActive && !tenant.IsSuspended,
            cancellationToken).ConfigureAwait(false);
        var trial = await tenants.CountAsync(
            tenant => tenant.SubscriptionStatus == SubscriptionStatus.Trial,
            cancellationToken).ConfigureAwait(false);
        var suspended = await tenants.CountAsync(tenant => tenant.IsSuspended, cancellationToken).ConfigureAwait(false);
        var inactive = await tenants.CountAsync(tenant => !tenant.IsActive, cancellationToken).ConfigureAwait(false);
        var billableSubscriptions = await dbContext.TenantSubscriptions.AsNoTracking()
            .Where(subscription =>
                (subscription.Status == SubscriptionStatus.Active
                    || subscription.Status == SubscriptionStatus.PastDue)
                && subscription.StartsUtc <= utcNow
                && (subscription.EndsUtc == null || subscription.EndsUtc > utcNow)
                && subscription.Tenant.IsActive
                && !subscription.Tenant.IsSuspended)
            .Select(subscription => new
            {
                subscription.BillingCycle,
                subscription.SubscriptionPlan.MonthlyPrice,
                subscription.SubscriptionPlan.AnnualPrice,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var users = await dbContext.UserAccounts.CountAsync(
            user => user.Scope == UserScope.Tenant,
            cancellationToken).ConfigureAwait(false);
        var latestUsage = await LatestUsageQuery().ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PlatformDashboardSummary(
            total,
            active,
            trial,
            suspended,
            inactive,
            billableSubscriptions.Sum(subscription => subscription.BillingCycle == BillingCycle.Annual
                ? (subscription.AnnualPrice ?? subscription.MonthlyPrice * 12m) / 12m
                : subscription.MonthlyPrice),
            users,
            latestUsage.Sum(usage => usage.CameraCount));
    }

    public async Task<PageResult<PlatformTenantListItem>> ListTenantsAsync(
        PlatformTenantQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        var tenants = dbContext.Tenants.AsNoTracking().Include(tenant => tenant.SubscriptionPlan).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            tenants = tenants.Where(tenant => tenant.TenantCode.Contains(search)
                || tenant.LegalName.Contains(search)
                || tenant.DisplayName.Contains(search)
                || tenant.PrimaryEmail.Contains(search));
        }

        if (query.PlanId is { } planId)
        {
            tenants = tenants.Where(tenant => tenant.SubscriptionPlanId == planId);
        }

        tenants = ApplyStatusFilter(tenants, query.Status);
        var total = await tenants.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var page = await tenants
            .OrderBy(tenant => tenant.TenantCode)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var ids = page.Select(tenant => tenant.Id).ToArray();
        var usage = await LatestUsageQuery()
            .Where(snapshot => ids.Contains(snapshot.TenantId))
            .ToDictionaryAsync(snapshot => snapshot.TenantId, cancellationToken)
            .ConfigureAwait(false);
        var userCounts = await dbContext.UserAccounts.AsNoTracking()
            .Where(user => user.TenantId != null && ids.Contains(user.TenantId.Value))
            .GroupBy(user => user.TenantId!.Value)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken)
            .ConfigureAwait(false);
        var items = page.Select(tenant => MapListItem(
            tenant,
            usage.GetValueOrDefault(tenant.Id),
            userCounts.GetValueOrDefault(tenant.Id))).ToArray();
        return new PageResult<PlatformTenantListItem>(items, query.Page, query.PageSize, total);
    }

    public async Task<PageResult<PlatformTenantUserListItem>> ListTenantUsersAsync(
        PlatformResourceQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        var users = dbContext.UserAccounts.AsNoTracking()
            .Where(user => user.Scope == UserScope.Tenant && user.TenantId != null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user => user.UserName.Contains(search)
                || user.DisplayName.Contains(search)
                || user.Email.Contains(search)
                || user.Tenant!.TenantCode.Contains(search)
                || user.Tenant.DisplayName.Contains(search));
        }

        var total = await users.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var page = await users.OrderBy(user => user.Tenant!.TenantCode).ThenBy(user => user.UserName)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(user => new
            {
                user.Id, TenantId = user.TenantId!.Value, user.Tenant!.TenantCode,
                TenantName = user.Tenant.DisplayName, user.UserName, user.DisplayName, user.Email,
                user.IsActive, user.LastLoginUtc,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ids = page.Select(user => user.Id).ToArray();
        var roleRows = await dbContext.UserRoles.AsNoTracking().Where(value => ids.Contains(value.UserId))
            .Select(value => new { value.UserId, RoleName = value.Role.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var roles = roleRows.GroupBy(value => value.UserId)
            .ToDictionary(group => group.Key, group => group.Select(value => value.RoleName).OrderBy(name => name).ToArray());
        var stores = await dbContext.UserStoreAssignments.AsNoTracking().Where(value => ids.Contains(value.UserId))
            .GroupBy(value => value.UserId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken)
            .ConfigureAwait(false);
        var items = page.Select(user => new PlatformTenantUserListItem(
            user.Id, user.TenantId, user.TenantCode, user.TenantName, user.UserName, user.DisplayName,
            user.Email, user.IsActive, roles.GetValueOrDefault(user.Id) ?? [], stores.GetValueOrDefault(user.Id),
            user.LastLoginUtc)).ToArray();
        return new PageResult<PlatformTenantUserListItem>(items, query.Page, query.PageSize, total);
    }

    public async Task<PageResult<PlatformStoreListItem>> ListStoresAsync(
        PlatformResourceQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        var stores = dbContext.Stores.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            stores = stores.Where(store => store.StoreCode.Contains(search)
                || store.StoreName.Contains(search)
                || store.City.Contains(search)
                || store.Tenant.TenantCode.Contains(search)
                || store.Tenant.DisplayName.Contains(search));
        }

        var total = await stores.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var page = await stores.OrderBy(store => store.Tenant.TenantCode).ThenBy(store => store.StoreCode)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(store => new
            {
                store.Id, store.TenantId, store.Tenant.TenantCode, TenantName = store.Tenant.DisplayName,
                store.StoreCode, store.StoreName, store.City, store.StateOrProvince, store.IsActive, store.UpdatedUtc,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ids = page.Select(store => store.Id).ToArray();
        var users = await dbContext.UserStoreAssignments.AsNoTracking().Where(value => ids.Contains(value.StoreId))
            .GroupBy(value => value.StoreId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken)
            .ConfigureAwait(false);
        var cameras = await dbContext.Cameras.AsNoTracking().Where(value => ids.Contains(value.StoreId))
            .GroupBy(value => value.StoreId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken)
            .ConfigureAwait(false);
        var items = page.Select(store => new PlatformStoreListItem(
            store.Id, store.TenantId, store.TenantCode, store.TenantName, store.StoreCode, store.StoreName,
            store.City, store.StateOrProvince, store.IsActive, users.GetValueOrDefault(store.Id),
            cameras.GetValueOrDefault(store.Id), store.UpdatedUtc)).ToArray();
        return new PageResult<PlatformStoreListItem>(items, query.Page, query.PageSize, total);
    }

    public async Task<PlatformTenantDetail?> GetTenantAsync(
        long tenantId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        var tenant = await dbContext.Tenants.AsNoTracking()
            .Include(candidate => candidate.SubscriptionPlan)
            .SingleOrDefaultAsync(candidate => candidate.Id == tenantId, cancellationToken)
            .ConfigureAwait(false);
        return tenant is null ? null : MapDetail(tenant);
    }

    public async Task<PlatformTenantDetail> CreateTenantAsync(
        CreatePlatformTenantCommand command,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        ValidateTimeZone(command.TimeZone);
        ValidatePassword(command.AdminPassword);
        var hasQuotaOverride = command.MaxStores.HasValue || command.MaxUsers.HasValue || command.MaxCameras.HasValue;
        if (hasQuotaOverride && string.IsNullOrWhiteSpace(command.AuditReason))
        {
            throw new PlatformBusinessRuleException("An audit reason is required for custom tenant quotas.");
        }

        var utcNow = UtcNow();
        SubscriptionPlan? plan = null;
        if (command.PlanId is { } planId)
        {
            plan = await dbContext.SubscriptionPlans.SingleOrDefaultAsync(
                candidate => candidate.Id == planId && candidate.IsActive,
                cancellationToken).ConfigureAwait(false)
                ?? throw new PlatformBusinessRuleException("The selected subscription plan is unavailable.");
        }

        var maxStores = command.MaxStores ?? plan?.MaxStores ?? 1;
        var maxUsers = command.MaxUsers ?? plan?.MaxUsers ?? 5;
        var maxCameras = command.MaxCameras ?? plan?.MaxCameras ?? 5;
        Tenant tenant;
        try
        {
            tenant = Tenant.Create(
                GenerateTenantCode(),
                command.LegalName,
                command.DisplayName,
                command.PrimaryContactName,
                command.PrimaryEmail,
                command.PrimaryMobile ?? string.Empty,
                command.CountryCode,
                command.TimeZone,
                command.CurrencyCode,
                maxStores,
                maxUsers,
                maxCameras,
                utcNow);
        }
        catch (ArgumentException exception)
        {
            throw new PlatformBusinessRuleException(exception.Message);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await ProvisionDefaultRolesAsync(tenant.Id, utcNow, cancellationToken).ConfigureAwait(false);
            var tenantAdminRole = await dbContext.Roles.SingleAsync(
                role => role.TenantId == tenant.Id && role.NormalizedName == "TENANTADMIN",
                cancellationToken).ConfigureAwait(false);
            var tenantAdmin = UserAccount.CreateTenant(
                tenant.Id,
                command.AdminUserName,
                command.PrimaryEmail,
                command.DisplayName,
                "TEMP",
                utcNow);
            tenantAdmin.SetPasswordHash(passwordHasher.HashPassword(tenantAdmin, command.AdminPassword));
            dbContext.UserAccounts.Add(tenantAdmin);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            dbContext.UserRoles.Add(UserRole.Assign(tenantAdmin, tenantAdminRole, utcNow, audit.ActorUserId));
            if (plan is not null)
            {
                tenant.ConfigureSubscription(
                    plan.Id,
                    SubscriptionStatus.Trial,
                    utcNow,
                    utcNow.AddDays(14),
                    null,
                    null,
                    utcNow);
                dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
                    tenant.Id,
                    plan.Id,
                    BillingCycle.Monthly,
                    SubscriptionStatus.Trial,
                    utcNow,
                    utcNow.AddDays(14),
                    false,
                    utcNow));
            }

            if (hasQuotaOverride)
            {
                dbContext.TenantQuotaOverrides.Add(TenantQuotaOverride.Create(
                    tenant.Id,
                    command.MaxStores,
                    command.MaxUsers,
                    command.MaxCameras,
                    null,
                    null,
                    command.AuditReason!,
                    audit.ActorUserId,
                    utcNow,
                    null));
            }

            RecordAudit(tenant.Id, audit, "TenantCreated", "Tenant", tenant.Id, null, SafeTenant(tenant), utcNow);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        await dbContext.Entry(tenant).Reference(candidate => candidate.SubscriptionPlan)
            .LoadAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(tenant);
    }

    public async Task<PlatformTenantAdministrator> GetTenantAdministratorAsync(
        long tenantId,
        CancellationToken cancellationToken = default)
    {
        await RequireTenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return await FindTenantAdministratorAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlatformTenantAdministrator> ResetTenantAdministratorPasswordAsync(
        long tenantId,
        ResetPlatformTenantAdminPasswordCommand command,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        ValidatePassword(command.NewPassword);
        await RequireTenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var administrator = await dbContext.UserAccounts
            .Where(user => user.TenantId == tenantId && user.IsActive
                && dbContext.UserRoles.Any(assignment => assignment.UserId == user.Id
                    && assignment.Role.NormalizedName == "TENANTADMIN" && assignment.Role.IsActive))
            .OrderBy(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Active tenant administrator");
        var utcNow = UtcNow();
        administrator.SetPasswordHash(passwordHasher.HashPassword(administrator, command.NewPassword));
        var sessions = await dbContext.RefreshTokens
            .Where(token => token.UserId == administrator.Id && token.RevokedUtc == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var session in sessions)
        {
            session.Revoke(utcNow, "PlatformAdminPasswordReset", audit.IpAddress);
        }

        RecordAudit(tenantId, audit, "TenantAdministratorPasswordReset", "User", administrator.Id,
            null, new { administrator.UserName, SessionsRevoked = true }, utcNow);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapAdministrator(administrator);
    }

    public async Task<PlatformTenantDetail> UpdateTenantAsync(
        long tenantId,
        UpdatePlatformTenantCommand command,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        ValidateTimeZone(command.TimeZone);
        var tenant = await GetTrackedTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        RequireVersion(tenant.RowVersion, command.ExpectedVersion);
        var before = SafeTenant(tenant);
        try
        {
            tenant.UpdateProfile(
                command.LegalName,
                command.DisplayName,
                command.PrimaryContactName,
                command.PrimaryEmail,
                command.PrimaryMobile ?? string.Empty,
                command.CountryCode,
                command.TimeZone,
                command.CurrencyCode,
                UtcNow());
        }
        catch (ArgumentException exception)
        {
            throw new PlatformBusinessRuleException(exception.Message);
        }

        RecordAudit(tenant.Id, audit, "TenantUpdated", "Tenant", tenant.Id, before, SafeTenant(tenant), UtcNow());
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(tenant);
    }

    public async Task<PlatformTenantDetail> ActivateTenantAsync(
        long tenantId,
        string expectedVersion,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTrackedTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        RequireVersion(tenant.RowVersion, expectedVersion);
        var before = SafeTenant(tenant);
        tenant.Activate(UtcNow());
        RecordAudit(tenant.Id, audit, "TenantActivated", "Tenant", tenant.Id, before, SafeTenant(tenant), UtcNow());
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(tenant);
    }

    public async Task<PlatformTenantDetail> SuspendTenantAsync(
        long tenantId,
        string reason,
        string expectedVersion,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new PlatformBusinessRuleException("A suspension reason is required.");
        }

        var tenant = await GetTrackedTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        RequireVersion(tenant.RowVersion, expectedVersion);
        var before = SafeTenant(tenant);
        var utcNow = UtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        tenant.Suspend(reason, utcNow);

        // Suspension invalidates every tenant refresh session in the same transaction so
        // no user can regain access between the lifecycle change and token revocation.
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.User.TenantId == tenantId && token.RevokedUtc == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow, "TenantSuspended", audit.IpAddress);
        }

        RecordAudit(tenant.Id, audit, "TenantSuspended", "Tenant", tenant.Id, before, SafeTenant(tenant), utcNow);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(tenant);
    }

    public async Task<PlatformTenantSummary?> GetTenantSummaryAsync(
        long tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants.AsNoTracking()
            .Include(candidate => candidate.SubscriptionPlan)
            .SingleOrDefaultAsync(candidate => candidate.Id == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            return null;
        }

        var usage = await LatestUsageQuery().SingleOrDefaultAsync(
            snapshot => snapshot.TenantId == tenantId,
            cancellationToken).ConfigureAwait(false);
        var users = await dbContext.UserAccounts.CountAsync(
            user => user.TenantId == tenantId,
            cancellationToken).ConfigureAwait(false);
        return new PlatformTenantSummary(
            tenant.Id,
            tenant.TenantCode,
            Status(tenant),
            tenant.SubscriptionStatus.ToString(),
            tenant.SubscriptionPlan?.PlanName,
            usage?.StoreCount ?? 0,
            users,
            usage?.CameraCount ?? 0,
            tenant.MaxStores,
            tenant.MaxUsers,
            tenant.MaxCameras,
            usage?.RecognitionCount ?? 0,
            usage?.ApiCallCount ?? 0,
            usage?.CapturedUtc);
    }

    public async Task<IReadOnlyList<PlatformTenantUsageItem>> GetTenantUsageAsync(
        long tenantId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        _ = await RequireTenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (fromUtc.HasValue && fromUtc.Value.Kind != DateTimeKind.Utc
            || toUtc.HasValue && toUtc.Value.Kind != DateTimeKind.Utc
            || fromUtc.HasValue && toUtc.HasValue && toUtc <= fromUtc)
        {
            throw new PlatformBusinessRuleException("Usage dates must be a valid UTC range.");
        }

        var usage = dbContext.TenantUsageSnapshots.AsNoTracking().Where(snapshot => snapshot.TenantId == tenantId);
        if (fromUtc.HasValue)
        {
            usage = usage.Where(snapshot => snapshot.PeriodEndUtc > fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            usage = usage.Where(snapshot => snapshot.PeriodStartUtc < toUtc.Value);
        }

        return await usage.OrderByDescending(snapshot => snapshot.PeriodStartUtc)
            .Take(36)
            .Select(snapshot => new PlatformTenantUsageItem(
                snapshot.PeriodStartUtc,
                snapshot.PeriodEndUtc,
                snapshot.StoreCount,
                snapshot.UserCount,
                snapshot.CameraCount,
                snapshot.RecognitionCount,
                snapshot.ApiCallCount,
                snapshot.CapturedUtc))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PageResult<PlatformAuditItem>> GetTenantAuditAsync(
        long tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        _ = await RequireTenantExistsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var query = dbContext.AuditLogs.AsNoTracking().Where(audit => audit.TenantId == tenantId);
        var total = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(audit => audit.CreatedUtc)
            .ThenByDescending(audit => audit.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(audit => new PlatformAuditItem(
                audit.Id,
                audit.TenantId,
                audit.UserId,
                audit.ActorType,
                audit.Action,
                audit.EntityType,
                audit.EntityId,
                audit.BeforeJson,
                audit.AfterJson,
                audit.IpAddress,
                audit.CorrelationId,
                audit.CreatedUtc))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return new PageResult<PlatformAuditItem>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<SubscriptionPlanView>> ListPlansAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SubscriptionPlans.AsNoTracking()
            .OrderBy(plan => plan.MonthlyPrice)
            .ThenBy(plan => plan.PlanCode)
            .Select(plan => MapPlan(plan))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

    public async Task<SubscriptionPlanView> CreatePlanAsync(
        SaveSubscriptionPlanCommand command,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var normalizedPlanCode = command.PlanCode.Trim().ToUpperInvariant();
        if (await dbContext.SubscriptionPlans.AnyAsync(
                plan => plan.PlanCode == normalizedPlanCode,
                cancellationToken).ConfigureAwait(false))
        {
            throw new PlatformBusinessRuleException("Plan code already exists.");
        }

        SubscriptionPlan plan;
        try
        {
            plan = SubscriptionPlan.Create(
                command.PlanCode,
                command.PlanName,
                command.MonthlyPrice,
                command.AnnualPrice,
                command.MaxStores,
                command.MaxUsers,
                command.MaxCameras,
                command.MaxMonthlyRecognitions,
                command.MaxMonthlyApiCalls,
                UtcNow());
            if (!command.IsActive)
            {
                plan.Deactivate(UtcNow());
            }
        }
        catch (ArgumentException exception)
        {
            throw new PlatformBusinessRuleException(exception.Message);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        dbContext.SubscriptionPlans.Add(plan);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(null, audit, "SubscriptionPlanCreated", "SubscriptionPlan", plan.Id, null, SafePlan(plan), UtcNow());
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return MapPlan(plan);
    }

    public async Task<SubscriptionPlanView> UpdatePlanAsync(
        long planId,
        SaveSubscriptionPlanCommand command,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.SubscriptionPlans.SingleOrDefaultAsync(
            candidate => candidate.Id == planId,
            cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Subscription plan");
        RequireVersion(plan.RowVersion, command.ExpectedVersion);
        if (!string.Equals(plan.PlanCode, command.PlanCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new PlatformBusinessRuleException("Plan code is immutable.");
        }

        var before = SafePlan(plan);
        try
        {
            plan.Update(
                command.PlanName,
                command.MonthlyPrice,
                command.AnnualPrice,
                command.MaxStores,
                command.MaxUsers,
                command.MaxCameras,
                command.MaxMonthlyRecognitions,
                command.MaxMonthlyApiCalls,
                UtcNow());
            plan.SetActive(command.IsActive, UtcNow());
        }
        catch (ArgumentException exception)
        {
            throw new PlatformBusinessRuleException(exception.Message);
        }

        RecordAudit(null, audit, "SubscriptionPlanUpdated", "SubscriptionPlan", plan.Id, before, SafePlan(plan), UtcNow());
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapPlan(plan);
    }

    public async Task<PlatformTenantDetail> AssignSubscriptionAsync(
        long tenantId,
        AssignTenantSubscriptionCommand command,
        PlatformAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.AuditReason))
        {
            throw new PlatformBusinessRuleException("An audit reason is required for subscription and quota changes.");
        }

        if (!Enum.TryParse<BillingCycle>(command.BillingCycle, true, out var billingCycle)
            || !Enum.IsDefined(billingCycle)
            || !Enum.TryParse<SubscriptionStatus>(command.Status, true, out var subscriptionStatus)
            || !Enum.IsDefined(subscriptionStatus)
            || command.StartsUtc.Kind != DateTimeKind.Utc
            || command.EndsUtc.HasValue && command.EndsUtc.Value.Kind != DateTimeKind.Utc
            || command.EndsUtc <= command.StartsUtc)
        {
            throw new PlatformBusinessRuleException("Subscription cycle, status or UTC period is invalid.");
        }

        var tenant = await GetTrackedTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        RequireVersion(tenant.RowVersion, command.ExpectedVersion);
        var plan = await dbContext.SubscriptionPlans.SingleOrDefaultAsync(
            candidate => candidate.Id == command.SubscriptionPlanId && candidate.IsActive,
            cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformBusinessRuleException("The selected subscription plan is inactive or unavailable.");
        var usage = await LatestUsageQuery().SingleOrDefaultAsync(
            snapshot => snapshot.TenantId == tenantId,
            cancellationToken).ConfigureAwait(false);
        var userCount = await dbContext.UserAccounts.CountAsync(
            user => user.TenantId == tenantId,
            cancellationToken).ConfigureAwait(false);
        var maxStores = command.MaxStores ?? plan.MaxStores;
        var maxUsers = command.MaxUsers ?? plan.MaxUsers;
        var maxCameras = command.MaxCameras ?? plan.MaxCameras;
        if (maxStores < (usage?.StoreCount ?? 0)
            || maxUsers < userCount
            || maxCameras < (usage?.CameraCount ?? 0))
        {
            throw new PlatformBusinessRuleException("A quota cannot be lower than the tenant's current authoritative usage.");
        }

        var before = SafeTenant(tenant);
        var utcNow = UtcNow();
        var hasOverride = command.MaxStores.HasValue || command.MaxUsers.HasValue || command.MaxCameras.HasValue
            || command.MaxMonthlyRecognitions.HasValue || command.MaxMonthlyApiCalls.HasValue;
        if (hasOverride && command.EndsUtc.HasValue && command.EndsUtc <= utcNow)
        {
            throw new PlatformBusinessRuleException("A quota override expiry must be later than the current time.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // Only one current subscription may exist. The previous current row is closed
        // inside this transaction before its replacement is inserted, preserving history.
        var currentSubscription = await dbContext.TenantSubscriptions
            .SingleOrDefaultAsync(
                subscription => subscription.TenantId == tenant.Id
                    && (subscription.Status == SubscriptionStatus.Trial
                        || subscription.Status == SubscriptionStatus.Active
                        || subscription.Status == SubscriptionStatus.PastDue
                        || subscription.Status == SubscriptionStatus.Suspended),
                cancellationToken).ConfigureAwait(false);
        if (currentSubscription is not null)
        {
            if (command.StartsUtc <= currentSubscription.StartsUtc)
            {
                throw new PlatformBusinessRuleException(
                    "A replacement subscription must start after the current subscription started.");
            }

            currentSubscription.UpdateStatus(
                SubscriptionStatus.Cancelled,
                command.StartsUtc,
                false,
                utcNow);
        }

        tenant.SetQuotas(maxStores, maxUsers, maxCameras, utcNow);
        tenant.ConfigureSubscription(
            plan.Id,
            subscriptionStatus,
            subscriptionStatus == SubscriptionStatus.Trial ? command.StartsUtc : null,
            subscriptionStatus == SubscriptionStatus.Trial ? command.EndsUtc : null,
            subscriptionStatus == SubscriptionStatus.Trial ? null : command.StartsUtc,
            subscriptionStatus == SubscriptionStatus.Trial ? null : command.EndsUtc,
            utcNow);
        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            tenant.Id,
            plan.Id,
            billingCycle,
            subscriptionStatus,
            command.StartsUtc,
            command.EndsUtc,
            command.AutoRenew,
            utcNow));

        if (hasOverride)
        {
            dbContext.TenantQuotaOverrides.Add(TenantQuotaOverride.Create(
                tenant.Id,
                command.MaxStores,
                command.MaxUsers,
                command.MaxCameras,
                command.MaxMonthlyRecognitions,
                command.MaxMonthlyApiCalls,
                command.AuditReason,
                audit.ActorUserId,
                utcNow,
                command.EndsUtc));
        }

        RecordAudit(
            tenant.Id,
            audit,
            "TenantSubscriptionAssigned",
            "Tenant",
            tenant.Id,
            before,
            new { Tenant = SafeTenant(tenant), Reason = command.AuditReason },
            utcNow);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Entry(tenant).Reference(candidate => candidate.SubscriptionPlan)
            .LoadAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(tenant);
    }

    private async Task ProvisionDefaultRolesAsync(long tenantId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsSqlServer())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId = {tenantId}",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var permissions = await dbContext.Permissions
            .Where(permission => permission.Scope == UserScope.Tenant && permission.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (permissions.Count == 0)
        {
            throw new PlatformBusinessRuleException("The tenant permission catalog is not provisioned.");
        }

        var roles = DefaultTenantRoleNames
            .Select(name => Role.CreateTenant(tenantId, name, $"Default {name} role.", true, utcNow))
            .ToArray();
        dbContext.Roles.AddRange(roles);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var role in roles)
        {
            foreach (var permission in permissions.Where(permission => DefaultRoleAllows(role.Name, permission.Name)))
            {
                dbContext.RolePermissions.Add(RolePermission.Grant(role, permission));
            }
        }
    }

    private static bool DefaultRoleAllows(string role, string permission) => role switch
    {
        "TenantAdmin" => true,
        "StoreAdmin" => permission is not (
            "TenantUsers.Create" or "TenantUsers.Edit" or "TenantUsers.Deactivate" or "TenantUsers.AssignRoles"
            or "TenantStores.Create" or "TenantStores.Edit" or "Roles.Manage" or "Settings.Manage"),
        "Manager" => permission is "TenantDashboard.View" or "TenantReports.View" or "TenantReports.Export"
            || StartsWithAny(permission, "Customers.", "Households.", "Visits.", "Invoices.", "Alerts.", "Reports.", "Preferences."),
        "CRMStaff" => StartsWithAny(permission, "Customers.", "Households.", "Visitors.", "Preferences.", "Consents.")
            || permission is "Visits.View" or "CustomerJourneys.View",
        "BillingStaff" => permission == "Customers.View" || StartsWithAny(permission, "Invoices.", "Payments."),
        // Camera operators land on the tenant dashboard after login, then operate only their
        // authoritative assigned-store camera/recognition scope. TenantDashboard.View makes
        // that entry route usable without granting tenant administration capabilities.
        "CameraOperator" => StartsWithAny(permission, "Cameras.", "Recognition.")
            || permission is "TenantDashboard.View" or "Visitors.View" or "Visits.View" or "Alerts.View" or "Alerts.Acknowledge",
        "IntegrationAdmin" => StartsWithAny(permission, "Integrations.", "Webhooks.") || permission == "Settings.View",
        "Auditor" => permission.EndsWith(".View", StringComparison.Ordinal)
            || permission is "TenantReports.Export" or "Reports.Export" or "VoiceCommands.Audit" or "AuditLogs.View",
        _ => false,
    };

    private static bool StartsWithAny(string value, params string[] prefixes) =>
        prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));

    private IQueryable<TenantUsageSnapshot> LatestUsageQuery() => dbContext.TenantUsageSnapshots.AsNoTracking()
        .Where(snapshot => !dbContext.TenantUsageSnapshots.Any(other =>
            other.TenantId == snapshot.TenantId
            && (other.PeriodEndUtc > snapshot.PeriodEndUtc
                || other.PeriodEndUtc == snapshot.PeriodEndUtc && other.Id > snapshot.Id)));

    private async Task<Tenant> GetTrackedTenantAsync(long tenantId, CancellationToken cancellationToken) =>
        await dbContext.Tenants.Include(tenant => tenant.SubscriptionPlan)
            .SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Tenant");

    private async Task<bool> RequireTenantExistsAsync(long tenantId, CancellationToken cancellationToken)
    {
        ValidateId(tenantId, nameof(tenantId));
        return await dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ? true
            : throw new PlatformResourceNotFoundException("Tenant");
    }

    private async Task<PlatformTenantAdministrator> FindTenantAdministratorAsync(long tenantId, CancellationToken cancellationToken)
    {
        var administrator = await dbContext.UserAccounts.AsNoTracking()
            .Where(user => user.TenantId == tenantId
                && dbContext.UserRoles.Any(assignment => assignment.UserId == user.Id
                    && assignment.Role.NormalizedName == "TENANTADMIN" && assignment.Role.IsActive))
            .OrderByDescending(user => user.IsActive)
            .ThenBy(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Tenant administrator");
        return MapAdministrator(administrator);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PlatformConcurrencyException();
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new PlatformBusinessRuleException("The change conflicts with existing platform data.");
        }
    }

    private void RecordAudit(
        long? tenantId,
        PlatformAuditContext context,
        string action,
        string entityType,
        long? entityId,
        object? before,
        object? after,
        DateTime utcNow) =>
        dbContext.AuditLogs.Add(AuditLog.Record(
            tenantId,
            null,
            context.ActorUserId,
            "PlatformAdmin",
            action,
            entityType,
            entityId?.ToString(CultureInfo.InvariantCulture),
            before is null ? null : JsonSerializer.Serialize(before),
            after is null ? null : JsonSerializer.Serialize(after),
            context.IpAddress,
            Truncate(context.UserAgent, 500),
            context.CorrelationId,
            utcNow));

    private static object SafeTenant(Tenant tenant) => new
    {
        tenant.TenantCode,
        tenant.LegalName,
        tenant.DisplayName,
        tenant.PrimaryContactName,
        tenant.PrimaryEmail,
        tenant.PrimaryMobile,
        tenant.CountryCode,
        tenant.TimeZone,
        tenant.CurrencyCode,
        tenant.SubscriptionPlanId,
        SubscriptionStatus = tenant.SubscriptionStatus.ToString(),
        tenant.MaxStores,
        tenant.MaxUsers,
        tenant.MaxCameras,
        tenant.IsActive,
        tenant.IsSuspended,
        tenant.SuspensionReason,
    };

    private static object SafePlan(SubscriptionPlan plan) => new
    {
        plan.PlanCode,
        plan.PlanName,
        plan.MonthlyPrice,
        plan.AnnualPrice,
        plan.MaxStores,
        plan.MaxUsers,
        plan.MaxCameras,
        plan.MaxMonthlyRecognitions,
        plan.MaxMonthlyApiCalls,
        plan.IsActive,
    };

    private static PlatformTenantListItem MapListItem(Tenant tenant, TenantUsageSnapshot? usage, int userCount) => new(
        tenant.Id,
        tenant.TenantCode,
        tenant.LegalName,
        tenant.DisplayName,
        tenant.PrimaryContactName,
        tenant.PrimaryEmail,
        NullIfEmpty(tenant.PrimaryMobile),
        tenant.SubscriptionPlan is null
            ? null
            : new TenantPlanSummary(
                tenant.SubscriptionPlan.Id,
                tenant.SubscriptionPlan.PlanCode,
                tenant.SubscriptionPlan.PlanName),
        usage?.StoreCount ?? 0,
        userCount,
        usage?.CameraCount ?? 0,
        0,
        Status(tenant),
        tenant.SubscriptionStatus.ToString(),
        usage?.CapturedUtc ?? tenant.UpdatedUtc,
        EncodeVersion(tenant.RowVersion));

    private static PlatformTenantDetail MapDetail(Tenant tenant) => new(
        tenant.Id,
        tenant.TenantCode,
        tenant.LegalName,
        tenant.DisplayName,
        tenant.TimeZone,
        tenant.PrimaryContactName,
        tenant.PrimaryEmail,
        NullIfEmpty(tenant.PrimaryMobile),
        tenant.CountryCode,
        tenant.CurrencyCode,
        Status(tenant),
        tenant.SubscriptionStatus.ToString(),
        tenant.SubscriptionPlan is null
            ? null
            : new TenantPlanSummary(
                tenant.SubscriptionPlan.Id,
                tenant.SubscriptionPlan.PlanCode,
                tenant.SubscriptionPlan.PlanName),
        tenant.TrialStartsUtc,
        tenant.TrialEndsUtc,
        tenant.SubscriptionStartsUtc,
        tenant.SubscriptionEndsUtc,
        tenant.MaxStores,
        tenant.MaxUsers,
        tenant.MaxCameras,
        tenant.SuspensionReason,
        tenant.CreatedUtc,
        tenant.UpdatedUtc,
        EncodeVersion(tenant.RowVersion));

    private static PlatformTenantAdministrator MapAdministrator(UserAccount user) =>
        new(user.Id, user.UserName, user.Email, user.DisplayName);

    private static SubscriptionPlanView MapPlan(SubscriptionPlan plan) => new(
        plan.Id,
        plan.PlanCode,
        plan.PlanName,
        plan.MonthlyPrice,
        plan.AnnualPrice,
        plan.MaxStores,
        plan.MaxUsers,
        plan.MaxCameras,
        plan.MaxMonthlyRecognitions,
        plan.MaxMonthlyApiCalls,
        plan.IsActive,
        EncodeVersion(plan.RowVersion));

    private static IQueryable<Tenant> ApplyStatusFilter(IQueryable<Tenant> query, string? status) =>
        status?.Trim().ToUpperInvariant() switch
        {
            null or "" or "ALL" => query,
            "ACTIVE" => query.Where(tenant => tenant.IsActive && !tenant.IsSuspended),
            "SUSPENDED" => query.Where(tenant => tenant.IsSuspended),
            "INACTIVE" => query.Where(tenant => !tenant.IsActive),
            "TRIAL" => query.Where(tenant => tenant.SubscriptionStatus == SubscriptionStatus.Trial),
            _ => throw new PlatformBusinessRuleException("Tenant status filter is invalid."),
        };

    private static string Status(Tenant tenant) => tenant.IsSuspended
        ? "Suspended"
        : !tenant.IsActive
            ? "Inactive"
            : "Active";

    private static void RequireVersion(byte[] current, string? expected)
    {
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(expected ?? string.Empty);
        }
        catch (FormatException)
        {
            throw new PlatformConcurrencyException();
        }

        if (current.Length == 0 || !current.AsSpan().SequenceEqual(decoded))
        {
            throw new PlatformConcurrencyException();
        }
    }

    private static string EncodeVersion(byte[] version) => Convert.ToBase64String(version);

    private static string GenerateTenantCode() => $"TEN-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

    private static void ValidatePage(int page, int pageSize)
    {
        if (page <= 0 || pageSize is <= 0 or > 100)
        {
            throw new PlatformBusinessRuleException("Page must be positive and pageSize must be between 1 and 100.");
        }
    }

    private static void ValidateId(long value, string name)
    {
        if (value <= 0)
        {
            throw new PlatformBusinessRuleException($"{name} must be positive.");
        }
    }

    private static void ValidateAudit(PlatformAuditContext audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        if (audit.ActorUserId <= 0 || string.IsNullOrWhiteSpace(audit.CorrelationId))
        {
            throw new PlatformBusinessRuleException("Platform audit context is incomplete.");
        }
    }

    private static void ValidateTimeZone(string timeZone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new PlatformBusinessRuleException("Time zone is not recognized.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new PlatformBusinessRuleException("Time zone is invalid.");
        }
    }

    private static void ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 10 || value.Length > 500
            || !value.Any(char.IsUpper) || !value.Any(char.IsLower) || !value.Any(char.IsDigit))
        {
            throw new PlatformBusinessRuleException(
                "Password must be 10 to 500 characters and contain upper, lower and numeric characters.");
        }
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? Truncate(string? value, int length) => string.IsNullOrWhiteSpace(value)
        ? null
        : value.Trim()[..Math.Min(value.Trim().Length, length)];
}
