# Database Object Catalog

Observed 2026-08-25 against `CustSearch_AI` using encrypted Windows Integrated Security.
This is a live inventory, not an inference from script filenames.

## Server and schema summary

| Item | Observed value |
|---|---|
| Connected server reported by SQL | `DESKTOP-K08UK5F` / configured alias `KRUTARTH-BHAVSA` |
| Login | `KRUTARTH-BHAVSA\Krutarth Bhavsar` |
| Engine | `17.0.1000.7` |
| Compatibility level | 160 |
| User tables | 81 |
| Views | 0 |
| Functions | 0 |
| Stored procedures | 75 |
| Indexes (including PK/unique) | 269 |
| Foreign keys | 176 |
| Check constraints | 196 |
| Default constraints | 137 |

The requested production target is SQL Server 2022. Compatibility is set to 160, but the local
engine is version 17; SQL Server 2022-specific execution remains a separate blocked environment gate.

## Version ledger

Exactly one row was observed for each version `V1.0.0` through `V1.16.0`. The selected branch's
canonical script ends at `V1.15.0`; V1.16 SQL was recovered on divergent `origin/AIMainBranch`
commit `055b052` and is not yet integrated into this branch. The live Phase 18 foundation is documented
as code/schema drift. It must not be removed or treated as source-complete.

## Tables by domain

| Domain / phase | Tables |
|---|---|
| Foundation/auth/RBAC | `DatabaseVersions`, `SystemSettings`, `Tenants`, `Users`, `RefreshTokens`, `AuthenticationEvents`, `Roles`, `Permissions`, `RolePermissions`, `UserRoles` |
| Platform tenancy | `SubscriptionPlans`, `TenantSubscriptions`, `TenantQuotaOverrides`, `TenantUsageSnapshots`, `AuditLogs` |
| Stores/staff/taxonomy | `Stores`, `UserStoreAssignments`, `StaffProfiles`, `StaffShifts`, `StaffPresenceSessions`, `ProductCategories`, `StoreVoiceCommandSettings`, `StoreVoiceCommandAliases`, `StoreVoiceCommandRuntimeSettings` |
| Customers/visitors | `Customers`, `CustomerStoreAssignments`, `AnonymousVisitors` |
| Households/visits | `Households`, `HouseholdMembers`, `CustomerVisits`, `VisitParties`, `VisitPartyMembers` |
| Retail billing | `Products`, `ProductStoreAvailabilities`, `RetailInvoices`, `RetailInvoiceItems`, `RetailInvoicePayments`, `RetailInvoiceParticipants`, `RetailInvoiceItemAttributions` |
| Platform billing | `PlatformInvoices`, `PlatformInvoiceItems`, `PlatformPayments` |
| Preferences/voice | `CustomerPreferenceSignals`, `CustomerPreferenceScores`, `HouseholdPreferenceTags`, `PreferenceWeightVersions`, `ProductCategoryAliases`, `VoiceCommandSessions` |
| Alerts/realtime | `Alerts`, `NotificationOutbox`, `RealtimeEvents` |
| Integrations | `IntegrationConfigurations`, `IntegrationInboundEvents`, `IntegrationOutbox`, `IntegrationDeliveryLogs` |
| CCTV/tracking | `Cameras`, `CameraZoneConfigurations`, `CameraOperationalEvents`, `PersonTrackSessions`, `CameraTrackHandoffs` |
| Consent/recognition | `CustomerRecognitionConsents`, `BiometricTemplates`, `RecognitionCandidates` |
| Reports/exports | `ReportExportJobs`, `ReportExportEvents` |
| Operations | `OperationalSettings`, `OperationalSecretReferences`, `WorkerControls`, `WorkerLeases`, `WorkerHeartbeats`, `RetentionPolicies`, `RetentionRuns` |
| Live-only Phase 18 security | `SecurityRules`, `SecurityIngestionRequests`, `SecurityObservations`, `SecurityIncidents`, `SecurityIncidentItems`, `SecurityIncidentEvidence`, `SecurityIncidentActions`, `SecurityNotificationDeliveries`, `SecurityPaymentCorrelations` |

All operational and business tables that require ownership expose `TenantId`; store-bound records
also expose `StoreId`. Bridge tables use composite tenant-safe foreign keys where implemented.
Security tables were inspected live but have no corresponding source implementation on this branch.

## Stored procedures by domain

| Domain | Procedures |
|---|---|
| Auth/tenant/platform | `User_GetByIdForTenant`, `UserAuthorization_GetForScope`, `Tenant_ProvisionDefaultRoles`, `Tenant_GetDetailSummary`, `Tenant_GetUsageSummary`, `TenantDashboard_GetSummary`, `Store_Search`, `Staff_Search`, `SystemSetting_List`, `SystemSetting_Upsert` |
| Customers/households/visits | `Customer_Search`, `AnonymousVisitor_Search`, `Household_Search`, `Household_GetDetail`, `CustomerVisit_Search`, `VisitParty_Search`, `VisitParty_GetDetail` |
| Retail | `Product_Search`, `RetailInvoice_Search`, `RetailInvoice_GetDetail`, `RetailSalesSummary_Get`, `RetailSalesByProduct_Get`, `RetailSalesByCategory_Get`, `RetailPaymentSummary_Get`, `CustomerPurchaseHistory_Get`, `HouseholdPurchaseSummary_Get` |
| Platform billing | `PlatformBilling_Plan_List`, `PlatformBilling_Subscription_List`, `PlatformBilling_Invoice_List`, `PlatformBilling_Invoice_Get`, `PlatformBilling_Payment_List`, `TenantPlatformBilling_Summary_Get` |
| Preferences | `CustomerPreference_Get`, `HouseholdPreference_Get`, `PreferenceAudit_Search`, `PreferenceWeight_GetActive`, `ProductCategoryAlias_Search` |
| Alerts/realtime | `Alert_Search`, `AlertRecovery_Get`, `NotificationOutbox_Claim`, `NotificationOutbox_Metrics` |
| Integrations | `Integration_Search`, `IntegrationDeliveryLog_Search`, `IntegrationOutbox_Claim`, `IntegrationOutbox_ManualRetry` |
| CCTV/recognition | `Camera_Search`, `PersonTrack_Search`, `RecognitionCandidate_Search`, `RecognitionConsent_Search` |
| Reports/exports | `TenantReport_Get`, `PlatformReport_Get`, `ReportAudit_Write`, `ReportExportJob_Create`, `ReportExportJob_List`, `ReportExportJob_Get`, `ReportExportJob_Claim`, `ReportExportJob_Progress`, `ReportExportJob_Complete`, `ReportExportJob_Fail`, `ReportExportJob_Expire`, `ReportExportJob_ArtifactDeleted`, `ReportExportRequesterScope_Get`, `ReportExportEvent_Claim`, `ReportExportEvent_Complete`, `ReportExportEvent_Fail` |
| Operations/audit | `AuditLog_Search`, `SystemHealth_Get`, `WorkerHeartbeat_Upsert`, `OperationalRetention_Run` |
| Live-only Phase 18 | `SecurityRule_List`, `SecurityRule_CreateVersion`, `SecurityObservation_Ingest`, `SecurityIncident_Search`, `SecurityIncident_Get`, `SecurityIncident_Transition` |

There are no user views or SQL functions in the live database. This is not automatically a gap;
the architecture uses stored procedures for search/report-heavy reads and EF Core for allowed
transactional entity operations.

## Important relationships and keys

- Tenant-owned records reference `Tenants`; store-bound records reference a store owned by the same
  tenant through composite/validated relationships where applicable.
- Household membership links explicit Customers only. Visit-party membership separately represents
  co-visit evidence and enforces an identity choice between Customer and Anonymous Visitor.
- Retail invoices/items/payments/attributions are separate from platform invoices/payments.
- Recognition templates/candidates are linked to consent and tenant/store/customer scope.
- Export jobs are requester- and tenant-bound; export events track asynchronous delivery.
- Security incidents link store, optional visit/track/customer, items, evidence, actions, notification
  deliveries and payment correlations. Human confirmation constraints are present live.

## Seed/configuration rows observed

| Table | Rows | Meaning |
|---|---:|---|
| `DatabaseVersions` | 17 | V1.0.0 through V1.16.0 |
| `Permissions` | 127 | platform/tenant/security permission catalog |
| `Roles` | 5 | platform role templates; no tenant exists yet |
| `RolePermissions` | 70 | seeded platform grants |
| `SubscriptionPlans` | 1 | baseline plan |
| `SystemSettings` | 47 | configuration catalog |
| `WorkerControls` | 5 | known worker types, initially enabled |
| `RetentionPolicies` | 7 | default operational retention domains |
| `WorkerHeartbeats` | 1 | prior local worker heartbeat |

All other business tables had zero rows at inventory time. No smoke tenant had yet been inserted.

## Validation performed

- Phase 16 upgrade ran twice through `database/run-phase16.ps1`.
- Phase 16 verifier passed.
- `DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS` passed.
- Canonical V1.15 fresh install passed in an isolated database and the exact disposable database was dropped.
- Existing `CustSearch_AI` was not dropped, recreated, truncated, or downgraded.
