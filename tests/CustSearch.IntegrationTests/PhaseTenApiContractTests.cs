using System.Reflection;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

public sealed class PhaseTenApiContractTests
{
    [Fact]
    public void PhaseTenRequestDtosNeverExposeTenantId()
    {
        var types=new[]{typeof(AddCustomerPreferenceRequest),typeof(AddHouseholdPreferenceRequest),typeof(SavePreferenceWeightRequest),typeof(SaveVoiceRuntimeSettingRequest),typeof(SaveProductCategoryAliasRequest),typeof(StartVoiceSessionRequest),typeof(InterpretVoiceSessionRequest)};
        foreach(var type in types)Assert.DoesNotContain(type.GetProperties(),p=>string.Equals(p.Name,"TenantId",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VoiceInterpretDtoCannotChooseArbitraryPreferenceIdentity()
    {
        var names=typeof(InterpretVoiceSessionRequest).GetProperties().Select(x=>x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("RecognizedText",names);Assert.Contains("RecognitionConfidence",names);Assert.Contains("SelectedCategoryId",names);
        Assert.DoesNotContain("PreferenceType",names);Assert.DoesNotContain("ReferenceId",names);Assert.DoesNotContain("Value",names);Assert.DoesNotContain("TenantId",names);Assert.DoesNotContain("StoreId",names);Assert.DoesNotContain("CustomerId",names);
    }

    [Theory]
    [InlineData(nameof(PreferencesVoiceController.CustomerPreferences),PermissionCatalog.Operations.PreferencesView)]
    [InlineData(nameof(PreferencesVoiceController.AddCustomerTag),PermissionCatalog.Operations.PreferencesManage)]
    [InlineData(nameof(PreferencesVoiceController.HouseholdPreferences),PermissionCatalog.Operations.PreferencesView)]
    [InlineData(nameof(PreferencesVoiceController.AddHouseholdTag),PermissionCatalog.Operations.PreferencesManage)]
    [InlineData(nameof(PreferencesVoiceController.VoiceSetting),PermissionCatalog.Operations.VoiceCommandsView)]
    [InlineData(nameof(PreferencesVoiceController.SaveVoiceSetting),PermissionCatalog.Operations.VoiceCommandsConfigure)]
    [InlineData(nameof(PreferencesVoiceController.CategoryAliases),PermissionCatalog.Operations.StoreCategoriesView)]
    [InlineData(nameof(PreferencesVoiceController.AddCategoryAlias),PermissionCatalog.Operations.StoreCategoriesManage)]
    [InlineData(nameof(PreferencesVoiceController.StartVoice),PermissionCatalog.Operations.VoiceCommandsUse)]
    [InlineData(nameof(PreferencesVoiceController.InterpretVoice),PermissionCatalog.Operations.VoiceCommandsUse)]
    [InlineData(nameof(PreferencesVoiceController.ConfirmVoice),PermissionCatalog.Operations.VoiceCommandsUse)]
    [InlineData(nameof(PreferencesVoiceController.RejectVoice),PermissionCatalog.Operations.VoiceCommandsUse)]
    [InlineData(nameof(PreferencesVoiceController.AuditHistory),PermissionCatalog.Operations.VoiceCommandsAudit)]
    public void PhaseTenEndpointsRequireExactPermission(string methodName,string permission)
    {
        var method=typeof(PreferencesVoiceController).GetMethod(methodName)!;var attr=method.GetCustomAttribute<HasPermissionAttribute>();Assert.NotNull(attr);Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attr.Policy);
    }

    [Fact]
    public void CategoryAliasWritesUseExistingCategoryRouteRatherThanCategoryCreationRoute()
    {
        var method=typeof(PreferencesVoiceController).GetMethod(nameof(PreferencesVoiceController.AddCategoryAlias))!;var route=method.GetCustomAttribute<HttpPostAttribute>()!.Template;
        Assert.Equal("store-categories/{categoryId:long}/aliases",route);Assert.DoesNotContain("create-category",route,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HouseholdPreferenceContractsDoNotExposeVisitPartyOrAnonymousIdentity()
    {
        var types=new[]{typeof(AddHouseholdPreferenceRequest)};
        foreach(var type in types){var names=type.GetProperties().Select(x=>x.Name).ToArray();Assert.DoesNotContain("VisitPartyId",names,StringComparer.OrdinalIgnoreCase);Assert.DoesNotContain("AnonymousVisitorId",names,StringComparer.OrdinalIgnoreCase);Assert.DoesNotContain("FaceId",names,StringComparer.OrdinalIgnoreCase);}
    }
}
