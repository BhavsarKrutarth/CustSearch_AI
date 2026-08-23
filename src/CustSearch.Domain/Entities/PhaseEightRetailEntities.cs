using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Phase 8A tenant-owned product catalog item. Historical invoices keep their own snapshots.</summary>
public sealed class Product
{
    private Product() { }
    private Product(long tenantId,string sku,string? barcode,string name,string? description,long categoryId,string? brand,string unitName,decimal salePrice,decimal? costPrice,decimal? taxPercent,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId);
        TenantId=tenantId; ProductCode=Require(sku,nameof(sku),50).ToUpperInvariant(); Barcode=Optional(barcode,nameof(barcode),100);
        Name=Require(name,nameof(name),200); Description=Optional(description,nameof(description),1000); CategoryId=categoryId; Brand=Optional(brand,nameof(brand),150); UnitName=Require(unitName,nameof(unitName),50);
        SalePrice=Money(salePrice,nameof(salePrice)); CostPrice=costPrice.HasValue?Money(costPrice.Value,nameof(costPrice)):null; TaxPercent=Percent(taxPercent,nameof(taxPercent));
        IsActive=true; CreatedUtc=RequireUtc(utcNow,nameof(utcNow)); UpdatedUtc=CreatedUtc;
    }
    public long Id{get;private set;} public long TenantId{get;private set;} public Tenant Tenant{get;private set;}=null!;
    public string ProductCode{get;private set;}=string.Empty; public string? Barcode{get;private set;} public string Name{get;private set;}=string.Empty; public string? Description{get;private set;}
    public long CategoryId{get;private set;} public ProductCategory Category{get;private set;}=null!; public string? Brand{get;private set;} public string UnitName{get;private set;}=string.Empty;
    public decimal SalePrice{get;private set;} public decimal? CostPrice{get;private set;} public decimal? TaxPercent{get;private set;} public bool IsActive{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;}
    public static Product Create(long tenantId,string sku,string? barcode,string name,string? description,long categoryId,string? brand,string unitName,decimal salePrice,decimal? costPrice,decimal? taxPercent,DateTime utcNow)=>new(tenantId,sku,barcode,name,description,categoryId,brand,unitName,salePrice,costPrice,taxPercent,utcNow);
    public void Update(string? barcode,string name,string? description,long categoryId,string? brand,string unitName,decimal salePrice,decimal? costPrice,decimal? taxPercent,bool isActive,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId); Barcode=Optional(barcode,nameof(barcode),100); Name=Require(name,nameof(name),200); Description=Optional(description,nameof(description),1000); CategoryId=categoryId; Brand=Optional(brand,nameof(brand),150); UnitName=Require(unitName,nameof(unitName),50);
        SalePrice=Money(salePrice,nameof(salePrice)); CostPrice=costPrice.HasValue?Money(costPrice.Value,nameof(costPrice)):null; TaxPercent=Percent(taxPercent,nameof(taxPercent)); IsActive=isActive; UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));
    }
    private static string Require(string value,string name,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value,name);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(name);}
    private static string? Optional(string? value,string name,int max)=>string.IsNullOrWhiteSpace(value)?null:Require(value,name,max);
    private static decimal Money(decimal value,string name){ArgumentOutOfRangeException.ThrowIfNegative(value,name);return decimal.Round(value,2,MidpointRounding.AwayFromZero);}
    private static decimal? Percent(decimal? value,string name){if(!value.HasValue)return null;ArgumentOutOfRangeException.ThrowIfNegative(value.Value,name);ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Value,100m,name);return decimal.Round(value.Value,4,MidpointRounding.AwayFromZero);}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 8A explicit product availability in one tenant store.</summary>
public sealed class ProductStoreAvailability
{
    private ProductStoreAvailability() { }
    private ProductStoreAvailability(long tenantId,long productId,long storeId,bool isActive,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);TenantId=tenantId;ProductId=productId;StoreId=storeId;IsActive=isActive;CreatedUtc=RequireUtc(utcNow,nameof(utcNow));UpdatedUtc=CreatedUtc;}
    public long TenantId{get;private set;} public long ProductId{get;private set;} public Product Product{get;private set;}=null!; public long StoreId{get;private set;} public Store Store{get;private set;}=null!; public bool IsActive{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;}
    public static ProductStoreAvailability Create(long tenantId,long productId,long storeId,bool isActive,DateTime utcNow)=>new(tenantId,productId,storeId,isActive,utcNow);
    public void SetActive(bool active,DateTime utcNow){IsActive=active;UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 8B factual retail invoice. Browser totals are never authoritative.</summary>
public sealed class RetailInvoice
{
    private RetailInvoice() { }
    private RetailInvoice(long tenantId,long storeId,string invoiceNumber,long? customerId,long? householdId,long? customerVisitId,long? visitPartyId,long createdByUserId,DateTime invoiceUtc,string? notes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);
        if(customerId is<=0)throw new ArgumentOutOfRangeException(nameof(customerId));if(householdId is<=0)throw new ArgumentOutOfRangeException(nameof(householdId));if(customerVisitId is<=0)throw new ArgumentOutOfRangeException(nameof(customerVisitId));if(visitPartyId is<=0)throw new ArgumentOutOfRangeException(nameof(visitPartyId));
        TenantId=tenantId;StoreId=storeId;InvoiceNumber=Require(invoiceNumber,nameof(invoiceNumber),50).ToUpperInvariant();CustomerId=customerId;HouseholdId=householdId;CustomerVisitId=customerVisitId;VisitPartyId=visitPartyId;InvoiceUtc=RequireUtc(invoiceUtc,nameof(invoiceUtc));Notes=Optional(notes,nameof(notes),1000);CreatedByUserId=createdByUserId;
        Subtotal=0;DiscountAmount=0;TaxAmount=0;GrandTotal=0;PaidAmount=0;BalanceAmount=0;Status=RetailInvoiceStatus.Draft;CreatedUtc=InvoiceUtc;UpdatedUtc=InvoiceUtc;
    }
    public long Id{get;private set;} public long TenantId{get;private set;} public Tenant Tenant{get;private set;}=null!; public long StoreId{get;private set;} public Store Store{get;private set;}=null!;
    public string InvoiceNumber{get;private set;}=string.Empty; public long? CustomerId{get;private set;} public Customer? Customer{get;private set;} public long? HouseholdId{get;private set;} public Household? Household{get;private set;}
    public long? CustomerVisitId{get;private set;} public CustomerVisit? CustomerVisit{get;private set;} public long? VisitPartyId{get;private set;} public VisitParty? VisitParty{get;private set;} public DateTime InvoiceUtc{get;private set;}
    public decimal Subtotal{get;private set;} public decimal DiscountAmount{get;private set;} public decimal TaxAmount{get;private set;} public decimal GrandTotal{get;private set;} public decimal PaidAmount{get;private set;} public decimal BalanceAmount{get;private set;}
    public RetailInvoiceStatus Status{get;private set;} public string? Notes{get;private set;} public long CreatedByUserId{get;private set;} public UserAccount CreatedByUser{get;private set;}=null!; public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;}
    public DateTime? CancelledUtc{get;private set;} public long? CancelledByUserId{get;private set;} public UserAccount? CancelledByUser{get;private set;} public string? CancellationReason{get;private set;} public byte[] RowVersion{get;private set;}=[];
    public static RetailInvoice Create(long tenantId,long storeId,string invoiceNumber,long? customerId,long? householdId,long? customerVisitId,long? visitPartyId,long createdByUserId,DateTime invoiceUtc,string? notes)=>new(tenantId,storeId,invoiceNumber,customerId,householdId,customerVisitId,visitPartyId,createdByUserId,invoiceUtc,notes);
    public void SetCalculatedTotals(decimal subtotal,decimal discount,decimal tax,decimal grandTotal,DateTime utcNow)
    {
        EnsureEditable();subtotal=Money(subtotal,nameof(subtotal));discount=Money(discount,nameof(discount));tax=Money(tax,nameof(tax));grandTotal=Money(grandTotal,nameof(grandTotal));
        if(discount>subtotal)throw new ArgumentOutOfRangeException(nameof(discount));var expected=Money(subtotal-discount+tax,"expected");if(expected!=grandTotal)throw new ArgumentException("GrandTotal must equal Subtotal - DiscountAmount + TaxAmount.",nameof(grandTotal));
        Subtotal=subtotal;DiscountAmount=discount;TaxAmount=tax;GrandTotal=grandTotal;ApplyPaidAmount(PaidAmount,utcNow);
    }
    public void FinalizeInvoice(DateTime utcNow){if(Status!=RetailInvoiceStatus.Draft)throw new InvalidOperationException("Only draft invoices can be finalized.");Status=PaidAmount>=GrandTotal&&GrandTotal>0?RetailInvoiceStatus.Paid:PaidAmount>0?RetailInvoiceStatus.PartiallyPaid:RetailInvoiceStatus.Finalized;BalanceAmount=Money(Math.Max(0,GrandTotal-PaidAmount),nameof(BalanceAmount));UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));}
    public void ApplyPaidAmount(decimal paidAmount,DateTime utcNow)
    {
        paidAmount=Money(paidAmount,nameof(paidAmount));if(paidAmount>GrandTotal)throw new ArgumentOutOfRangeException(nameof(paidAmount),"Paid amount cannot exceed invoice grand total.");PaidAmount=paidAmount;BalanceAmount=Money(GrandTotal-PaidAmount,nameof(BalanceAmount));
        if(Status!=RetailInvoiceStatus.Draft&&Status!=RetailInvoiceStatus.Cancelled)Status=PaidAmount==0?RetailInvoiceStatus.Finalized:BalanceAmount==0?RetailInvoiceStatus.Paid:RetailInvoiceStatus.PartiallyPaid;UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));
    }
    public void UpdateDraftLinks(long? customerId,long? householdId,long? customerVisitId,long? visitPartyId,string? notes,DateTime utcNow){EnsureEditable();if(customerId is<=0)throw new ArgumentOutOfRangeException(nameof(customerId));if(householdId is<=0)throw new ArgumentOutOfRangeException(nameof(householdId));if(customerVisitId is<=0)throw new ArgumentOutOfRangeException(nameof(customerVisitId));if(visitPartyId is<=0)throw new ArgumentOutOfRangeException(nameof(visitPartyId));CustomerId=customerId;HouseholdId=householdId;CustomerVisitId=customerVisitId;VisitPartyId=visitPartyId;Notes=Optional(notes,nameof(notes),1000);UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));}
    public void Cancel(long actorUserId,string reason,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actorUserId);if(Status==RetailInvoiceStatus.Cancelled)throw new InvalidOperationException("Invoice is already cancelled.");if(PaidAmount>0)throw new InvalidOperationException("Paid invoice must be refunded/voided before cancellation.");CancellationReason=Require(reason,nameof(reason),500);CancelledByUserId=actorUserId;CancelledUtc=RequireUtc(utcNow,nameof(utcNow));Status=RetailInvoiceStatus.Cancelled;UpdatedUtc=CancelledUtc.Value;}
    private void EnsureEditable(){if(Status!=RetailInvoiceStatus.Draft)throw new InvalidOperationException("Only draft invoices can be edited.");}
    private static string Require(string value,string name,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value,name);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(name);}
    private static string? Optional(string? value,string name,int max)=>string.IsNullOrWhiteSpace(value)?null:Require(value,name,max);
    private static decimal Money(decimal value,string name){ArgumentOutOfRangeException.ThrowIfNegative(value,name);return decimal.Round(value,2,MidpointRounding.AwayFromZero);}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 8C immutable financial line snapshot. Catalog changes never rewrite this history.</summary>
public sealed class RetailInvoiceItem
{
    private RetailInvoiceItem() { }
    private RetailInvoiceItem(long tenantId,long invoiceId,long? productId,long? categoryId,string productCodeSnapshot,string productNameSnapshot,string? categoryNameSnapshot,decimal quantity,decimal unitPrice,decimal discountAmount,decimal taxPercent,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(invoiceId);if(productId is<=0)throw new ArgumentOutOfRangeException(nameof(productId));if(categoryId is<=0)throw new ArgumentOutOfRangeException(nameof(categoryId));ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        TenantId=tenantId;InvoiceId=invoiceId;ProductId=productId;CategoryId=categoryId;ProductCodeSnapshot=Require(productCodeSnapshot,nameof(productCodeSnapshot),50);ProductNameSnapshot=Require(productNameSnapshot,nameof(productNameSnapshot),200);CategoryNameSnapshot=Optional(categoryNameSnapshot,nameof(categoryNameSnapshot),150);Quantity=decimal.Round(quantity,4,MidpointRounding.AwayFromZero);UnitPrice=Money(unitPrice,nameof(unitPrice));DiscountAmount=Money(discountAmount,nameof(discountAmount));TaxPercent=Percent(taxPercent,nameof(taxPercent));
        LineSubtotal=Money(Quantity*UnitPrice,nameof(LineSubtotal));if(DiscountAmount>LineSubtotal)throw new ArgumentOutOfRangeException(nameof(discountAmount));TaxAmount=Money((LineSubtotal-DiscountAmount)*TaxPercent/100m,nameof(TaxAmount));LineTotal=Money(LineSubtotal-DiscountAmount+TaxAmount,nameof(LineTotal));CreatedUtc=RequireUtc(utcNow,nameof(utcNow));
    }
    public long Id{get;private set;} public long TenantId{get;private set;} public long InvoiceId{get;private set;} public RetailInvoice Invoice{get;private set;}=null!; public long? ProductId{get;private set;} public Product? Product{get;private set;} public long? CategoryId{get;private set;} public ProductCategory? Category{get;private set;}
    public string ProductCodeSnapshot{get;private set;}=string.Empty; public string ProductNameSnapshot{get;private set;}=string.Empty; public string? CategoryNameSnapshot{get;private set;} public decimal Quantity{get;private set;} public decimal UnitPrice{get;private set;} public decimal DiscountAmount{get;private set;} public decimal TaxPercent{get;private set;} public decimal TaxAmount{get;private set;} public decimal LineSubtotal{get;private set;} public decimal LineTotal{get;private set;} public DateTime CreatedUtc{get;private set;}
    public static RetailInvoiceItem Create(long tenantId,long invoiceId,long? productId,long? categoryId,string productCodeSnapshot,string productNameSnapshot,string? categoryNameSnapshot,decimal quantity,decimal unitPrice,decimal discountAmount,decimal taxPercent,DateTime utcNow)=>new(tenantId,invoiceId,productId,categoryId,productCodeSnapshot,productNameSnapshot,categoryNameSnapshot,quantity,unitPrice,discountAmount,taxPercent,utcNow);
    private static string Require(string value,string name,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value,name);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(name);}
    private static string? Optional(string? value,string name,int max)=>string.IsNullOrWhiteSpace(value)?null:Require(value,name,max);
    private static decimal Money(decimal value,string name){ArgumentOutOfRangeException.ThrowIfNegative(value,name);return decimal.Round(value,2,MidpointRounding.AwayFromZero);}
    private static decimal Percent(decimal value,string name){ArgumentOutOfRangeException.ThrowIfNegative(value,name);ArgumentOutOfRangeException.ThrowIfGreaterThan(value,100m,name);return decimal.Round(value,4,MidpointRounding.AwayFromZero);}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 8D append-only retail payment fact. Successful rows are the only source of PaidAmount.</summary>
public sealed class RetailInvoicePayment
{
    private RetailInvoicePayment() { }
    private RetailInvoicePayment(long tenantId,long storeId,long invoiceId,string paymentReference,RetailPaymentMethod method,decimal amount,DateTime paymentUtc,RetailPaymentStatus status,string? externalTransactionId,string? notes,long receivedByUserId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(invoiceId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receivedByUserId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);if(!Enum.IsDefined(method))throw new ArgumentOutOfRangeException(nameof(method));if(!Enum.IsDefined(status))throw new ArgumentOutOfRangeException(nameof(status));
        TenantId=tenantId;StoreId=storeId;InvoiceId=invoiceId;PaymentReference=Require(paymentReference,nameof(paymentReference),100);PaymentMethod=method;Amount=decimal.Round(amount,2,MidpointRounding.AwayFromZero);PaymentUtc=RequireUtc(paymentUtc,nameof(paymentUtc));Status=status;ExternalTransactionId=Optional(externalTransactionId,nameof(externalTransactionId),150);Notes=Optional(notes,nameof(notes),500);ReceivedByUserId=receivedByUserId;CreatedUtc=PaymentUtc;
    }
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public RetailInvoice Invoice{get;private set;}=null!; public long InvoiceId{get;private set;} public string PaymentReference{get;private set;}=string.Empty; public RetailPaymentMethod PaymentMethod{get;private set;} public decimal Amount{get;private set;} public DateTime PaymentUtc{get;private set;} public RetailPaymentStatus Status{get;private set;} public string? ExternalTransactionId{get;private set;} public string? Notes{get;private set;} public long ReceivedByUserId{get;private set;} public UserAccount ReceivedByUser{get;private set;}=null!; public DateTime CreatedUtc{get;private set;}
    public static RetailInvoicePayment Create(long tenantId,long storeId,long invoiceId,string paymentReference,RetailPaymentMethod method,decimal amount,DateTime paymentUtc,RetailPaymentStatus status,string? externalTransactionId,string? notes,long receivedByUserId)=>new(tenantId,storeId,invoiceId,paymentReference,method,amount,paymentUtc,status,externalTransactionId,notes,receivedByUserId);
    private static string Require(string value,string name,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value,name);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(name);}
    private static string? Optional(string? value,string name,int max)=>string.IsNullOrWhiteSpace(value)?null:Require(value,name,max);
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 8E explicit known-customer participation. VisitParty membership never creates this automatically.</summary>
public sealed class RetailInvoiceParticipant
{
    private RetailInvoiceParticipant() { }
    private RetailInvoiceParticipant(long tenantId,long invoiceId,long customerId,RetailParticipationType participationType,bool isPayer,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(invoiceId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);if(!Enum.IsDefined(participationType))throw new ArgumentOutOfRangeException(nameof(participationType));if((participationType==RetailParticipationType.Payer)!=isPayer)throw new ArgumentException("Payer participation type and IsPayer must agree.");TenantId=tenantId;InvoiceId=invoiceId;CustomerId=customerId;ParticipationType=participationType;IsPayer=isPayer;CreatedUtc=RequireUtc(utcNow,nameof(utcNow));}
    public long TenantId{get;private set;} public long InvoiceId{get;private set;} public RetailInvoice Invoice{get;private set;}=null!; public long CustomerId{get;private set;} public Customer Customer{get;private set;}=null!; public RetailParticipationType ParticipationType{get;private set;} public bool IsPayer{get;private set;} public DateTime CreatedUtc{get;private set;}
    public static RetailInvoiceParticipant Create(long tenantId,long invoiceId,long customerId,RetailParticipationType participationType,bool isPayer,DateTime utcNow)=>new(tenantId,invoiceId,customerId,participationType,isPayer,utcNow);
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 8F explicit, auditable invoice-item spend attribution. No face/proximity-derived attribution exists.</summary>
public sealed class RetailInvoiceItemAttribution
{
    private RetailInvoiceItemAttribution() { }
    private RetailInvoiceItemAttribution(long tenantId,long invoiceId,long invoiceItemId,long customerId,RetailAttributionType attributionType,decimal? quantityAttributed,decimal amountAttributed,RetailAttributionSource source,long createdByUserId,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(invoiceId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(invoiceItemId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);if(!Enum.IsDefined(attributionType))throw new ArgumentOutOfRangeException(nameof(attributionType));if(!Enum.IsDefined(source))throw new ArgumentOutOfRangeException(nameof(source));if(quantityAttributed.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityAttributed.Value);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amountAttributed);
        TenantId=tenantId;InvoiceId=invoiceId;InvoiceItemId=invoiceItemId;CustomerId=customerId;AttributionType=attributionType;QuantityAttributed=quantityAttributed.HasValue?decimal.Round(quantityAttributed.Value,4,MidpointRounding.AwayFromZero):null;AmountAttributed=decimal.Round(amountAttributed,2,MidpointRounding.AwayFromZero);Source=source;CreatedByUserId=createdByUserId;CreatedUtc=RequireUtc(utcNow,nameof(utcNow));
    }
    public long Id{get;private set;} public long TenantId{get;private set;} public long InvoiceId{get;private set;} public RetailInvoice Invoice{get;private set;}=null!; public long InvoiceItemId{get;private set;} public RetailInvoiceItem InvoiceItem{get;private set;}=null!; public long CustomerId{get;private set;} public Customer Customer{get;private set;}=null!; public RetailAttributionType AttributionType{get;private set;} public decimal? QuantityAttributed{get;private set;} public decimal AmountAttributed{get;private set;} public RetailAttributionSource Source{get;private set;} public long CreatedByUserId{get;private set;} public UserAccount CreatedByUser{get;private set;}=null!; public DateTime CreatedUtc{get;private set;}
    public static RetailInvoiceItemAttribution Create(long tenantId,long invoiceId,long invoiceItemId,long customerId,RetailAttributionType attributionType,decimal? quantityAttributed,decimal amountAttributed,RetailAttributionSource source,long createdByUserId,DateTime utcNow)=>new(tenantId,invoiceId,invoiceItemId,customerId,attributionType,quantityAttributed,amountAttributed,source,createdByUserId,utcNow);
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}
