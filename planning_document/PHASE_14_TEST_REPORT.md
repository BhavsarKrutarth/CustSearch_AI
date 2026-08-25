# Phase 14 — Consent-Based Recognition Test Report

**Result:** Green  
**Branch:** `phase14-consent-recognition`  
**Validated implementation commit:** `1d3b6ed6542c41e02a272d092b81fef56b817e2c`  
**Tested canonical persistence head:** `1a6f329ca761ce473548f86fedbc8359bd922b08`  
**Phase 14 workflow:** run `32800628656`, job `97660590424`  
**Canonical SQL blob:** `3ea2c859f84b40901aa416886911f578b4597daa`

Phase 14 was created from the exact fully green Phase 13 head `9aa0b256a38973ea67824b5d39141127abcac215`. The full Phase 5–14 application regression and all privacy/security checks passed before the tested V1.13.0 content was persisted into the canonical database script. The branch and draft PR remain separate from `AIMainBranch`.

## Required gates

| Gate | Status |
|---|---:|
| Validated Phase 13 baseline | Green |
| .NET Release build | Green — 0 warnings, 0 errors |
| .NET unit | Green — 90/90 |
| .NET integration/API | Green — 204/204 |
| Angular lint/unit/production build | Green — 73/73 tests |
| Playwright full regression | Green — 40/40 |
| Python Ruff and pytest | Green — 7/7 tests |
| SQL V1.13 upgrade twice | Green — SQL Server 2022 |
| Standalone runner twice + verifier | Green — SQL Server 2022 |
| Prospective canonical fresh install | Green — SQL Server 2022 |
| Recognition without active consent | Rejected |
| Withdrawn or expired consent | Rejected |
| Cross-tenant biometric metadata | Rejected |
| Cross-store enrollment/access | Rejected |
| Anonymous track after accepted review | Remains anonymous |
| Ambiguous candidate | Human review required; no auto-merge |
| Audit, retention and protected-material erasure | Green |
| Encryption and disabled-by-default configuration gates | Green |

## Security and privacy evidence

- Enrollment requires an eligible customer, exact-purpose active consent, an authorized operator and a server-authorized store.
- Browser-supplied `TenantId`, unknown identity fields and raw-image input are rejected by the API contract.
- Only derived template bytes are accepted. They are protected with AES-256-GCM using a secret-supplied 32-byte key and opaque key reference; ciphertext, nonce, authentication tag and key reference are absent from public DTOs.
- Recognition is disabled by default and cannot start with an invalid or missing encryption configuration.
- Recognition results remain confidence-scored candidates. Ambiguous results require human review, and even an accepted review does not mutate the authoritative anonymous tracking session or associate a customer automatically.
- Consent withdrawal blocks future review, erases active protected template material according to the retention policy and writes an audit record without deleting unrelated customer or billing data.
- Exact `Recognition.View`, `Recognition.Enroll`, `Recognition.Review`, `Recognition.Settings.Manage` and `Recognition.Consent.Manage` policies protect the API surface.
- No external identity database, social-media identity, Aadhaar/PAN mapping, unnecessary raw frame or silent CCTV enrollment path is implemented.

## Database evidence

`database/09_Upgrade/V1.13.0_Phase14_ConsentRecognition.sql`, `database/run-phase14.sql` and `database/verify-phase14.sql` passed repeat execution against SQL Server 2022. The verifier confirmed the consent, protected template and candidate tables, permissions, procedures and exactly one `DatabaseVersions` row for V1.13.0. The workflow then installed the prospective canonical script into a fresh database before persisting its exact tested content into `database/CustSearchAi.sql`.
