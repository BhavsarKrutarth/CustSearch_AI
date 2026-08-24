using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Authorization;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CustSearch.IntegrationTests;

/// <summary>Phase 11 transport contract tests for DTO injection rejection, exact permissions and SignalR routing isolation.</summary>
public sealed class PhaseElevenApiContractTests
{
    [Fact]
    public void BrowserDtosRejectUnmappedTenantIdAndNeverDeclareTenantIdentity()
    {
        foreach(var type in new[]{typeof(CreateAlertRequest),typeof(UpdateAlertRequest),typeof(AcknowledgeAlertRequest),typeof(ResolveAlertRequest)}){Assert.DoesNotContain(type.GetProperties(),x=>string.Equals(x.Name,"TenantId",StringComparison.OrdinalIgnoreCase));var attribute=type.GetCustomAttribute<JsonUnmappedMemberHandlingAttribute>();Assert.NotNull(attribute);Assert.Equal(JsonUnmappedMemberHandling.Disallow,attribute.UnmappedMemberHandling);}
        Assert.NotNull(typeof(AlertsRealtimeController).GetCustomAttribute<RejectClientTenantIdAttribute>());
        const string injected="""{"alertType":"security.threshold","storeId":9,"severity":3,"title":"Threshold","message":"Threshold reached.","entityType":"Visit","entityId":"91","deduplicationKey":"security:91","tenantId":999}""";
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CreateAlertRequest>(injected,new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Theory]
    [InlineData(nameof(AlertsRealtimeController.List),PermissionCatalog.Operations.AlertsView)]
    [InlineData(nameof(AlertsRealtimeController.Recover),PermissionCatalog.Operations.AlertsView)]
    [InlineData(nameof(AlertsRealtimeController.Get),PermissionCatalog.Operations.AlertsView)]
    [InlineData(nameof(AlertsRealtimeController.Create),PermissionCatalog.Operations.AlertsConfigure)]
    [InlineData(nameof(AlertsRealtimeController.Update),PermissionCatalog.Operations.AlertsConfigure)]
    [InlineData(nameof(AlertsRealtimeController.Acknowledge),PermissionCatalog.Operations.AlertsAcknowledge)]
    [InlineData(nameof(AlertsRealtimeController.Resolve),PermissionCatalog.Operations.AlertsConfigure)]
    [InlineData(nameof(AlertsRealtimeController.Metrics),PermissionCatalog.Operations.AlertsConfigure)]
    public void AlertEndpointsRequireExactPermissions(string method,string permission){var attribute=typeof(AlertsRealtimeController).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.NotNull(attribute);Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attribute.Policy);}

    [Fact]
    public void HubIsTenantAuthorizedAndExposesNoArbitraryGroupJoin()
    {
        var authorize=typeof(AlertHub).GetCustomAttribute<AuthorizeAttribute>();Assert.NotNull(authorize);Assert.Equal(AuthorizationPolicyNames.TenantScope,authorize.Policy);var declared=typeof(AlertHub).GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly).Select(x=>x.Name).ToArray();Assert.DoesNotContain(declared,x=>x.Contains("Join",StringComparison.OrdinalIgnoreCase)||x.Contains("Subscribe",StringComparison.OrdinalIgnoreCase)||x.Contains("Group",StringComparison.OrdinalIgnoreCase));Assert.Equal("tenant:41",AlertGroupNames.Tenant(41));Assert.Equal("store:92",AlertGroupNames.Store(92));
    }

    [Fact]
    public async Task SignalRAdapterRoutesTenantWideAndStoreEventsToDifferentServerGroups()
    {
        var context=new RecordingHubContext();var adapter=new SignalRNotificationChannelAdapter(context);await adapter.DeliverAsync(Message(1,null));Assert.Equal(["tenant:1"],context.ClientsImpl.RequestedGroups);context.ClientsImpl.RequestedGroups.Clear();await adapter.DeliverAsync(Message(2,9));Assert.Equal(["store:9"],context.ClientsImpl.RequestedGroups);Assert.Equal(2,context.Client.Events.Count);
    }

    private static NotificationDeliveryMessage Message(long eventId,long?storeId){var alert=new AlertView(eventId,"test",storeId,AlertSeverity.Info,"Title","Message","Entity",null,DateTime.UtcNow,null,null,null,AlertStatus.New,"p11",$"key:{eventId}");var envelope=new AlertRealtimeEventV1(eventId,"alert.created",1,DateTime.UtcNow,1,storeId,"p11",alert);return new(eventId,1,storeId,eventId,eventId,"alert.created",1,JsonSerializer.Serialize(envelope,new JsonSerializerOptions(JsonSerializerDefaults.Web)),"p11",$"signalr:{eventId}");}

    private sealed class RecordingHubContext:IHubContext<AlertHub,IAlertClient>{public RecordingClient Client{get;}=new();public RecordingHubClients ClientsImpl{get;}public RecordingHubContext(){ClientsImpl=new(Client);}public IHubClients<IAlertClient>Clients=>ClientsImpl;public IGroupManager Groups{get;}=new NoopGroupManager();}
    private sealed class RecordingHubClients(RecordingClient client):IHubClients<IAlertClient>{public List<string>RequestedGroups{get;}=[];public IAlertClient All=>client;public IAlertClient AllExcept(IReadOnlyList<string>excludedConnectionIds)=>client;public IAlertClient Client(string connectionId)=>client;public IAlertClient Clients(IReadOnlyList<string>connectionIds)=>client;public IAlertClient Group(string groupName){RequestedGroups.Add(groupName);return client;}public IAlertClient GroupExcept(string groupName,IReadOnlyList<string>excludedConnectionIds)=>Group(groupName);public IAlertClient Groups(IReadOnlyList<string>groupNames){RequestedGroups.AddRange(groupNames);return client;}public IAlertClient User(string userId)=>client;public IAlertClient Users(IReadOnlyList<string>userIds)=>client;}
    private sealed class RecordingClient:IAlertClient{public List<AlertRealtimeEventV1>Events{get;}=[];public Task AlertEvent(AlertRealtimeEventV1 message){Events.Add(message);return Task.CompletedTask;}public Task RealtimeReady(AlertRealtimeReadyV1 message)=>Task.CompletedTask;}
    private sealed class NoopGroupManager:IGroupManager{public Task AddToGroupAsync(string connectionId,string groupName,CancellationToken cancellationToken=default)=>Task.CompletedTask;public Task RemoveFromGroupAsync(string connectionId,string groupName,CancellationToken cancellationToken=default)=>Task.CompletedTask;}
}
