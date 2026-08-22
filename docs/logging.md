# Logging and correlation

API and Worker diagnostic events use Serilog structured message templates. Minimum levels, sinks and rolling-file retention are configuration driven.

The API accepts `X-Correlation-ID` only when it is 1–64 characters containing ASCII letters, digits, hyphen, underscore or period. Unsafe values are replaced. The accepted/generated value is returned to the caller, assigned to `HttpContext.TraceIdentifier` and attached to request logs.

Do not log passwords, access/refresh tokens, signing keys, connection-string secrets, camera credentials, webhook secrets, biometric embeddings or complete sensitive request bodies. Audit logging is a separate durable business record and will be tenant scoped in the authorization phases.
