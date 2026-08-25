using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.Recognition;

public interface IRecognitionService
{
    Task<IReadOnlyList<RecognitionConsentView>>ListConsentsAsync(long customerId,CancellationToken ct=default);
    Task<RecognitionConsentView>GrantConsentAsync(long customerId,GrantRecognitionConsentCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<RecognitionConsentView>WithdrawConsentAsync(long consentId,string reason,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<BiometricTemplateView>>ListTemplatesAsync(long customerId,TenantAuditContext audit,CancellationToken ct=default);
    Task<BiometricTemplateView>EnrollAsync(long customerId,EnrollBiometricTemplateCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<RecognitionCandidateView>CreateCandidateAsync(CreateRecognitionCandidateCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<RecognitionCandidateView>>ListCandidatesAsync(long?storeId,RecognitionCandidateStatus?status,CancellationToken ct=default);
    Task<RecognitionCandidateView>ReviewAsync(long candidateId,bool accept,string reason,TenantAuditContext audit,CancellationToken ct=default);
}

public interface IRecognitionTemplateProtector{ProtectedRecognitionTemplate Protect(ReadOnlySpan<byte>derivedTemplate);}
public sealed record ProtectedRecognitionTemplate(byte[]Ciphertext,byte[]Nonce,byte[]AuthenticationTag,string KeyReference,string Algorithm);
public sealed record GrantRecognitionConsentCommand(RecognitionConsentType ConsentType,string Purpose,DateTime GrantedUtc,DateTime?ExpiresUtc,string ConsentVersion,string?EvidenceReference);
public sealed record EnrollBiometricTemplateCommand(long StoreId,long ConsentId,string Purpose,string DerivedTemplateBase64,string TemplateVersion);
public sealed record CreateRecognitionCandidateCommand(long StoreId,long PersonTrackSessionId,long BiometricTemplateId,string RequestId,string Purpose,decimal Confidence,decimal Quality,decimal?SecondBestConfidence);
public sealed record RecognitionConsentView(long Id,long CustomerId,RecognitionConsentType ConsentType,string Purpose,DateTime GrantedUtc,DateTime?ExpiresUtc,DateTime?WithdrawnUtc,string ConsentVersion,long CapturedByUserId,string?EvidenceReference,bool IsActive);
public sealed record BiometricTemplateView(long Id,long StoreId,long CustomerId,long ConsentId,string Algorithm,string TemplateVersion,BiometricTemplateStatus Status,DateTime CreatedUtc,DateTime?DisabledUtc,DateTime?DeletedUtc,DateTime?RetentionUntilUtc);
public sealed record RecognitionCandidateView(long Id,long StoreId,long PersonTrackSessionId,long BiometricTemplateId,long CustomerId,string RequestId,string Purpose,decimal Confidence,decimal Quality,decimal?SecondBestConfidence,RecognitionCandidateStatus Status,DateTime CreatedUtc,DateTime?ReviewedUtc,long?ReviewedByUserId,string?ReviewReason);

public sealed class RecognitionException(string message,RecognitionFailureKind kind):Exception(message){public RecognitionFailureKind Kind{get;}=kind;}
public enum RecognitionFailureKind{Validation,Forbidden,NotFound,Conflict,Unavailable}

public sealed class RecognitionSecurityOptions
{
    public const string SectionName="RecognitionSecurity";
    public bool Enabled{get;set;}public string EncryptionKeyReference{get;set;}=string.Empty;public string EncryptionKeyBase64{get;set;}=string.Empty;public decimal MinimumConfidence{get;set;}=.85m;public decimal MinimumQuality{get;set;}=.7m;public decimal AmbiguityDelta{get;set;}=.05m;public int RetentionDaysAfterWithdrawal{get;set;}=30;
    public bool HasValidEncryptionConfiguration(){if(!Enabled)return true;if(string.IsNullOrWhiteSpace(EncryptionKeyReference))return false;try{return Convert.FromBase64String(EncryptionKeyBase64).Length==32;}catch(FormatException){return false;}}
}
