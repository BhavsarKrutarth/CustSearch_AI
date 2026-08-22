using CustSearch.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace CustSearch.Domain.Entities;

/// <summary>
/// Represents one stable authorization capability shared by the API and admin UI.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Permission is the established domain and database term for an authorization capability.")]
public sealed class Permission
{
    private Permission()
    {
    }

    private Permission(UserScope scope, string name, string description, DateTime createdUtc)
    {
        if (scope is not UserScope.Platform and not UserScope.Tenant)
        {
            throw new ArgumentOutOfRangeException(nameof(scope), "Permission scope must be platform or tenant.");
        }

        Scope = scope;
        Name = Require(name, nameof(name), 150);
        Description = Require(description, nameof(description), 300);
        IsActive = true;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
    }

    public long Id { get; private set; }

    public UserScope Scope { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();

    public static Permission Create(UserScope scope, string name, string description, DateTime createdUtc) =>
        new(scope, name, description, createdUtc);

    public void Deactivate() => IsActive = false;

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
