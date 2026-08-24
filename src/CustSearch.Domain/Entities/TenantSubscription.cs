using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Records server-authoritative tenant plan history and Phase 9 commercial periods.</summary>
public sealed class TenantSubscription
{
    private TenantSubscription() { }

    private TenantSubscription(long tenantId,long subscriptionPlanId,BillingCycle billingCycle,SubscriptionStatus status,DateTime startsUtc,DateTime? endsUtc,bool autoRenew,DateTime createdUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionPlanId);
        StartsUtc=RequireUtc(startsUtc,nameof(startsUtc));EndsUtc=OptionalUtc(endsUtc,nameof(endsUtc));if(EndsUtc<=StartsUtc)throw new ArgumentException("Subscription end must be later than its start.",nameof(endsUtc));
        TenantId=tenantId;SubscriptionPlanId=subscriptionPlanId;BillingCycle=billingCycle;Status=status;AutoRenew=autoRenew;
        CurrentPeriodStartUtc=StartsUtc;CurrentPeriodEndUtc=EndsUtc;CancelAtPeriodEnd=!autoRenew;CreatedUtc=RequireUtc(createdUtc,nameof(createdUtc));UpdatedUtc=CreatedUtc;RowVersion=NewRowVersion();
    }

    public long Id{get;private set;} public long TenantId{get;private set;} public Tenant Tenant{get;private set;}=null!; public long SubscriptionPlanId{get;private set;} public SubscriptionPlan SubscriptionPlan{get;private set;}=null!;
    public BillingCycle BillingCycle{get;private set;} public SubscriptionStatus Status{get;private set;} public DateTime StartsUtc{get;private set;} public DateTime? EndsUtc{get;private set;} public bool AutoRenew{get;private set;}
    public DateTime? TrialEndUtc{get;private set;} public DateTime? CurrentPeriodStartUtc{get;private set;} public DateTime? CurrentPeriodEndUtc{get;private set;} public bool CancelAtPeriodEnd{get;private set;} public DateTime? CancelledUtc{get;private set;}
    public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;} public byte[] RowVersion{get;private set;}=[];

    public static TenantSubscription Create(long tenantId,long subscriptionPlanId,BillingCycle billingCycle,SubscriptionStatus status,DateTime startsUtc,DateTime? endsUtc,bool autoRenew,DateTime createdUtc)=>new(tenantId,subscriptionPlanId,billingCycle,status,startsUtc,endsUtc,autoRenew,createdUtc);

    public void ConfigureAuthoritativePeriod(DateTime? trialEndUtc,DateTime periodStartUtc,DateTime periodEndUtc,bool cancelAtPeriodEnd,DateTime updatedUtc)
    {
        var start=RequireUtc(periodStartUtc,nameof(periodStartUtc));var end=RequireUtc(periodEndUtc,nameof(periodEndUtc));if(end<=start)throw new ArgumentException("Current period end must be later than its start.",nameof(periodEndUtc));
        var trial=OptionalUtc(trialEndUtc,nameof(trialEndUtc));if(trial.HasValue&&trial<=StartsUtc)throw new ArgumentException("Trial end must be later than subscription start.",nameof(trialEndUtc));
        TrialEndUtc=trial;CurrentPeriodStartUtc=start;CurrentPeriodEndUtc=end;EndsUtc=end;CancelAtPeriodEnd=cancelAtPeriodEnd;AutoRenew=!cancelAtPeriodEnd;Touch(updatedUtc);
    }

    public void ChangePlan(long planId,BillingCycle billingCycle,DateTime periodStartUtc,DateTime periodEndUtc,DateTime updatedUtc)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planId);SubscriptionPlanId=planId;BillingCycle=billingCycle;Status=SubscriptionStatus.Active;TrialEndUtc=null;CancelledUtc=null;ConfigureAuthoritativePeriod(null,periodStartUtc,periodEndUtc,false,updatedUtc);}

    public void Renew(DateTime periodStartUtc,DateTime periodEndUtc,DateTime updatedUtc)
    {Status=SubscriptionStatus.Active;CancelledUtc=null;ConfigureAuthoritativePeriod(null,periodStartUtc,periodEndUtc,false,updatedUtc);}

    public void Cancel(bool atPeriodEnd,DateTime updatedUtc)
    {var utc=RequireUtc(updatedUtc,nameof(updatedUtc));CancelAtPeriodEnd=atPeriodEnd;AutoRenew=false;if(!atPeriodEnd){Status=SubscriptionStatus.Cancelled;CancelledUtc=utc;EndsUtc=utc;}Touch(utc);}

    /// <summary>Legacy lifecycle operation retained for existing platform-tenancy flows.</summary>
    public void UpdateStatus(SubscriptionStatus status,DateTime? endsUtc,bool autoRenew,DateTime updatedUtc)
    {var normalizedEnd=OptionalUtc(endsUtc,nameof(endsUtc));if(normalizedEnd<=StartsUtc)throw new ArgumentException("Subscription end must be later than its start.",nameof(endsUtc));Status=status;EndsUtc=normalizedEnd;CurrentPeriodEndUtc=normalizedEnd;AutoRenew=autoRenew;CancelAtPeriodEnd=!autoRenew;if(status==SubscriptionStatus.Cancelled)CancelledUtc=RequireUtc(updatedUtc,nameof(updatedUtc));Touch(updatedUtc);}

    private static DateTime? OptionalUtc(DateTime? value,string parameterName)=>value.HasValue?RequireUtc(value.Value,parameterName):null;
    private static DateTime RequireUtc(DateTime value,string parameterName)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",parameterName);
    private void Touch(DateTime utc){UpdatedUtc=RequireUtc(utc,nameof(utc));RowVersion=NewRowVersion();}
    private static byte[] NewRowVersion()=>Guid.NewGuid().ToByteArray();
}
