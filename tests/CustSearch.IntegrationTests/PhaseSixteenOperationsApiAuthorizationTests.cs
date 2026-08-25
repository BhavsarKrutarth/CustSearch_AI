using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.Operations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CustSearch.IntegrationTests;

[Collection("Auth API hosts")]
public sealed class PhaseSixteenOperationsApiAuthorizationTests:IClassFixture<PhaseSixteenOperationsApiFactory>
{
    private readonly HttpClient client;
    public PhaseSixteenOperationsApiAuthorizationTests(PhaseSixteenOperationsApiFactory factory)=>client=factory.CreateClient(new(){AllowAutoRedirect=false});

    [Theory]
    [InlineData("/api/platform/operations/health")]
    [InlineData("/api/tenant/operations/settings")]
    public async Task OperationsRejectAnonymousCaller(string path)=>Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);

    [Fact]
    public async Task ScopeAndPermissionsAreBothEnforced()
    {
        using var missing=Request("/api/platform/operations/health",Token(true));Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(missing)).StatusCode);
        using var tenantToPlatform=Request("/api/platform/operations/health",Token(false,PermissionCatalog.Platform.SystemHealthView));Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(tenantToPlatform)).StatusCode);
        using var platformToTenant=Request("/api/tenant/operations/settings",Token(true,PermissionCatalog.Operations.SettingsView));Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(platformToTenant)).StatusCode);
        using var allowed=Request("/api/platform/operations/health",Token(true,PermissionCatalog.Platform.SystemHealthView));Assert.Equal(HttpStatusCode.OK,(await client.SendAsync(allowed)).StatusCode);
    }

    [Fact]
    public async Task TenantEndpointIgnoresClientTenantIdAndAcceptsOnlyStoreFilter()
    {
        using var request=Request("/api/tenant/operations/settings?tenantId=999&storeId=601",Token(false,PermissionCatalog.Operations.SettingsView));
        Assert.Equal(HttpStatusCode.OK,(await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task InvalidSettingBodyReturnsBadRequest()
    {
        using var request=Request("/api/platform/operations/settings/WebhookRetryCount",Token(true,PermissionCatalog.Platform.SettingsManage));request.Method=HttpMethod.Put;request.Content=new StringContent("{}",Encoding.UTF8,"application/json");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(request)).StatusCode);
    }

    private static HttpRequestMessage Request(string path,string token){var request=new HttpRequestMessage(HttpMethod.Get,path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);return request;}
    private static string Token(bool platform,params string[]permissions)
    {
        var stamp=$"phase16|{(platform?"p":"t")}|{string.Join(',',permissions)}";var claims=new List<Claim>{new(JwtRegisteredClaimNames.Sub,"16001"),new(JwtRegisteredClaimNames.UniqueName,"phase16.test"),new(CustomClaimTypes.SecurityStamp,stamp),new(CustomClaimTypes.UserScope,platform?"Platform":"Tenant"),new(ClaimTypes.Role,platform?"PlatformSuperAdmin":"TenantAdmin")};if(!platform){claims.Add(new(CustomClaimTypes.TenantId,"501"));claims.Add(new(CustomClaimTypes.StoreId,"601"));}claims.AddRange(permissions.Select(x=>new Claim(CustomClaimTypes.Permission,x)));var jwt=new JwtSecurityToken("CustSearch.API","CustSearch.Web",claims,DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("development-only-signing-key-change-before-sharing-2026")),SecurityAlgorithms.HmacSha256));return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public sealed class PhaseSixteenOperationsApiFactory:WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");builder.ConfigureTestServices(services=>{services.RemoveAll<IAuthenticationService>();services.AddScoped<IAuthenticationService,ClaimEcho>();services.RemoveAll<IOperationalPlatformService>();services.AddSingleton<IOperationalPlatformService,StubOperations>();});
    }
    private sealed class ClaimEcho:IAuthenticationService
    {
        public Task<AuthenticatedUser>GetCurrentUserAsync(long userId,string securityStamp,string?ipAddress,string correlationId,CancellationToken cancellationToken=default){var parts=securityStamp.Split('|',3);var platform=parts[1]=="p";var permissions=parts[2].Length>0?parts[2].Split(','):[];return Task.FromResult(new AuthenticatedUser(userId,platform?null:501,null,"phase16.test","Phase 16","phase16@invalid.test",platform,[platform?"PlatformSuperAdmin":"TenantAdmin"],permissions,platform?[]:[601L]));}
        public Task<AuthenticationResult>LoginAsync(LoginCommand command,CancellationToken cancellationToken=default)=>throw new NotSupportedException();public Task<AuthenticationResult>RefreshAsync(string refreshToken,string?ipAddress,string correlationId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();public Task LogoutAsync(string refreshToken,string?ipAddress,string correlationId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
    private sealed class StubOperations:IOperationalPlatformService
    {
        public Task<IReadOnlyList<SystemSettingView>>ListPlatformSettingsAsync(CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<SystemSettingView>>([]);
        public Task<IReadOnlyList<SystemSettingView>>ListTenantSettingsAsync(long?storeId,bool effective,CancellationToken ct=default)=>storeId==601?Task.FromResult<IReadOnlyList<SystemSettingView>>([]):Task.FromException<IReadOnlyList<SystemSettingView>>(new InvalidOperationException("Unexpected store scope."));
        public Task<SystemSettingView>SavePlatformSettingAsync(SaveSystemSettingCommand command,CustSearch.Application.TenantOperations.TenantAuditContext audit,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<SystemSettingView>SaveTenantSettingAsync(long?storeId,SaveSystemSettingCommand command,CustSearch.Application.TenantOperations.TenantAuditContext audit,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<AuditLogPage>SearchPlatformAuditAsync(AuditLogQuery query,CancellationToken ct=default)=>Task.FromResult(new AuditLogPage([],0,query.PageNumber,query.PageSize));
        public Task<AuditLogPage>SearchTenantAuditAsync(AuditLogQuery query,CancellationToken ct=default)=>Task.FromResult(new AuditLogPage([],0,query.PageNumber,query.PageSize));
        public Task<SystemHealthView>GetSystemHealthAsync(CancellationToken ct=default)=>Task.FromResult(new SystemHealthView(new("test","test","test",DateTime.UtcNow,"Healthy"),[],new(0,0,0,0),new(0,0,0)));
    }
}
