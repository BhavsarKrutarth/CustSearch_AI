namespace CustSearch.Application.ShopperCustomers;

/// <summary>
/// Phase 6C Dapper search boundary. Every call requires server-resolved TenantId and the caller's allowed StoreIds;
/// implementations must not perform cross-tenant search and then filter in memory.
/// </summary>
public interface IShopperCustomerRepository
{
    Task<IReadOnlyList<CustomerSearchRow>> SearchCustomersAsync(long tenantId, IReadOnlySet<long> allowedStoreIds,
        bool tenantWide, CustomerSearchQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnonymousVisitorSearchRow>> SearchVisitorsAsync(long tenantId, IReadOnlySet<long> allowedStoreIds,
        bool tenantWide, AnonymousVisitorSearchQuery query, CancellationToken cancellationToken = default);
}
