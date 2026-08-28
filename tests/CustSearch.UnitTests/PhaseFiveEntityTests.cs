using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

public sealed class PhaseFiveEntityTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 23, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void StoreRequiresCoordinatePair()
    {
        Assert.Throws<ArgumentException>(() => Store.Create(
            1, "SURAT-01", "VR Mall", "Dumas Road", null, null, "Surat", null, "Gujarat", "395007", "IN",
            21.15m, null, 50m, null, StoreLocationSource.Manual, "India Standard Time", null, null, UtcNow));
    }

    [Fact]
    public void StoreVerificationIsResetWhenLocationChanges()
    {
        var store = Store.Create(1, "SURAT-01", "VR Mall", "Dumas Road", null, null, "Surat", null, "Gujarat", "395007", "IN",
            21.150000m, 72.780000m, 50m, null, StoreLocationSource.MapPin, "India Standard Time", null, null, UtcNow);
        store.VerifyLocation(7, UtcNow.AddMinutes(1));
        Assert.True(store.IsLocationVerified);

        store.Update("VR Mall", "Dumas Road", null, null, "Surat", null, "Gujarat", "395007", "IN",
            21.151000m, 72.781000m, 75m, null, StoreLocationSource.MapPin, "India Standard Time", null, null, UtcNow.AddMinutes(2));

        Assert.False(store.IsLocationVerified);
        Assert.Null(store.LocationVerifiedUtc);
        Assert.Null(store.LocationVerifiedByUserId);
    }

    [Fact]
    public void StaffShiftEnforcesStateMachine()
    {
        var shift = StaffShift.Create(1, 2, 3, UtcNow, UtcNow.AddHours(8), 9, UtcNow);
        Assert.Equal(StaffShiftStatus.Scheduled, shift.Status);
        shift.Start(UtcNow.AddMinutes(1));
        Assert.Equal(StaffShiftStatus.Active, shift.Status);
        shift.Complete(UtcNow.AddHours(8));
        Assert.Equal(StaffShiftStatus.Completed, shift.Status);
        Assert.Throws<InvalidOperationException>(() => shift.Start(UtcNow.AddHours(9)));
    }

    [Fact]
    public void PresenceConfidenceMustBeNormalized()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StaffPresenceSession.Start(1, 2, 3, StaffPresenceSource.Cctv, UtcNow, 1.1m));
    }

    [Fact]
    public void VoiceTriggerIsStoreDynamicAndNotHardCoded()
    {
        var setting = StoreVoiceCommandSetting.Create(1, 3, "Mira Add", VoiceResponseMode.InAppAndVoice, UtcNow);
        Assert.Equal("Mira Add", setting.TriggerKeyword);
        setting.Update("Shop Add", VoiceResponseMode.InApp, true, true, UtcNow.AddMinutes(1));
        Assert.Equal("Shop Add", setting.TriggerKeyword);
    }

    [Fact]
    public void TenantUserProfileUpdateRotatesSecurityStamp()
    {
        var user = UserAccount.CreateTenant(1, "staff1", "staff@example.com", "Staff One", "hash", UtcNow);
        var before = user.SecurityStamp;
        user.UpdateProfile("staff2@example.com", "Staff Two");
        Assert.NotEqual(before, user.SecurityStamp);
        Assert.Equal("STAFF2@EXAMPLE.COM", user.NormalizedEmail);
    }

    [Fact]
    public void DisplayPasswordRetainsTheExactPasswordForConfiguredDirectStorage()
    {
        var user = UserAccount.CreateTenant(1, "staff1", "staff@example.com", "Staff One", "hash", UtcNow);
        user.SetDisplayPassword("Password with spaces 123 ");

        Assert.Equal("Password with spaces 123 ", user.DisplayPassword);
    }
}
