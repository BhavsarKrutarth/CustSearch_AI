using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.ReportsExports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CustSearch.IntegrationTests;

[Collection("Auth API hosts")]
public sealed class PhaseFifteenReportsApiAuthorizationTests:IClassFixture<PhaseFifteenReportsApiFactory>
{
    private readonly HttpClient client;
    public PhaseFifteenReportsApiAuthorizationTests(PhaseFifteenReportsApiFactory factory)=>client=factory.CreateClient(new(){AllowAutoRedirect=false});

    [Theory]
    [InlineData("/api/tenant/reports/catalog")]
    [InlineData("/api/platform/reports/catalog")]
    public async Task CatalogRejectsAnonymousCaller(string path)=>Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync(path)).StatusCode);

    [Fact]
    public async Task PlatformCatalogRequiresExactPermission()
    {
        using var denied=Request("/api/platform/reports/catalog",Token(platform:true));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(denied)).StatusCode);
        using var allowed=Request("/api/platform/reports/catalog",Token(platform:true,PermissionCatalog.Platform.ReportsView));
        Assert.Equal(HttpStatusCode.OK,(await client.SendAsync(allowed)).StatusCode);
    }

    [Fact]
    public async Task TenantAndPlatformScopesCannotCross()
    {
        using var tenantToPlatform=Request("/api/platform/reports/catalog",Token(platform:false,PermissionCatalog.Platform.ReportsView));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(tenantToPlatform)).StatusCode);
        using var platformToTenant=Request("/api/tenant/reports/catalog",Token(platform:true,PermissionCatalog.Tenant.ReportsView));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.SendAsync(platformToTenant)).StatusCode);
        using var tenantAllowed=Request("/api/tenant/reports/catalog",Token(platform:false,PermissionCatalog.Tenant.ReportsView));
        Assert.Equal(HttpStatusCode.OK,(await client.SendAsync(tenantAllowed)).StatusCode);
    }

    [Fact]
    public async Task InvalidQueueBodyReturnsBadRequestBeforeServiceExecution()
    {
        using var request=Request("/api/tenant/reports/exports",Token(platform:false,PermissionCatalog.Tenant.ReportsExport));request.Method=HttpMethod.Post;request.Content=new StringContent("{}",Encoding.UTF8,"application/json");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task MissingRequesterOwnedDownloadReturnsNotFound()
    {
        using var request=Request("/api/platform/reports/exports/999/download",Token(platform:true,PermissionCatalog.Platform.ReportsExport));
        Assert.Equal(HttpStatusCode.NotFound,(await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task TenantQueueNeverForwardsClientSuppliedTenantId()
    {
        const string json="{\"reportType\":\"Tenant.Test\",\"format\":1,\"tenantId\":999}";using var request=Request("/api/tenant/reports/exports",Token(platform:false,PermissionCatalog.Tenant.ReportsExport));request.Method=HttpMethod.Post;request.Content=new StringContent(json,Encoding.UTF8,"application/json");
        Assert.Equal(HttpStatusCode.Accepted,(await client.SendAsync(request)).StatusCode);
    }

    private static HttpRequestMessage Request(string path,string token){var request=new HttpRequestMessage(HttpMethod.Get,path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);return request;}
    private static string Token(bool platform,params string[]permissions)
    {
        var stamp=$"phase15|{(platform?"p":"t")}|{string.Join(',',permissions)}";var claims=new List<Claim>{new(JwtRegisteredClaimNames.Sub,"15001"),new(JwtRegisteredClaimNames.UniqueName,"phase15.test"),new(CustomClaimTypes.SecurityStamp,stamp),new(CustomClaimTypes.UserScope,platform?"Platform":"Tenant"),new(ClaimTypes.Role,platform?"PlatformSuperAdmin":"TenantAdmin")};
        if(!platform){claims.Add(new(CustomClaimTypes.TenantId,"501"));claims.Add(new(CustomClaimTypes.StoreId,"601"));}
        claims.AddRange(permissions.Select(permission=>new Claim(CustomClaimTypes.Permission,permission)));
        var jwt=new JwtSecurityToken("CustSearch.API","CustSearch.Web",claims,DateTime.UtcNow.AddMinutes(-1),DateTime.UtcNow.AddMinutes(5),new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("development-only-signing-key-change-before-sharing-2026")),SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public sealed class PhaseFifteenReportsApiFactory:WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services=>
        {
            services.RemoveAll<IAuthenticationService>();services.AddScoped<IAuthenticationService,ClaimEchoAuthenticationService>();
            services.RemoveAll<IReportsExportsService>();services.AddSingleton<IReportsExportsService,StubReportsService>();
            services.RemoveAll<IReportExportEventDispatcher>();services.AddSingleton<IReportExportEventDispatcher,StubDispatcher>();
        });
    }
    private sealed class ClaimEchoAuthenticationService:IAuthenticationService
    {
        public Task<AuthenticatedUser>GetCurrentUserAsync(long userId,string securityStamp,string?ipAddress,string correlationId,CancellationToken cancellationToken=default)
        {
            var parts=securityStamp.Split('|',3);var platform=parts.Length==3&&parts[1]=="p";long?tenantId=platform?null:501L;var permissions=parts.Length==3&&parts[2].Length>0?parts[2].Split(','):[];
            return Task.FromResult(new AuthenticatedUser(userId,tenantId,null,"phase15.test","Phase 15 Test","phase15@invalid.test",platform,[platform?"PlatformSuperAdmin":"TenantAdmin"],permissions,platform?[]:[601L]));
        }
        public Task<AuthenticationResult>LoginAsync(LoginCommand command,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<AuthenticationResult>RefreshAsync(string refreshToken,string?ipAddress,string correlationId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task LogoutAsync(string refreshToken,string?ipAddress,string correlationId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
    private sealed class StubDispatcher:IReportExportEventDispatcher{public Task<int>ProcessDueAsync(int take=50,CancellationToken ct=default)=>Task.FromResult(0);}
    private sealed class StubReportsService:IReportsExportsService
    {
        public IReadOnlyList<ReportCatalogItem>GetTenantCatalog()=>[new("Tenant.Test","Test","Test",PermissionCatalog.Tenant.ReportsView,true,true)];
        public IReadOnlyList<ReportCatalogItem>GetPlatformCatalog()=>[new("Platform.Test","Test","Test",PermissionCatalog.Platform.ReportsView,false,true)];
        public Task<ReportDataView>PreviewTenantAsync(string type,ReportFilter filter,ReportRequestContext request,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<ReportDataView>PreviewPlatformAsync(string type,ReportFilter filter,ReportRequestContext request,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<ReportExportJobView>QueueTenantAsync(QueueReportExportCommand command,ReportRequestContext request,CancellationToken ct=default)=>command.Filter.TenantId is null?Task.FromResult(new ReportExportJobView(1,501,15001,command.ReportType,command.Format,ReportExportStatus.Queued,0,null,null,null,null,null,null,DateTime.UtcNow,null,null,null,0)):Task.FromException<ReportExportJobView>(new InvalidOperationException("Client TenantId crossed the tenant API boundary."));
        public Task<ReportExportJobView>QueuePlatformAsync(QueueReportExportCommand command,ReportRequestContext request,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<IReadOnlyList<ReportExportJobView>>ListTenantJobsAsync(ReportExportStatus?status,int take,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<IReadOnlyList<ReportExportJobView>>ListPlatformJobsAsync(ReportExportStatus?status,int take,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<ReportExportDownload>OpenTenantDownloadAsync(long id,ReportRequestContext request,CancellationToken ct=default)=>Task.FromException<ReportExportDownload>(new ReportExportNotFoundException("Report export was not found."));
        public Task<ReportExportDownload>OpenPlatformDownloadAsync(long id,ReportRequestContext request,CancellationToken ct=default)=>Task.FromException<ReportExportDownload>(new ReportExportNotFoundException("Report export was not found."));
    }
}
