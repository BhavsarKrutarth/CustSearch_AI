namespace CustSearch.Domain.Enums;

/// <summary>Phase 5C — identifies how a store's canonical location was supplied.</summary>
public enum StoreLocationSource : byte
{
    Manual = 1,
    MapPin = 2,
    Geocoded = 3,
    Imported = 4,
}

/// <summary>Phase 5D — lifecycle of one staff work shift. CCTV presence is never payroll authority.</summary>
public enum StaffShiftStatus : byte
{
    Scheduled = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4,
}

/// <summary>Phase 5D — source that opened a staff presence session.</summary>
public enum StaffPresenceSource : byte
{
    Login = 1,
    Manual = 2,
    Cctv = 3,
    Badge = 4,
    Combined = 5,
}

/// <summary>Phase 5F — response channel used by a store's dynamic voice-command configuration.</summary>
public enum VoiceResponseMode : byte
{
    InApp = 1,
    Toast = 2,
    Voice = 3,
    InAppAndVoice = 4,
}
