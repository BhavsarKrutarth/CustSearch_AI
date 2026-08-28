using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.CamerasTracking;

public interface ICameraEvidenceStorageService
{
    Task<TenantStorageSummaryView>GetTenantStorageAsync(CancellationToken ct=default);
    Task<TenantStorageSummaryView>GetPlatformStorageAsync(long tenantId,CancellationToken ct=default);
    Task<TenantStorageSummaryView>SavePlatformPolicyAsync(long tenantId,SaveTenantStoragePolicyCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<CameraEvidenceView>>ListEvidenceAsync(long?storeId,long?cameraId,int take=100,CancellationToken ct=default);
    Task<CameraEvidenceIngestionResult>IngestAsync(CctvEvidenceEnvelope envelope,CancellationToken ct=default);
}

public sealed record TenantStoragePolicyView(long TenantId,bool StorageEnabled,long StorageQuotaBytes,int DefaultRetentionDays,int MotionSnapshotRetentionDays,int MotionClipRetentionDays,int FalsePositiveRetentionDays,int UnreviewedEvidenceRetentionDays,int ConfirmedIncidentRetentionDays,int WarningPercent,int CriticalPercent,bool AllowSnapshots,bool AllowMotionClips,bool AutoCleanupEnabled,QuotaPressurePolicy QuotaPressurePolicy,DateTime UpdatedUtc);
public sealed record TenantStorageUsageView(long TenantId,long QuotaBytes,long UsedBytes,long AvailableBytes,long SnapshotBytes,long MotionClipBytes,long SecurityEvidenceBytes,long OtherBytes,decimal UsagePercent,string PressureLevel,DateTime LastCalculatedUtc,DateTime?LastCleanupUtc);
public sealed record TenantStorageSummaryView(TenantStoragePolicyView Policy,TenantStorageUsageView Usage);
public sealed record SaveTenantStoragePolicyCommand(bool StorageEnabled,long StorageQuotaBytes,int DefaultRetentionDays,int MotionSnapshotRetentionDays,int MotionClipRetentionDays,int FalsePositiveRetentionDays,int UnreviewedEvidenceRetentionDays,int ConfirmedIncidentRetentionDays,int WarningPercent,int CriticalPercent,bool AllowSnapshots,bool AllowMotionClips,bool AutoCleanupEnabled,QuotaPressurePolicy QuotaPressurePolicy);
public sealed record CameraEvidenceView(long Id,long StoreId,long CameraId,CameraEvidenceType EvidenceType,long FileSizeBytes,string ContentType,DateTime CapturedUtc,DateTime RetentionUntilUtc,bool IsRestricted,bool IsPinned,bool IsDeleted,DateTime CreatedUtc);
public sealed record CctvEvidenceMetadata(int ContractVersion,long TenantId,long StoreId,string CameraCode,CameraEvidenceType EvidenceType,string ContentType,DateTime CapturedUtc,bool IsRestricted=false,bool IsPinned=false,long?MotionEventId=null,long?SecurityIncidentId=null);
public sealed record CctvEvidenceEnvelope(string ServiceId,string Timestamp,string Signature,string EventId,string IdempotencyKey,string MetadataHeader,ReadOnlyMemory<byte>Body,string CorrelationId);
public sealed record CameraEvidenceIngestionResult(long EvidenceId,bool Duplicate,long UsedBytes,long QuotaBytes,DateTime RetentionUntilUtc,string Status);
