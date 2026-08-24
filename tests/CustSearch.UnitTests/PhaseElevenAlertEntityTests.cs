using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

/// <summary>Phase 11 alert/outbox lifecycle invariants independent of transport and persistence.</summary>
public sealed class PhaseElevenAlertEntityTests
{
    private static readonly DateTime Now=new(2026,8,24,15,0,0,DateTimeKind.Utc);
    [Fact]public void AlertSupportsDeliveredAcknowledgedAndResolvedLifecycle(){var alert=Alert.Create(1,2,"vip.customer",AlertSeverity.Warning,"VIP returned","Customer entered store.","Customer","9","p11","vip:9:visit:2",Now);Assert.Equal(AlertStatus.New,alert.Status);alert.MarkDelivered();Assert.Equal(AlertStatus.Delivered,alert.Status);alert.Acknowledge(7,Now.AddSeconds(1));Assert.Equal(AlertStatus.Acknowledged,alert.Status);Assert.Equal(7,alert.AcknowledgedByUserId);alert.Resolve(Now.AddSeconds(2));Assert.Equal(AlertStatus.Resolved,alert.Status);Assert.Throws<InvalidOperationException>(()=>alert.Update(AlertSeverity.Info,"x","y","Customer","9"));}
    [Fact]public void AlertRejectsNonUtcAndMissingDeduplicationIdentity(){Assert.Throws<ArgumentException>(()=>Alert.Create(1,null,"test",AlertSeverity.Info,"Title","Message","Entity",null,"corr","key",DateTime.Now));Assert.Throws<ArgumentException>(()=>Alert.Create(1,null,"test",AlertSeverity.Info,"Title","Message","Entity",null,"corr"," ",Now));}
    [Fact]public void OutboxRetriesThenDeadLettersAtConfiguredAttemptLimit(){var message=NotificationOutboxMessage.Queue(1,2,3,4,"SignalR","alert.created",1,"{}","p11","signalr:4",Now);message.StartAttempt(Now,TimeSpan.FromMinutes(2));message.MarkFailed("failure",Now,2,TimeSpan.FromSeconds(5));Assert.Equal(NotificationOutboxStatus.Failed,message.Status);Assert.Equal(Now.AddSeconds(5),message.NextAttemptUtc);message.StartAttempt(Now.AddSeconds(5),TimeSpan.FromMinutes(2));message.MarkFailed("failure",Now.AddSeconds(5),2,TimeSpan.FromSeconds(10));Assert.Equal(NotificationOutboxStatus.DeadLetter,message.Status);Assert.Equal(2,message.AttemptCount);}
    [Fact]public void DeliveredOutboxMessageIsTerminal(){var message=NotificationOutboxMessage.Queue(1,null,3,4,"SignalR","alert.created",1,"{}","p11","signalr:4",Now);message.StartAttempt(Now,TimeSpan.FromMinutes(2));message.MarkDelivered(Now.AddSeconds(1));Assert.Equal(NotificationOutboxStatus.Delivered,message.Status);Assert.Throws<InvalidOperationException>(()=>message.StartAttempt(Now.AddMinutes(3),TimeSpan.FromMinutes(2)));}
}
