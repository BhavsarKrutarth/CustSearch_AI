using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>
/// Defines a reusable platform or tenant role that groups permissions for users.
/// </summary>
public sealed class Role
{
    private Role()
    {
    }

    private Role(
        long? tenantId,
        UserScope scope,
        string name,
        string description,
        bool isSystem,
        DateTime createdUtc)
    {
        ValidateOwner(tenantId, scope);
        TenantId = tenantId;
        Scope = scope;
        Name = Require(name, nameof(name), 100);
        NormalizedName = Name.ToUpperInvariant();
        Description = Require(description, nameof(description), 300);
        IsSystem = isSystem;
        IsActive = true;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
    }

    public long Id { get; private set; }

    public long? TenantId { get; private set; }

    public Tenant? Tenant { get; private set; }

    public UserScope Scope { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();

    public static Role CreatePlatform(string name, string description, bool isSystem, DateTime createdUtc) =>
        new(null, UserScope.Platform, name, description, isSystem, createdUtc);

    public static Role CreateTenant(
        long tenantId,
        string name,
        string description,
        bool isSystem,
        DateTime createdUtc) =>
        new(tenantId, UserScope.Tenant, name, description, isSystem, createdUtc);

    public void Deactivate() => IsActive = false;

    private static void ValidateOwner(long? tenantId, UserScope scope)
    {
        if (scope == UserScope.Platform && tenantId is not null)
        {
            throw new ArgumentException("Platform roles cannot belong to a tenant.", nameof(tenantId));
        }

        if (scope == UserScope.Tenant && tenantId is null or <= 0)
        {
            throw new ArgumentException("Tenant roles require a valid tenant.", nameof(tenantId));
        }
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
