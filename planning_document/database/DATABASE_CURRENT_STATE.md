# CustSearch_AI Live Database Current State

Last verified: 2026-08-25 (Asia/Kolkata)

## Connection evidence

- Configured endpoint: `KRUTARTH-BHAVSA`
- SQL-reported server: `DESKTOP-K08UK5F`
- Database: `CustSearch_AI`
- Authentication: Windows Integrated Security
- Encryption: enabled with trusted server certificate
- Engine: `17.0.1000.7`, Standard Developer Edition (64-bit)
- Database compatibility level: `160`
- Connectivity: PASS using `System.Data.SqlClient` and the configured connection string
- SQL Server service: `MSSQLSERVER` running
- SQL Server Agent and Browser: stopped; neither is required for the current local application path

The requested target is SQL Server 2022, but this machine currently reports SQL Server major version 17 (2025). Compatibility level 160 matches SQL Server 2022 semantics; deployment scripts still need a real SQL Server 2022 gate before production readiness is claimed.

## Object totals

| Object | Live count |
|---|---:|
| User tables | 75 |
| Views | 0 |
| Stored procedures | 75 |
| Functions | 0 |
| Indexes (excluding heaps) | 257 |
| Foreign keys | 168 |
| Primary keys | 75 |
| Unique constraints | 2 |
| Default constraints | 135 |

Uniqueness is primarily implemented with named unique indexes rather than `UQ` constraints.

## Tables and important relationships

All current user tables have primary keys. Tenant-owned roots consistently carry `TenantId`; store-owned operational roots also carry `StoreId` and use composite tenant/store foreign keys where required.

| Domain | Tables | Major relationships / important columns |
|---|---|---|
| Versioning and audit | `DatabaseVersions`, `AuditLogs`, `AuthenticationEvents` | Version ledger; optional tenant/store/user audit scope; UTC and correlation fields |
| Identity/RBAC | `Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`, `RefreshTokens`, `UserStoreAssignments` | Tenant/platform scope, security stamp, hashed refresh lifecycle, authoritative user/store grants |
| Tenants/platform | `Tenants`, `Stores`, `SubscriptionPlans`, `TenantSubscriptions`, `TenantQuotaOverrides`, `TenantUsageSnapshots` | Tenant lifecycle, plan/usage/quota and store ownership |
| Staff/store setup | `StaffProfiles`, `StaffShifts`, `StaffPresenceSessions`, `ProductCategories`, `StoreVoiceCommandSettings`, `StoreVoiceCommandAliases`, `StoreVoiceCommandRuntimeSettings` | Tenant/store-safe staff operations, taxonomy and configurable voice settings |
| Customers/visitors | `Customers`, `CustomerStoreAssignments`, `AnonymousVisitors` | Customer visibility via explicit store assignment; explicit visitor conversion |
| Households/visits | `Households`, `HouseholdMembers`, `VisitParties`, `VisitPartyMembers`, `CustomerVisits` | Verified household truth remains separate from co-visit evidence |
| Retail | `Products`, `ProductStoreAvailabilities`, `RetailInvoices`, `RetailInvoiceItems`, `RetailInvoicePayments`, `RetailInvoiceParticipants`, `RetailInvoiceItemAttributions` | Tenant/store invoice facts, snapshots, payments and explicit spend attribution |
| Platform billing | `PlatformInvoices`, `PlatformInvoiceItems`, `PlatformPayments` | Separate from tenant retail billing |
| Preferences/voice | `ProductCategoryAliases`, `PreferenceWeightVersions`, `CustomerPreferenceSignals`, `CustomerPreferenceScores`, `HouseholdPreferenceTags`, `VoiceCommandSessions` | Factual signals, versioned scoring and confirmation-controlled voice actions |
| Alerts/realtime | `Alerts`, `RealtimeEvents`, `NotificationOutbox` | Durable tenant/store alert recovery and delivery outbox |
| Integrations | `IntegrationConfigurations`, `IntegrationInboundEvents`, `IntegrationOutbox`, `IntegrationDeliveryLogs` | Tenant config, HMAC/idempotency receipts, retry/outbox and delivery audit |
| CCTV/tracking | `Cameras`, `CameraZoneConfigurations`, `PersonTrackSessions`, `CameraTrackHandoffs`, `CameraOperationalEvents` | Tenant/store/camera ownership, anonymous tracks, zones and idempotent metadata events |
| Consent recognition | `CustomerRecognitionConsents`, `BiometricTemplates`, `RecognitionCandidates` | Consent-bound encrypted derived templates and human review |
| Reports/exports | `ReportExportJobs`, `ReportExportEvents` | Tenant-null platform or tenant scope, requester ownership, leased Worker lifecycle, opaque artifact metadata, expiry and durable requester events |
| Operational platform | `SystemSettings`, `WorkerHeartbeats` | Typed platform/tenant/store precedence, audited updates, durable worker state and health timestamps |
| Retail security | `SecurityRules`, `SecurityIngestionRequests`, `SecurityObservations`, `SecurityIncidents`, `SecurityIncidentItems`, `SecurityIncidentEvidence`, `SecurityIncidentActions`, `SecurityNotificationDeliveries`, `SecurityPaymentCorrelations` | Versioned rules, replay-safe signed observations, reviewable human incident workflow, opaque evidence and POS correlation; no watchlist |

## Stored procedures

The original 47 Phase 1-14 procedures remain. Phases 15-16 add report/export and operations procedures. Phase 18 currently adds six `Security*` procedures for replay-safe observation ingest, versioned rules, scoped incident reads and human transitions.

## Index and constraint observations

- High-frequency tenant/store/date paths have named indexes across customers, visitors, visits, invoices, alerts, integrations, cameras and recognition.
- Tenant-safe composite unique indexes exist for the main roots (`TenantId` plus business key or `Id`).
- All 168 foreign keys were enabled when inspected.
- High-volume procedures use tenant/store predicates before aggregation or paging.
- Phase 15 queue claiming, requester history, event relay, and expiration paths have dedicated indexes.

## Seed/configuration data

| Table | Rows |
|---|---:|
| `DatabaseVersions` | 17 (`V1.0.0` through `V1.16.0`) |
| `Permissions` | 125 |
| `Roles` | 5 |
| `RolePermissions` | 66 |
| `SubscriptionPlans` | 1 |
| `Users`, `Tenants`, `Stores` | 0 in the inspected local database |
| `SystemSettings` | 47 platform defaults |
