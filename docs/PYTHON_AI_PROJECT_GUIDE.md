# Python AI Project Guide

Last verified: 2026-08-26

## 1. Python project ka purpose

`src/CustSearch.AI` CCTV/AI processing boundary hai. Iska intended flow hai:

```text
IP Camera / Demo fixture
  -> OpenCV frame capture
  -> approved ONNX person detector
  -> anonymous track metadata
  -> normalized CCTV event
  -> HMAC-authenticated ASP.NET API
  -> SQL Server through .NET only
  -> Angular Camera Operations / alerts
```

Python ko SQL Server connection string nahi milti aur woh database directly access nahi karta. Tenant,
store, camera ownership, replay protection aur final persistence ASP.NET API validate karta hai.

## 2. Current implementation status

Currently executable:

- FastAPI service and `/health/live`.
- API-key protected Demo Mode events.
- API-key protected anonymous event normalization.
- Strict request validation; unexpected identity fields are rejected.
- OpenCV/ONNX model-loading and frame-preparation adapter.
- JSON correlation-aware logging.
- Ruff and pytest coverage.

Not yet wired as a continuous executable flow:

- RTSP camera capture loop.
- Frame-by-frame detector/tracker orchestration.
- HMAC publisher that sends normalized events to the .NET endpoint.
- Camera reconnect/backoff and per-camera health supervision.
- Production ONNX model packaging/calibration.

Therefore, starting FastAPI today does not automatically open the physical camera. Demo and normalize
endpoints work; live RTSP ingestion needs the remaining runner/publisher implementation.

## 3. File-by-file responsibility

| File | Use |
|---|---|
| `app/main.py` | FastAPI app, correlation middleware, health, API-key checks, demo and normalize endpoints |
| `app/config.py` | `CUSTSEARCH_AI_*` environment settings and Production Demo Mode guard |
| `app/tracking.py` | Detection/request/event contracts and anonymous event normalization |
| `app/vision_runtime.py` | Approved ONNX session loading and OpenCV frame preprocessing |
| `app/logging_config.py` | Safe JSON logs without request bodies or secrets |
| `app/__init__.py` | Python package marker |
| `requirements.txt` | Runtime dependencies: FastAPI, OpenCV, ONNX Runtime, NumPy, HTTPX, Uvicorn |
| `requirements-dev.txt` | Runtime dependencies plus pytest and Ruff |
| `pyproject.toml` | Ruff/pytest settings and Python 3.12 target |
| `tests/CustSearch.AI.Tests/test_health.py` | Health and correlation behavior |
| `tests/CustSearch.AI.Tests/test_phase13_tracking.py` | Auth, anonymous-only schema and deterministic Demo Mode tests |

## 4. First-time local setup

Open a new PowerShell terminal:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\src\CustSearch.AI"
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
```

If `py -3.12` is unavailable, verify installed Python with `py -0p` and use a repository-compatible
Python 3.12 interpreter.

## 5. Run Python in safe Demo Mode

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\src\CustSearch.AI"
$env:CUSTSEARCH_AI_ENVIRONMENT = "Development"
$env:CUSTSEARCH_AI_DEMO_MODE = "true"
$env:CUSTSEARCH_AI_API_KEY = "<local-random-key-not-committed>"
$env:CUSTSEARCH_AI_DOTNET_EVENT_URL = "https://localhost:7277/api/internal/cctv/events"
.\.venv\Scripts\python.exe -m uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

Do not place the API key in Git, screenshots or shared logs.

Verify liveness in another terminal:

```powershell
curl.exe -i "http://127.0.0.1:8000/health/live"
```

Current service exposes `/health/live`; it does not currently define `/health/ready`.

The .NET operational setting currently keeps AI health checking disabled. If you explicitly enable it
while Uvicorn is running with the HTTP command above, override its health URL before starting .NET:

```powershell
$env:OperationalPlatform__AiServiceEnabled = "true"
$env:OperationalPlatform__AiServiceHealthUrl = "http://127.0.0.1:8000/health/live"
```

## 6. Test Demo Mode endpoint

```powershell
$headers = @{ "X-CustSearch-AI-Key" = "<same-local-key>" }
Invoke-RestMethod `
  -Uri "http://127.0.0.1:8000/v1/cctv/demo/events" `
  -Headers $headers
```

Expected deterministic lifecycle:

```text
person.entered -> person.handoff -> person.exited
```

The demo track remains anonymous and does not open RTSP.

## 7. Test normalization endpoint

```powershell
$headers = @{ "X-CustSearch-AI-Key" = "<same-local-key>" }
$body = @{
  tenant_id = 10019
  store_id = 11
  camera_code = "ENTRY-01"
  captured_utc = (Get-Date).ToUniversalTime().ToString("o")
  detections = @(
    @{ track_id = "anonymous-local-001"; confidence = 0.92; state = "entered" }
  )
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
  -Method Post `
  -Uri "http://127.0.0.1:8000/v1/cctv/events/normalize" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $body
```

This returns normalized metadata only. It does not currently forward the result to .NET.

## 8. Environment variables

| Variable | Purpose | Current use |
|---|---|---|
| `CUSTSEARCH_AI_ENVIRONMENT` | Development/Production guard | Active |
| `CUSTSEARCH_AI_DEMO_MODE` | Enables deterministic demo fixtures | Active |
| `CUSTSEARCH_AI_API_KEY` | Protects Python normalize/demo endpoints | Active |
| `CUSTSEARCH_AI_DOTNET_EVENT_URL` | Target `.NET` ingestion URL | Configured, publisher not yet wired |
| `CUSTSEARCH_AI_SERVICE_ID` | HMAC service identity | Configured, publisher not yet wired |
| `CUSTSEARCH_AI_SERVICE_SECRET` | HMAC secret | Configured, publisher not yet wired |
| `CUSTSEARCH_CAMERA_<CAMERA_KEY>_RTSP` | Dynamic authorized RTSP source resolved from an opaque `env:` reference | Active for authenticated one-frame probe |

Environment variables are read once per process. Restart Uvicorn after changing them.

## 9. Run quality checks

From `src/CustSearch.AI`:

```powershell
.\.venv\Scripts\python.exe -m ruff check . ..\..\tests\CustSearch.AI.Tests
$env:PYTHONPATH = "."
.\.venv\Scripts\python.exe -m pytest -q ..\..\tests\CustSearch.AI.Tests
```

Current recorded regression result is Ruff PASS and pytest `10/10` PASS.

## 10. Privacy and security rules

- Keep tracking anonymous by default.
- Never send raw image bytes, face embeddings, passwords or RTSP URLs in normalized events.
- Never infer customer identity or household membership from proximity/face similarity.
- Recognition is a separate consent-gated and human-reviewed workflow.
- Do not run Demo Mode when `CUSTSEARCH_AI_ENVIRONMENT=Production`; configuration rejects it.
- Do not persist frames unless a separately approved retention/privacy design requires it.
- Do not bypass the .NET HMAC, tenant/store and camera authorization boundary.

## 11. Required work for a real camera

To complete live-camera support, implement and test these bounded components:

1. Extend the implemented allow-listed environment resolver with the production secret-vault provider.
2. Extend the implemented bounded one-frame RTSP probe into continuous capture with exponential reconnect.
3. Approved ONNX detector output parsing and non-biometric person tracker.
4. Frame dropping/backpressure so camera lag cannot exhaust memory.
5. Normalized event generation using `tracking.py` contracts.
6. HMAC request signing and idempotent POST to `/api/internal/cctv/events`.
7. Camera heartbeat, offline/degraded state and safe shutdown.
8. Unit tests with recorded/synthetic non-sensitive fixtures plus a physical-camera smoke test.

Read `CAMERA_CONNECTION_AND_RTSP_GUIDE.md` for the physical camera and .NET registration flow.
