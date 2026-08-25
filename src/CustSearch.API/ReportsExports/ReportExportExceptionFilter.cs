using CustSearch.Application.ReportsExports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.ReportsExports;

/// <summary>Maps expected Phase 15 report failures without leaking SQL, storage paths or worker details.</summary>
public sealed class ReportExportExceptionFilter:ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var(status,title) = context.Exception switch
        {
            ReportExportBusinessRuleException=>(StatusCodes.Status400BadRequest,"Invalid report request"),
            ReportExportNotFoundException=>(StatusCodes.Status404NotFound,"Report export not found"),
            UnauthorizedAccessException=>(StatusCodes.Status403Forbidden,"Report access forbidden"),
            _=>(0,string.Empty),
        };
        if(status==0)return;context.Result=new ObjectResult(new ProblemDetails{Status=status,Title=title,Detail=context.Exception.Message}){StatusCode=status};context.ExceptionHandled=true;
    }
}

