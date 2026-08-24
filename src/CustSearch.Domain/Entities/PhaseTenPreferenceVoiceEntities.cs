using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Phase 10A one factual customer preference observation. Derived scores are stored separately.</summary>
public sealed class CustomerPreferenceSignal
{
    private CustomerPreferenceSignal() { }
    private CustomerPreferenceSignal(long tenantId,long? storeId,long customerId,PreferenceType type,long? referenceId,string? value,
        decimal? signalScore,PreferenceSignalSource source,decimal? confidence,long? createdByUserId,string? reason,DateTime observedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        if(storeId.HasValue) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId.Value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        if(referenceId.HasValue) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceId.Value);
        if(createdByUserId.HasValue) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId.Value);
        if(!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if(!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        ValidatePercent(signalScore,nameof(signalScore)); ValidatePercent(confidence,nameof(confidence));
        if(referenceId is null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Preference signal requires ReferenceId or Value.");
        TenantId=tenantId; StoreId=storeId; CustomerId=customerId; PreferenceType=type; ReferenceId=referenceId; Value=Optional(value,200);
        SignalScore=signalScore; Source=source; Confidence=confidence; CreatedByUserId=createdByUserId; Reason=Optional(reason,500);
        FirstObservedUtc=RequireUtc(observedUtc,nameof(observedUtc)); LastObservedUtc=FirstObservedUtc; IsActive=true; CreatedUtc=FirstObservedUtc; UpdatedUtc=FirstObservedUtc;
    }
    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public long? StoreId { get; private set; }
    public long CustomerId { get; private set; }
    public PreferenceType PreferenceType { get; private set; }
    public long? ReferenceId { get; private set; }
    public string? Value { get; private set; }
    public decimal? SignalScore { get; private set; }
    public PreferenceSignalSource Source { get; private set; }
    public decimal? Confidence { get; private set; }
    public DateTime FirstObservedUtc { get; private set; }
    public DateTime LastObservedUtc { get; private set; }
    public bool IsActive { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public static CustomerPreferenceSignal Create(long tenantId,long? storeId,long customerId,PreferenceType type,long? referenceId,string? value,
        decimal? signalScore,PreferenceSignalSource source,decimal? confidence,long? createdByUserId,string? reason,DateTime observedUtc)=>
        new(tenantId,storeId,customerId,type,referenceId,value,signalScore,source,confidence,createdByUserId,reason,observedUtc);
    public void ObserveAgain(decimal? signalScore,decimal? confidence,string? reason,DateTime observedUtc)
    {
        observedUtc=RequireUtc(observedUtc,nameof(observedUtc)); ArgumentOutOfRangeException.ThrowIfLessThan(observedUtc,FirstObservedUtc);
        ValidatePercent(signalScore,nameof(signalScore));ValidatePercent(confidence,nameof(confidence));SignalScore=signalScore;Confidence=confidence;Reason=Optional(reason,500);LastObservedUtc=observedUtc;IsActive=true;UpdatedUtc=observedUtc;
    }
    public void Deactivate(DateTime utcNow){IsActive=false;UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));}
    private static void ValidatePercent(decimal? value,string name){if(value is <0 or >100) throw new ArgumentOutOfRangeException(name,"Value must be between 0 and 100.");}
    private static string? Optional(string? value,int max){if(string.IsNullOrWhiteSpace(value)) return null;var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 10F deterministic derived customer preference score produced from factual signals under a recorded weight version.</summary>
public sealed class CustomerPreferenceScore
{
    private CustomerPreferenceScore() { }
    private CustomerPreferenceScore(long tenantId,long customerId,PreferenceType type,long? referenceId,string? value,decimal score,long weightVersionId,DateTime calculatedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weightVersionId);
        if(referenceId.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceId.Value);if(!Enum.IsDefined(type))throw new ArgumentOutOfRangeException(nameof(type));
        if(score is <0 or >100)throw new ArgumentOutOfRangeException(nameof(score));if(referenceId is null&&string.IsNullOrWhiteSpace(value))throw new ArgumentException("Preference score requires ReferenceId or Value.");
        TenantId=tenantId;CustomerId=customerId;PreferenceType=type;ReferenceId=referenceId;Value=Optional(value,200);Score=score;WeightVersionId=weightVersionId;CalculatedUtc=RequireUtc(calculatedUtc,nameof(calculatedUtc));
    }
    public long Id { get; private set; } public long TenantId { get; private set; } public long CustomerId { get; private set; }
    public PreferenceType PreferenceType { get; private set; } public long? ReferenceId { get; private set; } public string? Value { get; private set; }
    public decimal Score { get; private set; } public long WeightVersionId { get; private set; } public DateTime CalculatedUtc { get; private set; }
    public static CustomerPreferenceScore Create(long tenantId,long customerId,PreferenceType type,long? referenceId,string? value,decimal score,long weightVersionId,DateTime calculatedUtc)=>new(tenantId,customerId,type,referenceId,value,score,weightVersionId,calculatedUtc);
    private static string? Optional(string? value,int max){if(string.IsNullOrWhiteSpace(value))return null;var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 10B explicit shared Household preference tag. Only verified Household membership is used for member aggregation.</summary>
public sealed class HouseholdPreferenceTag
{
    private HouseholdPreferenceTag() { }
    private HouseholdPreferenceTag(long tenantId,long householdId,PreferenceType type,long? referenceId,string value,HouseholdPreferenceTagSource source,long createdByUserId,string? reason,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(householdId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);
        if(referenceId.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceId.Value);if(!Enum.IsDefined(type))throw new ArgumentOutOfRangeException(nameof(type));if(!Enum.IsDefined(source))throw new ArgumentOutOfRangeException(nameof(source));
        TenantId=tenantId;HouseholdId=householdId;PreferenceType=type;ReferenceId=referenceId;Value=Require(value,200);Source=source;CreatedByUserId=createdByUserId;Reason=Optional(reason,500);IsActive=true;CreatedUtc=RequireUtc(utcNow,nameof(utcNow));UpdatedUtc=CreatedUtc;
    }
    public long Id { get; private set; } public long TenantId { get; private set; } public long HouseholdId { get; private set; }
    public PreferenceType PreferenceType { get; private set; } public long? ReferenceId { get; private set; } public string Value { get; private set; }=string.Empty;
    public HouseholdPreferenceTagSource Source { get; private set; } public long CreatedByUserId { get; private set; } public string? Reason { get; private set; }
    public bool IsActive { get; private set; } public DateTime CreatedUtc { get; private set; } public DateTime UpdatedUtc { get; private set; }
    public static HouseholdPreferenceTag Create(long tenantId,long householdId,PreferenceType type,long? referenceId,string value,HouseholdPreferenceTagSource source,long createdByUserId,string? reason,DateTime utcNow)=>new(tenantId,householdId,type,referenceId,value,source,createdByUserId,reason,utcNow);
    public void Deactivate(DateTime utcNow){IsActive=false;UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));}
    private static string Require(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static string? Optional(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:Require(value,max);
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 10F tenant-specific versioned weights used by deterministic preference recalculation.</summary>
public sealed class PreferenceWeightVersion
{
    private PreferenceWeightVersion() { }
    private PreferenceWeightVersion(long tenantId,string versionCode,decimal manualStaffWeight,decimal purchaseWeight,decimal categoryInteractionWeight,decimal voiceConfirmedWeight,long createdByUserId,DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);
        ValidateWeight(manualStaffWeight,nameof(manualStaffWeight));ValidateWeight(purchaseWeight,nameof(purchaseWeight));ValidateWeight(categoryInteractionWeight,nameof(categoryInteractionWeight));ValidateWeight(voiceConfirmedWeight,nameof(voiceConfirmedWeight));
        TenantId=tenantId;VersionCode=Require(versionCode,50).ToUpperInvariant();ManualStaffWeight=manualStaffWeight;PurchaseWeight=purchaseWeight;CategoryInteractionWeight=categoryInteractionWeight;VoiceConfirmedWeight=voiceConfirmedWeight;IsActive=true;CreatedByUserId=createdByUserId;CreatedUtc=RequireUtc(utcNow,nameof(utcNow));
    }
    public long Id { get; private set; } public long TenantId { get; private set; } public string VersionCode { get; private set; }=string.Empty;
    public decimal ManualStaffWeight { get; private set; } public decimal PurchaseWeight { get; private set; } public decimal CategoryInteractionWeight { get; private set; } public decimal VoiceConfirmedWeight { get; private set; }
    public bool IsActive { get; private set; } public long CreatedByUserId { get; private set; } public DateTime CreatedUtc { get; private set; }
    public static PreferenceWeightVersion Create(long tenantId,string versionCode,decimal manualStaffWeight,decimal purchaseWeight,decimal categoryInteractionWeight,decimal voiceConfirmedWeight,long createdByUserId,DateTime utcNow)=>new(tenantId,versionCode,manualStaffWeight,purchaseWeight,categoryInteractionWeight,voiceConfirmedWeight,createdByUserId,utcNow);
    public void Deactivate(){IsActive=false;}
    private static void ValidateWeight(decimal value,string name){if(value is <0 or >10)throw new ArgumentOutOfRangeException(name,"Weight must be between 0 and 10.");}
    private static string Require(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}

/// <summary>Phase 10E one server-authorized voice interaction. A proposed CRM change is not factual until confirmation/resolution rules allow it.</summary>
public sealed class VoiceCommandSession
{
    private VoiceCommandSession() { }
    private VoiceCommandSession(long tenantId,long storeId,long staffUserId,long customerId,string matchedTrigger,bool confirmationRequired,DateTime createdUtc,DateTime expiresUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(staffUserId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        createdUtc=RequireUtc(createdUtc,nameof(createdUtc));expiresUtc=RequireUtc(expiresUtc,nameof(expiresUtc));ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresUtc,createdUtc);
        TenantId=tenantId;StoreId=storeId;StaffUserId=staffUserId;CustomerId=customerId;MatchedTrigger=Require(matchedTrigger,100);ConfirmationRequired=confirmationRequired;Status=VoiceCommandSessionStatus.Listening;CreatedUtc=createdUtc;UpdatedUtc=createdUtc;ExpiresUtc=expiresUtc;
    }
    public long Id { get; private set; } public long TenantId { get; private set; } public long StoreId { get; private set; } public long StaffUserId { get; private set; } public long CustomerId { get; private set; }
    public string MatchedTrigger { get; private set; }=string.Empty; public string? RecognizedText { get; private set; } public decimal? RecognitionConfidence { get; private set; }
    public PreferenceType? ProposedPreferenceType { get; private set; } public long? ProposedReferenceId { get; private set; } public string? ProposedValue { get; private set; }
    public bool ConfirmationRequired { get; private set; } public VoiceCommandSessionStatus Status { get; private set; } public DateTime ExpiresUtc { get; private set; } public DateTime? ResolvedUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; } public DateTime UpdatedUtc { get; private set; }
    public static VoiceCommandSession Start(long tenantId,long storeId,long staffUserId,long customerId,string matchedTrigger,bool confirmationRequired,DateTime createdUtc,DateTime expiresUtc)=>new(tenantId,storeId,staffUserId,customerId,matchedTrigger,confirmationRequired,createdUtc,expiresUtc);
    public void Propose(string recognizedText,decimal confidence,PreferenceType type,long? referenceId,string? value,DateTime utcNow)
    {
        EnsureOpen(utcNow);if(confidence is <0 or >100)throw new ArgumentOutOfRangeException(nameof(confidence));if(referenceId.HasValue)ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceId.Value);if(referenceId is null&&string.IsNullOrWhiteSpace(value))throw new ArgumentException("Voice proposal requires ReferenceId or Value.");
        RecognizedText=Require(recognizedText,250);RecognitionConfidence=confidence;ProposedPreferenceType=type;ProposedReferenceId=referenceId;ProposedValue=Optional(value,200);Status=ConfirmationRequired?VoiceCommandSessionStatus.PendingConfirmation:VoiceCommandSessionStatus.Confirmed;UpdatedUtc=RequireUtc(utcNow,nameof(utcNow));if(Status==VoiceCommandSessionStatus.Confirmed)ResolvedUtc=UpdatedUtc;
    }
    public void Confirm(DateTime utcNow){EnsurePending(utcNow);Status=VoiceCommandSessionStatus.Confirmed;ResolvedUtc=RequireUtc(utcNow,nameof(utcNow));UpdatedUtc=ResolvedUtc.Value;}
    public void Reject(DateTime utcNow){EnsurePending(utcNow);Status=VoiceCommandSessionStatus.Rejected;ResolvedUtc=RequireUtc(utcNow,nameof(utcNow));UpdatedUtc=ResolvedUtc.Value;}
    public void Expire(DateTime utcNow){utcNow=RequireUtc(utcNow,nameof(utcNow));if(Status is VoiceCommandSessionStatus.Confirmed or VoiceCommandSessionStatus.Rejected)return;Status=VoiceCommandSessionStatus.Expired;ResolvedUtc=utcNow;UpdatedUtc=utcNow;}
    private void EnsureOpen(DateTime utcNow){utcNow=RequireUtc(utcNow,nameof(utcNow));if(utcNow>ExpiresUtc){Expire(utcNow);throw new InvalidOperationException("Voice command session has expired.");}if(Status!=VoiceCommandSessionStatus.Listening)throw new InvalidOperationException("Voice command session is not listening.");}
    private void EnsurePending(DateTime utcNow){utcNow=RequireUtc(utcNow,nameof(utcNow));if(utcNow>ExpiresUtc){Expire(utcNow);throw new InvalidOperationException("Voice command session has expired.");}if(Status!=VoiceCommandSessionStatus.PendingConfirmation)throw new InvalidOperationException("Voice command session is not pending confirmation.");}
    private static string Require(string value,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(nameof(value));}
    private static string? Optional(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:Require(value,max);
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}
