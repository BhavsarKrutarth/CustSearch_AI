using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>
/// Represents an auditable platform or tenant identity used by authentication.
/// </summary>
public sealed class UserAccount
{
    private UserAccount()
    {
    }

    private UserAccount(
        long? tenantId,
        UserScope scope,
        string userName,
        string email,
        string displayName,
        string passwordHash,
        DateTime createdUtc)
    {
        if (scope == UserScope.Platform && tenantId is not null)
        {
            throw new ArgumentException("Platform users cannot belong to a tenant.", nameof(tenantId));
        }

        if (scope == UserScope.Tenant && tenantId is null or <= 0)
        {
            throw new ArgumentException("Tenant users require a valid tenant.", nameof(tenantId));
        }

        TenantId = tenantId;
        Scope = scope;
        UserName = Require(userName, nameof(userName), 100);
        NormalizedUserName = UserName.ToUpperInvariant();
        Email = Require(email, nameof(email), 254);
        NormalizedEmail = Email.ToUpperInvariant();
        DisplayName = Require(displayName, nameof(displayName), 150);
        PasswordHash = Require(passwordHash, nameof(passwordHash), 500);
        SecurityStamp = Guid.NewGuid().ToString("N");
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        IsActive = true;
    }

    public long Id { get; private set; }

    public long? TenantId { get; private set; }

    public Tenant? Tenant { get; private set; }

    public UserScope Scope { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string SecurityStamp { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime? LastLoginUtc { get; private set; }

    public static UserAccount CreatePlatform(
        string userName,
        string email,
        string displayName,
        string passwordHash,
        DateTime createdUtc) =>
        new(null, UserScope.Platform, userName, email, displayName, passwordHash, createdUtc);

    public static UserAccount CreateTenant(
        long tenantId,
        string userName,
        string email,
        string displayName,
        string passwordHash,
        DateTime createdUtc) =>
        new(tenantId, UserScope.Tenant, userName, email, displayName, passwordHash, createdUtc);

    public void RecordSuccessfulLogin(DateTime loginUtc) => LastLoginUtc = RequireUtc(loginUtc, nameof(loginUtc));

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Require(passwordHash, nameof(passwordHash), 500);
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public void Deactivate()
    {
        IsActive = false;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maximumLength} characters.");
    }

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);
}
