namespace CustSearch.Domain.Entities;

/// <summary>
/// Records a security-safe authentication audit event without tokens or passwords.
/// </summary>
public sealed class AuthenticationEvent
{
    private AuthenticationEvent()
    {
    }

    private AuthenticationEvent(
        long? userId,
        long? tenantId,
        string eventType,
        bool isSuccess,
        string? failureCode,
        DateTime occurredUtc,
        string? ipAddress,
        string correlationId)
    {
        UserId = userId;
        TenantId = tenantId;
        EventType = Require(eventType, nameof(eventType), 60);
        IsSuccess = isSuccess;
        FailureCode = NormalizeOptional(failureCode, 60);
        OccurredUtc = occurredUtc.Kind == DateTimeKind.Utc
            ? occurredUtc
            : throw new ArgumentException("Timestamp must be UTC.", nameof(occurredUtc));
        IpAddress = NormalizeOptional(ipAddress, 64);
        CorrelationId = Require(correlationId, nameof(correlationId), 64);
    }

    public long Id { get; private set; }

    public long? UserId { get; private set; }

    public long? TenantId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public bool IsSuccess { get; private set; }

    public string? FailureCode { get; private set; }

    public DateTime OccurredUtc { get; private set; }

    public string? IpAddress { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public static AuthenticationEvent Record(
        long? userId,
        long? tenantId,
        string eventType,
        bool isSuccess,
        string? failureCode,
        DateTime occurredUtc,
        string? ipAddress,
        string correlationId) =>
        new(userId, tenantId, eventType, isSuccess, failureCode, occurredUtc, ipAddress, correlationId);

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maximumLength} characters.");
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : Require(normalized, nameof(value), maximumLength);
    }
}
