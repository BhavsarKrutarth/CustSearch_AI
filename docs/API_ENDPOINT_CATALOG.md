# API Endpoint Catalog

| Area | Base route | Scope/security |
|---|---|---|
| Authentication | `/api/auth` | anonymous login/refresh/logout; authorized `/me` and `/change-password`; rotating secure cookie and all-session revocation |
| System/health | `/api/system`, `/health/live`, `/health/ready` | public operational metadata is minimal; readiness probes dependencies |
| Platform tenancy | `/api/platform` | platform policy plus exact tenant/plan/audit permissions |
| Platform operations | `/api/platform/operations` | platform operations view/manage permissions |
| Platform reports | `/api/platform/reports` | platform report/export permission and requester binding |
| Platform billing | `/api/platform/billing` | platform billing permissions; separate financial domain |
| Tenant setup | `/api/tenant` via `TenantOperationsController` | current tenant and authoritative stores; user password reset requires `TenantUsers.Edit` plus target visibility |
| Customers/visitors | `/api/tenant/customers`, `/api/tenant/visitors` | tenant/store scoped; TenantId rejected from payload |
| Households/visits | `/api/tenant/households`, visits, visit-parties | explicit relationship/privacy rules |
| Retail | `/api/tenant/products`, retail invoice/payment/report routes | server totals and store authorization |
| Tenant platform billing | `/api/tenant/platform-billing` | current tenant read-only billing |
| Preferences/voice | `/api/tenant` preference/voice routes | configured trigger + server confirmation |
| Alerts | `/api/tenant/alerts` | tenant/store recovery and acknowledgement permissions |
| SignalR | `/hubs/alerts` | JWT plus server-assigned tenant/store groups |
| Integrations | `/api/tenant/integrations`, `/api/integrations/inbound` | tenant permissions or HMAC/replay boundary |
| Cameras | `/api/tenant/cameras`, `/api/internal/cctv/events` | tenant permissions or service auth/camera ownership |
| Recognition | `/api/tenant/recognition` | consent and recognition permissions |
| Tenant reports | `/api/tenant/reports` | tenant/store/report permission; requester-bound exports |
| Retail security | not present | Phase 18 source is missing; live schema is not an API |

Development Swagger is exposed at `/swagger`. Production Swagger exposure is not assumed.
