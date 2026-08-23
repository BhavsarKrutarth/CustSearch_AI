using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>CustSearch-to-tenant invoice. Never represents a shop-customer purchase.</summary>
public sealed class PlatformInvoice
{
    private PlatformInvoice() { }

    private PlatformInvoice(
        long tenantId,
        long tenantSubscriptionId,
        string invoiceNumber,
        string currency,
        DateTime invoiceUtc,
        DateTime dueUtc,
        decimal subtotal,
        decimal discountAmount,
        decimal taxAmount,
        DateTime createdUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantSubscriptionId);
        ArgumentOutOfRangeException.ThrowIfNegative(subtotal);
        ArgumentOutOfRangeException.ThrowIfNegative(discountAmount);
        ArgumentOutOfRangeException.ThrowIfNegative(taxAmount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(discountAmount, subtotal + taxAmount);

        TenantId = tenantId;
        TenantSubscriptionId = tenantSubscriptionId;
        InvoiceNumber = Require(invoiceNumber, nameof(invoiceNumber), 60).ToUpperInvariant();
        Currency = Require(currency, nameof(currency), 3).ToUpperInvariant();
        InvoiceUtc = RequireUtc(invoiceUtc, nameof(invoiceUtc));
        DueUtc = RequireUtc(dueUtc, nameof(dueUtc));
        if (DueUtc < InvoiceUtc)
            throw new ArgumentException("Due date cannot precede invoice date.", nameof(dueUtc));

        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        Total = subtotal - discountAmount + taxAmount;
        PaidAmount = 0;
        Status = PlatformInvoiceStatus.Open;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
        RowVersion = NewRowVersion();
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long TenantSubscriptionId { get; private set; }
    public TenantSubscription TenantSubscription { get; private set; } = null!;
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public DateTime InvoiceUtc { get; private set; }
    public DateTime DueUtc { get; private set; }
    public PlatformInvoiceStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public decimal PaidAmount { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PlatformInvoice Create(
        long tenantId,
        long tenantSubscriptionId,
        string invoiceNumber,
        string currency,
        DateTime invoiceUtc,
        DateTime dueUtc,
        decimal subtotal,
        decimal discountAmount,
        decimal taxAmount,
        DateTime createdUtc) =>
        new(tenantId, tenantSubscriptionId, invoiceNumber, currency, invoiceUtc, dueUtc, subtotal, discountAmount, taxAmount, createdUtc);

    public void ApplySuccessfulPayment(decimal amount, DateTime updatedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (Status == PlatformInvoiceStatus.Void)
            throw new InvalidOperationException("A void platform invoice cannot receive payment.");
        if (PaidAmount + amount > Total)
            throw new InvalidOperationException("Platform payment exceeds invoice balance.");

        PaidAmount += amount;
        Status = PaidAmount == Total ? PlatformInvoiceStatus.Paid : PlatformInvoiceStatus.Open;
        Touch(updatedUtc);
    }

    public void MarkOverdue(DateTime updatedUtc)
    {
        if (Status == PlatformInvoiceStatus.Open)
            Status = PlatformInvoiceStatus.Overdue;
        Touch(updatedUtc);
    }

    public void Void(DateTime updatedUtc)
    {
        if (PaidAmount > 0)
            throw new InvalidOperationException("Paid platform invoices cannot be voided.");
        Status = PlatformInvoiceStatus.Void;
        Touch(updatedUtc);
    }

    private void Touch(DateTime utc)
    {
        UpdatedUtc = RequireUtc(utc, nameof(utc));
        RowVersion = NewRowVersion();
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new ArgumentException($"Value cannot exceed {max} characters.", name);
        return normalized;
    }

    private static DateTime RequireUtc(DateTime value, string name) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", name);

    private static byte[] NewRowVersion() => Guid.NewGuid().ToByteArray();
}

/// <summary>Immutable commercial snapshot line belonging only to PlatformInvoice.</summary>
public sealed class PlatformInvoiceItem
{
    private PlatformInvoiceItem() { }

    private PlatformInvoiceItem(
        long tenantId,
        long platformInvoiceId,
        long? subscriptionPlanId,
        string planName,
        string description,
        decimal quantity,
        decimal rate,
        decimal discountAmount,
        decimal taxAmount,
        DateTime createdUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformInvoiceId);
        if (subscriptionPlanId.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionPlanId.Value, nameof(subscriptionPlanId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(rate);
        ArgumentOutOfRangeException.ThrowIfNegative(discountAmount);
        ArgumentOutOfRangeException.ThrowIfNegative(taxAmount);

        TenantId = tenantId;
        PlatformInvoiceId = platformInvoiceId;
        SubscriptionPlanId = subscriptionPlanId;
        PlanName = Require(planName, nameof(planName), 150);
        Description = Optional(description, nameof(description), 500);
        Quantity = quantity;
        Rate = rate;
        Subtotal = decimal.Round(quantity * rate, 2, MidpointRounding.AwayFromZero);
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(discountAmount, Subtotal + taxAmount);
        Total = Subtotal - discountAmount + taxAmount;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public long PlatformInvoiceId { get; private set; }
    public PlatformInvoice PlatformInvoice { get; private set; } = null!;
    public long? SubscriptionPlanId { get; private set; }
    public string PlanName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Rate { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Total { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static PlatformInvoiceItem Create(
        long tenantId,
        long platformInvoiceId,
        long? subscriptionPlanId,
        string planName,
        string description,
        decimal quantity,
        decimal rate,
        decimal discountAmount,
        decimal taxAmount,
        DateTime createdUtc) =>
        new(tenantId, platformInvoiceId, subscriptionPlanId, planName, description, quantity, rate, discountAmount, taxAmount, createdUtc);

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new ArgumentException($"Value cannot exceed {max} characters.", name);
        return normalized;
    }

    private static string? Optional(string? value, string name, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new ArgumentException($"Value cannot exceed {max} characters.", name);
        return normalized;
    }

    private static DateTime RequireUtc(DateTime value, string name) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", name);
}

/// <summary>CustSearch platform subscription payment. Separate from Phase 8 retail payments.</summary>
public sealed class PlatformPayment
{
    private PlatformPayment() { }

    private PlatformPayment(
        long tenantId,
        long platformInvoiceId,
        string paymentMethod,
        decimal amount,
        string currency,
        string? gatewayReference,
        string transactionReference,
        DateTime paymentUtc,
        PlatformPaymentStatus status,
        DateTime createdUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformInvoiceId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        TenantId = tenantId;
        PlatformInvoiceId = platformInvoiceId;
        PaymentMethod = Require(paymentMethod, nameof(paymentMethod), 50);
        Amount = amount;
        Currency = Require(currency, nameof(currency), 3).ToUpperInvariant();
        GatewayReference = Optional(gatewayReference, nameof(gatewayReference), 150);
        TransactionReference = Require(transactionReference, nameof(transactionReference), 150);
        PaymentUtc = RequireUtc(paymentUtc, nameof(paymentUtc));
        Status = status;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long PlatformInvoiceId { get; private set; }
    public PlatformInvoice PlatformInvoice { get; private set; } = null!;
    public string PaymentMethod { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? GatewayReference { get; private set; }
    public string TransactionReference { get; private set; } = string.Empty;
    public DateTime PaymentUtc { get; private set; }
    public PlatformPaymentStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static PlatformPayment Create(
        long tenantId,
        long platformInvoiceId,
        string paymentMethod,
        decimal amount,
        string currency,
        string? gatewayReference,
        string transactionReference,
        DateTime paymentUtc,
        PlatformPaymentStatus status,
        DateTime createdUtc) =>
        new(tenantId, platformInvoiceId, paymentMethod, amount, currency, gatewayReference, transactionReference, paymentUtc, status, createdUtc);

    public bool MatchesCallback(long invoiceId, decimal amount, string currency, string transactionReference) =>
        PlatformInvoiceId == invoiceId
        && Amount == amount
        && string.Equals(Currency, currency, StringComparison.OrdinalIgnoreCase)
        && string.Equals(TransactionReference, transactionReference, StringComparison.Ordinal);

    public void UpdateStatus(PlatformPaymentStatus status, DateTime updatedUtc)
    {
        Status = status;
        UpdatedUtc = RequireUtc(updatedUtc, nameof(updatedUtc));
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new ArgumentException($"Value cannot exceed {max} characters.", name);
        return normalized;
    }

    private static string? Optional(string? value, string name, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new ArgumentException($"Value cannot exceed {max} characters.", name);
        return normalized;
    }

    private static DateTime RequireUtc(DateTime value, string name) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", name);
}
