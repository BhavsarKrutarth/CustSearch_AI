namespace CustSearch.Domain.Entities;

/// <summary>
/// Preserves an audited platform override to one or more tenant resource limits.
/// </summary>
public sealed class TenantQuotaOverride
{
    private TenantQuotaOverride()
    {
    }

    private TenantQuotaOverride(
        long tenantId,
        int? maxStores,
        int? maxUsers,
        int? maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        string reason,
        long createdByUserId,
        DateTime createdUtc,
        DateTime? expiresUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);
        ValidateOptionalLimit(maxStores, nameof(maxStores));
        ValidateOptionalLimit(maxUsers, nameof(maxUsers));
        ValidateOptionalLimit(maxCameras, nameof(maxCameras));
        ValidateOptionalLimit(maxMonthlyRecognitions, nameof(maxMonthlyRecognitions));
        ValidateOptionalLimit(maxMonthlyApiCalls, nameof(maxMonthlyApiCalls));
        if (maxStores is null && maxUsers is null && maxCameras is null &&
            maxMonthlyRecognitions is null && maxMonthlyApiCalls is null)
        {
            throw new ArgumentException("At least one quota must be overridden.", nameof(maxStores));
        }

        TenantId = tenantId;
        MaxStores = maxStores;
        MaxUsers = maxUsers;
        MaxCameras = maxCameras;
        MaxMonthlyRecognitions = maxMonthlyRecognitions;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        Reason = Require(reason, nameof(reason), 500);
        CreatedByUserId = createdByUserId;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        ExpiresUtc = expiresUtc.HasValue ? RequireUtc(expiresUtc.Value, nameof(expiresUtc)) : null;
        if (ExpiresUtc <= CreatedUtc)
        {
            throw new ArgumentException("Override expiry must be later than creation.", nameof(expiresUtc));
        }
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public int? MaxStores { get; private set; }
    public int? MaxUsers { get; private set; }
    public int? MaxCameras { get; private set; }
    public long? MaxMonthlyRecognitions { get; private set; }
    public long? MaxMonthlyApiCalls { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public long CreatedByUserId { get; private set; }
    public UserAccount CreatedByUser { get; private set; } = null!;
    public DateTime CreatedUtc { get; private set; }
    public DateTime? ExpiresUtc { get; private set; }

    public static TenantQuotaOverride Create(
        long tenantId,
        int? maxStores,
        int? maxUsers,
        int? maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        string reason,
        long createdByUserId,
        DateTime createdUtc,
        DateTime? expiresUtc) =>
        new(
            tenantId,
            maxStores,
            maxUsers,
            maxCameras,
            maxMonthlyRecognitions,
            maxMonthlyApiCalls,
            reason,
            createdByUserId,
            createdUtc,
            expiresUtc);

    private static void ValidateOptionalLimit(long? value, string parameterName)
    {
        if (value is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
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
