using CustSearch.Application.ShopperCustomers;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.RetailBilling;

public sealed record ProductSearchQuery(int PageNumber=1,int PageSize=25,string? Search=null,long? StoreId=null,long? CategoryId=null,bool ActiveOnly=false);
public sealed record ProductListItem(long Id,string ProductCode,string? Barcode,string Name,long CategoryId,string CategoryName,string? Brand,string UnitName,decimal SalePrice,decimal? TaxPercent,bool IsActive);
public sealed record ProductStoreView(long StoreId,bool IsActive);
public sealed record ProductDetail(long Id,string ProductCode,string? Barcode,string Name,string? Description,long CategoryId,string CategoryName,string? Brand,string UnitName,decimal SalePrice,decimal? CostPrice,decimal? TaxPercent,bool IsActive,IReadOnlyList<ProductStoreView> Stores,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record CreateProductCommand(string ProductCode,string? Barcode,string Name,string? Description,long CategoryId,string? Brand,string UnitName,decimal SalePrice,decimal? CostPrice,decimal? TaxPercent,IReadOnlyList<long>? StoreIds);
public sealed record UpdateProductCommand(string? Barcode,string Name,string? Description,long CategoryId,string? Brand,string UnitName,decimal SalePrice,decimal? CostPrice,decimal? TaxPercent,bool IsActive);
public sealed record SetProductStoresCommand(IReadOnlyList<long> StoreIds);

public sealed record RetailInvoiceSearchQuery(int PageNumber=1,int PageSize=25,string? Search=null,long? StoreId=null,long? CustomerId=null,RetailInvoiceStatus? Status=null,DateTime? FromUtc=null,DateTime? ToUtc=null);
public sealed record RetailInvoiceListItem(long Id,string InvoiceNumber,long StoreId,long? CustomerId,string? CustomerCode,string? CustomerName,DateTime InvoiceUtc,decimal GrandTotal,decimal PaidAmount,decimal BalanceAmount,RetailInvoiceStatus Status);
public sealed record RetailInvoiceItemView(long Id,long? ProductId,string ProductCode,string ProductName,long? CategoryId,string? CategoryName,decimal Quantity,decimal UnitPrice,decimal DiscountAmount,decimal TaxPercent,decimal TaxAmount,decimal LineSubtotal,decimal LineTotal);
public sealed record RetailPaymentView(long Id,string PaymentReference,RetailPaymentMethod PaymentMethod,decimal Amount,DateTime PaymentUtc,RetailPaymentStatus Status,string? ExternalTransactionId,string? Notes,long ReceivedByUserId);
public sealed record RetailParticipantView(long CustomerId,string CustomerCode,string CustomerName,RetailParticipationType ParticipationType,bool IsPayer);
public sealed record RetailAttributionView(long Id,long InvoiceItemId,long CustomerId,string CustomerCode,string CustomerName,RetailAttributionType AttributionType,decimal? QuantityAttributed,decimal AmountAttributed,RetailAttributionSource Source,long CreatedByUserId,DateTime CreatedUtc);
public sealed record RetailInvoiceDetail(long Id,string InvoiceNumber,long StoreId,long? CustomerId,long? HouseholdId,long? CustomerVisitId,long? VisitPartyId,DateTime InvoiceUtc,decimal Subtotal,decimal DiscountAmount,decimal TaxAmount,decimal GrandTotal,decimal PaidAmount,decimal BalanceAmount,RetailInvoiceStatus Status,string? Notes,IReadOnlyList<RetailInvoiceItemView> Items,IReadOnlyList<RetailPaymentView> Payments,IReadOnlyList<RetailParticipantView> Participants,IReadOnlyList<RetailAttributionView> Attributions,decimal AttributedTotal,decimal UnattributedTotal,DateTime CreatedUtc,DateTime UpdatedUtc,DateTime? CancelledUtc,string? CancellationReason);

public sealed record RetailInvoiceItemInput(long ProductId,decimal Quantity,decimal DiscountAmount=0m);
public sealed record CreateRetailInvoiceCommand(long StoreId,long? CustomerId,long? HouseholdId,long? CustomerVisitId,long? VisitPartyId,string? Notes,IReadOnlyList<RetailInvoiceItemInput> Items);
public sealed record UpdateRetailInvoiceCommand(long? CustomerId,long? HouseholdId,long? CustomerVisitId,long? VisitPartyId,string? Notes,IReadOnlyList<RetailInvoiceItemInput> Items);
public sealed record AddRetailPaymentCommand(string? PaymentReference,RetailPaymentMethod PaymentMethod,decimal Amount,DateTime? PaymentUtc,string? ExternalTransactionId,string? Notes);
public sealed record SaveRetailParticipantCommand(long CustomerId,RetailParticipationType ParticipationType,bool IsPayer);
public sealed record SaveRetailAttributionCommand(long InvoiceItemId,long CustomerId,RetailAttributionType AttributionType,decimal? QuantityAttributed,decimal AmountAttributed,RetailAttributionSource Source);

public sealed record CustomerPurchaseHistoryItem(long InvoiceId,string InvoiceNumber,long StoreId,DateTime InvoiceUtc,RetailInvoiceStatus Status,decimal GrandTotal,decimal PayerAmount,decimal AttributedAmount);
public sealed record CustomerPurchaseHistory(long CustomerId,long InvoiceCount,decimal PayerSpend,decimal ExplicitAttributedSpend,DateTime? LastPurchaseUtc,long? LastPurchaseStoreId,IReadOnlyList<CustomerPurchaseHistoryItem> RecentInvoices);
public sealed record HouseholdPurchaseSummary(long HouseholdId,long InvoiceCount,decimal VerifiedMemberAttributedSpend,DateTime? LastPurchaseUtc);
public sealed record RetailSalesSummary(decimal GrossSales,decimal Discounts,decimal Tax,decimal NetSales,decimal PaidAmount,decimal OutstandingAmount,long InvoiceCount);
public sealed record RetailBreakdownItem(long Id,string Code,string Name,decimal NetSales,long InvoiceCount);
public sealed record RetailPaymentSummaryItem(RetailPaymentMethod PaymentMethod,decimal Amount,long PaymentCount);

public sealed record ProductSearchRow(long Id,string ProductCode,string? Barcode,string Name,long CategoryId,string CategoryName,string? Brand,string UnitName,decimal SalePrice,decimal? TaxPercent,bool IsActive,long TotalCount);
public sealed record RetailInvoiceSearchRow(long Id,string InvoiceNumber,long StoreId,long? CustomerId,string? CustomerCode,string? CustomerName,DateTime InvoiceUtc,decimal GrandTotal,decimal PaidAmount,decimal BalanceAmount,RetailInvoiceStatus Status,long TotalCount);

public static class PhaseEightPaging
{
    public static PagedResult<T> Empty<T>(int pageNumber,int pageSize)=>new([],pageNumber,pageSize,0);
}
