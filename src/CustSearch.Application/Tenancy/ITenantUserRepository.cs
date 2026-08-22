using CustSearch.Domain.Entities;

namespace CustSearch.Application.Tenancy;

/// <summary>
/// Reads tenant users only when both user and verified tenant identifiers match.
/// </summary>
public interface ITenantUserRepository
{
    Task<UserAccount?> GetByIdAsync(long tenantId, long userId, CancellationToken cancellationToken = default);
}
