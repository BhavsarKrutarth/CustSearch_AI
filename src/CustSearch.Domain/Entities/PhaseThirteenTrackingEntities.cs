using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>A tenant/store-owned camera. Only an opaque RTSP configuration reference is persisted.</summary>
public sealed class Camera
{
    private Camera() { }
    private Camera(long tenantId,long storeId,string cameraCode,string name,string rtspConfigurationReference,string?location,CameraDirection direction,bool isActive,DateTime utcNow)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);Validate(direction);TenantId=tenantId;StoreId=storeId;CameraCode=Required(cameraCode,50).ToUpperInvariant();Name=Required(name,150);RtspConfigurationReference=Required(rtspConfigurationReference,200);Location=Optional(location,250);Direction=direction;IsActive=isActive;Status=CameraStatus.Offline;CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc;}
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public string CameraCode{get;private set;}=string.Empty; public string Name{get;private set;}=string.Empty; public string RtspConfigurationReference{get;private set;}=string.Empty; public CameraStatus Status{get;private set;} public string?Location{get;private set;} public CameraDirection Direction{get;private set;} public bool IsActive{get;private set;} public DateTime?LastHeartbeatUtc{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;} public byte[]?RowVersion{get;private set;}
    public static Camera Create(long tenantId,long storeId,string cameraCode,string name,string rtspConfigurationReference,string?location,CameraDirection direction,bool isActive,DateTime utcNow)=>new(tenantId,storeId,cameraCode,name,rtspConfigurationReference,location,direction,isActive,utcNow);
    public void Update(string name,string?rtspConfigurationReference,string?location,CameraDirection direction,bool isActive,DateTime utcNow){Validate(direction);Name=Required(name,150);if(rtspConfigurationReference is not null)RtspConfigurationReference=Required(rtspConfigurationReference,200);Location=Optional(location,250);Direction=direction;IsActive=isActive;if(!isActive)Status=CameraStatus.Offline;UpdatedUtc=Utc(utcNow);}
    public void Heartbeat(CameraStatus status,DateTime utcNow){Validate(status);if(!IsActive&&status!=CameraStatus.Offline)throw new InvalidOperationException("Inactive camera cannot report an active status.");Status=status;LastHeartbeatUtc=Utc(utcNow);UpdatedUtc=LastHeartbeatUtc.Value;}
    private static void Validate<T>(T value)where T:struct,Enum{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static string?Optional(string?value,int max)=>string.IsNullOrWhiteSpace(value)?null:Required(value,max);
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

/// <summary>Immutable, versioned polygon/configuration for a camera zone.</summary>
public sealed class CameraZoneConfiguration
{
    private CameraZoneConfiguration() { }
    private CameraZoneConfiguration(long tenantId,long storeId,long cameraId,string zoneCode,string name,CameraZoneType zoneType,string geometryJson,int version,long?categoryId,DateTime effectiveUtc)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cameraId);ArgumentOutOfRangeException.ThrowIfLessThan(version,1);if(!Enum.IsDefined(zoneType))throw new ArgumentOutOfRangeException(nameof(zoneType));TenantId=tenantId;StoreId=storeId;CameraId=cameraId;ZoneCode=Required(zoneCode,50).ToUpperInvariant();Name=Required(name,150);ZoneType=zoneType;GeometryJson=Required(geometryJson,8000);Version=version;CategoryId=categoryId;EffectiveUtc=Utc(effectiveUtc);IsActive=true;CreatedUtc=EffectiveUtc;}
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public long CameraId{get;private set;} public string ZoneCode{get;private set;}=string.Empty; public string Name{get;private set;}=string.Empty; public CameraZoneType ZoneType{get;private set;} public string GeometryJson{get;private set;}="{}"; public int Version{get;private set;} public long?CategoryId{get;private set;} public DateTime EffectiveUtc{get;private set;} public DateTime?SupersededUtc{get;private set;} public bool IsActive{get;private set;} public DateTime CreatedUtc{get;private set;}
    public static CameraZoneConfiguration Create(long tenantId,long storeId,long cameraId,string zoneCode,string name,CameraZoneType zoneType,string geometryJson,int version,long?categoryId,DateTime effectiveUtc)=>new(tenantId,storeId,cameraId,zoneCode,name,zoneType,geometryJson,version,categoryId,effectiveUtc);
    public void Supersede(DateTime utcNow){if(!IsActive)throw new InvalidOperationException("Zone version is already superseded.");SupersededUtc=Utc(utcNow);IsActive=false;}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

/// <summary>Operational tracking session; identity is anonymous unless explicitly associated by an authorized application flow.</summary>
public sealed class PersonTrackSession
{
    private PersonTrackSession() { }
    private PersonTrackSession(long tenantId,long storeId,long cameraId,string personTrackId,DateTime startUtc,decimal confidence)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cameraId);ValidateConfidence(confidence);TenantId=tenantId;StoreId=storeId;CameraId=cameraId;PersonTrackId=Required(personTrackId,100);StartUtc=Utc(startUtc);Confidence=confidence;TrackingState=PersonTrackingState.Active;SubjectKind=TrackingSubjectKind.Anonymous;UpdatedUtc=StartUtc;}
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public long CameraId{get;private set;} public string PersonTrackId{get;private set;}=string.Empty; public DateTime StartUtc{get;private set;} public DateTime?EndUtc{get;private set;} public decimal Confidence{get;private set;} public PersonTrackingState TrackingState{get;private set;} public TrackingSubjectKind SubjectKind{get;private set;} public long?CustomerId{get;private set;} public long?StaffProfileId{get;private set;} public DateTime UpdatedUtc{get;private set;} public byte[]?RowVersion{get;private set;}
    public static PersonTrackSession Start(long tenantId,long storeId,long cameraId,string personTrackId,DateTime startUtc,decimal confidence)=>new(tenantId,storeId,cameraId,personTrackId,startUtc,confidence);
    public void Observe(long cameraId,decimal confidence,DateTime utcNow,bool handoff=false){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cameraId);ValidateConfidence(confidence);if(TrackingState is PersonTrackingState.Ended or PersonTrackingState.Lost)throw new InvalidOperationException("Terminal track cannot be updated.");CameraId=cameraId;Confidence=Math.Max(Confidence,confidence);TrackingState=handoff?PersonTrackingState.Handoff:PersonTrackingState.Active;UpdatedUtc=Utc(utcNow);}
    public void End(DateTime utcNow,bool lost=false){if(TrackingState is PersonTrackingState.Ended or PersonTrackingState.Lost)throw new InvalidOperationException("Track already ended.");EndUtc=Utc(utcNow);if(EndUtc<StartUtc)throw new ArgumentOutOfRangeException(nameof(utcNow));TrackingState=lost?PersonTrackingState.Lost:PersonTrackingState.Ended;UpdatedUtc=EndUtc.Value;}
    public void AssociateCustomer(long customerId,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);if(SubjectKind!=TrackingSubjectKind.Anonymous)throw new InvalidOperationException("Track is already associated.");SubjectKind=TrackingSubjectKind.Customer;CustomerId=customerId;StaffProfileId=null;UpdatedUtc=Utc(utcNow);}
    public void AssociateStaff(long staffProfileId,DateTime utcNow){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(staffProfileId);if(SubjectKind!=TrackingSubjectKind.Anonymous)throw new InvalidOperationException("Track is already associated.");SubjectKind=TrackingSubjectKind.Staff;StaffProfileId=staffProfileId;CustomerId=null;UpdatedUtc=Utc(utcNow);}
    private static void ValidateConfidence(decimal value){if(value is<0 or>1)throw new ArgumentOutOfRangeException(nameof(value));}
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

public sealed class CameraTrackHandoff
{
    private CameraTrackHandoff() { }
    private CameraTrackHandoff(long tenantId,long storeId,long personTrackSessionId,long fromCameraId,long toCameraId,decimal confidence,int gapMilliseconds,DateTime occurredUtc)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personTrackSessionId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fromCameraId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(toCameraId);if(fromCameraId==toCameraId)throw new ArgumentException("Handoff cameras must differ.");if(confidence is<0 or>1)throw new ArgumentOutOfRangeException(nameof(confidence));ArgumentOutOfRangeException.ThrowIfNegative(gapMilliseconds);TenantId=tenantId;StoreId=storeId;PersonTrackSessionId=personTrackSessionId;FromCameraId=fromCameraId;ToCameraId=toCameraId;Confidence=confidence;GapMilliseconds=gapMilliseconds;OccurredUtc=Utc(occurredUtc);}
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public long PersonTrackSessionId{get;private set;} public long FromCameraId{get;private set;} public long ToCameraId{get;private set;} public decimal Confidence{get;private set;} public int GapMilliseconds{get;private set;} public DateTime OccurredUtc{get;private set;}
    public static CameraTrackHandoff Create(long tenantId,long storeId,long personTrackSessionId,long fromCameraId,long toCameraId,decimal confidence,int gapMilliseconds,DateTime occurredUtc)=>new(tenantId,storeId,personTrackSessionId,fromCameraId,toCameraId,confidence,gapMilliseconds,occurredUtc);
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}

/// <summary>Idempotent receipt for normalized CCTV metadata. Raw frames and biometric data are never stored.</summary>
public sealed class CameraOperationalEvent
{
    private CameraOperationalEvent() { }
    private CameraOperationalEvent(long tenantId,long storeId,long cameraId,string serviceId,string eventId,string idempotencyKey,string eventType,int contractVersion,string payloadHash,string correlationId,DateTime occurredUtc,DateTime receivedUtc)
    {TenantId=tenantId;StoreId=storeId;CameraId=cameraId;ServiceId=Required(serviceId,100);EventId=Required(eventId,150);IdempotencyKey=Required(idempotencyKey,150);EventType=Required(eventType,100);ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion,1);ContractVersion=contractVersion;PayloadHash=Required(payloadHash,64);CorrelationId=Required(correlationId,64);OccurredUtc=Utc(occurredUtc);ReceivedUtc=Utc(receivedUtc);Status=CameraOperationalEventStatus.Accepted;}
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public long CameraId{get;private set;} public string ServiceId{get;private set;}=string.Empty; public string EventId{get;private set;}=string.Empty; public string IdempotencyKey{get;private set;}=string.Empty; public string EventType{get;private set;}=string.Empty; public int ContractVersion{get;private set;} public string PayloadHash{get;private set;}=string.Empty; public string CorrelationId{get;private set;}=string.Empty; public DateTime OccurredUtc{get;private set;} public DateTime ReceivedUtc{get;private set;} public CameraOperationalEventStatus Status{get;private set;}
    public static CameraOperationalEvent Accept(long tenantId,long storeId,long cameraId,string serviceId,string eventId,string idempotencyKey,string eventType,int contractVersion,string payloadHash,string correlationId,DateTime occurredUtc,DateTime receivedUtc)=>new(tenantId,storeId,cameraId,serviceId,eventId,idempotencyKey,eventType,contractVersion,payloadHash,correlationId,occurredUtc,receivedUtc);
    public void MarkProcessed()=>Status=CameraOperationalEventStatus.Processed;
    private static string Required(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime Utc(DateTime value)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",nameof(value));
}
