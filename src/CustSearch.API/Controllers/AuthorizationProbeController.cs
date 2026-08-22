using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>
/// Provides small protected endpoints used to verify platform, tenant and granular permission enforcement.
/// </summary>
[ApiController]
[Route("api/authorization/probe")]
public sealed class AuthorizationProbeController(ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("platform")]
    [Authorize(Policy = AuthorizationPolicyNames.PlatformScope)]
    public ActionResult<AuthorizationProbeResponse> Platform() => Ok(CreateResponse("platform"));

    [HttpGet("tenant")]
    [Authorize(Policy = AuthorizationPolicyNames.TenantScope)]
    public ActionResult<AuthorizationProbeResponse> Tenant() => Ok(CreateResponse("tenant"));

    [HttpGet("customers")]
    [HasPermission(PermissionCatalog.Operations.CustomersView)]
    public ActionResult<AuthorizationProbeResponse> Customers() => Ok(CreateResponse("permission"));

    private AuthorizationProbeResponse CreateResponse(string policy) => new(
        policy,
        currentUser.UserId,
        currentUser.TenantId,
        currentUser.Roles.Order(StringComparer.Ordinal).ToArray(),
        currentUser.Permissions.Order(StringComparer.Ordinal).ToArray(),
        currentUser.StoreIds.Order().ToArray());
}

/// <summary>
/// Shows the authoritative identity scope accepted by an authorization probe.
/// </summary>
public sealed record AuthorizationProbeResponse(
    string Policy,
    long UserId,
    long? TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<long> StoreIds);
