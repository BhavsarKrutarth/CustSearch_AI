# Worker Job Catalog

| Job | Host/service | Coordination | Failure behavior |
|---|---|---|---|
| Notification outbox | API hosted dispatcher | idempotency/status claims | retry then dead letter; REST remains authoritative |
| Integration outbox | Worker | database lease + bounded claim | retry/backoff/dead letter; secrets not logged |
| Export jobs | Worker | export lease + bounded batch | progress/failure/expiry; requester-bound artifact |
| Retention | Worker | single database lease | bounded audited deletes per enabled policy |
| Heartbeat | Worker | instance/worker key | fail-closed readiness for current not-ready heartbeat |

All loops accept shutdown cancellation. Phase 18 escalation/evidence/re-correlation jobs are absent.
