using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.RetailBilling;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Phase 8 tenant/store-scoped product and retail billing API. TenantId is never accepted from browser payloads.</summary>
[ApiController]
[Route("api/tenant")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
public sealed class RetailBillingController(IRetailBillingService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("products")]
    [HasPermission(PermissionCatalog.Operations.ProductsView)]
    public Task<PagedResult<ProductListItem>> SearchProducts([FromQuery]int pageNumber=1,[FromQuery]int pageSize=25,[FromQuery]string? search=null,[FromQuery]long? storeId=null,[FromQuery]long? categoryId=null,[FromQuery]bool activeOnly=false,CancellationToken ct=default)=>service.SearchProductsAsync(new(pageNumber,pageSize,search,storeId,categoryId,activeOnly),ct);

    [HttpGet("products/{productId:long}")]
    [HasPermission(PermissionCatalog.Operations.ProductsView)]
    public Task<ProductDetail> GetProduct(long productId,CancellationToken ct)=>service.GetProductAsync(productId,ct);

    [HttpPost("products")]
    [HasPermission(PermissionCatalog.Operations.ProductsCreate)]
    public Task<ProductDetail> CreateProduct(CreateProductRequest request,CancellationToken ct)=>service.CreateProductAsync(new(request.ProductCode,request.Barcode,request.Name,request.Description,request.CategoryId,request.Brand,request.UnitName,request.SalePrice,request.CostPrice,request.TaxPercent,request.StoreIds),Audit(),ct);

    [HttpPut("products/{productId:long}")]
    [HasPermission(PermissionCatalog.Operations.ProductsEdit)]
    public Task<ProductDetail> UpdateProduct(long productId,UpdateProductRequest request,CancellationToken ct)=>service.UpdateProductAsync(productId,new(request.Barcode,request.Name,request.Description,request.CategoryId,request.Brand,request.UnitName,request.SalePrice,request.CostPrice,request.TaxPercent,request.IsActive),Audit(),ct);

    [HttpPut("products/{productId:long}/stores")]
    [HasPermission(PermissionCatalog.Operations.ProductsManageStores)]
    public Task<ProductDetail> SetProductStores(long productId,SetProductStoresRequest request,CancellationToken ct)=>service.SetProductStoresAsync(productId,new(request.StoreIds),Audit(),ct);

    [HttpGet("retail/invoices")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesView)]
    public Task<PagedResult<RetailInvoiceListItem>> SearchInvoices([FromQuery]int pageNumber=1,[FromQuery]int pageSize=25,[FromQuery]string? search=null,[FromQuery]long? storeId=null,[FromQuery]long? customerId=null,[FromQuery]RetailInvoiceStatus? status=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,CancellationToken ct=default)=>service.SearchInvoicesAsync(new(pageNumber,pageSize,search,storeId,customerId,status,fromUtc,toUtc),ct);

    [HttpGet("retail/invoices/{invoiceId:long}")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesView)]
    public Task<RetailInvoiceDetail> GetInvoice(long invoiceId,CancellationToken ct)=>service.GetInvoiceAsync(invoiceId,ct);

    [HttpPost("retail/invoices")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesCreate)]
    public Task<RetailInvoiceDetail> CreateInvoice(CreateRetailInvoiceRequest request,CancellationToken ct)=>service.CreateInvoiceAsync(new(request.StoreId,request.CustomerId,request.HouseholdId,request.CustomerVisitId,request.VisitPartyId,request.Notes,request.Items.Select(Map).ToArray()),Audit(),ct);

    [HttpPut("retail/invoices/{invoiceId:long}")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesEdit)]
    public Task<RetailInvoiceDetail> UpdateInvoice(long invoiceId,UpdateRetailInvoiceRequest request,CancellationToken ct)=>service.UpdateInvoiceAsync(invoiceId,new(request.CustomerId,request.HouseholdId,request.CustomerVisitId,request.VisitPartyId,request.Notes,request.Items.Select(Map).ToArray()),Audit(),ct);

    [HttpPost("retail/invoices/{invoiceId:long}/finalize")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesFinalize)]
    public Task<RetailInvoiceDetail> FinalizeInvoice(long invoiceId,CancellationToken ct)=>service.FinalizeInvoiceAsync(invoiceId,Audit(),ct);

    [HttpPost("retail/invoices/{invoiceId:long}/cancel")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesCancel)]
    public Task<RetailInvoiceDetail> CancelInvoice(long invoiceId,CancelRetailInvoiceRequest request,CancellationToken ct)=>service.CancelInvoiceAsync(invoiceId,request.Reason,Audit(),ct);

    [HttpPost("retail/invoices/{invoiceId:long}/payments")]
    [HasPermission(PermissionCatalog.Operations.RetailPaymentsCreate)]
    public Task<RetailInvoiceDetail> AddPayment(long invoiceId,AddRetailPaymentRequest request,CancellationToken ct)=>service.AddPaymentAsync(invoiceId,new(request.PaymentReference,request.PaymentMethod,request.Amount,request.PaymentUtc,request.ExternalTransactionId,request.Notes),Audit(),ct);

    [HttpPost("retail/invoices/{invoiceId:long}/participants")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesEdit)]
    public Task<RetailInvoiceDetail> SaveParticipant(long invoiceId,SaveRetailParticipantRequest request,CancellationToken ct)=>service.SaveParticipantAsync(invoiceId,new(request.CustomerId,request.ParticipationType,request.IsPayer),Audit(),ct);

    [HttpPost("retail/invoices/{invoiceId:long}/attributions")]
    [HasPermission(PermissionCatalog.Operations.RetailSpendAttributionManage)]
    public Task<RetailInvoiceDetail> SaveAttribution(long invoiceId,SaveRetailAttributionRequest request,CancellationToken ct)=>service.SaveAttributionAsync(invoiceId,new(request.InvoiceItemId,request.CustomerId,request.AttributionType,request.QuantityAttributed,request.AmountAttributed,request.Source),Audit(),ct);

    [HttpGet("customers/{customerId:long}/purchase-history")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesView)]
    public Task<CustomerPurchaseHistory> CustomerPurchaseHistory(long customerId,[FromQuery]int recentCount=10,CancellationToken ct=default)=>service.GetCustomerPurchaseHistoryAsync(customerId,recentCount,ct);

    [HttpGet("households/{householdId:long}/purchase-summary")]
    [HasPermission(PermissionCatalog.Operations.RetailInvoicesView)]
    public Task<HouseholdPurchaseSummary> HouseholdPurchaseSummary(long householdId,CancellationToken ct)=>service.GetHouseholdPurchaseSummaryAsync(householdId,ct);

    [HttpGet("retail/reports/summary")]
    [HasPermission(PermissionCatalog.Operations.RetailReportsView)]
    public Task<RetailSalesSummary> SalesSummary([FromQuery]long? storeId=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,CancellationToken ct=default)=>service.GetSalesSummaryAsync(storeId,fromUtc,toUtc,ct);

    [HttpGet("retail/reports/products")]
    [HasPermission(PermissionCatalog.Operations.RetailReportsView)]
    public Task<IReadOnlyList<RetailBreakdownItem>> SalesByProduct([FromQuery]long? storeId=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,[FromQuery]int top=20,CancellationToken ct=default)=>service.GetSalesByProductAsync(storeId,fromUtc,toUtc,top,ct);

    [HttpGet("retail/reports/categories")]
    [HasPermission(PermissionCatalog.Operations.RetailReportsView)]
    public Task<IReadOnlyList<RetailBreakdownItem>> SalesByCategory([FromQuery]long? storeId=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,[FromQuery]int top=20,CancellationToken ct=default)=>service.GetSalesByCategoryAsync(storeId,fromUtc,toUtc,top,ct);

    [HttpGet("retail/reports/payments")]
    [HasPermission(PermissionCatalog.Operations.RetailReportsView)]
    public Task<IReadOnlyList<RetailPaymentSummaryItem>> PaymentSummary([FromQuery]long? storeId=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,CancellationToken ct=default)=>service.GetPaymentSummaryAsync(storeId,fromUtc,toUtc,ct);

    private static RetailInvoiceItemInput Map(RetailInvoiceItemRequest item)=>new(item.ProductId,item.Quantity,item.DiscountAmount);
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

public sealed record CreateProductRequest([param:Required,StringLength(50)]string ProductCode,[param:StringLength(100)]string? Barcode,[param:Required,StringLength(200)]string Name,[param:StringLength(1000)]string? Description,long CategoryId,[param:StringLength(150)]string? Brand,[param:Required,StringLength(50)]string UnitName,decimal SalePrice,decimal? CostPrice,decimal? TaxPercent,IReadOnlyList<long>? StoreIds);
public sealed record UpdateProductRequest([param:StringLength(100)]string? Barcode,[param:Required,StringLength(200)]string Name,[param:StringLength(1000)]string? Description,long CategoryId,[param:StringLength(150)]string? Brand,[param:Required,StringLength(50)]string UnitName,decimal SalePrice,decimal? CostPrice,decimal? TaxPercent,bool IsActive);
public sealed record SetProductStoresRequest([param:Required]IReadOnlyList<long> StoreIds);
public sealed record RetailInvoiceItemRequest(long ProductId,decimal Quantity,decimal DiscountAmount=0m);
public sealed record CreateRetailInvoiceRequest(long StoreId,long? CustomerId,long? HouseholdId,long? CustomerVisitId,long? VisitPartyId,[param:StringLength(1000)]string? Notes,[param:Required]IReadOnlyList<RetailInvoiceItemRequest> Items);
public sealed record UpdateRetailInvoiceRequest(long? CustomerId,long? HouseholdId,long? CustomerVisitId,long? VisitPartyId,[param:StringLength(1000)]string? Notes,[param:Required]IReadOnlyList<RetailInvoiceItemRequest> Items);
public sealed record CancelRetailInvoiceRequest([param:Required,StringLength(500)]string Reason);
public sealed record AddRetailPaymentRequest([param:StringLength(100)]string? PaymentReference,RetailPaymentMethod PaymentMethod,decimal Amount,DateTime? PaymentUtc,[param:StringLength(150)]string? ExternalTransactionId,[param:StringLength(500)]string? Notes);
public sealed record SaveRetailParticipantRequest(long CustomerId,RetailParticipationType ParticipationType,bool IsPayer);
public sealed record SaveRetailAttributionRequest(long InvoiceItemId,long CustomerId,RetailAttributionType AttributionType,decimal? QuantityAttributed,decimal AmountAttributed,RetailAttributionSource Source);
