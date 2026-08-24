using CustSearch.Application.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.Integrations;

public sealed class IntegrationExceptionFilter:IExceptionFilter
{
    public void OnException(ExceptionContext context){if(context.Exception is not IntegrationException failure)return;context.Result=failure.Kind switch{IntegrationFailureKind.Validation=>new BadRequestObjectResult(new{message=failure.Message}),IntegrationFailureKind.Unauthorized=>new UnauthorizedObjectResult(new{message=failure.Message}),IntegrationFailureKind.Forbidden=>new ObjectResult(new{message=failure.Message}){StatusCode=StatusCodes.Status403Forbidden},IntegrationFailureKind.NotFound=>new NotFoundObjectResult(new{message=failure.Message}),IntegrationFailureKind.Conflict=>new ConflictObjectResult(new{message=failure.Message}),IntegrationFailureKind.Unavailable=>new ObjectResult(new{message=failure.Message}){StatusCode=StatusCodes.Status503ServiceUnavailable},_=>new BadRequestObjectResult(new{message=failure.Message})};context.ExceptionHandled=true;}
}
