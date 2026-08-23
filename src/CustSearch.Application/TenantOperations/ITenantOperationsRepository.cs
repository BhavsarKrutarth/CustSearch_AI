using CustSearch.Domain.Entities;

namespace CustSearch.Application.TenantOperations;

/// <summary>
/// Phase 5 repository boundary. Every lookup requires the server-resolved TenantId so cross-tenant rows cannot be returned accidentally.
/// </summary>
public interface ITenantOperationsRepository
{
    Task<Tenant?> GetTenantAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetUserAsync(long tenantId, long userId, bool tracked, CancellationToken cancellationToken = default);
    Task<Store?> GetStoreAsync(long tenantId, long storeId, bool tracked, CancellationToken cancellationToken = default);
    Task<StaffProfile?> GetStaffAsync(long tenantId, long staffId, bool tracked, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Store>> ListStoresAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffProfile>> ListStaffAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<int> CountActiveStoresAsync(long tenantId, CancellationToken cancellationToken = default);
    Task<int> CountActiveUsersAsync(long tenantId, CancellationToken cancellationToken = default);
}
