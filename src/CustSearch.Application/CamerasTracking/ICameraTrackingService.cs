using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.CamerasTracking;

public interface ICameraTrackingService
{
    Task<IReadOnlyList<CameraView>> ListCamerasAsync(long?storeId,CancellationToken ct=default);
    Task<CameraView> SaveCameraAsync(long?cameraId,SaveCameraCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<CameraZoneView>> ListZonesAsync(long cameraId,CancellationToken ct=default);
    Task<CameraZoneView> AddZoneVersionAsync(long cameraId,SaveCameraZoneCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<PersonTrackView>> ListTracksAsync(long?storeId,long?afterId,int take=100,CancellationToken ct=default);
    Task<PersonTrackView> AssociateAsync(long trackId,AssociateTrackCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task<CctvEventAcknowledgement> ReceiveAsync(CctvInboundEnvelope envelope,CancellationToken ct=default);
}

public interface ICctvServiceSecretResolver
{
    ValueTask<CctvServiceCredential?> ResolveAsync(string serviceId,CancellationToken cancellationToken=default);
}

public sealed record CctvServiceCredential(string Secret,long TenantId,IReadOnlySet<long> StoreIds,bool AllowAllStores=false);

public sealed record SaveCameraCommand(long StoreId,string CameraCode,string Name,string?RtspConfigurationReference,string?Location,CameraDirection Direction,bool IsActive);
public sealed record SaveCameraZoneCommand(string ZoneCode,string Name,CameraZoneType ZoneType,string GeometryJson,long?CategoryId);
public sealed record AssociateTrackCommand(TrackingSubjectKind SubjectKind,long SubjectId);
public sealed record CameraView(long Id,long StoreId,string CameraCode,string Name,bool HasRtspConfiguration,string?RtspConfigurationHint,CameraStatus Status,string?Location,CameraDirection Direction,bool IsActive,DateTime?LastHeartbeatUtc,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record CameraZoneView(long Id,long CameraId,string ZoneCode,string Name,CameraZoneType ZoneType,string GeometryJson,int Version,long?CategoryId,DateTime EffectiveUtc,DateTime?SupersededUtc,bool IsActive);
public sealed record PersonTrackView(long Id,long StoreId,long CameraId,string PersonTrackId,DateTime StartUtc,DateTime?EndUtc,decimal Confidence,PersonTrackingState TrackingState,TrackingSubjectKind SubjectKind,long?CustomerId,long?StaffProfileId,DateTime UpdatedUtc);
public sealed record CctvInboundEnvelope(string ServiceId,string Timestamp,string Signature,string EventId,string IdempotencyKey,ReadOnlyMemory<byte> Body,string CorrelationId);
public sealed record CctvEventAcknowledgement(long EventReceiptId,long?TrackSessionId,bool Duplicate,string Status,string CorrelationId);
public sealed record CctvNormalizedEvent(int ContractVersion,string EventType,long TenantId,long StoreId,string CameraCode,string PersonTrackId,DateTime OccurredUtc,decimal Confidence,string?ZoneCode,string?FromCameraCode,int?GapMilliseconds,CameraStatus?CameraStatus);

public sealed class CctvSecurityOptions
{
    public const string SectionName="CctvSecurity";
    public int AllowedClockSkewSeconds{get;set;}=300;
    public int MaximumBodyBytes{get;set;}=131072;
}

public enum CameraTrackingFailureKind { Validation,Unauthorized,Forbidden,NotFound,Conflict,Unavailable }
public sealed class CameraTrackingException(string message,CameraTrackingFailureKind kind):Exception(message){public CameraTrackingFailureKind Kind{get;}=kind;}
