using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.Security;

public interface ISecurityPlatformService
{
    Task<SecurityIngestionResult>IngestAsync(SecurityIngestionEnvelope envelope,CancellationToken ct=default);
    Task<IReadOnlyList<SecurityIncidentSummary>>ListIncidentsAsync(long?storeId,SecurityIncidentStatus?status,int take=100,CancellationToken ct=default);
    Task<SecurityIncidentDetail>GetIncidentAsync(long id,CancellationToken ct=default);
    Task<SecurityIncidentDetail>AssignAsync(long id,long userId,TenantAuditContext audit,CancellationToken ct=default);
    Task<SecurityIncidentDetail>TransitionAsync(long id,SecurityIncidentStatus target,string?reason,string?notes,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<SecurityActionView>>TimelineAsync(long id,CancellationToken ct=default);
    Task<IReadOnlyList<SecurityEvidenceView>>EvidenceAsync(long id,TenantAuditContext audit,CancellationToken ct=default);
    Task<SecurityEvidenceTicket>EvidenceTicketAsync(long incidentId,long evidenceId,bool isExport,TenantAuditContext audit,CancellationToken ct=default);
    Task<SecurityEvidenceFile>OpenEvidenceAsync(long incidentId,long evidenceId,string token,bool isExport,TenantAuditContext audit,CancellationToken ct=default);
    Task<SecuritySettingsView>GetSettingsAsync(long?storeId,CancellationToken ct=default);
    Task<SecuritySettingsView>SaveSettingsAsync(long?storeId,SaveSecuritySettingsCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<SecurityRuleView>>ListRulesAsync(long?storeId,CancellationToken ct=default);
    Task<SecurityRuleView>SaveRuleAsync(long?storeId,SaveSecurityRuleCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<SecurityReportView>ReportAsync(long?storeId,DateTime fromUtc,DateTime toUtc,CancellationToken ct=default);
}

public sealed record SecurityIngestionEnvelope(string ServiceKeyId,string Timestamp,string Nonce,string Signature,string IdempotencyKey,byte[]Body,string CorrelationId);
public sealed record SecurityObservationRequest(long TenantId,long StoreId,long CameraId,long?VisitId,long?PersonTrackSessionId,string?PersonTrackId,SecurityObservationType ObservationType,DateTime OccurredUtc,long?ZoneId,long?ProductId,long?ProductCategoryId,decimal Confidence,string ModelVersion,string?MetadataJson);
public sealed record SecurityIngestionResult(long ObservationId,bool Duplicate,long?IncidentId,decimal?RiskScore,bool Suppressed,string Outcome);
public sealed record SecurityIncidentSummary(long Id,string IncidentNumber,long StoreId,string?PersonTrackId,SecuritySeverity Severity,decimal RiskScore,int RuleVersion,SecurityIncidentStatus Status,decimal?EstimatedLossAmount,string Currency,long?AssignedUserId,string?ResolutionCode,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record SecurityIncidentItemView(long Id,long?ProductId,long?ProductCategoryId,string Description,decimal?Quantity,decimal?UnitValue,decimal?ProductConfidence,SecurityPaymentMatchStatus PaymentMatchStatus);
public sealed record SecurityPaymentCorrelationView(long Id,long?InvoiceId,string?TransactionReference,byte MatchType,decimal MatchScore,DateTime MatchedUtc,string?Notes);
public sealed record SecurityEvidenceView(long Id,byte EvidenceType,long?CameraId,DateTime CapturedUtc,bool Available,DateTime RetentionUntilUtc,bool Restricted);
public sealed record SecurityActionView(long Id,string ActionType,SecurityIncidentStatus?FromStatus,SecurityIncidentStatus?ToStatus,long?UserId,string ActorType,string?ReasonCode,string?Notes,DateTime OccurredUtc,string CorrelationId);
public sealed record SecurityIncidentDetail(SecurityIncidentSummary Incident,IReadOnlyList<SecurityIncidentItemView>Items,IReadOnlyList<SecurityPaymentCorrelationView>PaymentCorrelations,IReadOnlyList<SecurityEvidenceView>Evidence,IReadOnlyList<SecurityActionView>Timeline);
public sealed record SecurityEvidenceTicket(string Token,DateTime ExpiresUtc);
public sealed record SecurityEvidenceFile(Stream Content,string ContentType,string FileName);
public sealed record SecuritySettingsView(long?StoreId,bool SecurityEnabled,bool UnpaidExitDetectionEnabled,bool RealtimeAlertsEnabled,bool ShadowMode,decimal RiskThreshold,decimal HighValueThreshold,int CheckoutCorrelationWindowMinutes,int ExitGracePeriodSeconds,int EvidenceBeforeSeconds,int EvidenceAfterSeconds,int EvidenceRetentionDays,string[]NotificationChannels,string EscalationPolicy,int RuleVersion);
public sealed record SaveSecuritySettingsCommand(bool SecurityEnabled,bool UnpaidExitDetectionEnabled,bool RealtimeAlertsEnabled,bool ShadowMode,decimal RiskThreshold,decimal HighValueThreshold,int CheckoutCorrelationWindowMinutes,int ExitGracePeriodSeconds,int EvidenceBeforeSeconds,int EvidenceAfterSeconds,int EvidenceRetentionDays,string[]NotificationChannels,string EscalationPolicy);
public sealed record SecurityRuleView(long Id,long?StoreId,string RuleCode,string Name,bool Enabled,SecuritySeverity Severity,string ConfigurationJson,int Version,long?CreatedByUserId,DateTime CreatedUtc);
public sealed record SaveSecurityRuleCommand(string RuleCode,string Name,bool Enabled,SecuritySeverity Severity,string ConfigurationJson);
public sealed record SecurityReportView(long?StoreId,DateTime FromUtc,DateTime ToUtc,long CandidateCount,long AlertedCount,long ConfirmedLossCount,long FalsePositiveCount,long ResolvedCount,decimal AverageRisk,decimal?Precision,decimal?FalsePositiveRate);
public sealed record SecurityRealtimeEvent(string EventType,long TenantId,long StoreId,long IncidentId,string IncidentNumber,SecurityIncidentStatus Status,SecuritySeverity Severity,decimal RiskScore,DateTime OccurredUtc,string CorrelationId);

public interface ISecurityRealtimePublisher{Task PublishAsync(SecurityRealtimeEvent message,CancellationToken ct=default);}
public interface ISecurityEvidenceTokenService{SecurityEvidenceTicket Create(long evidenceId,long incidentId,long userId,long tenantId,bool isExport,DateTime expiresUtc);void Validate(string token,long evidenceId,long incidentId,long userId,long tenantId,bool isExport,DateTime utcNow);}
public interface ISecurityEvidenceStore{Task SaveEncryptedAsync(string objectKey,ReadOnlyMemory<byte>content,CancellationToken ct=default);Task<Stream>OpenDecryptedAsync(string objectKey,CancellationToken ct=default);Task<bool>ExistsAsync(string objectKey,CancellationToken ct=default);Task DeleteAsync(string objectKey,CancellationToken ct=default);}
public interface ISecurityMaintenanceProcessor{Task<SecurityMaintenanceResult>RunOnceAsync(CancellationToken ct=default);}
public sealed record SecurityMaintenanceResult(int NotificationsDelivered,int EscalationsQueued,int EvidenceExpired,int PaymentsCorrelated,int StaleCandidatesResolved,long OpenCandidates);

public enum SecurityFailureKind{Validation,Unauthorized,Forbidden,NotFound,Conflict,Unavailable,Replay}
public sealed class SecurityException(string message,SecurityFailureKind kind):Exception(message){public SecurityFailureKind Kind{get;}=kind;}

public sealed class SecurityIngestionOptions
{
    public const string SectionName="SecurityIngestion";public int AllowedClockSkewSeconds{get;set;}=300;public int MaximumBodyBytes{get;set;}=262144;public Dictionary<string,string>ServiceKeys{get;set;}=[];
    public bool IsValid()=>AllowedClockSkewSeconds is>=30 and<=900&&MaximumBodyBytes is>=1024 and<=1048576&&ServiceKeys.All(x=>x.Key.Length is>=3 and<=100&&x.Value.Length>=32);
}

public sealed class SecurityEvidenceOptions
{
    public const string SectionName="SecurityEvidence";public string StoragePath{get;set;}="artifacts/security-evidence";public string DownloadSigningKey{get;set;}=string.Empty;public string EncryptionKeyBase64{get;set;}=string.Empty;public int TicketLifetimeMinutes{get;set;}=5;public int MaximumUploadBytes{get;set;}=26214400;
    public bool IsValid(bool requireSecrets)=>TicketLifetimeMinutes is>=1 and<=15&&MaximumUploadBytes is>=65536 and<=104857600&&(!requireSecrets||(DownloadSigningKey.Length>=32&&TryKey()));private bool TryKey(){try{return Convert.FromBase64String(EncryptionKeyBase64).Length==32;}catch(FormatException){return false;}}
}
