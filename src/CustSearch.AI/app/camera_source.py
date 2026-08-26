"""Dynamic, secret-reference-based RTSP camera connectivity boundary."""

import os
import re
from dataclasses import dataclass
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
