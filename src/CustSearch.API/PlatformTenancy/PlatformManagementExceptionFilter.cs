using CustSearch.Application.PlatformTenancy;
using CustSearch.Application.TenantOperations;
using CustSearch.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.PlatformTenancy;

/// <summary>Converts expected platform and tenant management failures into safe API error envelopes.</summary>
public sealed class PlatformManagementExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var mapped = context.Exception switch
        {
            PlatformResourceNotFoundException => (StatusCodes.Status404NotFound, "ResourceNotFound"),
            TenantResourceNotFoundException => (StatusCodes.Status404NotFound, "ResourceNotFound"),
            PlatformConcurrencyException => (StatusCodes.Status409Conflict, "ConcurrencyConflict"),
            PlatformBusinessRuleException => (StatusCodes.Status400BadRequest, "BusinessRuleViolation"),
            TenantBusinessRuleException => (StatusCodes.Status400BadRequest, "BusinessRuleViolation"),
            _ => ((int StatusCode, string Code)?)null,
        };
        if (mapped is not { } error) return;
        context.Result = new ObjectResult(new ApiErrorResponse(error.Code, context.Exception.Message, context.HttpContext.TraceIdentifier)) { StatusCode = error.StatusCode };
        context.ExceptionHandled = true;
    }
}
