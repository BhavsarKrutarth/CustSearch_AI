using CustSearch.Worker;
using CustSearch.Infrastructure;
using CustSearch.Integrations;
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
    builder.Services.AddCustSearchIntegrations();
    builder.Services.AddHostedService<Worker>();

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
