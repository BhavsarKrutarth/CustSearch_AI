namespace CustSearch.Domain.Enums;

/// <summary>
/// Describes the commercial lifecycle state of a tenant or subscription.
/// </summary>
public enum SubscriptionStatus : byte
{
    Trial = 1,
    Active = 2,
    PastDue = 3,
    Suspended = 4,
    Cancelled = 5,
    Expired = 6,
}
