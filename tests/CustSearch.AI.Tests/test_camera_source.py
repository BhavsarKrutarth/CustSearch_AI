"""Dynamic RTSP source resolution and authenticated probe boundary tests."""

from datetime import UTC, datetime

import httpx
import numpy as np
import pytest
from app.camera_source import (
    CameraPreviewFrame,
    CameraProbeResult,
    CameraSourceConfigurationError,
    probe_camera,
    resolve_camera_source,
)
from app.main import app, settings
from pydantic import SecretStr


def test_resolver_accepts_only_allow_listed_environment_reference(monkeypatch) -> None:
    monkeypatch.setenv("CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP", "rtsp://camera-user:secret@camera/stream")

    source = resolve_camera_source("env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP")

    assert source.url.startswith("rtsp://")
    with pytest.raises(CameraSourceConfigurationError, match="must use env"):
        resolve_camera_source("rtsp://browser-controlled/stream")
    with pytest.raises(CameraSourceConfigurationError, match="not configured"):
        resolve_camera_source("env:CUSTSEARCH_CAMERA_MISSING_RTSP")


def test_probe_reads_one_frame_and_releases_without_returning_secret(monkeypatch) -> None:
    monkeypatch.setenv("CUSTSEARCH_CAMERA_TEST_RTSP", "rtsp://camera-user:secret@camera/stream")

    class Capture:
        released = False

        def open(self, source, backend, parameters):
            assert source.endswith("@camera/stream")
            assert parameters
            return True

        def read(self):
            return True, np.zeros((720, 1280, 3), dtype=np.uint8)

        def release(self):
            Capture.released = True

    monkeypatch.setattr("app.camera_source.cv2.VideoCapture", Capture)

    result = probe_camera("env:CUSTSEARCH_CAMERA_TEST_RTSP", 2)

    assert result.connected and result.frame_received
    assert (result.width, result.height) == (1280, 720)
    assert Capture.released
    assert "secret" not in result.model_dump_json()


@pytest.mark.asyncio
async def test_probe_endpoint_is_authenticated_and_does_not_return_reference(monkeypatch) -> None:
    settings.api_key = SecretStr("camera-probe-key")
    expected = CameraProbeResult(
        connected=True,
        frame_received=True,
        width=640,
        height=480,
        elapsed_ms=12,
        status="Frame received.",
    )
    monkeypatch.setattr("app.main.probe_camera", lambda _reference, _timeout: expected)
    payload = {"configuration_reference": "env:CUSTSEARCH_CAMERA_DYNAMIC_RTSP", "timeout_seconds": 3}

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        denied = await client.post("/v1/cctv/cameras/probe", json=payload)
        accepted = await client.post(
            "/v1/cctv/cameras/probe",
            json=payload,
            headers={"X-CustSearch-AI-Key": "camera-probe-key"},
        )

    assert denied.status_code == 401
    assert accepted.status_code == 200
    assert accepted.json() == expected.model_dump(mode="json")
    assert "configuration_reference" not in accepted.text


@pytest.mark.asyncio
async def test_latest_frame_endpoint_is_authenticated_and_never_returns_reference(monkeypatch) -> None:
    settings.api_key = SecretStr("camera-frame-key")
    expected = CameraPreviewFrame(
        content=b"safe-jpeg",
        width=640,
        height=360,
        captured_utc=datetime(2026, 8, 26, 7, 0, tzinfo=UTC),
    )
    monkeypatch.setattr("app.main.preview_manager.get_latest", lambda _reference, _age: expected)
    payload = {"configuration_reference": "env:CUSTSEARCH_CAMERA_DYNAMIC_RTSP", "max_age_seconds": 5}

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        denied = await client.post("/v1/cctv/cameras/frame", json=payload)
        accepted = await client.post(
            "/v1/cctv/cameras/frame",
            json=payload,
            headers={"X-CustSearch-AI-Key": "camera-frame-key"},
        )

    assert denied.status_code == 401
    assert accepted.status_code == 200
    assert accepted.content == b"safe-jpeg"
    assert accepted.headers["content-type"] == "image/jpeg"
    assert accepted.headers["x-frame-width"] == "640"
    assert "configuration_reference" not in accepted.text
