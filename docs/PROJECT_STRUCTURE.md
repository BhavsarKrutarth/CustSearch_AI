# Project Structure

| Path | Responsibility |
|---|---|
| `src/CustSearch.Domain` | Entities, enums and business invariants |
| `src/CustSearch.Contracts` | Public request/response and paging contracts |
| `src/CustSearch.Application` | Use-case interfaces, authorization names and orchestration contracts |
| `src/CustSearch.Infrastructure` | EF transactional services, Dapper repositories, security and persistence |
| `src/CustSearch.Integrations` | External service/secret adapters |
| `src/CustSearch.API` | HTTP API, JWT/RBAC, SignalR, health, rate limits and Swagger |
| `src/CustSearch.Worker` | Outbox, exports, retention, leases and heartbeats |
| `src/CustSearch.Admin` | Angular Platform/Tenant Admin SPA |
| `src/CustSearch.AI` | FastAPI CCTV normalization, tracking and Demo Mode |
| `tests/CustSearch.UnitTests` | Domain/unit tests |
| `tests/CustSearch.IntegrationTests` | Service/API/database-bound contract tests |
| `tests/CustSearch.Admin.E2E` | Playwright browser workflows |
| `tests/CustSearch.AI.Tests` | Python API/tracking tests |
| `database/09_Upgrade` | Repeat-safe versioned schema upgrades |
| `database/10_TestData` | Deterministic smoke data and exact cleanup (to be created) |
| `planning_document` | Authoritative requirements, phase evidence and resume checkpoint |
| `docs` | Operator/developer catalogs and runbooks |

Dependencies point inward: Domain has no infrastructure dependency; Contracts/Application define
boundaries; Infrastructure implements them; API/Worker compose the executable hosts. Angular and
Python communicate through authenticated HTTP/SignalR boundaries, never direct database calls.
