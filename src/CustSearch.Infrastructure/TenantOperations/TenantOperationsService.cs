using System.Globalization;
using System.Text.Json;
using CustSearch.Application.Authentication;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.TenantOperations;

/// <summary>
/// Phase 5 application implementation for tenant users, stores, staff, categories and dynamic voice settings.
/// Tenant ownership is derived only from the server-validated <see cref="ICurrentUserContext"/>.
/// </summary>
public sealed class TenantOperationsService(
    CustSearchDbContext db,
    ITenantOperationsRepository repository,
    ICurrentUserContext currentUser,
    IPasswordHasher<UserAccount> passwordHasher,
    TimeProvider timeProvider) : ITenantOperationsService
{
    private static readonly HashSet<string> TenantWideRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "TenantAdmin",
        "TenantOwner",
        "ShopOwner",
    };

    public async Task<TenantDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        return new TenantDashboardSummary(
            await repository.CountActiveUsersAsync(tenantId, cancellationToken).ConfigureAwait(false),
            await repository.CountActiveStoresAsync(tenantId, cancellationToken).ConfigureAwait(false),
            await db.StaffProfiles.CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken).ConfigureAwait(false),
            await db.ProductCategories.CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken).ConfigureAwait(false),
            await db.StaffShifts.CountAsync(x => x.TenantId == tenantId && x.Status == StaffShiftStatus.Active, cancellationToken).ConfigureAwait(false),
            await db.StaffPresenceSessions.CountAsync(x => x.TenantId == tenantId && x.ExitedUtc == null, cancellationToken).ConfigureAwait(false));
    }

    public async Task<IReadOnlyList<TenantUserListItem>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var users = await db.UserAccounts.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Scope == UserScope.Tenant)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<TenantUserListItem>(users.Count);
        foreach (var user in users)
        {
            var authorization = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new(user.Id, user.UserName, user.Email, user.DisplayName, user.IsActive, user.CreatedUtc, authorization.Roles, authorization.StoreIds));
        }
        return result;
    }

    public async Task<TenantUserDetail> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var user = await repository.GetUserAsync(tenantId, userId, false, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("User");
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> CreateUserAsync(CreateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        ValidatePassword(command.Password);
        var tenantId = RequireTenantId();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (await repository.CountActiveUsersAsync(tenantId, cancellationToken).ConfigureAwait(false) >= tenant.MaxUsers)
            throw new TenantBusinessRuleException($"Tenant user quota ({tenant.MaxUsers}) has been reached.");

        var normalizedUserName = command.UserName.Trim().ToUpperInvariant();
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.TenantId == tenantId && (x.NormalizedUserName == normalizedUserName || x.NormalizedEmail == normalizedEmail), cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Username or email already exists in this tenant.");

        var now = UtcNow();
        var user = UserAccount.CreateTenant(tenantId, command.UserName, command.Email, command.DisplayName, "TEMP", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, command.Password));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await ReplaceRolesAsync(tenantId, user, command.Roles, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        var primaryStoreId = command.StoreIds.Count > 0 ? command.StoreIds[0] : (long?)null;
        await ReplaceStoresAsync(tenantId, user, command.StoreIds, primaryStoreId, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserCreated", "User", user.Id, null, new { user.UserName, user.Email, user.DisplayName, command.Roles, command.StoreIds }, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> UpdateUserAsync(long userId, UpdateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("User");
        var before = new { user.Email, user.DisplayName, user.IsActive };
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.TenantId == tenantId && x.Id != userId && x.NormalizedEmail == normalizedEmail, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Email already exists in this tenant.");
        user.UpdateProfile(command.Email, command.DisplayName);
        if (command.IsActive) user.Activate(); else user.Deactivate();
        var now = UtcNow();
        await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserUpdated", "User", user.Id, before, new { user.Email, user.DisplayName, user.IsActive }, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> SetUserRolesAsync(long userId, SetTenantUserRolesCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("User");
        var before = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false);
        var now = UtcNow();
        await ReplaceRolesAsync(tenantId, user, command.Roles, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        user.RotateSecurityStamp();
        await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserRolesChanged", "User", user.Id, before.Roles, command.Roles, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> SetUserStoresAsync(long userId, SetTenantUserStoresCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("User");
        var before = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false);
        var now = UtcNow();
        await ReplaceStoresAsync(tenantId, user, command.StoreIds, command.PrimaryStoreId, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        user.RotateSecurityStamp();
        await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserStoresChanged", "User", user.Id, before.StoreIds, command.StoreIds, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Allows an authorized tenant administrator to set a new password for an in-scope user.
    /// Password material is never included in audit JSON; the security stamp and every refresh
    /// session are revoked so the target must authenticate again on all devices.
    /// </summary>
    public async Task<TenantUserDetail> ResetUserPasswordAsync(
        long userId,
        ResetTenantUserPasswordCommand command,
        TenantAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        ValidatePassword(command.NewPassword);
        if (userId == audit.ActorUserId)
        {
            throw new TenantBusinessRuleException("Use the authenticated Change password page to change your own password.");
        }

        var tenantId = RequireTenantId();
        var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("User");
        var now = UtcNow();
        user.SetPasswordHash(passwordHasher.HashPassword(user, command.NewPassword));
        await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(
            tenantId,
            null,
            audit,
            "TenantUserPasswordReset",
            "User",
            user.Id,
            null,
            new { SessionsRevoked = true },
            now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoreView>> ListStoresAsync(CancellationToken cancellationToken = default)
    {
        var stores = await repository.ListStoresAsync(RequireTenantId(), cancellationToken).ConfigureAwait(false);
        return stores.Where(CanAccessStore).Select(MapStore).ToArray();
    }

    public async Task<StoreView> GetStoreAsync(long storeId, CancellationToken cancellationToken = default) =>
        MapStore(await RequireStoreAsync(storeId, false, cancellationToken).ConfigureAwait(false));

    public async Task<StoreView> CreateStoreAsync(SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        ValidateTimeZone(command.TimeZone);
        var tenantId = RequireTenantId();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (await repository.CountActiveStoresAsync(tenantId, cancellationToken).ConfigureAwait(false) >= tenant.MaxStores)
            throw new TenantBusinessRuleException($"Tenant store quota ({tenant.MaxStores}) has been reached.");
        var code = string.IsNullOrWhiteSpace(command.StoreCode)
            ? $"S-{Guid.NewGuid():N}"[..14].ToUpperInvariant()
            : command.StoreCode.Trim().ToUpperInvariant();
        if (await db.Stores.AnyAsync(x => x.TenantId == tenantId && x.StoreCode == code, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Store code already exists.");
        Store store;
        try
        {
            store = Store.Create(tenantId, code, command.StoreName, command.AddressLine1, command.AddressLine2, command.Landmark,
                command.City, command.District, command.StateOrProvince, command.PostalCode, command.CountryCode, command.Latitude,
                command.Longitude, command.GeoFenceRadiusMeters, command.ExternalPlaceId, command.LocationSource, command.TimeZone,
                command.ContactEmail, command.ContactMobile, UtcNow());
        }
        catch (ArgumentException exception)
        {
            throw new TenantBusinessRuleException(exception.Message);
        }
        db.Stores.Add(store);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, store.Id, audit, "StoreCreated", "Store", store.Id, null, MapStore(store), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapStore(store);
    }

    public async Task<StoreView> UpdateStoreAsync(long storeId, SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        ValidateTimeZone(command.TimeZone);
        var store = await RequireStoreAsync(storeId, true, cancellationToken).ConfigureAwait(false);
        var before = MapStore(store);
        try
        {
            store.Update(command.StoreName, command.AddressLine1, command.AddressLine2, command.Landmark, command.City, command.District,
                command.StateOrProvince, command.PostalCode, command.CountryCode, command.Latitude, command.Longitude,
                command.GeoFenceRadiusMeters, command.ExternalPlaceId, command.LocationSource, command.TimeZone, command.ContactEmail,
                command.ContactMobile, UtcNow());
        }
        catch (ArgumentException exception)
        {
            throw new TenantBusinessRuleException(exception.Message);
        }
        RecordAudit(RequireTenantId(), store.Id, audit, "StoreUpdated", "Store", store.Id, before, MapStore(store), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapStore(store);
    }

    public async Task<StoreView> SetStoreActiveAsync(long storeId, bool active, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var store = await RequireStoreAsync(storeId, true, cancellationToken).ConfigureAwait(false);
        if (active && !store.IsActive)
        {
            var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (await repository.CountActiveStoresAsync(tenantId, cancellationToken).ConfigureAwait(false) >= tenant.MaxStores)
                throw new TenantBusinessRuleException("Store quota has been reached.");
        }
        var before = store.IsActive;
        store.SetActive(active, UtcNow());
        RecordAudit(tenantId, store.Id, audit, "StoreLifecycleChanged", "Store", store.Id, new { IsActive = before }, new { store.IsActive }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapStore(store);
    }

    public async Task<StoreView> VerifyStoreLocationAsync(long storeId, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var store = await RequireStoreAsync(storeId, true, cancellationToken).ConfigureAwait(false);
        if (store.Latitude is null || store.Longitude is null)
            throw new TenantBusinessRuleException("Coordinates are required before location verification.");
        store.VerifyLocation(audit.ActorUserId, UtcNow());
        RecordAudit(tenantId, store.Id, audit, "StoreLocationVerified", "Store", store.Id, null,
            new { store.Latitude, store.Longitude, store.LocationVerifiedUtc }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapStore(store);
    }

    public async Task<IReadOnlyList<StaffView>> ListStaffAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var staff = await repository.ListStaffAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var result = new List<StaffView>(staff.Count);
        foreach (var profile in staff)
        {
            var storeIds = await UserStoreIdsAsync(tenantId, profile.UserId, cancellationToken).ConfigureAwait(false);
            if (IsTenantWide() || storeIds.Length == 0 || storeIds.Any(CanAccessStoreId)) result.Add(MapStaff(profile, storeIds));
        }
        return result;
    }

    public async Task<StaffView> GetStaffAsync(long staffId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var staff = await repository.GetStaffAsync(tenantId, staffId, false, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Staff");
        var storeIds = await UserStoreIdsAsync(tenantId, staff.UserId, cancellationToken).ConfigureAwait(false);
        EnsureAnyStoreAccess(storeIds);
        return MapStaff(staff, storeIds);
    }

    public async Task<StaffView> CreateStaffAsync(CreateStaffCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        ValidatePassword(command.Password);
        var tenantId = RequireTenantId();
        var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (await repository.CountActiveUsersAsync(tenantId, cancellationToken).ConfigureAwait(false) >= tenant.MaxUsers)
            throw new TenantBusinessRuleException("Tenant user quota has been reached.");
        var employeeCode = command.EmployeeCode.Trim().ToUpperInvariant();
        if (await db.StaffProfiles.AnyAsync(x => x.TenantId == tenantId && x.EmployeeCode == employeeCode, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Employee code already exists.");
        var normalizedUserName = command.UserName.Trim().ToUpperInvariant();
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.TenantId == tenantId && (x.NormalizedUserName == normalizedUserName || x.NormalizedEmail == normalizedEmail), cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Username or email already exists in this tenant.");
        var now = UtcNow();
        var displayName = string.Concat(command.FirstName.Trim(), " ", command.LastName.Trim()).Trim();
        var user = UserAccount.CreateTenant(tenantId, command.UserName, command.Email, displayName, "TEMP", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, command.Password));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var profile = StaffProfile.Create(tenantId, user.Id, employeeCode, command.FirstName, command.LastName, command.Mobile, now);
        db.StaffProfiles.Add(profile);
        IReadOnlyList<string> roles = command.Roles.Count == 0 ? ["SalesStaff"] : command.Roles;
        await ReplaceRolesAsync(tenantId, user, roles, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        var primaryStoreId = command.StoreIds.Count > 0 ? command.StoreIds[0] : (long?)null;
        await ReplaceStoresAsync(tenantId, user, command.StoreIds, primaryStoreId, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "StaffCreated", "StaffProfile", profile.Id, null,
            new { profile.EmployeeCode, profile.FirstName, profile.LastName, command.StoreIds }, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return MapStaff(profile, command.StoreIds.Distinct().ToArray());
    }

    public async Task<StaffView> UpdateStaffAsync(long staffId, UpdateStaffCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var staff = await repository.GetStaffAsync(tenantId, staffId, true, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Staff");
        var user = await repository.GetUserAsync(tenantId, staff.UserId, true, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Staff user");
        var before = new { staff.FirstName, staff.LastName, staff.Mobile, staff.IsActive };
        var now = UtcNow();
        staff.Update(command.FirstName, command.LastName, command.Mobile, now);
        staff.SetActive(command.IsActive, now);
        if (command.IsActive) user.Activate(); else user.Deactivate();
        var primaryStoreId = command.StoreIds.Count > 0 ? command.StoreIds[0] : (long?)null;
        await ReplaceStoresAsync(tenantId, user, command.StoreIds, primaryStoreId, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false);
        await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "StaffUpdated", "StaffProfile", staff.Id, before,
            new { staff.FirstName, staff.LastName, staff.Mobile, staff.IsActive, command.StoreIds }, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapStaff(staff, command.StoreIds.Distinct().ToArray());
    }

    public async Task<StaffShiftView> CreateShiftAsync(long staffId, CreateStaffShiftCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var staff = await repository.GetStaffAsync(tenantId, staffId, false, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Staff");
        await EnsureStaffAssignedToStoreAsync(staff.UserId, command.StoreId, cancellationToken).ConfigureAwait(false);
        EnsureStoreAccess(command.StoreId);
        var shift = StaffShift.Create(tenantId, staffId, command.StoreId, ToUtc(command.StartsUtc),
            command.ScheduledEndsUtc.HasValue ? ToUtc(command.ScheduledEndsUtc.Value) : null, audit.ActorUserId, UtcNow());
        db.StaffShifts.Add(shift);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, command.StoreId, audit, "StaffShiftCreated", "StaffShift", shift.Id, null, MapShift(shift), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapShift(shift);
    }

    public Task<StaffShiftView> StartShiftAsync(long shiftId, TenantAuditContext audit, CancellationToken cancellationToken = default) =>
        ChangeShiftStatusAsync(shiftId, true, audit, cancellationToken);

    public Task<StaffShiftView> CompleteShiftAsync(long shiftId, TenantAuditContext audit, CancellationToken cancellationToken = default) =>
        ChangeShiftStatusAsync(shiftId, false, audit, cancellationToken);

    public async Task<StaffPresenceView> StartPresenceAsync(long staffId, StartStaffPresenceCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var staff = await repository.GetStaffAsync(tenantId, staffId, false, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Staff");
        await EnsureStaffAssignedToStoreAsync(staff.UserId, command.StoreId, cancellationToken).ConfigureAwait(false);
        EnsureStoreAccess(command.StoreId);
        if (await db.StaffPresenceSessions.AnyAsync(x => x.TenantId == tenantId && x.StaffProfileId == staffId && x.StoreId == command.StoreId && x.ExitedUtc == null, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("An active presence session already exists.");
        var presence = StaffPresenceSession.Start(tenantId, staffId, command.StoreId, command.Source, UtcNow(), command.Confidence);
        db.StaffPresenceSessions.Add(presence);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, command.StoreId, audit, "StaffPresenceStarted", "StaffPresenceSession", presence.Id, null, MapPresence(presence), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapPresence(presence);
    }

    public async Task<StaffPresenceView> ClosePresenceAsync(long presenceId, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var presence = await db.StaffPresenceSessions.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == presenceId, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Presence session");
        EnsureStoreAccess(presence.StoreId);
        if (presence.ExitedUtc is not null) throw new TenantBusinessRuleException("Presence session is already closed.");
        presence.Close(UtcNow());
        RecordAudit(tenantId, presence.StoreId, audit, "StaffPresenceClosed", "StaffPresenceSession", presence.Id, null, MapPresence(presence), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapPresence(presence);
    }

    public async Task<IReadOnlyList<ProductCategoryView>> ListCategoriesAsync(long? storeId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = db.ProductCategories.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (storeId.HasValue)
        {
            EnsureStoreAccess(storeId.Value);
            query = query.Where(x => x.StoreId == null || x.StoreId == storeId.Value);
        }
        return await query.OrderBy(x => x.Name).Select(x => new ProductCategoryView(x.Id, x.StoreId, x.CategoryCode, x.Name, x.ParentCategoryId, x.IsActive)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductCategoryView> CreateCategoryAsync(SaveProductCategoryCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        if (command.StoreId.HasValue) await RequireStoreAsync(command.StoreId.Value, false, cancellationToken).ConfigureAwait(false);
        await ValidateParentCategoryAsync(command.ParentCategoryId, 0, cancellationToken).ConfigureAwait(false);
        var code = command.CategoryCode.Trim().ToUpperInvariant();
        if (await db.ProductCategories.AnyAsync(x => x.TenantId == tenantId && x.StoreId == command.StoreId && x.CategoryCode == code, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Category code already exists in this scope.");
        var category = ProductCategory.Create(tenantId, command.StoreId, code, command.Name, command.ParentCategoryId, UtcNow());
        if (!command.IsActive) category.Update(command.Name, command.ParentCategoryId, false, UtcNow());
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, command.StoreId, audit, "ProductCategoryCreated", "ProductCategory", category.Id, null, MapCategory(category), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapCategory(category);
    }

    public async Task<ProductCategoryView> UpdateCategoryAsync(long categoryId, SaveProductCategoryCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var category = await db.ProductCategories.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == categoryId, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Category");
        if (category.StoreId.HasValue) EnsureStoreAccess(category.StoreId.Value);
        if (command.StoreId != category.StoreId) throw new TenantBusinessRuleException("Category store scope cannot be changed.");
        await ValidateParentCategoryAsync(command.ParentCategoryId, categoryId, cancellationToken).ConfigureAwait(false);
        var before = MapCategory(category);
        category.Update(command.Name, command.ParentCategoryId, command.IsActive, UtcNow());
        RecordAudit(tenantId, category.StoreId, audit, "ProductCategoryUpdated", "ProductCategory", category.Id, before, MapCategory(category), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapCategory(category);
    }

    public async Task<StoreVoiceCommandSettingView> GetVoiceSettingAsync(long storeId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        await RequireStoreAsync(storeId, false, cancellationToken).ConfigureAwait(false);
        var setting = await db.StoreVoiceCommandSettings.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.StoreId == storeId, cancellationToken).ConfigureAwait(false);
        if (setting is null) return new(storeId, "Aasha Add", VoiceResponseMode.InAppAndVoice, true, true, [], DateTime.UnixEpoch);
        var aliases = await db.StoreVoiceCommandAliases.AsNoTracking().Where(x => x.TenantId == tenantId && x.StoreId == storeId).OrderBy(x => x.Alias).Select(x => x.Alias).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return MapVoice(setting, aliases);
    }

    public async Task<StoreVoiceCommandSettingView> SaveVoiceSettingAsync(long storeId, SaveStoreVoiceCommandSettingCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        await RequireStoreAsync(storeId, false, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(command.TriggerKeyword)) throw new TenantBusinessRuleException("Trigger keyword is required.");
        var setting = await db.StoreVoiceCommandSettings.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.StoreId == storeId, cancellationToken).ConfigureAwait(false);
        var before = setting is null ? null : await GetVoiceSettingAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (setting is null)
        {
            setting = StoreVoiceCommandSetting.Create(tenantId, storeId, command.TriggerKeyword, command.ResponseMode, UtcNow());
            db.StoreVoiceCommandSettings.Add(setting);
        }
        setting.Update(command.TriggerKeyword, command.ResponseMode, command.IsEnabled, command.RequireConfirmationForAmbiguousCategory, UtcNow());
        var existingAliases = await db.StoreVoiceCommandAliases.Where(x => x.TenantId == tenantId && x.StoreId == storeId).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.StoreVoiceCommandAliases.RemoveRange(existingAliases);
        foreach (var alias in command.Aliases.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!string.Equals(alias, command.TriggerKeyword, StringComparison.OrdinalIgnoreCase))
                db.StoreVoiceCommandAliases.Add(StoreVoiceCommandAlias.Create(tenantId, storeId, alias, UtcNow()));
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var after = await GetVoiceSettingAsync(storeId, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, storeId, audit, "StoreVoiceCommandSettingChanged", "StoreVoiceCommandSetting", storeId, before, after, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return after;
    }

    private async Task<StaffShiftView> ChangeShiftStatusAsync(long shiftId, bool start, TenantAuditContext audit, CancellationToken cancellationToken)
    {
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var shift = await db.StaffShifts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == shiftId, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Shift");
        EnsureStoreAccess(shift.StoreId);
        try { if (start) shift.Start(UtcNow()); else shift.Complete(UtcNow()); }
        catch (InvalidOperationException exception) { throw new TenantBusinessRuleException(exception.Message); }
        RecordAudit(tenantId, shift.StoreId, audit, start ? "StaffShiftStarted" : "StaffShiftCompleted", "StaffShift", shift.Id, null, MapShift(shift), UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapShift(shift);
    }

    private long RequireTenantId() => currentUser.TenantId is > 0 and var tenantId ? tenantId : throw new TenantBusinessRuleException("A tenant-scoped authenticated identity is required.");
    private bool IsTenantWide() => currentUser.Roles.Any(TenantWideRoles.Contains);
    private bool CanAccessStore(Store store) => CanAccessStoreId(store.Id);
    private bool CanAccessStoreId(long storeId) => IsTenantWide() || currentUser.StoreIds.Contains(storeId);
    private void EnsureStoreAccess(long storeId) { if (!CanAccessStoreId(storeId)) throw new TenantBusinessRuleException("The authenticated user is not assigned to this store."); }
    private void EnsureAnyStoreAccess(long[] storeIds) { if (!IsTenantWide() && storeIds.Length > 0 && !storeIds.Any(currentUser.StoreIds.Contains)) throw new TenantBusinessRuleException("The authenticated user cannot access this staff member."); }
    private async Task<Tenant> RequireTenantAsync(long tenantId, CancellationToken cancellationToken) => await repository.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false) ?? throw new TenantResourceNotFoundException("Tenant");

    private async Task<Store> RequireStoreAsync(long storeId, bool tracked, CancellationToken cancellationToken)
    {
        var store = await repository.GetStoreAsync(RequireTenantId(), storeId, tracked, cancellationToken).ConfigureAwait(false) ?? throw new TenantResourceNotFoundException("Store");
        EnsureStoreAccess(storeId);
        return store;
    }

    private async Task EnsureStaffAssignedToStoreAsync(long userId, long storeId, CancellationToken cancellationToken)
    {
        if (!await db.UserStoreAssignments.AnyAsync(x => x.TenantId == RequireTenantId() && x.UserId == userId && x.StoreId == storeId, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Staff member is not assigned to the selected store.");
    }

    private async Task ReplaceRolesAsync(long tenantId, UserAccount user, IReadOnlyList<string> names, long actorUserId, DateTime now, CancellationToken cancellationToken)
    {
        var normalizedNames = names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if (normalizedNames.Length == 0) throw new TenantBusinessRuleException("At least one tenant role is required.");
        var roles = await db.Roles.Where(x => x.TenantId == tenantId && x.Scope == UserScope.Tenant && x.IsActive && normalizedNames.Contains(x.NormalizedName)).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (roles.Count != normalizedNames.Length) throw new TenantBusinessRuleException("One or more roles are invalid for this tenant.");
        var existing = await db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.UserRoles.RemoveRange(existing);
        foreach (var role in roles) db.UserRoles.Add(UserRole.Assign(user, role, now, actorUserId));
    }

    private async Task ReplaceStoresAsync(long tenantId, UserAccount user, IReadOnlyList<long> storeIds, long? primaryStoreId, long actorUserId, DateTime now, CancellationToken cancellationToken)
    {
        var distinctStoreIds = storeIds.Where(x => x > 0).Distinct().ToArray();
        if (primaryStoreId is > 0 && !distinctStoreIds.Contains(primaryStoreId.Value)) throw new TenantBusinessRuleException("Primary store must be included in StoreIds.");
        if (distinctStoreIds.Length > 0)
        {
            var validCount = await db.Stores.CountAsync(x => x.TenantId == tenantId && x.IsActive && distinctStoreIds.Contains(x.Id), cancellationToken).ConfigureAwait(false);
            if (validCount != distinctStoreIds.Length) throw new TenantBusinessRuleException("One or more stores are invalid, inactive or belong to another tenant.");
        }
        var existing = await db.UserStoreAssignments.Where(x => x.TenantId == tenantId && x.UserId == user.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.UserStoreAssignments.RemoveRange(existing);
        foreach (var storeId in distinctStoreIds)
            db.UserStoreAssignments.Add(UserStoreAssignment.Assign(tenantId, user.Id, storeId, primaryStoreId == storeId, now, actorUserId));
    }

    private async Task RevokeSessionsAsync(long userId, DateTime now, CancellationToken cancellationToken)
    {
        var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedUtc == null).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var token in tokens) token.Revoke(now, "AuthorizationChanged", null);
    }

    private async Task<(IReadOnlyList<string> Roles, IReadOnlyList<long> StoreIds)> GetUserAuthorizationAsync(long tenantId, long userId, CancellationToken cancellationToken)
    {
        var roles = await db.UserRoles.AsNoTracking().Where(x => x.UserId == userId && x.Role.TenantId == tenantId && x.Role.IsActive).Select(x => x.Role.Name).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var storeIds = await UserStoreIdsAsync(tenantId, userId, cancellationToken).ConfigureAwait(false);
        return (roles, storeIds);
    }

    private Task<long[]> UserStoreIdsAsync(long tenantId, long userId, CancellationToken cancellationToken) => db.UserStoreAssignments.AsNoTracking().Where(x => x.TenantId == tenantId && x.UserId == userId && x.Store.IsActive).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.StoreId).Select(x => x.StoreId).ToArrayAsync(cancellationToken);

    private async Task<TenantUserDetail> MapUserAsync(long tenantId, UserAccount user, CancellationToken cancellationToken)
    {
        var authorization = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false);
        return new(user.Id, user.UserName, user.Email, user.DisplayName, user.IsActive, user.CreatedUtc, user.LastLoginUtc, authorization.Roles, authorization.StoreIds);
    }

    private async Task ValidateParentCategoryAsync(long? parentCategoryId, long currentCategoryId, CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue) return;
        if (parentCategoryId.Value == currentCategoryId) throw new TenantBusinessRuleException("Category cannot be its own parent.");
        if (!await db.ProductCategories.AnyAsync(x => x.TenantId == RequireTenantId() && x.Id == parentCategoryId.Value && x.IsActive, cancellationToken).ConfigureAwait(false)) throw new TenantBusinessRuleException("Parent category is invalid.");
    }

    private static StoreView MapStore(Store x) => new(x.Id, x.StoreCode, x.StoreName, x.AddressLine1, x.AddressLine2, x.Landmark, x.City, x.District, x.StateOrProvince, x.PostalCode, x.CountryCode, x.Latitude, x.Longitude, x.GeoFenceRadiusMeters, x.ExternalPlaceId, x.LocationSource, x.IsLocationVerified, x.LocationVerifiedUtc, x.TimeZone, x.ContactEmail, x.ContactMobile, x.IsActive, x.CreatedUtc, x.UpdatedUtc);
    private static StaffView MapStaff(StaffProfile x, IReadOnlyList<long> stores) => new(x.Id, x.UserId, x.EmployeeCode, x.FirstName, x.LastName, x.Mobile, x.IsActive, stores);
    private static StaffShiftView MapShift(StaffShift x) => new(x.Id, x.StaffProfileId, x.StoreId, x.StartsUtc, x.ScheduledEndsUtc, x.ActualEndsUtc, x.Status);
    private static StaffPresenceView MapPresence(StaffPresenceSession x) => new(x.Id, x.StaffProfileId, x.StoreId, x.Source, x.EnteredUtc, x.ExitedUtc, x.Confidence);
    private static ProductCategoryView MapCategory(ProductCategory x) => new(x.Id, x.StoreId, x.CategoryCode, x.Name, x.ParentCategoryId, x.IsActive);
    private static StoreVoiceCommandSettingView MapVoice(StoreVoiceCommandSetting x, IReadOnlyList<string> aliases) => new(x.StoreId, x.TriggerKeyword, x.ResponseMode, x.IsEnabled, x.RequireConfirmationForAmbiguousCategory, aliases, x.UpdatedUtc);

    private static void ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 10 || !value.Any(char.IsUpper) || !value.Any(char.IsLower) || !value.Any(char.IsDigit))
            throw new TenantBusinessRuleException("Password must be at least 10 characters and contain upper, lower and numeric characters.");
    }

    private static void ValidateTimeZone(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(value); }
        catch (TimeZoneNotFoundException) { throw new TenantBusinessRuleException("Time zone is invalid on this server."); }
        catch (InvalidTimeZoneException) { throw new TenantBusinessRuleException("Time zone is invalid on this server."); }
    }

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
    private static void ValidateAudit(TenantAuditContext audit) { if (audit.ActorUserId <= 0 || string.IsNullOrWhiteSpace(audit.CorrelationId)) throw new ArgumentException("Valid audit context is required.", nameof(audit)); }

    private void RecordAudit(long tenantId, long? storeId, TenantAuditContext audit, string action, string entityType, long entityId, object? before, object? after, DateTime now)
    {
        db.AuditLogs.Add(AuditLog.Record(tenantId, storeId, audit.ActorUserId, "User", action, entityType,
            entityId.ToString(CultureInfo.InvariantCulture), before is null ? null : JsonSerializer.Serialize(before),
            after is null ? null : JsonSerializer.Serialize(after), audit.IpAddress, audit.UserAgent, audit.CorrelationId, now));
    }
}
