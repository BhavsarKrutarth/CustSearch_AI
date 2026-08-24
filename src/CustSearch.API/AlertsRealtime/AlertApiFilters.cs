using CustSearch.Application.AlertsRealtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.AlertsRealtime;

/// <summary>Rejects query/route attempts to inject tenant identity before Phase 11 actions execute.</summary>
[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
public sealed class RejectClientTenantIdAttribute:ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context){if(context.HttpContext.Request.Query.Keys.Any(x=>string.Equals(x,"tenantId",StringComparison.OrdinalIgnoreCase))||context.RouteData.Values.Keys.Any(x=>string.Equals(x,"tenantId",StringComparison.OrdinalIgnoreCase)))context.Result=new BadRequestObjectResult(new{message="TenantId is derived from the authenticated server context."});}
}

/// <summary>Maps scoped alert failures without revealing cross-tenant resource existence.</summary>
public sealed class AlertExceptionFilter:IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result=context.Exception switch{AlertResourceNotFoundException=>new NotFoundObjectResult(new{message=context.Exception.Message}),AlertBusinessRuleException or InvalidOperationException=>new BadRequestObjectResult(new{message=context.Exception.Message}),_=>null};if(context.Result is not null)context.ExceptionHandled=true;
    }
}
