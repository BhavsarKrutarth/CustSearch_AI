# CustSearch.AI

FastAPI boundary for person detection/tracking, face detection and consent-based recognition. Phase 1 provides health, configuration, structured logging and Demo Mode only; camera/model workflows are implemented in their approved phases.

```powershell
python -m venv .venv
.\.venv\Scripts\python -m pip install -r requirements-dev.txt
.\.venv\Scripts\python -m ruff check . ..\..\tests\CustSearch.AI.Tests
.\.venv\Scripts\python -m pytest
.\.venv\Scripts\python -m uvicorn app.main:app --reload --port 8000
```
