namespace CustSearch.Domain.Enums;

/// <summary>Server-authoritative lifecycle for a CustSearch platform invoice.</summary>
public enum PlatformInvoiceStatus : byte
{
    Draft = 1,
    Open = 2,
    Paid = 3,
    Void = 4,
    Overdue = 5,
}

/// <summary>Factual state of a CustSearch platform payment attempt.</summary>
public enum PlatformPaymentStatus : byte
{
    Pending = 1,
    Successful = 2,
    Failed = 3,
    Refunded = 4,
}
