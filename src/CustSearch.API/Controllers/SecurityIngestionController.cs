using CustSearch.API.Security;
using CustSearch.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustSearch.API.Controllers;
[ApiController][Route("api/internal/security/observations")][AllowAnonymous][EnableRateLimiting("security-ingestion")][ServiceFilter(typeof(SecurityExceptionFilter))]
public sealed class SecurityIngestionController(ISecurityPlatformService service):ControllerBase
{
    public const int MaximumBodyBytes=262144;
    [HttpPost][Consumes("application/json")][RequestSizeLimit(MaximumBodyBytes)]
    public async Task<ActionResult<SecurityIngestionResult>>Receive(CancellationToken ct){await using var body=new MemoryStream();await Request.Body.CopyToAsync(body,ct).ConfigureAwait(false);if(body.Length>MaximumBodyBytes)return StatusCode(413,new{message="Observation body is too large."});var envelope=new SecurityIngestionEnvelope(Header("X-CustSearch-Service-Key"),Header("X-CustSearch-Timestamp"),Header("X-CustSearch-Nonce"),Header("X-CustSearch-Signature"),Header("Idempotency-Key"),body.ToArray(),HttpContext.TraceIdentifier);return Ok(await service.IngestAsync(envelope,ct).ConfigureAwait(false));}
    private string Header(string name)=>Request.Headers[name].FirstOrDefault()??string.Empty;
}
