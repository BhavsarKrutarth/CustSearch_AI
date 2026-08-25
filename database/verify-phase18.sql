USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.0')<>1 THROW 56100,'Phase 18 version is invalid.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE name IN(N'SecurityRules',N'SecurityIngestionRequests',N'SecurityObservations',N'SecurityIncidents',N'SecurityIncidentItems',N'SecurityIncidentEvidence',N'SecurityIncidentActions',N'SecurityNotificationDeliveries',N'SecurityPaymentCorrelations'))<>9 THROW 56101,'Phase 18 tables are missing.',1;
IF (SELECT COUNT(*) FROM sys.procedures WHERE name IN(N'SecurityObservation_Ingest',N'SecurityRule_List',N'SecurityRule_CreateVersion',N'SecurityIncident_Search',N'SecurityIncident_Get',N'SecurityIncident_Transition'))<>6 THROW 56102,'Phase 18 procedures are missing.',1;
IF (SELECT COUNT(*) FROM dbo.Permissions WHERE Scope=2 AND Name LIKE N'Security.%')<>13 THROW 56103,'Phase 18 permissions are invalid.',1;
IF EXISTS(SELECT 1 FROM dbo.SystemSettings WHERE TenantId IS NULL AND SettingKey IN(N'SecurityMonitoringEnabled',N'UnpaidExitDetectionEnabled',N'RealtimeSecurityAlertsEnabled') AND SettingValue<>N'false') THROW 56104,'Security rollout defaults must remain disabled.',1;
IF OBJECT_ID(N'dbo.SecurityWatchlistEntries',N'U') IS NOT NULL THROW 56105,'Unapproved watchlist table must not exist.',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
SELECT N'Phase 18 database verification passed' Result;
GO
