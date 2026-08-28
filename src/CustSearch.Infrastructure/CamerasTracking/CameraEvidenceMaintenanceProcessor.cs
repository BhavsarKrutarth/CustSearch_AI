using System.Data;
using System.Globalization;
using System.Text.Json;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.Security;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustSearch.Infrastructure.CamerasTracking;

public sealed partial class CameraEvidenceMaintenanceProcessor(
    CustSearchDbContext db,
    ISecurityEvidenceStore files,
    TimeProvider clock,
    ILogger<CameraEvidenceMaintenanceProcessor> logger) : ICameraEvidenceMaintenanceProcessor
{
    public async Task<CameraEvidenceMaintenanceResult> RunOnceAsync(
        int cleanupBatchSize,
        int reconciliationTenantBatchSize,
        TimeSpan reconciliationInterval,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cleanupBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cleanupBatchSize, 5000);
        ArgumentOutOfRangeException.ThrowIfLessThan(reconciliationTenantBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(reconciliationTenantBatchSize, 500);
        if (reconciliationInterval < TimeSpan.FromHours(1) || reconciliationInterval > TimeSpan.FromDays(7))
        {
            throw new ArgumentOutOfRangeException(nameof(reconciliationInterval));
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var expiredIds = await db.CameraEvidence
            .AsNoTracking()
            .Where(e => e.DeletedUtc == null && !e.IsPinned && e.RetentionUntilUtc <= now)
            .Where(e => db.TenantStoragePolicies.Any(p => p.TenantId == e.TenantId && p.AutoCleanupEnabled))
            .OrderBy(e => e.RetentionUntilUtc)
            .ThenBy(e => e.Id)
            .Select(e => e.Id)
            .Take(cleanupBatchSize)
            .ToListAsync(cancellationToken);

        var expiredDeleted = 0;
        var missingDeleted = 0;
        var tenantsReconciled = 0;
        var failed = 0;
        long bytesReleased = 0;

        foreach (var evidenceId in expiredIds)
        {
            try
            {
                var released = await DeleteExpiredAsync(evidenceId, now, cancellationToken);
                if (released.HasValue)
                {
                    expiredDeleted++;
                    bytesReleased = checked(bytesReleased + released.Value);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                LogEvidenceFailure(logger, evidenceId, exception);
                db.ChangeTracker.Clear();
            }
        }

        var reconcileBefore = now - reconciliationInterval;
        var tenantIds = await db.TenantStorageUsage
            .AsNoTracking()
            .Where(u => u.LastReconciledUtc == null || u.LastReconciledUtc <= reconcileBefore)
            .OrderBy(u => u.LastReconciledUtc)
            .ThenBy(u => u.TenantId)
            .Select(u => u.TenantId)
            .Take(reconciliationTenantBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var result = await ReconcileTenantAsync(tenantId, now, cancellationToken);
                tenantsReconciled++;
                missingDeleted += result.MissingDeleted;
                bytesReleased = checked(bytesReleased + result.BytesReleased);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                LogTenantFailure(logger, tenantId, exception);
                db.ChangeTracker.Clear();
            }
        }

        return new(expiredDeleted, missingDeleted, tenantsReconciled, failed, bytesReleased);
    }

    private async Task<long?> DeleteExpiredAsync(long evidenceId, DateTime now, CancellationToken cancellationToken)
    {
        var candidate = await db.CameraEvidence
            .AsNoTracking()
            .Where(e => e.Id == evidenceId && e.DeletedUtc == null && !e.IsPinned && e.RetentionUntilUtc <= now)
            .Select(e => new { e.Id, e.TenantId, e.StoreId, e.StorageObjectKey, e.FileSizeBytes })
            .SingleOrDefaultAsync(cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        var existed = await files.ExistsAsync(candidate.StorageObjectKey, cancellationToken);
        await files.DeleteAsync(candidate.StorageObjectKey, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockTenantAsync(candidate.TenantId, cancellationToken);
        var evidence = await db.CameraEvidence.SingleOrDefaultAsync(
            e => e.Id == candidate.Id && e.DeletedUtc == null && !e.IsPinned && e.RetentionUntilUtc <= now,
            cancellationToken);
        if (evidence is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var usage = await db.TenantStorageUsage.SingleAsync(u => u.TenantId == evidence.TenantId, cancellationToken);
        evidence.MarkDeleted(now, existed ? "RetentionExpired" : "RetentionExpiredObjectMissing");
        usage.Release(evidence.EvidenceType, evidence.FileSizeBytes, now, cleanup: true);
        AddAudit(evidence, existed ? "EvidenceRetentionDeleted" : "EvidenceRetentionMissingObject", now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return evidence.FileSizeBytes;
    }

    private async Task<ReconciliationResult> ReconcileTenantAsync(long tenantId, DateTime now, CancellationToken cancellationToken)
    {
        var snapshot = await db.CameraEvidence
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.DeletedUtc == null)
            .Select(e => new { e.Id, e.StorageObjectKey })
            .ToListAsync(cancellationToken);
        var missingIds = new HashSet<long>();
        foreach (var item in snapshot)
        {
            if (!await files.ExistsAsync(item.StorageObjectKey, cancellationToken))
            {
                missingIds.Add(item.Id);
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await LockTenantAsync(tenantId, cancellationToken);
        var evidence = await db.CameraEvidence
            .Where(e => e.TenantId == tenantId && e.DeletedUtc == null)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
        var missing = evidence.Where(e => missingIds.Contains(e.Id)).ToArray();
        foreach (var item in missing)
        {
            item.MarkDeleted(now, "ObjectMissingDuringReconciliation");
            AddAudit(item, "EvidenceReconciliationMissingObject", now);
        }

        var active = evidence.Where(e => !e.IsDeleted).ToArray();
        var snapshotBytes = active.Where(e => e.EvidenceType == CameraEvidenceType.MotionSnapshot).Sum(e => e.FileSizeBytes);
        var clipBytes = active.Where(e => e.EvidenceType == CameraEvidenceType.MotionClip).Sum(e => e.FileSizeBytes);
        var securityBytes = active.Where(e => e.EvidenceType is CameraEvidenceType.SecuritySnapshot or CameraEvidenceType.SecurityClip).Sum(e => e.FileSizeBytes);
        var otherBytes = active.Where(e => e.EvidenceType is CameraEvidenceType.RecognitionReviewSnapshot or CameraEvidenceType.Other).Sum(e => e.FileSizeBytes);
        var usage = await db.TenantStorageUsage.SingleAsync(u => u.TenantId == tenantId, cancellationToken);
        usage.Reconcile(snapshotBytes, clipBytes, securityBytes, otherBytes, now, cleanup: missing.Length > 0);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return new(missing.Length, missing.Sum(e => e.FileSizeBytes));
    }

    private void AddAudit(CameraEvidence evidence, string action, DateTime now)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            evidence.Id,
            evidence.CameraId,
            evidence.EvidenceType,
            evidence.FileSizeBytes,
            evidence.RetentionUntilUtc,
            evidence.DeleteReason,
        });
        db.AuditLogs.Add(AuditLog.Record(
            evidence.TenantId,
            evidence.StoreId,
            null,
            "Worker",
            action,
            "CameraEvidence",
            evidence.Id.ToString(CultureInfo.InvariantCulture),
            null,
            metadata,
            null,
            null,
            $"evidence-retention:{evidence.Id}",
            now));
    }

    private async Task LockTenantAsync(long tenantId, CancellationToken cancellationToken)
    {
        if (!db.Database.IsSqlServer())
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sys.sp_getapplock @Resource={"tenant-storage:" + tenantId.ToString(CultureInfo.InvariantCulture)}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000",
            cancellationToken);
    }

    [LoggerMessage(EventId = 1812, Level = LogLevel.Error, Message = "Evidence retention failed for evidence id {EvidenceId}; a later reconciliation cycle will retry safely")]
    private static partial void LogEvidenceFailure(ILogger logger, long evidenceId, Exception exception);

    [LoggerMessage(EventId = 1813, Level = LogLevel.Error, Message = "Evidence reconciliation failed for tenant id {TenantId}; a later cycle will retry")]
    private static partial void LogTenantFailure(ILogger logger, long tenantId, Exception exception);

    private sealed record ReconciliationResult(int MissingDeleted, long BytesReleased);
}
