using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CustSearch.Application.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CustSearch.IntegrationTests;

[Collection("Auth API hosts")]
public sealed class AuthEndpointTests : IClassFixture<AuthApiFactory>
{
    private const string Issuer = "CustSearch.API";
    private const string Audience = "CustSearch.Web";
    private const string SigningKey = "development-only-signing-key-change-before-sharing-2026";
    private readonly HttpClient _client;

    public AuthEndpointTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIn0.")]
    public async Task MeRejectsMalformedAccessToken(string token)
    {
        using var request = CreateRequest(token);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MeRejectsExpiredAccessToken()
    {
        var token = CreateToken(DateTime.UtcNow.AddMinutes(-2));
        using var request = CreateRequest(token);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Wrong.Issuer", Audience)]
    [InlineData(Issuer, "Wrong.Audience")]
    public async Task MeRejectsWrongIssuerOrAudience(string issuer, string audience)
    {
        using var request = CreateRequest(CreateToken(DateTime.UtcNow.AddMinutes(5), issuer, audience));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MeReturnsExpiryFromValidatedAccessToken()
    {
        var expectedExpiry = DateTime.UtcNow.AddMinutes(5);
        expectedExpiry = DateTimeOffset.FromUnixTimeSeconds(
            new DateTimeOffset(expectedExpiry).ToUnixTimeSeconds()).UtcDateTime;
        using var request = CreateRequest(CreateToken(expectedExpiry));

        using var response = await _client.SendAsync(request);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, payload.RootElement.GetProperty("user").GetProperty("userId").GetInt64());
        Assert.Equal(
            expectedExpiry,
            payload.RootElement.GetProperty("accessTokenExpiresUtc").GetDateTime().ToUniversalTime());
    }

    private static HttpRequestMessage CreateRequest(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static string CreateToken(
        DateTime expiresUtc,
        string issuer = Issuer,
        string audience = Audience)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "42"),
            new Claim(JwtRegisteredClaimNames.UniqueName, "platform.admin"),
            new Claim(CustomClaimTypes.SecurityStamp, "test-security-stamp"),
            new Claim(CustomClaimTypes.UserScope, "Platform"),
            new Claim(ClaimTypes.Role, "PlatformSuperAdmin"),
        };
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            DateTime.UtcNow.AddMinutes(-5),
            expiresUtc,
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "ConnectionStrings:CustSearchDatabase",
            "Server=127.0.0.1,1;Database=TestHostOnly;Integrated Security=True;" +
            "Encrypt=True;TrustServerCertificate=True;Connect Timeout=1");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService, StubAuthenticationService>();
        });
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticatedUser> GetCurrentUserAsync(
            long userId,
            string securityStamp,
            string? ipAddress,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(42, userId);
            Assert.Equal("test-security-stamp", securityStamp);
            return Task.FromResult(new AuthenticatedUser(
                userId,
                null,
                null,
                "platform.admin",
                "Platform Admin",
                "admin@example.test",
                true,
                ["PlatformSuperAdmin"],
                [],
                []));
        }

        public Task<AuthenticationResult> LoginAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AuthenticationResult> RefreshAsync(
            string refreshToken,
            string? ipAddress,
            string correlationId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task LogoutAsync(
            string refreshToken,
            string? ipAddress,
            string correlationId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
