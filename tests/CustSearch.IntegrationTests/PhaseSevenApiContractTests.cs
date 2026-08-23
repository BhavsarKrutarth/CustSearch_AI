using System.Reflection;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

/// <summary>Phase 7 API contracts keep tenant identity server-owned and household membership explicitly customer-based.</summary>
public sealed class PhaseSevenApiContractTests
{
    [Fact]
    public void PhaseSevenBrowserRequestsNeverExposeTenantId()
    {
        var types=new[]{typeof(CreateHouseholdRequest),typeof(UpdateHouseholdRequest),typeof(SaveHouseholdMemberRequest),typeof(UpdateHouseholdMemberRequest),typeof(CreateCustomerVisitRequest),typeof(CompleteCustomerVisitRequest)};
        foreach(var type in types)Assert.DoesNotContain(type.GetProperties(),p=>string.Equals(p.Name,"TenantId",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HouseholdMemberRequestCannotDirectlyLinkAnonymousVisitor()
    {
        Assert.DoesNotContain(typeof(SaveHouseholdMemberRequest).GetProperties(),p=>p.Name.Contains("AnonymousVisitor",StringComparison.OrdinalIgnoreCase)||p.Name.Contains("VisitorId",StringComparison.OrdinalIgnoreCase));
        Assert.Contains(typeof(SaveHouseholdMemberRequest).GetProperties(),p=>p.Name=="CustomerId");
    }

    [Theory]
    [InlineData(nameof(HouseholdsVisitsController.CreateHousehold),PermissionCatalog.Operations.HouseholdsCreate)]
    [InlineData(nameof(HouseholdsVisitsController.UpdateHousehold),PermissionCatalog.Operations.HouseholdsEdit)]
    [InlineData(nameof(HouseholdsVisitsController.AddHouseholdMember),PermissionCatalog.Operations.HouseholdsManageMembers)]
    [InlineData(nameof(HouseholdsVisitsController.UpdateHouseholdMember),PermissionCatalog.Operations.HouseholdsManageMembers)]
    [InlineData(nameof(HouseholdsVisitsController.RemoveHouseholdMember),PermissionCatalog.Operations.HouseholdsManageMembers)]
    [InlineData(nameof(HouseholdsVisitsController.CreateVisit),PermissionCatalog.Operations.VisitsEdit)]
    [InlineData(nameof(HouseholdsVisitsController.CompleteVisit),PermissionCatalog.Operations.VisitsEdit)]
    public void PhaseSevenWritesRequireExactPermission(string methodName,string permission)
    {
        var attr=typeof(HouseholdsVisitsController).GetMethod(methodName)!.GetCustomAttribute<HasPermissionAttribute>();
        Assert.NotNull(attr); Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attr.Policy);
    }

    [Fact]
    public void VisitPartyReadUsesDedicatedPermissionAndCoVisitRoute()
    {
        var method=typeof(HouseholdsVisitsController).GetMethod(nameof(HouseholdsVisitsController.SearchVisitParties))!;
        Assert.Equal("visit-parties",method.GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal(AuthorizationPolicyNames.ForPermission(PermissionCatalog.Operations.VisitPartiesView),method.GetCustomAttribute<HasPermissionAttribute>()!.Policy);
    }
}