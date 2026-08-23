# Phase 6 — Shopper Customers, Anonymous Visitors & Smart Profile Implementation Plan

Created: 2026-08-23 (Asia/Kolkata)

Status: Completed

Source of truth: `CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`

Detailed implementation record: `phase_implementation/PHASE_06_SHOPPER_CUSTOMERS.md`

## Objective

Deliver the shopper/customer CRM foundation on top of the completed Phase 5 tenant/store authorization layer: customer management, anonymous visitors, tenant/store-safe customer search, factual smart customer profile data, Angular customer/visitor UI, explicit visitor conversion, database relationships/indexes and cross-tenant/cross-store security.

Phase 6 intentionally does not fabricate household, purchase-history or preference data that belongs to later phases.

## Completion Status

Phase 6 implementation is complete and merged into `AIMainBranch`.

| Sub-phase | Scope | Status |
|---|---|---|
| 6A | Customer Management | Completed |
| 6B | Anonymous Visitors | Completed |
| 6C | Customer Search | Completed |
| 6D | Smart Customer Profile foundation | Completed |
| 6E | Angular Customer UI | Completed |
| 6F | Angular Visitor UI | Completed |
| 6G | Tenant Isolation & Authorization | Completed |
| 6H | E2E, Database, validation and documentation | Completed |

## Implemented Domain / Application Scope

### Customer Management

- `Customer` entity with tenant ownership.
- Customer code, first/last name, mobile, email, notes and active status.
- `CustomerStoreAssignment` for store visibility and primary-store relationship.
- Create, read, update and store-assignment workflows.
- Audit records for customer create/update/store changes.
- Customer codes are unique inside the tenant.

### Anonymous Visitors

- `AnonymousVisitor` entity owned by tenant + store.
- Visitor code, first seen, last seen and active state.
- Trusted visitor create/touch workflow.
- Explicit visitor-to-customer conversion.
- Conversion can create a new customer or link an existing authorized customer.
- Converted visitor becomes inactive and stores conversion metadata.
- Unknown visitors stay anonymous until an authorized explicit conversion occurs.

### Customer Search

- Dapper repository queries backed by stored procedures.
- Search by customer code/name/mobile/email.
- Store filtering.
- Active-only filtering.
- Paging with bounded page sizes.
- Tenant and allowed-store predicates are applied before paging.

### Smart Customer Profile

Phase 6 Smart Profile exposes only factual information already available in the Phase 6 data model:

- Customer identity/contact information.
- Active status.
- Authorized store visibility.
- Converted anonymous-visitor count.
- Last converted visitor seen time.
- Mobile/email availability indicators.

Deferred data is explicitly identified rather than fabricated:

- Households and visit history: Phase 7.
- Purchase history: Phase 8.
- Preferences/interests: later intelligence phase.

## API Implementation

Phase 6 customer/visitor APIs are tenant scoped and permission protected.

Key routes include:

- `GET /api/tenant/customers`
- `GET /api/tenant/customers/{id}`
- `GET /api/tenant/customers/{id}/smart-profile`
- `POST /api/tenant/customers`
- `PUT /api/tenant/customers/{id}`
- `PUT /api/tenant/customers/{id}/stores`
- `GET /api/tenant/visitors`
- `GET /api/tenant/visitors/{id}`
- `POST /api/tenant/visitors`
- `POST /api/tenant/visitors/{id}/touch`
- `POST /api/tenant/visitors/{id}/convert`

Required permissions include:

- `Customers.View`
- `Customers.Create`
- `Customers.Edit`
- `Visitors.View`
- `Visitors.Convert`

TenantId is never accepted from Angular customer/visitor request models as an authorization input.

## Database Implementation

Primary upgrade:

`database/09_Upgrade/V1.5.0_Phase6_ShopperCustomers.sql`

PowerShell runner:

`database/run-phase6.ps1`

Direct SQL Server / SSMS runner:

`database/run-phase6.sql`

Canonical database:

`database/CustSearchAi.sql`

### Phase 6 Tables

- `Customers`
- `CustomerStoreAssignments`
- `AnonymousVisitors`

### Required Stored Procedures

- `Customer_Search`
- `AnonymousVisitor_Search`

### Required Relationships / Isolation

- Customer rows reference the owning tenant.
- Customer-store assignments use tenant-safe customer/store relationships.
- Anonymous visitors use tenant-safe store relationships.
- Visitor conversion uses a tenant-safe customer relationship.
- Composite tenant keys prevent cross-tenant links even if invalid IDs are supplied.

### Important Indexes

- Unique tenant/customer code.
- Unique `(TenantId, Id)` customer key for composite relationships.
- Customer active/mobile/email lookup indexes.
- Customer-store tenant/store lookup index.
- Single primary-store filtered unique index.
- Unique tenant/store/visitor code.
- Visitor active/last-seen lookup index.
- Converted-customer visitor lookup index.
- Unique `(TenantId, Id)` store key required by tenant-safe composite relationships.

### Database Rules

- Version ledger: `V1.5.0`.
- SQL Server 2022 deterministic SET options are enabled for filtered indexes.
- Upgrade scripts are repeat-safe/idempotent.
- Version rows are inserted once.
- Existing Phase 5 data is preserved.
- No EF Core production migration workflow is introduced.

## Angular Implementation

### Customer UI

- Customer list/search.
- Create customer.
- Customer detail.
- Edit customer.
- Store visibility/primary-store management.
- Smart Profile view.
- Typed customer API service.

Routes include:

- `/customer-admin/customers`
- `/customer-admin/customers/:id`

### Visitor UI

- Anonymous visitor list/search.
- Store filtering.
- Explicit visitor conversion flow.
- Link existing customer or create a new customer during conversion.
- Typed visitor API service.

Route:

- `/customer-admin/visitors`

## Security / Privacy Rules

- TenantId is derived from authenticated server context.
- StoreIds are authoritative server-side assignments.
- Store-scoped users cannot query or mutate customers outside their store visibility.
- Store-scoped users cannot access anonymous visitors from unauthorized stores.
- Customer store assignment updates preserve inaccessible assignments instead of silently deleting them.
- Cross-tenant customer/store/visitor relationships are rejected at both service and database levels.
- Browser payloads do not control tenant ownership.
- Unknown people remain anonymous visitors until explicit conversion.
- No Aadhaar, PAN, social identity lookup or external identity discovery is performed.
- No face embedding or biometric identity field is introduced in the Phase 6 customer/visitor tables.

## Automated Validation

Phase 6 completion included validation across the full application stack:

- .NET build with zero build errors.
- Unit tests.
- Integration tests.
- API contract/security tests.
- Angular `npm ci` using the checked-in lockfile.
- Angular lint.
- Angular unit tests.
- Angular production build.
- Playwright Phase 5 regression + Phase 6 customer/visitor E2E.
- Python Ruff and baseline pytest regression.
- SQL structure validation.
- SQL Server 2022 upgrade execution.
- V1.5.0 repeat-apply/idempotency validation.
- Tenant/store stored-procedure predicate validation.
- Fresh canonical `database/CustSearchAi.sql` installation validation.

Final Phase 6 regression evidence included 40 unit tests, 64 integration tests, 41 Angular tests, 15 Playwright tests and 3 Python tests passing during the Phase 6 validation cycle.

## Completion Summary

Phase 6 is complete. The system now has tenant-safe shopper customer management and store-bound anonymous visitor management, Dapper/stored-procedure search, factual Smart Profile data, Angular Customer/Visitor workflows, explicit audited visitor conversion and defense-in-depth cross-tenant/cross-store authorization.

Phase 6 database state is represented by V1.5.0 and is maintained in both the versioned upgrade script and canonical `database/CustSearchAi.sql`.

Phase 6 was merged into `AIMainBranch` and becomes the baseline for the next planned phase.
