# Phase 12 — Integrations Implementation Plan

**Status:** Completed  
**Branch:** `phase12-integrations`  
**Validated baseline:** `AIMainBranch` at `b851c467293894c6c00e6ad67fcfcf28e776f853`  
**Database target:** SQL Server 2022 / V1.11.0

## Baseline gate

Phase 11 pull request 13 is merged. Its final branch head `b129eaab549836b88ac71824703cc2c5acfda59c`, green Phase 6–11 workflows, V1.10.0 canonical schema and `PHASE_11_TEST_REPORT.md` were re-verified before Phase 12 branching.

## Security and architecture rules

1. Database/API/UI contain only opaque credential and signing-secret references; secret values resolve from environment/vault-backed configuration at runtime.
2. Inbound integrations authenticate exact raw bytes with HMAC-SHA256 over signed tenant scope, timestamp, provider event ID, idempotency key and body.
3. Integration ID or an obscure URL is never treated as authentication.
4. Signed timestamps use bounded clock skew; provider event and idempotency uniqueness prevent replay and duplicate mutation.
5. Request body size, JSON envelope schema, correlation ID and per-integration rate limiting are enforced before persistence.
6. Outbound HTTP never runs inside a business transaction. Only committed outbox rows are delivered by the worker.
7. Retry uses bounded exponential delay; permanent 4xx responses and exhausted transient attempts dead-letter.
8. Manual retry is tenant-scoped, authorized and audited.
9. Delivery logs store correlation/provider/direction/status/duration/status-code/error category, never secret values or full inbound payloads.
10. Configured destinations require public non-loopback HTTPS and are rechecked at delivery to reduce SSRF risk.
11. Production database deployment is repeat-safe standalone T-SQL without EF migrations.

## Implementation slices

- 12A: tenant integration configuration, masked response hints and reference rotation with signing grace.
- 12B–12D: HMAC inbound endpoint, signed tenant scope, validation, size/rate limits, replay/idempotency receipt.
- 12E–12G: outbound outbox, generic configured HTTPS adapter, worker retry/dead-letter/manual retry and payload-free delivery history.
- 12H: invalid signature, expired timestamp, replay, wrong tenant, oversize, invalid JSON, rate policy, duplicate and rotation tests.
- 12I: Angular Integration Settings, connection/webhook status, masked hints, delivery history and retry/rotation controls.
- 12J: full .NET, Angular, Playwright, Python, security and SQL Server regression.

## Completion rule

Satisfied on 2026-08-24. The complete evidence is recorded in `planning_document/PHASE_12_TEST_REPORT.md`.
