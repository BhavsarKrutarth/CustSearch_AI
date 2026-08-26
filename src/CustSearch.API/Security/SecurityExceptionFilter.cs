using CustSearch.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.Security;
public sealed class SecurityExceptionFilter:IExceptionFilter
{
    public void OnException(ExceptionContext context){if(context.Exception is not SecurityException x)return;context.Result=x.Kind switch{SecurityFailureKind.Validation=>new BadRequestObjectResult(new{message=x.Message}),SecurityFailureKind.Unauthorized or SecurityFailureKind.Replay=>new UnauthorizedObjectResult(new{message=x.Message}),SecurityFailureKind.Forbidden=>new ObjectResult(new{message=x.Message}){StatusCode=403},SecurityFailureKind.NotFound=>new NotFoundObjectResult(new{message=x.Message}),SecurityFailureKind.Conflict=>new ConflictObjectResult(new{message=x.Message}),_=>new ObjectResult(new{message=x.Message}){StatusCode=503}};context.ExceptionHandled=true;}
}
