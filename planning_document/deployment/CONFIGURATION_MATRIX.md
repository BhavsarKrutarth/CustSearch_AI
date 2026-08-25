# Production Configuration Matrix

All secrets must come from environment variables or the deployment secret store. Values below are names, not credentials.

| Component | Required production values | Notes |
|---|---|---|
| API/Worker SQL | `ConnectionStrings__CustSearchDatabase` | Encrypted SQL Server 2022 connection; do not use `TrustServerCertificate=True` unless the certificate decision is explicitly approved |
| JWT | `Jwt__SigningKey`, issuer/audience/lifetimes | Use a high-entropy secret and a rotation procedure; refresh token remains HttpOnly cookie |
| Redis | `Redis__Enabled`, `Redis__ConnectionString`, `Redis__SignalRBackplaneEnabled` | Backplane requires Redis enabled; health degrades without replacing SQL readiness |
| CCTV inbound | `CctvServices__<id>__Secret`, tenant/store allow-list | Never place camera credentials/RTSP URLs in `SystemSettings` or logs |
| Recognition | `RecognitionSecurity__Enabled`, key reference and protected key value/provider | Production enablement fails closed without valid 256-bit key material |
| Integration secrets | `IntegrationSecrets__*` | Store only opaque references in SQL |
| Worker retention | `OperationalRetention__*`, `ReportExports__*` | Review legal/privacy retention before deployment |
| Logs | `Serilog__*` | Diagnostic retention is separate from business audit retention |

`SystemSettings` contains non-secret operational policy with platform/tenant/store precedence. It must never become a secret store.
