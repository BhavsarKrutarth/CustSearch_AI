/*
 CustSearch AI — Phase 13 production database upgrade
 Version: V1.12.0
 Scope: tenant/store cameras, versioned zones, anonymous-first tracking, camera handoffs and idempotent metadata receipts.
 Privacy: stores opaque RTSP references and normalized metadata only; no raw frames, embeddings or inferred identities.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET ARITHABORT ON;SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.11.0') THROW 54500,'Phase 12 V1.11.0 must be installed before Phase 13.',1;
GO

IF OBJECT_ID(N'dbo.Cameras',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.Cameras(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cameras PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraCode NVARCHAR(50) NOT NULL,Name NVARCHAR(150) NOT NULL,RtspConfigurationReference NVARCHAR(200) NOT NULL,Status TINYINT NOT NULL CONSTRAINT DF_Cameras_Status DEFAULT(1),Location NVARCHAR(250) NULL,Direction TINYINT NOT NULL,IsActive BIT NOT NULL,LastHeartbeatUtc DATETIME2(7) NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_Cameras_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_Cameras_Stores FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT CK_Cameras_Status CHECK(Status BETWEEN 1 AND 4),CONSTRAINT CK_Cameras_Direction CHECK(Direction BETWEEN 1 AND 4),CONSTRAINT CK_Cameras_Period CHECK(UpdatedUtc>=CreatedUtc AND (LastHeartbeatUtc IS NULL OR LastHeartbeatUtc>=CreatedUtc)),CONSTRAINT CK_Cameras_Reference CHECK(LEN(LTRIM(RTRIM(RtspConfigurationReference)))>0 AND RtspConfigurationReference NOT LIKE N'rtsp://%')
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Cameras') AND name=N'UX_Cameras_Tenant_Store_Code') CREATE UNIQUE INDEX UX_Cameras_Tenant_Store_Code ON dbo.Cameras(TenantId,StoreId,CameraCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Cameras') AND name=N'UX_Cameras_Tenant_Store_Id') CREATE UNIQUE INDEX UX_Cameras_Tenant_Store_Id ON dbo.Cameras(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Cameras') AND name=N'IX_Cameras_Tenant_Store_Active_Status') CREATE INDEX IX_Cameras_Tenant_Store_Active_Status ON dbo.Cameras(TenantId,StoreId,IsActive,Status,Id);
GO

IF OBJECT_ID(N'dbo.CameraZoneConfigurations',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CameraZoneConfigurations(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraZoneConfigurations PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,ZoneCode NVARCHAR(50) NOT NULL,Name NVARCHAR(150) NOT NULL,ZoneType TINYINT NOT NULL,GeometryJson NVARCHAR(MAX) NOT NULL,Version INT NOT NULL,CategoryId BIGINT NULL,EffectiveUtc DATETIME2(7) NOT NULL,SupersededUtc DATETIME2(7) NULL,IsActive BIT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_CameraZones_Cameras FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_CameraZones_Type CHECK(ZoneType BETWEEN 1 AND 7),CONSTRAINT CK_CameraZones_Version CHECK(Version>=1),CONSTRAINT CK_CameraZones_Geometry CHECK(ISJSON(GeometryJson)=1),CONSTRAINT CK_CameraZones_Category CHECK((ZoneType=5 AND CategoryId IS NOT NULL) OR ZoneType<>5),CONSTRAINT CK_CameraZones_Period CHECK((IsActive=1 AND SupersededUtc IS NULL) OR (IsActive=0 AND SupersededUtc>=EffectiveUtc))
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraZoneConfigurations') AND name=N'UX_CameraZones_Tenant_Camera_Code_Version') CREATE UNIQUE INDEX UX_CameraZones_Tenant_Camera_Code_Version ON dbo.CameraZoneConfigurations(TenantId,CameraId,ZoneCode,Version);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraZoneConfigurations') AND name=N'UX_CameraZones_Current') CREATE UNIQUE INDEX UX_CameraZones_Current ON dbo.CameraZoneConfigurations(TenantId,CameraId,ZoneCode) WHERE IsActive=1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraZoneConfigurations') AND name=N'IX_CameraZones_Tenant_Store_Camera') CREATE INDEX IX_CameraZones_Tenant_Store_Camera ON dbo.CameraZoneConfigurations(TenantId,StoreId,CameraId,IsActive);
GO

IF OBJECT_ID(N'dbo.PersonTrackSessions',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.PersonTrackSessions(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PersonTrackSessions PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,PersonTrackId NVARCHAR(100) NOT NULL,StartUtc DATETIME2(7) NOT NULL,EndUtc DATETIME2(7) NULL,Confidence DECIMAL(5,4) NOT NULL,TrackingState TINYINT NOT NULL,SubjectKind TINYINT NOT NULL CONSTRAINT DF_PersonTracks_SubjectKind DEFAULT(1),CustomerId BIGINT NULL,StaffProfileId BIGINT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_PersonTracks_Cameras FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_PersonTracks_Confidence CHECK(Confidence BETWEEN 0 AND 1),CONSTRAINT CK_PersonTracks_State CHECK(TrackingState BETWEEN 1 AND 4),CONSTRAINT CK_PersonTracks_Subject CHECK((SubjectKind=1 AND CustomerId IS NULL AND StaffProfileId IS NULL) OR (SubjectKind=2 AND CustomerId IS NOT NULL AND StaffProfileId IS NULL) OR (SubjectKind=3 AND CustomerId IS NULL AND StaffProfileId IS NOT NULL)),CONSTRAINT CK_PersonTracks_Period CHECK(EndUtc IS NULL OR EndUtc>=StartUtc)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PersonTrackSessions') AND name=N'UX_PersonTracks_Tenant_Store_Track') CREATE UNIQUE INDEX UX_PersonTracks_Tenant_Store_Track ON dbo.PersonTrackSessions(TenantId,StoreId,PersonTrackId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PersonTrackSessions') AND name=N'UX_PersonTracks_Tenant_Store_Id') CREATE UNIQUE INDEX UX_PersonTracks_Tenant_Store_Id ON dbo.PersonTrackSessions(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PersonTrackSessions') AND name=N'IX_PersonTracks_Tenant_Store_State') CREATE INDEX IX_PersonTracks_Tenant_Store_State ON dbo.PersonTrackSessions(TenantId,StoreId,TrackingState,UpdatedUtc DESC,Id DESC);
GO

IF OBJECT_ID(N'dbo.CameraTrackHandoffs',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CameraTrackHandoffs(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraTrackHandoffs PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,PersonTrackSessionId BIGINT NOT NULL,FromCameraId BIGINT NOT NULL,ToCameraId BIGINT NOT NULL,Confidence DECIMAL(5,4) NOT NULL,GapMilliseconds INT NOT NULL,OccurredUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_CameraHandoffs_Track FOREIGN KEY(TenantId,StoreId,PersonTrackSessionId) REFERENCES dbo.PersonTrackSessions(TenantId,StoreId,Id),CONSTRAINT FK_CameraHandoffs_From FOREIGN KEY(TenantId,StoreId,FromCameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT FK_CameraHandoffs_To FOREIGN KEY(TenantId,StoreId,ToCameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_CameraHandoffs_Cameras CHECK(FromCameraId<>ToCameraId),CONSTRAINT CK_CameraHandoffs_Confidence CHECK(Confidence BETWEEN 0 AND 1),CONSTRAINT CK_CameraHandoffs_Gap CHECK(GapMilliseconds>=0)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraTrackHandoffs') AND name=N'IX_CameraHandoffs_Tenant_Store_Track') CREATE INDEX IX_CameraHandoffs_Tenant_Store_Track ON dbo.CameraTrackHandoffs(TenantId,StoreId,PersonTrackSessionId,OccurredUtc,Id);
GO

IF OBJECT_ID(N'dbo.CameraOperationalEvents',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CameraOperationalEvents(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraOperationalEvents PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,ServiceId NVARCHAR(100) NOT NULL,EventId NVARCHAR(150) NOT NULL,IdempotencyKey NVARCHAR(150) NOT NULL,EventType NVARCHAR(100) NOT NULL,ContractVersion INT NOT NULL,PayloadHash CHAR(64) NOT NULL,CorrelationId NVARCHAR(64) NOT NULL,OccurredUtc DATETIME2(7) NOT NULL,ReceivedUtc DATETIME2(7) NOT NULL,Status TINYINT NOT NULL,
  CONSTRAINT FK_CameraEvents_Cameras FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_CameraEvents_Status CHECK(Status BETWEEN 1 AND 4),CONSTRAINT CK_CameraEvents_Contract CHECK(ContractVersion=1),CONSTRAINT CK_CameraEvents_Hash CHECK(PayloadHash NOT LIKE N'%[^0-9a-f]%' AND LEN(PayloadHash)=64)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraOperationalEvents') AND name=N'UX_CameraEvents_Service_Event') CREATE UNIQUE INDEX UX_CameraEvents_Service_Event ON dbo.CameraOperationalEvents(ServiceId,EventId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraOperationalEvents') AND name=N'UX_CameraEvents_Service_Idempotency') CREATE UNIQUE INDEX UX_CameraEvents_Service_Idempotency ON dbo.CameraOperationalEvents(ServiceId,IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraOperationalEvents') AND name=N'IX_CameraEvents_Tenant_Store_Received') CREATE INDEX IX_CameraEvents_Tenant_Store_Received ON dbo.CameraOperationalEvents(TenantId,StoreId,ReceivedUtc DESC,Id DESC);
GO

CREATE OR ALTER PROCEDURE dbo.Camera_Search @TenantId BIGINT,@StoreId BIGINT=NULL
AS BEGIN SET NOCOUNT ON;SELECT Id,StoreId,CameraCode,Name,CONVERT(BIT,1) HasRtspConfiguration,Status,Location,Direction,IsActive,LastHeartbeatUtc,CreatedUtc,UpdatedUtc,RowVersion FROM dbo.Cameras WHERE TenantId=@TenantId AND (@StoreId IS NULL OR StoreId=@StoreId) ORDER BY StoreId,CameraCode;END;
GO
CREATE OR ALTER PROCEDURE dbo.PersonTrack_Search @TenantId BIGINT,@StoreId BIGINT=NULL,@AfterId BIGINT=NULL,@Take INT=100
AS BEGIN SET NOCOUNT ON;IF @Take<1 SET @Take=1;IF @Take>500 SET @Take=500;SELECT TOP(@Take) Id,StoreId,CameraId,PersonTrackId,StartUtc,EndUtc,Confidence,TrackingState,SubjectKind,CustomerId,StaffProfileId,UpdatedUtc FROM dbo.PersonTrackSessions WHERE TenantId=@TenantId AND (@StoreId IS NULL OR StoreId=@StoreId) AND (@AfterId IS NULL OR Id>@AfterId) ORDER BY Id;END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.12.0') INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.12.0',N'Phase 13 cameras, versioned zones, anonymous tracking, handoffs and authenticated CCTV metadata receipts',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.12.0')<>1 THROW 54590,'V1.12.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.Cameras',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraZoneConfigurations',N'U') IS NULL OR OBJECT_ID(N'dbo.PersonTrackSessions',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraTrackHandoffs',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraOperationalEvents',N'U') IS NULL THROW 54591,'Phase 13 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.Camera_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.PersonTrack_Search',N'P') IS NULL THROW 54592,'Phase 13 procedures are incomplete.',1;
GO
