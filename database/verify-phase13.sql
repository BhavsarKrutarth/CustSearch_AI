/* Phase 13 verification. Run after database/run-phase13.sql. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL THROW 54600,'DatabaseVersions is missing.',1;
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.12.0')<>1 THROW 54601,'V1.12.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.Cameras',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraZoneConfigurations',N'U') IS NULL OR OBJECT_ID(N'dbo.PersonTrackSessions',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraTrackHandoffs',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraOperationalEvents',N'U') IS NULL THROW 54602,'Phase 13 tables are missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraOperationalEvents') AND name=N'UX_CameraEvents_Service_Event' AND is_unique=1) OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraOperationalEvents') AND name=N'UX_CameraEvents_Service_Idempotency' AND is_unique=1) THROW 54603,'CCTV event replay/idempotency indexes are missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.PersonTrackSessions') AND name=N'CK_PersonTracks_Subject') THROW 54604,'Anonymous/customer/staff subject distinction constraint is missing.',1;
IF COL_LENGTH(N'dbo.Cameras',N'RtspConfigurationReference') IS NULL OR COL_LENGTH(N'dbo.Cameras',N'RtspUrl') IS NOT NULL OR COL_LENGTH(N'dbo.Cameras',N'Password') IS NOT NULL THROW 54605,'Camera credential storage contract is invalid.',1;
IF COL_LENGTH(N'dbo.CameraOperationalEvents',N'PayloadJson') IS NOT NULL OR COL_LENGTH(N'dbo.CameraOperationalEvents',N'RawFrame') IS NOT NULL OR COL_LENGTH(N'dbo.PersonTrackSessions',N'Embedding') IS NOT NULL THROW 54606,'Phase 13 must not store raw frames, payloads or biometric templates.',1;
IF OBJECT_ID(N'dbo.Camera_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.PersonTrack_Search',N'P') IS NULL THROW 54607,'Phase 13 read procedures are missing.',1;
PRINT N'PHASE13_DATABASE_VERIFICATION_GREEN';
GO
