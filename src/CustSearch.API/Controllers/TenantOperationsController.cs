using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Phase 5 tenant-scoped operational APIs. Tenant ownership always comes from the validated JWT/session.</summary>
[ApiController]
[Route("api/tenant")]
[Authorize(Policy = AuthorizationPolicyNames.TenantScope)]
public sealed class TenantOperationsController(ITenantOperationsService service, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("dashboard/summary")]
    [HasPermission(PermissionCatalog.Tenant.DashboardView)]
    public Task<TenantDashboardSummary> Dashboard(CancellationToken ct) => service.GetDashboardAsync(ct);

    [HttpGet("users")]
    [HasPermission(PermissionCatalog.Tenant.UsersView)]
    public Task<IReadOnlyList<TenantUserListItem>> Users(CancellationToken ct) => service.ListUsersAsync(ct);

    [HttpGet("users/{userId:long}")]
    [HasPermission(PermissionCatalog.Tenant.UsersView)]
    public Task<TenantUserDetail> User(long userId, CancellationToken ct) => service.GetUserAsync(userId, ct);

    [HttpPost("users")]
    [HasPermission(PermissionCatalog.Tenant.UsersCreate)]
    public Task<TenantUserDetail> CreateUser(CreateTenantUserRequest request, CancellationToken ct) =>
        service.CreateUserAsync(request.ToCommand(), Audit(), ct);

    [HttpPut("users/{userId:long}")]
    [HasPermission(PermissionCatalog.Tenant.UsersEdit)]
    public Task<TenantUserDetail> UpdateUser(long userId, UpdateTenantUserRequest request, CancellationToken ct) =>
        service.UpdateUserAsync(userId, request.ToCommand(), Audit(), ct);

    [HttpPut("users/{userId:long}/roles")]
    [HasPermission(PermissionCatalog.Tenant.UsersAssignRoles)]
    public Task<TenantUserDetail> SetRoles(long userId, SetTenantUserRolesRequest request, CancellationToken ct) =>
        service.SetUserRolesAsync(userId, new(request.Roles), Audit(), ct);

    [HttpPut("users/{userId:long}/stores")]
    [HasPermission(PermissionCatalog.Tenant.UsersEdit)]
    public Task<TenantUserDetail> SetStores(long userId, SetTenantUserStoresRequest request, CancellationToken ct) =>
        service.SetUserStoresAsync(userId, new(request.StoreIds, request.PrimaryStoreId), Audit(), ct);

    [HttpGet("stores")]
    [HasPermission(PermissionCatalog.Tenant.StoresView)]
    public Task<IReadOnlyList<StoreView>> Stores(CancellationToken ct) => service.ListStoresAsync(ct);

    [HttpGet("stores/{storeId:long}")]
    [HasPermission(PermissionCatalog.Tenant.StoresView)]
    public Task<StoreView> Store(long storeId, CancellationToken ct) => service.GetStoreAsync(storeId, ct);

    [HttpPost("stores")]
    [HasPermission(PermissionCatalog.Tenant.StoresCreate)]
    public Task<StoreView> CreateStore(SaveStoreRequest request, CancellationToken ct) => service.CreateStoreAsync(request.ToCommand(), Audit(), ct);

    [HttpPut("stores/{storeId:long}")]
    [HasPermission(PermissionCatalog.Tenant.StoresEdit)]
    public Task<StoreView> UpdateStore(long storeId, SaveStoreRequest request, CancellationToken ct) => service.UpdateStoreAsync(storeId, request.ToCommand(), Audit(), ct);

    [HttpPost("stores/{storeId:long}/activate")]
    [HasPermission(PermissionCatalog.Tenant.StoresEdit)]
    public Task<StoreView> ActivateStore(long storeId, CancellationToken ct) => service.SetStoreActiveAsync(storeId, true, Audit(), ct);

    [HttpPost("stores/{storeId:long}/deactivate")]
    [HasPermission(PermissionCatalog.Tenant.StoresEdit)]
    public Task<StoreView> DeactivateStore(long storeId, CancellationToken ct) => service.SetStoreActiveAsync(storeId, false, Audit(), ct);

    [HttpPost("stores/{storeId:long}/verify-location")]
    [HasPermission(PermissionCatalog.Tenant.StoresEdit)]
    public Task<StoreView> VerifyLocation(long storeId, CancellationToken ct) => service.VerifyStoreLocationAsync(storeId, Audit(), ct);

    [HttpGet("staff")]
    [HasPermission(PermissionCatalog.Operations.StaffView)]
    public Task<IReadOnlyList<StaffView>> Staff(CancellationToken ct) => service.ListStaffAsync(ct);

    [HttpGet("staff/{staffId:long}")]
    [HasPermission(PermissionCatalog.Operations.StaffView)]
    public Task<StaffView> Staff(long staffId, CancellationToken ct) => service.GetStaffAsync(staffId, ct);

    [HttpPost("staff")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffView> CreateStaff(CreateStaffRequest request, CancellationToken ct) => service.CreateStaffAsync(request.ToCommand(), Audit(), ct);

    [HttpPut("staff/{staffId:long}")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffView> UpdateStaff(long staffId, UpdateStaffRequest request, CancellationToken ct) => service.UpdateStaffAsync(staffId, request.ToCommand(), Audit(), ct);

    [HttpPost("staff/{staffId:long}/shifts")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffShiftView> CreateShift(long staffId, CreateStaffShiftRequest request, CancellationToken ct) => service.CreateShiftAsync(staffId, new(request.StoreId, request.StartsUtc, request.ScheduledEndsUtc), Audit(), ct);

    [HttpPost("staff/shifts/{shiftId:long}/start")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffShiftView> StartShift(long shiftId, CancellationToken ct) => service.StartShiftAsync(shiftId, Audit(), ct);

    [HttpPost("staff/shifts/{shiftId:long}/complete")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffShiftView> CompleteShift(long shiftId, CancellationToken ct) => service.CompleteShiftAsync(shiftId, Audit(), ct);

    [HttpPost("staff/{staffId:long}/presence")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffPresenceView> StartPresence(long staffId, StartStaffPresenceRequest request, CancellationToken ct) => service.StartPresenceAsync(staffId, new(request.StoreId, request.Source, request.Confidence), Audit(), ct);

    [HttpPost("staff/presence/{presenceId:long}/close")]
    [HasPermission(PermissionCatalog.Operations.StaffManage)]
    public Task<StaffPresenceView> ClosePresence(long presenceId, CancellationToken ct) => service.ClosePresenceAsync(presenceId, Audit(), ct);

    [HttpGet("store-categories")]
    [HasPermission(PermissionCatalog.Operations.StoreCategoriesView)]
    public Task<IReadOnlyList<ProductCategoryView>> Categories([FromQuery] long? storeId, CancellationToken ct) => service.ListCategoriesAsync(storeId, ct);

    [HttpPost("store-categories")]
    [HasPermission(PermissionCatalog.Operations.StoreCategoriesManage)]
    public Task<ProductCategoryView> CreateCategory(SaveProductCategoryRequest request, CancellationToken ct) => service.CreateCategoryAsync(request.ToCommand(), Audit(), ct);

    [HttpPut("store-categories/{categoryId:long}")]
    [HasPermission(PermissionCatalog.Operations.StoreCategoriesManage)]
    public Task<ProductCategoryView> UpdateCategory(long categoryId, SaveProductCategoryRequest request, CancellationToken ct) => service.UpdateCategoryAsync(categoryId, request.ToCommand(), Audit(), ct);

    [HttpGet("stores/{storeId:long}/voice-command-setting")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsView)]
    public Task<StoreVoiceCommandSettingView> VoiceSetting(long storeId, CancellationToken ct) => service.GetVoiceSettingAsync(storeId, ct);

    [HttpPut("stores/{storeId:long}/voice-command-setting")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsConfigure)]
    public Task<StoreVoiceCommandSettingView> SaveVoiceSetting(long storeId, SaveVoiceSettingRequest request, CancellationToken ct) => service.SaveVoiceSettingAsync(storeId, request.ToCommand(), Audit(), ct);

    private TenantAuditContext Audit() => new(currentUser.UserId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
}

public sealed record CreateTenantUserRequest([param:Required,StringLength(100)]string UserName,[param:Required,EmailAddress,StringLength(254)]string Email,[param:Required,StringLength(150)]string DisplayName,[param:Required,MinLength(10)]string Password,IReadOnlyList<string> Roles,IReadOnlyList<long> StoreIds){public CreateTenantUserCommand ToCommand()=>new(UserName,Email,DisplayName,Password,Roles??[],StoreIds??[]);}
public sealed record UpdateTenantUserRequest([param:Required,EmailAddress,StringLength(254)]string Email,[param:Required,StringLength(150)]string DisplayName,bool IsActive){public UpdateTenantUserCommand ToCommand()=>new(Email,DisplayName,IsActive);}
public sealed record SetTenantUserRolesRequest(IReadOnlyList<string> Roles);
public sealed record SetTenantUserStoresRequest(IReadOnlyList<long> StoreIds,long? PrimaryStoreId);
public sealed record SaveStoreRequest([param:StringLength(30)]string? StoreCode,[param:Required,StringLength(150)]string StoreName,[param:Required,StringLength(250)]string AddressLine1,[param:StringLength(250)]string? AddressLine2,[param:StringLength(150)]string? Landmark,[param:Required,StringLength(100)]string City,[param:StringLength(100)]string? District,[param:Required,StringLength(100)]string StateOrProvince,[param:Required,StringLength(20)]string PostalCode,[param:Required,StringLength(2,MinimumLength=2)]string CountryCode,decimal? Latitude,decimal? Longitude,decimal? GeoFenceRadiusMeters,[param:StringLength(200)]string? ExternalPlaceId,StoreLocationSource LocationSource,[param:Required,StringLength(100)]string TimeZone,[param:EmailAddress,StringLength(254)]string? ContactEmail,[param:StringLength(30)]string? ContactMobile){public SaveStoreCommand ToCommand()=>new(StoreCode,StoreName,AddressLine1,AddressLine2,Landmark,City,District,StateOrProvince,PostalCode,CountryCode,Latitude,Longitude,GeoFenceRadiusMeters,ExternalPlaceId,LocationSource,TimeZone,ContactEmail,ContactMobile);}
public sealed record CreateStaffRequest([param:Required,StringLength(50)]string EmployeeCode,[param:Required,StringLength(100)]string FirstName,[param:Required,StringLength(100)]string LastName,[param:StringLength(30)]string? Mobile,[param:Required,StringLength(100)]string UserName,[param:Required,EmailAddress,StringLength(254)]string Email,[param:Required,MinLength(10)]string Password,IReadOnlyList<string> Roles,IReadOnlyList<long> StoreIds){public CreateStaffCommand ToCommand()=>new(EmployeeCode,FirstName,LastName,Mobile,UserName,Email,Password,Roles??[],StoreIds??[]);}
public sealed record UpdateStaffRequest([param:Required,StringLength(100)]string FirstName,[param:Required,StringLength(100)]string LastName,[param:StringLength(30)]string? Mobile,bool IsActive,IReadOnlyList<long> StoreIds){public UpdateStaffCommand ToCommand()=>new(FirstName,LastName,Mobile,IsActive,StoreIds??[]);}
public sealed record CreateStaffShiftRequest(long StoreId,DateTime StartsUtc,DateTime? ScheduledEndsUtc);
public sealed record StartStaffPresenceRequest(long StoreId,StaffPresenceSource Source,[param:Range(typeof(decimal),"0","1")]decimal Confidence);
public sealed record SaveProductCategoryRequest(long? StoreId,[param:Required,StringLength(50)]string CategoryCode,[param:Required,StringLength(150)]string Name,long? ParentCategoryId,bool IsActive){public SaveProductCategoryCommand ToCommand()=>new(StoreId,CategoryCode,Name,ParentCategoryId,IsActive);}
public sealed record SaveVoiceSettingRequest([param:Required,StringLength(100)]string TriggerKeyword,VoiceResponseMode ResponseMode,bool IsEnabled,bool RequireConfirmationForAmbiguousCategory,IReadOnlyList<string> Aliases){public SaveStoreVoiceCommandSettingCommand ToCommand()=>new(TriggerKeyword,ResponseMode,IsEnabled,RequireConfirmationForAmbiguousCategory,Aliases??[]);}
