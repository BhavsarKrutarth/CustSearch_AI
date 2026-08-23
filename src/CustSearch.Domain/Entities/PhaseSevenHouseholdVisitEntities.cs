using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>Phase 7A tenant-owned verified household/family grouping. Membership is always explicit and human-verifiable.</summary>
public sealed class Household
{
    private Household() { }
    private Household(long tenantId, string code, string name, string? notes, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        TenantId = tenantId;
        HouseholdCode = Require(code, nameof(code), 50).ToUpperInvariant();
        Name = Require(name, nameof(name), 150);
        Notes = Optional(notes, nameof(notes), 1000);
        IsActive = true;
        CreatedUtc = RequireUtc(utcNow, nameof(utcNow));
        UpdatedUtc = CreatedUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public string HouseholdCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static Household Create(long tenantId, string code, string name, string? notes, DateTime utcNow) => new(tenantId, code, name, notes, utcNow);

    public void Update(string name, string? notes, bool isActive, DateTime utcNow)
    {
        Name = Require(name, nameof(name), 150);
        Notes = Optional(notes, nameof(notes), 1000);
        IsActive = isActive;
        UpdatedUtc = RequireUtc(utcNow, nameof(utcNow));
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : throw new ArgumentOutOfRangeException(name);
    }
    private static string? Optional(string? value, string name, int max) => string.IsNullOrWhiteSpace(value) ? null : Require(value, name, max);
    private static DateTime RequireUtc(DateTime value, string name) => value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", name);
}

/// <summary>Phase 7B explicit customer-to-household relationship. No face-derived/inferred-family source exists.</summary>
public sealed class HouseholdMember
{
    private HouseholdMember() { }
    private HouseholdMember(long tenantId, long householdId, long customerId, string relationshipType,
        HouseholdRelationshipSource relationshipSource, long verifiedByUserId, DateTime verifiedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(householdId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(verifiedByUserId);
        if (!Enum.IsDefined(relationshipSource)) throw new ArgumentOutOfRangeException(nameof(relationshipSource));
        TenantId = tenantId;
        HouseholdId = householdId;
        CustomerId = customerId;
        RelationshipType = Require(relationshipType, nameof(relationshipType), 50);
        RelationshipSource = relationshipSource;
        IsVerified = true;
        VerifiedByUserId = verifiedByUserId;
        VerifiedUtc = RequireUtc(verifiedUtc, nameof(verifiedUtc));
        IsActive = true;
        CreatedUtc = VerifiedUtc;
        UpdatedUtc = VerifiedUtc;
    }

    public long TenantId { get; private set; }
    public long HouseholdId { get; private set; }
    public Household Household { get; private set; } = null!;
    public long CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public string RelationshipType { get; private set; } = string.Empty;
    public HouseholdRelationshipSource RelationshipSource { get; private set; }
    public bool IsVerified { get; private set; }
    public long VerifiedByUserId { get; private set; }
    public UserAccount VerifiedByUser { get; private set; } = null!;
    public DateTime VerifiedUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static HouseholdMember Link(long tenantId, long householdId, long customerId, string relationshipType,
        HouseholdRelationshipSource relationshipSource, long verifiedByUserId, DateTime verifiedUtc) =>
        new(tenantId, householdId, customerId, relationshipType, relationshipSource, verifiedByUserId, verifiedUtc);

    public void Update(string relationshipType, HouseholdRelationshipSource relationshipSource, bool isActive,
        long verifiedByUserId, DateTime verifiedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(verifiedByUserId);
        if (!Enum.IsDefined(relationshipSource)) throw new ArgumentOutOfRangeException(nameof(relationshipSource));
        RelationshipType = Require(relationshipType, nameof(relationshipType), 50);
        RelationshipSource = relationshipSource;
        IsVerified = true;
        VerifiedByUserId = verifiedByUserId;
        VerifiedUtc = RequireUtc(verifiedUtc, nameof(verifiedUtc));
        IsActive = isActive;
        UpdatedUtc = VerifiedUtc;
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : throw new ArgumentOutOfRangeException(name);
    }
    private static DateTime RequireUtc(DateTime value, string name) => value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", name);
}

/// <summary>Phase 7C store-bound observed co-visit group. It is evidence of visiting together, never evidence of family.</summary>
public sealed class VisitParty
{
    private VisitParty() { }
    private VisitParty(long tenantId, long storeId, string partyCode, DateTime startedUtc, VisitPartySource source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        TenantId = tenantId; StoreId = storeId; PartyCode = Require(partyCode, nameof(partyCode), 50).ToUpperInvariant();
        StartedUtc = RequireUtc(startedUtc, nameof(startedUtc)); Source = source; Status = VisitPartyStatus.Open;
        CreatedUtc = StartedUtc; UpdatedUtc = StartedUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public string PartyCode { get; private set; } = string.Empty;
    public DateTime StartedUtc { get; private set; }
    public DateTime? EndedUtc { get; private set; }
    public VisitPartySource Source { get; private set; }
    public VisitPartyStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static VisitParty Create(long tenantId, long storeId, string partyCode, DateTime startedUtc, VisitPartySource source) => new(tenantId, storeId, partyCode, startedUtc, source);
    public void Close(DateTime endedUtc)
    {
        endedUtc = RequireUtc(endedUtc, nameof(endedUtc));
        if (endedUtc < StartedUtc) throw new ArgumentOutOfRangeException(nameof(endedUtc));
        EndedUtc = endedUtc; Status = VisitPartyStatus.Closed; UpdatedUtc = endedUtc;
    }

    private static string Require(string value, string name, int max) { ArgumentException.ThrowIfNullOrWhiteSpace(value, name); var v=value.Trim(); return v.Length<=max?v:throw new ArgumentOutOfRangeException(name); }
    private static DateTime RequireUtc(DateTime value, string name) => value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", name);
}

/// <summary>Phase 7C one separately identified participant in a Visit Party. Exactly one identity reference is allowed.</summary>
public sealed class VisitPartyMember
{
    private VisitPartyMember() { }
    private VisitPartyMember(long tenantId, long storeId, long visitPartyId, VisitPartyMemberIdentityType identityType,
        long? customerId, long? anonymousVisitorId, DateTime joinedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(visitPartyId);
        if (!Enum.IsDefined(identityType)) throw new ArgumentOutOfRangeException(nameof(identityType));
        var valid = identityType == VisitPartyMemberIdentityType.Customer
            ? customerId is > 0 && anonymousVisitorId is null
            : anonymousVisitorId is > 0 && customerId is null;
        if (!valid) throw new ArgumentException("Visit party member must reference exactly one identity matching IdentityType.");
        TenantId=tenantId; StoreId=storeId; VisitPartyId=visitPartyId; IdentityType=identityType; CustomerId=customerId; AnonymousVisitorId=anonymousVisitorId;
        JoinedUtc = joinedUtc.Kind == DateTimeKind.Utc ? joinedUtc : throw new ArgumentException("JoinedUtc must be UTC.", nameof(joinedUtc));
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public long StoreId { get; private set; }
    public long VisitPartyId { get; private set; }
    public VisitParty VisitParty { get; private set; } = null!;
    public VisitPartyMemberIdentityType IdentityType { get; private set; }
    public long? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public long? AnonymousVisitorId { get; private set; }
    public AnonymousVisitor? AnonymousVisitor { get; private set; }
    public DateTime JoinedUtc { get; private set; }

    public static VisitPartyMember ForCustomer(long tenantId,long storeId,long partyId,long customerId,DateTime joinedUtc) => new(tenantId,storeId,partyId,VisitPartyMemberIdentityType.Customer,customerId,null,joinedUtc);
    public static VisitPartyMember ForAnonymousVisitor(long tenantId,long storeId,long partyId,long visitorId,DateTime joinedUtc) => new(tenantId,storeId,partyId,VisitPartyMemberIdentityType.AnonymousVisitor,null,visitorId,joinedUtc);
}

/// <summary>Phase 7D factual store/customer visit history. Purchase/invoice information is intentionally not stored here.</summary>
public sealed class CustomerVisit
{
    private CustomerVisit() { }
    private CustomerVisit(long tenantId,long storeId,long customerId,string visitCode,DateTime enteredUtc,CustomerVisitSource source,long? visitPartyId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        if (visitPartyId is <= 0) throw new ArgumentOutOfRangeException(nameof(visitPartyId));
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        TenantId=tenantId; StoreId=storeId; CustomerId=customerId; VisitCode=Require(visitCode,nameof(visitCode),50).ToUpperInvariant();
        EnteredUtc=RequireUtc(enteredUtc,nameof(enteredUtc)); Source=source; VisitPartyId=visitPartyId; Status=CustomerVisitStatus.Open; CreatedUtc=EnteredUtc; UpdatedUtc=EnteredUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public long CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public long? VisitPartyId { get; private set; }
    public VisitParty? VisitParty { get; private set; }
    public string VisitCode { get; private set; } = string.Empty;
    public DateTime EnteredUtc { get; private set; }
    public DateTime? ExitedUtc { get; private set; }
    public CustomerVisitSource Source { get; private set; }
    public CustomerVisitStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static CustomerVisit Create(long tenantId,long storeId,long customerId,string visitCode,DateTime enteredUtc,CustomerVisitSource source,long? visitPartyId=null) => new(tenantId,storeId,customerId,visitCode,enteredUtc,source,visitPartyId);
    public void Complete(DateTime exitedUtc)
    {
        exitedUtc=RequireUtc(exitedUtc,nameof(exitedUtc)); if(exitedUtc<EnteredUtc) throw new ArgumentOutOfRangeException(nameof(exitedUtc));
        ExitedUtc=exitedUtc; Status=CustomerVisitStatus.Completed; UpdatedUtc=exitedUtc;
    }

    private static string Require(string value,string name,int max){ArgumentException.ThrowIfNullOrWhiteSpace(value,name);var v=value.Trim();return v.Length<=max?v:throw new ArgumentOutOfRangeException(name);}
    private static DateTime RequireUtc(DateTime value,string name)=>value.Kind==DateTimeKind.Utc?value:throw new ArgumentException("Timestamp must be UTC.",name);
}