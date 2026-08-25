# Phase 14 — Consent-Based Recognition Implementation Plan

**Branch:** `phase14-consent-recognition`  
**Validated baseline:** Phase 13 final head `9aa0b256a38973ea67824b5d39141127abcac215`

## Privacy and trust boundary

- Recognition is disabled by default and cannot enroll or create a candidate until encryption/policy configuration is explicitly enabled.
- Enrollment accepts only a derived template for an explicitly selected active customer with active purpose-matching consent; no raw CCTV enrollment or anonymous auto-enrollment exists.
- Python/Phase 13 anonymous tracking remains authoritative for operational tracks. Phase 14 candidates never call the customer/staff association methods and never claim unquestionable identity.
- No external identity database, social profile, Aadhaar or PAN mapping is accepted or persisted.

## Implementation slices

1. Add purpose-specific consent with grant, expiry and withdrawal state plus evidence reference and audit.
2. Store only AES-256-GCM protected derived templates with opaque key references, separate metadata access and safe disabled defaults.
3. Validate tenant/store/customer/consent access before enrollment, candidate creation, review or template metadata access.
4. Create idempotent match candidates with confidence, quality, ambiguity and mandatory human-review state.
5. On withdrawal, stop future recognition and erase active ciphertext/nonce/tag while retaining only minimized audit/retention metadata.
6. Add exact permissions: `Recognition.View`, `Recognition.Enroll`, `Recognition.Review`, `Recognition.Settings.Manage`, `Recognition.Consent.Manage`.
7. Add Angular consent, enrollment metadata and review queue UI without exposing protected template material or client-controlled TenantId.
8. Add V1.13.0 versioned/standalone/verifier SQL and tested canonical persistence.

## Completion gates

- .NET Release build and complete unit/integration/API regression.
- Angular lint, unit tests and production build.
- Complete Playwright regression.
- Python Ruff and pytest regression.
- Phase 14 privacy/static checks.
- SQL Server V1.13.0 upgrade twice, standalone runner twice plus verifier, and prospective canonical fresh install.
- All Phase 6–14 workflows green on the final branch head before Phase 14 is marked completed.
