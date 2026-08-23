using CustSearch.Application.TenantOperations;

namespace CustSearch.Application.ShopperCustomers;

/// <summary>
/// Phase 6 tenant application boundary for shopper customers and anonymous visitors. TenantId is never accepted from
/// the browser; implementations resolve it from the authenticated server context.
/// </summary>
public interface IShopperCustomerService
{
    Task<PagedResult<CustomerListItem>> SearchCustomersAsync(CustomerSearchQuery query, CancellationToken cancellationToken = default);
    Task<CustomerDetail> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default);
    Task<CustomerSmartProfile> GetSmartProfileAsync(long customerId, CancellationToken cancellationToken = default);
    Task<CustomerDetail> CreateCustomerAsync(CreateCustomerCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<CustomerDetail> UpdateCustomerAsync(long customerId, UpdateCustomerCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<CustomerDetail> SetCustomerStoresAsync(long customerId, SetCustomerStoresCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);

    Task<PagedResult<AnonymousVisitorListItem>> SearchVisitorsAsync(AnonymousVisitorSearchQuery query, CancellationToken cancellationToken = default);
    Task<AnonymousVisitorDetail> GetVisitorAsync(long visitorId, CancellationToken cancellationToken = default);
    Task<AnonymousVisitorDetail> CreateVisitorAsync(CreateAnonymousVisitorCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<AnonymousVisitorDetail> TouchVisitorAsync(long visitorId, TouchAnonymousVisitorCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<CustomerDetail> ConvertVisitorAsync(long visitorId, ConvertAnonymousVisitorCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
}
