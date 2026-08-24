using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.PreferencesVoice;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Phase 10 tenant/store preference and voice API. Browser DTOs never contain TenantId and voice category resolution remains server-authoritative.</summary>
[ApiController]
[Route("api/tenant")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
public sealed class PreferencesVoiceController(IPreferencesVoiceService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("customers/{customerId:long}/preferences")]
    [HasPermission(PermissionCatalog.Operations.PreferencesView)]
    public Task<CustomerPreferencesView> CustomerPreferences(long customerId,CancellationToken ct)=>service.GetCustomerPreferencesAsync(customerId,ct);

    [HttpPost("customers/{customerId:long}/preferences/tags")]
    [HasPermission(PermissionCatalog.Operations.PreferencesManage)]
    public Task<CustomerPreferencesView> AddCustomerTag(long customerId,AddCustomerPreferenceRequest request,CancellationToken ct)=>service.AddCustomerTagAsync(customerId,new(request.StoreId,request.PreferenceType,request.ReferenceId,request.Value,request.SignalScore,request.Confidence,request.Reason),Audit(),ct);

    [HttpPost("customers/{customerId:long}/preferences/recalculate")]
    [HasPermission(PermissionCatalog.Operations.PreferencesManage)]
    public Task<CustomerPreferencesView> RecalculateCustomer(long customerId,CancellationToken ct)=>service.RecalculateCustomerAsync(customerId,Audit(),ct);

    [HttpGet("households/{householdId:long}/preferences")]
    [HasPermission(PermissionCatalog.Operations.PreferencesView)]
    public Task<HouseholdPreferencesView> HouseholdPreferences(long householdId,CancellationToken ct)=>service.GetHouseholdPreferencesAsync(householdId,ct);

    [HttpPost("households/{householdId:long}/preferences/tags")]
    [HasPermission(PermissionCatalog.Operations.PreferencesManage)]
    public Task<HouseholdPreferencesView> AddHouseholdTag(long householdId,AddHouseholdPreferenceRequest request,CancellationToken ct)=>service.AddHouseholdTagAsync(householdId,new(request.PreferenceType,request.ReferenceId,request.Value,request.Source,request.Reason),Audit(),ct);

    [HttpGet("preferences/weights/active")]
    [HasPermission(PermissionCatalog.Operations.PreferencesView)]
    public Task<PreferenceWeightView> ActiveWeights(CancellationToken ct)=>service.GetActiveWeightVersionAsync(ct);

    [HttpPost("preferences/weights")]
    [HasPermission(PermissionCatalog.Operations.PreferencesManage)]
    public Task<PreferenceWeightView> SaveWeights(SavePreferenceWeightRequest request,CancellationToken ct)=>service.SaveWeightVersionAsync(new(request.VersionCode,request.ManualStaffWeight,request.PurchaseWeight,request.CategoryInteractionWeight,request.VoiceConfirmedWeight),Audit(),ct);

    [HttpGet("stores/{storeId:long}/voice-command-runtime")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsView)]
    public Task<VoiceSettingView> VoiceSetting(long storeId,CancellationToken ct)=>service.GetVoiceSettingAsync(storeId,ct);

    [HttpPut("stores/{storeId:long}/voice-command-runtime")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsConfigure)]
    public Task<VoiceSettingView> SaveVoiceSetting(long storeId,SaveVoiceRuntimeSettingRequest request,CancellationToken ct)=>service.SaveVoiceSettingAsync(storeId,new(request.TriggerKeyword,request.ResponseMode,request.IsEnabled,request.RequireConfirmationForAmbiguousCategory,request.Aliases,request.LanguageCode,request.RequireConfirmation,request.ListeningTimeoutSeconds,request.MinimumRecognitionConfidence),Audit(),ct);

    [HttpGet("store-categories/{categoryId:long}/aliases")]
    [HasPermission(PermissionCatalog.Operations.StoreCategoriesView)]
    public Task<IReadOnlyList<ProductCategoryAliasView>> CategoryAliases(long categoryId,[FromQuery]long? storeId,CancellationToken ct)=>service.ListCategoryAliasesAsync(categoryId,storeId,ct);

    [HttpPost("store-categories/{categoryId:long}/aliases")]
    [HasPermission(PermissionCatalog.Operations.StoreCategoriesManage)]
    public Task<ProductCategoryAliasView> AddCategoryAlias(long categoryId,SaveProductCategoryAliasRequest request,CancellationToken ct)=>service.AddCategoryAliasAsync(categoryId,new(request.StoreId,request.AliasText,request.LanguageCode),Audit(),ct);

    [HttpPost("voice/commands/start")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsUse)]
    public Task<VoiceSessionView> StartVoice(StartVoiceSessionRequest request,CancellationToken ct)=>service.StartVoiceSessionAsync(new(request.StoreId,request.CustomerId,request.TriggerText),Audit(),ct);

    [HttpPost("voice/commands/{sessionId:long}/interpret")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsUse)]
    public Task<VoiceInterpretResult> InterpretVoice(long sessionId,InterpretVoiceSessionRequest request,CancellationToken ct)=>service.InterpretVoiceSessionAsync(sessionId,new(request.RecognizedText,request.RecognitionConfidence,request.SelectedCategoryId,request.Reason),Audit(),ct);

    [HttpPost("voice/commands/{sessionId:long}/confirm")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsUse)]
    public Task<VoiceSessionView> ConfirmVoice(long sessionId,CancellationToken ct)=>service.ConfirmVoiceSessionAsync(sessionId,Audit(),ct);

    [HttpPost("voice/commands/{sessionId:long}/reject")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsUse)]
    public Task<VoiceSessionView> RejectVoice(long sessionId,CancellationToken ct)=>service.RejectVoiceSessionAsync(sessionId,Audit(),ct);

    [HttpGet("preferences/audit")]
    [HasPermission(PermissionCatalog.Operations.VoiceCommandsAudit)]
    public Task<IReadOnlyList<PreferenceAuditItem>> AuditHistory([FromQuery]long? customerId=null,[FromQuery]long? storeId=null,[FromQuery]int take=100,CancellationToken ct=default)=>service.GetAuditHistoryAsync(customerId,storeId,take,ct);

    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

public sealed record AddCustomerPreferenceRequest(long StoreId,PreferenceType PreferenceType,long? ReferenceId,[param:StringLength(200)]string? Value,[param:Range(typeof(decimal),"0","100")]decimal? SignalScore,[param:Range(typeof(decimal),"0","100")]decimal? Confidence,[param:StringLength(500)]string? Reason);
public sealed record AddHouseholdPreferenceRequest(PreferenceType PreferenceType,long? ReferenceId,[param:Required,StringLength(200)]string Value,HouseholdPreferenceTagSource Source,[param:StringLength(500)]string? Reason);
public sealed record SavePreferenceWeightRequest([param:Required,StringLength(50)]string VersionCode,[param:Range(typeof(decimal),"0","10")]decimal ManualStaffWeight,[param:Range(typeof(decimal),"0","10")]decimal PurchaseWeight,[param:Range(typeof(decimal),"0","10")]decimal CategoryInteractionWeight,[param:Range(typeof(decimal),"0","10")]decimal VoiceConfirmedWeight);
public sealed record SaveVoiceRuntimeSettingRequest([param:Required,StringLength(100)]string TriggerKeyword,[param:Required,StringLength(30)]string ResponseMode,bool IsEnabled,bool RequireConfirmationForAmbiguousCategory,[param:Required]IReadOnlyList<string> Aliases,[param:Required,StringLength(20)]string LanguageCode,bool RequireConfirmation,[param:Range(3,120)]int ListeningTimeoutSeconds,[param:Range(typeof(decimal),"0","100")]decimal MinimumRecognitionConfidence);
public sealed record SaveProductCategoryAliasRequest(long? StoreId,[param:Required,StringLength(150)]string AliasText,[param:Required,StringLength(20)]string LanguageCode);
public sealed record StartVoiceSessionRequest(long StoreId,long CustomerId,[param:Required,StringLength(100)]string TriggerText);
/// <summary>Phase 10 voice DTO deliberately excludes PreferenceType/ReferenceId/Value; the server resolves category identity from transcript and aliases.</summary>
public sealed record InterpretVoiceSessionRequest([param:Required,StringLength(250)]string RecognizedText,[param:Range(typeof(decimal),"0","100")]decimal RecognitionConfidence,long? SelectedCategoryId,[param:StringLength(500)]string? Reason);
