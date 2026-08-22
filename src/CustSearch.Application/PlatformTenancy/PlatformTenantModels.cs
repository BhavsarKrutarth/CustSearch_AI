namespace CustSearch.Application.PlatformTenancy;

/// <summary>Returns one safe page of platform management data and its paging totals.</summary>
public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

/// <summary>Defines validated tenant-list filters accepted from a platform administrator.</summary>
public sealed record PlatformTenantQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Status,
    long? PlanId);

/// <summary>Provides the compact tenant data needed by the platform tenant table.</summary>
public sealed record PlatformTenantListItem(
    long Id,
    string TenantCode,
    string LegalName,
    string DisplayName,
    string PrimaryContactName,
    string PrimaryEmail,
    string? PrimaryMobile,
    TenantPlanSummary? Plan,
    int StoreCount,
    int UserCount,
    int CameraCount,
    int ShopperCustomerCount,
    string Status,
    string SubscriptionStatus,
    DateTime? LastActivityUtc,
    string Version);

/// <summary>Provides the small subscription plan identity embedded in tenant responses.</summary>
public sealed record TenantPlanSummary(long Id, string PlanCode, string PlanName);

/// <summary>Provides the complete safe platform view of one tenant profile and its limits.</summary>
public sealed record PlatformTenantDetail(
    long Id,
    string TenantCode,
    string LegalName,
    string DisplayName,
    string TimeZone,
    string PrimaryContactName,
    string PrimaryEmail,
    string? PrimaryMobile,
    string CountryCode,
    string CurrencyCode,
    string Status,
    string SubscriptionStatus,
    TenantPlanSummary? Plan,
    DateTime? TrialStartsUtc,
    DateTime? TrialEndsUtc,
    DateTime? SubscriptionStartsUtc,
    DateTime? SubscriptionEndsUtc,
    int MaxStores,
    int MaxUsers,
    int MaxCameras,
    string? SuspensionReason,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    string Version);

/// <summary>Contains validated tenant profile values for platform-side creation.</summary>
public sealed record CreatePlatformTenantCommand(
    string LegalName,
    string DisplayName,
    string TimeZone,
    string PrimaryContactName,
    string PrimaryEmail,
    string? PrimaryMobile,
    string CountryCode,
    string CurrencyCode,
    long? PlanId,
    int? MaxStores,
    int? MaxUsers,
    int? MaxCameras,
    string? AuditReason);

/// <summary>Contains editable tenant profile values plus the last version seen by the caller.</summary>
public sealed record UpdatePlatformTenantCommand(
    string LegalName,
    string DisplayName,
    string TimeZone,
    string PrimaryContactName,
    string PrimaryEmail,
    string? PrimaryMobile,
    string CountryCode,
    string CurrencyCode,
    string ExpectedVersion);

/// <summary>Captures request evidence written with every platform management audit record.</summary>
public sealed record PlatformAuditContext(
    long ActorUserId,
    string? IpAddress,
    string? UserAgent,
    string CorrelationId);

/// <summary>Provides current tenant counts and the limits that constrain future resources.</summary>
public sealed record PlatformTenantSummary(
    long TenantId,
    string TenantCode,
    string Status,
    string SubscriptionStatus,
    string? PlanName,
    int Stores,
    int Users,
    int Cameras,
    int MaxStores,
    int MaxUsers,
    int MaxCameras,
    long MonthlyRecognitions,
    long MonthlyApiCalls,
    DateTime? UsageCapturedUtc);

/// <summary>Provides one tenant usage period for quota charts and operational review.</summary>
public sealed record PlatformTenantUsageItem(
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int StoreCount,
    int UserCount,
    int CameraCount,
    long RecognitionCount,
    long ApiCallCount,
    DateTime CapturedUtc);

/// <summary>Provides a safe platform audit row without authentication secrets or raw entities.</summary>
public sealed record PlatformAuditItem(
    long Id,
    long? TenantId,
    long? UserId,
    string ActorType,
    string Action,
    string EntityType,
    string? EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? IpAddress,
    string CorrelationId,
    DateTime CreatedUtc);

/// <summary>Provides a subscription plan and its enforceable resource limits.</summary>
public sealed record SubscriptionPlanView(
    long Id,
    string PlanCode,
    string PlanName,
    decimal MonthlyPrice,
    decimal? AnnualPrice,
    int MaxStores,
    int MaxUsers,
    int MaxCameras,
    long? MaxMonthlyRecognitions,
    long? MaxMonthlyApiCalls,
    bool IsActive,
    string Version);

/// <summary>Contains validated values for creating or updating a subscription plan.</summary>
public sealed record SaveSubscriptionPlanCommand(
    string PlanCode,
    string PlanName,
    decimal MonthlyPrice,
    decimal? AnnualPrice,
    int MaxStores,
    int MaxUsers,
    int MaxCameras,
    long? MaxMonthlyRecognitions,
    long? MaxMonthlyApiCalls,
    bool IsActive,
    string? ExpectedVersion);

/// <summary>Assigns a plan and optional audited quota overrides to one tenant.</summary>
public sealed record AssignTenantSubscriptionCommand(
    long SubscriptionPlanId,
    string BillingCycle,
    string Status,
    DateTime StartsUtc,
    DateTime? EndsUtc,
    bool AutoRenew,
    int? MaxStores,
    int? MaxUsers,
    int? MaxCameras,
    long? MaxMonthlyRecognitions,
    long? MaxMonthlyApiCalls,
    string ExpectedVersion,
    string AuditReason);

/// <summary>Provides platform-wide tenant lifecycle and recurring revenue indicators.</summary>
public sealed record PlatformDashboardSummary(
    int TotalTenants,
    int ActiveTenants,
    int TrialTenants,
    int SuspendedTenants,
    int InactiveTenants,
    decimal MonthlyRecurringRevenue,
    int TotalTenantUsers,
    int TotalCameras);
