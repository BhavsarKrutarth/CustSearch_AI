# Postman

Use `CustSearch_AI.postman_collection.json` with `CustSearch_AI.local.postman_environment.json`.

The committed environment is deliberately secret-free. Set `userName` and `password` only in your local Postman current values; never save/export them back to the repository. For a platform login keep `tenantCodeJson` as `null`; for a tenant login set it to a quoted JSON value such as `"TENANT-CODE"`.

Run the Health folder first, then Login. The login test stores the short-lived access token only in the active collection runtime. The refresh token remains an HttpOnly cookie and is never copied into an environment variable.

Platform and Tenant folders require different authorized sessions. Tenant requests intentionally never contain `TenantId`; store filters remain subject to server-derived authorization.
