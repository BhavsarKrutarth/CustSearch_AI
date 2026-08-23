namespace CustSearch.Domain.Entities;

/// <summary>Phase 5B — authoritative server-side relation between a tenant user and a tenant store.</summary>
public sealed class UserStoreAssignment
{
    private UserStoreAssignment() { }

    private UserStoreAssignment(long tenantId, long userId, long storeId, bool isPrimary, DateTime assignedUtc, long assignedByUserId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assignedByUserId);
        if (assignedUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Timestamp must be UTC.", nameof(assignedUtc));
        TenantId = tenantId;
        UserId = userId;
        StoreId = storeId;
        IsPrimary = isPrimary;
        AssignedUtc = assignedUtc;
        AssignedByUserId = assignedByUserId;
    }

    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long UserId { get; private set; }
    public UserAccount User { get; private set; } = null!;
    public long StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public DateTime AssignedUtc { get; private set; }
    public long AssignedByUserId { get; private set; }
    public UserAccount AssignedByUser { get; private set; } = null!;

    public static UserStoreAssignment Assign(long tenantId, long userId, long storeId, bool isPrimary, DateTime assignedUtc, long assignedByUserId) =>
        new(tenantId, userId, storeId, isPrimary, assignedUtc, assignedByUserId);
}
