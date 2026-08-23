USE [master]
GO
/* CustSearch AI portable bootstrap: SQL Server 2022+, no machine-specific MDF/LDF paths. */
IF DB_ID(N'CustSearch_AI') IS NULL
BEGIN
    CREATE DATABASE [CustSearch_AI];
END
GO
ALTER DATABASE [CustSearch_AI] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [CustSearch_AI].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [CustSearch_AI] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [CustSearch_AI] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [CustSearch_AI] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [CustSearch_AI] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [CustSearch_AI] SET ARITHABORT OFF 
GO
ALTER DATABASE [CustSearch_AI] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [CustSearch_AI] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [CustSearch_AI] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [CustSearch_AI] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [CustSearch_AI] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [CustSearch_AI] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [CustSearch_AI] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [CustSearch_AI] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [CustSearch_AI] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [CustSearch_AI] SET  ENABLE_BROKER 
GO
ALTER DATABASE [CustSearch_AI] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [CustSearch_AI] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [CustSearch_AI] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [CustSearch_AI] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [CustSearch_AI] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [CustSearch_AI] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [CustSearch_AI] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [CustSearch_AI] SET RECOVERY FULL 
GO
ALTER DATABASE [CustSearch_AI] SET  MULTI_USER 
GO
ALTER DATABASE [CustSearch_AI] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [CustSearch_AI] SET DB_CHAINING OFF 
GO
ALTER DATABASE [CustSearch_AI] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [CustSearch_AI] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [CustSearch_AI] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [CustSearch_AI] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [CustSearch_AI] SET QUERY_STORE = ON
GO
ALTER DATABASE [CustSearch_AI] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [CustSearch_AI]
GO
/****** Object:  Table [dbo].[AuditLogs]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AuditLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantId] [bigint] NULL,
	[StoreId] [bigint] NULL,
	[UserId] [bigint] NULL,
	[ActorType] [nvarchar](50) NOT NULL,
	[Action] [nvarchar](100) NOT NULL,
	[EntityType] [nvarchar](100) NOT NULL,
	[EntityId] [nvarchar](100) NULL,
	[BeforeJson] [nvarchar](4000) NULL,
	[AfterJson] [nvarchar](4000) NULL,
	[IpAddress] [varchar](64) NULL,
	[UserAgent] [nvarchar](500) NULL,
	[CorrelationId] [varchar](64) NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AuthenticationEvents]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AuthenticationEvents](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[UserId] [bigint] NULL,
	[TenantId] [bigint] NULL,
	[EventType] [nvarchar](60) NOT NULL,
	[IsSuccess] [bit] NOT NULL,
	[FailureCode] [nvarchar](60) NULL,
	[OccurredUtc] [datetime2](7) NOT NULL,
	[IpAddress] [varchar](64) NULL,
	[CorrelationId] [varchar](64) NOT NULL,
 CONSTRAINT [PK_AuthenticationEvents] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DatabaseVersions]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DatabaseVersions](
	[VersionId] [bigint] IDENTITY(1,1) NOT NULL,
	[VersionNumber] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](250) NOT NULL,
	[AppliedUtc] [datetime2](7) NOT NULL,
	[AppliedBy] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_DatabaseVersions] PRIMARY KEY CLUSTERED 
(
	[VersionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Permissions]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Permissions](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Scope] [tinyint] NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[Description] [nvarchar](300) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RefreshTokens]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RefreshTokens](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[UserId] [bigint] NOT NULL,
	[TokenHash] [char](64) NOT NULL,
	[FamilyId] [uniqueidentifier] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[ExpiresUtc] [datetime2](7) NOT NULL,
	[RevokedUtc] [datetime2](7) NULL,
	[RevokedReason] [nvarchar](100) NULL,
	[ReplacedByTokenHash] [char](64) NULL,
	[CreatedByIp] [varchar](64) NULL,
	[RevokedByIp] [varchar](64) NULL,
	[IssuedSecurityStamp] [nvarchar](64) NOT NULL,
 CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RolePermissions]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolePermissions](
	[RoleId] [bigint] NOT NULL,
	[PermissionId] [bigint] NOT NULL,
 CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC,
	[PermissionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantId] [bigint] NULL,
	[Scope] [tinyint] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[NormalizedName] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](300) NOT NULL,
	[IsSystem] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SubscriptionPlans]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SubscriptionPlans](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[PlanCode] [nvarchar](30) NOT NULL,
	[PlanName] [nvarchar](100) NOT NULL,
	[MonthlyPrice] [decimal](19, 4) NOT NULL,
	[AnnualPrice] [decimal](19, 4) NULL,
	[MaxStores] [int] NOT NULL,
	[MaxUsers] [int] NOT NULL,
	[MaxCameras] [int] NOT NULL,
	[MaxMonthlyRecognitions] [bigint] NULL,
	[MaxMonthlyApiCalls] [bigint] NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[UpdatedUtc] [datetime2](7) NOT NULL,
	[RowVersion] [binary](16) NOT NULL,
 CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TenantQuotaOverrides]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TenantQuotaOverrides](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantId] [bigint] NOT NULL,
	[MaxStores] [int] NULL,
	[MaxUsers] [int] NULL,
	[MaxCameras] [int] NULL,
	[MaxMonthlyRecognitions] [bigint] NULL,
	[MaxMonthlyApiCalls] [bigint] NULL,
	[Reason] [nvarchar](500) NOT NULL,
	[CreatedByUserId] [bigint] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[ExpiresUtc] [datetime2](7) NULL,
 CONSTRAINT [PK_TenantQuotaOverrides] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tenants]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tenants](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantCode] [nvarchar](30) NOT NULL,
	[LegalName] [nvarchar](200) NOT NULL,
	[DisplayName] [nvarchar](150) NOT NULL,
	[TimeZone] [nvarchar](100) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IsSuspended] [bit] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[PrimaryContactName] [nvarchar](150) NOT NULL,
	[PrimaryEmail] [nvarchar](254) NOT NULL,
	[PrimaryMobile] [nvarchar](30) NOT NULL,
	[CountryCode] [char](2) NOT NULL,
	[CurrencyCode] [char](3) NOT NULL,
	[SubscriptionPlanId] [bigint] NULL,
	[SubscriptionStatus] [tinyint] NOT NULL,
	[TrialStartsUtc] [datetime2](7) NULL,
	[TrialEndsUtc] [datetime2](7) NULL,
	[SubscriptionStartsUtc] [datetime2](7) NULL,
	[SubscriptionEndsUtc] [datetime2](7) NULL,
	[MaxStores] [int] NOT NULL,
	[MaxUsers] [int] NOT NULL,
	[MaxCameras] [int] NOT NULL,
	[SuspensionReason] [nvarchar](500) NULL,
	[UpdatedUtc] [datetime2](7) NOT NULL,
	[RowVersion] [binary](16) NOT NULL,
 CONSTRAINT [PK_Tenants] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TenantSubscriptions]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TenantSubscriptions](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantId] [bigint] NOT NULL,
	[SubscriptionPlanId] [bigint] NOT NULL,
	[BillingCycle] [tinyint] NOT NULL,
	[Status] [tinyint] NOT NULL,
	[StartsUtc] [datetime2](7) NOT NULL,
	[EndsUtc] [datetime2](7) NULL,
	[AutoRenew] [bit] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[UpdatedUtc] [datetime2](7) NOT NULL,
	[RowVersion] [binary](16) NOT NULL,
 CONSTRAINT [PK_TenantSubscriptions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TenantUsageSnapshots]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TenantUsageSnapshots](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantId] [bigint] NOT NULL,
	[PeriodStartUtc] [datetime2](7) NOT NULL,
	[PeriodEndUtc] [datetime2](7) NOT NULL,
	[StoreCount] [int] NOT NULL,
	[UserCount] [int] NOT NULL,
	[CameraCount] [int] NOT NULL,
	[RecognitionCount] [bigint] NOT NULL,
	[ApiCallCount] [bigint] NOT NULL,
	[CapturedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_TenantUsageSnapshots] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoles]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[UserId] [bigint] NOT NULL,
	[RoleId] [bigint] NOT NULL,
	[AssignedUtc] [datetime2](7) NOT NULL,
	[AssignedByUserId] [bigint] NULL,
 CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TenantId] [bigint] NULL,
	[Scope] [tinyint] NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[NormalizedUserName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](254) NOT NULL,
	[NormalizedEmail] [nvarchar](254) NOT NULL,
	[DisplayName] [nvarchar](150) NOT NULL,
	[PasswordHash] [nvarchar](500) NOT NULL,
	[SecurityStamp] [nvarchar](64) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedUtc] [datetime2](7) NOT NULL,
	[LastLoginUtc] [datetime2](7) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[DatabaseVersions] ON 

INSERT [dbo].[DatabaseVersions] ([VersionId], [VersionNumber], [Description], [AppliedUtc], [AppliedBy]) VALUES (1, N'V1.0.0', N'CustSearch AI foundation database and version ledger', CAST(N'2026-08-15T17:19:57.7215845' AS DateTime2), N'KRUTARTH-BHAVSA\Krutarth Bhavsar')
INSERT [dbo].[DatabaseVersions] ([VersionId], [VersionNumber], [Description], [AppliedUtc], [AppliedBy]) VALUES (2, N'V1.1.0', N'Multi-tenant identity and rotating refresh-token authentication schema', CAST(N'2026-08-15T19:42:06.8777553' AS DateTime2), N'KRUTARTH-BHAVSA\Krutarth Bhavsar')
INSERT [dbo].[DatabaseVersions] ([VersionId], [VersionNumber], [Description], [AppliedUtc], [AppliedBy]) VALUES (3, N'V1.2.0', N'Phase 3 authorization roles and permissions', CAST(N'2026-08-15T20:15:43.6379486' AS DateTime2), N'KRUTARTH-BHAVSA\Krutarth Bhavsar')
INSERT [dbo].[DatabaseVersions] ([VersionId], [VersionNumber], [Description], [AppliedUtc], [AppliedBy]) VALUES (4, N'V1.3.0', N'Phase 4 platform tenant management', CAST(N'2026-08-15T21:02:51.2677579' AS DateTime2), N'KRUTARTH-BHAVSA\Krutarth Bhavsar')
SET IDENTITY_INSERT [dbo].[DatabaseVersions] OFF
GO
SET IDENTITY_INSERT [dbo].[Permissions] ON 

INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (1, 1, N'Tenants.View', N'Allows Tenants.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (2, 1, N'Tenants.Create', N'Allows Tenants.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (3, 1, N'Tenants.Edit', N'Allows Tenants.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (4, 1, N'Tenants.Activate', N'Allows Tenants.Activate operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (5, 1, N'Tenants.Suspend', N'Allows Tenants.Suspend operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (6, 1, N'Tenants.ViewUsage', N'Allows Tenants.ViewUsage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (7, 1, N'Tenants.ViewOperationalSummary', N'Allows Tenants.ViewOperationalSummary operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (8, 1, N'PlatformBilling.View', N'Allows PlatformBilling.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (9, 1, N'PlatformBilling.Manage', N'Allows PlatformBilling.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (10, 1, N'SubscriptionPlans.View', N'Allows SubscriptionPlans.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (11, 1, N'SubscriptionPlans.Manage', N'Allows SubscriptionPlans.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (12, 1, N'PlatformReports.View', N'Allows PlatformReports.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (13, 1, N'PlatformReports.Export', N'Allows PlatformReports.Export operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (14, 1, N'PlatformAudit.View', N'Allows PlatformAudit.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (15, 1, N'PlatformSupport.AccessTenant', N'Allows PlatformSupport.AccessTenant operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (16, 2, N'TenantDashboard.View', N'Allows TenantDashboard.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (17, 2, N'TenantUsers.View', N'Allows TenantUsers.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (18, 2, N'TenantUsers.Create', N'Allows TenantUsers.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (19, 2, N'TenantUsers.Edit', N'Allows TenantUsers.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (20, 2, N'TenantUsers.Deactivate', N'Allows TenantUsers.Deactivate operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (21, 2, N'TenantUsers.AssignRoles', N'Allows TenantUsers.AssignRoles operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (22, 2, N'TenantStores.View', N'Allows TenantStores.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (23, 2, N'TenantStores.Create', N'Allows TenantStores.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (24, 2, N'TenantStores.Edit', N'Allows TenantStores.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (25, 2, N'TenantBilling.View', N'Allows TenantBilling.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (26, 2, N'TenantReports.View', N'Allows TenantReports.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (27, 2, N'TenantReports.Export', N'Allows TenantReports.Export operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (28, 2, N'TenantAudit.View', N'Allows TenantAudit.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (29, 2, N'Customers.View', N'Allows Customers.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (30, 2, N'Customers.Create', N'Allows Customers.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (31, 2, N'Customers.Edit', N'Allows Customers.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (32, 2, N'Visitors.View', N'Allows Visitors.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (33, 2, N'Visitors.Convert', N'Allows Visitors.Convert operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (34, 2, N'Households.View', N'Allows Households.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (35, 2, N'Households.Create', N'Allows Households.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (36, 2, N'Households.Edit', N'Allows Households.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (37, 2, N'Households.ManageMembers', N'Allows Households.ManageMembers operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (38, 2, N'Visits.View', N'Allows Visits.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (39, 2, N'Visits.Edit', N'Allows Visits.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (40, 2, N'Invoices.View', N'Allows Invoices.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (41, 2, N'Invoices.Create', N'Allows Invoices.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (42, 2, N'Invoices.Edit', N'Allows Invoices.Edit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (43, 2, N'Payments.View', N'Allows Payments.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (44, 2, N'Payments.Create', N'Allows Payments.Create operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (45, 2, N'Cameras.View', N'Allows Cameras.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (46, 2, N'Cameras.Manage', N'Allows Cameras.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (47, 2, N'Cameras.Control', N'Allows Cameras.Control operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (48, 2, N'Recognition.View', N'Allows Recognition.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (49, 2, N'Recognition.Review', N'Allows Recognition.Review operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (50, 2, N'Preferences.View', N'Allows Preferences.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (51, 2, N'Preferences.Manage', N'Allows Preferences.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (52, 2, N'Alerts.View', N'Allows Alerts.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (53, 2, N'Alerts.Acknowledge', N'Allows Alerts.Acknowledge operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (54, 2, N'Alerts.Configure', N'Allows Alerts.Configure operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (55, 2, N'Consents.View', N'Allows Consents.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (56, 2, N'Consents.Manage', N'Allows Consents.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (57, 2, N'Integrations.View', N'Allows Integrations.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (58, 2, N'Integrations.Manage', N'Allows Integrations.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (59, 2, N'Webhooks.View', N'Allows Webhooks.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (60, 2, N'Webhooks.Manage', N'Allows Webhooks.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (61, 2, N'Reports.View', N'Allows Reports.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (62, 2, N'Reports.Export', N'Allows Reports.Export operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (63, 2, N'Users.View', N'Allows Users.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (64, 2, N'Users.Manage', N'Allows Users.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (65, 2, N'Staff.View', N'Allows Staff.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (66, 2, N'Staff.Manage', N'Allows Staff.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (67, 2, N'StaffTracking.View', N'Allows StaffTracking.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (68, 2, N'StaffPerformance.View', N'Allows StaffPerformance.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (69, 2, N'StaffPerformance.Export', N'Allows StaffPerformance.Export operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (70, 2, N'StaffCustomerInteractions.View', N'Allows StaffCustomerInteractions.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (71, 2, N'StoreCategories.View', N'Allows StoreCategories.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (72, 2, N'StoreCategories.Manage', N'Allows StoreCategories.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (73, 2, N'VoiceCommands.Use', N'Allows VoiceCommands.Use operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (74, 2, N'VoiceCommands.View', N'Allows VoiceCommands.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (75, 2, N'VoiceCommands.Configure', N'Allows VoiceCommands.Configure operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (76, 2, N'VoiceCommands.Audit', N'Allows VoiceCommands.Audit operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (77, 2, N'CustomerJourneys.View', N'Allows CustomerJourneys.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (78, 2, N'DwellAnalytics.View', N'Allows DwellAnalytics.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (79, 2, N'Roles.Manage', N'Allows Roles.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (80, 2, N'Settings.View', N'Allows Settings.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (81, 2, N'Settings.Manage', N'Allows Settings.Manage operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
INSERT [dbo].[Permissions] ([Id], [Scope], [Name], [Description], [IsActive], [CreatedUtc]) VALUES (82, 2, N'AuditLogs.View', N'Allows AuditLogs.View operations.', 1, CAST(N'2026-08-15T20:15:41.3731417' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Permissions] OFF
GO
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 1)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 1)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (3, 1)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (4, 1)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (5, 1)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 2)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 2)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 3)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 3)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 4)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 4)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 5)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 5)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 6)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 6)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (5, 6)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 7)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 7)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (4, 7)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (5, 7)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 8)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (3, 8)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 9)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (3, 9)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 10)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (3, 10)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 11)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (3, 11)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 12)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 12)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (5, 12)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 13)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (2, 13)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (5, 13)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 14)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (5, 14)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (1, 15)
INSERT [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (4, 15)
GO
SET IDENTITY_INSERT [dbo].[Roles] ON 

INSERT [dbo].[Roles] ([Id], [TenantId], [Scope], [Name], [NormalizedName], [Description], [IsSystem], [IsActive], [CreatedUtc]) VALUES (1, NULL, 1, N'PlatformSuperAdmin', N'PLATFORMSUPERADMIN', N'Full platform administration access.', 1, 1, CAST(N'2026-08-15T20:15:41.3850556' AS DateTime2))
INSERT [dbo].[Roles] ([Id], [TenantId], [Scope], [Name], [NormalizedName], [Description], [IsSystem], [IsActive], [CreatedUtc]) VALUES (2, NULL, 1, N'PlatformOperationsAdmin', N'PLATFORMOPERATIONSADMIN', N'Tenant lifecycle, health and usage operations.', 1, 1, CAST(N'2026-08-15T20:15:41.3850556' AS DateTime2))
INSERT [dbo].[Roles] ([Id], [TenantId], [Scope], [Name], [NormalizedName], [Description], [IsSystem], [IsActive], [CreatedUtc]) VALUES (3, NULL, 1, N'PlatformBillingAdmin', N'PLATFORMBILLINGADMIN', N'Platform billing and subscription administration.', 1, 1, CAST(N'2026-08-15T20:15:41.3850556' AS DateTime2))
INSERT [dbo].[Roles] ([Id], [TenantId], [Scope], [Name], [NormalizedName], [Description], [IsSystem], [IsActive], [CreatedUtc]) VALUES (4, NULL, 1, N'PlatformSupportAdmin', N'PLATFORMSUPPORTADMIN', N'Limited and audited tenant support access.', 1, 1, CAST(N'2026-08-15T20:15:41.3850556' AS DateTime2))
INSERT [dbo].[Roles] ([Id], [TenantId], [Scope], [Name], [NormalizedName], [Description], [IsSystem], [IsActive], [CreatedUtc]) VALUES (5, NULL, 1, N'PlatformAuditor', N'PLATFORMAUDITOR', N'Read-only platform reporting and audit access.', 1, 1, CAST(N'2026-08-15T20:15:41.3850556' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Roles] OFF
GO
SET IDENTITY_INSERT [dbo].[SubscriptionPlans] ON 

INSERT [dbo].[SubscriptionPlans] ([Id], [PlanCode], [PlanName], [MonthlyPrice], [AnnualPrice], [MaxStores], [MaxUsers], [MaxCameras], [MaxMonthlyRecognitions], [MaxMonthlyApiCalls], [IsActive], [CreatedUtc], [UpdatedUtc], [RowVersion]) VALUES (1, N'TRIAL', N'Trial', CAST(0.0000 AS Decimal(19, 4)), NULL, 1, 5, 5, 10000, 10000, 1, CAST(N'2026-08-15T21:02:49.9851495' AS DateTime2), CAST(N'2026-08-15T21:02:49.9851495' AS DateTime2), 0xEFEEECEFB9482D4896DA4DF4D650E07C)
SET IDENTITY_INSERT [dbo].[SubscriptionPlans] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AuditLogs_Action_CreatedUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Action_CreatedUtc] ON [dbo].[AuditLogs]
(
	[Action] ASC,
	[CreatedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AuditLogs_CorrelationId]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_AuditLogs_CorrelationId] ON [dbo].[AuditLogs]
(
	[CorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AuditLogs_TenantId_CreatedUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_AuditLogs_TenantId_CreatedUtc] ON [dbo].[AuditLogs]
(
	[TenantId] ASC,
	[CreatedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AuthenticationEvents_TenantId_OccurredUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_AuthenticationEvents_TenantId_OccurredUtc] ON [dbo].[AuthenticationEvents]
(
	[TenantId] ASC,
	[OccurredUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AuthenticationEvents_UserId_OccurredUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_AuthenticationEvents_UserId_OccurredUtc] ON [dbo].[AuthenticationEvents]
(
	[UserId] ASC,
	[OccurredUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_DatabaseVersions_VersionNumber]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_DatabaseVersions_VersionNumber] ON [dbo].[DatabaseVersions]
(
	[VersionNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Permissions_Scope_IsActive]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_Permissions_Scope_IsActive] ON [dbo].[Permissions]
(
	[Scope] ASC,
	[IsActive] ASC
)
INCLUDE([Name]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Permissions_Name]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Permissions_Name] ON [dbo].[Permissions]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RefreshTokens_ExpiresUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_ExpiresUtc] ON [dbo].[RefreshTokens]
(
	[ExpiresUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RefreshTokens_UserId_FamilyId]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId_FamilyId] ON [dbo].[RefreshTokens]
(
	[UserId] ASC,
	[FamilyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_RefreshTokens_TokenHash]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens]
(
	[TokenHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RolePermissions_PermissionId]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions]
(
	[PermissionId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Roles_Scope_IsActive]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_Roles_Scope_IsActive] ON [dbo].[Roles]
(
	[Scope] ASC,
	[IsActive] ASC
)
INCLUDE([TenantId],[NormalizedName]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Roles_TenantId_NormalizedName]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Roles_TenantId_NormalizedName] ON [dbo].[Roles]
(
	[TenantId] ASC,
	[NormalizedName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SubscriptionPlans_IsActive_PlanName]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_SubscriptionPlans_IsActive_PlanName] ON [dbo].[SubscriptionPlans]
(
	[IsActive] ASC,
	[PlanName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_SubscriptionPlans_PlanCode]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_SubscriptionPlans_PlanCode] ON [dbo].[SubscriptionPlans]
(
	[PlanCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TenantQuotaOverrides_ExpiresUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_TenantQuotaOverrides_ExpiresUtc] ON [dbo].[TenantQuotaOverrides]
(
	[ExpiresUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TenantQuotaOverrides_TenantId_CreatedUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_TenantQuotaOverrides_TenantId_CreatedUtc] ON [dbo].[TenantQuotaOverrides]
(
	[TenantId] ASC,
	[CreatedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Tenants_LifecycleSubscription]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_Tenants_LifecycleSubscription] ON [dbo].[Tenants]
(
	[IsActive] ASC,
	[IsSuspended] ASC,
	[SubscriptionStatus] ASC
)
INCLUDE([TenantCode],[DisplayName],[SubscriptionPlanId],[UpdatedUtc]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Tenants_TenantCode]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Tenants_TenantCode] ON [dbo].[Tenants]
(
	[TenantCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TenantSubscriptions_TenantId_Status_StartsUtc]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_TenantSubscriptions_TenantId_Status_StartsUtc] ON [dbo].[TenantSubscriptions]
(
	[TenantId] ASC,
	[Status] ASC,
	[StartsUtc] DESC
)
INCLUDE([SubscriptionPlanId],[EndsUtc],[AutoRenew]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_TenantUsageSnapshots_Tenant_Period]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_TenantUsageSnapshots_Tenant_Period] ON [dbo].[TenantUsageSnapshots]
(
	[TenantId] ASC,
	[PeriodStartUtc] ASC,
	[PeriodEndUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserRoles_AssignedByUserId]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_UserRoles_AssignedByUserId] ON [dbo].[UserRoles]
(
	[AssignedByUserId] ASC
)
INCLUDE([UserId],[RoleId],[AssignedUtc]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserRoles_RoleId]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]
(
	[RoleId] ASC
)
INCLUDE([UserId],[AssignedUtc]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Users_Scope]    Script Date: 22-08-2026 20:33:50 ******/
CREATE NONCLUSTERED INDEX [IX_Users_Scope] ON [dbo].[Users]
(
	[Scope] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Users_TenantId_NormalizedEmail]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Users_TenantId_NormalizedEmail] ON [dbo].[Users]
(
	[TenantId] ASC,
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Users_TenantId_NormalizedUserName]    Script Date: 22-08-2026 20:33:50 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Users_TenantId_NormalizedUserName] ON [dbo].[Users]
(
	[TenantId] ASC,
	[NormalizedUserName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AuditLogs] ADD  CONSTRAINT [DF_AuditLogs_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[AuthenticationEvents] ADD  CONSTRAINT [DF_AuthenticationEvents_OccurredUtc]  DEFAULT (sysutcdatetime()) FOR [OccurredUtc]
GO
ALTER TABLE [dbo].[DatabaseVersions] ADD  CONSTRAINT [DF_DatabaseVersions_AppliedUtc]  DEFAULT (sysutcdatetime()) FOR [AppliedUtc]
GO
ALTER TABLE [dbo].[DatabaseVersions] ADD  CONSTRAINT [DF_DatabaseVersions_AppliedBy]  DEFAULT (original_login()) FOR [AppliedBy]
GO
ALTER TABLE [dbo].[Permissions] ADD  CONSTRAINT [DF_Permissions_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Permissions] ADD  CONSTRAINT [DF_Permissions_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [DF_Roles_IsSystem]  DEFAULT ((0)) FOR [IsSystem]
GO
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [DF_Roles_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [DF_Roles_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  CONSTRAINT [DF_SubscriptionPlans_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  CONSTRAINT [DF_SubscriptionPlans_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  CONSTRAINT [DF_SubscriptionPlans_UpdatedUtc]  DEFAULT (sysutcdatetime()) FOR [UpdatedUtc]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  CONSTRAINT [DF_SubscriptionPlans_RowVersion]  DEFAULT (CONVERT([binary](16),newid())) FOR [RowVersion]
GO
ALTER TABLE [dbo].[TenantQuotaOverrides] ADD  CONSTRAINT [DF_TenantQuotaOverrides_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_IsSuspended]  DEFAULT ((0)) FOR [IsSuspended]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_PrimaryContactName]  DEFAULT (N'Unknown') FOR [PrimaryContactName]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_PrimaryEmail]  DEFAULT (N'unknown@invalid.local') FOR [PrimaryEmail]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_PrimaryMobile]  DEFAULT (N'') FOR [PrimaryMobile]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_CountryCode]  DEFAULT ('XX') FOR [CountryCode]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_CurrencyCode]  DEFAULT ('USD') FOR [CurrencyCode]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_SubscriptionStatus]  DEFAULT ((1)) FOR [SubscriptionStatus]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_MaxStores]  DEFAULT ((1)) FOR [MaxStores]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_MaxUsers]  DEFAULT ((5)) FOR [MaxUsers]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_MaxCameras]  DEFAULT ((5)) FOR [MaxCameras]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_UpdatedUtc]  DEFAULT (sysutcdatetime()) FOR [UpdatedUtc]
GO
ALTER TABLE [dbo].[Tenants] ADD  CONSTRAINT [DF_Tenants_RowVersion]  DEFAULT (CONVERT([binary](16),newid())) FOR [RowVersion]
GO
ALTER TABLE [dbo].[TenantSubscriptions] ADD  CONSTRAINT [DF_TenantSubscriptions_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[TenantSubscriptions] ADD  CONSTRAINT [DF_TenantSubscriptions_UpdatedUtc]  DEFAULT (sysutcdatetime()) FOR [UpdatedUtc]
GO
ALTER TABLE [dbo].[TenantSubscriptions] ADD  CONSTRAINT [DF_TenantSubscriptions_RowVersion]  DEFAULT (CONVERT([binary](16),newid())) FOR [RowVersion]
GO
ALTER TABLE [dbo].[TenantUsageSnapshots] ADD  CONSTRAINT [DF_TenantUsageSnapshots_CapturedUtc]  DEFAULT (sysutcdatetime()) FOR [CapturedUtc]
GO
ALTER TABLE [dbo].[UserRoles] ADD  CONSTRAINT [DF_UserRoles_AssignedUtc]  DEFAULT (sysutcdatetime()) FOR [AssignedUtc]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_CreatedUtc]  DEFAULT (sysutcdatetime()) FOR [CreatedUtc]
GO
ALTER TABLE [dbo].[AuditLogs]  WITH CHECK ADD  CONSTRAINT [FK_AuditLogs_Tenants_TenantId] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenants] ([Id])
GO
ALTER TABLE [dbo].[AuditLogs] CHECK CONSTRAINT [FK_AuditLogs_Tenants_TenantId]
GO
ALTER TABLE [dbo].[AuditLogs]  WITH CHECK ADD  CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[AuditLogs] CHECK CONSTRAINT [FK_AuditLogs_Users_UserId]
GO
ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [FK_RefreshTokens_Users_UserId]
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY([PermissionId])
REFERENCES [dbo].[Permissions] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_Permissions_PermissionId]
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_Roles_RoleId]
GO
ALTER TABLE [dbo].[Roles]  WITH CHECK ADD  CONSTRAINT [FK_Roles_Tenants_TenantId] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenants] ([Id])
GO
ALTER TABLE [dbo].[Roles] CHECK CONSTRAINT [FK_Roles_Tenants_TenantId]
GO
ALTER TABLE [dbo].[TenantQuotaOverrides]  WITH CHECK ADD  CONSTRAINT [FK_TenantQuotaOverrides_Tenants_TenantId] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenants] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TenantQuotaOverrides] CHECK CONSTRAINT [FK_TenantQuotaOverrides_Tenants_TenantId]
GO
ALTER TABLE [dbo].[TenantQuotaOverrides]  WITH CHECK ADD  CONSTRAINT [FK_TenantQuotaOverrides_Users_CreatedByUserId] FOREIGN KEY([CreatedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[TenantQuotaOverrides] CHECK CONSTRAINT [FK_TenantQuotaOverrides_Users_CreatedByUserId]
GO
ALTER TABLE [dbo].[Tenants]  WITH CHECK ADD  CONSTRAINT [FK_Tenants_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY([SubscriptionPlanId])
REFERENCES [dbo].[SubscriptionPlans] ([Id])
GO
ALTER TABLE [dbo].[Tenants] CHECK CONSTRAINT [FK_Tenants_SubscriptionPlans_SubscriptionPlanId]
GO
ALTER TABLE [dbo].[TenantSubscriptions]  WITH CHECK ADD  CONSTRAINT [FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY([SubscriptionPlanId])
REFERENCES [dbo].[SubscriptionPlans] ([Id])
GO
ALTER TABLE [dbo].[TenantSubscriptions] CHECK CONSTRAINT [FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId]
GO
ALTER TABLE [dbo].[TenantSubscriptions]  WITH CHECK ADD  CONSTRAINT [FK_TenantSubscriptions_Tenants_TenantId] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenants] ([Id])
GO
ALTER TABLE [dbo].[TenantSubscriptions] CHECK CONSTRAINT [FK_TenantSubscriptions_Tenants_TenantId]
GO
ALTER TABLE [dbo].[TenantUsageSnapshots]  WITH CHECK ADD  CONSTRAINT [FK_TenantUsageSnapshots_Tenants_TenantId] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenants] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TenantUsageSnapshots] CHECK CONSTRAINT [FK_TenantUsageSnapshots_Tenants_TenantId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles_RoleId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users_AssignedByUserId] FOREIGN KEY([AssignedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users_AssignedByUserId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users_UserId]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenants] ([Id])
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Tenants_TenantId]
GO
ALTER TABLE [dbo].[Permissions]  WITH CHECK ADD  CONSTRAINT [CK_Permissions_Scope] CHECK  (([Scope]=(2) OR [Scope]=(1)))
GO
ALTER TABLE [dbo].[Permissions] CHECK CONSTRAINT [CK_Permissions_Scope]
GO
ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [CK_RefreshTokens_Expiry] CHECK  (([ExpiresUtc]>[CreatedUtc]))
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [CK_RefreshTokens_Expiry]
GO
ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [CK_RefreshTokens_Revocation] CHECK  (([RevokedUtc] IS NULL AND [RevokedReason] IS NULL OR [RevokedUtc] IS NOT NULL AND [RevokedReason] IS NOT NULL))
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [CK_RefreshTokens_Revocation]
GO
ALTER TABLE [dbo].[Roles]  WITH CHECK ADD  CONSTRAINT [CK_Roles_ScopeTenant] CHECK  (([Scope]=(1) AND [TenantId] IS NULL OR [Scope]=(2) AND [TenantId] IS NOT NULL))
GO
ALTER TABLE [dbo].[Roles] CHECK CONSTRAINT [CK_Roles_ScopeTenant]
GO
ALTER TABLE [dbo].[SubscriptionPlans]  WITH CHECK ADD  CONSTRAINT [CK_SubscriptionPlans_Limits] CHECK  (([MaxStores]>(0) AND [MaxUsers]>(0) AND [MaxCameras]>(0) AND ([MaxMonthlyRecognitions] IS NULL OR [MaxMonthlyRecognitions]>(0)) AND ([MaxMonthlyApiCalls] IS NULL OR [MaxMonthlyApiCalls]>(0))))
GO
ALTER TABLE [dbo].[SubscriptionPlans] CHECK CONSTRAINT [CK_SubscriptionPlans_Limits]
GO
ALTER TABLE [dbo].[SubscriptionPlans]  WITH CHECK ADD  CONSTRAINT [CK_SubscriptionPlans_Prices] CHECK  (([MonthlyPrice]>=(0) AND ([AnnualPrice] IS NULL OR [AnnualPrice]>=(0))))
GO
ALTER TABLE [dbo].[SubscriptionPlans] CHECK CONSTRAINT [CK_SubscriptionPlans_Prices]
GO
ALTER TABLE [dbo].[TenantQuotaOverrides]  WITH CHECK ADD  CONSTRAINT [CK_TenantQuotaOverrides_AnyLimit] CHECK  (([MaxStores] IS NOT NULL OR [MaxUsers] IS NOT NULL OR [MaxCameras] IS NOT NULL OR [MaxMonthlyRecognitions] IS NOT NULL OR [MaxMonthlyApiCalls] IS NOT NULL))
GO
ALTER TABLE [dbo].[TenantQuotaOverrides] CHECK CONSTRAINT [CK_TenantQuotaOverrides_AnyLimit]
GO
ALTER TABLE [dbo].[TenantQuotaOverrides]  WITH CHECK ADD  CONSTRAINT [CK_TenantQuotaOverrides_Expiry] CHECK  (([ExpiresUtc] IS NULL OR [ExpiresUtc]>[CreatedUtc]))
GO
ALTER TABLE [dbo].[TenantQuotaOverrides] CHECK CONSTRAINT [CK_TenantQuotaOverrides_Expiry]
GO
ALTER TABLE [dbo].[TenantQuotaOverrides]  WITH CHECK ADD  CONSTRAINT [CK_TenantQuotaOverrides_Limits] CHECK  ((([MaxStores] IS NULL OR [MaxStores]>(0)) AND ([MaxUsers] IS NULL OR [MaxUsers]>(0)) AND ([MaxCameras] IS NULL OR [MaxCameras]>(0)) AND ([MaxMonthlyRecognitions] IS NULL OR [MaxMonthlyRecognitions]>(0)) AND ([MaxMonthlyApiCalls] IS NULL OR [MaxMonthlyApiCalls]>(0))))
GO
ALTER TABLE [dbo].[TenantQuotaOverrides] CHECK CONSTRAINT [CK_TenantQuotaOverrides_Limits]
GO
ALTER TABLE [dbo].[Tenants]  WITH CHECK ADD  CONSTRAINT [CK_Tenants_ActiveSuspended] CHECK  ((NOT ([IsActive]=(0) AND [IsSuspended]=(1))))
GO
ALTER TABLE [dbo].[Tenants] CHECK CONSTRAINT [CK_Tenants_ActiveSuspended]
GO
ALTER TABLE [dbo].[Tenants]  WITH CHECK ADD  CONSTRAINT [CK_Tenants_Quotas] CHECK  (([MaxStores]>(0) AND [MaxUsers]>(0) AND [MaxCameras]>(0)))
GO
ALTER TABLE [dbo].[Tenants] CHECK CONSTRAINT [CK_Tenants_Quotas]
GO
ALTER TABLE [dbo].[Tenants]  WITH CHECK ADD  CONSTRAINT [CK_Tenants_SubscriptionPeriod] CHECK  (([SubscriptionEndsUtc] IS NULL OR [SubscriptionStartsUtc] IS NULL OR [SubscriptionEndsUtc]>[SubscriptionStartsUtc]))
GO
ALTER TABLE [dbo].[Tenants] CHECK CONSTRAINT [CK_Tenants_SubscriptionPeriod]
GO
ALTER TABLE [dbo].[Tenants]  WITH CHECK ADD  CONSTRAINT [CK_Tenants_SubscriptionStatus] CHECK  (([SubscriptionStatus]>=(1) AND [SubscriptionStatus]<=(6)))
GO
ALTER TABLE [dbo].[Tenants] CHECK CONSTRAINT [CK_Tenants_SubscriptionStatus]
GO
ALTER TABLE [dbo].[Tenants]  WITH CHECK ADD  CONSTRAINT [CK_Tenants_TrialPeriod] CHECK  (([TrialEndsUtc] IS NULL OR [TrialStartsUtc] IS NULL OR [TrialEndsUtc]>[TrialStartsUtc]))
GO
ALTER TABLE [dbo].[Tenants] CHECK CONSTRAINT [CK_Tenants_TrialPeriod]
GO
ALTER TABLE [dbo].[TenantSubscriptions]  WITH CHECK ADD  CONSTRAINT [CK_TenantSubscriptions_BillingCycle] CHECK  (([BillingCycle]=(2) OR [BillingCycle]=(1)))
GO
ALTER TABLE [dbo].[TenantSubscriptions] CHECK CONSTRAINT [CK_TenantSubscriptions_BillingCycle]
GO
ALTER TABLE [dbo].[TenantSubscriptions]  WITH CHECK ADD  CONSTRAINT [CK_TenantSubscriptions_Period] CHECK  (([EndsUtc] IS NULL OR [EndsUtc]>[StartsUtc]))
GO
ALTER TABLE [dbo].[TenantSubscriptions] CHECK CONSTRAINT [CK_TenantSubscriptions_Period]
GO
ALTER TABLE [dbo].[TenantSubscriptions]  WITH CHECK ADD  CONSTRAINT [CK_TenantSubscriptions_Status] CHECK  (([Status]>=(1) AND [Status]<=(6)))
GO
ALTER TABLE [dbo].[TenantSubscriptions] CHECK CONSTRAINT [CK_TenantSubscriptions_Status]
GO
ALTER TABLE [dbo].[TenantUsageSnapshots]  WITH CHECK ADD  CONSTRAINT [CK_TenantUsageSnapshots_Counts] CHECK  (([StoreCount]>=(0) AND [UserCount]>=(0) AND [CameraCount]>=(0) AND [RecognitionCount]>=(0) AND [ApiCallCount]>=(0)))
GO
ALTER TABLE [dbo].[TenantUsageSnapshots] CHECK CONSTRAINT [CK_TenantUsageSnapshots_Counts]
GO
ALTER TABLE [dbo].[TenantUsageSnapshots]  WITH CHECK ADD  CONSTRAINT [CK_TenantUsageSnapshots_Period] CHECK  (([PeriodEndUtc]>[PeriodStartUtc]))
GO
ALTER TABLE [dbo].[TenantUsageSnapshots] CHECK CONSTRAINT [CK_TenantUsageSnapshots_Period]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [CK_Users_ScopeTenant] CHECK  (([Scope]=(1) AND [TenantId] IS NULL OR [Scope]=(2) AND [TenantId] IS NOT NULL))
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [CK_Users_ScopeTenant]
GO
/****** Object:  StoredProcedure [dbo].[Tenant_GetDetailSummary]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

-- Platform APIs pass an authorized tenant identifier and receive no other tenant's data.
CREATE   PROCEDURE [dbo].[Tenant_GetDetailSummary]
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.Id,
        t.TenantCode,
        t.LegalName,
        t.DisplayName,
        t.PrimaryContactName,
        t.PrimaryEmail,
        t.PrimaryMobile,
        t.CountryCode,
        t.TimeZone,
        t.CurrencyCode,
        t.SubscriptionStatus,
        subscriptionPlan.PlanCode,
        subscriptionPlan.PlanName,
        t.MaxStores,
        t.MaxUsers,
        t.MaxCameras,
        t.IsActive,
        t.IsSuspended,
        t.SuspensionReason,
        t.UpdatedUtc,
        (SELECT COUNT_BIG(*) FROM dbo.Users AS appUser WHERE appUser.TenantId = t.Id) AS UserCount,
        (SELECT COUNT_BIG(*) FROM dbo.Roles AS role WHERE role.TenantId = t.Id AND role.IsActive = 1) AS ActiveRoleCount,
        latestUsage.StoreCount,
        latestUsage.CameraCount,
        latestUsage.RecognitionCount,
        latestUsage.ApiCallCount,
        latestUsage.CapturedUtc AS UsageCapturedUtc
    FROM dbo.Tenants AS t
    LEFT JOIN dbo.SubscriptionPlans AS subscriptionPlan ON subscriptionPlan.Id = t.SubscriptionPlanId
    OUTER APPLY
    (
        SELECT TOP (1) snapshot.StoreCount, snapshot.CameraCount, snapshot.RecognitionCount,
            snapshot.ApiCallCount, snapshot.CapturedUtc
        FROM dbo.TenantUsageSnapshots AS snapshot
        WHERE snapshot.TenantId = t.Id
        ORDER BY snapshot.PeriodEndUtc DESC, snapshot.Id DESC
    ) AS latestUsage
    WHERE t.Id = @TenantId;
END;
GO
/****** Object:  StoredProcedure [dbo].[Tenant_GetUsageSummary]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

-- Each quota uses the newest non-expired override that supplied that specific limit.
CREATE   PROCEDURE [dbo].[Tenant_GetUsageSummary]
    @TenantId BIGINT,
    @AsOfUtc DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @AsOfUtc = COALESCE(@AsOfUtc, SYSUTCDATETIME());

    SELECT
        t.Id AS TenantId,
        t.TenantCode,
        latestUsage.PeriodStartUtc,
        latestUsage.PeriodEndUtc,
        latestUsage.StoreCount,
        latestUsage.UserCount,
        latestUsage.CameraCount,
        latestUsage.RecognitionCount,
        latestUsage.ApiCallCount,
        COALESCE(storeOverride.MaxStores, t.MaxStores, subscriptionPlan.MaxStores) AS MaxStores,
        COALESCE(userOverride.MaxUsers, t.MaxUsers, subscriptionPlan.MaxUsers) AS MaxUsers,
        COALESCE(cameraOverride.MaxCameras, t.MaxCameras, subscriptionPlan.MaxCameras) AS MaxCameras,
        COALESCE(recognitionOverride.MaxMonthlyRecognitions, subscriptionPlan.MaxMonthlyRecognitions) AS MaxMonthlyRecognitions,
        COALESCE(apiOverride.MaxMonthlyApiCalls, subscriptionPlan.MaxMonthlyApiCalls) AS MaxMonthlyApiCalls,
        latestUsage.CapturedUtc
    FROM dbo.Tenants AS t
    LEFT JOIN dbo.SubscriptionPlans AS subscriptionPlan ON subscriptionPlan.Id = t.SubscriptionPlanId
    OUTER APPLY (SELECT TOP (1) * FROM dbo.TenantUsageSnapshots AS item WHERE item.TenantId=t.Id ORDER BY item.PeriodEndUtc DESC, item.Id DESC) AS latestUsage
    OUTER APPLY (SELECT TOP (1) item.MaxStores FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxStores IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS storeOverride
    OUTER APPLY (SELECT TOP (1) item.MaxUsers FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxUsers IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS userOverride
    OUTER APPLY (SELECT TOP (1) item.MaxCameras FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxCameras IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS cameraOverride
    OUTER APPLY (SELECT TOP (1) item.MaxMonthlyRecognitions FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxMonthlyRecognitions IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS recognitionOverride
    OUTER APPLY (SELECT TOP (1) item.MaxMonthlyApiCalls FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxMonthlyApiCalls IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS apiOverride
    WHERE t.Id = @TenantId;
END;
GO
/****** Object:  StoredProcedure [dbo].[Tenant_ProvisionDefaultRoles]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

-- Call this procedure inside the tenant-creation transaction immediately after the tenant row is saved.
CREATE   PROCEDURE [dbo].[Tenant_ProvisionDefaultRoles]
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantId)
        THROW 51020, 'Tenant does not exist.', 1;

    DECLARE @StartedTransaction BIT = 0;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @StartedTransaction = 1;
    END;

    BEGIN TRY
        DECLARE @TenantRoles TABLE (Name NVARCHAR(100), Description NVARCHAR(300));
        INSERT INTO @TenantRoles (Name, Description)
        VALUES
            (N'TenantAdmin', N'Full administration access inside one tenant.'),
            (N'StoreAdmin', N'Assigned-store operations without tenant-wide security settings.'),
            (N'Manager', N'Day-to-day customer, visit, invoice, alert and report operations.'),
            (N'CRMStaff', N'Customer, household, preference and consent operations.'),
            (N'BillingStaff', N'Invoice, payment and purchase-related operations.'),
            (N'CameraOperator', N'Camera, recognition and live visitor operations.'),
            (N'IntegrationAdmin', N'Integration, webhook and synchronization operations.'),
            (N'Auditor', N'Read-only tenant operations and audit access.');

        INSERT INTO dbo.Roles (TenantId, Scope, Name, NormalizedName, Description, IsSystem, IsActive, CreatedUtc)
        SELECT @TenantId, 2, source.Name, UPPER(source.Name), source.Description, 1, 1, SYSUTCDATETIME()
        FROM @TenantRoles AS source
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Roles AS target
            WHERE target.TenantId = @TenantId AND target.NormalizedName = UPPER(source.Name)
        );

        -- These predicates mirror the reviewed Phase 3 least-privilege role defaults.
        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT role.Id, permission.Id
        FROM dbo.Roles AS role
        INNER JOIN dbo.Permissions AS permission ON permission.Scope = 2 AND permission.IsActive = 1
        WHERE role.TenantId = @TenantId AND role.Scope = 2 AND role.IsActive = 1
          AND
          (
              role.NormalizedName = N'TENANTADMIN'
              OR (role.NormalizedName = N'STOREADMIN' AND permission.Name NOT IN
                  (N'TenantUsers.Create', N'TenantUsers.Edit', N'TenantUsers.Deactivate', N'TenantUsers.AssignRoles',
                   N'TenantStores.Create', N'TenantStores.Edit', N'Roles.Manage', N'Settings.Manage'))
              OR (role.NormalizedName = N'MANAGER' AND
                  (permission.Name IN (N'TenantDashboard.View', N'TenantReports.View', N'TenantReports.Export')
                   OR permission.Name LIKE N'Customers.%' OR permission.Name LIKE N'Households.%'
                   OR permission.Name LIKE N'Visits.%' OR permission.Name LIKE N'Invoices.%'
                   OR permission.Name LIKE N'Alerts.%' OR permission.Name LIKE N'Reports.%'
                   OR permission.Name LIKE N'Preferences.%'))
              OR (role.NormalizedName = N'CRMSTAFF' AND
                  (permission.Name LIKE N'Customers.%' OR permission.Name LIKE N'Households.%'
                   OR permission.Name LIKE N'Visitors.%' OR permission.Name LIKE N'Preferences.%'
                   OR permission.Name LIKE N'Consents.%' OR permission.Name IN (N'Visits.View', N'CustomerJourneys.View')))
              OR (role.NormalizedName = N'BILLINGSTAFF' AND
                  (permission.Name = N'Customers.View' OR permission.Name LIKE N'Invoices.%' OR permission.Name LIKE N'Payments.%'))
              OR (role.NormalizedName = N'CAMERAOPERATOR' AND
                  (permission.Name LIKE N'Cameras.%' OR permission.Name LIKE N'Recognition.%'
                   OR permission.Name IN (N'Visitors.View', N'Visits.View', N'Alerts.View', N'Alerts.Acknowledge')))
              OR (role.NormalizedName = N'INTEGRATIONADMIN' AND
                  (permission.Name LIKE N'Integrations.%' OR permission.Name LIKE N'Webhooks.%' OR permission.Name = N'Settings.View'))
              OR (role.NormalizedName = N'AUDITOR' AND
                  (permission.Name LIKE N'%.View' OR permission.Name IN
                      (N'TenantReports.Export', N'Reports.Export', N'VoiceCommands.Audit', N'AuditLogs.View')))
          )
          AND NOT EXISTS
              (SELECT 1 FROM dbo.RolePermissions AS grantRow WHERE grantRow.RoleId = role.Id AND grantRow.PermissionId = permission.Id);

        IF @StartedTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

GO
/****** Object:  StoredProcedure [dbo].[User_GetByIdForTenant]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE   PROCEDURE [dbo].[User_GetByIdForTenant]
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TenantId IS NULL OR @TenantId <= 0
        THROW 50001, 'A valid tenant identifier is required.', 1;

    SELECT
        u.Id,
        u.TenantId,
        u.Scope,
        u.UserName,
        u.NormalizedUserName,
        u.Email,
        u.NormalizedEmail,
        u.DisplayName,
        u.IsActive,
        u.CreatedUtc,
        u.LastLoginUtc
    FROM dbo.Users AS u
    WHERE u.Id = @UserId
      AND u.TenantId = @TenantId
      AND u.Scope = 2;
END;

GO
/****** Object:  StoredProcedure [dbo].[UserAuthorization_GetForScope]    Script Date: 22-08-2026 20:33:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

-- The tenant predicate is mandatory for tenant sessions and prevents cross-tenant permission reads.
CREATE   PROCEDURE [dbo].[UserAuthorization_GetForScope]
    @UserId BIGINT,
    @TenantId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        role.Name AS RoleName,
        permission.Name AS PermissionName
    FROM dbo.Users AS appUser
    INNER JOIN dbo.UserRoles AS userRole ON userRole.UserId = appUser.Id
    INNER JOIN dbo.Roles AS role ON role.Id = userRole.RoleId AND role.IsActive = 1
    INNER JOIN dbo.RolePermissions AS rolePermission ON rolePermission.RoleId = role.Id
    INNER JOIN dbo.Permissions AS permission ON permission.Id = rolePermission.PermissionId AND permission.IsActive = 1
    WHERE appUser.Id = @UserId
      AND appUser.IsActive = 1
      AND
      (
          (appUser.Scope = 1 AND appUser.TenantId IS NULL AND @TenantId IS NULL)
          OR (appUser.Scope = 2 AND appUser.TenantId = @TenantId AND role.TenantId = @TenantId)
      )
    ORDER BY role.Name, permission.Name;
END;
-- ============================================================
-- PHASE 5 - TENANT USERS / STORES / STAFF
-- SUB-PHASE: 5A - Tenant Users / Roles / ShopOwner / TenantOwner
-- SUB-PHASE: 5B - User-Store Assignments / Store Scope / Quotas
-- SUB-PHASE: 5C - Store Master / Location / Geofence / Lifecycle
-- SUB-PHASE: 5D - Staff Profiles / Shifts / Presence
-- SUB-PHASE: 5E - Store Product Categories
-- SUB-PHASE: 5F - Dynamic Store Voice Trigger / Aliases
-- SUB-PHASE: 5G - Customer Admin Dashboard Base
-- SUB-PHASE: 5H - Completion / Validation / Canonical Database Gate
-- VERSION: V1.4.0
-- ============================================================
GO
USE [master]
GO
ALTER DATABASE [CustSearch_AI] SET  READ_WRITE 
GO

-- ============================================================
-- PHASE 5 - TENANT USERS / STORES / STAFF
-- SUB-PHASE: 5A - Tenant Users / Roles / ShopOwner / TenantOwner
-- SUB-PHASE: 5B - User-Store Assignments / Authoritative Store Scope / Quotas
-- SUB-PHASE: 5C - Store Master / Location / Geofence / Lifecycle
-- SUB-PHASE: 5D - Staff Profiles / Shifts / Presence
-- SUB-PHASE: 5E - Store Product Categories
-- SUB-PHASE: 5F - Dynamic Store Voice Trigger / Aliases
-- SUB-PHASE: 5G - Customer Admin Dashboard Base
-- SUB-PHASE: 5H - Completion / Validation / Canonical Database Gate
-- VERSION: V1.4.0
-- ============================================================
/*
 CustSearch AI — Phase 5 production database upgrade
 Version: V1.4.0
 Rules: idempotent, no EF migrations, SQL Server 2022, UTC timestamps.
 Every object below is tagged with its Phase 5 sub-phase.
*/
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
GO

/* Phase 5C — Store Master & Canonical Location: tenant-owned physical stores. */
IF OBJECT_ID(N'dbo.Stores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stores
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stores PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreCode NVARCHAR(30) NOT NULL,
        StoreName NVARCHAR(150) NOT NULL,
        AddressLine1 NVARCHAR(250) NOT NULL,
        AddressLine2 NVARCHAR(250) NULL,
        Landmark NVARCHAR(150) NULL,
        City NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NULL,
        StateOrProvince NVARCHAR(100) NOT NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        CountryCode CHAR(2) NOT NULL,
        Latitude DECIMAL(9,6) NULL,
        Longitude DECIMAL(9,6) NULL,
        GeoFenceRadiusMeters DECIMAL(10,2) NULL,
        ExternalPlaceId NVARCHAR(200) NULL,
        LocationSource TINYINT NOT NULL CONSTRAINT DF_Stores_LocationSource DEFAULT(1),
        IsLocationVerified BIT NOT NULL CONSTRAINT DF_Stores_IsLocationVerified DEFAULT(0),
        LocationVerifiedUtc DATETIME2(7) NULL,
        LocationVerifiedByUserId BIGINT NULL,
        TimeZone NVARCHAR(100) NOT NULL,
        ContactEmail NVARCHAR(254) NULL,
        ContactMobile NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Stores_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Stores_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Stores_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Stores_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Stores_Users_LocationVerifiedByUserId FOREIGN KEY(LocationVerifiedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Stores_Latitude CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
        CONSTRAINT CK_Stores_Longitude CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
        CONSTRAINT CK_Stores_CoordinatesPair CHECK ((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL)),
        CONSTRAINT CK_Stores_GeoFence CHECK (GeoFenceRadiusMeters IS NULL OR GeoFenceRadiusMeters > 0)
    );
END;
GO
/* Phase 5C — Store uniqueness/query indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_StoreCode')
    CREATE UNIQUE INDEX UX_Stores_Tenant_StoreCode ON dbo.Stores(TenantId, StoreCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'IX_Stores_Tenant_Active')
    CREATE INDEX IX_Stores_Tenant_Active ON dbo.Stores(TenantId, IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'IX_Stores_Tenant_City')
    CREATE INDEX IX_Stores_Tenant_City ON dbo.Stores(TenantId, City);
GO

/* Phase 5B — Store Assignment & Quotas: authoritative user-to-store authorization relation. */
IF OBJECT_ID(N'dbo.UserStoreAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserStoreAssignments
    (
        TenantId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        IsPrimary BIT NOT NULL CONSTRAINT DF_UserStoreAssignments_IsPrimary DEFAULT(0),
        AssignedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_UserStoreAssignments_AssignedUtc DEFAULT(SYSUTCDATETIME()),
        AssignedByUserId BIGINT NOT NULL,
        CONSTRAINT PK_UserStoreAssignments PRIMARY KEY(UserId, StoreId),
        CONSTRAINT FK_UserStoreAssignments_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_UserStoreAssignments_Users_UserId FOREIGN KEY(UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserStoreAssignments_Stores_StoreId FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserStoreAssignments_Users_AssignedBy FOREIGN KEY(AssignedByUserId) REFERENCES dbo.Users(Id)
    );
END;
GO
/* Phase 5B — Store assignment access indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.UserStoreAssignments') AND name=N'IX_UserStoreAssignments_Tenant_Store')
    CREATE INDEX IX_UserStoreAssignments_Tenant_Store ON dbo.UserStoreAssignments(TenantId, StoreId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.UserStoreAssignments') AND name=N'IX_UserStoreAssignments_User_Primary')
    CREATE INDEX IX_UserStoreAssignments_User_Primary ON dbo.UserStoreAssignments(UserId, IsPrimary);
GO

/* Phase 5D — Staff Profile: employee metadata linked one-to-one with a tenant user. */
IF OBJECT_ID(N'dbo.StaffProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffProfiles
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffProfiles PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        EmployeeCode NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Mobile NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_StaffProfiles_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffProfiles_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffProfiles_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StaffProfiles_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_StaffProfiles_Users_UserId FOREIGN KEY(UserId) REFERENCES dbo.Users(Id)
    );
END;
GO
/* Phase 5D — Staff uniqueness indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffProfiles') AND name=N'UX_StaffProfiles_UserId')
    CREATE UNIQUE INDEX UX_StaffProfiles_UserId ON dbo.StaffProfiles(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffProfiles') AND name=N'UX_StaffProfiles_Tenant_EmployeeCode')
    CREATE UNIQUE INDEX UX_StaffProfiles_Tenant_EmployeeCode ON dbo.StaffProfiles(TenantId, EmployeeCode);
GO

/* Phase 5D — Staff Shifts: operational scheduling context; not CCTV-derived payroll truth. */
IF OBJECT_ID(N'dbo.StaffShifts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffShifts
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffShifts PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StaffProfileId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        StartsUtc DATETIME2(7) NOT NULL,
        ScheduledEndsUtc DATETIME2(7) NULL,
        ActualEndsUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffShifts_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffShifts_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StaffShifts_StaffProfiles FOREIGN KEY(StaffProfileId) REFERENCES dbo.StaffProfiles(Id),
        CONSTRAINT FK_StaffShifts_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT FK_StaffShifts_Users_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_StaffShifts_Period CHECK (ScheduledEndsUtc IS NULL OR ScheduledEndsUtc > StartsUtc),
        CONSTRAINT CK_StaffShifts_Status CHECK (Status BETWEEN 1 AND 4)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffShifts') AND name=N'IX_StaffShifts_Tenant_Store_Start')
    CREATE INDEX IX_StaffShifts_Tenant_Store_Start ON dbo.StaffShifts(TenantId, StoreId, StartsUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffShifts') AND name=N'IX_StaffShifts_Staff_Status')
    CREATE INDEX IX_StaffShifts_Staff_Status ON dbo.StaffShifts(StaffProfileId, Status);
GO

/* Phase 5D — Staff Presence: optional presence signals used for operations, not authoritative attendance/payroll. */
IF OBJECT_ID(N'dbo.StaffPresenceSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffPresenceSessions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffPresenceSessions PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StaffProfileId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        Source TINYINT NOT NULL,
        EnteredUtc DATETIME2(7) NOT NULL,
        ExitedUtc DATETIME2(7) NULL,
        Confidence DECIMAL(5,4) NOT NULL,
        CONSTRAINT FK_StaffPresence_StaffProfiles FOREIGN KEY(StaffProfileId) REFERENCES dbo.StaffProfiles(Id),
        CONSTRAINT FK_StaffPresence_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT CK_StaffPresence_Confidence CHECK (Confidence BETWEEN 0 AND 1),
        CONSTRAINT CK_StaffPresence_Period CHECK (ExitedUtc IS NULL OR ExitedUtc > EnteredUtc)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffPresenceSessions') AND name=N'IX_StaffPresence_Tenant_Store_Entered')
    CREATE INDEX IX_StaffPresence_Tenant_Store_Entered ON dbo.StaffPresenceSessions(TenantId, StoreId, EnteredUtc DESC);
GO

/* Phase 5E — Store Category Taxonomy: category master available before Phase 8 product master. */
IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategories
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductCategories PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        CategoryCode NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        ParentCategoryId BIGINT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProductCategories_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductCategories_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductCategories_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_ProductCategories_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_ProductCategories_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT FK_ProductCategories_Parent FOREIGN KEY(ParentCategoryId) REFERENCES dbo.ProductCategories(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'UX_ProductCategories_Tenant_Store_Code')
    CREATE UNIQUE INDEX UX_ProductCategories_Tenant_Store_Code ON dbo.ProductCategories(TenantId, StoreId, CategoryCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'IX_ProductCategories_Tenant_Active')
    CREATE INDEX IX_ProductCategories_Tenant_Active ON dbo.ProductCategories(TenantId, IsActive);
GO

/* Phase 5F — Dynamic Store Voice Configuration: store-specific trigger; “Aasha Add” is default only. */
IF OBJECT_ID(N'dbo.StoreVoiceCommandSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoreVoiceCommandSettings
    (
        StoreId BIGINT NOT NULL CONSTRAINT PK_StoreVoiceCommandSettings PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        TriggerKeyword NVARCHAR(100) NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Trigger DEFAULT(N'Aasha Add'),
        ResponseMode TINYINT NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Response DEFAULT(4),
        IsEnabled BIT NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Enabled DEFAULT(1),
        RequireConfirmationForAmbiguousCategory BIT NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Confirm DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StoreVoiceCommandSettings_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id) ON DELETE CASCADE,
        CONSTRAINT FK_StoreVoiceCommandSettings_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT CK_StoreVoiceCommandSettings_Response CHECK (ResponseMode BETWEEN 1 AND 4)
    );
END;
GO
/* Phase 5F — Voice aliases for alternative store-specific trigger phrases. */
IF OBJECT_ID(N'dbo.StoreVoiceCommandAliases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoreVoiceCommandAliases
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoreVoiceCommandAliases PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        Alias NVARCHAR(100) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceCommandAliases_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StoreVoiceCommandAliases_Settings FOREIGN KEY(StoreId) REFERENCES dbo.StoreVoiceCommandSettings(StoreId) ON DELETE CASCADE,
        CONSTRAINT FK_StoreVoiceCommandAliases_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandAliases') AND name=N'UX_StoreVoiceCommandAliases_Tenant_Store_Alias')
    CREATE UNIQUE INDEX UX_StoreVoiceCommandAliases_Tenant_Store_Alias ON dbo.StoreVoiceCommandAliases(TenantId, StoreId, Alias);
GO

/* Phase 5C — AuditLogs StoreId FK becomes enforceable after dbo.Stores exists. */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AuditLogs_Stores_StoreId')
    ALTER TABLE dbo.AuditLogs WITH CHECK ADD CONSTRAINT FK_AuditLogs_Stores_StoreId FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id);
GO

/* Phase 5A/5D/5E/5F — ensure required permission catalog entries exist without duplicate rows. */
DECLARE @Phase5Permissions TABLE(Name NVARCHAR(150));
INSERT INTO @Phase5Permissions(Name) VALUES
(N'TenantUsers.View'),(N'TenantUsers.Create'),(N'TenantUsers.Edit'),(N'TenantUsers.Deactivate'),(N'TenantUsers.AssignRoles'),
(N'TenantStores.View'),(N'TenantStores.Create'),(N'TenantStores.Edit'),
(N'Staff.View'),(N'Staff.Manage'),(N'StaffTracking.View'),(N'StaffPerformance.View'),(N'StaffPerformance.Export'),(N'StaffCustomerInteractions.View'),
(N'StoreCategories.View'),(N'StoreCategories.Manage'),
(N'VoiceCommands.Use'),(N'VoiceCommands.View'),(N'VoiceCommands.Configure'),(N'VoiceCommands.Audit');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME()
FROM @Phase5Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

/* Phase 5A — default tenant roles including TenantOwner/ShopOwner/StoreManager/SalesStaff. */
CREATE OR ALTER PROCEDURE dbo.Tenant_ProvisionDefaultRoles
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Tenants WHERE Id=@TenantId) THROW 51020,'Tenant does not exist.',1;
    DECLARE @Roles TABLE(Name NVARCHAR(100), Description NVARCHAR(300));
    INSERT INTO @Roles VALUES
    (N'TenantAdmin',N'Full tenant administration.'),(N'TenantOwner',N'Business owner with full tenant operations.'),(N'ShopOwner',N'Shop owner with full tenant operations.'),
    (N'StoreAdmin',N'Assigned-store administration.'),(N'StoreManager',N'Assigned-store staff and customer operations.'),(N'Manager',N'Day-to-day operational management.'),
    (N'SalesStaff',N'Assigned-store staff operations.'),(N'CRMStaff',N'Customer CRM operations.'),(N'BillingStaff',N'Billing operations.'),
    (N'CameraOperator',N'Camera and recognition operations.'),(N'IntegrationAdmin',N'Integration administration.'),(N'Auditor',N'Read-only audit operations.');
    INSERT dbo.Roles(TenantId,Scope,Name,NormalizedName,Description,IsSystem,IsActive,CreatedUtc)
    SELECT @TenantId,2,r.Name,UPPER(r.Name),r.Description,1,1,SYSUTCDATETIME() FROM @Roles r
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles x WHERE x.TenantId=@TenantId AND x.NormalizedName=UPPER(r.Name));

    /* TenantOwner/ShopOwner/TenantAdmin receive all tenant permissions. */
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.TenantId=@TenantId AND r.IsActive=1 AND p.Scope=2 AND p.IsActive=1
      AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    /* StoreAdmin/StoreManager remain store-scoped and cannot manage tenant-wide roles/settings. */
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.TenantId=@TenantId AND r.IsActive=1 AND p.Scope=2 AND p.IsActive=1
      AND r.NormalizedName IN(N'STOREADMIN',N'STOREMANAGER')
      AND p.Name NOT IN(N'TenantUsers.Create',N'TenantUsers.AssignRoles',N'TenantStores.Create',N'Roles.Manage',N'Settings.Manage')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    /* SalesStaff receives least-privilege staff/customer/category/voice-use permissions. */
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.TenantId=@TenantId AND r.NormalizedName=N'SALESSTAFF' AND p.Scope=2 AND p.IsActive=1
      AND (p.Name IN(N'TenantDashboard.View',N'Staff.View',N'StoreCategories.View',N'VoiceCommands.Use',N'VoiceCommands.View',N'Customers.View',N'Customers.Create',N'Customers.Edit',N'Visits.View'))
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
END;
GO

/* Phase 5A — upgrade existing tenants with new roles/permissions. */
DECLARE @TenantId BIGINT;
DECLARE Phase5TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
OPEN Phase5TenantCursor; FETCH NEXT FROM Phase5TenantCursor INTO @TenantId;
WHILE @@FETCH_STATUS=0 BEGIN EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@TenantId; FETCH NEXT FROM Phase5TenantCursor INTO @TenantId; END
CLOSE Phase5TenantCursor; DEALLOCATE Phase5TenantCursor;
GO

/* Phase 5G — real Customer Admin dashboard summary SP. */
CREATE OR ALTER PROCEDURE dbo.TenantDashboard_GetSummary @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
      (SELECT COUNT_BIG(*) FROM dbo.Users WHERE TenantId=@TenantId AND Scope=2 AND IsActive=1) ActiveUsers,
      (SELECT COUNT_BIG(*) FROM dbo.Stores WHERE TenantId=@TenantId AND IsActive=1) ActiveStores,
      (SELECT COUNT_BIG(*) FROM dbo.StaffProfiles WHERE TenantId=@TenantId AND IsActive=1) ActiveStaff,
      (SELECT COUNT_BIG(*) FROM dbo.ProductCategories WHERE TenantId=@TenantId AND IsActive=1) ActiveCategories,
      (SELECT COUNT_BIG(*) FROM dbo.StaffShifts WHERE TenantId=@TenantId AND Status=2) OpenShifts,
      (SELECT COUNT_BIG(*) FROM dbo.StaffPresenceSessions WHERE TenantId=@TenantId AND ExitedUtc IS NULL) ActivePresenceSessions;
END;
GO

/* Phase 5C — tenant-safe store search SP for admin/report use. */
CREATE OR ALTER PROCEDURE dbo.Store_Search @TenantId BIGINT,@Search NVARCHAR(150)=NULL,@ActiveOnly BIT=0
AS
BEGIN
 SET NOCOUNT ON;
 SELECT Id,StoreCode,StoreName,City,StateOrProvince,CountryCode,Latitude,Longitude,IsLocationVerified,TimeZone,IsActive,UpdatedUtc
 FROM dbo.Stores WHERE TenantId=@TenantId AND (@ActiveOnly=0 OR IsActive=1)
   AND (@Search IS NULL OR StoreCode LIKE N'%'+@Search+N'%' OR StoreName LIKE N'%'+@Search+N'%' OR City LIKE N'%'+@Search+N'%')
 ORDER BY StoreName;
END;
GO

/* Phase 5D — tenant-safe staff search SP. */
CREATE OR ALTER PROCEDURE dbo.Staff_Search @TenantId BIGINT,@StoreId BIGINT=NULL,@Search NVARCHAR(150)=NULL,@ActiveOnly BIT=0
AS
BEGIN
 SET NOCOUNT ON;
 SELECT DISTINCT s.Id,s.UserId,s.EmployeeCode,s.FirstName,s.LastName,s.Mobile,s.IsActive
 FROM dbo.StaffProfiles s LEFT JOIN dbo.UserStoreAssignments usa ON usa.TenantId=s.TenantId AND usa.UserId=s.UserId
 WHERE s.TenantId=@TenantId AND (@StoreId IS NULL OR usa.StoreId=@StoreId) AND (@ActiveOnly=0 OR s.IsActive=1)
 AND (@Search IS NULL OR s.EmployeeCode LIKE N'%'+@Search+N'%' OR s.FirstName LIKE N'%'+@Search+N'%' OR s.LastName LIKE N'%'+@Search+N'%')
 ORDER BY s.FirstName,s.LastName;
END;
GO

/* Phase 5H — database version ledger, idempotent. */
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.4.0')
 INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
 VALUES(N'V1.4.0',N'Phase 5 tenant users, stores, staff, taxonomy and dynamic voice configuration',SYSUTCDATETIME(),SUSER_SNAME());
GO

-- ============================================================
-- PHASE 6 - SHOPPER CUSTOMERS / ANONYMOUS VISITORS
-- SUB-PHASE: 6A - Customer Management
-- SUB-PHASE: 6B - Anonymous Visitors
-- SUB-PHASE: 6C - Customer Search
-- SUB-PHASE: 6D - Smart Customer Profile
-- SUB-PHASE: 6E - Angular Customer UI
-- SUB-PHASE: 6F - Angular Visitor UI
-- SUB-PHASE: 6G - Tenant Isolation & Authorization
-- SUB-PHASE: 6H - E2E / Database / Documentation
-- VERSION: V1.5.0
-- ============================================================
/*
 CustSearch AI — Phase 6 production database upgrade
 Version: V1.5.0
 Rules: idempotent, no EF migrations, SQL Server 2022, UTC timestamps, tenant/store predicates before paging.
*/
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
GO
/* Phase 6H — required deterministic SET options for filtered indexes on SQL Server 2022. */
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
SET ANSI_WARNINGS ON;
GO
SET CONCAT_NULL_YIELDS_NULL ON;
GO
SET ARITHABORT ON;
GO
SET NUMERIC_ROUNDABORT OFF;
GO

-- ============================================================
-- PHASE 6 - SHOPPER CUSTOMERS / ANONYMOUS VISITORS
-- SUB-PHASE: 6A - Customer Management
-- VERSION: V1.5.0
-- ============================================================
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        CustomerCode NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NULL,
        Mobile NVARCHAR(30) NULL,
        Email NVARCHAR(254) NULL,
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Customers_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Customers_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Customers_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_CustomerCode')
    CREATE UNIQUE INDEX UX_Customers_Tenant_CustomerCode ON dbo.Customers(TenantId,CustomerCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_Id')
    CREATE UNIQUE INDEX UX_Customers_Tenant_Id ON dbo.Customers(TenantId,Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Active')
    CREATE INDEX IX_Customers_Tenant_Active ON dbo.Customers(TenantId,IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Mobile')
    CREATE INDEX IX_Customers_Tenant_Mobile ON dbo.Customers(TenantId,Mobile) WHERE Mobile IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Email')
    CREATE INDEX IX_Customers_Tenant_Email ON dbo.Customers(TenantId,Email) WHERE Email IS NOT NULL;
GO

-- ============================================================
-- SUB-PHASE: 6G - Tenant Isolation & Authorization
-- Customer/store visibility uses tenant-safe composite foreign keys.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_Id')
    CREATE UNIQUE INDEX UX_Stores_Tenant_Id ON dbo.Stores(TenantId,Id);
GO

IF OBJECT_ID(N'dbo.CustomerStoreAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerStoreAssignments
    (
        TenantId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        IsPrimary BIT NOT NULL CONSTRAINT DF_CustomerStoreAssignments_IsPrimary DEFAULT(0),
        AssignedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_CustomerStoreAssignments_AssignedUtc DEFAULT(SYSUTCDATETIME()),
        AssignedByUserId BIGINT NOT NULL,
        CONSTRAINT PK_CustomerStoreAssignments PRIMARY KEY(CustomerId,StoreId),
        CONSTRAINT FK_CustomerStoreAssignments_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CustomerStoreAssignments_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_CustomerStoreAssignments_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_CustomerStoreAssignments_Users_AssignedBy FOREIGN KEY(AssignedByUserId) REFERENCES dbo.Users(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'IX_CustomerStoreAssignments_Tenant_Store')
    CREATE INDEX IX_CustomerStoreAssignments_Tenant_Store ON dbo.CustomerStoreAssignments(TenantId,StoreId,CustomerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'IX_CustomerStoreAssignments_Customer_Primary')
    CREATE INDEX IX_CustomerStoreAssignments_Customer_Primary ON dbo.CustomerStoreAssignments(CustomerId,IsPrimary);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'UX_CustomerStoreAssignments_Primary')
    CREATE UNIQUE INDEX UX_CustomerStoreAssignments_Primary ON dbo.CustomerStoreAssignments(CustomerId) WHERE IsPrimary=1;
GO

-- ============================================================
-- SUB-PHASE: 6B - Anonymous Visitors
-- No biometric/external identity fields are stored here; conversion is explicit and audited.
-- ============================================================
IF OBJECT_ID(N'dbo.AnonymousVisitors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnonymousVisitors
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnonymousVisitors PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        VisitorCode NVARCHAR(50) NOT NULL,
        FirstSeenUtc DATETIME2(7) NOT NULL,
        LastSeenUtc DATETIME2(7) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AnonymousVisitors_IsActive DEFAULT(1),
        ConvertedCustomerId BIGINT NULL,
        ConvertedUtc DATETIME2(7) NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_AnonymousVisitors_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_AnonymousVisitors_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_AnonymousVisitors_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_AnonymousVisitors_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_AnonymousVisitors_Customers_TenantCustomer FOREIGN KEY(TenantId,ConvertedCustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT CK_AnonymousVisitors_LastSeen CHECK(LastSeenUtc >= FirstSeenUtc),
        CONSTRAINT CK_AnonymousVisitors_Conversion CHECK((ConvertedCustomerId IS NULL AND ConvertedUtc IS NULL) OR (ConvertedCustomerId IS NOT NULL AND ConvertedUtc IS NOT NULL))
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'UX_AnonymousVisitors_Tenant_Store_Code')
    CREATE UNIQUE INDEX UX_AnonymousVisitors_Tenant_Store_Code ON dbo.AnonymousVisitors(TenantId,StoreId,VisitorCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'IX_AnonymousVisitors_Tenant_Store_Active_LastSeen')
    CREATE INDEX IX_AnonymousVisitors_Tenant_Store_Active_LastSeen ON dbo.AnonymousVisitors(TenantId,StoreId,IsActive,LastSeenUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'IX_AnonymousVisitors_Tenant_ConvertedCustomer')
    CREATE INDEX IX_AnonymousVisitors_Tenant_ConvertedCustomer ON dbo.AnonymousVisitors(TenantId,ConvertedCustomerId) WHERE ConvertedCustomerId IS NOT NULL;
GO

-- ============================================================
-- SUB-PHASE: 6C - Customer Search
-- Tenant/store authorization is applied before paging. NULL AllowedStoreIdsCsv means tenant-wide role.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Customer_Search
    @TenantId BIGINT,
    @AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,
    @StoreId BIGINT=NULL,
    @Search NVARCHAR(200)=NULL,
    @ActiveOnly BIT=0,
    @PageNumber INT=1,
    @PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 SET @PageNumber=1;
    IF @PageSize < 1 SET @PageSize=25;
    IF @PageSize > 100 SET @PageSize=100;

    ;WITH Filtered AS
    (
        SELECT c.Id,c.CustomerCode,c.FirstName,c.LastName,c.Mobile,c.Email,c.IsActive,c.UpdatedUtc
        FROM dbo.Customers c
        WHERE c.TenantId=@TenantId
          AND (@ActiveOnly=0 OR c.IsActive=1)
          AND (@Search IS NULL OR c.CustomerCode LIKE N'%'+@Search+N'%' OR c.FirstName LIKE N'%'+@Search+N'%'
               OR c.LastName LIKE N'%'+@Search+N'%' OR c.Mobile LIKE N'%'+@Search+N'%' OR c.Email LIKE N'%'+@Search+N'%')
          AND (@StoreId IS NULL OR EXISTS(
                SELECT 1 FROM dbo.CustomerStoreAssignments cs
                WHERE cs.TenantId=@TenantId AND cs.CustomerId=c.Id AND cs.StoreId=@StoreId))
          AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(
                SELECT 1
                FROM dbo.CustomerStoreAssignments cs
                INNER JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=cs.StoreId
                WHERE cs.TenantId=@TenantId AND cs.CustomerId=c.Id))
    )
    SELECT Id,CustomerCode,FirstName,LastName,Mobile,Email,IsActive,UpdatedUtc,COUNT_BIG(1) OVER() TotalCount
    FROM Filtered
    ORDER BY UpdatedUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.AnonymousVisitor_Search
    @TenantId BIGINT,
    @AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,
    @StoreId BIGINT=NULL,
    @Search NVARCHAR(200)=NULL,
    @ActiveOnly BIT=0,
    @PageNumber INT=1,
    @PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 SET @PageNumber=1;
    IF @PageSize < 1 SET @PageSize=25;
    IF @PageSize > 100 SET @PageSize=100;

    ;WITH Filtered AS
    (
        SELECT v.Id,v.VisitorCode,v.StoreId,v.FirstSeenUtc,v.LastSeenUtc,v.IsActive,v.ConvertedCustomerId,v.ConvertedUtc
        FROM dbo.AnonymousVisitors v
        WHERE v.TenantId=@TenantId
          AND (@ActiveOnly=0 OR v.IsActive=1)
          AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@Search IS NULL OR v.VisitorCode LIKE N'%'+@Search+N'%')
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(
                SELECT DISTINCT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT Id,VisitorCode,StoreId,FirstSeenUtc,LastSeenUtc,IsActive,ConvertedCustomerId,ConvertedUtc,COUNT_BIG(1) OVER() TotalCount
    FROM Filtered
    ORDER BY LastSeenUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================
-- SUB-PHASE: 6G - Tenant Isolation & Authorization
-- Reuse the stable Customers.* and Visitors.* permission names already defined by the application catalog.
-- ============================================================
DECLARE @Phase6Permissions TABLE(Name NVARCHAR(150));
INSERT INTO @Phase6Permissions(Name) VALUES
(N'Customers.View'),(N'Customers.Create'),(N'Customers.Edit'),(N'Visitors.View'),(N'Visitors.Convert');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME()
FROM @Phase6Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

-- Re-run the existing Phase 5 role provisioner so TenantAdmin/TenantOwner/ShopOwner/StoreManager/SalesStaff receive
-- newly materialized stable permissions according to the already-established role policy.
IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles',N'P') IS NOT NULL
BEGIN
    DECLARE @Phase6TenantId BIGINT;
    DECLARE Phase6TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
    OPEN Phase6TenantCursor; FETCH NEXT FROM Phase6TenantCursor INTO @Phase6TenantId;
    WHILE @@FETCH_STATUS=0
    BEGIN
        EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@Phase6TenantId;
        FETCH NEXT FROM Phase6TenantCursor INTO @Phase6TenantId;
    END
    CLOSE Phase6TenantCursor; DEALLOCATE Phase6TenantCursor;
END;
GO

-- CRMStaff receives the customer/visitor permissions required for Phase 6 CRM operations.
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 AND p.Name IN(N'Customers.View',N'Customers.Create',N'Customers.Edit',N'Visitors.View',N'Visitors.Convert')
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CRMSTAFF'
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

-- ============================================================
-- SUB-PHASE: 6H - E2E, Database & Documentation
-- Version ledger is idempotent and records Phase 6 only once.
-- ============================================================
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.5.0',N'Phase 6 shopper customers, anonymous visitors, smart profile foundation and tenant-safe search',SYSUTCDATETIME(),SUSER_SNAME());
GO
