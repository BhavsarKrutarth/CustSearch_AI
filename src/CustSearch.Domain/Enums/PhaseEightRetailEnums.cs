namespace CustSearch.Domain.Enums;

/// <summary>Phase 8 retail invoice lifecycle. Finalized financial records are never hard-deleted.</summary>
public enum RetailInvoiceStatus : byte
{
    Draft = 1,
    Finalized = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5
}

/// <summary>Factual payment method recorded for one retail payment.</summary>
public enum RetailPaymentMethod : byte
{
    Cash = 1,
    Upi = 2,
    Card = 3,
    BankTransfer = 4,
    Other = 5
}

/// <summary>Payment processing/result state. Only Successful payments contribute to PaidAmount.</summary>
public enum RetailPaymentStatus : byte
{
    Pending = 1,
    Successful = 2,
    Failed = 3,
    Refunded = 4
}

/// <summary>Explicit invoice participant role. VisitParty/co-visit membership never creates one automatically.</summary>
public enum RetailParticipationType : byte
{
    Payer = 1,
    Shopper = 2,
    Beneficiary = 3,
    Other = 4
}

/// <summary>How an invoice item amount was explicitly attributed to a known customer.</summary>
public enum RetailAttributionType : byte
{
    Amount = 1,
    Quantity = 2,
    Adjustment = 3
}

/// <summary>Auditable source for spend attribution. No face/proximity-derived source exists.</summary>
public enum RetailAttributionSource : byte
{
    Staff = 1,
    CustomerProvided = 2,
    InvoiceRule = 3,
    Imported = 4
}
