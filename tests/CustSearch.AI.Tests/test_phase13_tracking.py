"""Phase 13 authenticated, anonymous-only and deterministic tracking tests."""

from datetime import UTC

import httpx
import pytest
from pydantic import SecretStr, ValidationError

from app.config import Settings
from app.main import app, settings
from app.tracking import deterministic_demo_events


@pytest.mark.asyncio
async def test_python_api_rejects_missing_and_invalid_credentials() -> None:
    settings.api_key = SecretStr("phase13-test-key")
    payload = {
        "tenant_id": 1,
        "store_id": 2,
        "camera_code": "ENTRY-01",
        "captured_utc": "2026-08-25T09:00:00Z",
        "detections": [],
    }
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        missing = await client.post("/v1/cctv/events/normalize", json=payload)
        invalid = await client.post(
            "/v1/cctv/events/normalize",
            json=payload,
            headers={"X-CustSearch-AI-Key": "wrong"},
        )

    assert missing.status_code == 401
    assert invalid.status_code == 401


@pytest.mark.asyncio
async def test_normalized_tracking_stays_anonymous_and_rejects_injected_subject() -> None:
    settings.api_key = SecretStr("phase13-test-key")
    payload = {
        "tenant_id": 1,
        "store_id": 2,
        "camera_code": "entry-01",
        "captured_utc": "2026-08-25T09:00:00Z",
        "detections": [
            {"track_id": "anonymous-1", "confidence": 0.92, "state": "entered"}
        ],
    }
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        accepted = await client.post(
            "/v1/cctv/events/normalize",
            json=payload,
            headers={"X-CustSearch-AI-Key": "phase13-test-key"},
        )
        payload["detections"][0]["customer_id"] = 99
        rejected = await client.post(
            "/v1/cctv/events/normalize",
            json=payload,
            headers={"X-CustSearch-AI-Key": "phase13-test-key"},
        )

    assert accepted.status_code == 200
    assert accepted.json()[0]["person_track_id"] == "anonymous-1"
    assert "customer" not in accepted.text.casefold()
    assert rejected.status_code == 422


def test_demo_mode_is_deterministic_and_production_guarded() -> None:
    first = [event.model_dump(mode="json") for event in deterministic_demo_events()]
    second = [event.model_dump(mode="json") for event in deterministic_demo_events()]
    assert first == second
    assert [event["event_type"] for event in first] == [
        "person.entered",
        "person.handoff",
        "person.exited",
    ]
    with pytest.raises(ValidationError, match="Demo Mode"):
        Settings(environment="Production", demo_mode=True)


def test_demo_fixture_uses_utc_and_one_continuous_anonymous_track() -> None:
    events = deterministic_demo_events()
    assert {event.person_track_id for event in events} == {"demo-track-0001"}
    assert all(event.occurred_utc.utcoffset() == UTC.utcoffset(None) for event in events)
