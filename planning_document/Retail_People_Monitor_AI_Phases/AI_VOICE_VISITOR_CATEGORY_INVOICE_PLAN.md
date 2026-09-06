# Tenant Voice Commands, Anonymous Visitor Interests and Invoice Context

Status: Planned only, 2026-09-06. Source review completed; verification results are recorded below. This document does not implement microphone capture, guest sessions or invoice integration.

[Roadmap](AI_PHASE_00_ROADMAP.md) | [Assistance](AI_PHASE_05_STAFF_CUSTOMER_ASSISTANCE.md) | [Invoices](AI_PHASE_06_INVOICE_STAFF_ATTRIBUTION.md)

## Intended experience

Staff Ravi signs into his own device and starts assistance for Guest G104 by selecting the visitor card or a counter/QR token. No customer name/mobile is required. He says the configured trigger followed by "Banarasi sari aur silk sari add karo". The app displays the target Guest G104, resolves two existing store categories and adds both as visit interests according to the confirmation policy. Further commands can add/remove categories. When the cashier opens a draft from this assistance session, the same interest categories and salesperson context appear automatically. Actual purchased product lines remain explicit selections with server-calculated pricing.

Unknown is an anonymous visit, not an invented customer identity. The voice command belongs to the signed-in staff member; the category belongs to the selected visitor/customer assistance session, not to the staff profile.

## Current implementation evidence

| Area | Reviewed source | Finding |
|---|---|---|
| Dynamic settings UI | src/CustSearch.Admin/src/app/features/preferences/voice-preferences-page.ts | Store-specific trigger, aliases, enablement, language, timeout, confidence and response/confirmation controls exist. Current test flow uses typed trigger/transcript/confidence inputs; no actual microphone/wake-word/TTS implementation was found in the Admin source search. |
| Voice service | src/CustSearch.Infrastructure/PreferencesVoice/PreferencesVoiceService.cs | Resolves tenant from authenticated context, checks store/customer visibility, resolves existing category names/codes/aliases, handles ambiguity and creates VoiceConfirmed preference signals. Session access requires the originating StaffUserId. |
| Request contract | src/CustSearch.Application/PreferencesVoice/IPreferencesVoiceService.cs | StartVoiceSessionCommand requires CustomerId; proposed category reference is singular. No anonymous visitor or assistance target in this contract. |
| Invoice editor | src/CustSearch.Admin/src/app/features/retail/invoice-editor-page.ts | Accepts customer/visit references and selected products. No voice-interest/assistance carry-forward is wired in this editor. |
| Existing regression fixtures | tests/CustSearch.IntegrationTests/PhaseTenPreferencesVoiceServiceTests.cs | Tests cover store triggers, aliases, ambiguity/confirmation, rejected/unknown categories, cross-store customer isolation, audit scope and deterministic preference scores. These fixtures do not establish real microphone or multi-staff voice accuracy. |

Review gaps to address before rollout:

- [ ] Settings are currently per-store inside a tenant. Add explicit tenant defaults and store overrides; do not describe tenant-default inheritance as already implemented.
- [ ] IsEnabled is checked at session start; the reviewed interpretation/confirmation paths do not recheck it consistently. Revalidate effective enablement, current grants, target visibility, category availability and policy version at every mutation, including delayed confirmations.
- [ ] Settings saving delegates base-setting persistence before runtime-setting validation/save. Make the full update transactional with all validation before persistence; test rollback on invalid runtime inputs.
- [ ] RequireConfirmationForAmbiguousCategory is exposed, but ambiguous results always return a candidate choice in the reviewed resolver. Preserve that safe behavior and make the setting/help text reflect actual semantics; never let disabling confirmation choose randomly.
- [ ] Current confidence is supplied by the text-test client. Production decisions must use authenticated adapter output with provider-specific calibration; absent confidence requires confirmation, not a fabricated score.
- [ ] Add explicit target/device leases, concurrent command idempotency and multi-category proposals. Sequential state guards alone do not prove concurrent requests cannot duplicate writes.

## Tenant settings and device behavior

- [ ] Effective settings = tenant default + permitted store overrides. User/device preferences may select an allowed language or microphone, but cannot weaken approval, scope or confirmation requirements.
- [ ] Configure enabled state, trigger phrase/aliases, language choices, microphone mode, wake-word model version, listening timeout, maximum utterance length, response mode, confirmation mode, category aliases, provider connection, quotas and retention.
- [ ] Confirmation modes: AlwaysConfirm (initial default) and AutoAddExactMatches. Auto mode requires an explicitly bound target, valid device/staff lease and each phrase mapping unambiguously to an active category with adequate measured speech quality. Fuzzy, conflicting, missing-confidence and ambiguous results always need confirmation.
- [ ] Provide Test microphone, Test trigger, Test category, listening indicator, target badge, undo and mute. Settings show Saved versus DeployedToDevice distinctly; changing text alone does not train/deploy an acoustic wake-word model.
- [ ] Recommend a configurable application phrase such as "Hey Aasha". "He Seri" can be evaluated as a requested custom phrase; this is not Apple Siri integration. Test false activation and device-assistant interference before approving a phrase.
- [ ] First ship push-to-talk on the staff device. Add on-device wake-word detection for supported foreground/native clients after real device/noise tests. Preserve push-to-talk when background listening, permission or platform support is unavailable.
- [ ] A wake word is an activation cue, not staff authentication. Prefer individual signed-in devices/headsets; no automatic write from a shared room/CCTV microphone. Multiple nearby devices or overlapping speech must reject/confirm uncertain commands rather than guess the speaker/recipient.

## Stable target selection for many staff and visitors

1. Create a server-owned anonymous VisitorSession scoped to tenant/store, with a human-friendly short code such as G104. Optionally link a reviewed camera track using camera ID + stream epoch + local track ID; the visitor ID survives temporary track loss.
2. Staff explicitly chooses the visitor/customer through a card, QR, counter token or authorized lookup. Camera proximity may suggest candidates but never silently changes the selected target.
3. Create or extend the Phase 05 AssistanceSession. It references exactly one VisitorSession or existing customer visit as its primary subject, with nullable confirmed CustomerId for a guest. Extend the planned customer-only schema explicitly; do not insert dummy Customer rows.
4. Permit one primary assistant per visitor visit, with explicit supporting participants and audited transfers. Each staff/device has one selected voice target at a time, even when the staff queue contains several customers.
5. Issue a short-lived target lease with assistance ID, subject ID, device ID, actor and binding revision. Resolve tenant/actor server-side. Include command ID, lease revision, settings version and expected session version in each command.
6. Recheck the lease at commit. Switching targets, transferring assistance, logout, revocation or ending a visit invalidates pending commands. Return a target-changed result; never redirect a delayed G104 utterance to newly selected G105.
7. Use per-session concurrency controls, atomic batches and a unique tenant/device/command idempotency key. Retried delivery returns the original outcome. The same active interest is unique per visitor visit/category; repeats do not inflate preference weights.

Example: Ravi/G104 says Banarasi; Meena/G105 says Cotton at the same time on their respective devices. Their leases keep commands isolated. If Ravi assists two visitors, he selects G104/G105 first or uses a validated target code with explicit confirmation; the system never guesses from who is standing nearest.

## Category interpretation and persistence

- [ ] Parse a bounded command schema: action AddCategories/RemoveCategories/ListCategories/Undo, target lease, list of category phrases, locale and recognition quality. Never execute unrestricted model-produced API/SQL instructions.
- [ ] Handle Hindi/English/Gujarati and common transliterations through tested speech support and configured aliases: Banarashi sadi, Banarasi sari and the tenant-approved equivalent map to the same catalog ID.
- [ ] Split coordinated phrases and preserve negation/corrections: "Banarasi nahi, cotton add karo" must not add Banarasi. For unsupported grammar return a confirmation preview instead of broad substring auto-matching.
- [ ] Resolve only active tenant/store categories and aliases. Speech never creates a new catalog category. A manager can separately create a category or approve a new alias.
- [ ] Show all proposed additions/removals with target code. If any phrase is unresolved/ambiguous, hold the complete batch for correction/confirmation; do not silently save a partial command.
- [ ] Save VisitorCategoryInterests as one-to-many rows with category ID, category-name snapshot, status, assistance/session provenance, staff actor, command ID, time and revision. This supports multiple categories without adding one column per category.
- [ ] Use states Active/Removed and an event history. Undo is an audited compensating action linked to the command; do not erase another staff member's later changes or rewrite finalized invoices.
- [ ] When a visitor voluntarily registers, an authorized reviewed link associates that visitor with CustomerId. Transfer eligible interests idempotently into customer preferences as VoiceConfirmed/interest evidence, not as purchases. Do not merge unrelated visitor visits based on track number or uncertain face match.
- [ ] Apply bounded retention to anonymous visit interests and transcripts; discard raw utterance audio after processing by default. Store only what is needed for the feature and audit.

## Invoice binding: interests versus purchased items

- [ ] Add Create draft from assistance to the staff/cashier flow. Resolve the exact assistance/visitor/customer visit on the server; never choose the latest store-wide guest.
- [ ] Carry active category interests into an InvoiceInterestCategories context snapshot plus salesperson/visitor provenance. Display them as Requested categories and prefilter product suggestions.
- [ ] Auto-binding means carrying this context into the draft. It does not create chargeable lines for every requested category. Staff selects/scans the actual SKU, quantity and options; item CategoryId comes from the selected product's server catalog snapshot.
- [ ] Example: G104 requests Banarasi and Silk, buys one Banarasi SKU. The draft shows both interests; the bill charges only that selected SKU and records its catalog category. Unpurchased Silk is not counted as a sale.
- [ ] Keep walk-in CustomerId nullable when the existing invoice rules allow it; link the guest session separately. Require explicit reviewed association before adding a registered customer identity.
- [ ] Snapshot interest IDs/revisions at draft creation. Later interest changes show an available-refresh indicator; refresh is explicit and cannot overwrite line items. Freeze context at finalization and preserve correction history.
- [ ] Handle split bills and multiple visitors explicitly with per-session selections and existing item-level attribution. Never distribute one visitor's interests to all invoice participants.
- [ ] .NET retains price, tax, stock, payment and finalization authority. Voice can propose navigation to a draft; financial finalization requires the existing authorized checkout action.

## Python and application responsibilities

| Component | Planned ownership |
|---|---|
| Microphone capture / target badge / push-to-talk / local wake detector | Staff client or explicitly paired device agent |
| Voice activity detection and speech-to-text | Separate Python voice worker or approved speech provider adapter |
| Bounded multilingual intent parsing | Python voice module when needed; deterministic grammar/alias parsing first |
| Final category resolution, target checks and transactional writes | Existing .NET PreferencesVoice service extended with compatible contracts |
| Visitor tracking | Existing planned Python vision worker; optional context only |
| Tenant settings, secrets, leases, visit interests and invoice snapshots | .NET/database |
| Spoken acknowledgement | Optional client/provider TTS, respecting response mode; no inference loop from its own playback |

Proposed Python modules under src/CustSearch.AI/app/voice/: contracts.py, vad.py, transcription.py, intents.py and provider_adapter.py. Run voice jobs with separate bounded resources/queues so speech cannot stall camera inference. Reuse pretrained multilingual speech recognition initially; benchmark languages, model footprint and latency before selecting/pinning a model. A new category alias does not require retraining the face/person model or speech model. Custom wake-word training is a separate optional device artifact workflow.

## Logical data/API additions

Reuse existing VoiceCommandSessions, category aliases, preferences, audit and assistance plans rather than building a parallel command system. Version existing contracts to keep the current CustomerId-based flow compatible.

| Addition | Key fields / invariant |
|---|---|
| TenantVoiceDefaults + store overrides | TenantId, settings version, override mask, allowed provider/model references; existing store records migrate as explicit overrides |
| VisitorSessions | TenantId, StoreId, ID/code, start/end, nullable reviewed CustomerId, RowVersion |
| Assistance subject/participant extension | VisitorSessionId or customer visit subject, primary/supporting staff, transfer history and revision |
| VoiceDeviceTargetLeases | TenantId, StoreId, DeviceId, StaffUserId, AssistanceSessionId, target revision, expiry/revocation |
| VoiceCommand proposal items | CommandId, phrase, action, resolved category or candidate status, per-item quality, settings/target versions |
| VisitorCategoryInterests + events | VisitorSessionId, CategoryId, state, actor/source command, revisions; unique active interest per visit/category |
| RetailInvoiceInterestCategories | InvoiceId, visitor/assistance source, interest/category snapshot, source revision |

Proposed operations: effective voice settings/test; guest visit start/end; assistance select/transfer; voice lease acquire/revoke; command interpret/confirm/undo; visitor interests list; draft from assistance; explicit draft-context refresh. All scope derives from authenticated context, and every referenced resource is checked. Select physical schema/FKs/migration numbers after checking the existing database.

## Acceptance gates and delivery order

1. Verify existing settings/aliases/confirmation and repair identified settings atomicity/revocation gaps. Add actual tenant-default inheritance and permission coverage.
2. Implement manual guest assistance and multi-category UI with idempotency first; it must work when Python/audio is offline.
3. Implement push-to-talk, short utterance transcription, target-bound category batches and acknowledgement. Test permission denied, unsupported browser, noise, accents, silence, timeouts, TTS echo, dropped network and provider failure.
4. Add invoice context carry-forward, SKU-derived category binding and walk-in/registered/split-bill tests.
5. Pilot optional wake-word mode on selected hardware. Measure false wakes per hour, command/category accuracy, wrong-target writes, p95 latency and duplicate mutations. Record actual results and approve numeric release targets before rollout; do not promise perfect acoustic accuracy.

Required correctness scenarios: at least 10 simultaneous staff sessions across multiple tenants/stores; same phrase on different devices; two guests per staff queue; cross-tenant guessed IDs; expired leases; target switch while speech is in flight; supporting-staff concurrency; transfer; duplicate delivery; repeated categories; multi-category ambiguity; negation; revoked settings mid-command; closed visit; anonymous-to-customer linking; draft refresh versus finalized bill. No cross-target or duplicate mutation is acceptable in these tests.

## Verification record

Source inspection and targeted regression execution completed on 2026-09-06:

| Check | Actual result |
|---|---|
| PhaseTenPreferenceVoiceEntityTests | 9 passed, 0 failed, 0 skipped |
| PhaseTenPreferencesVoiceServiceTests | 7 passed, 0 failed, 0 skipped; SQLite relational fixture |
| Phase markdown local links | Verified |
| Initial default-output test build | Blocked by files held by the running API/Visual Studio; retried successfully with separate temporary build artifacts |

Commands used: `dotnet test tests/CustSearch.UnitTests/CustSearch.UnitTests.csproj --artifacts-path <temporary-unit-artifacts> --filter FullyQualifiedName~PhaseTenPreferenceVoiceEntityTests` and `dotnet test tests/CustSearch.IntegrationTests/CustSearch.IntegrationTests.csproj --artifacts-path <temporary-integration-artifacts> --filter FullyQualifiedName~PhaseTenPreferencesVoiceServiceTests`. TRX reports: tests/CustSearch.UnitTests/TestResults/voice-entity-review.trx and tests/CustSearch.IntegrationTests/TestResults/voice-service-review.trx.

These 16 existing tests confirm their covered entity/service behaviors, not the new plan or every tenant setting. The integration fixtures use two stores in one tenant; cross-tenant and multi-staff concurrency scenarios remain acceptance work. No live store, real microphone, browser session or production database was tested. Reviewed revocation/atomicity gaps above remain open; application source was not changed in this planning task.

## Technical references checked 2026-09-06

- [MDN SpeechRecognition](https://developer.mozilla.org/en-US/docs/Web/API/SpeechRecognition): browser availability is limited; do not make this the only supported speech path.
- [Porcupine wake-word documentation](https://picovoice.ai/docs/porcupine/): candidate for custom wake-word artifacts, subject to platform/language/license evaluation.
- [Porcupine Web quick start](https://picovoice.ai/docs/quick-start/porcupine-web/): custom models target Web/WASM; configuration text alone is not a deployed wake model.
