using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseThirteenTrackingEntityTests
{
    private static readonly DateTime Now=new(2026,8,25,9,0,0,DateTimeKind.Utc);
    [Fact]public void CameraStoresOpaqueReferenceAndGuardsInactiveHeartbeat(){var camera=Camera.Create(1,2,"entry-1","Entry","vault:cameras/entry-1",null,CameraDirection.Entry,true,Now);Assert.Equal("vault:cameras/entry-1",camera.RtspConfigurationReference);camera.Heartbeat(CameraStatus.Online,Now.AddSeconds(2));Assert.Equal(CameraStatus.Online,camera.Status);camera.Update("Entry",null,null,CameraDirection.Entry,false,Now.AddSeconds(3));Assert.Throws<InvalidOperationException>(()=>camera.Heartbeat(CameraStatus.Online,Now.AddSeconds(4)));}
    [Fact]public void ZoneVersionsAreImmutableAndSuperseded(){var zone=CameraZoneConfiguration.Create(1,2,3,"checkout","Checkout",CameraZoneType.Checkout,"{\"points\":[[0,0],[1,0],[1,1]]}",1,null,Now);zone.Supersede(Now.AddMinutes(1));Assert.False(zone.IsActive);Assert.Equal(Now.AddMinutes(1),zone.SupersededUtc);Assert.Throws<InvalidOperationException>(()=>zone.Supersede(Now.AddMinutes(2)));}
    [Fact]public void TrackStartsAnonymousAndAssociationsAreExplicitAndExclusive(){var track=PersonTrackSession.Start(1,2,3,"anonymous-1",Now,.8m);Assert.Equal(TrackingSubjectKind.Anonymous,track.SubjectKind);Assert.Null(track.CustomerId);track.AssociateCustomer(10,Now.AddSeconds(1));Assert.Equal(TrackingSubjectKind.Customer,track.SubjectKind);Assert.Equal(10,track.CustomerId);Assert.Throws<InvalidOperationException>(()=>track.AssociateStaff(11,Now.AddSeconds(2)));}
    [Fact]public void HandoffRequiresTwoCamerasAndDoesNotAssertIdentity(){var handoff=CameraTrackHandoff.Create(1,2,3,4,5,.88m,750,Now);Assert.Equal(4,handoff.FromCameraId);Assert.Equal(5,handoff.ToCameraId);Assert.Throws<ArgumentException>(()=>CameraTrackHandoff.Create(1,2,3,4,4,.8m,0,Now));}
    [Fact]public void TrackLifecycleIsBounded(){var track=PersonTrackSession.Start(1,2,3,"anonymous-2",Now,.7m);track.Observe(4,.9m,Now.AddSeconds(5),true);Assert.Equal(PersonTrackingState.Handoff,track.TrackingState);track.End(Now.AddSeconds(10));Assert.Equal(PersonTrackingState.Ended,track.TrackingState);Assert.Throws<InvalidOperationException>(()=>track.Observe(4,.9m,Now.AddSeconds(11)));}
}
