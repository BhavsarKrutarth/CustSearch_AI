using CustSearch.Application.ReportsExports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.ReportsExports;

public sealed class ReportExportExceptionFilter:ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context){if(context.Exception is not ReportExportException exception)return;var status=exception.Kind switch{ReportExportFailureKind.Validation=>400,ReportExportFailureKind.Forbidden=>403,ReportExportFailureKind.NotFound=>404,ReportExportFailureKind.Conflict=>409,_=>503};context.Result=new ObjectResult(new ProblemDetails{Status=status,Title="Report/export request rejected",Detail=exception.Message}){StatusCode=status};context.ExceptionHandled=true;}
}
