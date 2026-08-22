namespace CustSearch.Domain.Entities;

/// <summary>
/// Represents a SQL schema version that was applied by a versioned database script.
/// </summary>
/// <remarks>
/// The application reads this entity for diagnostics only. Schema changes are never
/// applied through EF Core migrations or runtime database creation.
/// </remarks>
public sealed class DatabaseVersion
{
    private DatabaseVersion()
    {
    }

    private DatabaseVersion(string versionNumber, string description, DateTime appliedUtc, string appliedBy)
    {
        VersionNumber = Require(versionNumber, nameof(versionNumber), 50);
        Description = Require(description, nameof(description), 250);
        AppliedBy = Require(appliedBy, nameof(appliedBy), 100);
        AppliedUtc = appliedUtc.Kind == DateTimeKind.Utc
            ? appliedUtc
            : throw new ArgumentException("Applied time must be UTC.", nameof(appliedUtc));
    }

    public long VersionId { get; private set; }

    public string VersionNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTime AppliedUtc { get; private set; }

    public string AppliedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Creates an in-memory version record for application tests and administrative tooling.
    /// Production schema version records are inserted by SQL deployment scripts.
    /// </summary>
    public static DatabaseVersion Record(
        string versionNumber,
        string description,
        DateTime appliedUtc,
        string appliedBy) => new(versionNumber, description, appliedUtc, appliedBy);

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maximumLength} characters.");
    }
}
