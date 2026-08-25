# Project Component and Comment Catalog

This is the project-wise functional guide requested after the validation run. It records each
project's purpose, primary boundaries, and the comments maintainers should expect around sensitive
or non-obvious logic. Comments explain business/security intent rather than restating syntax.

## CustSearch.Domain

Owns entities, enums, state transitions and invariants without HTTP or database concerns. Important
rules include tenant/store ownership, invoice/payment state, verified household membership,
anonymous tracking, recognition consent, worker leases and retention state. Domain comments must
call out invariants such as co-visit not implying family and AI observation not proving loss.

Dependencies: none outward. Persistence maps these entities in Infrastructure.

## CustSearch.Contracts

Owns typed API request/response models, paging/filter contracts and public enum shapes. Request DTOs
must not make TenantId authoritative. Comments are most valuable where fields are deliberately
omitted, store identifiers require server authorization, or unknown JSON fields are rejected.

Dependencies: shared by API and Angular-equivalent client models; no database access.

## CustSearch.Application

Defines use-case interfaces and orchestration contracts: authentication, tenant operations,
customers, visits, billing, preferences, alerts, integrations, cameras, recognition, reports and
operations. Interface comments should identify caller, permission/scope requirement, transaction or
idempotency expectation and sensitive result handling.

Dependencies: Domain/Contracts abstractions; implemented mainly by Infrastructure.

## CustSearch.Infrastructure

Implements EF transactional services, Dapper repositories/procedure calls, JWT/authentication,
template protection, local export storage, outbox processing, worker leases and retention. This is
the main tenant-boundary enforcement layer. Significant comments must explain server-derived scope,
composite ownership checks, atomic state transitions, replay/idempotency, secret masking, evidence
privacy and cleanup concurrency.

Notable audited comment improvements in this run:

- readiness checks now explain SQL authority, optional Redis fail-closed behavior and REST recovery;
- export stream sharing explains why expiry cleanup may unlink an already-authorized download;
- strict JSON contract test explains serializer reuse and unknown-field rejection.

## CustSearch.Integrations

Resolves external service references/secrets without placing secret values in business payloads or
logs. Inbound HMAC/CCTV authentication and outbound retry/dead-letter behavior are orchestrated with
Infrastructure/API/Worker. Comments should state rotation source, replay window and non-logging rule.

## CustSearch.API

Hosts controllers, JWT/RBAC policies, exception mapping, correlation IDs, rate limits, Swagger,
health checks and SignalR. Controllers remain thin. Authorization comments must state platform vs
tenant scope and why StoreId is revalidated. Internal ingestion comments must cover service auth,
clock/replay/body-size checks and camera ownership.

Current gap: production forwarded headers, HSTS, AllowedHosts and IIS validation are Phase 17 work.

## CustSearch.Worker

Runs notification/integration/export/retention processing and operational heartbeats. Leases prevent
duplicate concurrent processing; outbox operations are idempotent and cancellation enables graceful
shutdown. Worker comments should explain lease acquisition/release, retry bounds, poison/dead-letter
handling and authoritative database state.

## CustSearch.Admin

Angular Admin provides guarded Platform Admin and Customer Admin routes, typed API services,
permission-aware navigation/forms, SignalR reconnect and REST state recovery. UI state is never an
authorization boundary. Comments are required around token refresh single-flight behavior, route
guards, realtime de-duplication and requester-bound export downloads.

## CustSearch.AI

FastAPI normalizes anonymous CCTV detections, offers deterministic Demo Mode and protects service
endpoints with an environment-supplied key. Python does not receive SQL credentials and does not
make final identity/theft decisions. Docstrings state camera-independent liveness, correlation-ID
sanitization, anonymous-first behavior and production Demo Mode rejection.

Current gap: Phase 18 pickup/put-back/POS-correlation observation schemas and scenario tests do not
exist in the selected source.

## Test projects

- `CustSearch.UnitTests`: domain rules, options, tokens, parser/risk-independent logic.
- `CustSearch.IntegrationTests`: EF/Dapper services, API contracts, auth/scope, privacy, readiness.
- `CustSearch.Admin.E2E`: Playwright role/route/workflow behavior with controlled API fixtures.
- `CustSearch.AI.Tests`: Python normalization, API-key, Demo Mode and tracking scenarios.

Tests must describe the business/security outcome in their names. A passing assertion is not a
substitute for comments in non-obvious production code, and documentation never substitutes for an
executed negative test.
