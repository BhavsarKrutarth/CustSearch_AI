"""Anonymous-only normalized camera tracking contracts for Phase 13."""

from datetime import UTC, datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator


class Detection(BaseModel):
    """Minimal detector output; pixels and identity attributes are deliberately absent."""

    model_config = ConfigDict(extra="forbid")
    track_id: str = Field(min_length=1, max_length=100)
    confidence: float = Field(ge=0, le=1)
    zone_code: str | None = Field(default=None, max_length=50)
    state: Literal["entered", "updated", "exited", "lost"] = "updated"


class NormalizeRequest(BaseModel):
    """Camera metadata accepted by the normalized service API."""

    model_config = ConfigDict(extra="forbid")
    tenant_id: int = Field(gt=0)
    store_id: int = Field(gt=0)
    camera_code: str = Field(min_length=1, max_length=50)
    captured_utc: datetime
    detections: list[Detection] = Field(max_length=500)

    @field_validator("captured_utc")
    @classmethod
    def require_utc(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() != UTC.utcoffset(value):
            raise ValueError("captured_utc must be UTC")
        return value


class NormalizedEvent(BaseModel):
    """Versioned metadata sent to the authenticated .NET ingestion boundary."""

    contract_version: int = 1
    event_type: str
    tenant_id: int
    store_id: int
    camera_code: str
    person_track_id: str
    occurred_utc: datetime
    confidence: float
    zone_code: str | None = None
    from_camera_code: str | None = None
    gap_milliseconds: int | None = None
    camera_status: int | None = None


def normalize(request: NormalizeRequest) -> list[NormalizedEvent]:
    """Convert detector metadata into stable anonymous tracking events."""

    return [
        NormalizedEvent(
            event_type=f"person.{detection.state}",
            tenant_id=request.tenant_id,
            store_id=request.store_id,
            camera_code=request.camera_code.upper(),
            person_track_id=detection.track_id,
            occurred_utc=request.captured_utc,
            confidence=detection.confidence,
            zone_code=detection.zone_code,
        )
        for detection in request.detections
    ]


def deterministic_demo_events() -> list[NormalizedEvent]:
    """Return a CI-stable lifecycle and camera handoff without opening RTSP."""

    instant = datetime(2026, 8, 25, 9, 0, tzinfo=UTC)
    common = {
        "tenant_id": 1,
        "store_id": 1,
        "person_track_id": "demo-track-0001",
        "confidence": 0.91,
    }
    return [
        NormalizedEvent(
            event_type="person.entered", camera_code="DEMO-ENTRY", occurred_utc=instant, **common
        ),
        NormalizedEvent(
            event_type="person.handoff",
            camera_code="DEMO-AISLE",
            from_camera_code="DEMO-ENTRY",
            gap_milliseconds=800,
            occurred_utc=instant.replace(second=8),
            **common,
        ),
        NormalizedEvent(
            event_type="person.exited",
            camera_code="DEMO-AISLE",
            occurred_utc=instant.replace(second=20),
            **common,
        ),
    ]
