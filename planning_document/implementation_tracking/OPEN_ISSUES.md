# Open Issues

## Critical

- Phase 18 branch/application drift: live V1.16 contains nine security tables and six procedures. Its
  repeat-safe SQL/verifier were recovered at divergent `origin/AIMainBranch` commit `055b052`, while
  the selected Phase 16 chain contains no Phase 18 application implementation. Blocking; review the
  3-vs-15 commit divergence and integrate deliberately before application work.

## High

- Phase 17 IIS/HTTPS/WebSocket deployment has not been executed. Blocking Phase 17 completion.
- Production Redis topology, authentication/TLS and sustained failover/load behavior remain deployment
  concerns; local two-API-node cross-node SignalR delivery is verified and no longer blocks Phase 16.

## Medium

- Required SQL Server 2022 validation cannot run against the reachable engine, which reports version 17
  with compatibility 160. Blocking environment certification, not local functional regression.
- Physical RTSP and production ONNX calibration remain untested; Demo Mode remains available.

## Low

- Angular production build reports the existing admin-shell style budget exceeded by 61 bytes.
