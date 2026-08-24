using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseTenPreferenceVoiceEntityTests
{
    private static readonly DateTime Now=new(2026,8,24,2,0,0,DateTimeKind.Utc);

    [Fact]
    public void PreferenceSignalKeepsFactualSourceSeparateFromDerivedScore()
    {
        var signal=CustomerPreferenceSignal.Create(1,2,3,PreferenceType.Category,9,"Banarasi Saree",80,PreferenceSignalSource.ManualStaff,95,4,"staff observation",Now);
        Assert.Equal(PreferenceSignalSource.ManualStaff,signal.Source);Assert.Equal(80,signal.SignalScore);Assert.Equal(95,signal.Confidence);Assert.True(signal.IsActive);
    }

    [Fact]
    public void PreferenceSignalRejectsMissingReferenceAndValue()=>Assert.Throws<ArgumentException>(()=>CustomerPreferenceSignal.Create(1,2,3,PreferenceType.Tag,null,null,null,PreferenceSignalSource.ManualStaff,90,4,null,Now));

    [Fact]
    public void HouseholdTagHasNoCctvOrVisitPartySource()
    {
        var values=Enum.GetNames<HouseholdPreferenceTagSource>();
        Assert.DoesNotContain(values,x=>x.Contains("Cctv",StringComparison.OrdinalIgnoreCase)||x.Contains("Visit",StringComparison.OrdinalIgnoreCase)||x.Contains("Face",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VoiceSessionWithConfirmationDoesNotConfirmDuringProposal()
    {
        var session=VoiceCommandSession.Start(1,2,3,4,"Magic Add",true,Now,Now.AddSeconds(30));
        session.Propose("Magic Add Banarasi Sadi",91,PreferenceType.Category,10,"Banarasi Saree",Now.AddSeconds(2));
        Assert.Equal(VoiceCommandSessionStatus.PendingConfirmation,session.Status);Assert.Null(session.ResolvedUtc);
        session.Confirm(Now.AddSeconds(3));Assert.Equal(VoiceCommandSessionStatus.Confirmed,session.Status);Assert.NotNull(session.ResolvedUtc);
    }

    [Fact]
    public void VoiceSessionWithoutConfirmationCanResolveImmediately()
    {
        var session=VoiceCommandSession.Start(1,2,3,4,"Smart Add",false,Now,Now.AddSeconds(30));
        session.Propose("Wedding Saree",88,PreferenceType.Category,12,"Wedding Saree",Now.AddSeconds(1));
        Assert.Equal(VoiceCommandSessionStatus.Confirmed,session.Status);Assert.NotNull(session.ResolvedUtc);
    }

    [Fact]
    public void RejectedVoiceProposalCannotBeConfirmed()
    {
        var session=VoiceCommandSession.Start(1,2,3,4,"Aasha Add",true,Now,Now.AddSeconds(30));session.Propose("Banarasi",90,PreferenceType.Category,10,"Banarasi Saree",Now.AddSeconds(1));session.Reject(Now.AddSeconds(2));
        Assert.Equal(VoiceCommandSessionStatus.Rejected,session.Status);Assert.Throws<InvalidOperationException>(()=>session.Confirm(Now.AddSeconds(3)));
    }

    [Fact]
    public void ExpiredVoiceSessionRejectsProposal()
    {
        var session=VoiceCommandSession.Start(1,2,3,4,"Aasha Add",true,Now,Now.AddSeconds(5));
        Assert.Throws<InvalidOperationException>(()=>session.Propose("Banarasi",90,PreferenceType.Category,10,"Banarasi Saree",Now.AddSeconds(6)));Assert.Equal(VoiceCommandSessionStatus.Expired,session.Status);
    }

    [Fact]
    public void CategoryAliasNormalizesSpeechButRetainsDisplayText()
    {
        var alias=ProductCategoryAlias.Create(1,2,3,"  Banarasi   SADI ","gu-IN",4,Now);
        Assert.Equal("Banarasi   SADI",alias.AliasText);Assert.Equal("banarasi sadi",alias.NormalizedAliasText);Assert.True(alias.IsActive);
    }

    [Fact]
    public void WeightVersionRejectsOutOfRangeWeight()=>Assert.Throws<ArgumentOutOfRangeException>(()=>PreferenceWeightVersion.Create(1,"W1",11,1,1,1,2,Now));
}
