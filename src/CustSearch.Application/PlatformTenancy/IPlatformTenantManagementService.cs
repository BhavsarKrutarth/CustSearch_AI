namespace CustSearch.Application.PlatformTenancy;

/// <summary>
/// Defines permission-protected platform tenant operations implemented against authoritative server data.
/// </summary>
public interface IPlatformTenantManagementService
{
    Task<PlatformDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<PageResult<PlatformTenantListItem>> ListTenantsAsync(PlatformTenantQuery query, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail?> GetTenantAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail> CreateTenantAsync(CreatePlatformTenantCommand command, PlatformAuditContext audit, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail> UpdateTenantAsync(long tenantId, UpdatePlatformTenantCommand command, PlatformAuditContext audit, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail> ActivateTenantAsync(long tenantId, string expectedVersion, PlatformAuditContext audit, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail> SuspendTenantAsync(long tenantId, string reason, string expectedVersion, PlatformAuditContext audit, CancellationToken cancellationToken = default);
    Task<PlatformTenantSummary?> GetTenantSummaryAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformTenantUsageItem>> GetTenantUsageAsync(long tenantId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<PageResult<PlatformAuditItem>> GetTenantAuditAsync(long tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionPlanView>> ListPlansAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlanView> CreatePlanAsync(SaveSubscriptionPlanCommand command, PlatformAuditContext audit, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanView> UpdatePlanAsync(long planId, SaveSubscriptionPlanCommand command, PlatformAuditContext audit, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail> AssignSubscriptionAsync(long tenantId, AssignTenantSubscriptionCommand command, PlatformAuditContext audit, CancellationToken cancellationToken = default);
}
