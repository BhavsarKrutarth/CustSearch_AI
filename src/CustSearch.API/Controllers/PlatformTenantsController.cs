using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.PlatformTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>
/// Exposes platform-only tenant lifecycle, subscription, usage and audit operations with granular permissions.
/// </summary>
[ApiController]
[Route("api/platform")]
[Authorize(Policy = AuthorizationPolicyNames.PlatformScope)]
public sealed class PlatformTenantsController(
    IPlatformTenantManagementService service,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("dashboard")]
    [HasPermission(PermissionCatalog.Platform.TenantsViewOperationalSummary)]
    public Task<PlatformDashboardSummary> Dashboard(CancellationToken cancellationToken) =>
        service.GetDashboardAsync(cancellationToken);

    [HttpGet("tenants")]
    [HasPermission(PermissionCatalog.Platform.TenantsView)]
    public Task<PageResult<PlatformTenantListItem>> ListTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] long? planId = null,
        CancellationToken cancellationToken = default) =>
        service.ListTenantsAsync(
            new PlatformTenantQuery(page, pageSize, search, status, planId),
            cancellationToken);

    [HttpGet("tenants/{tenantId:long}")]
    [HasPermission(PermissionCatalog.Platform.TenantsView)]
    public async Task<PlatformTenantDetail> GetTenant(long tenantId, CancellationToken cancellationToken) =>
        await service.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Tenant");

    [HttpPost("tenants")]
    [HasPermission(PermissionCatalog.Platform.TenantsCreate)]
    public async Task<ActionResult<PlatformTenantDetail>> CreateTenant(
        CreatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateTenantAsync(
            request.ToCommand(),
            CreateAuditContext(),
            cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetTenant), new { tenantId = created.Id }, created);
    }

    [HttpPut("tenants/{tenantId:long}")]
    [HasPermission(PermissionCatalog.Platform.TenantsEdit)]
    public Task<PlatformTenantDetail> UpdateTenant(
        long tenantId,
        UpdatePlatformTenantRequest request,
        CancellationToken cancellationToken) =>
        service.UpdateTenantAsync(tenantId, request.ToCommand(), CreateAuditContext(), cancellationToken);

    [HttpPost("tenants/{tenantId:long}/activate")]
    [HasPermission(PermissionCatalog.Platform.TenantsActivate)]
    public Task<PlatformTenantDetail> ActivateTenant(
        long tenantId,
        TenantLifecycleRequest request,
        CancellationToken cancellationToken) =>
        service.ActivateTenantAsync(tenantId, request.ExpectedVersion, CreateAuditContext(), cancellationToken);

    [HttpPost("tenants/{tenantId:long}/suspend")]
    [HasPermission(PermissionCatalog.Platform.TenantsSuspend)]
    public Task<PlatformTenantDetail> SuspendTenant(
        long tenantId,
        TenantLifecycleRequest request,
        CancellationToken cancellationToken) =>
        service.SuspendTenantAsync(
            tenantId,
            request.Reason ?? string.Empty,
            request.ExpectedVersion,
            CreateAuditContext(),
            cancellationToken);

    [HttpGet("tenants/{tenantId:long}/summary")]
    [HasPermission(PermissionCatalog.Platform.TenantsViewOperationalSummary)]
    public async Task<PlatformTenantSummary> TenantSummary(
        long tenantId,
        CancellationToken cancellationToken) =>
        await service.GetTenantSummaryAsync(tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformResourceNotFoundException("Tenant");

    [HttpGet("tenants/{tenantId:long}/usage")]
    [HasPermission(PermissionCatalog.Platform.TenantsViewUsage)]
    public Task<IReadOnlyList<PlatformTenantUsageItem>> TenantUsage(
        long tenantId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken) =>
        service.GetTenantUsageAsync(tenantId, fromUtc, toUtc, cancellationToken);

    [HttpGet("tenants/{tenantId:long}/audit")]
    [HasPermission(PermissionCatalog.Platform.AuditView)]
    public Task<PageResult<PlatformAuditItem>> TenantAudit(
        long tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        service.GetTenantAuditAsync(tenantId, page, pageSize, cancellationToken);

    [HttpPut("tenants/{tenantId:long}/subscription")]
    [HasPermission(PermissionCatalog.Platform.SubscriptionPlansManage)]
    public Task<PlatformTenantDetail> AssignSubscription(
        long tenantId,
        AssignTenantSubscriptionRequest request,
        CancellationToken cancellationToken) =>
        service.AssignSubscriptionAsync(tenantId, request.ToCommand(), CreateAuditContext(), cancellationToken);

    [HttpGet("subscription-plans")]
    [HasPermission(PermissionCatalog.Platform.SubscriptionPlansView)]
    public Task<IReadOnlyList<SubscriptionPlanView>> ListPlans(CancellationToken cancellationToken) =>
        service.ListPlansAsync(cancellationToken);

    [HttpPost("subscription-plans")]
    [HasPermission(PermissionCatalog.Platform.SubscriptionPlansManage)]
    public async Task<ActionResult<SubscriptionPlanView>> CreatePlan(
        SaveSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreatePlanAsync(
            request.ToCommand(),
            CreateAuditContext(),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/platform/subscription-plans/{created.Id}", created);
    }

    [HttpPut("subscription-plans/{planId:long}")]
    [HasPermission(PermissionCatalog.Platform.SubscriptionPlansManage)]
    public Task<SubscriptionPlanView> UpdatePlan(
        long planId,
        SaveSubscriptionPlanRequest request,
        CancellationToken cancellationToken) =>
        service.UpdatePlanAsync(planId, request.ToCommand(), CreateAuditContext(), cancellationToken);

    private PlatformAuditContext CreateAuditContext() => new(
        currentUser.UserId,
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(),
        HttpContext.TraceIdentifier);
}

/// <summary>Validates the platform request used to create one tenant.</summary>
public sealed record CreatePlatformTenantRequest(
    [param: Required, StringLength(200)] string LegalName,
    [param: Required, StringLength(150)] string DisplayName,
    [param: Required, StringLength(150)] string PrimaryContactName,
    [param: Required, EmailAddress, StringLength(254)] string PrimaryEmail,
    [param: StringLength(30)] string? PrimaryMobile,
    [param: Required, StringLength(2, MinimumLength = 2)] string CountryCode,
    [param: Required, StringLength(100)] string TimeZone,
    [param: Required, StringLength(3, MinimumLength = 3)] string CurrencyCode,
    long? PlanId,
    [param: Range(1, int.MaxValue)] int? MaxStores,
    [param: Range(1, int.MaxValue)] int? MaxUsers,
    [param: Range(1, int.MaxValue)] int? MaxCameras,
    [param: StringLength(500)] string? AuditReason)
{
    public CreatePlatformTenantCommand ToCommand() => new(
        LegalName, DisplayName, TimeZone, PrimaryContactName, PrimaryEmail,
        PrimaryMobile, CountryCode, CurrencyCode, PlanId, MaxStores, MaxUsers, MaxCameras, AuditReason);
}

/// <summary>Validates editable tenant profile values and its optimistic version token.</summary>
public sealed record UpdatePlatformTenantRequest(
    [param: Required, StringLength(200)] string LegalName,
    [param: Required, StringLength(150)] string DisplayName,
    [param: Required, StringLength(150)] string PrimaryContactName,
    [param: Required, EmailAddress, StringLength(254)] string PrimaryEmail,
    [param: StringLength(30)] string? PrimaryMobile,
    [param: Required, StringLength(2, MinimumLength = 2)] string CountryCode,
    [param: Required, StringLength(100)] string TimeZone,
    [param: Required, StringLength(3, MinimumLength = 3)] string CurrencyCode,
    [param: Required] string ExpectedVersion)
{
    public UpdatePlatformTenantCommand ToCommand() => new(
        LegalName, DisplayName, TimeZone, PrimaryContactName, PrimaryEmail,
        PrimaryMobile, CountryCode, CurrencyCode, ExpectedVersion);
}

/// <summary>Requires the last version and a reason for lifecycle actions that need explanation.</summary>
public sealed record TenantLifecycleRequest(
    [param: Required] string ExpectedVersion,
    [param: StringLength(500)] string? Reason);

/// <summary>Validates subscription plan prices and enforceable limits.</summary>
public sealed record SaveSubscriptionPlanRequest(
    [param: Required, StringLength(30)] string PlanCode,
    [param: Required, StringLength(100)] string PlanName,
    [param: Range(typeof(decimal), "0", "999999999999.99")] decimal MonthlyPrice,
    [param: Range(typeof(decimal), "0", "999999999999.99")] decimal? AnnualPrice,
    [param: Range(1, int.MaxValue)] int MaxStores,
    [param: Range(1, int.MaxValue)] int MaxUsers,
    [param: Range(1, int.MaxValue)] int MaxCameras,
    [param: Range(1, long.MaxValue)] long? MaxMonthlyRecognitions,
    [param: Range(1, long.MaxValue)] long? MaxMonthlyApiCalls,
    bool IsActive,
    string? ExpectedVersion)
{
    public SaveSubscriptionPlanCommand ToCommand() => new(
        PlanCode, PlanName, MonthlyPrice, AnnualPrice, MaxStores, MaxUsers, MaxCameras,
        MaxMonthlyRecognitions, MaxMonthlyApiCalls, IsActive, ExpectedVersion);
}

/// <summary>Validates plan assignment, quota overrides, concurrency and the required audit reason.</summary>
public sealed record AssignTenantSubscriptionRequest(
    [param: Range(1, long.MaxValue)] long SubscriptionPlanId,
    [param: Required, StringLength(20)] string BillingCycle,
    [param: Required, StringLength(30)] string Status,
    DateTime StartsUtc,
    DateTime? EndsUtc,
    bool AutoRenew,
    [param: Range(1, int.MaxValue)] int? MaxStores,
    [param: Range(1, int.MaxValue)] int? MaxUsers,
    [param: Range(1, int.MaxValue)] int? MaxCameras,
    [param: Range(1, long.MaxValue)] long? MaxMonthlyRecognitions,
    [param: Range(1, long.MaxValue)] long? MaxMonthlyApiCalls,
    [param: Required] string ExpectedVersion,
    [param: Required, StringLength(500, MinimumLength = 3)] string AuditReason)
{
    public AssignTenantSubscriptionCommand ToCommand() => new(
        SubscriptionPlanId, BillingCycle, Status, StartsUtc, EndsUtc, AutoRenew,
        MaxStores, MaxUsers, MaxCameras, MaxMonthlyRecognitions, MaxMonthlyApiCalls,
        ExpectedVersion, AuditReason);
}
