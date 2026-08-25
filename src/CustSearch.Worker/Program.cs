using CustSearch.Worker;
using CustSearch.Infrastructure;
using CustSearch.Integrations;
using CustSearch.Application.ReportsExports;
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
    // Infrastructure also contains request-facing services. The Worker supplies a deliberately
    // unauthenticated context so DI validation succeeds while any accidental user operation still
    // fails its platform/tenant authorization guard instead of inheriting a fabricated identity.
    builder.Services.AddScoped<ICurrentUserContext, BackgroundCurrentUserContext>();
    builder.Services.AddSingleton<IAlertConnectionMetrics, BackgroundAlertConnectionMetrics>();
    builder.Services.AddCustSearchIntegrations();
    builder.Services.AddOptions<IntegrationDispatcherOptions>().Bind(builder.Configuration.GetSection(IntegrationDispatcherOptions.SectionName)).Validate(x=>x.PollIntervalSeconds is>=1 and<=60,"IntegrationDispatcher:PollIntervalSeconds must be between 1 and 60.").Validate(x=>x.BatchSize is>=1 and<=200,"IntegrationDispatcher:BatchSize must be between 1 and 200.").ValidateOnStart();
    builder.Services.AddOptions<ReportsExportsOptions>().Bind(builder.Configuration.GetSection(ReportsExportsOptions.SectionName)).Validate(x=>x.IsValid(false),"ReportsExports settings are invalid.").ValidateOnStart();
    builder.Services.AddOptions<ExportWorkerOptions>().Bind(builder.Configuration.GetSection(ExportWorkerOptions.SectionName)).Validate(x=>x.PollIntervalSeconds is>=1 and<=60&&x.BatchSize is>=1 and<=50,"ExportWorker settings are invalid.").ValidateOnStart();
    builder.Services.AddOptions<OperationalPlatformOptions>().Bind(builder.Configuration.GetSection(OperationalPlatformOptions.SectionName)).Validate(x=>x.IsValid(),"OperationalPlatform settings are invalid.").ValidateOnStart();
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<IntegrationOutboxHostedService>();
    builder.Services.AddHostedService<ExportJobsHostedService>();
    builder.Services.AddHostedService<OperationalRetentionHostedService>();
    builder.Services.AddHostedService<OperationalHeartbeatHostedService>();

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
