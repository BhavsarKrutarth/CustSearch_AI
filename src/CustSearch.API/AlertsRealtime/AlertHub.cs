using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CustSearch.API.AlertsRealtime;

/// <summary>Authenticated server-controlled alert hub. It exposes no client-selectable group join operation.</summary>
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
public sealed class AlertHub(ICurrentUserContext currentUser,IAlertConnectionMetrics metrics):Hub<IAlertClient>
{
    public override async Task OnConnectedAsync()
    {
        var tenantId=RequireTenant();metrics.Connected(Context.ConnectionId,tenantId);await Groups.AddToGroupAsync(Context.ConnectionId,AlertGroupNames.Tenant(tenantId),Context.ConnectionAborted).ConfigureAwait(false);foreach(var storeId in currentUser.StoreIds)await Groups.AddToGroupAsync(Context.ConnectionId,AlertGroupNames.Store(storeId),Context.ConnectionAborted).ConfigureAwait(false);await Clients.Caller.RealtimeReady(new(1,Context.ConnectionId,DateTime.UtcNow)).ConfigureAwait(false);await base.OnConnectedAsync().ConfigureAwait(false);
    }
    public override async Task OnDisconnectedAsync(Exception?exception){metrics.Disconnected(Context.ConnectionId);await base.OnDisconnectedAsync(exception).ConfigureAwait(false);}
    /// <summary>Records a reconnect after the client has re-established its server-authorized memberships; it never changes groups.</summary>
    public Task ReportReconnect(long lastEventId){if(lastEventId<0)throw new HubException("Recovery cursor cannot be negative.");metrics.Reconnected(Context.ConnectionId,RequireTenant());return Task.CompletedTask;}
    private long RequireTenant()=>currentUser.IsAuthenticated&&!currentUser.IsPlatformAdmin&&currentUser.TenantId is>0?currentUser.TenantId.Value:throw new HubException("A tenant session is required.");
}

/// <summary>Strongly typed methods emitted by the Phase 11 alert hub.</summary>
public interface IAlertClient
{
    Task AlertEvent(AlertRealtimeEventV1 message);
    Task RealtimeReady(AlertRealtimeReadyV1 message);
}

public sealed record AlertRealtimeReadyV1(int ContractVersion,string ConnectionId,DateTime ConnectedUtc);

/// <summary>Stable group names are derived only from validated server identity.</summary>
public static class AlertGroupNames
{
    public static string Tenant(long tenantId){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);return $"tenant:{tenantId}";}
    public static string Store(long storeId){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);return $"store:{storeId}";}
}
