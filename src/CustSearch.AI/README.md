# CustSearch.AI

FastAPI/OpenCV/ONNX boundary for anonymous person detection and operational tracking. Phase 13 does not perform identity or biometric recognition, stores no frames, and publishes normalized metadata only through the authenticated .NET API. Demo Mode is deterministic and cannot run in Production.

```powershell
python -m venv .venv
.\.venv\Scripts\python -m pip install -r requirements-dev.txt
.\.venv\Scripts\python -m ruff check . ..\..\tests\CustSearch.AI.Tests
.\.venv\Scripts\python -m pytest
.\.venv\Scripts\python -m uvicorn app.main:app --reload --port 8000
```

In Development the service targets
`https://localhost:7277/api/internal/cctv/events`. It never connects to SQL Server directly. Set
`CUSTSEARCH_AI_DOTNET_EVENT_URL`, `CUSTSEARCH_AI_SERVICE_ID` and
`CUSTSEARCH_AI_SERVICE_SECRET` for another API endpoint or authenticated event publishing.
