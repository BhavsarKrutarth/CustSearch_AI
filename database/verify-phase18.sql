USE [CustSearch_AI];
GO
SET NOCOUNT ON;

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.17.0') THROW 56180,'Phase 18 version is missing.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE name IN(N'SecurityRules',N'SecurityObservations',N'SecurityIncidents',N'SecurityIncidentItems',N'SecurityIncidentEvidence',N'SecurityIncidentActions',N'SecurityNotificationDeliveries',N'SecurityPaymentCorrelations'))<>8 THROW 56181,'Phase 18 required tables are missing.',1;
IF (SELECT COUNT(*) FROM dbo.Permissions WHERE Scope=2 AND IsActive=1 AND Name IN(N'Security.Incidents.View',N'Security.Incidents.Acknowledge',N'Security.Incidents.Assign',N'Security.Incidents.Review',N'Security.Incidents.ConfirmLoss',N'Security.Incidents.Resolve',N'Security.Evidence.View',N'Security.Evidence.Export',N'Security.Settings.View',N'Security.Settings.Manage',N'Security.Rules.View',N'Security.Rules.Manage',N'Security.Reports.View'))<>13 THROW 56182,'Phase 18 permissions are incomplete.',1;
IF OBJECT_ID(N'dbo.SecurityObservation_Ingest',N'P') IS NULL OR OBJECT_ID(N'dbo.SecurityIncident_Transition',N'P') IS NULL THROW 56183,'Phase 18 procedures are missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityNotificationDeliveries') AND name=N'UX_SecurityDelivery_Idempotency' AND is_unique=1) THROW 56186,'Notification delivery idempotency index is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIngestionRequests') AND name=N'UX_SecurityIngestion_Nonce' AND is_unique=1) THROW 56187,'Ingestion replay protection index is missing.',1;
IF EXISTS(SELECT 1 FROM dbo.SecurityRules WHERE ISJSON(ConfigurationJson)<>1) THROW 56184,'Invalid rule JSON exists.',1;
IF EXISTS(SELECT 1 FROM dbo.SecurityIncidents WHERE Status=6 AND (ConfirmedByUserId IS NULL OR ConfirmedUtc IS NULL OR ResolutionCode IS NULL)) THROW 56185,'Human confirmation provenance is incomplete.',1;
SELECT N'Phase 18 schema verification passed' Result,(SELECT COUNT(*) FROM dbo.SecurityRules) SecurityRuleVersions,(SELECT COUNT(*) FROM dbo.SecurityIncidents) Incidents;
GO
