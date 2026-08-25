using CustSearch.Worker;
using CustSearch.Infrastructure;
using CustSearch.Integrations;
using CustSearch.Infrastructure.ReportsExports;
using CustSearch.Application.Authentication;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Infrastructure.Operations;
using Microsoft.Extensions.Options;
using Serilog;
using System.Globalization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("CustSearchDatabase")
        ?? throw new InvalidOperationException("ConnectionStrings:CustSearchDatabase is required.");

    builder.Services.AddInfrastructure(connectionString);
    builder.Services.AddSingleton<ICurrentUserContext,WorkerCurrentUserContext>();
    builder.Services.AddSingleton<IAlertConnectionMetrics,WorkerAlertConnectionMetrics>();
    builder.Services.AddCustSearchIntegrations();
    builder.Services.AddOptions<IntegrationDispatcherOptions>().Bind(builder.Configuration.GetSection(IntegrationDispatcherOptions.SectionName)).Validate(x=>x.PollIntervalSeconds is>=1 and<=60,"IntegrationDispatcher:PollIntervalSeconds must be between 1 and 60.").Validate(x=>x.BatchSize is>=1 and<=200,"IntegrationDispatcher:BatchSize must be between 1 and 200.").ValidateOnStart();
    builder.Services.AddOptions<ReportExportOptions>().Bind(builder.Configuration.GetSection(ReportExportOptions.SectionName)).Validate(x=>x.RetentionHours is>=1 and<=168,"ReportExports:RetentionHours must be between 1 and 168.").Validate(x=>x.PollIntervalSeconds is>=1 and<=60,"ReportExports:PollIntervalSeconds must be between 1 and 60.").Validate(x=>x.LeaseSeconds is>=30 and<=3600,"ReportExports:LeaseSeconds must be between 30 and 3600.").Validate(x=>x.CleanupIntervalSeconds is>=10 and<=3600,"ReportExports:CleanupIntervalSeconds must be between 10 and 3600.").Validate(x=>x.CleanupBatchSize is>=1 and<=1000,"ReportExports:CleanupBatchSize must be between 1 and 1000.").ValidateOnStart();
    builder.Services.AddOptions<OperationalRetentionOptions>().Bind(builder.Configuration.GetSection(OperationalRetentionOptions.SectionName)).Validate(x=>x.IntervalSeconds is>=30 and<=86400,"OperationalRetention:IntervalSeconds must be between 30 and 86400.").Validate(x=>x.BatchSize is>=1 and<=1000,"OperationalRetention:BatchSize must be between 1 and 1000.").Validate(x=>x.RecognitionMetadataRetentionDays is>=0 and<=3650,"OperationalRetention:RecognitionMetadataRetentionDays must be between 0 and 3650.").ValidateOnStart();
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<IntegrationOutboxHostedService>();
    builder.Services.AddHostedService<ReportExportHostedService>();
    builder.Services.AddHostedService<ReportExportCleanupHostedService>();
    builder.Services.AddHostedService<OperationalRetentionHostedService>();

    var host = builder.Build();
    await host.RunAsync().ConfigureAwait(false);
}
catch (Exception exception)
{
    Log.Fatal(exception, "CustSearch Worker terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

/// <summary>Fail-closed identity used only to satisfy shared registrations in the non-HTTP Worker host.</summary>
file sealed class WorkerCurrentUserContext:ICurrentUserContext
{
    public bool IsAuthenticated=>false;public long UserId=>throw new InvalidOperationException("Worker has no interactive user.");public long?TenantId=>null;public bool IsPlatformAdmin=>false;public string SecurityStamp=>string.Empty;public IReadOnlySet<string>Roles=>new HashSet<string>();public IReadOnlySet<string>Permissions=>new HashSet<string>();public IReadOnlySet<long>StoreIds=>new HashSet<long>();
}

/// <summary>Worker has no SignalR connections; interactive connection metrics are API-owned.</summary>
file sealed class WorkerAlertConnectionMetrics:IAlertConnectionMetrics
{
    public long ActiveConnections(long tenantId)=>0;public long Reconnects(long tenantId)=>0;public long TotalActiveConnections()=>0;public long TotalReconnects()=>0;public void Connected(string connectionId,long tenantId){}public void Disconnected(string connectionId){}public void Reconnected(string connectionId,long tenantId){}
}
