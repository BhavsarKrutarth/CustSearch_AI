namespace CustSearch.Application.RetailBilling;

public interface IRetailBillingRepository
{
    Task<IReadOnlyList<ProductSearchRow>> SearchProductsAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,ProductSearchQuery query,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailInvoiceSearchRow>> SearchInvoicesAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,RetailInvoiceSearchQuery query,CancellationToken cancellationToken=default);
    Task<CustomerPurchaseHistory> GetCustomerPurchaseHistoryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long customerId,int recentCount,CancellationToken cancellationToken=default);
    Task<HouseholdPurchaseSummary> GetHouseholdPurchaseSummaryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long householdId,CancellationToken cancellationToken=default);
    Task<RetailSalesSummary> GetSalesSummaryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailBreakdownItem>> GetSalesByProductAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,int top,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailBreakdownItem>> GetSalesByCategoryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,int top,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<RetailPaymentSummaryItem>> GetPaymentSummaryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,CancellationToken cancellationToken=default);
}
