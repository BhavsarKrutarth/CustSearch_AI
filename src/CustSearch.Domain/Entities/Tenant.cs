using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Defines the client organization boundary for all tenant-owned CustSearch data.</summary>
public sealed class Tenant
{
    private Tenant() { }

    private Tenant(string tenantCode,string legalName,string displayName,string timeZone,DateTime createdUtc)
        :this(tenantCode,legalName,displayName,displayName,"unknown@invalid.local",string.Empty,"XX",timeZone,"USD",1,5,5,createdUtc) { }

    private Tenant(string tenantCode,string legalName,string displayName,string primaryContactName,string primaryEmail,string primaryMobile,string countryCode,string timeZone,string currencyCode,int maxStores,int maxUsers,int maxCameras,DateTime createdUtc)
    {
        TenantCode=Require(tenantCode,nameof(tenantCode),30).ToUpperInvariant();LegalName=Require(legalName,nameof(legalName),200);DisplayName=Require(displayName,nameof(displayName),150);PrimaryContactName=Require(primaryContactName,nameof(primaryContactName),150);PrimaryEmail=Require(primaryEmail,nameof(primaryEmail),254);PrimaryMobile=Optional(primaryMobile,nameof(primaryMobile),30)??string.Empty;CountryCode=Require(countryCode,nameof(countryCode),2).ToUpperInvariant();TimeZone=Require(timeZone,nameof(timeZone),100);CurrencyCode=Require(currencyCode,nameof(currencyCode),3).ToUpperInvariant();
        ValidateQuotas(maxStores,maxUsers,maxUsers,maxCameras);MaxStores=maxStores;MaxUsers=maxUsers;MaxStaff=maxUsers;MaxCameras=maxCameras;SubscriptionStatus=SubscriptionStatus.Trial;CreatedUtc=RequireUtc(createdUtc,nameof(createdUtc));UpdatedUtc=CreatedUtc;RowVersion=NewRowVersion();IsActive=true;
    }

    public long Id{get;private set;} public string TenantCode{get;private set;}=string.Empty; public string LegalName{get;private set;}=string.Empty; public string DisplayName{get;private set;}=string.Empty; public string PrimaryContactName{get;private set;}=string.Empty; public string PrimaryEmail{get;private set;}=string.Empty; public string PrimaryMobile{get;private set;}=string.Empty; public string CountryCode{get;private set;}=string.Empty; public string TimeZone{get;private set;}=string.Empty; public string CurrencyCode{get;private set;}=string.Empty;
    public long? SubscriptionPlanId{get;private set;} public SubscriptionPlan? SubscriptionPlan{get;private set;} public SubscriptionStatus SubscriptionStatus{get;private set;} public DateTime? TrialStartsUtc{get;private set;} public DateTime? TrialEndsUtc{get;private set;} public DateTime? SubscriptionStartsUtc{get;private set;} public DateTime? SubscriptionEndsUtc{get;private set;}
    public int MaxStores{get;private set;} public int MaxUsers{get;private set;} public int MaxStaff{get;private set;} public int MaxCameras{get;private set;}
    public bool IsActive{get;private set;} public bool IsSuspended{get;private set;} public string? SuspensionReason{get;private set;} public DateTime CreatedUtc{get;private set;} public DateTime UpdatedUtc{get;private set;} public byte[] RowVersion{get;private set;}=[];

    public static Tenant Create(string tenantCode,string legalName,string displayName,string timeZone,DateTime createdUtc)=>new(tenantCode,legalName,displayName,timeZone,createdUtc);
    public static Tenant Create(string tenantCode,string legalName,string displayName,string primaryContactName,string primaryEmail,string primaryMobile,string countryCode,string timeZone,string currencyCode,int maxStores,int maxUsers,int maxCameras,DateTime createdUtc)=>new(tenantCode,legalName,displayName,primaryContactName,primaryEmail,primaryMobile,countryCode,timeZone,currencyCode,maxStores,maxUsers,maxCameras,createdUtc);

    public void Suspend()=>IsSuspended=true;
    public void Suspend(string reason,DateTime updatedUtc){IsSuspended=true;SuspensionReason=Require(reason,nameof(reason),500);Touch(updatedUtc);}
    public void Deactivate(){IsActive=false;IsSuspended=false;SuspensionReason=null;}
    public void Deactivate(DateTime updatedUtc){Deactivate();Touch(updatedUtc);}
    public void Activate(){IsActive=true;IsSuspended=false;SuspensionReason=null;}
    public void Activate(DateTime updatedUtc){Activate();Touch(updatedUtc);}

    public void UpdateProfile(string legalName,string displayName,string primaryContactName,string primaryEmail,string primaryMobile,string countryCode,string timeZone,string currencyCode,DateTime updatedUtc)
    {LegalName=Require(legalName,nameof(legalName),200);DisplayName=Require(displayName,nameof(displayName),150);PrimaryContactName=Require(primaryContactName,nameof(primaryContactName),150);PrimaryEmail=Require(primaryEmail,nameof(primaryEmail),254);PrimaryMobile=Optional(primaryMobile,nameof(primaryMobile),30)??string.Empty;CountryCode=Require(countryCode,nameof(countryCode),2).ToUpperInvariant();TimeZone=Require(timeZone,nameof(timeZone),100);CurrencyCode=Require(currencyCode,nameof(currencyCode),3).ToUpperInvariant();Touch(updatedUtc);}

    /// <summary>Backwards-compatible quota update; staff stays at its current effective limit.</summary>
    public void SetQuotas(int maxStores,int maxUsers,int maxCameras,DateTime updatedUtc)=>SetQuotas(maxStores,maxUsers,MaxStaff>0?MaxStaff:maxUsers,maxCameras,updatedUtc);

    /// <summary>Applies Phase 9 plan limits, including a distinct staff quota.</summary>
    public void SetQuotas(int maxStores,int maxUsers,int maxStaff,int maxCameras,DateTime updatedUtc)
    {ValidateQuotas(maxStores,maxUsers,maxStaff,maxCameras);MaxStores=maxStores;MaxUsers=maxUsers;MaxStaff=maxStaff;MaxCameras=maxCameras;Touch(updatedUtc);}

    public void ConfigureSubscription(long subscriptionPlanId,SubscriptionStatus status,DateTime? trialStartsUtc,DateTime? trialEndsUtc,DateTime? subscriptionStartsUtc,DateTime? subscriptionEndsUtc,DateTime updatedUtc)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subscriptionPlanId);ValidatePeriod(trialStartsUtc,trialEndsUtc,nameof(trialEndsUtc));ValidatePeriod(subscriptionStartsUtc,subscriptionEndsUtc,nameof(subscriptionEndsUtc));SubscriptionPlanId=subscriptionPlanId;SubscriptionStatus=status;TrialStartsUtc=OptionalUtc(trialStartsUtc,nameof(trialStartsUtc));TrialEndsUtc=OptionalUtc(trialEndsUtc,nameof(trialEndsUtc));SubscriptionStartsUtc=OptionalUtc(subscriptionStartsUtc,nameof(subscriptionStartsUtc));SubscriptionEndsUtc=OptionalUtc(subscriptionEndsUtc,nameof(subscriptionEndsUtc));Touch(updatedUtc);}

    private static string Require(string value,string parameterName,int maximumLength){ArgumentException.ThrowIfNullOrWhiteSpace(value,parameterName);var normalized=value.Trim();return normalized.Length<=maximumLength?normalized:throw new ArgumentOutOfRangeException(parameterName,$"Value cannot exceed {maximumLength} characters.");}
    private static string? Optional(string? value,string parameterName,int maximumLength){if(string.IsNullOrWhiteSpace(value))return null;return Require(value,parameterName,maximumLength);}
    private static void ValidateQuotas(int maxStores,int maxUsers,int maxStaff,int maxCameras){ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStores);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxUsers);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStaff);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCameras);}
    private static void ValidatePeriod(DateTime? startsUtc,DateTime? endsUtc,string parameterName){if(startsUtc.HasValue&&endsUtc.HasValue&&endsUtc<=startsUtc)throw new ArgumentException("Period end must be later than its start.",parameterName);}
    private static DateTime? OptionalUtc(DateTime? value,string parameterName)=>value.HasValue?RequireUtc(value.Value,parameterName):null;
    private void Touch(DateTime utc){UpdatedUtc=RequireUtc(utc,nameof(utc));RowVersion=NewRowVersion();}
    private static byte[] NewRowVersion()=>Guid.NewGuid().ToByteArray();
    private static DateTime RequireUtc(DateTime value,string parameterName)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",parameterName);
}
