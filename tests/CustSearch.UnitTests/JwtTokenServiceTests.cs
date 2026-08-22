using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CustSearch.UnitTests;

public sealed class JwtTokenServiceTests
{
    private const string SigningKey = "test-only-signing-key-with-at-least-thirty-two-bytes";

    [Fact]
    public void CreateUsesConfiguredExpiryAndIdentityClaims()
    {
        var now = DateTime.UtcNow;
        var user = UserAccount.CreatePlatform(
            "platform.admin",
            "admin@example.test",
            "Platform Admin",
            "not-used-by-token-tests",
            now);
        var service = new JwtTokenService(Options.Create(CreateOptions(accessMinutes: 9)));

        var authorization = new AuthorizationProfile(
            ["PlatformOperationsAdmin"],
            [PermissionCatalog.Platform.TenantsView],
            [17]);
        var result = service.Create(user, authorization, now);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(now.AddMinutes(9), result.ExpiresUtc);
        Assert.Equal("CustSearch.Tests", token.Issuer);
        Assert.Contains("CustSearch.TestClient", token.Audiences);
        Assert.Equal("platform.admin", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(user.SecurityStamp, token.Claims.Single(claim => claim.Type == "security_stamp").Value);
        Assert.Contains(token.Claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role
            && claim.Value == "PlatformOperationsAdmin");
        Assert.Contains(token.Claims, claim => claim.Type == CustomClaimTypes.Permission
            && claim.Value == PermissionCatalog.Platform.TenantsView);
        Assert.Contains(token.Claims, claim => claim.Type == CustomClaimTypes.StoreId && claim.Value == "17");
    }

    [Theory]
    [InlineData("WrongIssuer", "CustSearch.TestClient")]
    [InlineData("CustSearch.Tests", "WrongAudience")]
    public void ValidationRejectsWrongIssuerOrAudience(string issuer, string audience)
    {
        var now = DateTime.UtcNow;
        var user = UserAccount.CreatePlatform("admin", "admin@example.test", "Admin", "hash", now);
        var result = new JwtTokenService(Options.Create(CreateOptions()))
            .Create(user, AuthorizationProfile.Empty, now);
        var parameters = CreateValidationParameters(issuer, audience);

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(result.Token, parameters, out _));
    }

    [Fact]
    public void ValidationRejectsExpiredTokenUsingConfiguredLifetime()
    {
        var issued = DateTime.UtcNow.AddMinutes(-3);
        var user = UserAccount.CreatePlatform("admin", "admin@example.test", "Admin", "hash", issued);
        var result = new JwtTokenService(Options.Create(CreateOptions(accessMinutes: 1)))
            .Create(user, AuthorizationProfile.Empty, issued);

        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(
                result.Token,
                CreateValidationParameters("CustSearch.Tests", "CustSearch.TestClient"),
                out _));
    }

    private static JwtOptions CreateOptions(int accessMinutes = 15) => new()
    {
        Issuer = "CustSearch.Tests",
        Audience = "CustSearch.TestClient",
        SigningKey = SigningKey,
        AccessTokenLifetimeMinutes = accessMinutes,
        RefreshTokenLifetimeDays = 7,
        ClockSkewSeconds = 0,
    };

    private static TokenValidationParameters CreateValidationParameters(string issuer, string audience) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };
}
