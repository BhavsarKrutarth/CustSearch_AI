using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustSearch.Application.Authentication;

namespace CustSearch.API.Security;

/// <summary>Reads the identity established by JwtBearer without accepting client-supplied tenant values.</summary>
public sealed class HttpCurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("There is no active HTTP request.");

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public long UserId => ParseRequiredInt64(JwtRegisteredClaimNames.Sub);

    public long? TenantId => TryParseInt64(CustomClaimTypes.TenantId);

    public bool IsPlatformAdmin => Principal.HasClaim(CustomClaimTypes.UserScope, "Platform")
        && TenantId is null;

    public string SecurityStamp => Principal.FindFirstValue(CustomClaimTypes.SecurityStamp)
        ?? throw new InvalidOperationException("The authenticated identity has no security stamp.");

    public IReadOnlySet<string> Roles => Principal.FindAll(ClaimTypes.Role)
        .Select(claim => claim.Value)
        .ToHashSet(StringComparer.Ordinal);

    public IReadOnlySet<string> Permissions => Principal.FindAll(CustomClaimTypes.Permission)
        .Select(claim => claim.Value)
        .ToHashSet(StringComparer.Ordinal);

    public IReadOnlySet<long> StoreIds => Principal.FindAll(CustomClaimTypes.StoreId)
        .Select(claim => long.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0)
        .Where(id => id > 0)
        .ToHashSet();

    private long ParseRequiredInt64(string claimType) => TryParseInt64(claimType)
        ?? throw new InvalidOperationException($"The authenticated identity has no valid '{claimType}' claim.");

    private long? TryParseInt64(string claimType) =>
        long.TryParse(Principal.FindFirstValue(claimType), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
