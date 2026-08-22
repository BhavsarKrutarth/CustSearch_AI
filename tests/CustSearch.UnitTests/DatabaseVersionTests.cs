using CustSearch.Domain.Entities;

namespace CustSearch.UnitTests;

public sealed class DatabaseVersionTests
{
    [Fact]
    public void RecordNormalizesValidValues()
    {
        var appliedUtc = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

        var version = DatabaseVersion.Record(" V1.0.0 ", " Foundation ", appliedUtc, " deployment ");

        Assert.Equal("V1.0.0", version.VersionNumber);
        Assert.Equal("Foundation", version.Description);
        Assert.Equal("deployment", version.AppliedBy);
        Assert.Equal(appliedUtc, version.AppliedUtc);
    }

    [Fact]
    public void RecordRejectsNonUtcAppliedTime()
    {
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            DatabaseVersion.Record("V1.0.0", "Foundation", localTime, "deployment"));
    }
}
