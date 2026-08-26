"""Dynamic, secret-reference-based RTSP camera connectivity boundary."""

import os
import re
import threading
import time
from dataclasses import dataclass
from datetime import UTC, datetime
from time import perf_counter
from urllib.parse import urlsplit

import cv2
from pydantic import BaseModel, Field

ENV_REFERENCE = re.compile(r"^env:(?://)?(?P<name>CUSTSEARCH_CAMERA_[A-Z0-9_]{1,96})$")


class CameraSourceConfigurationError(ValueError):
    """Indicates an invalid/missing source reference without revealing its secret value."""


class CameraProbeRequest(BaseModel):
    """Requests a bounded server-side probe using an opaque runtime configuration reference."""

    configuration_reference: str = Field(min_length=1, max_length=200)
    timeout_seconds: int = Field(default=5, ge=1, le=15)


class CameraFrameRequest(BaseModel):
    """Requests the latest JPEG from an allow-listed server-side camera source."""

    configuration_reference: str = Field(min_length=1, max_length=200)
    max_age_seconds: int = Field(default=5, ge=1, le=30)


class CameraProbeResult(BaseModel):
    """Returns connectivity metadata only; it never returns the resolved RTSP URL or a frame."""

    connected: bool
    frame_received: bool
    width: int | None = None
    height: int | None = None
    elapsed_ms: int
    status: str


@dataclass(frozen=True)
class ResolvedCameraSource:
    """Keeps the secret URL inside the capture boundary and out of API serialization/logging."""

    url: str


@dataclass(frozen=True)
class CameraPreviewFrame:
    """In-memory preview frame; callers must not persist it or include it in logs."""

    content: bytes
    width: int
    height: int
    captured_utc: datetime


class CameraFrameUnavailableError(RuntimeError):
    """Indicates that a configured camera has no recent frame available."""


class _ContinuousCameraCapture:
    """Maintains one bounded capture loop per opaque configuration reference."""

    def __init__(self, configuration_reference: str) -> None:
        self.configuration_reference = configuration_reference
        self.condition = threading.Condition()
        self.latest: CameraPreviewFrame | None = None
        self.last_requested = time.monotonic()
        self.thread = threading.Thread(target=self._run, daemon=True, name="camera-preview")
        self.thread.start()

    def get(self, max_age_seconds: int) -> CameraPreviewFrame:
        deadline = time.monotonic() + min(max_age_seconds, 10)
        with self.condition:
            self.last_requested = time.monotonic()
            while True:
                if self.latest is not None:
                    age = (datetime.now(UTC) - self.latest.captured_utc).total_seconds()
                    if age <= max_age_seconds:
                        return self.latest
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    raise CameraFrameUnavailableError("No recent camera frame is available.")
                self.condition.wait(timeout=remaining)

    def _run(self) -> None:
        delay = 0.5
        while time.monotonic() - self.last_requested <= 60:
            capture = cv2.VideoCapture()
            try:
                source = resolve_camera_source(self.configuration_reference)
                opened = capture.open(
                    source.url,
                    cv2.CAP_FFMPEG,
                    [cv2.CAP_PROP_OPEN_TIMEOUT_MSEC, 5000, cv2.CAP_PROP_READ_TIMEOUT_MSEC, 5000],
                )
                if not opened:
                    time.sleep(delay)
                    delay = min(delay * 2, 10)
                    continue
                delay = 0.5
                while time.monotonic() - self.last_requested <= 60:
                    received, frame = capture.read()
                    if not received or frame is None:
                        break
                    encoded, jpeg = cv2.imencode(".jpg", frame, [cv2.IMWRITE_JPEG_QUALITY, 75])
                    if not encoded:
                        continue
                    preview = CameraPreviewFrame(
                        content=jpeg.tobytes(),
                        width=int(frame.shape[1]),
                        height=int(frame.shape[0]),
                        captured_utc=datetime.now(UTC),
                    )
                    with self.condition:
                        self.latest = preview
                        self.condition.notify_all()
                    time.sleep(0.2)
            except CameraSourceConfigurationError:
                return
            finally:
                capture.release()
            time.sleep(delay)
            delay = min(delay * 2, 10)


class CameraPreviewManager:
    """Starts and reuses bounded capture workers without exposing resolved URLs."""

    def __init__(self, maximum_sources: int = 16) -> None:
        self.maximum_sources = maximum_sources
        self.lock = threading.Lock()
        self.sources: dict[str, _ContinuousCameraCapture] = {}

    def get_latest(self, configuration_reference: str, max_age_seconds: int) -> CameraPreviewFrame:
        resolve_camera_source(configuration_reference)
        with self.lock:
            capture = self.sources.get(configuration_reference)
            if capture is None or not capture.thread.is_alive():
                self.sources = {
                    key: value
                    for key, value in self.sources.items()
                    if value.thread.is_alive()
                }
                if len(self.sources) >= self.maximum_sources:
                    raise CameraFrameUnavailableError(
                        "Camera preview source limit has been reached."
                    )
                capture = _ContinuousCameraCapture(configuration_reference)
                self.sources[configuration_reference] = capture
        return capture.get(max_age_seconds)


preview_manager = CameraPreviewManager()


def resolve_camera_source(configuration_reference: str) -> ResolvedCameraSource:
    """Resolve only allow-listed camera environment variables, never caller-provided URLs."""

    match = ENV_REFERENCE.fullmatch(configuration_reference.strip())
    if match is None:
        raise CameraSourceConfigurationError(
            "Camera reference must use env:CUSTSEARCH_CAMERA_<NAME>."
        )

    value = os.environ.get(match.group("name"), "").strip()
    if not value:
        raise CameraSourceConfigurationError(
            "Camera source secret is not configured on this server."
        )

    parsed = urlsplit(value)
    if parsed.scheme.casefold() not in {"rtsp", "rtsps"} or not parsed.hostname:
        raise CameraSourceConfigurationError("Configured camera source is not a valid RTSP URL.")
    return ResolvedCameraSource(value)


def probe_camera(configuration_reference: str, timeout_seconds: int) -> CameraProbeResult:
    """Open one source briefly, read at most one frame, then always release the connection."""

    source = resolve_camera_source(configuration_reference)
    started = perf_counter()
    capture = cv2.VideoCapture()
    timeout_ms = timeout_seconds * 1000
    parameters = [
        cv2.CAP_PROP_OPEN_TIMEOUT_MSEC,
        timeout_ms,
        cv2.CAP_PROP_READ_TIMEOUT_MSEC,
        timeout_ms,
    ]
    try:
        connected = bool(capture.open(source.url, cv2.CAP_FFMPEG, parameters))
        if not connected:
            return CameraProbeResult(
                connected=False,
                frame_received=False,
                elapsed_ms=round((perf_counter() - started) * 1000),
                status="Camera connection failed.",
            )

        frame_received, frame = capture.read()
        height = int(frame.shape[0]) if frame_received and frame is not None else None
        width = int(frame.shape[1]) if frame_received and frame is not None else None
        return CameraProbeResult(
            connected=True,
            frame_received=bool(frame_received),
            width=width,
            height=height,
            elapsed_ms=round((perf_counter() - started) * 1000),
            status="Frame received." if frame_received else "Connected, but no frame was received.",
        )
    finally:
        capture.release()
