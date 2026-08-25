using CustSearch.Application.Recognition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.Recognition;

public sealed class RecognitionExceptionFilter:IExceptionFilter
{
    public void OnException(ExceptionContext context){if(context.Exception is not RecognitionException failure)return;context.Result=failure.Kind switch{RecognitionFailureKind.Validation=>new BadRequestObjectResult(new{message=failure.Message}),RecognitionFailureKind.Forbidden=>new ObjectResult(new{message=failure.Message}){StatusCode=StatusCodes.Status403Forbidden},RecognitionFailureKind.NotFound=>new NotFoundObjectResult(new{message=failure.Message}),RecognitionFailureKind.Conflict=>new ConflictObjectResult(new{message=failure.Message}),RecognitionFailureKind.Unavailable=>new ObjectResult(new{message=failure.Message}){StatusCode=StatusCodes.Status503ServiceUnavailable},_=>new BadRequestObjectResult(new{message=failure.Message})};context.ExceptionHandled=true;}
}
