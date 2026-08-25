using CustSearch.Application.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.Operations;

public sealed class OperationalExceptionFilter:ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if(context.Exception is not OperationalException exception)return;
        var status=exception.Kind switch{OperationalFailureKind.Validation=>StatusCodes.Status400BadRequest,OperationalFailureKind.Forbidden=>StatusCodes.Status403Forbidden,OperationalFailureKind.NotFound=>StatusCodes.Status404NotFound,_=>StatusCodes.Status400BadRequest};
        context.Result=new ObjectResult(new ProblemDetails{Status=status,Title="Operational request failed",Detail=exception.Message}){StatusCode=status};context.ExceptionHandled=true;
    }
}
