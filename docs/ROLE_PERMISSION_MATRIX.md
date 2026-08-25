# Role and Permission Matrix

| Role family | Scope | Typical capabilities | Explicit boundary |
|---|---|---|---|
| PlatformSuperAdmin | platform | tenant lifecycle, plans/billing, reports, operations/audit | no tenant identity claim; support access must be audited |
| PlatformOperationsAdmin | platform | health, queues, worker controls, settings, retention | cannot become tenant user through client TenantId |
| TenantOwner/TenantAdmin/ShopOwner | tenant | tenant setup, stores/users/staff, customers, retail, reports | current tenant only; assigned-store limits still apply when scoped |
| Store manager/custom role | tenant/store | configured operational permissions | cannot grant tenant-wide owner/admin roles outside authority |
| Staff | tenant/store | least-privilege customers/visits/retail/voice/alerts | assigned stores only; no role escalation/settings access |

Permissions are database-authoritative and refreshed on each validated request. The complete stable
names live in `PermissionCatalog`; Angular permission checks hide controls but never authorize API
operations. Phase 18 `Security.*` permissions exist live only and have no source consumer here.
