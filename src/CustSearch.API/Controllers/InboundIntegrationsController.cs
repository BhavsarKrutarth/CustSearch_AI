using CustSearch.API.Integrations;
using CustSearch.Application.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustSearch.API.Controllers;

/// <summary>Partner webhook boundary authenticated by HMAC over tenant, timestamp, IDs and exact raw body.</summary>
[ApiController]
[Route("api/integrations/inbound")]
[AllowAnonymous]
[EnableRateLimiting("integration-inbound")]
[ServiceFilter(typeof(IntegrationExceptionFilter))]
public sealed class InboundIntegrationsController(IInboundIntegrationService service):ControllerBase
{
    public const int MaximumBodyBytes=262144;
    [HttpPost("{integrationId:long}/events")]
    [Consumes("application/json")]
    [RequestSizeLimit(MaximumBodyBytes)]
    public async Task<ActionResult<InboundIntegrationAcknowledgement>>Receive(long integrationId,CancellationToken ct)
    {
        await using var body=new MemoryStream();await Request.Body.CopyToAsync(body,ct).ConfigureAwait(false);if(body.Length>MaximumBodyBytes)return StatusCode(StatusCodes.Status413PayloadTooLarge,new{message="Inbound request body is too large."});var request=new InboundIntegrationRequest(integrationId,Header("X-CustSearch-Tenant-Id"),Header("X-CustSearch-Timestamp"),Header("X-CustSearch-Signature"),Header("X-CustSearch-Event-Id"),Header("Idempotency-Key"),body.ToArray(),HttpContext.TraceIdentifier);return Ok(await service.ReceiveAsync(request,ct).ConfigureAwait(false));
    }
    private string Header(string name)=>Request.Headers[name].FirstOrDefault()??string.Empty;
}
