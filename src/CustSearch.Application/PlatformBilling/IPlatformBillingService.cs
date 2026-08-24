using CustSearch.Domain.Enums;

namespace CustSearch.Application.PlatformBilling;

public interface IPlatformBillingService
{
    Task<IReadOnlyList<PlatformPlanView>> ListPlansAsync(CancellationToken cancellationToken=default);
    Task<PlatformPlanView> CreatePlanAsync(SavePlatformPlanCommand command,CancellationToken cancellationToken=default);
    Task<PlatformPlanView> UpdatePlanAsync(long planId,SavePlatformPlanCommand command,CancellationToken cancellationToken=default);

    Task<IReadOnlyList<PlatformSubscriptionView>> ListSubscriptionsAsync(CancellationToken cancellationToken=default);
    Task<PlatformSubscriptionView> CreateSubscriptionAsync(long tenantId,CreatePlatformSubscriptionCommand command,CancellationToken cancellationToken=default);
    Task<PlatformSubscriptionView> RenewSubscriptionAsync(long tenantId,CancellationToken cancellationToken=default);
    Task<PlatformSubscriptionView> ChangePlanAsync(long tenantId,ChangePlatformPlanCommand command,CancellationToken cancellationToken=default);
    Task<PlatformSubscriptionView> CancelSubscriptionAsync(long tenantId,bool atPeriodEnd,CancellationToken cancellationToken=default);

    Task<IReadOnlyList<PlatformInvoiceView>> ListInvoicesAsync(CancellationToken cancellationToken=default);
    Task<PlatformInvoiceView> GenerateInvoiceAsync(long tenantId,GeneratePlatformInvoiceCommand command,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<PlatformPaymentView>> ListPaymentsAsync(CancellationToken cancellationToken=default);
    Task<PlatformPaymentView> RecordPaymentAsync(RecordPlatformPaymentCommand command,CancellationToken cancellationToken=default);

    Task<TenantPlatformBillingSummary> GetTenantSummaryAsync(CancellationToken cancellationToken=default);
    Task<PlatformSubscriptionView?> GetTenantSubscriptionAsync(CancellationToken cancellationToken=default);
    Task<IReadOnlyList<PlatformInvoiceView>> ListTenantInvoicesAsync(CancellationToken cancellationToken=default);
    Task<IReadOnlyList<PlatformPaymentView>> ListTenantPaymentsAsync(CancellationToken cancellationToken=default);
}

public sealed record PlatformPlanView(long Id,string PlanCode,string Name,string? Description,decimal MonthlyPrice,decimal? AnnualPrice,string Currency,int TrialDays,int MaxStores,int MaxUsers,int MaxStaff,int MaxCameras,long? MaxMonthlyRecognitions,long? MaxMonthlyApiCalls,string? FeatureLimitsJson,bool IsActive,int DisplayOrder,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record SavePlatformPlanCommand(string PlanCode,string Name,string? Description,decimal MonthlyPrice,decimal? AnnualPrice,string Currency,int TrialDays,int MaxStores,int MaxUsers,int MaxStaff,int MaxCameras,long? MaxMonthlyRecognitions,long? MaxMonthlyApiCalls,string? FeatureLimitsJson,bool IsActive,int DisplayOrder);

public sealed record PlatformSubscriptionView(long Id,long TenantId,long PlanId,string PlanCode,string PlanName,string BillingCycle,string Status,DateTime StartUtc,DateTime? TrialEndUtc,DateTime? CurrentPeriodStartUtc,DateTime? CurrentPeriodEndUtc,bool CancelAtPeriodEnd,DateTime? CancelledUtc,int MaxStores,int MaxUsers,int MaxStaff,int MaxCameras);
public sealed record CreatePlatformSubscriptionCommand(long PlanId,string BillingCycle,DateTime StartUtc,bool UseTrial);
public sealed record ChangePlatformPlanCommand(long PlanId,string BillingCycle);

public sealed record PlatformInvoiceItemView(long Id,long? PlanId,string PlanName,string? Description,decimal Quantity,decimal Rate,decimal Discount,decimal Tax,decimal Subtotal,decimal Total);
public sealed record PlatformInvoiceView(long Id,long TenantId,long TenantSubscriptionId,string InvoiceNumber,string Currency,DateTime InvoiceUtc,DateTime DueUtc,string Status,decimal Subtotal,decimal Discount,decimal Tax,decimal Total,decimal PaidAmount,IReadOnlyList<PlatformInvoiceItemView> Items);
public sealed record GeneratePlatformInvoiceCommand(decimal DiscountAmount,decimal TaxAmount,DateTime? DueUtc);

public sealed record PlatformPaymentView(long Id,long TenantId,long PlatformInvoiceId,string PaymentMethod,decimal Amount,string Currency,string? GatewayReference,string TransactionReference,DateTime PaymentUtc,string Status);
public sealed record RecordPlatformPaymentCommand(long PlatformInvoiceId,string PaymentMethod,decimal Amount,string Currency,string? GatewayReference,string TransactionReference,DateTime PaymentUtc,PlatformPaymentStatus Status);

public sealed record TenantPlatformBillingSummary(PlatformSubscriptionView? Subscription,string? CurrentPlan,int MaxStores,int MaxUsers,int MaxStaff,int MaxCameras,DateTime? RenewalUtc,string? LatestPaymentStatus,int InvoiceCount);
