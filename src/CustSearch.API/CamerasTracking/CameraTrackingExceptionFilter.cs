using CustSearch.Application.CamerasTracking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustSearch.API.CamerasTracking;

public sealed class CameraTrackingExceptionFilter:IExceptionFilter
{
    public void OnException(ExceptionContext context){if(context.Exception is not CameraTrackingException failure)return;context.Result=failure.Kind switch{CameraTrackingFailureKind.Validation=>new BadRequestObjectResult(new{message=failure.Message}),CameraTrackingFailureKind.Unauthorized=>new UnauthorizedObjectResult(new{message=failure.Message}),CameraTrackingFailureKind.Forbidden=>new ObjectResult(new{message=failure.Message}){StatusCode=StatusCodes.Status403Forbidden},CameraTrackingFailureKind.NotFound=>new NotFoundObjectResult(new{message=failure.Message}),CameraTrackingFailureKind.Conflict=>new ConflictObjectResult(new{message=failure.Message}),CameraTrackingFailureKind.Unavailable=>new ObjectResult(new{message=failure.Message}){StatusCode=StatusCodes.Status503ServiceUnavailable},_=>new BadRequestObjectResult(new{message=failure.Message})};context.ExceptionHandled=true;}
}
