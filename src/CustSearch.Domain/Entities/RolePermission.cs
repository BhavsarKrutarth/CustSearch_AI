using System.Diagnostics.CodeAnalysis;

namespace CustSearch.Domain.Entities;

/// <summary>
/// Grants one permission to a role only when their platform or tenant scopes match.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "RolePermission is the established domain and database term for a role capability grant.")]
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    private RolePermission(Role role, Permission permission)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permission);

        if (role.Scope != permission.Scope)
        {
            throw new ArgumentException("A role can only receive permissions from the same scope.", nameof(permission));
        }

        Role = role;
        RoleId = role.Id;
        Permission = permission;
        PermissionId = permission.Id;
    }

    public long RoleId { get; private set; }

    public Role Role { get; private set; } = null!;

    public long PermissionId { get; private set; }

    public Permission Permission { get; private set; } = null!;

    public static RolePermission Grant(Role role, Permission permission) => new(role, permission);
}
