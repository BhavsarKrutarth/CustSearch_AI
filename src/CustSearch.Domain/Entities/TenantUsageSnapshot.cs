namespace CustSearch.Domain.Entities;

/// <summary>
/// Stores an immutable tenant usage total for one reporting period.
/// </summary>
public sealed class TenantUsageSnapshot
{
    private TenantUsageSnapshot()
    {
    }

    private TenantUsageSnapshot(
        long tenantId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        int storeCount,
        int userCount,
        int cameraCount,
        long recognitionCount,
        long apiCallCount,
        DateTime capturedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegative(storeCount);
        ArgumentOutOfRangeException.ThrowIfNegative(userCount);
        ArgumentOutOfRangeException.ThrowIfNegative(cameraCount);
        ArgumentOutOfRangeException.ThrowIfNegative(recognitionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(apiCallCount);
        PeriodStartUtc = RequireUtc(periodStartUtc, nameof(periodStartUtc));
        PeriodEndUtc = RequireUtc(periodEndUtc, nameof(periodEndUtc));
        if (PeriodEndUtc <= PeriodStartUtc)
        {
            throw new ArgumentException("Usage period end must be later than its start.", nameof(periodEndUtc));
        }

        TenantId = tenantId;
        StoreCount = storeCount;
        UserCount = userCount;
        CameraCount = cameraCount;
        RecognitionCount = recognitionCount;
        ApiCallCount = apiCallCount;
        CapturedUtc = RequireUtc(capturedUtc, nameof(capturedUtc));
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public DateTime PeriodStartUtc { get; private set; }
    public DateTime PeriodEndUtc { get; private set; }
    public int StoreCount { get; private set; }
    public int UserCount { get; private set; }
    public int CameraCount { get; private set; }
    public long RecognitionCount { get; private set; }
    public long ApiCallCount { get; private set; }
    public DateTime CapturedUtc { get; private set; }

    public static TenantUsageSnapshot Capture(
        long tenantId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        int storeCount,
        int userCount,
        int cameraCount,
        long recognitionCount,
        long apiCallCount,
        DateTime capturedUtc) =>
        new(
            tenantId,
            periodStartUtc,
            periodEndUtc,
            storeCount,
            userCount,
            cameraCount,
            recognitionCount,
            apiCallCount,
            capturedUtc);

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);
}
