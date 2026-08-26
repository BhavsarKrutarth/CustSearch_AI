using CustSearch.Application.Security;

namespace CustSearch.Worker;
public sealed class SecurityMaintenanceHostedService(IServiceScopeFactory scopes,ILogger<SecurityMaintenanceHostedService>logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopes.CreateAsyncScope();var result=await scope.ServiceProvider.GetRequiredService<ISecurityMaintenanceProcessor>().RunOnceAsync(stoppingToken).ConfigureAwait(false);logger.LogInformation("Security maintenance: notifications={Notifications}, escalations={Escalations}, evidence={Evidence}, correlations={Correlations}, stale={Stale}, open={Open}",result.NotificationsDelivered,result.EscalationsQueued,result.EvidenceExpired,result.PaymentsCorrelated,result.StaleCandidatesResolved,result.OpenCandidates);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex,"Phase 18 security maintenance failed; the next idempotent run will retry.");}await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken).ConfigureAwait(false);}}
}
