namespace CustSearch.Domain.Enums;

/// <summary>Phase 10 preference dimensions supported by factual signals and derived scores.</summary>
public enum PreferenceType : byte
{
    Category = 1,
    Product = 2,
    Brand = 3,
    PriceRange = 4,
    Tag = 5,
}

/// <summary>Auditable factual source of a customer preference signal.</summary>
public enum PreferenceSignalSource : byte
{
    ManualStaff = 1,
    Purchase = 2,
    CategoryInteraction = 3,
    VoiceConfirmed = 4,
}

/// <summary>Source of an explicit shared Household tag. No CCTV/co-visit inference source exists.</summary>
public enum HouseholdPreferenceTagSource : byte
{
    CustomerProvided = 1,
    StaffVerified = 2,
    AdminVerified = 3,
}

/// <summary>Server-authoritative lifecycle for one store voice command interaction.</summary>
public enum VoiceCommandSessionStatus : byte
{
    Listening = 1,
    PendingConfirmation = 2,
    Confirmed = 3,
    Rejected = 4,
    Expired = 5,
}
