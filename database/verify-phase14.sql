USE [CustSearch_AI];
GO
SET NOCOUNT ON;
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0')<>1 THROW 54800,'V1.13.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.CustomerRecognitionConsents',N'U') IS NULL OR OBJECT_ID(N'dbo.BiometricTemplates',N'U') IS NULL OR OBJECT_ID(N'dbo.RecognitionCandidates',N'U') IS NULL THROW 54801,'Phase 14 tables are missing.',1;
IF COL_LENGTH(N'dbo.BiometricTemplates',N'EncryptedTemplate') IS NULL OR COL_LENGTH(N'dbo.BiometricTemplates',N'Nonce') IS NULL OR COL_LENGTH(N'dbo.BiometricTemplates',N'AuthenticationTag') IS NULL THROW 54802,'Encrypted template columns are missing.',1;
IF COL_LENGTH(N'dbo.BiometricTemplates',N'RawImage') IS NOT NULL OR COL_LENGTH(N'dbo.BiometricTemplates',N'FaceImage') IS NOT NULL OR COL_LENGTH(N'dbo.BiometricTemplates',N'Aadhaar') IS NOT NULL OR COL_LENGTH(N'dbo.BiometricTemplates',N'PAN') IS NOT NULL THROW 54803,'Forbidden identity/raw-image column found.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RecognitionCandidates') AND name=N'UX_RecognitionCandidates_Tenant_Store_Request' AND is_unique=1) THROW 54804,'Candidate idempotency index is missing.',1;
IF(SELECT COUNT(*) FROM dbo.Permissions WHERE Name IN(N'Recognition.View',N'Recognition.Enroll',N'Recognition.Review',N'Recognition.Settings.Manage',N'Recognition.Consent.Manage') AND Scope=2 AND IsActive=1)<>5 THROW 54805,'Recognition permissions are incomplete.',1;
IF OBJECT_ID(N'dbo.RecognitionConsent_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.RecognitionCandidate_Search',N'P') IS NULL THROW 54806,'Phase 14 procedures are missing.',1;
SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0';
SELECT N'Phase 14 consent-based recognition verification passed.' Result;
GO
