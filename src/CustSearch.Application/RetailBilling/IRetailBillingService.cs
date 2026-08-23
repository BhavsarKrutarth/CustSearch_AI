using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;

namespace CustSearch.Application.RetailBilling;

public interface IRetailBillingService
{
    Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductSearchQuery query,CancellationToken cancellationToken=default);
    Task<ProductDetail> GetProductAsync(long productId,CancellationToken cancellationToken=default);
    Task<ProductDetail> CreateProductAsync(CreateProductCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<ProductDetail> UpdateProductAsync(long productId,UpdateProductCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<ProductDetail> SetProductStoresAsync(long productId,SetProductStoresCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);

    Task<PagedResult<RetailInvoiceListItem>> SearchInvoicesAsync(RetailInvoiceSearchQuery query,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> GetInvoiceAsync(long invoiceId,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> CreateInvoiceAsync(CreateRetailInvoiceCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> UpdateInvoiceAsync(long invoiceId,UpdateRetailInvoiceCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> FinalizeInvoiceAsync(long invoiceId,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> CancelInvoiceAsync(long invoiceId,string reason,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> AddPaymentAsync(long invoiceId,AddRetailPaymentCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> SaveParticipantAsync(long invoiceId,SaveRetailParticipantCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<RetailInvoiceDetail> SaveAttributionAsync(long invoiceId,SaveRetailAttributionCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);

    Task<CustomerPurchaseHistory> GetCustomerPurchaseHistoryAsync(long customerId,int recentCount=10,CancellationToken cancellationToken=default);
    Task<HouseholdPurchaseSummary> GetHouseholdPurchaseSummaryAsync(long householdId,CancellationToken cancellationToken=default);
    Task<RetailSalesSummary> GetSalesSummaryAsync(long? storeId,DateTime? fromUtc,DateTime? toUtc,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailBreakdownItem>> GetSalesByProductAsync(long? storeId,DateTime? fromUtc,DateTime? toUtc,int top=20,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailBreakdownItem>> GetSalesByCategoryAsync(long? storeId,DateTime? fromUtc,DateTime? toUtc,int top=20,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailPaymentSummaryItem>> GetPaymentSummaryAsync(long? storeId,DateTime? fromUtc,DateTime? toUtc,CancellationToken cancellationToken=default);
}
