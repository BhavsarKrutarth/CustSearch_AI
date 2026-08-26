using CustSearch.Application.Authentication;
using CustSearch.Application.TenantOperations;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.TenantOperations;

/// <summary>
/// Phase 5 defense-in-depth wrapper. It preserves the existing operational implementation while
/// enforcing store visibility, tenant-wide role boundaries and effective subscription quotas.
/// </summary>
public sealed class TenantOperationsSecurityDecorator(
    TenantOperationsService inner,
    CustSearchDbContext db,
    ICurrentUserContext currentUser) : ITenantOperationsService
{
    private static readonly HashSet<string> TenantWideRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "TenantAdmin", "TenantOwner", "ShopOwner",
    };

    public Task<TenantDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default) => inner.GetDashboardAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantUserListItem>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await inner.ListUsersAsync(cancellationToken).ConfigureAwait(false);
        return IsTenantWide() ? users : users.Where(x => HasStoreOverlap(x.StoreIds)).ToArray();
    }

    public async Task<TenantUserDetail> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await inner.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        EnsureTargetStoreAccess(user.StoreIds);
        return user;
    }

    public async Task<TenantUserDetail> CreateUserAsync(CreateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        EnsureRoleAssignmentAllowed(command.Roles);
        EnsureRequestedStoresAllowed(command.StoreIds);
        return await inner.CreateUserAsync(command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> UpdateUserAsync(long userId, UpdateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        var existing = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        await EnsureUserReactivationWithinQuotaAsync(existing.IsActive, command.IsActive, cancellationToken).ConfigureAwait(false);
        return await inner.UpdateUserAsync(userId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> SetUserRolesAsync(long userId, SetTenantUserRolesCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        _ = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        EnsureRoleAssignmentAllowed(command.Roles);
        return await inner.SetUserRolesAsync(userId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> SetUserStoresAsync(long userId, SetTenantUserStoresCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        _ = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        EnsureRequestedStoresAllowed(command.StoreIds);
        return await inner.SetUserStoresAsync(userId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> ResetUserPasswordAsync(
        long userId,
        ResetTenantUserPasswordCommand command,
        TenantAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        _ = await GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return await inner.ResetUserPasswordAsync(userId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<StoreView>> ListStoresAsync(CancellationToken cancellationToken = default) => inner.ListStoresAsync(cancellationToken);
    public Task<StoreView> GetStoreAsync(long storeId, CancellationToken cancellationToken = default) => inner.GetStoreAsync(storeId, cancellationToken);
    public Task<StoreView> CreateStoreAsync(SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.CreateStoreAsync(command, audit, cancellationToken);
    public Task<StoreView> UpdateStoreAsync(long storeId, SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.UpdateStoreAsync(storeId, command, audit, cancellationToken);
    public Task<StoreView> SetStoreActiveAsync(long storeId, bool active, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.SetStoreActiveAsync(storeId, active, audit, cancellationToken);
    public Task<StoreView> VerifyStoreLocationAsync(long storeId, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.VerifyStoreLocationAsync(storeId, audit, cancellationToken);

    public async Task<IReadOnlyList<StaffView>> ListStaffAsync(CancellationToken cancellationToken = default)
    {
        var staff = await inner.ListStaffAsync(cancellationToken).ConfigureAwait(false);
        return IsTenantWide() ? staff : staff.Where(x => HasStoreOverlap(x.StoreIds)).ToArray();
    }

    public async Task<StaffView> GetStaffAsync(long staffId, CancellationToken cancellationToken = default)
    {
        var staff = await inner.GetStaffAsync(staffId, cancellationToken).ConfigureAwait(false);
        EnsureTargetStoreAccess(staff.StoreIds);
        return staff;
    }

    public async Task<StaffView> CreateStaffAsync(CreateStaffCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        EnsureRoleAssignmentAllowed(command.Roles.Count == 0 ? ["SalesStaff"] : command.Roles);
        EnsureRequestedStoresAllowed(command.StoreIds);
        await EnsureNewStaffWithinQuotaAsync(cancellationToken).ConfigureAwait(false);
        return await inner.CreateStaffAsync(command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<StaffView> UpdateStaffAsync(long staffId, UpdateStaffCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        var existing = await GetStaffAsync(staffId, cancellationToken).ConfigureAwait(false);
        var userIsActive = await db.UserAccounts.AsNoTracking()
            .Where(x => x.TenantId == RequireTenantId() && x.Id == existing.UserId)
            .Select(x => x.IsActive)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
        await EnsureUserReactivationWithinQuotaAsync(userIsActive, command.IsActive, cancellationToken).ConfigureAwait(false);
        await EnsureStaffReactivationWithinQuotaAsync(existing.IsActive, command.IsActive, cancellationToken).ConfigureAwait(false);
        EnsureRequestedStoresAllowed(command.StoreIds);
        return await inner.UpdateStaffAsync(staffId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<StaffShiftView> CreateShiftAsync(long staffId, CreateStaffShiftCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        _ = await GetStaffAsync(staffId, cancellationToken).ConfigureAwait(false);
        EnsureStoreAccess(command.StoreId);
        return await inner.CreateShiftAsync(staffId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public Task<StaffShiftView> StartShiftAsync(long shiftId, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.StartShiftAsync(shiftId, audit, cancellationToken);
    public Task<StaffShiftView> CompleteShiftAsync(long shiftId, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.CompleteShiftAsync(shiftId, audit, cancellationToken);

    public async Task<StaffPresenceView> StartPresenceAsync(long staffId, StartStaffPresenceCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        _ = await GetStaffAsync(staffId, cancellationToken).ConfigureAwait(false);
        EnsureStoreAccess(command.StoreId);
        return await inner.StartPresenceAsync(staffId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public Task<StaffPresenceView> ClosePresenceAsync(long presenceId, TenantAuditContext audit, CancellationToken cancellationToken = default) => inner.ClosePresenceAsync(presenceId, audit, cancellationToken);

    public async Task<IReadOnlyList<ProductCategoryView>> ListCategoriesAsync(long? storeId, CancellationToken cancellationToken = default)
    {
        if (storeId.HasValue) EnsureStoreAccess(storeId.Value);
        var categories = await inner.ListCategoriesAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (IsTenantWide()) return categories;
        return categories.Where(x => x.StoreId is null || currentUser.StoreIds.Contains(x.StoreId.Value)).ToArray();
    }

    public async Task<ProductCategoryView> CreateCategoryAsync(SaveProductCategoryCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        EnsureCategoryScopeAllowed(command.StoreId);
        return await inner.CreateCategoryAsync(command, audit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductCategoryView> UpdateCategoryAsync(long categoryId, SaveProductCategoryCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var existing = await db.ProductCategories.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == categoryId)
            .Select(x => new { x.Id, x.StoreId })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Category");
        EnsureCategoryScopeAllowed(existing.StoreId);
        EnsureCategoryScopeAllowed(command.StoreId);
        return await inner.UpdateCategoryAsync(categoryId, command, audit, cancellationToken).ConfigureAwait(false);
    }

    public Task<StoreVoiceCommandSettingView> GetVoiceSettingAsync(long storeId, CancellationToken cancellationToken = default)
    {
        EnsureStoreAccess(storeId);
        return inner.GetVoiceSettingAsync(storeId, cancellationToken);
    }

    public Task<StoreVoiceCommandSettingView> SaveVoiceSettingAsync(long storeId, SaveStoreVoiceCommandSettingCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        EnsureStoreAccess(storeId);
        return inner.SaveVoiceSettingAsync(storeId, command, audit, cancellationToken);
    }

    private bool IsTenantWide() => currentUser.Roles.Any(TenantWideRoles.Contains);
    private long RequireTenantId() => currentUser.TenantId is > 0 and var tenantId ? tenantId : throw new TenantBusinessRuleException("A tenant-scoped authenticated identity is required.");
    private bool HasStoreOverlap(IEnumerable<long> targetStoreIds) => targetStoreIds.Any(currentUser.StoreIds.Contains);

    private void EnsureTargetStoreAccess(IEnumerable<long> targetStoreIds)
    {
        if (!IsTenantWide() && !HasStoreOverlap(targetStoreIds))
            throw new TenantResourceNotFoundException("Resource");
    }

    private void EnsureStoreAccess(long storeId)
    {
        if (!IsTenantWide() && !currentUser.StoreIds.Contains(storeId))
            throw new TenantResourceNotFoundException("Store");
    }

    private void EnsureRequestedStoresAllowed(IReadOnlyCollection<long> requestedStoreIds)
    {
        if (IsTenantWide()) return;
        if (requestedStoreIds.Count == 0 || requestedStoreIds.Any(x => !currentUser.StoreIds.Contains(x)))
            throw new TenantBusinessRuleException("Store-scoped administrators may assign only their own stores and must assign at least one store.");
    }

    private void EnsureRoleAssignmentAllowed(IEnumerable<string> roles)
    {
        if (IsTenantWide()) return;
        if (roles.Any(TenantWideRoles.Contains))
            throw new TenantBusinessRuleException("Store-scoped administrators cannot assign TenantAdmin, TenantOwner or ShopOwner roles.");
    }

    private void EnsureCategoryScopeAllowed(long? storeId)
    {
        if (IsTenantWide()) return;
        if (!storeId.HasValue)
            throw new TenantBusinessRuleException("Only tenant-wide administrators can create or edit tenant-wide categories.");
        EnsureStoreAccess(storeId.Value);
    }

    private async Task EnsureNewStaffWithinQuotaAsync(CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var maxStaff = await db.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.MaxStaff)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);
        var activeStaff = await db.StaffProfiles.AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (activeStaff >= maxStaff)
            throw new TenantBusinessRuleException($"Tenant staff quota ({maxStaff}) has been reached.");
    }

    private async Task EnsureUserReactivationWithinQuotaAsync(bool isCurrentlyActive, bool requestedActive, CancellationToken cancellationToken)
    {
        if (isCurrentlyActive || !requestedActive) return;
        var tenantId = RequireTenantId();
        var maxUsers = await db.Tenants.AsNoTracking().Where(x => x.Id == tenantId).Select(x => x.MaxUsers).SingleAsync(cancellationToken).ConfigureAwait(false);
        var activeUsers = await db.UserAccounts.AsNoTracking().CountAsync(x => x.TenantId == tenantId && x.Scope == CustSearch.Domain.Enums.UserScope.Tenant && x.IsActive, cancellationToken).ConfigureAwait(false);
        if (activeUsers >= maxUsers)
            throw new TenantBusinessRuleException($"Tenant user quota ({maxUsers}) has been reached; the account cannot be reactivated.");
    }

    private async Task EnsureStaffReactivationWithinQuotaAsync(bool isCurrentlyActive, bool requestedActive, CancellationToken cancellationToken)
    {
        if (isCurrentlyActive || !requestedActive) return;
        var tenantId = RequireTenantId();
        var maxStaff = await db.Tenants.AsNoTracking().Where(x => x.Id == tenantId).Select(x => x.MaxStaff).SingleAsync(cancellationToken).ConfigureAwait(false);
        var activeStaff = await db.StaffProfiles.AsNoTracking().CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken).ConfigureAwait(false);
        if (activeStaff >= maxStaff)
            throw new TenantBusinessRuleException($"Tenant staff quota ({maxStaff}) has been reached; the staff profile cannot be reactivated.");
    }
}
