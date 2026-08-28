namespace CustSearch.Application.CamerasTracking;

public interface ICameraEvidenceMaintenanceProcessor
{
    Task<CameraEvidenceMaintenanceResult> RunOnceAsync(
        int cleanupBatchSize,
        int reconciliationTenantBatchSize,
        TimeSpan reconciliationInterval,
        CancellationToken cancellationToken = default);
}

public sealed record CameraEvidenceMaintenanceResult(
    int ExpiredDeleted,
    int MissingDeleted,
    int TenantsReconciled,
    int Failed,
    long BytesReleased);

public sealed class EvidenceRetentionOptions
{
    public const string SectionName = "EvidenceRetention";

    public bool Enabled { get; set; } = true;
    public int PollMinutes { get; set; } = 60;
    public int CleanupBatchSize { get; set; } = 100;
    public int ReconciliationTenantBatchSize { get; set; } = 5;
    public int ReconciliationIntervalHours { get; set; } = 24;
    public int LeaseSeconds { get; set; } = 600;

    public bool IsValid() =>
        PollMinutes is >= 1 and <= 1440 &&
        CleanupBatchSize is >= 1 and <= 5000 &&
        ReconciliationTenantBatchSize is >= 1 and <= 500 &&
        ReconciliationIntervalHours is >= 1 and <= 168 &&
        LeaseSeconds is >= 30 and <= 600;
}
