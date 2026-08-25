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

-- ============================================================
-- PHASE 7 - HOUSEHOLDS / VISITS
-- SUB-PHASE: 7A - Household Management
-- SUB-PHASE: 7B - Household Members & Verified Relationships
-- SUB-PHASE: 7C - Visit Parties / Co-Visit Evidence
-- SUB-PHASE: 7D - Customer Visits
-- SUB-PHASE: 7E - Angular Household UI
-- SUB-PHASE: 7F - Angular Visits / Visit Party UI
-- SUB-PHASE: 7G - Tenant Isolation / Store Authorization / Privacy
-- SUB-PHASE: 7H - Database / E2E / Documentation
-- VERSION: V1.6.0
-- ============================================================
/*
 CustSearch AI — Phase 7 production database upgrade
 Version: V1.6.0
 Rules: idempotent, no EF migrations, SQL Server 2022, UTC timestamps, tenant/store predicates before paging.
 Privacy: Visit Party/co-visit evidence never creates or proves a Household/family relationship.
*/
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
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

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0')
    THROW 51200,'Phase 6 V1.5.0 must be installed before Phase 7.',1;
GO

-- ============================================================
-- PHASE 7 - HOUSEHOLDS / VISITS
-- SUB-PHASE: 7A - Household Management
-- VERSION: V1.6.0
-- ============================================================
IF OBJECT_ID(N'dbo.Households',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Households
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Households PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        HouseholdCode NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Households_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Households_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Households_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Households_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Households') AND name=N'UX_Households_Tenant_Code')
    CREATE UNIQUE INDEX UX_Households_Tenant_Code ON dbo.Households(TenantId,HouseholdCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Households') AND name=N'UX_Households_Tenant_Id')
    CREATE UNIQUE INDEX UX_Households_Tenant_Id ON dbo.Households(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Households') AND name=N'IX_Households_Tenant_Active_Updated')
    CREATE INDEX IX_Households_Tenant_Active_Updated ON dbo.Households(TenantId,IsActive,UpdatedUtc DESC);
GO

-- ============================================================
-- SUB-PHASE: 7B - Household Members & Verified Relationships
-- RelationshipSource: 1 CustomerProvided, 2 StaffVerified, 3 AdminVerified, 4 ImportedVerified.
-- No FaceInferredFamily value exists by design.
-- ============================================================
IF OBJECT_ID(N'dbo.HouseholdMembers',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HouseholdMembers
    (
        TenantId BIGINT NOT NULL,
        HouseholdId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        RelationshipType NVARCHAR(50) NOT NULL,
        RelationshipSource TINYINT NOT NULL,
        IsVerified BIT NOT NULL CONSTRAINT DF_HouseholdMembers_IsVerified DEFAULT(1),
        VerifiedByUserId BIGINT NOT NULL,
        VerifiedUtc DATETIME2(7) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_HouseholdMembers_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_HouseholdMembers_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_HouseholdMembers_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_HouseholdMembers PRIMARY KEY(HouseholdId,CustomerId),
        CONSTRAINT FK_HouseholdMembers_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_HouseholdMembers_Households_TenantHousehold FOREIGN KEY(TenantId,HouseholdId) REFERENCES dbo.Households(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_HouseholdMembers_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_HouseholdMembers_Users_VerifiedBy FOREIGN KEY(VerifiedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_HouseholdMembers_RelationshipSource CHECK(RelationshipSource BETWEEN 1 AND 4),
        CONSTRAINT CK_HouseholdMembers_Verified CHECK(IsVerified=1 AND VerifiedByUserId>0 AND VerifiedUtc IS NOT NULL)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HouseholdMembers') AND name=N'IX_HouseholdMembers_Tenant_Customer_Active')
    CREATE INDEX IX_HouseholdMembers_Tenant_Customer_Active ON dbo.HouseholdMembers(TenantId,CustomerId,IsActive);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HouseholdMembers') AND name=N'IX_HouseholdMembers_Tenant_Household_Active')
    CREATE INDEX IX_HouseholdMembers_Tenant_Household_Active ON dbo.HouseholdMembers(TenantId,HouseholdId,IsActive);
GO

-- ============================================================
-- SUB-PHASE: 7C - Visit Parties / Co-Visit Evidence
-- A VisitParty means identities were observed visiting together. It does NOT mean family.
-- ============================================================
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'UX_AnonymousVisitors_Tenant_Store_Id')
    CREATE UNIQUE INDEX UX_AnonymousVisitors_Tenant_Store_Id ON dbo.AnonymousVisitors(TenantId,StoreId,Id);
GO

IF OBJECT_ID(N'dbo.VisitParties',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VisitParties
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VisitParties PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        PartyCode NVARCHAR(50) NOT NULL,
        StartedUtc DATETIME2(7) NOT NULL,
        EndedUtc DATETIME2(7) NULL,
        Source TINYINT NOT NULL,
        Status TINYINT NOT NULL CONSTRAINT DF_VisitParties_Status DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_VisitParties_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_VisitParties_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_VisitParties_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_VisitParties_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_VisitParties_Source CHECK(Source BETWEEN 1 AND 4),
        CONSTRAINT CK_VisitParties_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_VisitParties_Period CHECK(EndedUtc IS NULL OR EndedUtc>=StartedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitParties') AND name=N'UX_VisitParties_Tenant_Store_Code')
    CREATE UNIQUE INDEX UX_VisitParties_Tenant_Store_Code ON dbo.VisitParties(TenantId,StoreId,PartyCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitParties') AND name=N'UX_VisitParties_Tenant_Store_Id')
    CREATE UNIQUE INDEX UX_VisitParties_Tenant_Store_Id ON dbo.VisitParties(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitParties') AND name=N'IX_VisitParties_Tenant_Store_Status_Start')
    CREATE INDEX IX_VisitParties_Tenant_Store_Status_Start ON dbo.VisitParties(TenantId,StoreId,Status,StartedUtc DESC);
GO

IF OBJECT_ID(N'dbo.VisitPartyMembers',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VisitPartyMembers
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VisitPartyMembers PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        VisitPartyId BIGINT NOT NULL,
        IdentityType TINYINT NOT NULL,
        CustomerId BIGINT NULL,
        AnonymousVisitorId BIGINT NULL,
        JoinedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_VisitPartyMembers_Party_TenantStoreParty FOREIGN KEY(TenantId,StoreId,VisitPartyId) REFERENCES dbo.VisitParties(TenantId,StoreId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_VisitPartyMembers_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_VisitPartyMembers_Visitors_TenantStoreVisitor FOREIGN KEY(TenantId,StoreId,AnonymousVisitorId) REFERENCES dbo.AnonymousVisitors(TenantId,StoreId,Id),
        CONSTRAINT CK_VisitPartyMembers_IdentityType CHECK(IdentityType IN(1,2)),
        CONSTRAINT CK_VisitPartyMembers_IdentityXor CHECK(
            (IdentityType=1 AND CustomerId IS NOT NULL AND AnonymousVisitorId IS NULL)
            OR (IdentityType=2 AND CustomerId IS NULL AND AnonymousVisitorId IS NOT NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'IX_VisitPartyMembers_Tenant_Party')
    CREATE INDEX IX_VisitPartyMembers_Tenant_Party ON dbo.VisitPartyMembers(TenantId,StoreId,VisitPartyId,JoinedUtc);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'UX_VisitPartyMembers_Party_Customer')
    CREATE UNIQUE INDEX UX_VisitPartyMembers_Party_Customer ON dbo.VisitPartyMembers(VisitPartyId,CustomerId) WHERE CustomerId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'UX_VisitPartyMembers_Party_Visitor')
    CREATE UNIQUE INDEX UX_VisitPartyMembers_Party_Visitor ON dbo.VisitPartyMembers(VisitPartyId,AnonymousVisitorId) WHERE AnonymousVisitorId IS NOT NULL;
GO

-- ============================================================
-- SUB-PHASE: 7D - Customer Visits
-- Factual visit history only. Purchases/invoices/preferences are not fabricated here.
-- ============================================================
IF OBJECT_ID(N'dbo.CustomerVisits',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerVisits
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerVisits PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        VisitPartyId BIGINT NULL,
        VisitCode NVARCHAR(50) NOT NULL,
        EnteredUtc DATETIME2(7) NOT NULL,
        ExitedUtc DATETIME2(7) NULL,
        Source TINYINT NOT NULL,
        Status TINYINT NOT NULL CONSTRAINT DF_CustomerVisits_Status DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_CustomerVisits_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_CustomerVisits_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_CustomerVisits_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CustomerVisits_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_CustomerVisits_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_CustomerVisits_Parties_TenantStoreParty FOREIGN KEY(TenantId,StoreId,VisitPartyId) REFERENCES dbo.VisitParties(TenantId,StoreId,Id),
        CONSTRAINT CK_CustomerVisits_Source CHECK(Source BETWEEN 1 AND 4),
        CONSTRAINT CK_CustomerVisits_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_CustomerVisits_Period CHECK(ExitedUtc IS NULL OR ExitedUtc>=EnteredUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'UX_CustomerVisits_Tenant_Code')
    CREATE UNIQUE INDEX UX_CustomerVisits_Tenant_Code ON dbo.CustomerVisits(TenantId,VisitCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'IX_CustomerVisits_Tenant_Store_Entered')
    CREATE INDEX IX_CustomerVisits_Tenant_Store_Entered ON dbo.CustomerVisits(TenantId,StoreId,EnteredUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'IX_CustomerVisits_Tenant_Customer_Entered')
    CREATE INDEX IX_CustomerVisits_Tenant_Customer_Entered ON dbo.CustomerVisits(TenantId,CustomerId,EnteredUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'IX_CustomerVisits_Tenant_Party')
    CREATE INDEX IX_CustomerVisits_Tenant_Party ON dbo.CustomerVisits(TenantId,VisitPartyId) WHERE VisitPartyId IS NOT NULL;
GO

-- ============================================================
-- 7A/7G - Household search. Store-scoped visibility is derived from an active member's authorized customer-store assignment.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Household_Search
    @TenantId BIGINT,
    @AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,
    @Search NVARCHAR(200)=NULL,
    @ActiveOnly BIT=0,
    @PageNumber INT=1,
    @PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber<1 SET @PageNumber=1; IF @PageSize<1 SET @PageSize=25; IF @PageSize>100 SET @PageSize=100;
    ;WITH Filtered AS
    (
        SELECT h.Id,h.HouseholdCode,h.Name,h.IsActive,h.UpdatedUtc,
          (SELECT COUNT(*) FROM dbo.HouseholdMembers hm
           WHERE hm.TenantId=@TenantId AND hm.HouseholdId=h.Id AND hm.IsActive=1
             AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(
                 SELECT 1 FROM dbo.CustomerStoreAssignments csa
                 INNER JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId
                 WHERE csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId))) VisibleMemberCount
        FROM dbo.Households h
        WHERE h.TenantId=@TenantId
          AND (@ActiveOnly=0 OR h.IsActive=1)
          AND (@Search IS NULL OR h.HouseholdCode LIKE N'%'+@Search+N'%' OR h.Name LIKE N'%'+@Search+N'%')
          AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(
              SELECT 1 FROM dbo.HouseholdMembers hm
              INNER JOIN dbo.CustomerStoreAssignments csa ON csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId
              INNER JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId
              WHERE hm.TenantId=@TenantId AND hm.HouseholdId=h.Id AND hm.IsActive=1))
    )
    SELECT Id,HouseholdCode,Name,VisibleMemberCount,IsActive,UpdatedUtc,COUNT_BIG(1) OVER() TotalCount
    FROM Filtered ORDER BY UpdatedUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Household_GetDetail @TenantId BIGINT,@HouseholdId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @AllowedStoreIdsCsv IS NOT NULL AND NOT EXISTS(
        SELECT 1 FROM dbo.HouseholdMembers hm JOIN dbo.CustomerStoreAssignments csa ON csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId
        JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId
        WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1) RETURN;
    SELECT Id,HouseholdCode,Name,Notes,IsActive,CreatedUtc,UpdatedUtc FROM dbo.Households WHERE TenantId=@TenantId AND Id=@HouseholdId;
    SELECT hm.CustomerId,c.CustomerCode,c.FirstName,c.LastName,hm.RelationshipType,hm.RelationshipSource,hm.IsVerified,hm.VerifiedByUserId,hm.VerifiedUtc,hm.IsActive
    FROM dbo.HouseholdMembers hm JOIN dbo.Customers c ON c.TenantId=hm.TenantId AND c.Id=hm.CustomerId
    WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId
      AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId))
    ORDER BY hm.IsActive DESC,c.FirstName,c.LastName;
END;
GO

-- ============================================================
-- 7D/7G - Customer visit search with tenant/store predicates before paging.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.CustomerVisit_Search
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@CustomerId BIGINT=NULL,@Search NVARCHAR(200)=NULL,
    @FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON; IF @PageNumber<1 SET @PageNumber=1; IF @PageSize<1 SET @PageSize=25; IF @PageSize>100 SET @PageSize=100;
    ;WITH Filtered AS
    (
      SELECT v.Id,v.VisitCode,v.CustomerId,c.CustomerCode,CONCAT(c.FirstName,CASE WHEN c.LastName IS NULL THEN N'' ELSE N' '+c.LastName END) CustomerName,
             v.StoreId,v.VisitPartyId,v.EnteredUtc,v.ExitedUtc,v.Source,v.Status
      FROM dbo.CustomerVisits v JOIN dbo.Customers c ON c.TenantId=v.TenantId AND c.Id=v.CustomerId
      WHERE v.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId) AND (@CustomerId IS NULL OR v.CustomerId=@CustomerId)
        AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc)
        AND (@Search IS NULL OR v.VisitCode LIKE N'%'+@Search+N'%' OR c.CustomerCode LIKE N'%'+@Search+N'%' OR c.FirstName LIKE N'%'+@Search+N'%' OR c.LastName LIKE N'%'+@Search+N'%')
        AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT *,COUNT_BIG(1) OVER() TotalCount FROM Filtered ORDER BY EnteredUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================
-- 7C/7G - Visit Party / Co-Visit search/detail. These procedures never join or infer Households.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.VisitParty_Search
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@Search NVARCHAR(200)=NULL,@Status TINYINT=NULL,
    @FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON; IF @PageNumber<1 SET @PageNumber=1; IF @PageSize<1 SET @PageSize=25; IF @PageSize>100 SET @PageSize=100;
    ;WITH Filtered AS
    (
      SELECT p.Id,p.PartyCode,p.StoreId,p.StartedUtc,p.EndedUtc,p.Source,p.Status,(SELECT COUNT(*) FROM dbo.VisitPartyMembers m WHERE m.TenantId=@TenantId AND m.VisitPartyId=p.Id) MemberCount
      FROM dbo.VisitParties p
      WHERE p.TenantId=@TenantId AND (@StoreId IS NULL OR p.StoreId=@StoreId) AND (@Status IS NULL OR p.Status=@Status)
        AND (@Search IS NULL OR p.PartyCode LIKE N'%'+@Search+N'%') AND (@FromUtc IS NULL OR p.StartedUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.StartedUtc<@ToUtc)
        AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT *,COUNT_BIG(1) OVER() TotalCount FROM Filtered ORDER BY StartedUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.VisitParty_GetDetail @TenantId BIGINT,@VisitPartyId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id,p.PartyCode,p.StoreId,p.StartedUtc,p.EndedUtc,p.Source,p.Status,p.CreatedUtc,p.UpdatedUtc
    FROM dbo.VisitParties p WHERE p.TenantId=@TenantId AND p.Id=@VisitPartyId
      AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
    SELECT m.Id,m.IdentityType,m.CustomerId,c.CustomerCode,m.AnonymousVisitorId,av.VisitorCode,m.JoinedUtc
    FROM dbo.VisitPartyMembers m LEFT JOIN dbo.Customers c ON c.TenantId=m.TenantId AND c.Id=m.CustomerId
    LEFT JOIN dbo.AnonymousVisitors av ON av.TenantId=m.TenantId AND av.StoreId=m.StoreId AND av.Id=m.AnonymousVisitorId
    JOIN dbo.VisitParties p ON p.TenantId=m.TenantId AND p.StoreId=m.StoreId AND p.Id=m.VisitPartyId
    WHERE m.TenantId=@TenantId AND m.VisitPartyId=@VisitPartyId
      AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    ORDER BY m.JoinedUtc,m.Id;
END;
GO

-- ============================================================
-- 7G - Permission catalog and existing role provisioning.
-- ============================================================
DECLARE @Phase7Permissions TABLE(Name NVARCHAR(150));
INSERT @Phase7Permissions(Name) VALUES
(N'Households.View'),(N'Households.Create'),(N'Households.Edit'),(N'Households.ManageMembers'),(N'Visits.View'),(N'Visits.Edit'),(N'VisitParties.View');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME() FROM @Phase7Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO
IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles',N'P') IS NOT NULL
BEGIN
    DECLARE @Phase7TenantId BIGINT;
    DECLARE Phase7TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
    OPEN Phase7TenantCursor; FETCH NEXT FROM Phase7TenantCursor INTO @Phase7TenantId;
    WHILE @@FETCH_STATUS=0 BEGIN EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@Phase7TenantId; FETCH NEXT FROM Phase7TenantCursor INTO @Phase7TenantId; END
    CLOSE Phase7TenantCursor; DEALLOCATE Phase7TenantCursor;
END;
GO
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CRMSTAFF'
  AND p.Name IN(N'Households.View',N'Households.Create',N'Households.Edit',N'Households.ManageMembers',N'Visits.View',N'VisitParties.View')
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

-- ============================================================
-- 7H - Idempotent version ledger.
-- ============================================================
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.6.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.6.0',N'Phase 7 verified households, co-visit parties and factual customer visits',SYSUTCDATETIME(),SUSER_SNAME());
GO

-- ============================================================
-- PHASE 8 - PRODUCTS & RETAIL BILLING
-- VERSION: V1.7.0
-- ============================================================
/* CustSearch AI — Phase 8 / V1.7.0 — Products & Retail Billing */
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.6.0')
    THROW 51300,'Phase 7 V1.6.0 must be installed before Phase 8.',1;
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'UX_ProductCategories_Tenant_Id')
    CREATE UNIQUE INDEX UX_ProductCategories_Tenant_Id ON dbo.ProductCategories(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'UX_CustomerVisits_Tenant_Store_Id')
    CREATE UNIQUE INDEX UX_CustomerVisits_Tenant_Store_Id ON dbo.CustomerVisits(TenantId,StoreId,Id);
GO

-- 8A Product catalog
IF OBJECT_ID(N'dbo.Products',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.Products(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
  TenantId BIGINT NOT NULL, ProductCode NVARCHAR(50) NOT NULL, Barcode NVARCHAR(100) NULL,
  Name NVARCHAR(200) NOT NULL, Description NVARCHAR(1000) NULL, CategoryId BIGINT NOT NULL,
  Brand NVARCHAR(150) NULL, UnitName NVARCHAR(50) NOT NULL, SalePrice DECIMAL(18,2) NOT NULL,
  CostPrice DECIMAL(18,2) NULL, TaxPercent DECIMAL(9,4) NULL, IsActive BIT NOT NULL CONSTRAINT DF_Products_Active DEFAULT(1),
  CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Products_Created DEFAULT(SYSUTCDATETIME()), UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Products_Updated DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT FK_Products_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
  CONSTRAINT FK_Products_Categories_TenantCategory FOREIGN KEY(TenantId,CategoryId) REFERENCES dbo.ProductCategories(TenantId,Id),
  CONSTRAINT CK_Products_Prices CHECK(SalePrice>=0 AND (CostPrice IS NULL OR CostPrice>=0)),
  CONSTRAINT CK_Products_Tax CHECK(TaxPercent IS NULL OR TaxPercent BETWEEN 0 AND 100));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'UX_Products_Tenant_Code') CREATE UNIQUE INDEX UX_Products_Tenant_Code ON dbo.Products(TenantId,ProductCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'UX_Products_Tenant_Id') CREATE UNIQUE INDEX UX_Products_Tenant_Id ON dbo.Products(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'UX_Products_Tenant_Barcode') CREATE UNIQUE INDEX UX_Products_Tenant_Barcode ON dbo.Products(TenantId,Barcode) WHERE Barcode IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'IX_Products_Tenant_Category_Active') CREATE INDEX IX_Products_Tenant_Category_Active ON dbo.Products(TenantId,CategoryId,IsActive) INCLUDE(ProductCode,Name,SalePrice,TaxPercent);
GO
IF OBJECT_ID(N'dbo.ProductStoreAvailabilities',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ProductStoreAvailabilities(
  TenantId BIGINT NOT NULL, ProductId BIGINT NOT NULL, StoreId BIGINT NOT NULL, IsActive BIT NOT NULL CONSTRAINT DF_ProductStores_Active DEFAULT(1),
  CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductStores_Created DEFAULT(SYSUTCDATETIME()), UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductStores_Updated DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT PK_ProductStoreAvailabilities PRIMARY KEY(ProductId,StoreId),
  CONSTRAINT FK_ProductStores_Products_TenantProduct FOREIGN KEY(TenantId,ProductId) REFERENCES dbo.Products(TenantId,Id) ON DELETE CASCADE,
  CONSTRAINT FK_ProductStores_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductStoreAvailabilities') AND name=N'IX_ProductStores_Tenant_Store_Active') CREATE INDEX IX_ProductStores_Tenant_Store_Active ON dbo.ProductStoreAvailabilities(TenantId,StoreId,IsActive,ProductId);
GO

-- 8B Invoice headers
IF OBJECT_ID(N'dbo.RetailInvoices',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetailInvoices(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoices PRIMARY KEY,
  TenantId BIGINT NOT NULL, StoreId BIGINT NOT NULL, InvoiceNumber NVARCHAR(50) NOT NULL,
  CustomerId BIGINT NULL, HouseholdId BIGINT NULL, CustomerVisitId BIGINT NULL, VisitPartyId BIGINT NULL,
  InvoiceUtc DATETIME2(7) NOT NULL, Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Subtotal DEFAULT(0),
  DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Discount DEFAULT(0), TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Tax DEFAULT(0),
  GrandTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Total DEFAULT(0), PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Paid DEFAULT(0), BalanceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Balance DEFAULT(0),
  Status TINYINT NOT NULL CONSTRAINT DF_RetailInvoices_Status DEFAULT(1), Notes NVARCHAR(1000) NULL, CreatedByUserId BIGINT NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoices_Created DEFAULT(SYSUTCDATETIME()), UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoices_Updated DEFAULT(SYSUTCDATETIME()),
  CancelledUtc DATETIME2(7) NULL, CancelledByUserId BIGINT NULL, CancellationReason NVARCHAR(500) NULL, RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_RetailInvoices_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
  CONSTRAINT FK_RetailInvoices_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
  CONSTRAINT FK_RetailInvoices_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
  CONSTRAINT FK_RetailInvoices_Households_TenantHousehold FOREIGN KEY(TenantId,HouseholdId) REFERENCES dbo.Households(TenantId,Id),
  CONSTRAINT FK_RetailInvoices_Visits_TenantStoreVisit FOREIGN KEY(TenantId,StoreId,CustomerVisitId) REFERENCES dbo.CustomerVisits(TenantId,StoreId,Id),
  CONSTRAINT FK_RetailInvoices_Parties_TenantStoreParty FOREIGN KEY(TenantId,StoreId,VisitPartyId) REFERENCES dbo.VisitParties(TenantId,StoreId,Id),
  CONSTRAINT FK_RetailInvoices_Users_Created FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_RetailInvoices_Users_Cancelled FOREIGN KEY(CancelledByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT CK_RetailInvoices_Status CHECK(Status BETWEEN 1 AND 5),
  CONSTRAINT CK_RetailInvoices_Amounts CHECK(Subtotal>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND GrandTotal>=0 AND PaidAmount>=0 AND BalanceAmount>=0 AND DiscountAmount<=Subtotal AND GrandTotal=Subtotal-DiscountAmount+TaxAmount AND PaidAmount<=GrandTotal AND BalanceAmount=GrandTotal-PaidAmount),
  CONSTRAINT CK_RetailInvoices_Cancel CHECK((Status=5 AND CancelledUtc IS NOT NULL AND CancelledByUserId IS NOT NULL AND CancellationReason IS NOT NULL) OR (Status<>5 AND CancelledUtc IS NULL AND CancelledByUserId IS NULL)));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'UX_RetailInvoices_Tenant_Store_Number') CREATE UNIQUE INDEX UX_RetailInvoices_Tenant_Store_Number ON dbo.RetailInvoices(TenantId,StoreId,InvoiceNumber);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'UX_RetailInvoices_Tenant_Id') CREATE UNIQUE INDEX UX_RetailInvoices_Tenant_Id ON dbo.RetailInvoices(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'UX_RetailInvoices_Tenant_Store_Id') CREATE UNIQUE INDEX UX_RetailInvoices_Tenant_Store_Id ON dbo.RetailInvoices(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'IX_RetailInvoices_Tenant_Store_Date') CREATE INDEX IX_RetailInvoices_Tenant_Store_Date ON dbo.RetailInvoices(TenantId,StoreId,InvoiceUtc DESC) INCLUDE(InvoiceNumber,CustomerId,GrandTotal,PaidAmount,BalanceAmount,Status);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'IX_RetailInvoices_Tenant_Customer_Date') CREATE INDEX IX_RetailInvoices_Tenant_Customer_Date ON dbo.RetailInvoices(TenantId,CustomerId,InvoiceUtc DESC) WHERE CustomerId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'IX_RetailInvoices_Tenant_Status_Date') CREATE INDEX IX_RetailInvoices_Tenant_Status_Date ON dbo.RetailInvoices(TenantId,Status,InvoiceUtc DESC);
GO

-- 8C Immutable invoice item snapshots
IF OBJECT_ID(N'dbo.RetailInvoiceItems',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetailInvoiceItems(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoiceItems PRIMARY KEY, TenantId BIGINT NOT NULL, InvoiceId BIGINT NOT NULL,
  ProductId BIGINT NULL, CategoryId BIGINT NULL, ProductCodeSnapshot NVARCHAR(50) NOT NULL, ProductNameSnapshot NVARCHAR(200) NOT NULL, CategoryNameSnapshot NVARCHAR(150) NULL,
  Quantity DECIMAL(18,4) NOT NULL, UnitPrice DECIMAL(18,2) NOT NULL, DiscountAmount DECIMAL(18,2) NOT NULL, TaxPercent DECIMAL(9,4) NOT NULL,
  TaxAmount DECIMAL(18,2) NOT NULL, LineSubtotal DECIMAL(18,2) NOT NULL, LineTotal DECIMAL(18,2) NOT NULL, CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailItems_Created DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT FK_RetailItems_Invoices_TenantInvoice FOREIGN KEY(TenantId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,Id) ON DELETE CASCADE,
  CONSTRAINT FK_RetailItems_Products_TenantProduct FOREIGN KEY(TenantId,ProductId) REFERENCES dbo.Products(TenantId,Id),
  CONSTRAINT FK_RetailItems_Categories_TenantCategory FOREIGN KEY(TenantId,CategoryId) REFERENCES dbo.ProductCategories(TenantId,Id),
  CONSTRAINT CK_RetailItems_Qty CHECK(Quantity>0), CONSTRAINT CK_RetailItems_Amounts CHECK(UnitPrice>=0 AND DiscountAmount>=0 AND TaxPercent BETWEEN 0 AND 100 AND TaxAmount>=0 AND LineSubtotal>=0 AND LineTotal>=0 AND DiscountAmount<=LineSubtotal AND LineTotal=LineSubtotal-DiscountAmount+TaxAmount));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItems') AND name=N'UX_RetailItems_Tenant_Invoice_Id') CREATE UNIQUE INDEX UX_RetailItems_Tenant_Invoice_Id ON dbo.RetailInvoiceItems(TenantId,InvoiceId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItems') AND name=N'IX_RetailItems_Tenant_Product') CREATE INDEX IX_RetailItems_Tenant_Product ON dbo.RetailInvoiceItems(TenantId,ProductId) INCLUDE(InvoiceId,Quantity,LineTotal);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItems') AND name=N'IX_RetailItems_Tenant_Category') CREATE INDEX IX_RetailItems_Tenant_Category ON dbo.RetailInvoiceItems(TenantId,CategoryId) INCLUDE(InvoiceId,LineTotal);
GO

-- 8D Payments
IF OBJECT_ID(N'dbo.RetailInvoicePayments',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetailInvoicePayments(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoicePayments PRIMARY KEY, TenantId BIGINT NOT NULL, StoreId BIGINT NOT NULL, InvoiceId BIGINT NOT NULL,
  PaymentReference NVARCHAR(100) NOT NULL, PaymentMethod TINYINT NOT NULL, Amount DECIMAL(18,2) NOT NULL, PaymentUtc DATETIME2(7) NOT NULL, Status TINYINT NOT NULL,
  ExternalTransactionId NVARCHAR(150) NULL, Notes NVARCHAR(500) NULL, ReceivedByUserId BIGINT NOT NULL, CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailPayments_Created DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT FK_RetailPayments_Invoices_TenantStoreInvoice FOREIGN KEY(TenantId,StoreId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,StoreId,Id),
  CONSTRAINT FK_RetailPayments_Users FOREIGN KEY(ReceivedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT CK_RetailPayments_Method CHECK(PaymentMethod BETWEEN 1 AND 5), CONSTRAINT CK_RetailPayments_Status CHECK(Status BETWEEN 1 AND 4), CONSTRAINT CK_RetailPayments_Amount CHECK(Amount>0));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoicePayments') AND name=N'UX_RetailPayments_Tenant_Reference') CREATE UNIQUE INDEX UX_RetailPayments_Tenant_Reference ON dbo.RetailInvoicePayments(TenantId,PaymentReference);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoicePayments') AND name=N'IX_RetailPayments_Tenant_Store_Date') CREATE INDEX IX_RetailPayments_Tenant_Store_Date ON dbo.RetailInvoicePayments(TenantId,StoreId,PaymentUtc DESC) INCLUDE(InvoiceId,PaymentMethod,Amount,Status);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoicePayments') AND name=N'IX_RetailPayments_Tenant_Invoice_Status') CREATE INDEX IX_RetailPayments_Tenant_Invoice_Status ON dbo.RetailInvoicePayments(TenantId,InvoiceId,Status) INCLUDE(Amount);
GO

-- 8E Explicit participants
IF OBJECT_ID(N'dbo.RetailInvoiceParticipants',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetailInvoiceParticipants(
  TenantId BIGINT NOT NULL, InvoiceId BIGINT NOT NULL, CustomerId BIGINT NOT NULL, ParticipationType TINYINT NOT NULL, IsPayer BIT NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailParticipants_Created DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT PK_RetailInvoiceParticipants PRIMARY KEY(InvoiceId,CustomerId),
  CONSTRAINT FK_RetailParticipants_Invoices_TenantInvoice FOREIGN KEY(TenantId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,Id) ON DELETE CASCADE,
  CONSTRAINT FK_RetailParticipants_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
  CONSTRAINT CK_RetailParticipants_Type CHECK(ParticipationType BETWEEN 1 AND 4),
  CONSTRAINT CK_RetailParticipants_Payer CHECK((ParticipationType=1 AND IsPayer=1) OR (ParticipationType<>1 AND IsPayer=0)));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceParticipants') AND name=N'UX_RetailParticipants_OnePayer') CREATE UNIQUE INDEX UX_RetailParticipants_OnePayer ON dbo.RetailInvoiceParticipants(InvoiceId) WHERE IsPayer=1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceParticipants') AND name=N'IX_RetailParticipants_Tenant_Customer') CREATE INDEX IX_RetailParticipants_Tenant_Customer ON dbo.RetailInvoiceParticipants(TenantId,CustomerId,InvoiceId);
GO

-- 8F Explicit auditable spend attribution; no cascade paths and no inferred identity source.
IF OBJECT_ID(N'dbo.RetailInvoiceItemAttributions',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetailInvoiceItemAttributions(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoiceItemAttributions PRIMARY KEY, TenantId BIGINT NOT NULL, InvoiceId BIGINT NOT NULL, InvoiceItemId BIGINT NOT NULL, CustomerId BIGINT NOT NULL,
  AttributionType TINYINT NOT NULL, QuantityAttributed DECIMAL(18,4) NULL, AmountAttributed DECIMAL(18,2) NOT NULL, Source TINYINT NOT NULL, CreatedByUserId BIGINT NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailAttributions_Created DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT FK_RetailAttributions_Invoices_TenantInvoice FOREIGN KEY(TenantId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,Id),
  CONSTRAINT FK_RetailAttributions_Items_TenantInvoiceItem FOREIGN KEY(TenantId,InvoiceId,InvoiceItemId) REFERENCES dbo.RetailInvoiceItems(TenantId,InvoiceId,Id),
  CONSTRAINT FK_RetailAttributions_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
  CONSTRAINT FK_RetailAttributions_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT CK_RetailAttributions_Type CHECK(AttributionType BETWEEN 1 AND 3), CONSTRAINT CK_RetailAttributions_Source CHECK(Source BETWEEN 1 AND 4),
  CONSTRAINT CK_RetailAttributions_Amount CHECK(AmountAttributed>0), CONSTRAINT CK_RetailAttributions_Qty CHECK(QuantityAttributed IS NULL OR QuantityAttributed>0));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItemAttributions') AND name=N'UX_RetailAttributions_Item_Customer') CREATE UNIQUE INDEX UX_RetailAttributions_Item_Customer ON dbo.RetailInvoiceItemAttributions(TenantId,InvoiceItemId,CustomerId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItemAttributions') AND name=N'IX_RetailAttributions_Tenant_Customer') CREATE INDEX IX_RetailAttributions_Tenant_Customer ON dbo.RetailInvoiceItemAttributions(TenantId,CustomerId,InvoiceId) INCLUDE(AmountAttributed);
GO

-- 8H Search and report procedures
CREATE OR ALTER PROCEDURE dbo.Product_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@CategoryId BIGINT=NULL,@Search NVARCHAR(200)=NULL,@ActiveOnly BIT=0,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
 SET NOCOUNT ON;IF @PageNumber<1 SET @PageNumber=1;IF @PageSize<1 SET @PageSize=25;IF @PageSize>100 SET @PageSize=100;
 ;WITH F AS(SELECT p.Id,p.ProductCode,p.Barcode,p.Name,p.CategoryId,c.Name CategoryName,p.Brand,p.UnitName,p.SalePrice,p.TaxPercent,p.IsActive FROM dbo.Products p JOIN dbo.ProductCategories c ON c.TenantId=p.TenantId AND c.Id=p.CategoryId
 WHERE p.TenantId=@TenantId AND (@ActiveOnly=0 OR p.IsActive=1) AND (@CategoryId IS NULL OR p.CategoryId=@CategoryId)
 AND (@Search IS NULL OR p.ProductCode LIKE N'%'+@Search+N'%' OR p.Barcode LIKE N'%'+@Search+N'%' OR p.Name LIKE N'%'+@Search+N'%' OR p.Brand LIKE N'%'+@Search+N'%')
 AND (@StoreId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.IsActive=1) OR EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.StoreId=@StoreId AND a.IsActive=1))
 AND (@AllowedStoreIdsCsv IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.IsActive=1) OR EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=a.StoreId WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.IsActive=1)))
 SELECT *,COUNT_BIG(*) OVER() TotalCount FROM F ORDER BY Name,Id OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailInvoice_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@CustomerId BIGINT=NULL,@Status TINYINT=NULL,@Search NVARCHAR(200)=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
 SET NOCOUNT ON;IF @PageNumber<1 SET @PageNumber=1;IF @PageSize<1 SET @PageSize=25;IF @PageSize>100 SET @PageSize=100;
 ;WITH F AS(SELECT i.Id,i.InvoiceNumber,i.StoreId,i.CustomerId,c.CustomerCode,CASE WHEN c.Id IS NULL THEN NULL ELSE CONCAT(c.FirstName,CASE WHEN c.LastName IS NULL THEN N'' ELSE N' '+c.LastName END) END CustomerName,i.InvoiceUtc,i.GrandTotal,i.PaidAmount,i.BalanceAmount,i.Status FROM dbo.RetailInvoices i LEFT JOIN dbo.Customers c ON c.TenantId=i.TenantId AND c.Id=i.CustomerId
 WHERE i.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@CustomerId IS NULL OR i.CustomerId=@CustomerId) AND (@Status IS NULL OR i.Status=@Status) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
 AND (@Search IS NULL OR i.InvoiceNumber LIKE N'%'+@Search+N'%' OR c.CustomerCode LIKE N'%'+@Search+N'%' OR c.FirstName LIKE N'%'+@Search+N'%' OR c.LastName LIKE N'%'+@Search+N'%')
 AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)))
 SELECT *,COUNT_BIG(*) OVER() TotalCount FROM F ORDER BY InvoiceUtc DESC,Id DESC OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailInvoice_GetDetail @TenantId BIGINT,@InvoiceId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SELECT i.* FROM dbo.RetailInvoices i WHERE i.TenantId=@TenantId AND i.Id=@InvoiceId AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
 SELECT x.* FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId WHERE x.TenantId=@TenantId AND x.InvoiceId=@InvoiceId AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)) ORDER BY x.Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.CustomerPurchaseHistory_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@CustomerId BIGINT,@RecentCount INT=10
AS
BEGIN
 SET NOCOUNT ON;IF @RecentCount<1 SET @RecentCount=10;IF @RecentCount>100 SET @RecentCount=100;
 DECLARE @Visible TABLE(Id BIGINT PRIMARY KEY,StoreId BIGINT,InvoiceUtc DATETIME2(7),InvoiceNumber NVARCHAR(50),Status TINYINT,GrandTotal DECIMAL(18,2));
 INSERT @Visible(Id,StoreId,InvoiceUtc,InvoiceNumber,Status,GrandTotal)
 SELECT DISTINCT i.Id,i.StoreId,i.InvoiceUtc,i.InvoiceNumber,i.Status,i.GrandTotal FROM dbo.RetailInvoices i
 LEFT JOIN dbo.RetailInvoiceParticipants p ON p.TenantId=i.TenantId AND p.InvoiceId=i.Id AND p.CustomerId=@CustomerId
 LEFT JOIN dbo.RetailInvoiceItemAttributions a ON a.TenantId=i.TenantId AND a.InvoiceId=i.Id AND a.CustomerId=@CustomerId
 WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (i.CustomerId=@CustomerId OR p.CustomerId IS NOT NULL OR a.CustomerId IS NOT NULL)
 AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
 SELECT @CustomerId CustomerId,COUNT_BIG(*) InvoiceCount,
  COALESCE((SELECT SUM(i.GrandTotal) FROM dbo.RetailInvoiceParticipants p JOIN dbo.RetailInvoices i ON i.TenantId=p.TenantId AND i.Id=p.InvoiceId WHERE p.TenantId=@TenantId AND p.CustomerId=@CustomerId AND p.IsPayer=1 AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))),0) PayerSpend,
  COALESCE((SELECT SUM(a.AmountAttributed) FROM dbo.RetailInvoiceItemAttributions a JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId WHERE a.TenantId=@TenantId AND a.CustomerId=@CustomerId AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))),0) ExplicitAttributedSpend,
  MAX(InvoiceUtc) LastPurchaseUtc,(SELECT TOP(1) StoreId FROM @Visible ORDER BY InvoiceUtc DESC,Id DESC) LastPurchaseStoreId FROM @Visible;
 SELECT TOP(@RecentCount) v.Id InvoiceId,v.InvoiceNumber,v.StoreId,v.InvoiceUtc,v.Status,v.GrandTotal,
  CASE WHEN EXISTS(SELECT 1 FROM dbo.RetailInvoiceParticipants p WHERE p.TenantId=@TenantId AND p.InvoiceId=v.Id AND p.CustomerId=@CustomerId AND p.IsPayer=1) THEN v.GrandTotal ELSE CAST(0 AS DECIMAL(18,2)) END PayerAmount,
  COALESCE((SELECT SUM(a.AmountAttributed) FROM dbo.RetailInvoiceItemAttributions a WHERE a.TenantId=@TenantId AND a.InvoiceId=v.Id AND a.CustomerId=@CustomerId),0) AttributedAmount
 FROM @Visible v ORDER BY v.InvoiceUtc DESC,v.Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.HouseholdPurchaseSummary_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@HouseholdId BIGINT
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @H TABLE(InvoiceId BIGINT PRIMARY KEY,InvoiceUtc DATETIME2(7));
 INSERT @H SELECT DISTINCT i.Id,i.InvoiceUtc FROM dbo.RetailInvoiceItemAttributions a JOIN dbo.HouseholdMembers m ON m.TenantId=a.TenantId AND m.CustomerId=a.CustomerId AND m.HouseholdId=@HouseholdId AND m.IsActive=1 AND m.IsVerified=1 JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId WHERE a.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
 SELECT @HouseholdId HouseholdId,(SELECT COUNT_BIG(*) FROM @H) InvoiceCount,
  COALESCE((SELECT SUM(a.AmountAttributed) FROM dbo.RetailInvoiceItemAttributions a JOIN dbo.HouseholdMembers m ON m.TenantId=a.TenantId AND m.CustomerId=a.CustomerId AND m.HouseholdId=@HouseholdId AND m.IsActive=1 AND m.IsVerified=1 JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId WHERE a.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))),0) VerifiedMemberAttributedSpend,(SELECT MAX(InvoiceUtc) FROM @H) LastPurchaseUtc;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailSalesSummary_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL
AS
BEGIN SET NOCOUNT ON;SELECT COALESCE(SUM(Subtotal),0) GrossSales,COALESCE(SUM(DiscountAmount),0) Discounts,COALESCE(SUM(TaxAmount),0) Tax,COALESCE(SUM(GrandTotal),0) NetSales,COALESCE(SUM(PaidAmount),0) PaidAmount,COALESCE(SUM(BalanceAmount),0) OutstandingAmount,COUNT_BIG(*) InvoiceCount FROM dbo.RetailInvoices i WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));END;
GO
CREATE OR ALTER PROCEDURE dbo.RetailSalesByProduct_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@Top INT=20
AS
BEGIN SET NOCOUNT ON;IF @Top<1 SET @Top=20;IF @Top>100 SET @Top=100;SELECT TOP(@Top) COALESCE(x.ProductId,0) Id,x.ProductCodeSnapshot Code,x.ProductNameSnapshot Name,SUM(x.LineTotal) NetSales,CONVERT(BIGINT,COUNT(DISTINCT i.Id)) InvoiceCount FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)) GROUP BY x.ProductId,x.ProductCodeSnapshot,x.ProductNameSnapshot ORDER BY NetSales DESC,Name;END;
GO
CREATE OR ALTER PROCEDURE dbo.RetailSalesByCategory_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@Top INT=20
AS
BEGIN SET NOCOUNT ON;IF @Top<1 SET @Top=20;IF @Top>100 SET @Top=100;SELECT TOP(@Top) COALESCE(x.CategoryId,0) Id,COALESCE(c.CategoryCode,N'UNCATEGORIZED') Code,COALESCE(x.CategoryNameSnapshot,N'Uncategorized') Name,SUM(x.LineTotal) NetSales,CONVERT(BIGINT,COUNT(DISTINCT i.Id)) InvoiceCount FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId LEFT JOIN dbo.ProductCategories c ON c.TenantId=x.TenantId AND c.Id=x.CategoryId WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)) GROUP BY x.CategoryId,c.CategoryCode,x.CategoryNameSnapshot ORDER BY NetSales DESC,Name;END;
GO
CREATE OR ALTER PROCEDURE dbo.RetailPaymentSummary_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL
AS
BEGIN SET NOCOUNT ON;SELECT PaymentMethod,SUM(Amount) Amount,COUNT_BIG(*) PaymentCount FROM dbo.RetailInvoicePayments p WHERE p.TenantId=@TenantId AND p.Status=2 AND (@StoreId IS NULL OR p.StoreId=@StoreId) AND (@FromUtc IS NULL OR p.PaymentUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.PaymentUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)) GROUP BY PaymentMethod ORDER BY PaymentMethod;END;
GO

DECLARE @P TABLE(Name NVARCHAR(150));
INSERT @P VALUES(N'Products.View'),(N'Products.Create'),(N'Products.Edit'),(N'Products.ManageStores'),(N'RetailInvoices.View'),(N'RetailInvoices.Create'),(N'RetailInvoices.Edit'),(N'RetailInvoices.Finalize'),(N'RetailInvoices.Cancel'),(N'RetailPayments.View'),(N'RetailPayments.Create'),(N'RetailSpendAttribution.View'),(N'RetailSpendAttribution.Manage'),(N'RetailReports.View');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME() FROM @P p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER') AND p.Name IN(N'Products.View',N'Products.Create',N'Products.Edit',N'Products.ManageStores',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailInvoices.Edit',N'RetailInvoices.Finalize',N'RetailInvoices.Cancel',N'RetailPayments.View',N'RetailPayments.Create',N'RetailSpendAttribution.View',N'RetailSpendAttribution.Manage',N'RetailReports.View') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'STOREMANAGER' AND p.Name IN(N'Products.View',N'Products.Create',N'Products.Edit',N'Products.ManageStores',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailInvoices.Edit',N'RetailInvoices.Finalize',N'RetailPayments.View',N'RetailPayments.Create',N'RetailSpendAttribution.View',N'RetailSpendAttribution.Manage',N'RetailReports.View') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'SALESSTAFF',N'CRMSTAFF') AND p.Name IN(N'Products.View',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailInvoices.Edit',N'RetailInvoices.Finalize',N'RetailPayments.View',N'RetailPayments.Create',N'RetailSpendAttribution.View') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.7.0') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.7.0',N'Phase 8 products, retail billing, participants, spend attribution and tenant retail reports',SYSUTCDATETIME(),SUSER_SNAME());
GO

-- ============================================================
-- PHASE 9 - PLATFORM BILLING
-- VERSION: V1.8.0
-- ============================================================
/*
 CustSearch AI — Phase 9 production database upgrade
 Version: V1.8.0
 Rules: SQL Server 2022, idempotent/repeat-safe, no EF migrations.

 Phase 9 is Platform Billing (tenant/shop owner pays CustSearch).
 It is intentionally separate from Phase 8 Retail Billing (shop-customer purchases).
 This upgrade extends the existing plan/subscription foundation and creates only
 PlatformInvoices / PlatformInvoiceItems / PlatformPayments plus read procedures.
*/
USE [CustSearch_AI];
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'CustSearch_AI'
    THROW 51800, 'Run Phase 9 against the CustSearch_AI database.', 1;
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.7.0')
    THROW 51801, 'Phase 9 requires validated V1.7.0 Phase 8 baseline.', 1;
IF OBJECT_ID(N'dbo.SubscriptionPlans',N'U') IS NULL OR OBJECT_ID(N'dbo.Tenants',N'U') IS NULL OR OBJECT_ID(N'dbo.TenantSubscriptions',N'U') IS NULL
    THROW 51802, 'Platform tenancy/subscription baseline is incomplete.', 1;
IF OBJECT_ID(N'dbo.Permissions',N'U') IS NULL OR OBJECT_ID(N'dbo.Roles',N'U') IS NULL OR OBJECT_ID(N'dbo.RolePermissions',N'U') IS NULL
    THROW 51803, 'Authorization baseline is incomplete.', 1;

BEGIN TRANSACTION;
BEGIN TRY
    /* =========================================================
       9A — Subscription plan catalog extension
       ========================================================= */
    IF COL_LENGTH('dbo.SubscriptionPlans','Description') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD Description NVARCHAR(1000) NOT NULL CONSTRAINT DF_SubscriptionPlans_Description DEFAULT(N'');
    IF COL_LENGTH('dbo.SubscriptionPlans','Currency') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD Currency CHAR(3) NOT NULL CONSTRAINT DF_SubscriptionPlans_Currency DEFAULT('USD');
    IF COL_LENGTH('dbo.SubscriptionPlans','TrialDays') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD TrialDays INT NOT NULL CONSTRAINT DF_SubscriptionPlans_TrialDays DEFAULT(0);
    IF COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL
    BEGIN
        ALTER TABLE dbo.SubscriptionPlans ADD MaxStaff INT NULL;
        EXEC sys.sp_executesql N'UPDATE dbo.SubscriptionPlans SET MaxStaff=MaxUsers WHERE MaxStaff IS NULL;';
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans ALTER COLUMN MaxStaff INT NOT NULL;';
    END;
    IF COL_LENGTH('dbo.SubscriptionPlans','FeatureLimitsJson') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD FeatureLimitsJson NVARCHAR(4000) NULL;
    IF COL_LENGTH('dbo.SubscriptionPlans','DisplayOrder') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD DisplayOrder INT NOT NULL CONSTRAINT DF_SubscriptionPlans_DisplayOrder DEFAULT(0);

    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_Limits',N'C') IS NOT NULL
        ALTER TABLE dbo.SubscriptionPlans DROP CONSTRAINT CK_SubscriptionPlans_Limits;
    EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_Limits CHECK(MaxStores>0 AND MaxUsers>0 AND MaxStaff>0 AND MaxCameras>0 AND (MaxMonthlyRecognitions IS NULL OR MaxMonthlyRecognitions>0) AND (MaxMonthlyApiCalls IS NULL OR MaxMonthlyApiCalls>0));';
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_TrialDisplay',N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_TrialDisplay CHECK(TrialDays>=0 AND DisplayOrder>=0);';
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_FeatureLimitsJson',N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_FeatureLimitsJson CHECK(FeatureLimitsJson IS NULL OR ISJSON(FeatureLimitsJson)=1);';
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND name=N'IX_SubscriptionPlans_Display')
        EXEC sys.sp_executesql N'CREATE INDEX IX_SubscriptionPlans_Display ON dbo.SubscriptionPlans(IsActive,DisplayOrder,PlanName);';

    /* =========================================================
       9B — Effective tenant quotas and subscription lifecycle
       ========================================================= */
    IF COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL
    BEGIN
        ALTER TABLE dbo.Tenants ADD MaxStaff INT NULL;
        EXEC sys.sp_executesql N'UPDATE dbo.Tenants SET MaxStaff=MaxUsers WHERE MaxStaff IS NULL;';
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Tenants ALTER COLUMN MaxStaff INT NOT NULL;';
    END;
    IF OBJECT_ID(N'dbo.CK_Tenants_Quotas',N'C') IS NOT NULL
        ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_Quotas;
    EXEC sys.sp_executesql N'ALTER TABLE dbo.Tenants WITH CHECK ADD CONSTRAINT CK_Tenants_Quotas CHECK(MaxStores>0 AND MaxUsers>0 AND MaxStaff>0 AND MaxCameras>0);';

    IF COL_LENGTH('dbo.TenantSubscriptions','TrialEndUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD TrialEndUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodStartUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CurrentPeriodStartUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CurrentPeriodEndUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CancelAtPeriodEnd') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CancelAtPeriodEnd BIT NOT NULL CONSTRAINT DF_TenantSubscriptions_CancelAtPeriodEnd DEFAULT(0);
    IF COL_LENGTH('dbo.TenantSubscriptions','CancelledUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CancelledUtc DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'UPDATE dbo.TenantSubscriptions SET CurrentPeriodStartUtc=COALESCE(CurrentPeriodStartUtc,StartsUtc),CurrentPeriodEndUtc=COALESCE(CurrentPeriodEndUtc,EndsUtc),CancelAtPeriodEnd=CASE WHEN AutoRenew=0 THEN 1 ELSE CancelAtPeriodEnd END;';
    IF OBJECT_ID(N'dbo.CK_TenantSubscriptions_CurrentPeriod',N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.TenantSubscriptions WITH CHECK ADD CONSTRAINT CK_TenantSubscriptions_CurrentPeriod CHECK(CurrentPeriodEndUtc IS NULL OR (CurrentPeriodStartUtc IS NOT NULL AND CurrentPeriodEndUtc>CurrentPeriodStartUtc));';

    /* =========================================================
       9C — Platform invoices. Never use RetailInvoices here.
       ========================================================= */
    IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlatformInvoices(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformInvoices PRIMARY KEY,
            TenantId BIGINT NOT NULL,
            TenantSubscriptionId BIGINT NOT NULL,
            InvoiceNumber NVARCHAR(60) NOT NULL,
            Currency CHAR(3) NOT NULL,
            InvoiceUtc DATETIME2(7) NOT NULL,
            DueUtc DATETIME2(7) NOT NULL,
            Status TINYINT NOT NULL,
            Subtotal DECIMAL(19,4) NOT NULL,
            DiscountAmount DECIMAL(19,4) NOT NULL,
            TaxAmount DECIMAL(19,4) NOT NULL,
            Total DECIMAL(19,4) NOT NULL,
            PaidAmount DECIMAL(19,4) NOT NULL,
            CreatedUtc DATETIME2(7) NOT NULL,
            UpdatedUtc DATETIME2(7) NOT NULL,
            RowVersion BINARY(16) NOT NULL,
            CONSTRAINT FK_PlatformInvoices_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
            CONSTRAINT FK_PlatformInvoices_TenantSubscriptions FOREIGN KEY(TenantSubscriptionId) REFERENCES dbo.TenantSubscriptions(Id),
            CONSTRAINT CK_PlatformInvoices_Status CHECK(Status BETWEEN 1 AND 5),
            CONSTRAINT CK_PlatformInvoices_Amounts CHECK(Subtotal>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND Total>=0 AND PaidAmount>=0 AND PaidAmount<=Total),
            CONSTRAINT CK_PlatformInvoices_Due CHECK(DueUtc>=InvoiceUtc)
        );
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND name=N'UX_PlatformInvoices_Tenant_Number')
        CREATE UNIQUE INDEX UX_PlatformInvoices_Tenant_Number ON dbo.PlatformInvoices(TenantId,InvoiceNumber);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND name=N'IX_PlatformInvoices_Tenant_Status_Date')
        CREATE INDEX IX_PlatformInvoices_Tenant_Status_Date ON dbo.PlatformInvoices(TenantId,Status,InvoiceUtc DESC);

    IF OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlatformInvoiceItems(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformInvoiceItems PRIMARY KEY,
            TenantId BIGINT NOT NULL,
            PlatformInvoiceId BIGINT NOT NULL,
            SubscriptionPlanId BIGINT NULL,
            PlanName NVARCHAR(150) NOT NULL,
            Description NVARCHAR(500) NULL,
            Quantity DECIMAL(19,4) NOT NULL,
            Rate DECIMAL(19,4) NOT NULL,
            DiscountAmount DECIMAL(19,4) NOT NULL,
            TaxAmount DECIMAL(19,4) NOT NULL,
            Subtotal DECIMAL(19,4) NOT NULL,
            Total DECIMAL(19,4) NOT NULL,
            CreatedUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_PlatformInvoiceItems_PlatformInvoices FOREIGN KEY(PlatformInvoiceId) REFERENCES dbo.PlatformInvoices(Id) ON DELETE CASCADE,
            CONSTRAINT CK_PlatformInvoiceItems_Amounts CHECK(Quantity>0 AND Rate>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND Subtotal>=0 AND Total>=0)
        );
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformInvoiceItems') AND name=N'IX_PlatformInvoiceItems_Tenant_Invoice')
        CREATE INDEX IX_PlatformInvoiceItems_Tenant_Invoice ON dbo.PlatformInvoiceItems(TenantId,PlatformInvoiceId);

    /* =========================================================
       9D — Provider-neutral idempotent platform payments
       ========================================================= */
    IF OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlatformPayments(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformPayments PRIMARY KEY,
            TenantId BIGINT NOT NULL,
            PlatformInvoiceId BIGINT NOT NULL,
            PaymentMethod NVARCHAR(50) NOT NULL,
            Amount DECIMAL(19,4) NOT NULL,
            Currency CHAR(3) NOT NULL,
            GatewayReference NVARCHAR(150) NULL,
            TransactionReference NVARCHAR(150) NOT NULL,
            PaymentUtc DATETIME2(7) NOT NULL,
            Status TINYINT NOT NULL,
            CreatedUtc DATETIME2(7) NOT NULL,
            UpdatedUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_PlatformPayments_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
            CONSTRAINT FK_PlatformPayments_PlatformInvoices FOREIGN KEY(PlatformInvoiceId) REFERENCES dbo.PlatformInvoices(Id),
            CONSTRAINT CK_PlatformPayments_Status CHECK(Status BETWEEN 1 AND 4),
            CONSTRAINT CK_PlatformPayments_Amount CHECK(Amount>0)
        );
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformPayments') AND name=N'UX_PlatformPayments_Tenant_TransactionReference')
        CREATE UNIQUE INDEX UX_PlatformPayments_Tenant_TransactionReference ON dbo.PlatformPayments(TenantId,TransactionReference);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformPayments') AND name=N'IX_PlatformPayments_Tenant_Invoice_Status')
        CREATE INDEX IX_PlatformPayments_Tenant_Invoice_Status ON dbo.PlatformPayments(TenantId,PlatformInvoiceId,Status);

    /* =========================================================
       9F — Platform-admin vs tenant read-only permission split.
       Repair names left by an earlier failed Phase 9 attempt if present.
       ========================================================= */
    IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'PlatformBilling.Subscriptions.View')
       AND NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'TenantPlatformBilling.Subscriptions.View')
        UPDATE dbo.Permissions SET Name=N'TenantPlatformBilling.Subscriptions.View',Description=N'View this tenant CustSearch platform subscription.' WHERE Scope=2 AND Name=N'PlatformBilling.Subscriptions.View';
    IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'PlatformBilling.Invoices.View')
       AND NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'TenantPlatformBilling.Invoices.View')
        UPDATE dbo.Permissions SET Name=N'TenantPlatformBilling.Invoices.View',Description=N'View this tenant CustSearch platform invoices.' WHERE Scope=2 AND Name=N'PlatformBilling.Invoices.View';
    IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'PlatformBilling.Payments.View')
       AND NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'TenantPlatformBilling.Payments.View')
        UPDATE dbo.Permissions SET Name=N'TenantPlatformBilling.Payments.View',Description=N'View this tenant CustSearch platform payments.' WHERE Scope=2 AND Name=N'PlatformBilling.Payments.View';

    DECLARE @PlatformPermissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
    INSERT INTO @PlatformPermissions VALUES
      (N'PlatformBilling.Plans.View',N'View CustSearch subscription plans.'),
      (N'PlatformBilling.Plans.Manage',N'Manage CustSearch subscription plans.'),
      (N'PlatformBilling.Subscriptions.View',N'View tenant platform subscriptions.'),
      (N'PlatformBilling.Subscriptions.Manage',N'Manage tenant platform subscriptions, invoices and payment callbacks.'),
      (N'PlatformBilling.Invoices.View',N'View CustSearch platform invoices.'),
      (N'PlatformBilling.Payments.View',N'View CustSearch platform payments.');
    INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
    SELECT 1,p.Name,p.Description,1,SYSUTCDATETIME() FROM @PlatformPermissions p
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);

    DECLARE @TenantPermissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
    INSERT INTO @TenantPermissions VALUES
      (N'TenantPlatformBilling.Subscriptions.View',N'View this tenant CustSearch platform subscription.'),
      (N'TenantPlatformBilling.Invoices.View',N'View this tenant CustSearch platform invoices.'),
      (N'TenantPlatformBilling.Payments.View',N'View this tenant CustSearch platform payments.');
    INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
    SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @TenantPermissions p
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);

    INSERT INTO dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id
    FROM dbo.Roles r
    JOIN dbo.Permissions p ON p.Scope=1 AND p.IsActive=1 AND p.Name LIKE N'PlatformBilling.%'
    WHERE r.Scope=1 AND r.IsActive=1 AND (
      r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMBILLINGADMIN') OR
      (r.NormalizedName IN(N'PLATFORMOPERATIONSADMIN',N'PLATFORMAUDITOR') AND p.Name IN(N'PlatformBilling.Plans.View',N'PlatformBilling.Subscriptions.View',N'PlatformBilling.Invoices.View',N'PlatformBilling.Payments.View')))
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    INSERT INTO dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id
    FROM dbo.Roles r
    JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
      AND p.Name IN(N'TenantPlatformBilling.Subscriptions.View',N'TenantPlatformBilling.Invoices.View',N'TenantPlatformBilling.Payments.View')
    WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'AUDITOR')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    /* =========================================================
       9G — Read-only stored procedures for platform/tenant views.
       Tenant API callers pass TenantId from trusted server context.
       ========================================================= */
    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Plan_List
    @IncludeInactive BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,PlanCode,PlanName AS [Name],Description,MonthlyPrice,AnnualPrice,Currency,TrialDays,MaxStores,MaxUsers,MaxStaff,MaxCameras,MaxMonthlyRecognitions,MaxMonthlyApiCalls,FeatureLimitsJson,IsActive,DisplayOrder,CreatedUtc,UpdatedUtc
    FROM dbo.SubscriptionPlans
    WHERE @IncludeInactive=1 OR IsActive=1
    ORDER BY DisplayOrder,PlanName,Id;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Subscription_List
    @TenantId BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ts.Id,ts.TenantId,t.TenantCode,t.DisplayName AS TenantName,ts.SubscriptionPlanId AS PlanId,sp.PlanCode,sp.PlanName,ts.BillingCycle,ts.Status,ts.StartsUtc AS StartUtc,ts.TrialEndUtc,ts.CurrentPeriodStartUtc,ts.CurrentPeriodEndUtc,ts.CancelAtPeriodEnd,ts.CancelledUtc,t.MaxStores,t.MaxUsers,t.MaxStaff,t.MaxCameras
    FROM dbo.TenantSubscriptions ts
    INNER JOIN dbo.Tenants t ON t.Id=ts.TenantId
    INNER JOIN dbo.SubscriptionPlans sp ON sp.Id=ts.SubscriptionPlanId
    WHERE @TenantId IS NULL OR ts.TenantId=@TenantId
    ORDER BY ts.StartsUtc DESC,ts.Id DESC;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Invoice_List
    @TenantId BIGINT=NULL,
    @PageNumber INT=1,
    @PageSize INT=50
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber<1 SET @PageNumber=1;
    IF @PageSize<1 SET @PageSize=50;
    IF @PageSize>200 SET @PageSize=200;
    SELECT i.Id,i.TenantId,i.TenantSubscriptionId,i.InvoiceNumber,i.Currency,i.InvoiceUtc,i.DueUtc,i.Status,i.Subtotal,i.DiscountAmount,i.TaxAmount,i.Total,i.PaidAmount,i.CreatedUtc,i.UpdatedUtc,COUNT_BIG(1) OVER() AS TotalCount
    FROM dbo.PlatformInvoices i
    WHERE @TenantId IS NULL OR i.TenantId=@TenantId
    ORDER BY i.InvoiceUtc DESC,i.Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Invoice_Get
    @PlatformInvoiceId BIGINT,
    @TenantId BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.Id,i.TenantId,i.TenantSubscriptionId,i.InvoiceNumber,i.Currency,i.InvoiceUtc,i.DueUtc,i.Status,i.Subtotal,i.DiscountAmount,i.TaxAmount,i.Total,i.PaidAmount,i.CreatedUtc,i.UpdatedUtc
    FROM dbo.PlatformInvoices i
    WHERE i.Id=@PlatformInvoiceId AND (@TenantId IS NULL OR i.TenantId=@TenantId);
    SELECT x.Id,x.TenantId,x.PlatformInvoiceId,x.SubscriptionPlanId,x.PlanName,x.Description,x.Quantity,x.Rate,x.DiscountAmount,x.TaxAmount,x.Subtotal,x.Total,x.CreatedUtc
    FROM dbo.PlatformInvoiceItems x
    INNER JOIN dbo.PlatformInvoices i ON i.Id=x.PlatformInvoiceId
    WHERE x.PlatformInvoiceId=@PlatformInvoiceId AND (@TenantId IS NULL OR i.TenantId=@TenantId)
    ORDER BY x.Id;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Payment_List
    @TenantId BIGINT=NULL,
    @PlatformInvoiceId BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id,p.TenantId,p.PlatformInvoiceId,p.PaymentMethod,p.Amount,p.Currency,p.GatewayReference,p.TransactionReference,p.PaymentUtc,p.Status,p.CreatedUtc,p.UpdatedUtc
    FROM dbo.PlatformPayments p
    WHERE (@TenantId IS NULL OR p.TenantId=@TenantId) AND (@PlatformInvoiceId IS NULL OR p.PlatformInvoiceId=@PlatformInvoiceId)
    ORDER BY p.PaymentUtc DESC,p.Id DESC;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.TenantPlatformBilling_Summary_Get
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF @TenantId<=0 THROW 51870,''TenantId must be positive.'',1;
    SELECT TOP(1) t.Id AS TenantId,t.TenantCode,t.DisplayName AS TenantName,ts.Id AS TenantSubscriptionId,ts.SubscriptionPlanId AS PlanId,sp.PlanCode,sp.PlanName,ts.BillingCycle,ts.Status AS SubscriptionStatus,ts.StartsUtc AS StartUtc,ts.TrialEndUtc,ts.CurrentPeriodStartUtc,ts.CurrentPeriodEndUtc AS RenewalUtc,ts.CancelAtPeriodEnd,ts.CancelledUtc,t.MaxStores,t.MaxUsers,t.MaxStaff,t.MaxCameras,
      (SELECT COUNT_BIG(1) FROM dbo.PlatformInvoices pi WHERE pi.TenantId=t.Id) AS InvoiceCount,
      (SELECT TOP(1) pp.Status FROM dbo.PlatformPayments pp WHERE pp.TenantId=t.Id ORDER BY pp.PaymentUtc DESC,pp.Id DESC) AS LatestPaymentStatus
    FROM dbo.Tenants t
    LEFT JOIN dbo.TenantSubscriptions ts ON ts.Id=(SELECT TOP(1) ts2.Id FROM dbo.TenantSubscriptions ts2 WHERE ts2.TenantId=t.Id ORDER BY ts2.StartsUtc DESC,ts2.Id DESC)
    LEFT JOIN dbo.SubscriptionPlans sp ON sp.Id=ts.SubscriptionPlanId
    WHERE t.Id=@TenantId;
END;';

    /* Version row is written only after all Phase 9 objects compile. */
    IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')
        INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
        VALUES(N'V1.8.0',N'Phase 9 platform plans, authoritative tenant subscriptions, separate platform invoices/payments and billing read procedures',SYSUTCDATETIME(),SUSER_SNAME());

    IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL
        THROW 51880,'Phase 9 platform billing tables are missing.',1;
    IF OBJECT_ID(N'dbo.PlatformBilling_Plan_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Subscription_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Payment_List',N'P') IS NULL OR OBJECT_ID(N'dbo.TenantPlatformBilling_Summary_Get',N'P') IS NULL
        THROW 51881,'Phase 9 stored procedures are missing.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Subscriptions.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Subscriptions.View')
        THROW 51882,'Phase 9 platform/tenant permission separation is missing.',1;
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND referenced_object_id=OBJECT_ID(N'dbo.RetailInvoices'))
        THROW 51883,'PlatformInvoices must never reference RetailInvoices.',1;
    IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')<>1
        THROW 51884,'V1.8.0 must exist exactly once.',1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')<>1 THROW 51890,'V1.8.0 version row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL THROW 51891,'Phase 9 platform billing tables are missing.',1;
IF COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL OR COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL OR COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL THROW 51892,'Phase 9 subscription/quota columns are missing.',1;
IF OBJECT_ID(N'dbo.PlatformBilling_Plan_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Subscription_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Payment_List',N'P') IS NULL OR OBJECT_ID(N'dbo.TenantPlatformBilling_Summary_Get',N'P') IS NULL THROW 51893,'Phase 9 platform billing procedures are missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Subscriptions.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Subscriptions.View') THROW 51894,'Phase 9 platform/tenant billing permission separation is missing.',1;

-- ============================================================
-- PHASE 10 - PREFERENCES & STAFF VOICE TAGGING
-- VERSION: V1.9.0
-- ============================================================
/*
 CustSearch AI — Phase 10 production database upgrade
 Version: V1.9.0
 Scope: factual customer preference signals, derived scores, verified-household preference tags,
        store/tenant category aliases, versioned recalculation weights and confirmation-controlled staff voice sessions.
 Rules: SQL Server 2022, repeat-safe, no EF migrations, UTC timestamps.
 Privacy: VisitParty/co-visit is NEVER used to create Household preference truth.
 Voice: Phase 5 StoreVoiceCommandSettings + trigger aliases remain the trigger master; voice category text resolves only to existing ProductCategories/ProductCategoryAliases.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')
    THROW 52000,'Phase 9 V1.8.0 must be installed before Phase 10.',1;
GO

-- ============================================================
-- 10A — FACTUAL CUSTOMER PREFERENCE SIGNALS
-- ============================================================
IF OBJECT_ID(N'dbo.CustomerPreferenceSignals',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerPreferenceSignals
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPreferenceSignals PRIMARY KEY,
        TenantId BIGINT NOT NULL, StoreId BIGINT NULL, CustomerId BIGINT NOT NULL,
        PreferenceType TINYINT NOT NULL, ReferenceId BIGINT NULL, Value NVARCHAR(200) NULL,
        SignalScore DECIMAL(6,2) NULL, Source TINYINT NOT NULL, Confidence DECIMAL(6,2) NULL,
        FirstObservedUtc DATETIME2(7) NOT NULL, LastObservedUtc DATETIME2(7) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CustomerPreferenceSignals_IsActive DEFAULT(1),
        CreatedByUserId BIGINT NULL, Reason NVARCHAR(500) NULL,
        CreatedUtc DATETIME2(7) NOT NULL, UpdatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_CustomerPreferenceSignals_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CustomerPreferenceSignals_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_CustomerPreferenceSignals_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_CustomerPreferenceSignals_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_CustomerPreferenceSignals_Type CHECK(PreferenceType BETWEEN 1 AND 5),
        CONSTRAINT CK_CustomerPreferenceSignals_Source CHECK(Source BETWEEN 1 AND 4),
        CONSTRAINT CK_CustomerPreferenceSignals_Identity CHECK(ReferenceId IS NOT NULL OR NULLIF(LTRIM(RTRIM(Value)),N'') IS NOT NULL),
        CONSTRAINT CK_CustomerPreferenceSignals_Score CHECK(SignalScore IS NULL OR SignalScore BETWEEN 0 AND 100),
        CONSTRAINT CK_CustomerPreferenceSignals_Confidence CHECK(Confidence IS NULL OR Confidence BETWEEN 0 AND 100),
        CONSTRAINT CK_CustomerPreferenceSignals_Period CHECK(LastObservedUtc>=FirstObservedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceSignals') AND name=N'UX_CustomerPreferenceSignals_Tenant_Id') CREATE UNIQUE INDEX UX_CustomerPreferenceSignals_Tenant_Id ON dbo.CustomerPreferenceSignals(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceSignals') AND name=N'IX_CustomerPreferenceSignals_Tenant_Customer_Active') CREATE INDEX IX_CustomerPreferenceSignals_Tenant_Customer_Active ON dbo.CustomerPreferenceSignals(TenantId,CustomerId,IsActive,LastObservedUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceSignals') AND name=N'IX_CustomerPreferenceSignals_Tenant_Store_Customer') CREATE INDEX IX_CustomerPreferenceSignals_Tenant_Store_Customer ON dbo.CustomerPreferenceSignals(TenantId,StoreId,CustomerId);
GO

-- ============================================================
-- 10B — EXPLICIT HOUSEHOLD TAGS; only verified HouseholdMembers are aggregated by reads.
-- ============================================================
IF OBJECT_ID(N'dbo.HouseholdPreferenceTags',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HouseholdPreferenceTags
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HouseholdPreferenceTags PRIMARY KEY,
        TenantId BIGINT NOT NULL, HouseholdId BIGINT NOT NULL, PreferenceType TINYINT NOT NULL,
        ReferenceId BIGINT NULL, Value NVARCHAR(200) NOT NULL, Source TINYINT NOT NULL,
        CreatedByUserId BIGINT NOT NULL, Reason NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_HouseholdPreferenceTags_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL, UpdatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_HouseholdPreferenceTags_Households_TenantHousehold FOREIGN KEY(TenantId,HouseholdId) REFERENCES dbo.Households(TenantId,Id),
        CONSTRAINT FK_HouseholdPreferenceTags_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_HouseholdPreferenceTags_Type CHECK(PreferenceType BETWEEN 1 AND 5),
        CONSTRAINT CK_HouseholdPreferenceTags_Source CHECK(Source BETWEEN 1 AND 3),
        CONSTRAINT CK_HouseholdPreferenceTags_Value CHECK(NULLIF(LTRIM(RTRIM(Value)),N'') IS NOT NULL)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HouseholdPreferenceTags') AND name=N'IX_HouseholdPreferenceTags_Tenant_Household_Active') CREATE INDEX IX_HouseholdPreferenceTags_Tenant_Household_Active ON dbo.HouseholdPreferenceTags(TenantId,HouseholdId,IsActive,CreatedUtc DESC);
GO

-- ============================================================
-- 10C/10D — CATEGORY VOICE ALIASES. They map speech to existing ProductCategories only.
-- StoreId NULL means tenant-wide alias; duplicate phrases may map to multiple categories so ambiguity can be confirmed safely.
-- ============================================================
IF OBJECT_ID(N'dbo.ProductCategoryAliases',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategoryAliases
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductCategoryAliases PRIMARY KEY,
        TenantId BIGINT NOT NULL, StoreId BIGINT NULL, ProductCategoryId BIGINT NOT NULL,
        AliasText NVARCHAR(150) NOT NULL, NormalizedAliasText NVARCHAR(150) NOT NULL,
        LanguageCode NVARCHAR(20) NOT NULL, IsActive BIT NOT NULL CONSTRAINT DF_ProductCategoryAliases_IsActive DEFAULT(1),
        CreatedByUserId BIGINT NOT NULL, CreatedUtc DATETIME2(7) NOT NULL, UpdatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_ProductCategoryAliases_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_ProductCategoryAliases_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_ProductCategoryAliases_Categories_TenantCategory FOREIGN KEY(TenantId,ProductCategoryId) REFERENCES dbo.ProductCategories(TenantId,Id),
        CONSTRAINT FK_ProductCategoryAliases_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ProductCategoryAliases_Text CHECK(NULLIF(LTRIM(RTRIM(AliasText)),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(NormalizedAliasText)),N'') IS NOT NULL)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'UX_ProductCategoryAliases_Scope_Phrase_Category') CREATE UNIQUE INDEX UX_ProductCategoryAliases_Scope_Phrase_Category ON dbo.ProductCategoryAliases(TenantId,StoreId,NormalizedAliasText,ProductCategoryId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'IX_ProductCategoryAliases_Tenant_Category_Active') CREATE INDEX IX_ProductCategoryAliases_Tenant_Category_Active ON dbo.ProductCategoryAliases(TenantId,ProductCategoryId,IsActive);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'IX_ProductCategoryAliases_Tenant_Store_Phrase') CREATE INDEX IX_ProductCategoryAliases_Tenant_Store_Phrase ON dbo.ProductCategoryAliases(TenantId,StoreId,NormalizedAliasText,IsActive);
GO

-- ============================================================
-- 10D — PHASE 5 VOICE MASTER RUNTIME EXTENSION
-- ============================================================
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandSettings') AND name=N'UX_StoreVoiceCommandSettings_Tenant_Store') CREATE UNIQUE INDEX UX_StoreVoiceCommandSettings_Tenant_Store ON dbo.StoreVoiceCommandSettings(TenantId,StoreId);
GO
IF OBJECT_ID(N'dbo.StoreVoiceCommandRuntimeSettings',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoreVoiceCommandRuntimeSettings
    (
        StoreId BIGINT NOT NULL CONSTRAINT PK_StoreVoiceCommandRuntimeSettings PRIMARY KEY,
        TenantId BIGINT NOT NULL, LanguageCode NVARCHAR(20) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Language DEFAULT(N'en-IN'),
        RequireConfirmation BIT NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Confirmation DEFAULT(1),
        ListeningTimeoutSeconds INT NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Timeout DEFAULT(30),
        MinimumRecognitionConfidence DECIMAL(6,2) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Confidence DEFAULT(70),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Created DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Updated DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StoreVoiceRuntime_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_StoreVoiceRuntime_Timeout CHECK(ListeningTimeoutSeconds BETWEEN 3 AND 120),
        CONSTRAINT CK_StoreVoiceRuntime_Confidence CHECK(MinimumRecognitionConfidence BETWEEN 0 AND 100)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandRuntimeSettings') AND name=N'UX_StoreVoiceRuntime_Tenant_Store') CREATE UNIQUE INDEX UX_StoreVoiceRuntime_Tenant_Store ON dbo.StoreVoiceCommandRuntimeSettings(TenantId,StoreId);
GO

-- ============================================================
-- 10E — CONFIRMATION-CONTROLLED VOICE COMMAND SESSIONS
-- ============================================================
IF OBJECT_ID(N'dbo.VoiceCommandSessions',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VoiceCommandSessions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoiceCommandSessions PRIMARY KEY,
        TenantId BIGINT NOT NULL, StoreId BIGINT NOT NULL, StaffUserId BIGINT NOT NULL, CustomerId BIGINT NOT NULL,
        MatchedTrigger NVARCHAR(100) NOT NULL, RecognizedText NVARCHAR(250) NULL,
        RecognitionConfidence DECIMAL(6,2) NULL, ProposedPreferenceType TINYINT NULL,
        ProposedReferenceId BIGINT NULL, ProposedValue NVARCHAR(200) NULL,
        ConfirmationRequired BIT NOT NULL, Status TINYINT NOT NULL,
        ExpiresUtc DATETIME2(7) NOT NULL, ResolvedUtc DATETIME2(7) NULL,
        CreatedUtc DATETIME2(7) NOT NULL, UpdatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_VoiceCommandSessions_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_VoiceCommandSessions_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_VoiceCommandSessions_Users FOREIGN KEY(StaffUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_VoiceCommandSessions_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_VoiceCommandSessions_Confidence CHECK(RecognitionConfidence IS NULL OR RecognitionConfidence BETWEEN 0 AND 100),
        CONSTRAINT CK_VoiceCommandSessions_Type CHECK(ProposedPreferenceType IS NULL OR ProposedPreferenceType BETWEEN 1 AND 5),
        CONSTRAINT CK_VoiceCommandSessions_Period CHECK(ExpiresUtc>CreatedUtc AND (ResolvedUtc IS NULL OR ResolvedUtc>=CreatedUtc))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VoiceCommandSessions') AND name=N'IX_VoiceCommandSessions_Tenant_Store_Customer_Status') CREATE INDEX IX_VoiceCommandSessions_Tenant_Store_Customer_Status ON dbo.VoiceCommandSessions(TenantId,StoreId,CustomerId,Status,CreatedUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VoiceCommandSessions') AND name=N'IX_VoiceCommandSessions_Tenant_Staff_Created') CREATE INDEX IX_VoiceCommandSessions_Tenant_Staff_Created ON dbo.VoiceCommandSessions(TenantId,StaffUserId,CreatedUtc DESC);
GO

-- ============================================================
-- 10F — VERSIONED RECALCULATION WEIGHTS + DERIVED SCORES
-- ============================================================
IF OBJECT_ID(N'dbo.PreferenceWeightVersions',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PreferenceWeightVersions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreferenceWeightVersions PRIMARY KEY,
        TenantId BIGINT NOT NULL, VersionCode NVARCHAR(50) NOT NULL,
        ManualStaffWeight DECIMAL(6,3) NOT NULL, PurchaseWeight DECIMAL(6,3) NOT NULL,
        CategoryInteractionWeight DECIMAL(6,3) NOT NULL, VoiceConfirmedWeight DECIMAL(6,3) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PreferenceWeightVersions_IsActive DEFAULT(1),
        CreatedByUserId BIGINT NOT NULL, CreatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_PreferenceWeightVersions_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_PreferenceWeightVersions_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_PreferenceWeightVersions_Weights CHECK(ManualStaffWeight BETWEEN 0 AND 10 AND PurchaseWeight BETWEEN 0 AND 10 AND CategoryInteractionWeight BETWEEN 0 AND 10 AND VoiceConfirmedWeight BETWEEN 0 AND 10)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PreferenceWeightVersions') AND name=N'UX_PreferenceWeightVersions_Tenant_Code') CREATE UNIQUE INDEX UX_PreferenceWeightVersions_Tenant_Code ON dbo.PreferenceWeightVersions(TenantId,VersionCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PreferenceWeightVersions') AND name=N'UX_PreferenceWeightVersions_Tenant_Id') CREATE UNIQUE INDEX UX_PreferenceWeightVersions_Tenant_Id ON dbo.PreferenceWeightVersions(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PreferenceWeightVersions') AND name=N'UX_PreferenceWeightVersions_OneActive') CREATE UNIQUE INDEX UX_PreferenceWeightVersions_OneActive ON dbo.PreferenceWeightVersions(TenantId) WHERE IsActive=1;
GO
IF OBJECT_ID(N'dbo.CustomerPreferenceScores',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerPreferenceScores
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPreferenceScores PRIMARY KEY,
        TenantId BIGINT NOT NULL, CustomerId BIGINT NOT NULL, PreferenceType TINYINT NOT NULL,
        ReferenceId BIGINT NULL, Value NVARCHAR(200) NULL, Score DECIMAL(6,2) NOT NULL,
        WeightVersionId BIGINT NOT NULL, CalculatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_CustomerPreferenceScores_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_CustomerPreferenceScores_Weight_TenantVersion FOREIGN KEY(TenantId,WeightVersionId) REFERENCES dbo.PreferenceWeightVersions(TenantId,Id),
        CONSTRAINT CK_CustomerPreferenceScores_Type CHECK(PreferenceType BETWEEN 1 AND 5),
        CONSTRAINT CK_CustomerPreferenceScores_Identity CHECK(ReferenceId IS NOT NULL OR NULLIF(LTRIM(RTRIM(Value)),N'') IS NOT NULL),
        CONSTRAINT CK_CustomerPreferenceScores_Value CHECK(Score BETWEEN 0 AND 100)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceScores') AND name=N'IX_CustomerPreferenceScores_Tenant_Customer_Score') CREATE INDEX IX_CustomerPreferenceScores_Tenant_Customer_Score ON dbo.CustomerPreferenceScores(TenantId,CustomerId,Score DESC);
GO

-- ============================================================
-- 10G — STABLE PERMISSIONS
-- ============================================================
DECLARE @Phase10Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
INSERT INTO @Phase10Permissions VALUES
(N'Preferences.View',N'View customer and verified-household preferences.'),
(N'Preferences.Manage',N'Manage explicit preferences and run recalculation.'),
(N'VoiceCommands.Use',N'Use store-configured voice commands.'),
(N'VoiceCommands.View',N'View store voice-command settings.'),
(N'VoiceCommands.Configure',N'Configure store voice triggers, aliases and runtime controls.'),
(N'VoiceCommands.Audit',N'View preference and voice-command audit history.');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Phase10Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
GO

-- ============================================================
-- READ PROCEDURES — TenantId/store scope is applied before return/paging.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.CustomerPreference_Get @TenantId BIGINT,@CustomerId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.Customers c WHERE c.TenantId=@TenantId AND c.Id=@CustomerId AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=c.Id))) RETURN;
 SELECT Id,StoreId,CustomerId,PreferenceType,ReferenceId,Value,SignalScore,Source,Confidence,FirstObservedUtc,LastObservedUtc,IsActive,Reason FROM dbo.CustomerPreferenceSignals WHERE TenantId=@TenantId AND CustomerId=@CustomerId ORDER BY LastObservedUtc DESC,Id DESC;
 SELECT Id,CustomerId,PreferenceType,ReferenceId,Value,Score,WeightVersionId,CalculatedUtc FROM dbo.CustomerPreferenceScores WHERE TenantId=@TenantId AND CustomerId=@CustomerId ORDER BY Score DESC,Id DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.HouseholdPreference_Get @TenantId BIGINT,@HouseholdId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.Households h WHERE h.TenantId=@TenantId AND h.Id=@HouseholdId AND h.IsActive=1) RETURN;
 IF @AllowedStoreIdsCsv IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.HouseholdMembers hm JOIN dbo.CustomerStoreAssignments csa ON csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1 AND hm.IsVerified=1) RETURN;
 SELECT hm.CustomerId,c.FirstName,c.LastName,hm.RelationshipType,hm.RelationshipSource,hm.VerifiedUtc FROM dbo.HouseholdMembers hm JOIN dbo.Customers c ON c.TenantId=hm.TenantId AND c.Id=hm.CustomerId WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1 AND hm.IsVerified=1 AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId));
 SELECT s.CustomerId,s.PreferenceType,s.ReferenceId,s.Value,s.Score,s.WeightVersionId,s.CalculatedUtc FROM dbo.CustomerPreferenceScores s JOIN dbo.HouseholdMembers hm ON hm.TenantId=s.TenantId AND hm.CustomerId=s.CustomerId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1 AND hm.IsVerified=1 WHERE s.TenantId=@TenantId AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') x ON TRY_CONVERT(BIGINT,x.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=s.CustomerId));
 SELECT Id,PreferenceType,ReferenceId,Value,Source,Reason,CreatedUtc FROM dbo.HouseholdPreferenceTags WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND IsActive=1 ORDER BY CreatedUtc DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.ProductCategoryAlias_Search @TenantId BIGINT,@StoreId BIGINT=NULL,@ProductCategoryId BIGINT=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SELECT a.Id,a.StoreId,a.ProductCategoryId,c.CategoryCode,c.Name CategoryName,a.AliasText,a.NormalizedAliasText,a.LanguageCode,a.IsActive,a.CreatedUtc
 FROM dbo.ProductCategoryAliases a JOIN dbo.ProductCategories c ON c.TenantId=a.TenantId AND c.Id=a.ProductCategoryId
 WHERE a.TenantId=@TenantId AND a.IsActive=1 AND (@StoreId IS NULL OR a.StoreId IS NULL OR a.StoreId=@StoreId) AND (@ProductCategoryId IS NULL OR a.ProductCategoryId=@ProductCategoryId)
 ORDER BY a.AliasText,a.Id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.PreferenceWeight_GetActive @TenantId BIGINT
AS
BEGIN
 SET NOCOUNT ON; SELECT TOP(1) Id,VersionCode,ManualStaffWeight,PurchaseWeight,CategoryInteractionWeight,VoiceConfirmedWeight,IsActive,CreatedUtc FROM dbo.PreferenceWeightVersions WHERE TenantId=@TenantId AND IsActive=1 ORDER BY Id DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.PreferenceAudit_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@PageNumber INT=1,@PageSize INT=50
AS
BEGIN
 SET NOCOUNT ON;IF @PageNumber<1 SET @PageNumber=1;IF @PageSize<1 SET @PageSize=50;IF @PageSize>200 SET @PageSize=200;
 SELECT Id,StoreId,UserId,Action,EntityType,EntityId,BeforeJson,AfterJson,CorrelationId,CreatedUtc,COUNT_BIG(1) OVER() TotalCount FROM dbo.AuditLogs
 WHERE TenantId=@TenantId AND (Action LIKE N'CustomerPreference%' OR Action LIKE N'HouseholdPreference%' OR Action LIKE N'PreferenceWeight%' OR Action LIKE N'VoiceCommand%' OR Action LIKE N'StoreVoice%' OR Action LIKE N'ProductCategoryAlias%')
 AND (@AllowedStoreIdsCsv IS NULL OR StoreId IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=StoreId))
 ORDER BY CreatedUtc DESC,Id DESC OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0')
    INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.9.0',N'Phase 10 factual preferences, category aliases, verified-household aggregation, dynamic voice confirmation and versioned recalculation',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0')<>1 THROW 52090,'V1.9.0 DatabaseVersions row must exist exactly once.',1;
GO

-- ============================================================
-- PHASE 11 - ALERTS & REAL-TIME
-- VERSION: V1.10.0
-- ============================================================
/*
 CustSearch AI — Phase 11 production database upgrade
 Version: V1.10.0
 Scope: tenant/store alerts, durable real-time recovery events and transactional notification outbox.
 Rules: SQL Server 2022, repeat-safe, no EF migrations, UTC timestamps, no provider credentials.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0')
    THROW 53000,'Phase 10 V1.9.0 must be installed before Phase 11.',1;
GO

-- 11A — Authoritative tenant/store alert domain. StoreId NULL means tenant-wide.
IF OBJECT_ID(N'dbo.Alerts',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alerts
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Alerts PRIMARY KEY,
        AlertType NVARCHAR(100) NOT NULL,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        Severity TINYINT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(2000) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        AcknowledgedUtc DATETIME2(7) NULL,
        AcknowledgedByUserId BIGINT NULL,
        ResolvedUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        DeduplicationKey NVARCHAR(200) NOT NULL,
        CONSTRAINT FK_Alerts_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Alerts_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_Alerts_AcknowledgedByUser FOREIGN KEY(AcknowledgedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Alerts_Severity CHECK(Severity BETWEEN 1 AND 3),
        CONSTRAINT CK_Alerts_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_Alerts_Acknowledgement CHECK((AcknowledgedUtc IS NULL AND AcknowledgedByUserId IS NULL) OR (AcknowledgedUtc IS NOT NULL AND AcknowledgedByUserId IS NOT NULL)),
        CONSTRAINT CK_Alerts_Resolution CHECK(ResolvedUtc IS NULL OR ResolvedUtc>=CreatedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'UX_Alerts_Tenant_Id') CREATE UNIQUE INDEX UX_Alerts_Tenant_Id ON dbo.Alerts(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'UX_Alerts_Tenant_DeduplicationKey') CREATE UNIQUE INDEX UX_Alerts_Tenant_DeduplicationKey ON dbo.Alerts(TenantId,DeduplicationKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'IX_Alerts_Tenant_Store_Status_Created') CREATE INDEX IX_Alerts_Tenant_Store_Status_Created ON dbo.Alerts(TenantId,StoreId,Status,CreatedUtc DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'IX_Alerts_Tenant_Entity') CREATE INDEX IX_Alerts_Tenant_Entity ON dbo.Alerts(TenantId,EntityType,EntityId);
GO

-- 11D/11E — Durable ordered events remain authoritative for reconnect recovery.
IF OBJECT_ID(N'dbo.RealtimeEvents',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RealtimeEvents
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RealtimeEvents PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        AlertId BIGINT NOT NULL,
        EventName NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        OccurredUtc DATETIME2(7) NOT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        DeduplicationKey NVARCHAR(200) NOT NULL,
        CONSTRAINT FK_RealtimeEvents_Alerts_TenantAlert FOREIGN KEY(TenantId,AlertId) REFERENCES dbo.Alerts(TenantId,Id),
        CONSTRAINT FK_RealtimeEvents_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_RealtimeEvents_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_RealtimeEvents_Payload CHECK(ISJSON(PayloadJson)=1)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'UX_RealtimeEvents_Tenant_Id') CREATE UNIQUE INDEX UX_RealtimeEvents_Tenant_Id ON dbo.RealtimeEvents(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'UX_RealtimeEvents_Tenant_DeduplicationKey') CREATE UNIQUE INDEX UX_RealtimeEvents_Tenant_DeduplicationKey ON dbo.RealtimeEvents(TenantId,DeduplicationKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'IX_RealtimeEvents_Tenant_Store_Cursor') CREATE INDEX IX_RealtimeEvents_Tenant_Store_Cursor ON dbo.RealtimeEvents(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'IX_RealtimeEvents_Tenant_Occurred') CREATE INDEX IX_RealtimeEvents_Tenant_Occurred ON dbo.RealtimeEvents(TenantId,OccurredUtc DESC);
GO

-- 11B/11G — Transactional channel outbox. External adapters run only after commit.
IF OBJECT_ID(N'dbo.NotificationOutbox',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationOutbox
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationOutbox PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        AlertId BIGINT NOT NULL,
        RealtimeEventId BIGINT NOT NULL,
        Channel NVARCHAR(30) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        Status TINYINT NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_NotificationOutbox_AttemptCount DEFAULT(0),
        NextAttemptUtc DATETIME2(7) NOT NULL,
        LastError NVARCHAR(2000) NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        ProcessedUtc DATETIME2(7) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_NotificationOutbox_Alerts_TenantAlert FOREIGN KEY(TenantId,AlertId) REFERENCES dbo.Alerts(TenantId,Id),
        CONSTRAINT FK_NotificationOutbox_RealtimeEvents_TenantEvent FOREIGN KEY(TenantId,RealtimeEventId) REFERENCES dbo.RealtimeEvents(TenantId,Id),
        CONSTRAINT FK_NotificationOutbox_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_NotificationOutbox_Status CHECK(Status BETWEEN 1 AND 6),
        CONSTRAINT CK_NotificationOutbox_AttemptCount CHECK(AttemptCount>=0),
        CONSTRAINT CK_NotificationOutbox_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_NotificationOutbox_Payload CHECK(ISJSON(PayloadJson)=1),
        CONSTRAINT CK_NotificationOutbox_Processed CHECK((Status IN(3,6) AND ProcessedUtc IS NOT NULL) OR (Status NOT IN(3,6) AND ProcessedUtc IS NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'UX_NotificationOutbox_IdempotencyKey') CREATE UNIQUE INDEX UX_NotificationOutbox_IdempotencyKey ON dbo.NotificationOutbox(IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'IX_NotificationOutbox_Status_NextAttempt') CREATE INDEX IX_NotificationOutbox_Status_NextAttempt ON dbo.NotificationOutbox(Status,NextAttemptUtc,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'IX_NotificationOutbox_Tenant_Status_Created') CREATE INDEX IX_NotificationOutbox_Tenant_Status_Created ON dbo.NotificationOutbox(TenantId,Status,CreatedUtc);
GO

-- Tenant/store reads always accept server-authorized store IDs, never browser TenantId.
CREATE OR ALTER PROCEDURE dbo.Alert_Search
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@Status TINYINT=NULL,@Take INT=100
AS
BEGIN
    SET NOCOUNT ON;
    IF @Take<1 SET @Take=1;IF @Take>200 SET @Take=200;
    IF @StoreId IS NOT NULL AND @AllowedStoreIdsCsv IS NOT NULL AND NOT EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=@StoreId) RETURN;
    SELECT TOP(@Take) Id,AlertType,StoreId,Severity,Title,Message,EntityType,EntityId,CreatedUtc,AcknowledgedUtc,AcknowledgedByUserId,ResolvedUtc,Status,CorrelationId,DeduplicationKey
    FROM dbo.Alerts
    WHERE TenantId=@TenantId AND (@StoreId IS NULL OR StoreId=@StoreId) AND (@Status IS NULL OR Status=@Status)
      AND (StoreId IS NULL OR @AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=StoreId))
    ORDER BY CreatedUtc DESC,Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.AlertRecovery_Get
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@AfterEventId BIGINT=0,@Take INT=200
AS
BEGIN
    SET NOCOUNT ON;
    IF @AfterEventId<0 THROW 53020,'Recovery cursor cannot be negative.',1;
    IF @Take<1 SET @Take=1;IF @Take>500 SET @Take=500;
    SELECT TOP(@Take) Id EventId,EventName,ContractVersion,PayloadJson,OccurredUtc,StoreId,CorrelationId
    FROM dbo.RealtimeEvents
    WHERE TenantId=@TenantId AND Id>@AfterEventId
      AND (StoreId IS NULL OR @AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=StoreId))
    ORDER BY Id;
END;
GO

-- READPAST/UPDLOCK claims avoid duplicate processing between concurrent dispatchers.
CREATE OR ALTER PROCEDURE dbo.NotificationOutbox_Claim @BatchSize INT=50,@UtcNow DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    IF @UtcNow IS NULL SET @UtcNow=SYSUTCDATETIME();IF @BatchSize<1 SET @BatchSize=1;IF @BatchSize>200 SET @BatchSize=200;
    ;WITH Due AS
    (
        SELECT TOP(@BatchSize) * FROM dbo.NotificationOutbox WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE Status IN(1,4,5,2) AND NextAttemptUtc<=@UtcNow ORDER BY NextAttemptUtc,Id
    )
    UPDATE Due SET Status=2,AttemptCount=AttemptCount+1,NextAttemptUtc=DATEADD(MINUTE,2,@UtcNow),LastError=NULL
    OUTPUT inserted.Id,inserted.TenantId,inserted.StoreId,inserted.AlertId,inserted.RealtimeEventId,inserted.Channel,inserted.EventType,inserted.ContractVersion,inserted.PayloadJson,inserted.AttemptCount,inserted.CorrelationId,inserted.IdempotencyKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.NotificationOutbox_Metrics @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
      SUM(CASE WHEN Status IN(1,2,4,5) THEN CONVERT(BIGINT,1) ELSE 0 END) OutboxBacklog,
      SUM(CASE WHEN Status=3 THEN CONVERT(BIGINT,1) ELSE 0 END) DeliverySuccesses,
      SUM(CASE WHEN Status IN(4,5,6) THEN CONVERT(BIGINT,1) ELSE 0 END) DeliveryFailures,
      SUM(CASE WHEN AttemptCount>1 THEN CONVERT(BIGINT,AttemptCount-1) ELSE 0 END) Retries,
      SUM(CASE WHEN Status=6 THEN CONVERT(BIGINT,1) ELSE 0 END) DeadLetters,
      MIN(CASE WHEN Status IN(1,2,4,5) THEN CreatedUtc END) OldestPendingUtc
    FROM dbo.NotificationOutbox WHERE TenantId=@TenantId;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0')
    INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.10.0',N'Phase 11 tenant/store alerts, durable real-time recovery and transactional notification outbox',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0')<>1 THROW 53090,'V1.10.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.Alerts',N'U') IS NULL OR OBJECT_ID(N'dbo.RealtimeEvents',N'U') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox',N'U') IS NULL THROW 53091,'Phase 11 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.Alert_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.AlertRecovery_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox_Metrics',N'P') IS NULL THROW 53092,'Phase 11 procedures are incomplete.',1;
GO

-- ============================================================
-- PHASE 12 - SECURE INTEGRATIONS
-- VERSION: V1.11.0
-- ============================================================
/*
 CustSearch AI — Phase 12 production database upgrade
 Version: V1.11.0
 Scope: tenant integration configuration, authenticated inbound receipts, outbound outbox and delivery audit.
 Security: only opaque credential/signing-secret references are stored; no provider secret values or full inbound payloads.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0') THROW 54000,'Phase 11 V1.10.0 must be installed before Phase 12.',1;
GO

IF OBJECT_ID(N'dbo.IntegrationConfigurations',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationConfigurations
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationConfigurations PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        IntegrationType TINYINT NOT NULL,
        Enabled BIT NOT NULL,
        EndpointBaseUrl NVARCHAR(500) NOT NULL,
        CredentialReference NVARCHAR(200) NULL,
        WebhookSigningSecretReference NVARCHAR(200) NULL,
        PreviousWebhookSigningSecretReference NVARCHAR(200) NULL,
        PreviousSigningSecretValidUntilUtc DATETIME2(7) NULL,
        TimeoutSeconds INT NOT NULL,
        RetryMaxAttempts INT NOT NULL,
        RetryBaseDelaySeconds INT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        UpdatedUtc DATETIME2(7) NOT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_IntegrationConfigurations_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT CK_IntegrationConfigurations_Type CHECK(IntegrationType BETWEEN 1 AND 4),
        CONSTRAINT CK_IntegrationConfigurations_Endpoint CHECK(EndpointBaseUrl LIKE N'https://%' AND EndpointBaseUrl NOT LIKE N'%@%'),
        CONSTRAINT CK_IntegrationConfigurations_Timeout CHECK(TimeoutSeconds BETWEEN 1 AND 120),
        CONSTRAINT CK_IntegrationConfigurations_Retry CHECK(RetryMaxAttempts BETWEEN 1 AND 10 AND RetryBaseDelaySeconds BETWEEN 1 AND 300),
        CONSTRAINT CK_IntegrationConfigurations_Period CHECK(UpdatedUtc>=CreatedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'UX_IntegrationConfigurations_Tenant_Id') CREATE UNIQUE INDEX UX_IntegrationConfigurations_Tenant_Id ON dbo.IntegrationConfigurations(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'UX_IntegrationConfigurations_Tenant_Provider_Type') CREATE UNIQUE INDEX UX_IntegrationConfigurations_Tenant_Provider_Type ON dbo.IntegrationConfigurations(TenantId,Provider,IntegrationType);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'IX_IntegrationConfigurations_Tenant_Enabled_Updated') CREATE INDEX IX_IntegrationConfigurations_Tenant_Enabled_Updated ON dbo.IntegrationConfigurations(TenantId,Enabled,UpdatedUtc DESC);
GO

IF OBJECT_ID(N'dbo.IntegrationInboundEvents',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationInboundEvents
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationInboundEvents PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        IntegrationConfigurationId BIGINT NOT NULL,
        ProviderEventId NVARCHAR(200) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadHash CHAR(64) NOT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        ProviderTimestampUtc DATETIME2(7) NOT NULL,
        ReceivedUtc DATETIME2(7) NOT NULL,
        ProcessedUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CONSTRAINT FK_IntegrationInboundEvents_Configuration FOREIGN KEY(TenantId,IntegrationConfigurationId) REFERENCES dbo.IntegrationConfigurations(TenantId,Id),
        CONSTRAINT CK_IntegrationInboundEvents_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_IntegrationInboundEvents_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_IntegrationInboundEvents_Hash CHECK(PayloadHash NOT LIKE N'%[^0-9a-f]%' AND LEN(PayloadHash)=64),
        CONSTRAINT CK_IntegrationInboundEvents_Period CHECK(ProcessedUtc IS NULL OR ProcessedUtc>=ReceivedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Config_Event') CREATE UNIQUE INDEX UX_IntegrationInboundEvents_Tenant_Config_Event ON dbo.IntegrationInboundEvents(TenantId,IntegrationConfigurationId,ProviderEventId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Config_Idempotency') CREATE UNIQUE INDEX UX_IntegrationInboundEvents_Tenant_Config_Idempotency ON dbo.IntegrationInboundEvents(TenantId,IntegrationConfigurationId,IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Id') CREATE UNIQUE INDEX UX_IntegrationInboundEvents_Tenant_Id ON dbo.IntegrationInboundEvents(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'IX_IntegrationInboundEvents_Tenant_Received') CREATE INDEX IX_IntegrationInboundEvents_Tenant_Received ON dbo.IntegrationInboundEvents(TenantId,ReceivedUtc DESC,Id DESC);
GO

IF OBJECT_ID(N'dbo.IntegrationOutbox',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationOutbox
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationOutbox PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        IntegrationConfigurationId BIGINT NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(500) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        PayloadHash CHAR(64) NOT NULL,
        Status TINYINT NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_IntegrationOutbox_AttemptCount DEFAULT(0),
        MaxAttempts INT NOT NULL,
        RetryBaseDelaySeconds INT NOT NULL,
        NextAttemptUtc DATETIME2(7) NOT NULL,
        LastResponseCode INT NULL,
        LastError NVARCHAR(2000) NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        DeliveredUtc DATETIME2(7) NULL,
        CompletedUtc DATETIME2(7) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_IntegrationOutbox_Configuration FOREIGN KEY(TenantId,IntegrationConfigurationId) REFERENCES dbo.IntegrationConfigurations(TenantId,Id),
        CONSTRAINT CK_IntegrationOutbox_Status CHECK(Status BETWEEN 1 AND 6),
        CONSTRAINT CK_IntegrationOutbox_Attempts CHECK(AttemptCount>=0 AND MaxAttempts BETWEEN 1 AND 10),
        CONSTRAINT CK_IntegrationOutbox_RetryBase CHECK(RetryBaseDelaySeconds BETWEEN 1 AND 300),
        CONSTRAINT CK_IntegrationOutbox_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_IntegrationOutbox_Payload CHECK(ISJSON(PayloadJson)=1),
        CONSTRAINT CK_IntegrationOutbox_Hash CHECK(PayloadHash NOT LIKE N'%[^0-9a-f]%' AND LEN(PayloadHash)=64),
        CONSTRAINT CK_IntegrationOutbox_Completion CHECK((Status=3 AND DeliveredUtc IS NOT NULL AND CompletedUtc IS NOT NULL) OR (Status=6 AND CompletedUtc IS NOT NULL) OR (Status NOT IN(3,6) AND CompletedUtc IS NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'UX_IntegrationOutbox_Tenant_Id') CREATE UNIQUE INDEX UX_IntegrationOutbox_Tenant_Id ON dbo.IntegrationOutbox(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'UX_IntegrationOutbox_Tenant_Idempotency') CREATE UNIQUE INDEX UX_IntegrationOutbox_Tenant_Idempotency ON dbo.IntegrationOutbox(TenantId,IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'IX_IntegrationOutbox_Status_NextAttempt') CREATE INDEX IX_IntegrationOutbox_Status_NextAttempt ON dbo.IntegrationOutbox(Status,NextAttemptUtc,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'IX_IntegrationOutbox_Tenant_Config_Created') CREATE INDEX IX_IntegrationOutbox_Tenant_Config_Created ON dbo.IntegrationOutbox(TenantId,IntegrationConfigurationId,CreatedUtc DESC);
GO

IF OBJECT_ID(N'dbo.IntegrationDeliveryLogs',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationDeliveryLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationDeliveryLogs PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        IntegrationConfigurationId BIGINT NOT NULL,
        InboundEventId BIGINT NULL,
        OutboxMessageId BIGINT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        Direction TINYINT NOT NULL,
        Status TINYINT NOT NULL,
        DurationMilliseconds BIGINT NOT NULL,
        HttpStatusCode INT NULL,
        ErrorCategory NVARCHAR(100) NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_IntegrationDeliveryLogs_Configuration FOREIGN KEY(TenantId,IntegrationConfigurationId) REFERENCES dbo.IntegrationConfigurations(TenantId,Id),
        CONSTRAINT FK_IntegrationDeliveryLogs_Inbound FOREIGN KEY(TenantId,InboundEventId) REFERENCES dbo.IntegrationInboundEvents(TenantId,Id),
        CONSTRAINT FK_IntegrationDeliveryLogs_Outbox FOREIGN KEY(TenantId,OutboxMessageId) REFERENCES dbo.IntegrationOutbox(TenantId,Id),
        CONSTRAINT CK_IntegrationDeliveryLogs_Direction CHECK(Direction BETWEEN 1 AND 2),
        CONSTRAINT CK_IntegrationDeliveryLogs_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_IntegrationDeliveryLogs_Source CHECK((InboundEventId IS NULL AND OutboxMessageId IS NOT NULL) OR (InboundEventId IS NOT NULL AND OutboxMessageId IS NULL)),
        CONSTRAINT CK_IntegrationDeliveryLogs_Duration CHECK(DurationMilliseconds>=0),
        CONSTRAINT CK_IntegrationDeliveryLogs_Http CHECK(HttpStatusCode IS NULL OR HttpStatusCode BETWEEN 100 AND 599)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationDeliveryLogs') AND name=N'IX_IntegrationDeliveryLogs_Tenant_Config_Created') CREATE INDEX IX_IntegrationDeliveryLogs_Tenant_Config_Created ON dbo.IntegrationDeliveryLogs(TenantId,IntegrationConfigurationId,CreatedUtc DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationDeliveryLogs') AND name=N'IX_IntegrationDeliveryLogs_Tenant_Direction_Status') CREATE INDEX IX_IntegrationDeliveryLogs_Tenant_Direction_Status ON dbo.IntegrationDeliveryLogs(TenantId,Direction,Status,CreatedUtc DESC);
GO

CREATE OR ALTER PROCEDURE dbo.Integration_Search @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Provider,IntegrationType,Enabled,EndpointBaseUrl,
           CONVERT(BIT,CASE WHEN CredentialReference IS NULL THEN 0 ELSE 1 END) HasCredentialReference,
           CONVERT(BIT,CASE WHEN WebhookSigningSecretReference IS NULL THEN 0 ELSE 1 END) HasWebhookSigningSecret,
           TimeoutSeconds,RetryMaxAttempts,RetryBaseDelaySeconds,CreatedUtc,UpdatedUtc,RowVersion
    FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId ORDER BY Provider,IntegrationType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.IntegrationOutbox_Claim @BatchSize INT=50,@UtcNow DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;IF @UtcNow IS NULL SET @UtcNow=SYSUTCDATETIME();IF @BatchSize<1 SET @BatchSize=1;IF @BatchSize>200 SET @BatchSize=200;
    ;WITH Due AS(SELECT TOP(@BatchSize)* FROM dbo.IntegrationOutbox WITH(UPDLOCK,READPAST,ROWLOCK) WHERE Status IN(1,2,4,5) AND NextAttemptUtc<=@UtcNow ORDER BY NextAttemptUtc,Id)
    UPDATE Due SET Status=2,AttemptCount=AttemptCount+1,NextAttemptUtc=DATEADD(MINUTE,2,@UtcNow),LastError=NULL
    OUTPUT inserted.Id,inserted.TenantId,inserted.IntegrationConfigurationId,inserted.Provider,inserted.Destination,inserted.EventType,inserted.ContractVersion,inserted.PayloadJson,inserted.AttemptCount,inserted.MaxAttempts,inserted.RetryBaseDelaySeconds,inserted.CorrelationId,inserted.IdempotencyKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.IntegrationOutbox_ManualRetry @TenantId BIGINT,@DeliveryId BIGINT,@UtcNow DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;IF @UtcNow IS NULL SET @UtcNow=SYSUTCDATETIME();
    UPDATE dbo.IntegrationOutbox SET Status=5,AttemptCount=0,NextAttemptUtc=@UtcNow,LastResponseCode=NULL,LastError=NULL,DeliveredUtc=NULL,CompletedUtc=NULL WHERE TenantId=@TenantId AND Id=@DeliveryId AND Status IN(4,5,6);
    IF @@ROWCOUNT<>1 THROW 54020,'Failed/dead-letter integration delivery was not found.',1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.IntegrationDeliveryLog_Search @TenantId BIGINT,@IntegrationConfigurationId BIGINT=NULL,@Take INT=100
AS
BEGIN
    SET NOCOUNT ON;IF @Take<1 SET @Take=1;IF @Take>500 SET @Take=500;
    SELECT TOP(@Take) Id,IntegrationConfigurationId,InboundEventId,OutboxMessageId,CorrelationId,Provider,Direction,Status,DurationMilliseconds,HttpStatusCode,ErrorCategory,CreatedUtc FROM dbo.IntegrationDeliveryLogs WHERE TenantId=@TenantId AND (@IntegrationConfigurationId IS NULL OR IntegrationConfigurationId=@IntegrationConfigurationId) ORDER BY CreatedUtc DESC,Id DESC;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.11.0') INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.11.0',N'Phase 12 secure tenant integrations, HMAC inbound receipts, outbound outbox and delivery audit',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.11.0')<>1 THROW 54090,'V1.11.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.IntegrationConfigurations',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationInboundEvents',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationDeliveryLogs',N'U') IS NULL THROW 54091,'Phase 12 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.Integration_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox_ManualRetry',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationDeliveryLog_Search',N'P') IS NULL THROW 54092,'Phase 12 procedures are incomplete.',1;
GO

-- ============================================================
-- PHASE 13 - CAMERAS & ANONYMOUS TRACKING
-- VERSION: V1.12.0
-- ============================================================
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
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraZoneConfigurations PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,ZoneCode NVARCHAR(50) NOT NULL,Name NVARCHAR(150) NOT NULL,ZoneType TINYINT NOT NULL,GeometryJson NVARCHAR(4000) NOT NULL,Version INT NOT NULL,CategoryId BIGINT NULL,EffectiveUtc DATETIME2(7) NOT NULL,SupersededUtc DATETIME2(7) NULL,IsActive BIT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,
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

-- ============================================================
-- PHASE 14 - CONSENT-BASED RECOGNITION
-- VERSION: V1.13.0
-- ============================================================
/* CustSearch AI Phase 14 — consent-gated recognition metadata and encrypted derived templates. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET ARITHABORT ON;SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.12.0') THROW 54700,'Phase 13 V1.12.0 must be installed before Phase 14.',1;
GO

IF OBJECT_ID(N'dbo.CustomerRecognitionConsents',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CustomerRecognitionConsents(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerRecognitionConsents PRIMARY KEY,TenantId BIGINT NOT NULL,CustomerId BIGINT NOT NULL,ConsentType TINYINT NOT NULL,Purpose NVARCHAR(200) NOT NULL,GrantedUtc DATETIME2(7) NOT NULL,ExpiresUtc DATETIME2(7) NULL,WithdrawnUtc DATETIME2(7) NULL,ConsentVersion NVARCHAR(50) NOT NULL,CapturedByUserId BIGINT NOT NULL,EvidenceReference NVARCHAR(500) NULL,CreatedUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_RecognitionConsents_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_RecognitionConsents_Customers FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT CK_RecognitionConsents_Type CHECK(ConsentType=1),CONSTRAINT CK_RecognitionConsents_Period CHECK(ExpiresUtc IS NULL OR ExpiresUtc>GrantedUtc),CONSTRAINT CK_RecognitionConsents_Withdrawal CHECK(WithdrawnUtc IS NULL OR WithdrawnUtc>=GrantedUtc)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerRecognitionConsents') AND name=N'UX_RecognitionConsents_Tenant_Id') CREATE UNIQUE INDEX UX_RecognitionConsents_Tenant_Id ON dbo.CustomerRecognitionConsents(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerRecognitionConsents') AND name=N'IX_RecognitionConsents_Tenant_Customer_Purpose') CREATE INDEX IX_RecognitionConsents_Tenant_Customer_Purpose ON dbo.CustomerRecognitionConsents(TenantId,CustomerId,ConsentType,Purpose,GrantedUtc DESC);
GO

IF OBJECT_ID(N'dbo.BiometricTemplates',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BiometricTemplates(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BiometricTemplates PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CustomerId BIGINT NOT NULL,ConsentId BIGINT NOT NULL,EncryptedTemplate VARBINARY(MAX) NOT NULL,Nonce VARBINARY(12) NOT NULL,AuthenticationTag VARBINARY(16) NOT NULL,EncryptionKeyReference NVARCHAR(200) NOT NULL,Algorithm NVARCHAR(50) NOT NULL,TemplateVersion NVARCHAR(50) NOT NULL,Status TINYINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,DisabledUtc DATETIME2(7) NULL,DeletedUtc DATETIME2(7) NULL,RetentionUntilUtc DATETIME2(7) NULL,
  CONSTRAINT FK_BiometricTemplates_Stores FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT FK_BiometricTemplates_Customers FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT FK_BiometricTemplates_Consents FOREIGN KEY(TenantId,ConsentId) REFERENCES dbo.CustomerRecognitionConsents(TenantId,Id),CONSTRAINT CK_BiometricTemplates_Status CHECK(Status BETWEEN 1 AND 3),CONSTRAINT CK_BiometricTemplates_Protected CHECK((Status=1 AND DATALENGTH(EncryptedTemplate)>0 AND DATALENGTH(Nonce)=12 AND DATALENGTH(AuthenticationTag)=16) OR (Status IN(2,3) AND DATALENGTH(EncryptedTemplate)=0 AND DATALENGTH(Nonce)=0 AND DATALENGTH(AuthenticationTag)=0)),CONSTRAINT CK_BiometricTemplates_Deletion CHECK((Status=1 AND DisabledUtc IS NULL AND DeletedUtc IS NULL) OR (Status IN(2,3) AND DisabledUtc IS NOT NULL))
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BiometricTemplates') AND name=N'UX_BiometricTemplates_Tenant_Store_Id') CREATE UNIQUE INDEX UX_BiometricTemplates_Tenant_Store_Id ON dbo.BiometricTemplates(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BiometricTemplates') AND name=N'UX_BiometricTemplates_Current') CREATE UNIQUE INDEX UX_BiometricTemplates_Current ON dbo.BiometricTemplates(TenantId,StoreId,CustomerId) WHERE Status=1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BiometricTemplates') AND name=N'IX_BiometricTemplates_Consent') CREATE INDEX IX_BiometricTemplates_Consent ON dbo.BiometricTemplates(TenantId,ConsentId,Status);
GO

IF OBJECT_ID(N'dbo.RecognitionCandidates',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RecognitionCandidates(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecognitionCandidates PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,PersonTrackSessionId BIGINT NOT NULL,BiometricTemplateId BIGINT NOT NULL,CustomerId BIGINT NOT NULL,RequestId NVARCHAR(150) NOT NULL,Purpose NVARCHAR(200) NOT NULL,Confidence DECIMAL(5,4) NOT NULL,Quality DECIMAL(5,4) NOT NULL,SecondBestConfidence DECIMAL(5,4) NULL,Status TINYINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,ReviewedUtc DATETIME2(7) NULL,ReviewedByUserId BIGINT NULL,ReviewReason NVARCHAR(500) NULL,
  CONSTRAINT FK_RecognitionCandidates_Tracks FOREIGN KEY(TenantId,StoreId,PersonTrackSessionId) REFERENCES dbo.PersonTrackSessions(TenantId,StoreId,Id),CONSTRAINT FK_RecognitionCandidates_Templates FOREIGN KEY(TenantId,StoreId,BiometricTemplateId) REFERENCES dbo.BiometricTemplates(TenantId,StoreId,Id),CONSTRAINT FK_RecognitionCandidates_Customers FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT CK_RecognitionCandidates_Status CHECK(Status BETWEEN 1 AND 5),CONSTRAINT CK_RecognitionCandidates_Scores CHECK(Confidence BETWEEN 0 AND 1 AND Quality BETWEEN 0 AND 1 AND (SecondBestConfidence IS NULL OR SecondBestConfidence BETWEEN 0 AND 1)),CONSTRAINT CK_RecognitionCandidates_Review CHECK((Status IN(1,2) AND ReviewedUtc IS NULL AND ReviewedByUserId IS NULL) OR (Status IN(3,4,5) AND ReviewedUtc IS NOT NULL AND ReviewedByUserId IS NOT NULL))
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RecognitionCandidates') AND name=N'UX_RecognitionCandidates_Tenant_Store_Request') CREATE UNIQUE INDEX UX_RecognitionCandidates_Tenant_Store_Request ON dbo.RecognitionCandidates(TenantId,StoreId,RequestId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RecognitionCandidates') AND name=N'IX_RecognitionCandidates_Tenant_Store_Status') CREATE INDEX IX_RecognitionCandidates_Tenant_Store_Status ON dbo.RecognitionCandidates(TenantId,StoreId,Status,CreatedUtc DESC,Id DESC);
GO

DECLARE @Phase14Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
INSERT @Phase14Permissions VALUES
(N'Recognition.View',N'View consent, template metadata and recognition candidates.'),(N'Recognition.Enroll',N'Enroll a derived biometric template for an explicitly consenting customer.'),(N'Recognition.Review',N'Human-review recognition candidates.'),(N'Recognition.Settings.Manage',N'Manage recognition thresholds and submit protected candidate results.'),(N'Recognition.Consent.Manage',N'Record and withdraw purpose-specific recognition consent.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Phase14Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'STOREADMIN',N'STOREMANAGER') AND p.Name IN(N'Recognition.View',N'Recognition.Enroll',N'Recognition.Review',N'Recognition.Settings.Manage',N'Recognition.Consent.Manage') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CAMERAOPERATOR' AND p.Name IN(N'Recognition.View',N'Recognition.Review') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CRMSTAFF' AND p.Name IN(N'Recognition.View',N'Recognition.Enroll',N'Recognition.Consent.Manage') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

CREATE OR ALTER PROCEDURE dbo.RecognitionConsent_Search @TenantId BIGINT,@CustomerId BIGINT
AS BEGIN SET NOCOUNT ON;SELECT Id,CustomerId,ConsentType,Purpose,GrantedUtc,ExpiresUtc,WithdrawnUtc,ConsentVersion,CapturedByUserId,EvidenceReference,CreatedUtc FROM dbo.CustomerRecognitionConsents WHERE TenantId=@TenantId AND CustomerId=@CustomerId ORDER BY GrantedUtc DESC,Id DESC;END;
GO
CREATE OR ALTER PROCEDURE dbo.RecognitionCandidate_Search @TenantId BIGINT,@StoreId BIGINT=NULL,@Status TINYINT=NULL
AS BEGIN SET NOCOUNT ON;SELECT TOP(500) Id,StoreId,PersonTrackSessionId,BiometricTemplateId,CustomerId,RequestId,Purpose,Confidence,Quality,SecondBestConfidence,Status,CreatedUtc,ReviewedUtc,ReviewedByUserId,ReviewReason FROM dbo.RecognitionCandidates WHERE TenantId=@TenantId AND (@StoreId IS NULL OR StoreId=@StoreId) AND (@Status IS NULL OR Status=@Status) ORDER BY CreatedUtc DESC,Id DESC;END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.13.0',N'Phase 14 consent-gated encrypted biometric templates and human-reviewed recognition candidates',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0')<>1 THROW 54790,'V1.13.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.CustomerRecognitionConsents',N'U') IS NULL OR OBJECT_ID(N'dbo.BiometricTemplates',N'U') IS NULL OR OBJECT_ID(N'dbo.RecognitionCandidates',N'U') IS NULL THROW 54791,'Phase 14 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.RecognitionConsent_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.RecognitionCandidate_Search',N'P') IS NULL THROW 54792,'Phase 14 procedures are incomplete.',1;
GO

-- ============================================================
-- PHASE 15 - REPORTS AND ASYNC EXPORTS
-- VERSION: V1.14.0
-- ============================================================
/* CustSearch AI Phase 15 — server-scoped reports and asynchronous export jobs. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET ARITHABORT ON;SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0') THROW 54900,'Phase 14 V1.13.0 must be installed before Phase 15.',1;
GO
IF OBJECT_ID(N'dbo.ExportJobs',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ExportJobs(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExportJobs PRIMARY KEY,
  RequestedByUserId BIGINT NOT NULL,TenantId BIGINT NULL,AlertId BIGINT NULL,ReportType TINYINT NOT NULL,Format TINYINT NOT NULL,
  FilterJson NVARCHAR(4000) NOT NULL,AuthorizedStoreIdsJson NVARCHAR(4000) NOT NULL,Status TINYINT NOT NULL,Progress TINYINT NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL,StartedUtc DATETIME2(7) NULL,CompletedUtc DATETIME2(7) NULL,ExpiresUtc DATETIME2(7) NOT NULL,
  Error NVARCHAR(2000) NULL,FilePath NVARCHAR(1000) NULL,FileName NVARCHAR(260) NULL,ContentType NVARCHAR(150) NULL,
  AttemptCount INT NOT NULL,LeaseId UNIQUEIDENTIFIER NULL,LeaseExpiresUtc DATETIME2(7) NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_ExportJobs_Users FOREIGN KEY(RequestedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_ExportJobs_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
  CONSTRAINT FK_ExportJobs_Alerts FOREIGN KEY(AlertId) REFERENCES dbo.Alerts(Id),
  CONSTRAINT CK_ExportJobs_Scope CHECK((TenantId IS NULL AND ReportType=20) OR (TenantId IS NOT NULL AND ReportType BETWEEN 1 AND 10)),
  CONSTRAINT CK_ExportJobs_Format CHECK(Format BETWEEN 1 AND 3),CONSTRAINT CK_ExportJobs_Status CHECK(Status BETWEEN 1 AND 5),
  CONSTRAINT CK_ExportJobs_Progress CHECK(Progress BETWEEN 0 AND 100),CONSTRAINT CK_ExportJobs_Attempts CHECK(AttemptCount>=0),
  CONSTRAINT CK_ExportJobs_Json CHECK(ISJSON(FilterJson)=1 AND ISJSON(AuthorizedStoreIdsJson)=1),
  CONSTRAINT CK_ExportJobs_Retention CHECK(ExpiresUtc>CreatedUtc),
  CONSTRAINT CK_ExportJobs_File CHECK((Status=3 AND FilePath IS NOT NULL AND FileName IS NOT NULL AND ContentType IS NOT NULL AND Progress=100) OR Status<>3)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'IX_ExportJobs_Status_Created') CREATE INDEX IX_ExportJobs_Status_Created ON dbo.ExportJobs(Status,CreatedUtc,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'IX_ExportJobs_Tenant_User_Created') CREATE INDEX IX_ExportJobs_Tenant_User_Created ON dbo.ExportJobs(TenantId,RequestedByUserId,CreatedUtc DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'UX_ExportJobs_Alert') CREATE UNIQUE INDEX UX_ExportJobs_Alert ON dbo.ExportJobs(AlertId) WHERE AlertId IS NOT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.Report_TenantOperationalSummary
 @TenantId BIGINT,@ReportType TINYINT,@StoreIdsJson NVARCHAR(4000),@FromUtc DATETIME2(7),@ToUtc DATETIME2(7),@PageNumber INT=1,@PageSize INT=100
AS
BEGIN
 SET NOCOUNT ON;
 IF @TenantId<=0 OR @ReportType NOT BETWEEN 1 AND 10 OR @FromUtc>=@ToUtc THROW 54910,'Invalid tenant report filter.',1;
 IF @PageNumber<1 OR @PageSize<1 OR @PageSize>500 THROW 54911,'Invalid tenant report paging.',1;
 IF ISJSON(@StoreIdsJson)<>1 THROW 54912,'Authorized store scope must be JSON.',1;
 DECLARE @Stores TABLE(StoreId BIGINT PRIMARY KEY);
 INSERT @Stores(StoreId) SELECT DISTINCT TRY_CONVERT(BIGINT,[value]) FROM OPENJSON(@StoreIdsJson) WHERE TRY_CONVERT(BIGINT,[value])>0;
 IF EXISTS(SELECT 1 FROM @Stores a LEFT JOIN dbo.Stores s ON s.Id=a.StoreId AND s.TenantId=@TenantId AND s.IsActive=1 WHERE s.Id IS NULL) THROW 54913,'Authorized store scope is invalid.',1;
 DECLARE @Rows TABLE(Domain NVARCHAR(80),StoreId BIGINT NULL,Metric NVARCHAR(120),Value DECIMAL(19,4),Label NVARCHAR(250) NULL,OccurredUtc DATETIME2(7) NULL);
 IF @ReportType IN(1,2) INSERT @Rows SELECT N'Customers',a.StoreId,N'Active customers',COUNT_BIG(DISTINCT a.CustomerId),NULL,NULL FROM dbo.CustomerStoreAssignments a JOIN @Stores s ON s.StoreId=a.StoreId JOIN dbo.Customers c ON c.Id=a.CustomerId AND c.TenantId=@TenantId AND c.IsActive=1 WHERE a.TenantId=@TenantId GROUP BY a.StoreId;
 IF @ReportType IN(1,3) INSERT @Rows SELECT N'Households',NULL,N'Households',COUNT_BIG(*),N'Tenant-wide factual households',NULL FROM dbo.Households h WHERE h.TenantId=@TenantId AND h.CreatedUtc>=@FromUtc AND h.CreatedUtc<@ToUtc;
 IF @ReportType IN(1,4) INSERT @Rows SELECT N'Visits',v.StoreId,N'Customer visits',COUNT_BIG(*),NULL,MAX(v.EnteredUtc) FROM dbo.CustomerVisits v JOIN @Stores s ON s.StoreId=v.StoreId WHERE v.TenantId=@TenantId AND v.EnteredUtc>=@FromUtc AND v.EnteredUtc<@ToUtc GROUP BY v.StoreId;
 IF @ReportType IN(1,5) INSERT @Rows SELECT N'Retail billing',i.StoreId,N'Invoice total',SUM(i.GrandTotal),CONCAT(COUNT_BIG(*),N' invoices'),MAX(i.InvoiceUtc) FROM dbo.RetailInvoices i JOIN @Stores s ON s.StoreId=i.StoreId WHERE i.TenantId=@TenantId AND i.InvoiceUtc>=@FromUtc AND i.InvoiceUtc<@ToUtc GROUP BY i.StoreId;
 IF @ReportType IN(1,6) INSERT @Rows SELECT N'Preferences',p.StoreId,N'Preference signals',COUNT_BIG(*),N'Factual and derived signals remain distinguishable',MAX(p.LastObservedUtc) FROM dbo.CustomerPreferenceSignals p LEFT JOIN @Stores s ON s.StoreId=p.StoreId WHERE p.TenantId=@TenantId AND (p.StoreId IS NULL OR s.StoreId IS NOT NULL) AND p.LastObservedUtc>=@FromUtc AND p.LastObservedUtc<@ToUtc GROUP BY p.StoreId;
 IF @ReportType IN(1,7) INSERT @Rows SELECT N'Alerts',a.StoreId,N'Alerts',COUNT_BIG(*),CONCAT(N'Open: ',SUM(CASE WHEN a.Status IN(1,2,3) THEN 1 ELSE 0 END)),MAX(a.CreatedUtc) FROM dbo.Alerts a LEFT JOIN @Stores s ON s.StoreId=a.StoreId WHERE a.TenantId=@TenantId AND (a.StoreId IS NULL OR s.StoreId IS NOT NULL) AND a.CreatedUtc>=@FromUtc AND a.CreatedUtc<@ToUtc GROUP BY a.StoreId;
 IF @ReportType IN(1,8) INSERT @Rows SELECT N'Integrations',NULL,N'Delivery attempts',COUNT_BIG(*),CONCAT(N'Failures: ',SUM(CASE WHEN l.Status=2 THEN 1 ELSE 0 END)),MAX(l.CreatedUtc) FROM dbo.IntegrationDeliveryLogs l WHERE l.TenantId=@TenantId AND l.CreatedUtc>=@FromUtc AND l.CreatedUtc<@ToUtc;
 IF @ReportType IN(1,9) INSERT @Rows SELECT N'Cameras',c.StoreId,N'Active cameras',COUNT_BIG(*),CONCAT(N'Online: ',SUM(CASE WHEN c.Status=2 THEN 1 ELSE 0 END)),MAX(c.LastHeartbeatUtc) FROM dbo.Cameras c JOIN @Stores s ON s.StoreId=c.StoreId WHERE c.TenantId=@TenantId AND c.IsActive=1 GROUP BY c.StoreId;
 IF @ReportType IN(1,10) INSERT @Rows SELECT N'Staff operations',p.StoreId,N'Presence sessions',COUNT_BIG(*),N'Operational only; not payroll or discipline truth',MAX(p.EnteredUtc) FROM dbo.StaffPresenceSessions p JOIN @Stores s ON s.StoreId=p.StoreId WHERE p.TenantId=@TenantId AND p.EnteredUtc>=@FromUtc AND p.EnteredUtc<@ToUtc GROUP BY p.StoreId;
 ;WITH Ordered AS(SELECT COUNT_BIG(*) OVER()TotalRows,Domain,StoreId,Metric,Value,Label,OccurredUtc,ROW_NUMBER()OVER(ORDER BY Domain,StoreId,Metric)rn FROM @Rows)
 SELECT TotalRows,Domain,StoreId,Metric,Value,Label,OccurredUtc FROM Ordered WHERE rn BETWEEN ((@PageNumber-1)*@PageSize)+1 AND @PageNumber*@PageSize ORDER BY rn;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Report_PlatformTenantSummary @FromUtc DATETIME2(7),@ToUtc DATETIME2(7),@PageNumber INT=1,@PageSize INT=100
AS
BEGIN
 SET NOCOUNT ON;
 IF @FromUtc>=@ToUtc OR @PageNumber<1 OR @PageSize<1 OR @PageSize>500 THROW 54920,'Invalid platform report filter.',1;
 ;WITH SourceRows AS(
  SELECT N'Platform tenants' Domain,CAST(NULL AS BIGINT)StoreId,N'Tenant' Metric,CAST(t.Id AS DECIMAL(19,4))Value,CONCAT(t.TenantCode,N' · ',t.DisplayName,N' · active=',t.IsActive,N' · suspended=',t.IsSuspended)Label,t.CreatedUtc OccurredUtc
  FROM dbo.Tenants t WHERE t.CreatedUtc>=@FromUtc AND t.CreatedUtc<@ToUtc
 ),Ordered AS(SELECT COUNT_BIG(*)OVER()TotalRows,*,ROW_NUMBER()OVER(ORDER BY OccurredUtc,Value)rn FROM SourceRows)
 SELECT TotalRows,Domain,StoreId,Metric,Value,Label,OccurredUtc FROM Ordered WHERE rn BETWEEN ((@PageNumber-1)*@PageSize)+1 AND @PageNumber*@PageSize ORDER BY rn;
END;
GO

DECLARE @Permissions TABLE(Scope TINYINT,Name NVARCHAR(150),Description NVARCHAR(300));
INSERT @Permissions VALUES(1,N'PlatformReports.View',N'View platform operational reports.'),(1,N'PlatformReports.Export',N'Create and download platform exports.'),(2,N'Reports.View',N'View tenant/store-scoped reports.'),(2,N'Reports.Export',N'Create and download tenant/store-scoped exports.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT p.Scope,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=r.Scope AND p.IsActive=1 WHERE r.IsActive=1 AND ((r.Scope=1 AND r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMOPERATIONSADMIN',N'PLATFORMAUDITOR') AND p.Name IN(N'PlatformReports.View',N'PlatformReports.Export')) OR (r.Scope=2 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'STOREADMIN',N'STOREMANAGER') AND p.Name IN(N'Reports.View',N'Reports.Export'))) AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.14.0',N'Phase 15 server-scoped report catalog and authorized asynchronous CSV Excel PDF exports',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')<>1 THROW 54990,'V1.14.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.ExportJobs',N'U') IS NULL OR OBJECT_ID(N'dbo.Report_TenantOperationalSummary',N'P') IS NULL OR OBJECT_ID(N'dbo.Report_PlatformTenantSummary',N'P') IS NULL THROW 54991,'Phase 15 database objects are incomplete.',1;
GO

-- ============================================================
-- PHASE 16 - OPERATIONAL PLATFORM
-- VERSION: V1.15.0
-- ============================================================
/* CustSearch AI Phase 16 — operational settings, worker coordination, health and audited retention. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET ARITHABORT ON;SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0') THROW 55100,'Phase 15 V1.14.0 must be installed before Phase 16.',1;
GO
IF OBJECT_ID(N'dbo.OperationalSettings',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.OperationalSettings(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperationalSettings PRIMARY KEY,Scope TINYINT NOT NULL,TenantId BIGINT NULL,StoreId BIGINT NULL,[Key] NVARCHAR(120) NOT NULL,ValueJson NVARCHAR(4000) NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_OperationalSettings_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_OperationalSettings_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
  CONSTRAINT CK_OperationalSettings_Scope CHECK((Scope=1 AND TenantId IS NULL AND StoreId IS NULL) OR(Scope=2 AND TenantId IS NOT NULL AND StoreId IS NULL)OR(Scope=3 AND TenantId IS NOT NULL AND StoreId IS NOT NULL)),
  CONSTRAINT CK_OperationalSettings_Json CHECK(ISJSON(ValueJson)=1)
 );
 CREATE UNIQUE INDEX UX_OperationalSettings_ScopeKey ON dbo.OperationalSettings(Scope,TenantId,StoreId,[Key]);
END;
GO
IF OBJECT_ID(N'dbo.OperationalSecretReferences',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.OperationalSecretReferences(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperationalSecretReferences PRIMARY KEY,Scope TINYINT NOT NULL,TenantId BIGINT NULL,StoreId BIGINT NULL,[Key] NVARCHAR(120) NOT NULL,Reference NVARCHAR(250) NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_OperationalSecretReferences_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_OperationalSecretReferences_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
  CONSTRAINT CK_OperationalSecretReferences_Scope CHECK((Scope=1 AND TenantId IS NULL AND StoreId IS NULL) OR(Scope=2 AND TenantId IS NOT NULL AND StoreId IS NULL)OR(Scope=3 AND TenantId IS NOT NULL AND StoreId IS NOT NULL))
 );
 CREATE UNIQUE INDEX UX_OperationalSecretReferences_ScopeKey ON dbo.OperationalSecretReferences(Scope,TenantId,StoreId,[Key]);
END;
GO
IF OBJECT_ID(N'dbo.WorkerControls',N'U') IS NULL
 CREATE TABLE dbo.WorkerControls(WorkerType NVARCHAR(80) NOT NULL CONSTRAINT PK_WorkerControls PRIMARY KEY,IsPaused BIT NOT NULL CONSTRAINT DF_WorkerControls_Paused DEFAULT(0),Reason NVARCHAR(500) NULL,UpdatedByUserId BIGINT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT FK_WorkerControls_Users FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id));
GO
IF OBJECT_ID(N'dbo.WorkerLeases',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.WorkerLeases(WorkerType NVARCHAR(80) NOT NULL CONSTRAINT PK_WorkerLeases PRIMARY KEY,LeaseId UNIQUEIDENTIFIER NOT NULL,OwnerId NVARCHAR(150) NOT NULL,AcquiredUtc DATETIME2(7) NOT NULL,RenewedUtc DATETIME2(7) NOT NULL,ExpiresUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT CK_WorkerLeases_Period CHECK(ExpiresUtc>=RenewedUtc));
 CREATE INDEX IX_WorkerLeases_Expires ON dbo.WorkerLeases(ExpiresUtc);
END;
GO
IF OBJECT_ID(N'dbo.WorkerHeartbeats',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.WorkerHeartbeats(InstanceId NVARCHAR(150) NOT NULL,WorkerType NVARCHAR(80) NOT NULL,StartedUtc DATETIME2(7) NOT NULL,LastHeartbeatUtc DATETIME2(7) NOT NULL,IsReady BIT NOT NULL,LastError NVARCHAR(1000) NULL,CONSTRAINT PK_WorkerHeartbeats PRIMARY KEY(InstanceId,WorkerType));
 CREATE INDEX IX_WorkerHeartbeats_LastHeartbeat ON dbo.WorkerHeartbeats(LastHeartbeatUtc);
END;
GO
/* Preserve and adapt legacy Phase 16 heartbeat rows for the current Worker entity. */
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'WorkerType') IS NULL
 ALTER TABLE dbo.WorkerHeartbeats ADD WorkerType NVARCHAR(80) NOT NULL CONSTRAINT DF_WorkerHeartbeats_WorkerType DEFAULT(N'custsearch-worker') WITH VALUES;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'IsReady') IS NULL
BEGIN
 ALTER TABLE dbo.WorkerHeartbeats ADD IsReady BIT NOT NULL CONSTRAINT DF_WorkerHeartbeats_IsReady DEFAULT(0) WITH VALUES;
 IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'Status') IS NOT NULL EXEC(N'UPDATE dbo.WorkerHeartbeats SET IsReady=CASE WHEN Status=1 THEN 1 ELSE 0 END;');
END;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'WorkerName') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'WorkerName' AND default_object_id=0)
 ALTER TABLE dbo.WorkerHeartbeats ADD CONSTRAINT DF_WorkerHeartbeats_WorkerName DEFAULT(N'CustSearch.Worker') FOR WorkerName;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'Status') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'Status' AND default_object_id=0)
 ALTER TABLE dbo.WorkerHeartbeats ADD CONSTRAINT DF_WorkerHeartbeats_StatusCompat DEFAULT(1) FOR Status;
GO
IF OBJECT_ID(N'dbo.RetentionPolicies',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetentionPolicies(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetentionPolicies PRIMARY KEY,Domain TINYINT NOT NULL,TenantId BIGINT NULL,StoreId BIGINT NULL,RetentionDays INT NOT NULL,Enabled BIT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
 CONSTRAINT FK_RetentionPolicies_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_RetentionPolicies_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),CONSTRAINT CK_RetentionPolicies_Domain CHECK(Domain BETWEEN 1 AND 7),CONSTRAINT CK_RetentionPolicies_Days CHECK(RetentionDays BETWEEN 1 AND 36500),CONSTRAINT CK_RetentionPolicies_Scope CHECK(StoreId IS NULL OR TenantId IS NOT NULL));
 CREATE UNIQUE INDEX UX_RetentionPolicies_Scope ON dbo.RetentionPolicies(Domain,TenantId,StoreId);
END;
GO
IF OBJECT_ID(N'dbo.RetentionRuns',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetentionRuns(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetentionRuns PRIMARY KEY,PolicyId BIGINT NOT NULL,RunId UNIQUEIDENTIFIER NOT NULL,DeletedCount INT NOT NULL CONSTRAINT DF_RetentionRuns_Deleted DEFAULT(0),Status NVARCHAR(30) NOT NULL,Error NVARCHAR(2000) NULL,StartedUtc DATETIME2(7) NOT NULL,CompletedUtc DATETIME2(7) NULL,CONSTRAINT FK_RetentionRuns_Policies FOREIGN KEY(PolicyId) REFERENCES dbo.RetentionPolicies(Id),CONSTRAINT CK_RetentionRuns_Status CHECK(Status IN(N'Processing',N'Completed',N'Failed')));
 CREATE UNIQUE INDEX UX_RetentionRuns_RunId ON dbo.RetentionRuns(RunId);CREATE INDEX IX_RetentionRuns_PolicyStarted ON dbo.RetentionRuns(PolicyId,StartedUtc);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_AuditLogs_ImmutableUpdate ON dbo.AuditLogs INSTEAD OF UPDATE AS THROW 55120,'Audit entries are immutable.',1;
GO
CREATE OR ALTER PROCEDURE dbo.OperationalRetention_Run @PolicyId BIGINT,@BatchSize INT,@UtcNow DATETIME2(7)
AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @BatchSize NOT BETWEEN 1 AND 5000 THROW 55130,'Retention batch size is invalid.',1;
 DECLARE @Domain TINYINT,@TenantId BIGINT,@StoreId BIGINT,@Days INT,@Enabled BIT,@Deleted INT=0,@Cutoff DATETIME2(7);
 SELECT @Domain=Domain,@TenantId=TenantId,@StoreId=StoreId,@Days=RetentionDays,@Enabled=Enabled FROM dbo.RetentionPolicies WHERE Id=@PolicyId;
 IF @Domain IS NULL THROW 55131,'Retention policy was not found.',1;
 IF @Enabled=0 BEGIN SELECT 0;RETURN;END;
 SET @Cutoff=DATEADD(DAY,-@Days,@UtcNow);
 BEGIN TRANSACTION;
 IF @Domain=1
 BEGIN
  SELECT TOP(@BatchSize) Id INTO #AlertTargets FROM dbo.Alerts WHERE CreatedUtc<@Cutoff AND Status IN(4,5) AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId) ORDER BY Id;
  DELETE n FROM dbo.NotificationOutbox n JOIN #AlertTargets t ON t.Id=n.AlertId;
  DELETE r FROM dbo.RealtimeEvents r JOIN #AlertTargets t ON t.Id=r.AlertId;
  DELETE a FROM dbo.Alerts a JOIN #AlertTargets t ON t.Id=a.Id;SET @Deleted=@@ROWCOUNT;
 END
 ELSE IF @Domain=2 BEGIN DELETE TOP(@BatchSize) FROM dbo.IntegrationDeliveryLogs WHERE CreatedUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=3 BEGIN DELETE TOP(@BatchSize) FROM dbo.ExportJobs WHERE Status=5 AND FilePath IS NULL AND ExpiresUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=4 BEGIN DELETE TOP(@BatchSize) FROM dbo.CameraOperationalEvents WHERE ReceivedUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=5
 BEGIN
  DELETE TOP(@BatchSize) FROM dbo.RecognitionCandidates WHERE CreatedUtc<@Cutoff AND Status IN(3,4) AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@@ROWCOUNT;
  DELETE TOP(@BatchSize) FROM dbo.BiometricTemplates WHERE Status=3 AND RetentionUntilUtc<=@UtcNow AND NOT EXISTS(SELECT 1 FROM dbo.RecognitionCandidates c WHERE c.BiometricTemplateId=BiometricTemplates.Id) AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@Deleted+@@ROWCOUNT;
 END
 ELSE IF @Domain=6 BEGIN DELETE TOP(@BatchSize) FROM dbo.AuditLogs WHERE CreatedUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=7 SET @Deleted=0;
 INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
 VALUES(@TenantId,@StoreId,NULL,N'Worker',N'RetentionExecuted',N'RetentionPolicy',CONVERT(NVARCHAR(100),@PolicyId),NULL,CONCAT(N'{"deleted":',@Deleted,N',"domain":',@Domain,N'}'),NULL,NULL,CONVERT(NVARCHAR(64),NEWID()),@UtcNow);
 COMMIT TRANSACTION;SELECT @Deleted;
END;
GO
DECLARE @WorkerTypes TABLE(WorkerType NVARCHAR(80));INSERT @WorkerTypes VALUES(N'notifications'),(N'integrations'),(N'exports'),(N'retention'),(N'cctv-operations');
INSERT dbo.WorkerControls(WorkerType,IsPaused,Reason,UpdatedByUserId,UpdatedUtc)SELECT WorkerType,0,NULL,NULL,SYSUTCDATETIME()FROM @WorkerTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.WorkerControls x WHERE x.WorkerType=s.WorkerType);
GO
DECLARE @Defaults TABLE(Domain TINYINT,Days INT);INSERT @Defaults VALUES(1,365),(2,180),(3,30),(4,30),(5,30),(6,2555),(7,7);
INSERT dbo.RetentionPolicies(Domain,TenantId,StoreId,RetentionDays,Enabled,CreatedUtc,UpdatedUtc)SELECT Domain,NULL,NULL,Days,1,SYSUTCDATETIME(),SYSUTCDATETIME()FROM @Defaults d WHERE NOT EXISTS(SELECT 1 FROM dbo.RetentionPolicies p WHERE p.Domain=d.Domain AND p.TenantId IS NULL AND p.StoreId IS NULL);
GO
DECLARE @Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));INSERT @Permissions VALUES(N'PlatformOperations.View',N'View platform health queues settings and retention.'),(N'PlatformOperations.Manage',N'Manage workers settings secret references retention and dead letters.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)SELECT 1,Name,Description,1,SYSUTCDATETIME()FROM @Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
INSERT dbo.RolePermissions(RoleId,PermissionId)SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Name IN(N'PlatformOperations.View',N'PlatformOperations.Manage')WHERE r.Scope=1 AND r.IsActive=1 AND r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMOPERATIONSADMIN')AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)VALUES(N'V1.15.0',N'Phase 16 operational settings worker controls leases health retention and audit hardening',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF(SELECT COUNT(*)FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')<>1 THROW 55190,'V1.15.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.OperationalSettings',N'U')IS NULL OR OBJECT_ID(N'dbo.WorkerLeases',N'U')IS NULL OR OBJECT_ID(N'dbo.RetentionPolicies',N'U')IS NULL OR OBJECT_ID(N'dbo.OperationalRetention_Run',N'P')IS NULL THROW 55191,'Phase 16 operational objects are incomplete.',1;
GO
