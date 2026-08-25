using CustSearch.Application.Authentication;
using CustSearch.Application.ReportsExports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CustSearch.API.ReportsExports;

/// <summary>User-specific report hub. It exposes no client-selected group membership operation.</summary>
[Authorize]
public sealed class ReportExportHub(ICurrentUserContext currentUser):Hub<IReportExportClient>
{
    public override async Task OnConnectedAsync(){if(!currentUser.IsAuthenticated||currentUser.UserId<=0)throw new HubException("An authenticated report user is required.");await Groups.AddToGroupAsync(Context.ConnectionId,ReportExportGroup.User(currentUser.UserId),Context.ConnectionAborted).ConfigureAwait(false);await base.OnConnectedAsync().ConfigureAwait(false);}
}
public interface IReportExportClient{Task ReportExportEvent(ReportExportRealtimeEvent message);}
public static class ReportExportGroup{public static string User(long userId){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);return $"report-user:{userId}";}}

/// <summary>Publishes only to the server-derived requester group carried by the durable event.</summary>
public sealed class SignalRReportExportPublisher(IHubContext<ReportExportHub,IReportExportClient>hub):IReportExportRealtimePublisher
{
    public Task PublishAsync(ReportExportRealtimeEvent message,CancellationToken ct=default){ArgumentNullException.ThrowIfNull(message);return hub.Clients.Group(ReportExportGroup.User(message.RequestedByUserId)).ReportExportEvent(message);}
}

