using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CustSearch.Infrastructure.Security;

/// <summary>
/// Issues short-lived signed JWT access tokens from validated server-side identity data.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult Create(UserAccount user, AuthorizationProfile authorization, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(authorization);
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Token issue time must be UTC.", nameof(utcNow));
        }

        var expiresUtc = utcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(CustomClaimTypes.UserScope, user.Scope.ToString()),
            new(CustomClaimTypes.SecurityStamp, user.SecurityStamp),
        };

        claims.AddRange(authorization.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(authorization.Permissions.Select(permission =>
            new Claim(CustomClaimTypes.Permission, permission)));
        claims.AddRange(authorization.StoreIds.Select(storeId => new Claim(
            CustomClaimTypes.StoreId,
            storeId.ToString(System.Globalization.CultureInfo.InvariantCulture))));

        if (user.TenantId is { } tenantId)
        {
            claims.Add(new Claim(
                CustomClaimTypes.TenantId,
                tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            utcNow,
            expiresUtc,
            credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresUtc);
    }
}

/// <summary>
/// Returns the signed token and its exact UTC expiry to the caller.
/// </summary>
public sealed record AccessTokenResult(string Token, DateTime ExpiresUtc);
