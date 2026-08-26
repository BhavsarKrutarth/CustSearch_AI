"""Phase 18 normalized observations. This module deliberately contains no loss decision."""

from datetime import UTC, datetime
from enum import IntEnum

from pydantic import BaseModel, ConfigDict, Field, field_validator


def camel(value: str) -> str:
    head, *tail = value.split("_")
    return head + "".join(part.title() for part in tail)


class ObservationType(IntEnum):
    PERSON_ENTRY = 1
    PERSON_EXIT = 2
    SHELF_INTERACTION = 3
    PROBABLE_PICKUP = 4
    PROBABLE_PUT_BACK = 5
    CHECKOUT_ZONE_VISIT = 6
    TRACK_CONTINUITY = 7
    OCCLUSION_QUALITY = 8
    PROBABLE_ITEM_ASSOCIATION = 9
    RFID_EAS_SIGNAL = 10


class RawSecurityObservation(BaseModel):
    model_config = ConfigDict(extra="forbid")
    observation_type: ObservationType
    confidence: float = Field(ge=0, le=1)
    zone_id: int | None = Field(default=None, gt=0)
    product_id: int | None = Field(default=None, gt=0)
    product_category_id: int | None = Field(default=None, gt=0)
    metadata: dict[str, bool | int | float | str | None] = Field(default_factory=dict)


class SecurityNormalizeRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    tenant_id: int = Field(gt=0)
    store_id: int = Field(gt=0)
    camera_id: int = Field(gt=0)
    visit_id: int | None = Field(default=None, gt=0)
    person_track_session_id: int | None = Field(default=None, gt=0)
    person_track_id: str | None = Field(default=None, min_length=1, max_length=200)
    captured_utc: datetime
    model_version: str = Field(min_length=1, max_length=100)
    observations: list[RawSecurityObservation] = Field(min_length=1, max_length=500)

    @field_validator("captured_utc")
    @classmethod
    def require_utc(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() != UTC.utcoffset(value):
            raise ValueError("captured_utc must be UTC")
        return value


class NormalizedSecurityObservation(BaseModel):
    model_config = ConfigDict(alias_generator=camel, populate_by_name=True)
    tenant_id: int
    store_id: int
    camera_id: int
    visit_id: int | None
    person_track_session_id: int | None
    person_track_id: str | None
    observation_type: ObservationType
    occurred_utc: datetime
    zone_id: int | None
    product_id: int | None
    product_category_id: int | None
    confidence: float
    model_version: str
    metadata_json: str | None


def normalize_security(request: SecurityNormalizeRequest) -> list[NormalizedSecurityObservation]:
    """Normalize factual/probabilistic signals without calculating authoritative risk."""
    import json

    return [
        NormalizedSecurityObservation(
            tenant_id=request.tenant_id,
            store_id=request.store_id,
            camera_id=request.camera_id,
            visit_id=request.visit_id,
            person_track_session_id=request.person_track_session_id,
            person_track_id=request.person_track_id,
            observation_type=item.observation_type,
            occurred_utc=request.captured_utc,
            zone_id=item.zone_id,
            product_id=item.product_id,
            product_category_id=item.product_category_id,
            confidence=item.confidence,
            model_version=request.model_version,
            metadata_json=json.dumps(item.metadata, separators=(",", ":")) if item.metadata else None,
        )
        for item in request.observations
    ]
