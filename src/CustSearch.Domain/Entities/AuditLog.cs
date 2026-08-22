namespace CustSearch.Domain.Entities;

/// <summary>
/// Stores a masked, tenant-aware record of security and administrative changes.
/// </summary>
public sealed class AuditLog
{
    private AuditLog()
    {
    }

    private AuditLog(
        long? tenantId,
        long? storeId,
        long? userId,
        string actorType,
        string action,
        string entityType,
        string? entityId,
        string? beforeJson,
        string? afterJson,
        string? ipAddress,
        string? userAgent,
        string correlationId,
        DateTime createdUtc)
    {
        if (tenantId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId));
        }

        if (storeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (userId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        TenantId = tenantId;
        StoreId = storeId;
        UserId = userId;
        ActorType = Require(actorType, nameof(actorType), 50);
        Action = Require(action, nameof(action), 100);
        EntityType = Require(entityType, nameof(entityType), 100);
        EntityId = Optional(entityId, nameof(entityId), 100);
        BeforeJson = Optional(beforeJson, nameof(beforeJson), 4000);
        AfterJson = Optional(afterJson, nameof(afterJson), 4000);
        IpAddress = Optional(ipAddress, nameof(ipAddress), 64);
        UserAgent = Optional(userAgent, nameof(userAgent), 500);
        CorrelationId = Require(correlationId, nameof(correlationId), 64);
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
    }

    public long Id { get; private set; }
    public long? TenantId { get; private set; }
    public Tenant? Tenant { get; private set; }
    public long? StoreId { get; private set; }
    public long? UserId { get; private set; }
    public UserAccount? User { get; private set; }
    public string ActorType { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }

    public static AuditLog Record(
        long? tenantId,
        long? storeId,
        long? userId,
        string actorType,
        string action,
        string entityType,
        string? entityId,
        string? beforeJson,
        string? afterJson,
        string? ipAddress,
        string? userAgent,
        string correlationId,
        DateTime createdUtc) =>
        new(
            tenantId,
            storeId,
            userId,
            actorType,
            action,
            entityType,
            entityId,
            beforeJson,
            afterJson,
            ipAddress,
            userAgent,
            correlationId,
            createdUtc);

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maximumLength} characters.");
    }

    private static string? Optional(string? value, string parameterName, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Require(value, parameterName, maximumLength);

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);
}
