using CustSearch.API.CamerasTracking;
using CustSearch.Application.CamerasTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustSearch.API.Controllers;

/// <summary>HMAC-authenticated service boundary used by the Python CCTV process; it never exposes SQL Server.</summary>
[ApiController]
[Route("api/internal/cctv/events")]
[AllowAnonymous]
[EnableRateLimiting("cctv-inbound")]
[ServiceFilter(typeof(CameraTrackingExceptionFilter))]
public sealed class CctvEventsController(ICameraTrackingService service):ControllerBase
{
    public const int MaximumBodyBytes=131072;
    [HttpPost][Consumes("application/json")][RequestSizeLimit(MaximumBodyBytes)]
    public async Task<ActionResult<CctvEventAcknowledgement>>Receive(CancellationToken ct){await using var body=new MemoryStream();await Request.Body.CopyToAsync(body,ct).ConfigureAwait(false);if(body.Length>MaximumBodyBytes)return StatusCode(StatusCodes.Status413PayloadTooLarge,new{message="CCTV event body is too large."});var envelope=new CctvInboundEnvelope(Header("X-CustSearch-Service-Id"),Header("X-CustSearch-Timestamp"),Header("X-CustSearch-Signature"),Header("X-CustSearch-Event-Id"),Header("Idempotency-Key"),body.ToArray(),HttpContext.TraceIdentifier);return Ok(await service.ReceiveAsync(envelope,ct).ConfigureAwait(false));}
    private string Header(string name)=>Request.Headers[name].FirstOrDefault()??string.Empty;
}
