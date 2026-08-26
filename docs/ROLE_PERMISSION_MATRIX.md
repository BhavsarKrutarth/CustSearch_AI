# Role and Permission Matrix

| Role family | Scope | Typical capabilities | Explicit boundary |
|---|---|---|---|
| PlatformSuperAdmin | platform | tenant lifecycle, plans/billing, reports, operations/audit | no tenant identity claim; support access must be audited |
| PlatformOperationsAdmin | platform | health, queues, worker controls, settings, retention | cannot become tenant user through client TenantId |
| TenantOwner/TenantAdmin/ShopOwner | tenant | tenant setup, stores/users/staff, customers, retail, reports | current tenant only; `TenantUsers.Edit` may reset another visible user but cannot bypass own current-password check |
| Store manager/custom role | tenant/store | configured operational permissions | cannot grant tenant-wide owner/admin roles or reset a user outside authoritative store visibility |
| Staff | tenant/store | least-privilege customers/visits/retail/voice/alerts | assigned stores only; no role escalation/settings access |

Permissions are database-authoritative and refreshed on each validated request. The complete stable
names live in `PermissionCatalog`; Angular permission checks hide controls but never authorize API
operations. Phase 18 `Security.*` permissions exist live only and have no source consumer here.
Every authenticated identity may change its own password through `/api/auth/change-password`; this is
identity verification, not a role grant. Successful self-change or administrator reset revokes all sessions.
