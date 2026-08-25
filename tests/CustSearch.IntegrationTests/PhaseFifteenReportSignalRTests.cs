using CustSearch.API.ReportsExports;
using CustSearch.Application.ReportsExports;
using Microsoft.AspNetCore.SignalR;

namespace CustSearch.IntegrationTests;

public sealed class PhaseFifteenReportSignalRTests
{
    [Fact]
    public async Task PublisherTargetsOnlyServerDerivedRequesterGroup()
    {
        var clients=new CapturingHubClients();var publisher=new SignalRReportExportPublisher(new StubHubContext(clients));var message=new ReportExportRealtimeEvent(7,19,501,42,"ReportExportReady",ReportExportStatus.Completed,100,DateTime.UtcNow);
        await publisher.PublishAsync(message);
        Assert.Equal("report-user:42",clients.GroupName);Assert.Same(message,clients.CapturedClient.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RequesterGroupRejectsInvalidIdentity(long userId)=>Assert.Throws<ArgumentOutOfRangeException>(()=>ReportExportGroup.User(userId));

    private sealed class CapturingClient:IReportExportClient
    {
        public ReportExportRealtimeEvent?Message{get;private set;}
        public Task ReportExportEvent(ReportExportRealtimeEvent message){Message=message;return Task.CompletedTask;}
    }
    private sealed class CapturingHubClients:IHubClients<IReportExportClient>
    {
        public CapturingClient CapturedClient{get;}=new();public string?GroupName{get;private set;}
        IReportExportClient IHubClients<IReportExportClient>.All=>CapturedClient;
        public IReportExportClient AllExcept(IReadOnlyList<string>excludedConnectionIds)=>CapturedClient;
        public IReportExportClient Client(string connectionId)=>CapturedClient;
        public IReportExportClient Clients(IReadOnlyList<string>connectionIds)=>CapturedClient;
        public IReportExportClient Group(string groupName){GroupName=groupName;return CapturedClient;}
        public IReportExportClient GroupExcept(string groupName,IReadOnlyList<string>excludedConnectionIds)=>CapturedClient;
        public IReportExportClient Groups(IReadOnlyList<string>groupNames)=>CapturedClient;
        public IReportExportClient User(string userId)=>CapturedClient;
        public IReportExportClient Users(IReadOnlyList<string>userIds)=>CapturedClient;
    }
    private sealed class StubHubContext(CapturingHubClients clients):IHubContext<ReportExportHub,IReportExportClient>
    {
        public IHubClients<IReportExportClient>Clients=>clients;
        public IGroupManager Groups{get;}=new StubGroupManager();
    }
    private sealed class StubGroupManager:IGroupManager
    {
        public Task AddToGroupAsync(string connectionId,string groupName,CancellationToken cancellationToken=default)=>Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId,string groupName,CancellationToken cancellationToken=default)=>Task.CompletedTask;
    }
}
