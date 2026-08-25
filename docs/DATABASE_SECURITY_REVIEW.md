# Database Security Review

Observed run: `CUSTSEARCH_SMOKE_20260825_001`.

## Controls verified in source and live schema

- Application requests derive TenantId from the validated server identity; tenant controllers use
  tenant policies and reject client TenantId fields.
- Store-bound services validate StoreId against authoritative assignments before querying or writing.
- Search/report procedures expose tenant/store scope parameters and apply scope before paging or aggregation.
- Composite tenant/store/customer foreign keys constrain many cross-tenant relationship mistakes.
- HMAC/CCTV inbound flows have idempotency, clock-skew, size and service-credential boundaries.
- Audit update protection, secret-reference masking, worker leases and bounded retention batches exist.
- Recognition is consent-gated and human review remains separate from identity inference.
- Phase 18 live constraints require human confirmation metadata for confirmed loss states.

## Findings

| Severity | Finding | Evidence | Required action |
|---|---|---|---|
| Critical | Phase 18 live schema has no matching application chain on this branch. | Live `V1.16.0`; repeat-safe SQL/verifier recovered at `origin/AIMainBranch` commit `055b052`, but no application flow exists in the selected chain. | Integrate the divergent branch deliberately, verify source/live equivalence, then implement the reviewed flow. Never downgrade live DB. |
| High | Phase 18 APIs, service authorization, evidence access, ingestion replay controls and tests are absent in selected source. | No Security controller/service/Admin routes/Python events in branch. | Implement only after Phase 17 and source/schema reconciliation. |
| High | Production reverse-proxy/HSTS/AllowedHosts/IIS evidence is incomplete. | `AllowedHosts` remains wildcard; no deployment smoke evidence. | Phase 17 must add environment-specific hardening and IIS validation. |
| Medium | SQL Server 2022-specific validation cannot be executed locally. | Local engine `17.0.1000.7`; Docker unavailable. | Execute current scripts on an approved SQL Server 2022 environment. |
| Medium | Redis multi-node/SignalR backplane behavior is not environment-tested. | Redis disabled locally; fail-closed readiness tests pass. | Validate approved Redis topology and cross-node reconnect/delivery. |
| Medium | Canonical SQL contains historical machine identity in old seed ledger rows. | `AppliedBy` values in canonical data. | Replace machine identity with portable bootstrap metadata in a separately validated canonical cleanup; avoid broad unrelated rewrite. |
| Low | Several old phase planning files report stale progress. | Phase 5–10 files conflict with later tracker/test evidence. | Reconcile documentation without changing evidence history. |

## Privacy conclusions

- The selected code keeps co-visit and verified household concepts separate.
- Anonymous tracking is the CCTV default; recognition is not required for ordinary tracking.
- No external face database or automatic biometric watchlist is present in the selected source.
- Evidence, recognition templates, webhook secrets and export signing keys must remain outside logs and Git.
- Phase 18 must use reviewable states such as candidate/under-review/false-positive/confirmed loss;
  AI output alone is never proof or a “thief” label.

## Database-change rule

Only versioned, repeat-safe scripts may change schema. No EF migration, runtime schema creation,
database reset, truncation, or broad cleanup is authorized. Destructive fresh-install checks must use
a uniquely named disposable database and must verify exact cleanup.
