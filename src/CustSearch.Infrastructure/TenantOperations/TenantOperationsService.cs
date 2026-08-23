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
/// Phase 5 application implementation. Tenant ownership is derived only from ICurrentUserContext;
/// every database predicate includes TenantId and every administrative write is audited.
/// </summary>
public sealed class TenantOperationsService(
    CustSearchDbContext db,
    ITenantOperationsRepository repository,
    ICurrentUserContext currentUser,
    IPasswordHasher<UserAccount> passwordHasher,
    TimeProvider timeProvider) : ITenantOperationsService
{
    private static readonly HashSet<string> TenantWideRoles = new(StringComparer.OrdinalIgnoreCase)
    { "TenantAdmin", "TenantOwner", "ShopOwner" };

    public async Task<TenantDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = TenantId();
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
        var tenantId = TenantId();
        var users = await db.UserAccounts.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Scope == UserScope.Tenant)
            .OrderBy(x => x.DisplayName).ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<TenantUserListItem>(users.Count);
        foreach (var user in users)
        {
            var auth = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new(user.Id, user.UserName, user.Email, user.DisplayName, user.IsActive, user.CreatedUtc, auth.Roles, auth.StoreIds));
        }
        return result;
    }

    public async Task<TenantUserDetail> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var tenantId = TenantId();
        var user = await repository.GetUserAsync(tenantId, userId, false, cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("User");
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> CreateUserAsync(CreateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command); ValidateAudit(audit); ValidatePassword(command.Password);
        var tenantId = TenantId(); var tenant = await RequireTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var count = await db.UserAccounts.CountAsync(x => x.TenantId == tenantId && x.Scope == UserScope.Tenant && x.IsActive, cancellationToken).ConfigureAwait(false);
        if (count >= tenant.MaxUsers) throw new TenantBusinessRuleException($"Tenant user quota ({tenant.MaxUsers}) has been reached.");
        var normalizedUser = command.UserName.Trim().ToUpperInvariant(); var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.TenantId == tenantId && (x.NormalizedUserName == normalizedUser || x.NormalizedEmail == normalizedEmail), cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Username or email already exists in this tenant.");

        var utcNow = UtcNow();
        var user = UserAccount.CreateTenant(tenantId, command.UserName, command.Email, command.DisplayName, "TEMP", utcNow);
        user.SetPasswordHash(passwordHasher.HashPassword(user, command.Password));
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        db.UserAccounts.Add(user); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await ReplaceRolesAsync(tenantId, user, command.Roles, audit.ActorUserId, utcNow, cancellationToken).ConfigureAwait(false);
        await ReplaceStoresAsync(tenantId, user, command.StoreIds, command.StoreIds.FirstOrDefault(), audit.ActorUserId, utcNow, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserCreated", "User", user.Id, null, new { user.UserName, user.Email, user.DisplayName, command.Roles, command.StoreIds }, utcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> UpdateUserAsync(long userId, UpdateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit); var tenantId = TenantId(); var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false) ?? throw new TenantResourceNotFoundException("User");
        var before = new { user.Email, user.DisplayName, user.IsActive };
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.TenantId == tenantId && x.Id != userId && x.NormalizedEmail == normalizedEmail, cancellationToken).ConfigureAwait(false)) throw new TenantBusinessRuleException("Email already exists in this tenant.");
        user.UpdateProfile(command.Email, command.DisplayName); if (command.IsActive) user.Activate(); else user.Deactivate();
        await RevokeSessionsAsync(user.Id, UtcNow(), cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserUpdated", "User", user.Id, before, new { user.Email, user.DisplayName, user.IsActive }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> SetUserRolesAsync(long userId, SetTenantUserRolesCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit); var tenantId = TenantId(); var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false) ?? throw new TenantResourceNotFoundException("User");
        var before = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false); var now = UtcNow();
        await ReplaceRolesAsync(tenantId, user, command.Roles, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false); user.RotateSecurityStamp(); await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserRolesChanged", "User", user.Id, before.Roles, command.Roles, now); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantUserDetail> SetUserStoresAsync(long userId, SetTenantUserStoresCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit); var tenantId = TenantId(); var user = await repository.GetUserAsync(tenantId, userId, true, cancellationToken).ConfigureAwait(false) ?? throw new TenantResourceNotFoundException("User");
        var before = await GetUserAuthorizationAsync(tenantId, user.Id, cancellationToken).ConfigureAwait(false); var now = UtcNow();
        await ReplaceStoresAsync(tenantId, user, command.StoreIds, command.PrimaryStoreId, audit.ActorUserId, now, cancellationToken).ConfigureAwait(false); user.RotateSecurityStamp(); await RevokeSessionsAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, null, audit, "TenantUserStoresChanged", "User", user.Id, before.StoreIds, command.StoreIds, now); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapUserAsync(tenantId, user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoreView>> ListStoresAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = TenantId(); var stores = await repository.ListStoresAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return stores.Where(CanAccessStore).Select(MapStore).ToArray();
    }

    public async Task<StoreView> GetStoreAsync(long storeId, CancellationToken cancellationToken = default)
    { var store = await RequireStoreAsync(storeId, false, cancellationToken).ConfigureAwait(false); return MapStore(store); }

    public async Task<StoreView> CreateStoreAsync(SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit); ValidateTimeZone(command.TimeZone); var tenantId=TenantId(); var tenant=await RequireTenantAsync(tenantId,cancellationToken).ConfigureAwait(false);
        var activeCount=await repository.CountActiveStoresAsync(tenantId,cancellationToken).ConfigureAwait(false); if(activeCount>=tenant.MaxStores)throw new TenantBusinessRuleException($"Tenant store quota ({tenant.MaxStores}) has been reached.");
        var code=string.IsNullOrWhiteSpace(command.StoreCode)?$"S-{Guid.NewGuid():N}"[..14].ToUpperInvariant():command.StoreCode;
        if(await db.Stores.AnyAsync(x=>x.TenantId==tenantId&&x.StoreCode==code.Trim().ToUpper(),cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("Store code already exists.");
        Store store; try{store=Store.Create(tenantId,code,command.StoreName,command.AddressLine1,command.AddressLine2,command.Landmark,command.City,command.District,command.StateOrProvince,command.PostalCode,command.CountryCode,command.Latitude,command.Longitude,command.GeoFenceRadiusMeters,command.ExternalPlaceId,command.LocationSource,command.TimeZone,command.ContactEmail,command.ContactMobile,UtcNow());}catch(ArgumentException e){throw new TenantBusinessRuleException(e.Message);}
        db.Stores.Add(store); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); RecordAudit(tenantId,store.Id,audit,"StoreCreated","Store",store.Id,null,MapStore(store),UtcNow()); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return MapStore(store);
    }

    public async Task<StoreView> UpdateStoreAsync(long storeId, SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ValidateAudit(audit);ValidateTimeZone(command.TimeZone);var store=await RequireStoreAsync(storeId,true,cancellationToken).ConfigureAwait(false);var before=MapStore(store);
        try{store.Update(command.StoreName,command.AddressLine1,command.AddressLine2,command.Landmark,command.City,command.District,command.StateOrProvince,command.PostalCode,command.CountryCode,command.Latitude,command.Longitude,command.GeoFenceRadiusMeters,command.ExternalPlaceId,command.LocationSource,command.TimeZone,command.ContactEmail,command.ContactMobile,UtcNow());}catch(ArgumentException e){throw new TenantBusinessRuleException(e.Message);}
        RecordAudit(TenantId(),store.Id,audit,"StoreUpdated","Store",store.Id,before,MapStore(store),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapStore(store);
    }

    public async Task<StoreView> SetStoreActiveAsync(long storeId,bool active,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {ValidateAudit(audit);var store=await RequireStoreAsync(storeId,true,cancellationToken).ConfigureAwait(false);if(active&&!store.IsActive){var tenant=await RequireTenantAsync(TenantId(),cancellationToken).ConfigureAwait(false);if(await repository.CountActiveStoresAsync(TenantId(),cancellationToken).ConfigureAwait(false)>=tenant.MaxStores)throw new TenantBusinessRuleException("Store quota has been reached.");}var before=store.IsActive;store.SetActive(active,UtcNow());RecordAudit(TenantId(),store.Id,audit,"StoreLifecycleChanged","Store",store.Id,new{IsActive=before},new{store.IsActive},UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapStore(store);}

    public async Task<StoreView> VerifyStoreLocationAsync(long storeId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {ValidateAudit(audit);var store=await RequireStoreAsync(storeId,true,cancellationToken).ConfigureAwait(false);if(store.Latitude is null||store.Longitude is null)throw new TenantBusinessRuleException("Coordinates are required before location verification.");store.VerifyLocation(audit.ActorUserId,UtcNow());RecordAudit(TenantId(),store.Id,audit,"StoreLocationVerified","Store",store.Id,null,new{store.Latitude,store.Longitude,store.LocationVerifiedUtc},UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapStore(store);}

    public async Task<IReadOnlyList<StaffView>> ListStaffAsync(CancellationToken cancellationToken=default){var tenantId=TenantId();var staff=await repository.ListStaffAsync(tenantId,cancellationToken).ConfigureAwait(false);var list=new List<StaffView>();foreach(var s in staff){var ids=await db.UserStoreAssignments.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.UserId==s.UserId).Select(x=>x.StoreId).ToArrayAsync(cancellationToken).ConfigureAwait(false);if(ids.Length==0||ids.Any(CanAccessStoreId)||IsTenantWide())list.Add(MapStaff(s,ids));}return list;}
    public async Task<StaffView> GetStaffAsync(long staffId,CancellationToken cancellationToken=default){var s=await repository.GetStaffAsync(TenantId(),staffId,false,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Staff");var ids=await UserStoreIdsAsync(TenantId(),s.UserId,cancellationToken).ConfigureAwait(false);EnsureAnyStoreAccess(ids);return MapStaff(s,ids);}

    public async Task<StaffView> CreateStaffAsync(CreateStaffCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ValidateAudit(audit);ValidatePassword(command.Password);var tenantId=TenantId();var tenant=await RequireTenantAsync(tenantId,cancellationToken).ConfigureAwait(false);if(await repository.CountActiveUsersAsync(tenantId,cancellationToken).ConfigureAwait(false)>=tenant.MaxUsers)throw new TenantBusinessRuleException("Tenant user quota has been reached.");
        if(await db.StaffProfiles.AnyAsync(x=>x.TenantId==tenantId&&x.EmployeeCode==command.EmployeeCode.Trim().ToUpper(),cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("Employee code already exists.");
        var now=UtcNow();var user=UserAccount.CreateTenant(tenantId,command.UserName,command.Email,$"{command.FirstName} {command.LastName}".Trim(),"TEMP",now);user.SetPasswordHash(passwordHasher.HashPassword(user,command.Password));
        await using var tx=await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);db.UserAccounts.Add(user);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);var profile=StaffProfile.Create(tenantId,user.Id,command.EmployeeCode,command.FirstName,command.LastName,command.Mobile,now);db.StaffProfiles.Add(profile);
        var roles=command.Roles.Count==0?new[]{"SalesStaff"}:command.Roles;await ReplaceRolesAsync(tenantId,user,roles,audit.ActorUserId,now,cancellationToken).ConfigureAwait(false);await ReplaceStoresAsync(tenantId,user,command.StoreIds,command.StoreIds.FirstOrDefault(),audit.ActorUserId,now,cancellationToken).ConfigureAwait(false);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);RecordAudit(tenantId,null,audit,"StaffCreated","StaffProfile",profile.Id,null,new{profile.EmployeeCode,profile.FirstName,profile.LastName,command.StoreIds},now);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);await tx.CommitAsync(cancellationToken).ConfigureAwait(false);return MapStaff(profile,command.StoreIds.Distinct().ToArray());
    }

    public async Task<StaffView> UpdateStaffAsync(long staffId,UpdateStaffCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var tenantId=TenantId();var staff=await repository.GetStaffAsync(tenantId,staffId,true,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Staff");var user=await repository.GetUserAsync(tenantId,staff.UserId,true,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Staff user");var before=new{staff.FirstName,staff.LastName,staff.Mobile,staff.IsActive};staff.Update(command.FirstName,command.LastName,command.Mobile,UtcNow());staff.SetActive(command.IsActive,UtcNow());if(command.IsActive)user.Activate();else user.Deactivate();await ReplaceStoresAsync(tenantId,user,command.StoreIds,command.StoreIds.FirstOrDefault(),audit.ActorUserId,UtcNow(),cancellationToken).ConfigureAwait(false);await RevokeSessionsAsync(user.Id,UtcNow(),cancellationToken).ConfigureAwait(false);RecordAudit(tenantId,null,audit,"StaffUpdated","StaffProfile",staff.Id,before,new{staff.FirstName,staff.LastName,staff.Mobile,staff.IsActive,command.StoreIds},UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapStaff(staff,command.StoreIds.Distinct().ToArray());}

    public async Task<StaffShiftView> CreateShiftAsync(long staffId,CreateStaffShiftCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var staff=await repository.GetStaffAsync(TenantId(),staffId,false,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Staff");await EnsureStaffAssignedToStoreAsync(staff.UserId,command.StoreId,cancellationToken).ConfigureAwait(false);EnsureStoreAccess(command.StoreId);var shift=StaffShift.Create(TenantId(),staffId,command.StoreId,ToUtc(command.StartsUtc),command.ScheduledEndsUtc.HasValue?ToUtc(command.ScheduledEndsUtc.Value):null,audit.ActorUserId,UtcNow());db.StaffShifts.Add(shift);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);RecordAudit(TenantId(),command.StoreId,audit,"StaffShiftCreated","StaffShift",shift.Id,null,MapShift(shift),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapShift(shift);}
    public async Task<StaffShiftView> StartShiftAsync(long shiftId,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var shift=await db.StaffShifts.SingleOrDefaultAsync(x=>x.TenantId==TenantId()&&x.Id==shiftId,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Shift");EnsureStoreAccess(shift.StoreId);try{shift.Start(UtcNow());}catch(InvalidOperationException e){throw new TenantBusinessRuleException(e.Message);}RecordAudit(TenantId(),shift.StoreId,audit,"StaffShiftStarted","StaffShift",shift.Id,null,MapShift(shift),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapShift(shift);}
    public async Task<StaffShiftView> CompleteShiftAsync(long shiftId,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var shift=await db.StaffShifts.SingleOrDefaultAsync(x=>x.TenantId==TenantId()&&x.Id==shiftId,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Shift");EnsureStoreAccess(shift.StoreId);try{shift.Complete(UtcNow());}catch(InvalidOperationException e){throw new TenantBusinessRuleException(e.Message);}RecordAudit(TenantId(),shift.StoreId,audit,"StaffShiftCompleted","StaffShift",shift.Id,null,MapShift(shift),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapShift(shift);}
    public async Task<StaffPresenceView> StartPresenceAsync(long staffId,StartStaffPresenceCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var staff=await repository.GetStaffAsync(TenantId(),staffId,false,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Staff");await EnsureStaffAssignedToStoreAsync(staff.UserId,command.StoreId,cancellationToken).ConfigureAwait(false);EnsureStoreAccess(command.StoreId);if(await db.StaffPresenceSessions.AnyAsync(x=>x.TenantId==TenantId()&&x.StaffProfileId==staffId&&x.StoreId==command.StoreId&&x.ExitedUtc==null,cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("An active presence session already exists.");var p=StaffPresenceSession.Start(TenantId(),staffId,command.StoreId,command.Source,UtcNow(),command.Confidence);db.StaffPresenceSessions.Add(p);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);RecordAudit(TenantId(),command.StoreId,audit,"StaffPresenceStarted","StaffPresenceSession",p.Id,null,MapPresence(p),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapPresence(p);}
    public async Task<StaffPresenceView> ClosePresenceAsync(long presenceId,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var p=await db.StaffPresenceSessions.SingleOrDefaultAsync(x=>x.TenantId==TenantId()&&x.Id==presenceId,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Presence session");EnsureStoreAccess(p.StoreId);if(p.ExitedUtc!=null)throw new TenantBusinessRuleException("Presence session is already closed.");p.Close(UtcNow());RecordAudit(TenantId(),p.StoreId,audit,"StaffPresenceClosed","StaffPresenceSession",p.Id,null,MapPresence(p),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapPresence(p);}

    public async Task<IReadOnlyList<ProductCategoryView>> ListCategoriesAsync(long? storeId,CancellationToken cancellationToken=default){var q=db.ProductCategories.AsNoTracking().Where(x=>x.TenantId==TenantId());if(storeId.HasValue){EnsureStoreAccess(storeId.Value);q=q.Where(x=>x.StoreId==null||x.StoreId==storeId);}return await q.OrderBy(x=>x.Name).Select(x=>new ProductCategoryView(x.Id,x.StoreId,x.CategoryCode,x.Name,x.ParentCategoryId,x.IsActive)).ToArrayAsync(cancellationToken).ConfigureAwait(false);}
    public async Task<ProductCategoryView> CreateCategoryAsync(SaveProductCategoryCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);if(command.StoreId.HasValue)await RequireStoreAsync(command.StoreId.Value,false,cancellationToken).ConfigureAwait(false);await ValidateParentCategoryAsync(command.ParentCategoryId,0,cancellationToken).ConfigureAwait(false);var code=command.CategoryCode.Trim().ToUpperInvariant();if(await db.ProductCategories.AnyAsync(x=>x.TenantId==TenantId()&&x.StoreId==command.StoreId&&x.CategoryCode==code,cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("Category code already exists in this scope.");var c=ProductCategory.Create(TenantId(),command.StoreId,code,command.Name,command.ParentCategoryId,UtcNow());if(!command.IsActive)c.Update(command.Name,command.ParentCategoryId,false,UtcNow());db.ProductCategories.Add(c);await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);RecordAudit(TenantId(),command.StoreId,audit,"ProductCategoryCreated","ProductCategory",c.Id,null,MapCategory(c),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapCategory(c);}
    public async Task<ProductCategoryView> UpdateCategoryAsync(long categoryId,SaveProductCategoryCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);var c=await db.ProductCategories.SingleOrDefaultAsync(x=>x.TenantId==TenantId()&&x.Id==categoryId,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Category");if(c.StoreId.HasValue)EnsureStoreAccess(c.StoreId.Value);if(command.StoreId!=c.StoreId)throw new TenantBusinessRuleException("Category store scope cannot be changed.");await ValidateParentCategoryAsync(command.ParentCategoryId,categoryId,cancellationToken).ConfigureAwait(false);var before=MapCategory(c);c.Update(command.Name,command.ParentCategoryId,command.IsActive,UtcNow());RecordAudit(TenantId(),c.StoreId,audit,"ProductCategoryUpdated","ProductCategory",c.Id,before,MapCategory(c),UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return MapCategory(c);}

    public async Task<StoreVoiceCommandSettingView> GetVoiceSettingAsync(long storeId,CancellationToken cancellationToken=default){await RequireStoreAsync(storeId,false,cancellationToken).ConfigureAwait(false);var setting=await db.StoreVoiceCommandSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==TenantId()&&x.StoreId==storeId,cancellationToken).ConfigureAwait(false);if(setting==null)return new(storeId,"Aasha Add",VoiceResponseMode.InAppAndVoice,true,true,[],DateTime.UnixEpoch);var aliases=await db.StoreVoiceCommandAliases.AsNoTracking().Where(x=>x.TenantId==TenantId()&&x.StoreId==storeId).OrderBy(x=>x.Alias).Select(x=>x.Alias).ToArrayAsync(cancellationToken).ConfigureAwait(false);return MapVoice(setting,aliases);}
    public async Task<StoreVoiceCommandSettingView> SaveVoiceSettingAsync(long storeId,SaveStoreVoiceCommandSettingCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default){ValidateAudit(audit);await RequireStoreAsync(storeId,false,cancellationToken).ConfigureAwait(false);if(string.IsNullOrWhiteSpace(command.TriggerKeyword))throw new TenantBusinessRuleException("Trigger keyword is required.");var setting=await db.StoreVoiceCommandSettings.SingleOrDefaultAsync(x=>x.TenantId==TenantId()&&x.StoreId==storeId,cancellationToken).ConfigureAwait(false);var before=setting==null?null:await GetVoiceSettingAsync(storeId,cancellationToken).ConfigureAwait(false);if(setting==null){setting=StoreVoiceCommandSetting.Create(TenantId(),storeId,command.TriggerKeyword,command.ResponseMode,UtcNow());db.StoreVoiceCommandSettings.Add(setting);}setting.Update(command.TriggerKeyword,command.ResponseMode,command.IsEnabled,command.RequireConfirmationForAmbiguousCategory,UtcNow());var old=await db.StoreVoiceCommandAliases.Where(x=>x.TenantId==TenantId()&&x.StoreId==storeId).ToListAsync(cancellationToken).ConfigureAwait(false);db.StoreVoiceCommandAliases.RemoveRange(old);foreach(var alias in command.Aliases.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)){if(string.Equals(alias,command.TriggerKeyword,StringComparison.OrdinalIgnoreCase))continue;db.StoreVoiceCommandAliases.Add(StoreVoiceCommandAlias.Create(TenantId(),storeId,alias,UtcNow()));}await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);var after=await GetVoiceSettingAsync(storeId,cancellationToken).ConfigureAwait(false);RecordAudit(TenantId(),storeId,audit,"StoreVoiceCommandSettingChanged","StoreVoiceCommandSetting",storeId,before,after,UtcNow());await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);return after;}

    private long TenantId()=>currentUser.TenantId is >0 and var id?id:throw new TenantBusinessRuleException("A tenant-scoped authenticated identity is required.");
    private bool IsTenantWide()=>currentUser.Roles.Any(TenantWideRoles.Contains);
    private bool CanAccessStore(Store s)=>CanAccessStoreId(s.Id); private bool CanAccessStoreId(long storeId)=>IsTenantWide()||currentUser.StoreIds.Contains(storeId); private void EnsureStoreAccess(long storeId){if(!CanAccessStoreId(storeId))throw new TenantBusinessRuleException("The authenticated user is not assigned to this store.");}
    private void EnsureAnyStoreAccess(IReadOnlyCollection<long> ids){if(!IsTenantWide()&&ids.Count>0&&!ids.Any(currentUser.StoreIds.Contains))throw new TenantBusinessRuleException("The authenticated user cannot access this staff member.");}
    private async Task<Tenant> RequireTenantAsync(long tenantId,CancellationToken ct)=>await repository.GetTenantAsync(tenantId,ct).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Tenant");
    private async Task<Store> RequireStoreAsync(long storeId,bool tracked,CancellationToken ct){var store=await repository.GetStoreAsync(TenantId(),storeId,tracked,ct).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Store");EnsureStoreAccess(storeId);return store;}
    private async Task EnsureStaffAssignedToStoreAsync(long userId,long storeId,CancellationToken ct){if(!await db.UserStoreAssignments.AnyAsync(x=>x.TenantId==TenantId()&&x.UserId==userId&&x.StoreId==storeId,ct).ConfigureAwait(false))throw new TenantBusinessRuleException("Staff member is not assigned to the selected store.");}
    private async Task ReplaceRolesAsync(long tenantId,UserAccount user,IReadOnlyList<string> names,long actor,DateTime now,CancellationToken ct){var distinct=names.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim().ToUpperInvariant()).Distinct().ToArray();if(distinct.Length==0)throw new TenantBusinessRuleException("At least one tenant role is required.");var roles=await db.Roles.Where(x=>x.TenantId==tenantId&&x.Scope==UserScope.Tenant&&x.IsActive&&distinct.Contains(x.NormalizedName)).ToListAsync(ct).ConfigureAwait(false);if(roles.Count!=distinct.Length)throw new TenantBusinessRuleException("One or more roles are invalid for this tenant.");var old=await db.UserRoles.Where(x=>x.UserId==user.Id).ToListAsync(ct).ConfigureAwait(false);db.UserRoles.RemoveRange(old);foreach(var role in roles)db.UserRoles.Add(UserRole.Assign(user,role,now,actor));}
    private async Task ReplaceStoresAsync(long tenantId,UserAccount user,IReadOnlyList<long> ids,long? primary,long actor,DateTime now,CancellationToken ct){var distinct=ids.Where(x=>x>0).Distinct().ToArray();if(primary.HasValue&&primary.Value>0&&!distinct.Contains(primary.Value))throw new TenantBusinessRuleException("Primary store must be included in StoreIds.");if(distinct.Length>0){var valid=await db.Stores.CountAsync(x=>x.TenantId==tenantId&&x.IsActive&&distinct.Contains(x.Id),ct).ConfigureAwait(false);if(valid!=distinct.Length)throw new TenantBusinessRuleException("One or more stores are invalid, inactive or belong to another tenant.");}var old=await db.UserStoreAssignments.Where(x=>x.TenantId==tenantId&&x.UserId==user.Id).ToListAsync(ct).ConfigureAwait(false);db.UserStoreAssignments.RemoveRange(old);foreach(var id in distinct)db.UserStoreAssignments.Add(UserStoreAssignment.Assign(tenantId,user.Id,id,primary==id,now,actor));}
    private async Task RevokeSessionsAsync(long userId,DateTime now,CancellationToken ct){var tokens=await db.RefreshTokens.Where(x=>x.UserId==userId&&x.RevokedUtc==null).ToListAsync(ct).ConfigureAwait(false);foreach(var token in tokens)token.Revoke(now,"AuthorizationChanged",null);}
    private async Task<(IReadOnlyList<string> Roles,IReadOnlyList<long> StoreIds)> GetUserAuthorizationAsync(long tenantId,long userId,CancellationToken ct){var roles=await db.UserRoles.AsNoTracking().Where(x=>x.UserId==userId&&x.Role.TenantId==tenantId&&x.Role.IsActive).Select(x=>x.Role.Name).Distinct().OrderBy(x=>x).ToArrayAsync(ct).ConfigureAwait(false);var stores=await UserStoreIdsAsync(tenantId,userId,ct).ConfigureAwait(false);return(roles,stores);}
    private Task<long[]> UserStoreIdsAsync(long tenantId,long userId,CancellationToken ct)=>db.UserStoreAssignments.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.UserId==userId&&x.Store.IsActive).OrderByDescending(x=>x.IsPrimary).ThenBy(x=>x.StoreId).Select(x=>x.StoreId).ToArrayAsync(ct);
    private async Task<TenantUserDetail> MapUserAsync(long tenantId,UserAccount u,CancellationToken ct){var a=await GetUserAuthorizationAsync(tenantId,u.Id,ct).ConfigureAwait(false);return new(u.Id,u.UserName,u.Email,u.DisplayName,u.IsActive,u.CreatedUtc,u.LastLoginUtc,a.Roles,a.StoreIds);}
    private static StoreView MapStore(Store x)=>new(x.Id,x.StoreCode,x.StoreName,x.AddressLine1,x.AddressLine2,x.Landmark,x.City,x.District,x.StateOrProvince,x.PostalCode,x.CountryCode,x.Latitude,x.Longitude,x.GeoFenceRadiusMeters,x.ExternalPlaceId,x.LocationSource,x.IsLocationVerified,x.LocationVerifiedUtc,x.TimeZone,x.ContactEmail,x.ContactMobile,x.IsActive,x.CreatedUtc,x.UpdatedUtc);
    private static StaffView MapStaff(StaffProfile x,IReadOnlyList<long> stores)=>new(x.Id,x.UserId,x.EmployeeCode,x.FirstName,x.LastName,x.Mobile,x.IsActive,stores);
    private static StaffShiftView MapShift(StaffShift x)=>new(x.Id,x.StaffProfileId,x.StoreId,x.StartsUtc,x.ScheduledEndsUtc,x.ActualEndsUtc,x.Status);
    private static StaffPresenceView MapPresence(StaffPresenceSession x)=>new(x.Id,x.StaffProfileId,x.StoreId,x.Source,x.EnteredUtc,x.ExitedUtc,x.Confidence);
    private static ProductCategoryView MapCategory(ProductCategory x)=>new(x.Id,x.StoreId,x.CategoryCode,x.Name,x.ParentCategoryId,x.IsActive);
    private static StoreVoiceCommandSettingView MapVoice(StoreVoiceCommandSetting x,IReadOnlyList<string> aliases)=>new(x.StoreId,x.TriggerKeyword,x.ResponseMode,x.IsEnabled,x.RequireConfirmationForAmbiguousCategory,aliases,x.UpdatedUtc);
    private async Task ValidateParentCategoryAsync(long? parentId,long currentId,CancellationToken ct){if(!parentId.HasValue)return;if(parentId==currentId)throw new TenantBusinessRuleException("Category cannot be its own parent.");if(!await db.ProductCategories.AnyAsync(x=>x.TenantId==TenantId()&&x.Id==parentId&&x.IsActive,ct).ConfigureAwait(false))throw new TenantBusinessRuleException("Parent category is invalid.");}
    private static void ValidatePassword(string value){if(string.IsNullOrWhiteSpace(value)||value.Length<10||!value.Any(char.IsUpper)||!value.Any(char.IsLower)||!value.Any(char.IsDigit))throw new TenantBusinessRuleException("Password must be at least 10 characters and contain upper, lower and numeric characters.");}
    private static void ValidateTimeZone(string value){ArgumentException.ThrowIfNullOrWhiteSpace(value);try{_ = TimeZoneInfo.FindSystemTimeZoneById(value);}catch(TimeZoneNotFoundException){throw new TenantBusinessRuleException("Time zone is invalid on this server.");}catch(InvalidTimeZoneException){throw new TenantBusinessRuleException("Time zone is invalid on this server.");}}
    private static DateTime ToUtc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:value.ToUniversalTime(); private DateTime UtcNow()=>timeProvider.GetUtcNow().UtcDateTime;
    private static void ValidateAudit(TenantAuditContext a){if(a.ActorUserId<=0||string.IsNullOrWhiteSpace(a.CorrelationId))throw new ArgumentException("Valid audit context is required.");}
    private void RecordAudit(long tenantId,long? storeId,TenantAuditContext a,string action,string type,long id,object? before,object? after,DateTime now)=>db.AuditLogs.Add(AuditLog.Record(tenantId,storeId,a.ActorUserId,"User",action,type,id.ToString(System.Globalization.CultureInfo.InvariantCulture),before==null?null:JsonSerializer.Serialize(before),after==null?null:JsonSerializer.Serialize(after),a.IpAddress,a.UserAgent,a.CorrelationId,now));
}
