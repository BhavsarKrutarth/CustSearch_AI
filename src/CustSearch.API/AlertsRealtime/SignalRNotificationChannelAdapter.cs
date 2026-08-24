using System.Text.Json;
using CustSearch.Application.AlertsRealtime;
using Microsoft.AspNetCore.SignalR;

namespace CustSearch.API.AlertsRealtime;

/// <summary>Configured in-app real-time adapter. Store alerts never broadcast to the tenant-wide group.</summary>
public sealed class SignalRNotificationChannelAdapter(IHubContext<AlertHub,IAlertClient> hub):INotificationChannelAdapter
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);public string Channel=>"SignalR";
    public Task DeliverAsync(NotificationDeliveryMessage message,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(message);var payload=JsonSerializer.Deserialize<AlertRealtimeEventV1>(message.PayloadJson,JsonOptions)??throw new InvalidOperationException("SignalR outbox payload is invalid.");if(payload.TenantId!=message.TenantId||payload.StoreId!=message.StoreId||payload.EventId!=message.RealtimeEventId)throw new InvalidOperationException("SignalR outbox routing metadata is inconsistent.");var group=message.StoreId.HasValue?AlertGroupNames.Store(message.StoreId.Value):AlertGroupNames.Tenant(message.TenantId);return hub.Clients.Group(group).AlertEvent(payload);
    }
}
