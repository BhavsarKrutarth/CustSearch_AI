namespace CustSearch.Domain.Entities;

/// <summary>
/// Assigns one role to one user after verifying that both belong to the same authorization scope.
/// </summary>
public sealed class UserRole
{
    private UserRole()
    {
    }

    private UserRole(UserAccount user, Role role, DateTime assignedUtc, long? assignedByUserId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(role);

        if (user.Scope != role.Scope || user.TenantId != role.TenantId)
        {
            throw new ArgumentException("A user can only receive a role from the same platform or tenant scope.", nameof(role));
        }

        if (assignedByUserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assignedByUserId));
        }

        User = user;
        UserId = user.Id;
        Role = role;
        RoleId = role.Id;
        AssignedUtc = RequireUtc(assignedUtc, nameof(assignedUtc));
        AssignedByUserId = assignedByUserId;
    }

    public long UserId { get; private set; }

    public UserAccount User { get; private set; } = null!;

    public long RoleId { get; private set; }

    public Role Role { get; private set; } = null!;

    public DateTime AssignedUtc { get; private set; }

    public long? AssignedByUserId { get; private set; }

    public UserAccount? AssignedByUser { get; private set; }

    public static UserRole Assign(UserAccount user, Role role, DateTime assignedUtc, long? assignedByUserId = null) =>
        new(user, role, assignedUtc, assignedByUserId);

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);
}
