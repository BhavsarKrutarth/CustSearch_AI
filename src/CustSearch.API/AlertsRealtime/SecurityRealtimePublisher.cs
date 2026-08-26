using CustSearch.Application.Security;
using Microsoft.AspNetCore.SignalR;

namespace CustSearch.API.AlertsRealtime;
public sealed class SecurityRealtimePublisher(IHubContext<AlertHub,IAlertClient>hub):ISecurityRealtimePublisher
{
    public Task PublishAsync(SecurityRealtimeEvent message,CancellationToken ct=default)=>hub.Clients.Group(AlertGroupNames.Store(message.StoreId)).SecurityEvent(message);
}
