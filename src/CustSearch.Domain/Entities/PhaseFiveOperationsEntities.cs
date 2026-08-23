using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Phase 5D — staff-specific tenant profile linked 1:1 to an authenticated tenant user.</summary>
public sealed class StaffProfile
{
    private StaffProfile() { }
    private StaffProfile(long tenantId, long userId, string employeeCode, string firstName, string lastName, string? mobile, DateTime utcNow)
    {
        ValidateId(tenantId); ValidateId(userId);
        TenantId = tenantId; UserId = userId; EmployeeCode = Require(employeeCode, 50).ToUpperInvariant();
        FirstName = Require(firstName, 100); LastName = Require(lastName, 100); Mobile = Optional(mobile, 30);
        IsActive = true; CreatedUtc = Utc(utcNow); UpdatedUtc = CreatedUtc;
    }
    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long UserId { get; private set; }
    public UserAccount User { get; private set; } = null!;
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Mobile { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public static StaffProfile Create(long tenantId, long userId, string employeeCode, string firstName, string lastName, string? mobile, DateTime utcNow) => new(tenantId, userId, employeeCode, firstName, lastName, mobile, utcNow);
    public void Update(string firstName, string lastName, string? mobile, DateTime utcNow) { FirstName=Require(firstName,100); LastName=Require(lastName,100); Mobile=Optional(mobile,30); UpdatedUtc=Utc(utcNow); }
    public void SetActive(bool active, DateTime utcNow) { IsActive=active; UpdatedUtc=Utc(utcNow); }
    private static void ValidateId(long id)=>ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
    private static string Require(string v,int max){ArgumentException.ThrowIfNullOrWhiteSpace(v);v=v.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(v));}
    private static string? Optional(string? v,int max)=>string.IsNullOrWhiteSpace(v)?null:Require(v,max);
    private static DateTime Utc(DateTime v)=>v.Kind==DateTimeKind.Utc?v:throw new ArgumentException("Timestamp must be UTC.");
}

/// <summary>Phase 5D — planned/actual staff shift. It is operational context, not CCTV-derived payroll truth.</summary>
public sealed class StaffShift
{
    private StaffShift() { }
    private StaffShift(long tenantId,long staffProfileId,long storeId,DateTime startsUtc,DateTime? scheduledEndsUtc,long createdByUserId,DateTime utcNow)
    { Validate(tenantId);Validate(staffProfileId);Validate(storeId);Validate(createdByUserId); TenantId=tenantId;StaffProfileId=staffProfileId;StoreId=storeId;StartsUtc=Utc(startsUtc);ScheduledEndsUtc=scheduledEndsUtc.HasValue?Utc(scheduledEndsUtc.Value):null;if(ScheduledEndsUtc<=StartsUtc)throw new ArgumentException("Shift end must follow start.");Status=StaffShiftStatus.Scheduled;CreatedByUserId=createdByUserId;CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc; }
    public long Id{get;private set;} public long TenantId{get;private set;} public long StaffProfileId{get;private set;} public StaffProfile StaffProfile{get;private set;}=null!; public long StoreId{get;private set;} public Store Store{get;private set;}=null!; public DateTime StartsUtc{get;private set;} public DateTime? ScheduledEndsUtc{get;private set;} public DateTime? ActualEndsUtc{get;private set;} public StaffShiftStatus Status{get;private set;} public long CreatedByUserId{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;}
    public static StaffShift Create(long tenantId,long staffProfileId,long storeId,DateTime startsUtc,DateTime? scheduledEndsUtc,long createdByUserId,DateTime utcNow)=>new(tenantId,staffProfileId,storeId,startsUtc,scheduledEndsUtc,createdByUserId,utcNow);
    public void Start(DateTime utcNow){if(Status!=StaffShiftStatus.Scheduled)throw new InvalidOperationException("Only scheduled shifts can start.");Status=StaffShiftStatus.Active;UpdatedUtc=Utc(utcNow);} public void Complete(DateTime utcNow){if(Status!=StaffShiftStatus.Active)throw new InvalidOperationException("Only active shifts can complete.");ActualEndsUtc=Utc(utcNow);Status=StaffShiftStatus.Completed;UpdatedUtc=ActualEndsUtc.Value;} public void Cancel(DateTime utcNow){if(Status==StaffShiftStatus.Completed)throw new InvalidOperationException("Completed shifts cannot be cancelled.");Status=StaffShiftStatus.Cancelled;UpdatedUtc=Utc(utcNow);}
    private static void Validate(long id)=>ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id); private static DateTime Utc(DateTime v)=>v.Kind==DateTimeKind.Utc?v:throw new ArgumentException("Timestamp must be UTC.");
}

/// <summary>Phase 5D — optional presence signal associated with a staff member and store.</summary>
public sealed class StaffPresenceSession
{
    private StaffPresenceSession() { }
    private StaffPresenceSession(long tenantId,long staffProfileId,long storeId,StaffPresenceSource source,DateTime enteredUtc,decimal confidence)
    { if(tenantId<=0||staffProfileId<=0||storeId<=0)throw new ArgumentOutOfRangeException();if(confidence is <0 or >1)throw new ArgumentOutOfRangeException(nameof(confidence));TenantId=tenantId;StaffProfileId=staffProfileId;StoreId=storeId;Source=source;EnteredUtc=Utc(enteredUtc);Confidence=confidence; }
    public long Id{get;private set;} public long TenantId{get;private set;} public long StaffProfileId{get;private set;} public StaffProfile StaffProfile{get;private set;}=null!; public long StoreId{get;private set;} public Store Store{get;private set;}=null!; public StaffPresenceSource Source{get;private set;} public DateTime EnteredUtc{get;private set;} public DateTime? ExitedUtc{get;private set;} public decimal Confidence{get;private set;}
    public static StaffPresenceSession Start(long tenantId,long staffProfileId,long storeId,StaffPresenceSource source,DateTime enteredUtc,decimal confidence)=>new(tenantId,staffProfileId,storeId,source,enteredUtc,confidence); public void Close(DateTime exitedUtc){var x=Utc(exitedUtc);if(x<=EnteredUtc)throw new ArgumentException("Exit must follow entry.");ExitedUtc=x;}
    private static DateTime Utc(DateTime v)=>v.Kind==DateTimeKind.Utc?v:throw new ArgumentException("Timestamp must be UTC.");
}

/// <summary>Phase 5E — tenant/store product-category taxonomy used by staff preferences and later product analytics.</summary>
public sealed class ProductCategory
{
    private ProductCategory() { }
    private ProductCategory(long tenantId,long? storeId,string categoryCode,string name,long? parentCategoryId,DateTime utcNow){if(tenantId<=0)throw new ArgumentOutOfRangeException(nameof(tenantId));if(storeId is <=0)throw new ArgumentOutOfRangeException(nameof(storeId));TenantId=tenantId;StoreId=storeId;CategoryCode=Req(categoryCode,50).ToUpperInvariant();Name=Req(name,150);ParentCategoryId=parentCategoryId;IsActive=true;CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc;}
    public long Id{get;private set;} public long TenantId{get;private set;} public long? StoreId{get;private set;} public Store? Store{get;private set;} public string CategoryCode{get;private set;}=string.Empty; public string Name{get;private set;}=string.Empty; public long? ParentCategoryId{get;private set;} public ProductCategory? ParentCategory{get;private set;} public bool IsActive{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;}
    public static ProductCategory Create(long tenantId,long? storeId,string categoryCode,string name,long? parentCategoryId,DateTime utcNow)=>new(tenantId,storeId,categoryCode,name,parentCategoryId,utcNow); public void Update(string name,long? parentCategoryId,bool active,DateTime utcNow){Name=Req(name,150);ParentCategoryId=parentCategoryId;IsActive=active;UpdatedUtc=Utc(utcNow);} private static string Req(string v,int m){ArgumentException.ThrowIfNullOrWhiteSpace(v);v=v.Trim();return v.Length<=m?v:throw new ArgumentOutOfRangeException(nameof(v));} private static DateTime Utc(DateTime v)=>v.Kind==DateTimeKind.Utc?v:throw new ArgumentException("Timestamp must be UTC.");
}

/// <summary>Phase 5F — store-specific dynamic trigger and response behavior. The trigger is configurable; “Aasha Add” is only a default.</summary>
public sealed class StoreVoiceCommandSetting
{
    private StoreVoiceCommandSetting() { }
    private StoreVoiceCommandSetting(long tenantId,long storeId,string triggerKeyword,VoiceResponseMode responseMode,DateTime utcNow){if(tenantId<=0||storeId<=0)throw new ArgumentOutOfRangeException();TenantId=tenantId;StoreId=storeId;TriggerKeyword=Req(triggerKeyword,100);ResponseMode=responseMode;IsEnabled=true;RequireConfirmationForAmbiguousCategory=true;CreatedUtc=Utc(utcNow);UpdatedUtc=CreatedUtc;}
    public long StoreId{get;private set;} public long TenantId{get;private set;} public Store Store{get;private set;}=null!; public string TriggerKeyword{get;private set;}=string.Empty; public VoiceResponseMode ResponseMode{get;private set;} public bool IsEnabled{get;private set;} public bool RequireConfirmationForAmbiguousCategory{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;}
    public static StoreVoiceCommandSetting Create(long tenantId,long storeId,string triggerKeyword,VoiceResponseMode responseMode,DateTime utcNow)=>new(tenantId,storeId,triggerKeyword,responseMode,utcNow); public void Update(string triggerKeyword,VoiceResponseMode responseMode,bool enabled,bool requireConfirmation,DateTime utcNow){TriggerKeyword=Req(triggerKeyword,100);ResponseMode=responseMode;IsEnabled=enabled;RequireConfirmationForAmbiguousCategory=requireConfirmation;UpdatedUtc=Utc(utcNow);} private static string Req(string v,int m){ArgumentException.ThrowIfNullOrWhiteSpace(v);v=v.Trim();return v.Length<=m?v:throw new ArgumentOutOfRangeException(nameof(v));} private static DateTime Utc(DateTime v)=>v.Kind==DateTimeKind.Utc?v:throw new ArgumentException("Timestamp must be UTC.");
}

/// <summary>Phase 5F — optional alternative phrase for a store voice trigger.</summary>
public sealed class StoreVoiceCommandAlias
{
    private StoreVoiceCommandAlias() { }
    private StoreVoiceCommandAlias(long tenantId,long storeId,string alias,DateTime utcNow){if(tenantId<=0||storeId<=0)throw new ArgumentOutOfRangeException();TenantId=tenantId;StoreId=storeId;Alias=Req(alias);CreatedUtc=Utc(utcNow);}
    public long Id{get;private set;} public long TenantId{get;private set;} public long StoreId{get;private set;} public StoreVoiceCommandSetting Setting{get;private set;}=null!; public string Alias{get;private set;}=string.Empty; public DateTime CreatedUtc{get;private set;}
    public static StoreVoiceCommandAlias Create(long tenantId,long storeId,string alias,DateTime utcNow)=>new(tenantId,storeId,alias,utcNow); private static string Req(string v){ArgumentException.ThrowIfNullOrWhiteSpace(v);v=v.Trim();return v.Length<=100?v:throw new ArgumentOutOfRangeException(nameof(v));} private static DateTime Utc(DateTime v)=>v.Kind==DateTimeKind.Utc?v:throw new ArgumentException("Timestamp must be UTC.");
}
