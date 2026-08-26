from datetime import UTC, datetime

import pytest
from pydantic import ValidationError

from app.security_observations import (
    ObservationType,
    RawSecurityObservation,
    SecurityNormalizeRequest,
    normalize_security,
)


def request(*observations: RawSecurityObservation) -> SecurityNormalizeRequest:
    return SecurityNormalizeRequest(
        tenant_id=1,
        store_id=2,
        camera_id=3,
        person_track_id="anonymous-track-1",
        captured_utc=datetime(2026, 8, 27, tzinfo=UTC),
        model_version="phase18-test",
        observations=list(observations),
    )


def test_emits_normalized_observations_without_final_decision() -> None:
    result = normalize_security(
        request(
            RawSecurityObservation(
                observation_type=ObservationType.PROBABLE_PICKUP, confidence=0.91
            ),
            RawSecurityObservation(observation_type=ObservationType.PERSON_EXIT, confidence=0.95),
        )
    )
    assert [item.observation_type for item in result] == [
        ObservationType.PROBABLE_PICKUP,
        ObservationType.PERSON_EXIT,
    ]
    serialized = result[-1].model_dump(by_alias=True)
    assert "riskScore" not in serialized
    assert "finalDecision" not in serialized
    assert "customerId" not in serialized


@pytest.mark.parametrize(
    "observation_type",
    [
        ObservationType.PERSON_ENTRY,
        ObservationType.PERSON_EXIT,
        ObservationType.SHELF_INTERACTION,
        ObservationType.PROBABLE_PICKUP,
        ObservationType.PROBABLE_PUT_BACK,
        ObservationType.CHECKOUT_ZONE_VISIT,
        ObservationType.TRACK_CONTINUITY,
        ObservationType.OCCLUSION_QUALITY,
        ObservationType.PROBABLE_ITEM_ASSOCIATION,
    ],
)
def test_all_planned_visual_observations_are_supported(observation_type: ObservationType) -> None:
    assert normalize_security(request(RawSecurityObservation(observation_type=observation_type, confidence=0.8)))


def test_unknown_identity_and_pixels_are_rejected() -> None:
    with pytest.raises(ValidationError):
        RawSecurityObservation.model_validate(
            {"observation_type": 4, "confidence": 0.9, "identity": "unknown person"}
        )
