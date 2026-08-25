using CustSearch.API.Operations;
using CustSearch.Infrastructure.Operations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CustSearch.IntegrationTests;

public sealed class PhaseSixteenReadinessTests
{
    [Fact]
    public async Task RedisReadinessIsHealthyWhenOptionalScaleOutIsDisabled()
    {
        var check = new RedisReadinessHealthCheck(Options.Create(new OperationalPlatformOptions
        {
            RedisEnabled = false,
        }));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task RedisReadinessFailsClosedWhenConfiguredEndpointIsUnavailable()
    {
        var check = new RedisReadinessHealthCheck(Options.Create(new OperationalPlatformOptions
        {
            RedisEnabled = true,
            RedisEndpoint = "redis://127.0.0.1:1",
        }));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis endpoint is unavailable.", result.Description);
    }

    [Fact]
    public async Task SqlReadinessFailsClosedWhenAuthoritativeDatabaseIsUnavailable()
    {
        await using var factory = new UnavailableSqlApiFactory();
        var healthChecks = factory.Services.GetRequiredService<HealthCheckService>();

        var report = await healthChecks.CheckHealthAsync(
            registration => registration.Name == "sql-server");

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries["sql-server"].Status);
    }

    private sealed class UnavailableSqlApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                "ConnectionStrings:CustSearchDatabase",
                "Server=127.0.0.1,1;Database=CustSearch_AI;Integrated Security=True;" +
                "Encrypt=True;TrustServerCertificate=True;Connect Timeout=1");
        }
    }
}
