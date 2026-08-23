namespace CustSearch.Domain.Enums;

/// <summary>Phase 7B explicit, human-verifiable source for a household relationship. AI/face inference is intentionally absent.</summary>
public enum HouseholdRelationshipSource : byte
{
    CustomerProvided = 1,
    StaffVerified = 2,
    AdminVerified = 3,
    ImportedVerified = 4,
}

/// <summary>Phase 7C describes how a co-visit party was observed/created without implying a family relationship.</summary>
public enum VisitPartySource : byte
{
    Manual = 1,
    CctvCoVisit = 2,
    Imported = 3,
    System = 4,
}

public enum VisitPartyStatus : byte
{
    Open = 1,
    Closed = 2,
    Cancelled = 3,
}

/// <summary>Exactly one linked identity is valid for each VisitPartyMember.</summary>
public enum VisitPartyMemberIdentityType : byte
{
    Customer = 1,
    AnonymousVisitor = 2,
}

public enum CustomerVisitSource : byte
{
    Manual = 1,
    Cctv = 2,
    Imported = 3,
    System = 4,
}

public enum CustomerVisitStatus : byte
{
    Open = 1,
    Completed = 2,
    Cancelled = 3,
}