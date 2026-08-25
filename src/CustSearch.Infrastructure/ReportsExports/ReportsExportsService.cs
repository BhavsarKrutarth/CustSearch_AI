using System.Text.Json;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.ReportsExports;
using CustSearch.Application.ShopperCustomers;

namespace CustSearch.Infrastructure.ReportsExports;

/// <summary>Validates report allowlists and permission/store boundaries before reaching Dapper.</summary>
public sealed class ReportsExportsService(IReportsExportsRepository repository,IReportArtifactStore artifacts,ICurrentUserContext currentUser,TimeProvider clock):IReportsExportsService
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    private static readonly ReportCatalogItem[] TenantCatalog=
    [
        new("Tenant.DailyVisitors","Daily visitors","Daily known-customer visit totals.",PermissionCatalog.Tenant.ReportsView,true,true),
        new("Tenant.CurrentVisitors","Current visitors","Known customers with an open visit.",PermissionCatalog.Tenant.ReportsView,true,false),
        new("Tenant.NewCustomers","New customers","Customer profiles created in the selected period.",PermissionCatalog.Tenant.ReportsView,true,true),
        new("Tenant.ReturningCustomers","Returning customers","Customers with two or more factual visits in the selected period.",PermissionCatalog.Tenant.ReportsView,true,true),
        new("Tenant.HouseholdVisits","Household visits","Verified household members with factual customer visits.",PermissionCatalog.Operations.HouseholdsView,true,true),
        new("Tenant.RetailSales","Retail sales","Factual finalized retail sales totals.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.RetailInvoices","Retail invoices","Factual invoice totals and payment state.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.Payments","Retail payments","Recorded retail payment facts.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.PersonalSpend","Personal spend","Explicit customer item-attribution totals only.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.HouseholdSpend","Household spend","Invoices explicitly linked to a verified household.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.ProductSales","Product sales","Finalized invoice-item quantities and totals by product.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.ProductCategorySales","Product category sales","Finalized invoice-item quantities and totals by category snapshot.",PermissionCatalog.Operations.RetailReportsView,true,true),
        new("Tenant.CustomerPreferences","Customer preferences","Calculated customer preference scores with authorized customer scope.",PermissionCatalog.Operations.PreferencesView,false,true),
        new("Tenant.HouseholdPreferences","Household preferences","Explicit active household preference tags.",PermissionCatalog.Operations.PreferencesView,false,true),
        new("Tenant.CustomerJourneys","Customer journeys","Factual visit-to-invoice journey evidence.",PermissionCatalog.Operations.CustomerJourneysView,true,true),
        new("Tenant.StaffPerformance","Staff performance","Shift counts and completed minutes; not payroll truth.",PermissionCatalog.Operations.StaffPerformanceView,true,true),
        new("Tenant.CustomerDwell","Customer dwell","Consent/policy-bound known-customer tracking durations.",PermissionCatalog.Operations.DwellAnalyticsView,true,true),
        new("Tenant.VoiceCommandUsage","Voice command usage","Confirmation-controlled voice action history.",PermissionCatalog.Operations.VoiceCommandsAudit,true,true),
        new("Tenant.FamilyVisitParty","Family / Visit Party tracking","Co-visit party facts, kept separate from verified households.",PermissionCatalog.Operations.VisitPartiesView,true,true),
        new("Tenant.CameraHealth","Camera health","Configured camera state and heartbeat age.",PermissionCatalog.Operations.CamerasView,true,false),
        new("Tenant.Recognition","Recognition review","Consent-gated recognition candidate outcomes.",PermissionCatalog.Operations.RecognitionView,true,true),
        new("Tenant.Alerts","Alerts","Tenant/store alert lifecycle.",PermissionCatalog.Operations.AlertsView,true,true),
        new("Tenant.WebhookDelivery","Webhook delivery","Integration delivery outcomes without payloads or secrets.",PermissionCatalog.Operations.WebhooksView,false,true),
        new("Tenant.IntegrationSync","Integration sync","Inbound/outbound integration processing facts without payloads or secrets.",PermissionCatalog.Operations.IntegrationsView,false,true),
        new("Tenant.UserActivity","User activity","Tenant user actions from the append-only audit trail.",PermissionCatalog.Tenant.AuditView,true,true),
        new("Tenant.AuditActivity","Audit activity","Tenant operational audit trail.",PermissionCatalog.Tenant.AuditView,true,true),
    ];
    private static readonly ReportCatalogItem[] PlatformCatalog=
    [
        new("Platform.TenantOperationalSummary","Tenant operational summary","Tenant status, usage and operational totals.",PermissionCatalog.Platform.ReportsView,false,true),
        new("Platform.PlatformBillingInvoices","Platform billing invoices","CustSearch subscription invoice facts.",PermissionCatalog.Platform.ReportsView,false,true),
        new("Platform.PaymentCollection","Platform payment collection","CustSearch subscription payment facts.",PermissionCatalog.Platform.ReportsView,false,true),
        new("Platform.SubscriptionExpiry","Subscription expiry","Tenant subscription end/renewal periods.",PermissionCatalog.Platform.ReportsView,false,true),
        new("Platform.WebhookFailures","Webhook failures","Cross-tenant integration failure metadata.",PermissionCatalog.Platform.ReportsView,false,true),
        new("Platform.AuditActivity","Platform audit activity","Cross-tenant/platform audit activity.",PermissionCatalog.Platform.AuditView,false,true),
    ];
    public IReadOnlyList<ReportCatalogItem>GetTenantCatalog()=>TenantCatalog.Where(Allowed).ToArray();
    public IReadOnlyList<ReportCatalogItem>GetPlatformCatalog()=>PlatformCatalog.Where(Allowed).ToArray();
    public async Task<ReportDataView>PreviewTenantAsync(string type,ReportFilter filter,ReportRequestContext request,CancellationToken ct=default){var item=Require(TenantCatalog,type);RequirePermission(item);Validate(filter,item,false);var tenant=RequireTenant();var data=await repository.QueryTenantAsync(tenant,currentUser.StoreIds,TenantWide(),item.ReportType,filter,500,ct).ConfigureAwait(false);await AuditAsync(tenant,filter.StoreId,"ReportPreviewed",item.ReportType,null,filter,request,ct).ConfigureAwait(false);return data;}
    public async Task<ReportDataView>PreviewPlatformAsync(string type,ReportFilter filter,ReportRequestContext request,CancellationToken ct=default){RequirePlatform();var item=Require(PlatformCatalog,type);RequirePermission(item);Validate(filter,item,true);var data=await repository.QueryPlatformAsync(item.ReportType,filter,500,ct).ConfigureAwait(false);await AuditAsync(filter.TenantId,null,"PlatformReportPreviewed",item.ReportType,null,filter,request,ct).ConfigureAwait(false);return data;}
    public async Task<ReportExportJobView>QueueTenantAsync(QueueReportExportCommand command,ReportRequestContext request,CancellationToken ct=default){RequireExportPermission(PermissionCatalog.Tenant.ReportsExport);var item=Require(TenantCatalog,command.ReportType);RequirePermission(item);Validate(command.Filter,item,false);ValidateFormat(command.Format);var tenant=RequireTenant();return await repository.CreateJobAsync(tenant,currentUser.UserId,item.ReportType,JsonSerializer.Serialize(command.Filter,JsonOptions),command.Format,request,ct).ConfigureAwait(false);}
    public async Task<ReportExportJobView>QueuePlatformAsync(QueueReportExportCommand command,ReportRequestContext request,CancellationToken ct=default){RequirePlatform();RequireExportPermission(PermissionCatalog.Platform.ReportsExport);var item=Require(PlatformCatalog,command.ReportType);RequirePermission(item);Validate(command.Filter,item,true);ValidateFormat(command.Format);return await repository.CreateJobAsync(null,currentUser.UserId,item.ReportType,JsonSerializer.Serialize(command.Filter,JsonOptions),command.Format,request,ct).ConfigureAwait(false);}
    public Task<IReadOnlyList<ReportExportJobView>>ListTenantJobsAsync(ReportExportStatus?status,int take,CancellationToken ct=default){RequireExportPermission(PermissionCatalog.Tenant.ReportsExport);return repository.ListJobsAsync(RequireTenant(),currentUser.UserId,false,status,Math.Clamp(take,1,200),ct);}
    public Task<IReadOnlyList<ReportExportJobView>>ListPlatformJobsAsync(ReportExportStatus?status,int take,CancellationToken ct=default){RequirePlatform();RequireExportPermission(PermissionCatalog.Platform.ReportsExport);return repository.ListJobsAsync(null,currentUser.UserId,true,status,Math.Clamp(take,1,200),ct);}
    public Task<ReportExportDownload>OpenTenantDownloadAsync(long id,ReportRequestContext request,CancellationToken ct=default)=>OpenAsync(id,RequireTenant(),false,PermissionCatalog.Tenant.ReportsExport,request,ct);
    public Task<ReportExportDownload>OpenPlatformDownloadAsync(long id,ReportRequestContext request,CancellationToken ct=default){RequirePlatform();return OpenAsync(id,null,true,PermissionCatalog.Platform.ReportsExport,request,ct);}
    private async Task<ReportExportDownload>OpenAsync(long id,long?tenant,bool platform,string permission,ReportRequestContext request,CancellationToken ct){RequireExportPermission(permission);var job=await repository.GetJobAsync(id,tenant,currentUser.UserId,platform,ct).ConfigureAwait(false)??throw new ReportExportNotFoundException("Report export was not found.");if(job.Status!=ReportExportStatus.Completed||job.ExpiresUtc<=clock.GetUtcNow().UtcDateTime||string.IsNullOrWhiteSpace(job.StorageReference)||string.IsNullOrWhiteSpace(job.DownloadFileName)||string.IsNullOrWhiteSpace(job.ContentType)||job.ContentLength is null)throw new ReportExportNotFoundException("Report export is not available.");var content=await artifacts.OpenReadAsync(job.StorageReference,ct).ConfigureAwait(false);try{await AuditAsync(tenant,null,platform?"PlatformReportExportDownloaded":"ReportExportDownloaded",job.ReportType,job.Id,new(),request,ct).ConfigureAwait(false);return new(content,job.DownloadFileName,job.ContentType,job.ContentLength.Value);}catch{await content.DisposeAsync().ConfigureAwait(false);throw;}}
    private Task AuditAsync(long?tenant,long?store,string action,string reportType,long?jobId,ReportFilter filter,ReportRequestContext request,CancellationToken ct)=>repository.RecordAuditAsync(tenant,store,currentUser.UserId,action,"ReportExport",jobId?.ToString(System.Globalization.CultureInfo.InvariantCulture),JsonSerializer.Serialize(new{ReportType=reportType,Filter=filter},JsonOptions),request,ct);
    private static ReportCatalogItem Require(IEnumerable<ReportCatalogItem>catalog,string type)=>catalog.SingleOrDefault(x=>string.Equals(x.ReportType,type,StringComparison.Ordinal))??throw new ReportExportBusinessRuleException("Report type is not supported.");
    private void RequirePermission(ReportCatalogItem item){if(!Allowed(item))throw new UnauthorizedAccessException("The required report permission is missing.");}
    private bool Allowed(ReportCatalogItem item)=>currentUser.Permissions.Contains(item.RequiredPermission);
    private void RequireExportPermission(string permission){if(!currentUser.Permissions.Contains(permission))throw new UnauthorizedAccessException("Report export permission is required.");}
    private void RequirePlatform(){if(!currentUser.IsAuthenticated||!currentUser.IsPlatformAdmin||currentUser.TenantId is not null)throw new UnauthorizedAccessException("Platform context is required.");}
    private long RequireTenant()=>currentUser.IsAuthenticated&&!currentUser.IsPlatformAdmin&&currentUser.TenantId is>0 and var id?id:throw new UnauthorizedAccessException("Tenant context is required.");
    private bool TenantWide()=>PhaseSixAccessRules.IsTenantWide(currentUser.Roles);
    private void Validate(ReportFilter filter,ReportCatalogItem item,bool platform){ArgumentNullException.ThrowIfNull(filter);if(filter.FromUtc.HasValue&&filter.ToUtc.HasValue&&filter.FromUtc>=filter.ToUtc)throw new ReportExportBusinessRuleException("FromUtc must be earlier than ToUtc.");if(!item.SupportsStoreFilter&&filter.StoreId.HasValue)throw new ReportExportBusinessRuleException("This report does not support a store filter.");if(!item.SupportsDateFilter&&(filter.FromUtc.HasValue||filter.ToUtc.HasValue))throw new ReportExportBusinessRuleException("This report does not support date filters.");if(!platform&&filter.TenantId.HasValue)throw new ReportExportBusinessRuleException("TenantId cannot be supplied for tenant reports.");if(!platform&&filter.StoreId.HasValue&&!PhaseSixAccessRules.CanAccessStore(filter.StoreId.Value,currentUser.StoreIds,TenantWide()))throw new ReportExportNotFoundException("Store was not found.");if(platform&&filter.StoreId.HasValue)throw new ReportExportBusinessRuleException("Platform reports do not accept StoreId.");}
    private static void ValidateFormat(ReportExportFormat value){if(!Enum.IsDefined(value))throw new ReportExportBusinessRuleException("Export format is invalid.");}
}
