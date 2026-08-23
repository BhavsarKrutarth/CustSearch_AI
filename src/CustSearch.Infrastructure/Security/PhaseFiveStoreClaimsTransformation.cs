using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustSearch.Application.Authentication;
using CustSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.Security;

/// <summary>
/// Phase 5B — refreshes StoreId claims from UserStoreAssignments after JWT validation.
/// Browser-supplied store identifiers are never trusted; only active same-tenant database rows are emitted.
/// </summary>
public sealed class PhaseFiveStoreClaimsTransformation(CustSearchDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated) return principal;
        foreach (var claim in identity.FindAll(CustomClaimTypes.StoreId).ToArray()) identity.RemoveClaim(claim);

        var userIdValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var tenantIdValue = principal.FindFirstValue(CustomClaimTypes.TenantId);
        if (!long.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            || !long.TryParse(tenantIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var tenantId)) return principal;

        var storeIds = await db.UserStoreAssignments.AsNoTracking()
            .Where(x => x.UserId == userId && x.TenantId == tenantId && x.Store.IsActive && x.User.IsActive)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.StoreId)
            .Select(x => x.StoreId).Distinct().ToArrayAsync().ConfigureAwait(false);
        identity.AddClaims(storeIds.Select(id => new Claim(CustomClaimTypes.StoreId, id.ToString(CultureInfo.InvariantCulture))));
        return principal;
    }
}
