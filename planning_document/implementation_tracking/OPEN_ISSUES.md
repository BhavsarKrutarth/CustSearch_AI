# Open Issues

## Critical

- Phase 18 source/live drift: live V1.16 contains nine security tables and six procedures while the
  selected Phase 16 source chain contains no Phase 18 implementation. Blocking; recover provenance
  and create a reviewed repeat-safe script before implementation or canonical update.

## High

- Phase 17 IIS/HTTPS/WebSocket deployment has not been executed. Blocking Phase 17 completion.
- Redis multi-node SignalR/backplane behavior has no configured validation topology. Blocking universal
  Phase 16/17 completion.

## Medium

- Required SQL Server 2022 validation cannot run against the reachable engine, which reports version 17
  with compatibility 160. Blocking environment certification, not local functional regression.
- Physical RTSP and production ONNX calibration remain untested; Demo Mode remains available.

## Low

- Angular production build reports the existing admin-shell style budget exceeded by 61 bytes.
