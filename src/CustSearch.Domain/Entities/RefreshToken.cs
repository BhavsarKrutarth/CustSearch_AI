namespace CustSearch.Domain.Entities;

/// <summary>
/// Stores only the SHA-256 hash and lifecycle metadata for a rotating refresh token.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        long userId,
        string tokenHash,
        Guid familyId,
        string issuedSecurityStamp,
        DateTime createdUtc,
        DateTime expiresUtc,
        string? createdByIp)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        UserId = userId;
        TokenHash = RequireHash(tokenHash);
        FamilyId = familyId == Guid.Empty ? throw new ArgumentException("Family ID is required.", nameof(familyId)) : familyId;
        IssuedSecurityStamp = RequireSecurityStamp(issuedSecurityStamp);
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        ExpiresUtc = RequireUtc(expiresUtc, nameof(expiresUtc));
        if (ExpiresUtc <= CreatedUtc)
        {
            throw new ArgumentException("Refresh-token expiry must be after creation.", nameof(expiresUtc));
        }

        CreatedByIp = NormalizeOptional(createdByIp, 64);
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }

    public UserAccount User { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;

    public Guid FamilyId { get; private set; }

    /// <summary>
    /// Captures the user's session version when this token is issued so password changes invalidate it.
    /// </summary>
    public string IssuedSecurityStamp { get; private set; } = string.Empty;

    public DateTime CreatedUtc { get; private set; }

    public DateTime ExpiresUtc { get; private set; }

    public DateTime? RevokedUtc { get; private set; }

    public string? RevokedReason { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public static RefreshToken Issue(
        long userId,
        string tokenHash,
        Guid familyId,
        string issuedSecurityStamp,
        DateTime createdUtc,
        DateTime expiresUtc,
        string? createdByIp) =>
        new(userId, tokenHash, familyId, issuedSecurityStamp, createdUtc, expiresUtc, createdByIp);

    public bool IsExpired(DateTime utcNow) => ExpiresUtc <= RequireUtc(utcNow, nameof(utcNow));

    public bool IsActive(DateTime utcNow) => RevokedUtc is null && !IsExpired(utcNow);

    public void Rotate(DateTime revokedUtc, string replacementHash, string? revokedByIp) =>
        Revoke(revokedUtc, "Rotated", revokedByIp, replacementHash);

    public void Revoke(
        DateTime revokedUtc,
        string reason,
        string? revokedByIp,
        string? replacementHash = null)
    {
        if (RevokedUtc is not null)
        {
            return;
        }

        RevokedUtc = RequireUtc(revokedUtc, nameof(revokedUtc));
        RevokedReason = NormalizeOptional(reason, 100)
            ?? throw new ArgumentException("Revocation reason is required.", nameof(reason));
        RevokedByIp = NormalizeOptional(revokedByIp, 64);
        ReplacedByTokenHash = replacementHash is null ? null : RequireHash(replacementHash);
    }

    private static string RequireHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Length == 64 && value.All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : throw new ArgumentException("Token hash must be a 64-character SHA-256 hex value.", nameof(value));
    }

    private static string RequireSecurityStamp(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        return normalized.Length <= 64
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(value), "Security stamp cannot exceed 64 characters.");
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(value), $"Value cannot exceed {maximumLength} characters.");
    }

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);
}
