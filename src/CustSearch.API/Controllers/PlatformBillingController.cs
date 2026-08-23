using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using CustSearch.Application.PlatformBilling;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Platform-admin API for CustSearch subscription billing. It never exposes Phase 8 retail billing data.</summary>
[ApiController]
[Route("api/platform/billing")]
[Authorize(Policy=AuthorizationPolicyNames.PlatformScope)]
public sealed class PlatformBillingController(IPlatformBillingService service):ControllerBase
{
    [HttpGet("plans")][HasPermission(PermissionCatalog.PlatformBilling.PlansView)] public Task<IReadOnlyList<PlatformPlanView>> Plans(CancellationToken ct)=>service.ListPlansAsync(ct);
    [HttpPost("plans")][HasPermission(PermissionCatalog.PlatformBilling.PlansManage)] public async Task<ActionResult<PlatformPlanView>> CreatePlan(SavePlatformPlanRequest request,CancellationToken ct){var created=await service.CreatePlanAsync(request.ToCommand(),ct).ConfigureAwait(false);return Created($"/api/platform/billing/plans/{created.Id}",created);}
    [HttpPut("plans/{planId:long}")][HasPermission(PermissionCatalog.PlatformBilling.PlansManage)] public Task<PlatformPlanView> UpdatePlan(long planId,SavePlatformPlanRequest request,CancellationToken ct)=>service.UpdatePlanAsync(planId,request.ToCommand(),ct);

    [HttpGet("subscriptions")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsView)] public Task<IReadOnlyList<PlatformSubscriptionView>> Subscriptions(CancellationToken ct)=>service.ListSubscriptionsAsync(ct);
    [HttpPost("subscriptions/{tenantId:long}")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsManage)] public Task<PlatformSubscriptionView> CreateSubscription(long tenantId,CreatePlatformSubscriptionRequest request,CancellationToken ct)=>service.CreateSubscriptionAsync(tenantId,request.ToCommand(),ct);
    [HttpPost("subscriptions/{tenantId:long}/renew")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsManage)] public Task<PlatformSubscriptionView> Renew(long tenantId,CancellationToken ct)=>service.RenewSubscriptionAsync(tenantId,ct);
    [HttpPut("subscriptions/{tenantId:long}/plan")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsManage)] public Task<PlatformSubscriptionView> ChangePlan(long tenantId,ChangePlatformPlanRequest request,CancellationToken ct)=>service.ChangePlanAsync(tenantId,request.ToCommand(),ct);
    [HttpPost("subscriptions/{tenantId:long}/cancel")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsManage)] public Task<PlatformSubscriptionView> Cancel(long tenantId,[FromQuery]bool atPeriodEnd=true,CancellationToken ct=default)=>service.CancelSubscriptionAsync(tenantId,atPeriodEnd,ct);

    [HttpGet("invoices")][HasPermission(PermissionCatalog.PlatformBilling.InvoicesView)] public Task<IReadOnlyList<PlatformInvoiceView>> Invoices(CancellationToken ct)=>service.ListInvoicesAsync(ct);
    [HttpPost("invoices/{tenantId:long}")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsManage)] public Task<PlatformInvoiceView> GenerateInvoice(long tenantId,GeneratePlatformInvoiceRequest request,CancellationToken ct)=>service.GenerateInvoiceAsync(tenantId,request.ToCommand(),ct);
    [HttpGet("payments")][HasPermission(PermissionCatalog.PlatformBilling.PaymentsView)] public Task<IReadOnlyList<PlatformPaymentView>> Payments(CancellationToken ct)=>service.ListPaymentsAsync(ct);
    [HttpPost("payments")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsManage)] public Task<PlatformPaymentView> RecordPayment(RecordPlatformPaymentRequest request,CancellationToken ct)=>service.RecordPaymentAsync(request.ToCommand(),ct);
}

/// <summary>Tenant-facing Phase 9 read model. TenantId comes only from the authenticated server context.</summary>
[ApiController]
[Route("api/tenant/platform-billing")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
public sealed class TenantPlatformBillingController(IPlatformBillingService service):ControllerBase
{
    [HttpGet][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsView)] public Task<TenantPlatformBillingSummary> Summary(CancellationToken ct)=>service.GetTenantSummaryAsync(ct);
    [HttpGet("subscription")][HasPermission(PermissionCatalog.PlatformBilling.SubscriptionsView)] public Task<PlatformSubscriptionView?> Subscription(CancellationToken ct)=>service.GetTenantSubscriptionAsync(ct);
    [HttpGet("invoices")][HasPermission(PermissionCatalog.PlatformBilling.InvoicesView)] public Task<IReadOnlyList<PlatformInvoiceView>> Invoices(CancellationToken ct)=>service.ListTenantInvoicesAsync(ct);
    [HttpGet("payments")][HasPermission(PermissionCatalog.PlatformBilling.PaymentsView)] public Task<IReadOnlyList<PlatformPaymentView>> Payments(CancellationToken ct)=>service.ListTenantPaymentsAsync(ct);
}

public sealed record SavePlatformPlanRequest(
    [param:Required,StringLength(30)]string PlanCode,
    [param:Required,StringLength(100)]string Name,
    [param:StringLength(1000)]string? Description,
    [param:Range(typeof(decimal),"0","999999999999.99")]decimal MonthlyPrice,
    [param:Range(typeof(decimal),"0","999999999999.99")]decimal? AnnualPrice,
    [param:Required,StringLength(3,MinimumLength=3)]string Currency,
    [param:Range(0,3650)]int TrialDays,
    [param:Range(1,int.MaxValue)]int MaxStores,
    [param:Range(1,int.MaxValue)]int MaxUsers,
    [param:Range(1,int.MaxValue)]int MaxStaff,
    [param:Range(1,int.MaxValue)]int MaxCameras,
    [param:Range(1,long.MaxValue)]long? MaxMonthlyRecognitions,
    [param:Range(1,long.MaxValue)]long? MaxMonthlyApiCalls,
    [param:StringLength(4000)]string? FeatureLimitsJson,
    bool IsActive,
    [param:Range(0,int.MaxValue)]int DisplayOrder)
{
    public SavePlatformPlanCommand ToCommand()=>new(PlanCode,Name,Description,MonthlyPrice,AnnualPrice,Currency,TrialDays,MaxStores,MaxUsers,MaxStaff,MaxCameras,MaxMonthlyRecognitions,MaxMonthlyApiCalls,FeatureLimitsJson,IsActive,DisplayOrder);
}

public sealed record CreatePlatformSubscriptionRequest([param:Range(1,long.MaxValue)]long PlanId,[param:Required,StringLength(20)]string BillingCycle,DateTime StartUtc,bool UseTrial)
{public CreatePlatformSubscriptionCommand ToCommand()=>new(PlanId,BillingCycle,StartUtc,UseTrial);}
public sealed record ChangePlatformPlanRequest([param:Range(1,long.MaxValue)]long PlanId,[param:Required,StringLength(20)]string BillingCycle)
{public ChangePlatformPlanCommand ToCommand()=>new(PlanId,BillingCycle);}
public sealed record GeneratePlatformInvoiceRequest([param:Range(typeof(decimal),"0","999999999999.99")]decimal DiscountAmount,[param:Range(typeof(decimal),"0","999999999999.99")]decimal TaxAmount,DateTime? DueUtc)
{public GeneratePlatformInvoiceCommand ToCommand()=>new(DiscountAmount,TaxAmount,DueUtc);}
public sealed record RecordPlatformPaymentRequest([param:Range(1,long.MaxValue)]long PlatformInvoiceId,[param:Required,StringLength(50)]string PaymentMethod,[param:Range(typeof(decimal),"0.01","999999999999.99")]decimal Amount,[param:Required,StringLength(3,MinimumLength=3)]string Currency,[param:StringLength(150)]string? GatewayReference,[param:Required,StringLength(150)]string TransactionReference,DateTime PaymentUtc,[param:Required,StringLength(20)]string Status)
{
    public RecordPlatformPaymentCommand ToCommand()=>Enum.TryParse<PlatformPaymentStatus>(Status,true,out var parsed)&&Enum.IsDefined(parsed)?new(PlatformInvoiceId,PaymentMethod,Amount,Currency,GatewayReference,TransactionReference,PaymentUtc,parsed):throw new PlatformBusinessRuleException("Platform payment status is invalid.");
}
