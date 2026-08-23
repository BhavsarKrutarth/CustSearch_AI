using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.TenantOperations;

/// <summary>Phase 5 repository with tenant predicates in SQL, never in-memory post filtering.</summary>
public sealed class TenantOperationsRepository(CustSearchDbContext db) : ITenantOperationsRepository
{
    public Task<Tenant?> GetTenantAsync(long tenantId, CancellationToken cancellationToken = default) =>
        db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken);

    public Task<UserAccount?> GetUserAsync(long tenantId, long userId, bool tracked, CancellationToken cancellationToken = default)
    {
        var q = db.UserAccounts.Where(x => x.TenantId == tenantId && x.Scope == UserScope.Tenant && x.Id == userId);
        return (tracked ? q : q.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Store?> GetStoreAsync(long tenantId, long storeId, bool tracked, CancellationToken cancellationToken = default)
    {
        var q = db.Stores.Where(x => x.TenantId == tenantId && x.Id == storeId);
        return (tracked ? q : q.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<StaffProfile?> GetStaffAsync(long tenantId, long staffId, bool tracked, CancellationToken cancellationToken = default)
    {
        var q = db.StaffProfiles.Where(x => x.TenantId == tenantId && x.Id == staffId);
        return (tracked ? q : q.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListStoresAsync(long tenantId, CancellationToken cancellationToken = default) =>
        await db.Stores.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.StoreName).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<StaffProfile>> ListStaffAsync(long tenantId, CancellationToken cancellationToken = default) =>
        await db.StaffProfiles.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountActiveStoresAsync(long tenantId, CancellationToken cancellationToken = default) =>
        db.Stores.CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

    public Task<int> CountActiveUsersAsync(long tenantId, CancellationToken cancellationToken = default) =>
        db.UserAccounts.CountAsync(x => x.TenantId == tenantId && x.Scope == UserScope.Tenant && x.IsActive, cancellationToken);
}
