# Open Issues

## Critical

- Phase 18 branch/application drift: live V1.16 contains nine security tables and six procedures. Its
  repeat-safe SQL/verifier were recovered at divergent `origin/AIMainBranch` commit `055b052`, while
  the selected Phase 16 chain contains no Phase 18 application implementation. Blocking; review the
  3-vs-15 commit divergence and integrate deliberately before application work.

## High

- Phase 17 IIS/HTTPS/WebSocket deployment has not been executed. The complete execution plan is in
  `planning_document/PHASE_17_IIS_HTTPS_WEBSOCKET_DEPLOYMENT_TEST_PLAN.md`; an IIS UAT host, FQDN,
  trusted certificate and approved service identities are required. Blocking Phase 17 completion.
- Production Redis topology, authentication/TLS and sustained failover/load behavior remain deployment
  concerns; local two-API-node cross-node SignalR delivery is verified and no longer blocks Phase 16.

## Medium

- Required SQL Server 2022 validation cannot run against the reachable engine, which reports version 17
  with compatibility 160. Blocking environment certification, not local functional regression.
- Physical RTSP frame capture remains blocked until an authorized stream URL is set only in the
  Python server environment. Secure user-wise preview, continuous capture/reconnect and authenticated frame
  proxy are implemented/tested; continuous ONNX detection, HMAC event publishing and production calibration
  remain pending. Demo Mode remains available.
- Production account recovery still needs a recipient-owned, short-lived, single-use forgot-password/
  invitation token and verified notification delivery. Self-service change password and authorized tenant-admin
  reset are implemented; no hash decoding or committed default credential is permitted.
- Platform tenant creation provisions tenant roles but does not yet create/invite the first Tenant Admin.
  Implement this together with the one-time invitation flow rather than accepting a reusable bootstrap password.

## Low

- Angular production build reports the admin-shell style budget exceeded by 151 bytes; build still passes.
