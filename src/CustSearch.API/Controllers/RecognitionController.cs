using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Recognition;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.Recognition;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Controllers;

[ApiController][Route("api/tenant/recognition")][Authorize(Policy=AuthorizationPolicyNames.TenantScope)][RejectClientTenantId][ServiceFilter(typeof(RecognitionExceptionFilter))]
public sealed class RecognitionController(IRecognitionService service,ICurrentUserContext currentUser,IOptions<RecognitionSecurityOptions>options):ControllerBase
{
    [HttpGet("customers/{customerId:long}/consents")][HasPermission(PermissionCatalog.Operations.RecognitionView)]public Task<IReadOnlyList<RecognitionConsentView>>Consents(long customerId,CancellationToken ct)=>service.ListConsentsAsync(customerId,ct);
    [HttpPost("customers/{customerId:long}/consents")][HasPermission(PermissionCatalog.Operations.RecognitionConsentManage)]public Task<RecognitionConsentView>Grant(long customerId,GrantRecognitionConsentRequest request,CancellationToken ct)=>service.GrantConsentAsync(customerId,request.Command(),Audit(),ct);
    [HttpPost("consents/{consentId:long}/withdraw")][HasPermission(PermissionCatalog.Operations.RecognitionConsentManage)]public Task<RecognitionConsentView>Withdraw(long consentId,WithdrawRecognitionConsentRequest request,CancellationToken ct)=>service.WithdrawConsentAsync(consentId,request.Reason,Audit(),ct);
    [HttpGet("customers/{customerId:long}/templates")][HasPermission(PermissionCatalog.Operations.RecognitionView)]public Task<IReadOnlyList<BiometricTemplateView>>Templates(long customerId,CancellationToken ct)=>service.ListTemplatesAsync(customerId,Audit(),ct);
    [HttpPost("customers/{customerId:long}/templates")][HasPermission(PermissionCatalog.Operations.RecognitionEnroll)]public Task<BiometricTemplateView>Enroll(long customerId,EnrollBiometricTemplateRequest request,CancellationToken ct)=>service.EnrollAsync(customerId,request.Command(),Audit(),ct);
    [HttpPost("candidates")][HasPermission(PermissionCatalog.Operations.RecognitionSettingsManage)]public Task<RecognitionCandidateView>CreateCandidate(CreateRecognitionCandidateRequest request,CancellationToken ct)=>service.CreateCandidateAsync(request.Command(),Audit(),ct);
    [HttpGet("candidates")][HasPermission(PermissionCatalog.Operations.RecognitionView)]public Task<IReadOnlyList<RecognitionCandidateView>>Candidates([FromQuery]long?storeId=null,[FromQuery]RecognitionCandidateStatus?status=null,CancellationToken ct=default)=>service.ListCandidatesAsync(storeId,status,ct);
    [HttpPost("candidates/{candidateId:long}/review")][HasPermission(PermissionCatalog.Operations.RecognitionReview)]public Task<RecognitionCandidateView>Review(long candidateId,ReviewRecognitionCandidateRequest request,CancellationToken ct)=>service.ReviewAsync(candidateId,request.Accept,request.Reason,Audit(),ct);
    [HttpGet("settings")][HasPermission(PermissionCatalog.Operations.RecognitionView)]public ActionResult<object>Settings(){var value=options.Value;return Ok(new{enabled=value.Enabled,minimumConfidence=value.MinimumConfidence,minimumQuality=value.MinimumQuality,ambiguityDelta=value.AmbiguityDelta,retentionDaysAfterWithdrawal=value.RetentionDaysAfterWithdrawal,storesRawImages=false,automaticIdentityMerge=false,externalIdentityDatabases=false});}
    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record GrantRecognitionConsentRequest(RecognitionConsentType ConsentType,[param:Required,StringLength(200)]string Purpose,DateTime GrantedUtc,DateTime?ExpiresUtc,[param:Required,StringLength(50)]string ConsentVersion,[param:StringLength(500)]string?EvidenceReference){public GrantRecognitionConsentCommand Command()=>new(ConsentType,Purpose,GrantedUtc,ExpiresUtc,ConsentVersion,EvidenceReference);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record WithdrawRecognitionConsentRequest([param:Required,StringLength(500)]string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record EnrollBiometricTemplateRequest([param:Range(1,long.MaxValue)]long StoreId,[param:Range(1,long.MaxValue)]long ConsentId,[param:Required,StringLength(200)]string Purpose,[param:Required,StringLength(32768)]string DerivedTemplateBase64,[param:Required,StringLength(50)]string TemplateVersion){public EnrollBiometricTemplateCommand Command()=>new(StoreId,ConsentId,Purpose,DerivedTemplateBase64,TemplateVersion);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record CreateRecognitionCandidateRequest([param:Range(1,long.MaxValue)]long StoreId,[param:Range(1,long.MaxValue)]long PersonTrackSessionId,[param:Range(1,long.MaxValue)]long BiometricTemplateId,[param:Required,StringLength(150)]string RequestId,[param:Required,StringLength(200)]string Purpose,[param:Range(typeof(decimal),"0","1")]decimal Confidence,[param:Range(typeof(decimal),"0","1")]decimal Quality,[param:Range(typeof(decimal),"0","1")]decimal?SecondBestConfidence){public CreateRecognitionCandidateCommand Command()=>new(StoreId,PersonTrackSessionId,BiometricTemplateId,RequestId,Purpose,Confidence,Quality,SecondBestConfidence);}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]public sealed record ReviewRecognitionCandidateRequest(bool Accept,[param:Required,StringLength(500)]string Reason);
