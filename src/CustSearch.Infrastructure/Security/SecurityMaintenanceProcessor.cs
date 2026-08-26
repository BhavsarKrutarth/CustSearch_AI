using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.Security;
using Dapper;

namespace CustSearch.Infrastructure.Security;

/// <summary>Idempotent Phase 18 maintenance batch. External channel adapters consume rows left pending.</summary>
public sealed class SecurityMaintenanceProcessor(IDbConnectionFactory connections,ISecurityEvidenceStore evidenceStore):ISecurityMaintenanceProcessor
{
    public async Task<SecurityMaintenanceResult>RunOnceAsync(CancellationToken ct=default)
    {
        await using var db=await connections.OpenConnectionAsync(ct).ConfigureAwait(false);
        var expired=(await db.QueryAsync<ExpiredEvidence>(new CommandDefinition("SELECT TOP(500)Id,StorageObjectKey FROM dbo.SecurityIncidentEvidence WHERE DeletedUtc IS NULL AND RetentionUntilUtc<=SYSUTCDATETIME() ORDER BY Id",cancellationToken:ct)).ConfigureAwait(false)).AsList();
        foreach(var item in expired)await evidenceStore.DeleteAsync(item.StorageObjectKey,ct).ConfigureAwait(false);
        await using var tx=await db.BeginTransactionAsync(ct).ConfigureAwait(false);
        var notifications=await db.ExecuteAsync(new CommandDefinition(@"UPDATE TOP(200) dbo.SecurityNotificationDeliveries SET Status=3,AttemptCount=AttemptCount+1,SentUtc=SYSUTCDATETIME(),ProviderMessageId=COALESCE(ProviderMessageId,N'in-app') WHERE Status IN(1,2) AND NextAttemptUtc<=SYSUTCDATETIME() AND Channel=N'InApp'",transaction:tx,cancellationToken:ct)).ConfigureAwait(false);
        var escalations=await db.ExecuteAsync(new CommandDefinition(@"INSERT dbo.SecurityNotificationDeliveries(TenantId,StoreId,SecurityIncidentId,Channel,Status,AttemptCount,QueuedUtc,NextAttemptUtc,IdempotencyKey)
SELECT TOP(100)i.TenantId,i.StoreId,i.Id,N'InApp',1,0,SYSUTCDATETIME(),SYSUTCDATETIME(),CONCAT(N'security-escalation:',i.Id,N':',i.Status)
FROM dbo.SecurityIncidents i WHERE i.Status IN(2,3) AND i.UpdatedUtc<DATEADD(MINUTE,-5,SYSUTCDATETIME()) AND NOT EXISTS(SELECT 1 FROM dbo.SecurityNotificationDeliveries d WHERE d.IdempotencyKey=CONCAT(N'security-escalation:',i.Id,N':',i.Status))",transaction:tx,cancellationToken:ct)).ConfigureAwait(false);
        var evidence=expired.Count==0?0:await db.ExecuteAsync(new CommandDefinition("UPDATE dbo.SecurityIncidentEvidence SET DeletedUtc=SYSUTCDATETIME() WHERE DeletedUtc IS NULL AND Id IN @Ids",new{Ids=expired.Select(x=>x.Id).ToArray()},tx,cancellationToken:ct)).ConfigureAwait(false);
        var payments=await db.ExecuteAsync(new CommandDefinition(@"INSERT dbo.SecurityPaymentCorrelations(TenantId,StoreId,SecurityIncidentId,InvoiceId,TransactionReference,MatchType,MatchScore,MatchedUtc,Notes)
SELECT TOP(200)i.TenantId,i.StoreId,i.Id,r.Id,r.InvoiceNumber,1,1,SYSUTCDATETIME(),N'Idempotent worker re-correlation by visit and paid invoice'
FROM dbo.SecurityIncidents i JOIN dbo.RetailInvoices r ON r.TenantId=i.TenantId AND r.StoreId=i.StoreId AND r.CustomerVisitId=i.VisitId AND r.Status=4 AND r.BalanceAmount=0 AND r.CancelledUtc IS NULL
WHERE i.Status IN(2,3,4,5) AND NOT EXISTS(SELECT 1 FROM dbo.SecurityPaymentCorrelations p WHERE p.TenantId=i.TenantId AND p.StoreId=i.StoreId AND p.SecurityIncidentId=i.Id AND p.InvoiceId=r.Id)",transaction:tx,cancellationToken:ct)).ConfigureAwait(false);
        var stale=await db.ExecuteAsync(new CommandDefinition(@"DECLARE @Expired TABLE(TenantId BIGINT,StoreId BIGINT,Id BIGINT,OldStatus TINYINT);
UPDATE TOP(200) i SET Status=8,ResolutionCode=N'STALE_CANDIDATE_EXPIRED',ResolutionNotes=N'No additional risk signal arrived during the review window.',UpdatedUtc=SYSUTCDATETIME() OUTPUT inserted.TenantId,inserted.StoreId,inserted.Id,deleted.Status INTO @Expired FROM dbo.SecurityIncidents i WHERE i.Status=2 AND i.UpdatedUtc<DATEADD(HOUR,-24,SYSUTCDATETIME());
INSERT dbo.SecurityIncidentActions(TenantId,StoreId,SecurityIncidentId,ActionType,FromStatus,ToStatus,ActorType,ReasonCode,Notes,CorrelationId)SELECT TenantId,StoreId,Id,N'StaleCandidateExpired',OldStatus,8,N'Worker',N'STALE_CANDIDATE_EXPIRED',N'Idempotent stale candidate expiry',CONCAT(N'worker-stale-',Id) FROM @Expired;",transaction:tx,cancellationToken:ct)).ConfigureAwait(false);
        var open=await db.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT_BIG(*) FROM dbo.SecurityIncidents WHERE Status IN(2,3,4,5)",transaction:tx,cancellationToken:ct)).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);return new(notifications,escalations,evidence,payments,stale,open);
    }
    private sealed class ExpiredEvidence{public long Id{get;set;}public string StorageObjectKey{get;set;}="";}
}
