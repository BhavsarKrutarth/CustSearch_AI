using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CustSearch.Application.Authorization;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CustSearch.IntegrationTests;

[Collection("Auth API hosts")]
public sealed class AuthHttpFlowTests : IClassFixture<RealAuthApiFactory>, IAsyncLifetime
{
    private readonly RealAuthApiFactory _factory;
    private HttpClient _client = null!;

    public AuthHttpFlowTests(RealAuthApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LoginRefreshLogoutAndMeUseSecureCookieBoundary()
    {
        using var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantCode = "SHOP-HTTP",
            userName = "owner",
            password = "correct-password",
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginCookie = GetRefreshCookie(loginResponse);
        AssertCookieSecurityFlags(loginResponse);

        using var refreshRequest = CreateCookieRequest(HttpMethod.Post, "/api/auth/refresh", loginCookie);
        using var refreshResponse = await _client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotatedCookie = GetRefreshCookie(refreshResponse);
        Assert.NotEqual(loginCookie, rotatedCookie);
        AssertCookieSecurityFlags(refreshResponse);
        var refreshPayload = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var accessToken = refreshPayload.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var meResponse = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        using var logoutRequest = CreateCookieRequest(HttpMethod.Post, "/api/auth/logout", rotatedCookie);
        using var logoutResponse = await _client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        var deletionCookie = Assert.Single(logoutResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains("custsearch_refresh=", deletionCookie, StringComparison.Ordinal);
        Assert.Contains("path=/api/auth", deletionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", deletionCookie, StringComparison.OrdinalIgnoreCase);

        using var anonymousMeResponse = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousMeResponse.StatusCode);
    }

    [Fact]
    public async Task PermissionAndScopePoliciesReturn401Or403AndAcceptOnlyMatchingIdentity()
    {
        using var anonymous = await _client.GetAsync("/api/authorization/probe/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("Bearer", anonymous.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal("AuthenticationRequired", await ReadErrorCodeAsync(anonymous));

        var tenantToken = await LoginAndReadAccessTokenAsync("SHOP-HTTP", "owner", "correct-password");
        using var tenantProbe = await SendBearerAsync("/api/authorization/probe/tenant", tenantToken);
        using var customerProbe = await SendBearerAsync("/api/authorization/probe/customers", tenantToken);
        using var tenantAtPlatform = await SendBearerAsync("/api/authorization/probe/platform", tenantToken);
        Assert.Equal(HttpStatusCode.OK, tenantProbe.StatusCode);
        Assert.Equal(HttpStatusCode.OK, customerProbe.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tenantAtPlatform.StatusCode);
        Assert.Equal("PermissionDenied", await ReadErrorCodeAsync(tenantAtPlatform));

        var platformToken = await LoginAndReadAccessTokenAsync(null, "platform.operator", "platform-password");
        using var platformProbe = await SendBearerAsync("/api/authorization/probe/platform", platformToken);
        using var platformAtTenant = await SendBearerAsync("/api/authorization/probe/tenant", platformToken);
        using var permissionDenied = await SendBearerAsync("/api/platform/dashboard", platformToken);
        Assert.Equal(HttpStatusCode.OK, platformProbe.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, platformAtTenant.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, permissionDenied.StatusCode);

        using var malformedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/platform/tenants")
        {
            Content = JsonContent.Create(new { }),
        };
        malformedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        using var malformedResponse = await _client.SendAsync(malformedRequest);
        Assert.Equal(HttpStatusCode.BadRequest, malformedResponse.StatusCode);
    }

    [Fact]
    public async Task SuspendedTenantRejectsExistingAccessAndRevokesRefreshSessions()
    {
        var token = await LoginAndReadAccessTokenAsync("SHOP-HTTP", "owner", "correct-password");
        await _factory.SetTenantSuspensionAsync(true);
        try
        {
            using var response = await SendBearerAsync("/api/authorization/probe/tenant", token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            await using var scope = _factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
            var ownerId = await context.UserAccounts
                .Where(user => user.UserName == "owner")
                .Select(user => user.Id)
                .SingleAsync();
            Assert.Equal(0, await context.RefreshTokens.CountAsync(
                refresh => refresh.UserId == ownerId && refresh.RevokedUtc == null));
            Assert.Contains(
                await context.AuthenticationEvents.ToListAsync(),
                audit => audit.EventType == "AccessSessionRejected"
                    && audit.FailureCode == "TenantUnavailable");
        }
        finally
        {
            await _factory.SetTenantSuspensionAsync(false);
        }
    }

    [Fact]
    public async Task InactiveTenantRejectsExistingAccessAndRevokesRefreshSessions()
    {
        var token = await LoginAndReadAccessTokenAsync("SHOP-HTTP", "owner", "correct-password");
        await _factory.SetTenantInactiveAsync(true);
        try
        {
            using var response = await SendBearerAsync("/api/authorization/probe/tenant", token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            await _factory.AssertSessionRejectionAsync("owner", "TenantUnavailable");
        }
        finally
        {
            await _factory.SetTenantInactiveAsync(false);
        }
    }

    [Fact]
    public async Task DisabledUserRejectsExistingAccessAndAuditsTheSession()
    {
        const string userName = "disabled.session.user";
        await _factory.SeedTenantUserAsync(userName, "user-password");
        var token = await LoginAndReadAccessTokenAsync("SHOP-HTTP", userName, "user-password");
        await _factory.InvalidateUserSessionAsync(userName, disable: true);

        using var response = await SendBearerAsync("/api/authorization/probe/customers", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _factory.AssertSessionRejectionAsync(userName, "UserDisabled");
    }

    [Fact]
    public async Task ChangedSecurityStampRejectsExistingAccessAndAuditsTheSession()
    {
        const string userName = "changed.stamp.user";
        await _factory.SeedTenantUserAsync(userName, "user-password");
        var token = await LoginAndReadAccessTokenAsync("SHOP-HTTP", userName, "user-password");
        await _factory.InvalidateUserSessionAsync(userName, disable: false);

        using var response = await SendBearerAsync("/api/authorization/probe/customers", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _factory.AssertSessionRejectionAsync(userName, "SessionRevoked");
    }

    [Fact]
    public async Task ChangedSecurityStampRejectsRefreshCookieWithoutAccessTokenRequest()
    {
        const string userName = "direct.refresh.stamp.user";
        await _factory.SeedTenantUserAsync(userName, "user-password");
        using var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantCode = "SHOP-HTTP",
            userName,
            password = "user-password",
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var refreshCookie = GetRefreshCookie(loginResponse);
        await _factory.InvalidateUserSessionAsync(userName, disable: false);

        using var refreshRequest = CreateCookieRequest(HttpMethod.Post, "/api/auth/refresh", refreshCookie);
        using var refreshResponse = await _client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        Assert.Equal("SessionRevoked", await ReadErrorCodeAsync(refreshResponse));
        await _factory.AssertRefreshRejectionAsync(userName, "SessionRevoked");
    }

    [Fact]
    public async Task DeactivatedRoleRemovesPermissionBeforeAccessTokenExpires()
    {
        const string userName = "revoked.role.user";
        const string roleName = "TemporaryCustomerReader";
        await _factory.SeedTenantUserAsync(userName, "user-password", roleName);
        var token = await LoginAndReadAccessTokenAsync("SHOP-HTTP", userName, "user-password");
        using var initiallyAllowed = await SendBearerAsync("/api/authorization/probe/customers", token);
        Assert.Equal(HttpStatusCode.OK, initiallyAllowed.StatusCode);

        await _factory.DeactivateRoleAsync(roleName);
        using var denied = await SendBearerAsync("/api/authorization/probe/customers", token);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("PermissionDenied", await ReadErrorCodeAsync(denied));
    }

    [Fact]
    public async Task PlatformTenantHttpFlowCreatesReadsRejectsStaleUpdateAndSuspends()
    {
        var token = await LoginAndReadAccessTokenAsync(null, "platform.operator", "platform-password");
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/platform/tenants")
        {
            Content = JsonContent.Create(new
            {
                legalName = "HTTP Phase Four Retail Pvt Ltd",
                displayName = "HTTP Phase Four Retail",
                primaryContactName = "HTTP Owner",
                primaryEmail = "phase4.owner@example.test",
                primaryMobile = "+919888888888",
                countryCode = "IN",
                timeZone = "Asia/Kolkata",
                currencyCode = "INR",
            }),
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement;
        var tenantId = created.GetProperty("id").GetInt64();
        var originalVersion = created.GetProperty("version").GetString()!;
        Assert.StartsWith("TEN-", created.GetProperty("tenantCode").GetString(), StringComparison.Ordinal);

        using var detail = await SendBearerAsync($"/api/platform/tenants/{tenantId}", token);
        using var list = await SendBearerAsync("/api/platform/tenants?page=1&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var updateBody = new
        {
            legalName = "HTTP Phase Four Retail Pvt Ltd",
            displayName = "HTTP Retail Updated",
            primaryContactName = "HTTP Owner",
            primaryEmail = "phase4.owner@example.test",
            primaryMobile = "+919888888888",
            countryCode = "IN",
            timeZone = "Asia/Kolkata",
            currencyCode = "INR",
            expectedVersion = originalVersion,
        };
        using var update = await SendBearerJsonAsync(HttpMethod.Put, $"/api/platform/tenants/{tenantId}", token, updateBody);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = JsonDocument.Parse(await update.Content.ReadAsStringAsync()).RootElement;
        var updatedVersion = updated.GetProperty("version").GetString()!;

        using var stale = await SendBearerJsonAsync(HttpMethod.Put, $"/api/platform/tenants/{tenantId}", token, updateBody);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("ConcurrencyConflict", await ReadErrorCodeAsync(stale));

        using var suspended = await SendBearerJsonAsync(
            HttpMethod.Post,
            $"/api/platform/tenants/{tenantId}/suspend",
            token,
            new { expectedVersion = updatedVersion, reason = "HTTP compliance test" });
        Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        var suspendedPayload = JsonDocument.Parse(await suspended.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Suspended", suspendedPayload.GetProperty("status").GetString());
    }

    private async Task<string> LoginAndReadAccessTokenAsync(string? tenantCode, string userName, string password)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantCode,
            userName,
            password,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> SendBearerAsync(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendBearerJsonAsync(
        HttpMethod method,
        string path,
        string token,
        object body)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private static async Task<string> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("code").GetString()!;
    }

    private static HttpRequestMessage CreateCookieRequest(HttpMethod method, string path, string cookie)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", cookie);
        return request;
    }

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        return setCookie.Split(';', 2, StringSplitOptions.TrimEntries)[0];
    }

    private static void AssertCookieSecurityFlags(HttpResponseMessage response)
    {
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class RealAuthApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private bool _initialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Apply before the minimal-host entry point reads configuration during startup.
        builder.UseSetting("AuthRateLimiting:PermitLimit", "1000");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:RefreshCookie:Secure"] = "true",
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CustSearchDbContext>();
            services.RemoveAll<DbContextOptions<CustSearchDbContext>>();
            services.AddSingleton(_connection);
            services.AddDbContext<CustSearchDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _connection.OpenAsync();
        _ = Services;
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        await context.Database.EnsureCreatedAsync();
        if (!await context.Tenants.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var tenant = Tenant.Create(
                "SHOP-HTTP", "HTTP Shop Pvt Ltd", "HTTP Shop", "Asia/Kolkata", now);
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
            var hasher = new PasswordHasher<UserAccount>();
            var user = UserAccount.CreateTenant(
                tenant.Id, "owner", "owner@http.example", "HTTP Owner", "temporary", now);
            user.SetPasswordHash(hasher.HashPassword(user, "correct-password"));
            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();

            var platformUser = UserAccount.CreatePlatform(
                "platform.operator",
                "platform.operator@example.test",
                "Platform Operator",
                "temporary",
                now);
            platformUser.SetPasswordHash(hasher.HashPassword(platformUser, "platform-password"));
            var tenantRole = Role.CreateTenant(
                tenant.Id,
                "Manager",
                "Tenant manager with customer visibility.",
                true,
                now);
            var platformRole = Role.CreatePlatform(
                "PlatformOperationsAdmin",
                "Platform operations administrator.",
                true,
                now);
            var customerView = Permission.Create(
                UserScope.Tenant,
                PermissionCatalog.Operations.CustomersView,
                "Can view tenant customers.",
                now);
            var tenantsView = Permission.Create(
                UserScope.Platform,
                PermissionCatalog.Platform.TenantsView,
                "Can view platform tenants.",
                now);
            var tenantsCreate = Permission.Create(
                UserScope.Platform,
                PermissionCatalog.Platform.TenantsCreate,
                "Can create platform tenants.",
                now);
            var tenantsEdit = Permission.Create(
                UserScope.Platform,
                PermissionCatalog.Platform.TenantsEdit,
                "Can edit platform tenants.",
                now);
            var tenantsSuspend = Permission.Create(
                UserScope.Platform,
                PermissionCatalog.Platform.TenantsSuspend,
                "Can suspend platform tenants.",
                now);
            context.AddRange(
                platformUser,
                tenantRole,
                platformRole,
                customerView,
                tenantsView,
                tenantsCreate,
                tenantsEdit,
                tenantsSuspend);
            await context.SaveChangesAsync();
            context.AddRange(
                UserRole.Assign(user, tenantRole, now),
                UserRole.Assign(platformUser, platformRole, now),
                RolePermission.Grant(tenantRole, customerView),
                RolePermission.Grant(platformRole, tenantsView),
                RolePermission.Grant(platformRole, tenantsCreate),
                RolePermission.Grant(platformRole, tenantsEdit),
                RolePermission.Grant(platformRole, tenantsSuspend));
            await context.SaveChangesAsync();
        }

        _initialized = true;
    }

    public async Task SetTenantSuspensionAsync(bool suspended)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var tenant = await context.Tenants.SingleAsync(candidate => candidate.TenantCode == "SHOP-HTTP");
        if (suspended)
        {
            tenant.Suspend();
        }
        else
        {
            tenant.Activate();
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Changes active state independently from suspension for session-policy tests.</summary>
    public async Task SetTenantInactiveAsync(bool inactive)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var tenant = await context.Tenants.SingleAsync(candidate => candidate.TenantCode == "SHOP-HTTP");
        if (inactive)
        {
            tenant.Deactivate();
        }
        else
        {
            tenant.Activate();
        }

        await context.SaveChangesAsync();
    }

    public async Task SeedTenantUserAsync(string userName, string password, string? uniqueRoleName = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var tenant = await context.Tenants.SingleAsync(candidate => candidate.TenantCode == "SHOP-HTTP");
        var role = uniqueRoleName is null
            ? await context.Roles.SingleAsync(candidate => candidate.TenantId == tenant.Id && candidate.Name == "Manager")
            : Role.CreateTenant(
                tenant.Id,
                uniqueRoleName,
                "A test-only role used to verify immediate permission revocation.",
                false,
                DateTime.UtcNow);
        if (uniqueRoleName is not null)
        {
            context.Roles.Add(role);
            await context.SaveChangesAsync();
            var customerPermission = await context.Permissions.SingleAsync(
                permission => permission.Name == PermissionCatalog.Operations.CustomersView);
            context.RolePermissions.Add(RolePermission.Grant(role, customerPermission));
            await context.SaveChangesAsync();
        }
        var hasher = new PasswordHasher<UserAccount>();
        var user = UserAccount.CreateTenant(
            tenant.Id,
            userName,
            $"{userName}@example.test",
            userName,
            "temporary",
            DateTime.UtcNow);
        user.SetPasswordHash(hasher.HashPassword(user, password));
        context.UserAccounts.Add(user);
        await context.SaveChangesAsync();
        context.UserRoles.Add(UserRole.Assign(user, role, DateTime.UtcNow));
        await context.SaveChangesAsync();
    }

    public async Task DeactivateRoleAsync(string roleName)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var role = await context.Roles.SingleAsync(candidate => candidate.Name == roleName);
        role.Deactivate();
        await context.SaveChangesAsync();
    }

    public async Task InvalidateUserSessionAsync(string userName, bool disable)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var user = await context.UserAccounts.SingleAsync(candidate => candidate.UserName == userName);
        if (disable)
        {
            user.Deactivate();
        }
        else
        {
            user.SetPasswordHash(new PasswordHasher<UserAccount>().HashPassword(user, "changed-password"));
        }

        await context.SaveChangesAsync();
    }

    public async Task AssertSessionRejectionAsync(string userName, string failureCode)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var userId = await context.UserAccounts
            .Where(user => user.UserName == userName)
            .Select(user => user.Id)
            .SingleAsync();
        Assert.Equal(0, await context.RefreshTokens.CountAsync(
            refresh => refresh.UserId == userId && refresh.RevokedUtc == null));
        Assert.Contains(
            await context.AuthenticationEvents.Where(audit => audit.UserId == userId).ToListAsync(),
            audit => audit.EventType == "AccessSessionRejected"
                && audit.FailureCode == failureCode
                && !string.IsNullOrWhiteSpace(audit.CorrelationId)
                && audit.CorrelationId != "access-session-validation");
    }

    public async Task AssertRefreshRejectionAsync(string userName, string failureCode)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CustSearchDbContext>();
        var userId = await context.UserAccounts
            .Where(user => user.UserName == userName)
            .Select(user => user.Id)
            .SingleAsync();
        Assert.Equal(0, await context.RefreshTokens.CountAsync(
            refresh => refresh.UserId == userId && refresh.RevokedUtc == null));
        Assert.Contains(
            await context.AuthenticationEvents.Where(audit => audit.UserId == userId).ToListAsync(),
            audit => audit.EventType == "RefreshFailed" && audit.FailureCode == failureCode);
    }
}
