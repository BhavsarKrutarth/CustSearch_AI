using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>
/// Records a tenant's plan assignment and commercial billing period over time.
/// </summary>
public sealed class TenantSubscription
{
    private TenantSubscription()
    {
    }

    private TenantSubscription(
        long tenantId,
        long subscriptionPlanId,
        BillingCycle billingCycle,
        SubscriptionStatus status,
        DateTime startsUtc,
        DateTime? endsUtc,
        bool autoRenew,
        DateTime createdUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionPlanId);
        StartsUtc = RequireUtc(startsUtc, nameof(startsUtc));
        EndsUtc = OptionalUtc(endsUtc, nameof(endsUtc));
        if (EndsUtc <= StartsUtc)
        {
            throw new ArgumentException("Subscription end must be later than its start.", nameof(endsUtc));
        }

        TenantId = tenantId;
        SubscriptionPlanId = subscriptionPlanId;
        BillingCycle = billingCycle;
        Status = status;
        AutoRenew = autoRenew;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
        RowVersion = NewRowVersion();
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long SubscriptionPlanId { get; private set; }
    public SubscriptionPlan SubscriptionPlan { get; private set; } = null!;
    public BillingCycle BillingCycle { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartsUtc { get; private set; }
    public DateTime? EndsUtc { get; private set; }
    public bool AutoRenew { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static TenantSubscription Create(
        long tenantId,
        long subscriptionPlanId,
        BillingCycle billingCycle,
        SubscriptionStatus status,
        DateTime startsUtc,
        DateTime? endsUtc,
        bool autoRenew,
        DateTime createdUtc) =>
        new(tenantId, subscriptionPlanId, billingCycle, status, startsUtc, endsUtc, autoRenew, createdUtc);

    /// <summary>Changes a subscription state and end date without rewriting its history.</summary>
    public void UpdateStatus(SubscriptionStatus status, DateTime? endsUtc, bool autoRenew, DateTime updatedUtc)
    {
        var normalizedEnd = OptionalUtc(endsUtc, nameof(endsUtc));
        if (normalizedEnd <= StartsUtc)
        {
            throw new ArgumentException("Subscription end must be later than its start.", nameof(endsUtc));
        }

        Status = status;
        EndsUtc = normalizedEnd;
        AutoRenew = autoRenew;
        UpdatedUtc = RequireUtc(updatedUtc, nameof(updatedUtc));
        RowVersion = NewRowVersion();
    }

    private static DateTime? OptionalUtc(DateTime? value, string parameterName) =>
        value.HasValue ? RequireUtc(value.Value, parameterName) : null;

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);

    private static byte[] NewRowVersion() => Guid.NewGuid().ToByteArray();
}
