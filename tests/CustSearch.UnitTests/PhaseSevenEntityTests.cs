using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseSevenEntityTests
{
    private static readonly DateTime Now=DateTime.SpecifyKind(new DateTime(2026,8,23,14,0,0),DateTimeKind.Utc);

    [Fact]
    public void HouseholdNormalizesCodeAndKeepsExplicitIdentity()
    {
        var household=Household.Create(25," hh-001 "," Shah Family "," verified at counter ",Now);
        Assert.Equal("HH-001",household.HouseholdCode); Assert.Equal("Shah Family",household.Name); Assert.True(household.IsActive);
    }

    [Fact]
    public void HouseholdMemberRequiresExplicitSupportedRelationshipSource()
    {
        var member=HouseholdMember.Link(25,10,500,"Parent",HouseholdRelationshipSource.CustomerProvided,99,Now);
        Assert.True(member.IsVerified); Assert.Equal(HouseholdRelationshipSource.CustomerProvided,member.RelationshipSource); Assert.Equal(500,member.CustomerId);
        Assert.DoesNotContain("Face",Enum.GetNames<HouseholdRelationshipSource>(),StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void VisitPartyMemberRequiresExactlyOneMatchingIdentity()
    {
        var customer=VisitPartyMember.ForCustomer(25,101,77,500,Now);
        Assert.Equal(VisitPartyMemberIdentityType.Customer,customer.IdentityType); Assert.Equal(500,customer.CustomerId); Assert.Null(customer.AnonymousVisitorId);
        var visitor=VisitPartyMember.ForAnonymousVisitor(25,101,77,900,Now);
        Assert.Equal(VisitPartyMemberIdentityType.AnonymousVisitor,visitor.IdentityType); Assert.Null(visitor.CustomerId); Assert.Equal(900,visitor.AnonymousVisitorId);
    }

    [Fact]
    public void VisitPartyDoesNotContainHouseholdRelationshipState()
    {
        var party=VisitParty.Create(25,101,"party-1",Now,VisitPartySource.CctvCoVisit);
        Assert.Equal("PARTY-1",party.PartyCode); Assert.Equal(VisitPartyStatus.Open,party.Status);
        Assert.DoesNotContain(typeof(VisitParty).GetProperties(),p=>p.Name.Contains("Household",StringComparison.OrdinalIgnoreCase)||p.Name.Contains("Family",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CustomerVisitRejectsExitBeforeEntry()
    {
        var visit=CustomerVisit.Create(25,101,500,"visit-1",Now,CustomerVisitSource.Manual);
        Assert.Throws<ArgumentOutOfRangeException>(()=>visit.Complete(Now.AddMinutes(-1)));
    }

    [Fact]
    public void CustomerVisitContainsNoPurchaseOrInvoiceFields()
    {
        var names=typeof(CustomerVisit).GetProperties().Select(x=>x.Name).ToArray();
        Assert.DoesNotContain(names,x=>x.Contains("Purchase",StringComparison.OrdinalIgnoreCase)||x.Contains("Invoice",StringComparison.OrdinalIgnoreCase)||x.Contains("Spend",StringComparison.OrdinalIgnoreCase));
    }
}