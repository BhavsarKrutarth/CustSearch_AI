using CustSearch.Application.Tenancy;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.Tenancy;

/// <summary>
/// Enforces tenant scope in the SQL predicate rather than filtering results in memory.
/// </summary>
public sealed class TenantUserRepository(CustSearchDbContext dbContext) : ITenantUserRepository
{
    public Task<UserAccount?> GetByIdAsync(
        long tenantId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        return dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == userId && user.TenantId == tenantId && user.Scope == UserScope.Tenant,
                cancellationToken);
    }
}
