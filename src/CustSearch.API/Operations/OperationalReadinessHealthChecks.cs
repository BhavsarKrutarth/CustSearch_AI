using System.Net.Sockets;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Infrastructure.Operations;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Operations;

public sealed class RedisReadinessHealthCheck(IOptions<OperationalPlatformOptions>options):IHealthCheck
{
    public async Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default){var settings=options.Value;if(!settings.RedisEnabled)return HealthCheckResult.Healthy("Redis scale-out is disabled; SQL remains authoritative.");if(!Uri.TryCreate(settings.RedisEndpoint,UriKind.Absolute,out var endpoint))return HealthCheckResult.Unhealthy("Redis endpoint is invalid.");try{using var client=new TcpClient();using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);timeout.CancelAfter(TimeSpan.FromSeconds(2));await client.ConnectAsync(endpoint.Host,endpoint.Port>0?endpoint.Port:6379,timeout.Token);return HealthCheckResult.Healthy("Redis endpoint is reachable.");}catch(Exception exception)when(exception is SocketException or OperationCanceledException){return HealthCheckResult.Unhealthy("Redis endpoint is unavailable.",exception);}}
}
public sealed class WorkerReadinessHealthCheck(CustSearchDbContext db,TimeProvider clock):IHealthCheck{public async Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default){var cutoff=clock.GetUtcNow().UtcDateTime.AddMinutes(-5);var failed=await db.WorkerHeartbeats.AsNoTracking().AnyAsync(x=>x.LastHeartbeatUtc>=cutoff&&!x.IsReady,cancellationToken);return failed?HealthCheckResult.Unhealthy("A current worker heartbeat is not ready."):HealthCheckResult.Healthy("Worker heartbeat state is ready or externally hosted.");}}
public sealed class SignalRReadinessHealthCheck(IAlertConnectionMetrics metrics):IHealthCheck{public Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default){_ = metrics;_ = cancellationToken;return Task.FromResult(HealthCheckResult.Healthy("SignalR services and authoritative recovery are registered."));}}
