"""FastAPI entry point for CCTV and recognition capabilities."""

import logging
import re
import secrets
from time import perf_counter
from uuid import uuid4

from fastapi import FastAPI, Header, HTTPException, Request, status
from fastapi.concurrency import run_in_threadpool
from fastapi.responses import Response

from app.camera_source import (
    CameraFrameRequest,
    CameraFrameUnavailableError,
    CameraProbeRequest,
    CameraProbeResult,
    CameraSourceConfigurationError,
    preview_manager,
    probe_camera,
)
from app.config import get_settings
from app.logging_config import configure_logging, correlation_id_context
from app.tracking import NormalizedEvent, NormalizeRequest, deterministic_demo_events, normalize

CORRELATION_HEADER = "X-Correlation-ID"
CORRELATION_PATTERN = re.compile(r"^[A-Za-z0-9_.-]{1,64}$")

configure_logging()
logger = logging.getLogger(__name__)
settings = get_settings()
app = FastAPI(title=settings.service_name, version="0.1.0")


@app.middleware("http")
async def correlation_middleware(request: Request, call_next) -> Response:
    """Propagate only validated log-safe correlation identifiers."""

    supplied = request.headers.get(CORRELATION_HEADER, "")
    correlation_id = supplied if CORRELATION_PATTERN.fullmatch(supplied) else uuid4().hex
    context_token = correlation_id_context.set(correlation_id)
    started = perf_counter()
    try:
        response = await call_next(request)
        response.headers[CORRELATION_HEADER] = correlation_id
        duration_ms = round((perf_counter() - started) * 1000)
        logger.info(
            "HTTP %s %s completed with %s in %sms",
            request.method,
            request.url.path,
            response.status_code,
            duration_ms,
        )
        return response
    finally:
        correlation_id_context.reset(context_token)


@app.get("/health/live", tags=["Health"])
async def live_health() -> dict[str, str | bool]:
    """Return a camera-independent liveness response for CI and service monitoring."""

    return {
        "service": settings.service_name,
        "status": "Healthy",
        "environment": settings.environment,
        "demoMode": settings.demo_mode,
    }


def require_api_key(value: str | None) -> None:
    """Authenticate the Python boundary without logging or returning configured credentials."""

    configured = settings.api_key.get_secret_value()
    if not configured:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="AI API key is not configured",
        )
    if value is None or not secrets.compare_digest(value, configured):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid service credential",
        )


@app.post("/v1/cctv/events/normalize", response_model=list[NormalizedEvent], tags=["CCTV"])
async def normalize_events(
    request: NormalizeRequest, x_custsearch_ai_key: str | None = Header(default=None)
) -> list[NormalizedEvent]:
    """Normalize anonymous detector metadata; this endpoint never receives SQL credentials."""

    require_api_key(x_custsearch_ai_key)
    return normalize(request)


@app.post("/v1/cctv/cameras/probe", response_model=CameraProbeResult, tags=["CCTV"])
async def probe_camera_source(
    request: CameraProbeRequest,
    x_custsearch_ai_key: str | None = Header(default=None),
) -> CameraProbeResult:
    """Probe a dynamically configured RTSP source without exposing its URL or captured frame."""

    require_api_key(x_custsearch_ai_key)
    try:
        return await run_in_threadpool(
            probe_camera, request.configuration_reference, request.timeout_seconds
        )
    except CameraSourceConfigurationError as exception:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT, detail=str(exception)
        ) from exception


@app.post("/v1/cctv/cameras/frame", tags=["CCTV"])
async def latest_camera_frame(
    request: CameraFrameRequest,
    x_custsearch_ai_key: str | None = Header(default=None),
) -> Response:
    """Return one recent in-memory JPEG to the authenticated .NET preview proxy."""

    require_api_key(x_custsearch_ai_key)
    try:
        frame = await run_in_threadpool(
            preview_manager.get_latest,
            request.configuration_reference,
            request.max_age_seconds,
        )
    except CameraSourceConfigurationError as exception:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT, detail=str(exception)
        ) from exception
    except CameraFrameUnavailableError as exception:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail=str(exception)
        ) from exception
    return Response(
        content=frame.content,
        media_type="image/jpeg",
        headers={
            "Cache-Control": "no-store, no-cache, max-age=0",
            "X-Frame-Width": str(frame.width),
            "X-Frame-Height": str(frame.height),
            "X-Frame-Captured-Utc": frame.captured_utc.isoformat().replace("+00:00", "Z"),
        },
    )


@app.get("/v1/cctv/demo/events", response_model=list[NormalizedEvent], tags=["CCTV"])
async def demo_events(
    x_custsearch_ai_key: str | None = Header(default=None),
) -> list[NormalizedEvent]:
    """Return deterministic CI fixtures only when Demo Mode is explicitly enabled."""

    require_api_key(x_custsearch_ai_key)
    if not settings.demo_mode:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Demo Mode is disabled")
    return deterministic_demo_events()
