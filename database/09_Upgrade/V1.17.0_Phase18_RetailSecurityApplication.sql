/* Phase 18 application reconciliation. Requires the actual live V1.16.2 baseline. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
 OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.0')
 OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.2')
 THROW 56170,'Phase 18 foundation V1.16.0 and live baseline V1.16.2 are required.',1;
GO

/* Default access is intentionally limited to tenant ownership/admin roles. Store scope is still enforced by APIs. */
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.Scope=2 AND p.Name LIKE N'Security.%' AND p.IsActive=1
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER')
 AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityObservations') AND name=N'IX_SecurityObservations_Correlation')
 CREATE INDEX IX_SecurityObservations_Correlation ON dbo.SecurityObservations(TenantId,StoreId,OccurredUtc,ObservationType) INCLUDE(PersonTrackId,VisitId,ProductId,ProductCategoryId,Confidence);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIncidents') AND name=N'IX_SecurityIncidents_Metrics')
 CREATE INDEX IX_SecurityIncidents_Metrics ON dbo.SecurityIncidents(TenantId,StoreId,CreatedUtc,Status) INCLUDE(RiskScore,Severity,RuleVersion,ResolutionCode);
GO

CREATE OR ALTER PROCEDURE dbo.SecurityIncident_Transition
 @TenantId BIGINT,@StoreId BIGINT,@IncidentId BIGINT,@ToStatus TINYINT,@UserId BIGINT,
 @ReasonCode NVARCHAR(100)=NULL,@Notes NVARCHAR(2000)=NULL,@ExpectedRowVersion BINARY(8),@CorrelationId NVARCHAR(64)
AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;BEGIN TRY BEGIN TRAN;
 DECLARE @From TINYINT;
 SELECT @From=Status FROM dbo.SecurityIncidents WITH(UPDLOCK,HOLDLOCK)
 WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@IncidentId AND RowVersion=@ExpectedRowVersion;
 IF @From IS NULL THROW 56171,'Incident not found in scope or was modified.',1;
 IF NOT ((@From=1 AND @ToStatus=2) OR (@From=2 AND @ToStatus IN(3,5,8))
      OR (@From=3 AND @ToStatus IN(4,8)) OR (@From=4 AND @ToStatus IN(5,8))
      OR (@From=5 AND @ToStatus IN(6,7,8)) OR (@From IN(6,7,8) AND @ToStatus=9))
  THROW 56172,'Invalid incident state transition.',1;
 IF @ToStatus IN(6,7) AND NULLIF(LTRIM(RTRIM(@ReasonCode)),N'') IS NULL
  THROW 56173,'Confirmed loss and false positive require a human review reason.',1;
 UPDATE dbo.SecurityIncidents SET Status=@ToStatus,
  ResolutionCode=CASE WHEN @ToStatus IN(6,7,8) THEN @ReasonCode ELSE ResolutionCode END,
  ResolutionNotes=CASE WHEN @ToStatus IN(6,7,8) THEN @Notes ELSE ResolutionNotes END,
  ConfirmedByUserId=CASE WHEN @ToStatus=6 THEN @UserId WHEN @ToStatus=9 THEN ConfirmedByUserId ELSE NULL END,
  ConfirmedUtc=CASE WHEN @ToStatus=6 THEN SYSUTCDATETIME() WHEN @ToStatus=9 THEN ConfirmedUtc ELSE NULL END,
  UpdatedUtc=SYSUTCDATETIME()
 WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@IncidentId;
 INSERT dbo.SecurityIncidentActions(TenantId,StoreId,SecurityIncidentId,ActionType,FromStatus,ToStatus,UserId,ActorType,ReasonCode,Notes,OccurredUtc,CorrelationId)
 VALUES(@TenantId,@StoreId,@IncidentId,N'StatusChanged',@From,@ToStatus,@UserId,N'Human',@ReasonCode,@Notes,SYSUTCDATETIME(),@CorrelationId);
 INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
 VALUES(@TenantId,@StoreId,@UserId,N'User',N'SecurityIncident.StatusChanged',N'SecurityIncident',CONVERT(NVARCHAR(50),@IncidentId),JSON_OBJECT(N'Status':@From),JSON_OBJECT(N'Status':@ToStatus,N'ReasonCode':@ReasonCode),NULL,NULL,@CorrelationId,SYSUTCDATETIME());
 COMMIT;
 SELECT Id,Status,RowVersion,UpdatedUtc FROM dbo.SecurityIncidents WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@IncidentId;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.17.0')
 INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
 VALUES(N'V1.17.0',N'Phase 18 retail security application, authorization and state hardening',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.17.0')<>1
 THROW 56179,'V1.17.0 must exist exactly once.',1;
GO
