# Database Phase Mapping

| Phase | Database domain | Backend/frontend consumer | Test evidence |
|---:|---|---|---|
| 1-4 | versions, settings, tenant/auth/RBAC/platform administration | API/Admin foundation | phase reports + current regression |
| 5-7 | stores/staff/categories, customers/visitors, households/visits | tenant operations and Admin pages | integration/Angular/E2E/live smoke |
| 8-10 | products/retail, platform billing, preferences/voice | domain services/API/Admin | integration/Angular/E2E/live smoke |
| 11-12 | alerts/realtime and integrations/outboxes | API/Worker/SignalR/Admin | integration/E2E; Redis external blocked |
| 13-14 | camera/tracking and consent/recognition | API/Admin/Python | integration/E2E/Python Demo Mode |
| 15 | reports/export jobs | stored procedures/Dapper/API/Worker/Admin | integration/E2E/export tests |
| 16 | operations/leases/retention/health | API/Worker/Admin | live runner/verifier + regression |
| 17 | no schema planned | hardening/docs/deployment | local green; IIS blocked |
| 18 | security incident objects live-only | no selected-branch consumers | blocked by provenance drift |

The complete per-requirement mapping is `../ALL_PHASE_IMPLEMENTATION_MATRIX.md`.
