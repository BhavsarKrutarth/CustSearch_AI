using CustSearch.Contracts.System;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>
/// Provides non-sensitive runtime metadata used by deployment smoke tests.
/// </summary>
[ApiController]
[Route("api/system")]
public sealed class SystemController(IHostEnvironment environment, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType<SystemInfoResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo() => Ok(new SystemInfoResponse(
        "CustSearch.API",
        environment.EnvironmentName,
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        timeProvider.GetUtcNow().UtcDateTime,
        HttpContext.TraceIdentifier));
}
