# Phase 13 — Cameras, Python CCTV & Tracking Implementation Plan

**Branch:** `phase13-cctv-tracking`  
**Database version:** `V1.12.0`  
**Baseline:** tested Phase 12 merge commit `456c840ae9705cbd560d8213f924442920b4acd5`

## Safety boundary

- Phase 13 is anonymous operational tracking, not identity or biometric recognition.
- Camera rows store opaque RTSP configuration references; secret values remain in environment/vault-backed configuration.
- The Python process has no SQL Server dependency. It publishes normalized metadata through an HMAC-authenticated, tenant/store-authorized .NET service endpoint.
- Raw frames, model inputs, embeddings and external identity results are not persisted.
- Customer/staff association is an explicit authorized application command after tenant and store eligibility checks. CCTV events cannot supply those identifiers.
- Staff observations are operational context only and cannot be authoritative payroll, attendance, discipline or salary evidence.

## Delivery slices

1. Add tenant/store-scoped Cameras with protected configuration hints, status and heartbeat.
2. Add immutable versioned camera-zone polygons for Entry, Exit, Checkout, Shelf, Category, Restricted and Staff Area.
3. Add authenticated FastAPI/OpenCV/ONNX normalization and a HMAC .NET ingestion boundary with request limits, correlation and service tenant/store scope.
4. Add anonymous-first person sessions, explicit lifecycle, cross-camera handoffs, confidence and gap duration.
5. Add deterministic Demo Mode fixtures with explicit Production startup guards.
6. Add Angular camera management, zone editor, status, authoritative tracking recovery and visible Demo/operational-use labels.
7. Add idempotent V1.12.0 upgrade, standalone runner, verifier and canonical database update.
8. Run .NET, Angular, Playwright, Ruff, pytest, SQL Server and full Phase 5–13 regression; only then mark Phase 13 complete.

## Completion gate

The branch remains unmerged until every requested Phase 13 and prior regression gate is green and the tested V1.12.0 canonical SQL is committed.
