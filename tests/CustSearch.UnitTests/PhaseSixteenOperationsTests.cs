using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseSixteenOperationsTests
{
    private static readonly DateTime Now=new(2026,8,25,12,0,0,DateTimeKind.Utc);
    [Fact]public void OrdinarySettingsRejectSecretLikeKeys(){Assert.Throws<ArgumentException>(()=>OperationalSetting.Create(OperationalScope.Platform,null,null,"JwtSigningKey","{}",Now));}
    [Fact]public void ScopeHierarchyIsStrict(){Assert.Throws<ArgumentException>(()=>OperationalSetting.Create(OperationalScope.Store,4,null,"Reports.PageSize","100",Now));var setting=OperationalSetting.Create(OperationalScope.Store,4,8,"Reports.PageSize","100",Now);Assert.Equal(8,setting.StoreId);}
    [Fact]public void SecretReferencesStoreOnlyOpaqueReference(){var secret=OperationalSecretReference.Create(OperationalScope.Tenant,4,null,"WebhookSigningSecret","vault://tenant-4/webhook/v2",Now);Assert.Equal("vault://tenant-4/webhook/v2",secret.Reference);}
    [Fact]public void WorkerLeaseCannotBeTakenBeforeExpiry(){var lease=WorkerLease.Acquire("exports",Guid.NewGuid(),"worker-a",Now,Now.AddMinutes(1));Assert.Throws<InvalidOperationException>(()=>lease.Reassign(Guid.NewGuid(),"worker-b",Now.AddSeconds(30),Now.AddMinutes(2)));}
    [Fact]public void PausingWorkerRequiresReason(){var control=WorkerControl.Create("exports",Now);Assert.Throws<ArgumentException>(()=>control.SetPaused(true,null,1,Now));control.SetPaused(true,"maintenance",1,Now);Assert.True(control.IsPaused);}
    [Theory][InlineData(0)][InlineData(36501)]public void RetentionIsBounded(int days)=>Assert.Throws<ArgumentOutOfRangeException>(()=>RetentionPolicy.Create(RetentionDomain.Alerts,null,null,days,true,Now));
}

