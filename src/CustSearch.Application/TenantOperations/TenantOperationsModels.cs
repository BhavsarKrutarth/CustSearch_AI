using CustSearch.Domain.Enums;

namespace CustSearch.Application.TenantOperations;

/// <summary>Phase 5 shared audit metadata supplied by the trusted API boundary.</summary>
public sealed record TenantAuditContext(long ActorUserId, string? IpAddress, string? UserAgent, string CorrelationId);

/// <summary>Phase 5G — real tenant dashboard base, replacing hard-coded UI data.</summary>
public sealed record TenantDashboardSummary(long ActiveUsers, long ActiveStores, long ActiveStaff, long ActiveCategories, long OpenShifts, long ActivePresenceSessions);

public sealed record TenantUserListItem(long Id, string UserName, string Email, string DisplayName, bool IsActive, DateTime CreatedUtc, IReadOnlyList<string> Roles, IReadOnlyList<long> StoreIds);
public sealed record TenantUserDetail(long Id, string UserName, string Email, string DisplayName, bool IsActive, DateTime CreatedUtc, DateTime? LastLoginUtc, IReadOnlyList<string> Roles, IReadOnlyList<long> StoreIds);
public sealed record CreateTenantUserCommand(string UserName, string Email, string DisplayName, string Password, IReadOnlyList<string> Roles, IReadOnlyList<long> StoreIds);
public sealed record UpdateTenantUserCommand(string Email, string DisplayName, bool IsActive);
public sealed record SetTenantUserRolesCommand(IReadOnlyList<string> Roles);
public sealed record SetTenantUserStoresCommand(IReadOnlyList<long> StoreIds, long? PrimaryStoreId);
public sealed record ResetTenantUserPasswordCommand(string NewPassword);

public sealed record StoreView(long Id, string StoreCode, string StoreName, string AddressLine1, string? AddressLine2, string? Landmark, string City, string? District, string StateOrProvince, string PostalCode, string CountryCode, decimal? Latitude, decimal? Longitude, decimal? GeoFenceRadiusMeters, string? ExternalPlaceId, StoreLocationSource LocationSource, bool IsLocationVerified, DateTime? LocationVerifiedUtc, string TimeZone, string? ContactEmail, string? ContactMobile, bool IsActive, DateTime CreatedUtc, DateTime UpdatedUtc);
public sealed record SaveStoreCommand(string? StoreCode, string StoreName, string AddressLine1, string? AddressLine2, string? Landmark, string City, string? District, string StateOrProvince, string PostalCode, string CountryCode, decimal? Latitude, decimal? Longitude, decimal? GeoFenceRadiusMeters, string? ExternalPlaceId, StoreLocationSource LocationSource, string TimeZone, string? ContactEmail, string? ContactMobile);

public sealed record StaffView(long Id, long UserId, string EmployeeCode, string FirstName, string LastName, string? Mobile, bool IsActive, IReadOnlyList<long> StoreIds);
public sealed record CreateStaffCommand(string EmployeeCode, string FirstName, string LastName, string? Mobile, string UserName, string Email, string Password, IReadOnlyList<string> Roles, IReadOnlyList<long> StoreIds);
public sealed record UpdateStaffCommand(string FirstName, string LastName, string? Mobile, bool IsActive, IReadOnlyList<long> StoreIds);
public sealed record StaffShiftView(long Id, long StaffProfileId, long StoreId, DateTime StartsUtc, DateTime? ScheduledEndsUtc, DateTime? ActualEndsUtc, StaffShiftStatus Status);
public sealed record CreateStaffShiftCommand(long StoreId, DateTime StartsUtc, DateTime? ScheduledEndsUtc);
public sealed record StaffPresenceView(long Id, long StaffProfileId, long StoreId, StaffPresenceSource Source, DateTime EnteredUtc, DateTime? ExitedUtc, decimal Confidence);
public sealed record StartStaffPresenceCommand(long StoreId, StaffPresenceSource Source, decimal Confidence);

public sealed record ProductCategoryView(long Id, long? StoreId, string CategoryCode, string Name, long? ParentCategoryId, bool IsActive);
public sealed record SaveProductCategoryCommand(long? StoreId, string CategoryCode, string Name, long? ParentCategoryId, bool IsActive);

public sealed record StoreVoiceCommandSettingView(long StoreId, string TriggerKeyword, VoiceResponseMode ResponseMode, bool IsEnabled, bool RequireConfirmationForAmbiguousCategory, IReadOnlyList<string> Aliases, DateTime UpdatedUtc);
public sealed record SaveStoreVoiceCommandSettingCommand(string TriggerKeyword, VoiceResponseMode ResponseMode, bool IsEnabled, bool RequireConfirmationForAmbiguousCategory, IReadOnlyList<string> Aliases);

public sealed class TenantResourceNotFoundException(string resource) : Exception($"{resource} was not found.");
public sealed class TenantBusinessRuleException(string message) : Exception(message);
