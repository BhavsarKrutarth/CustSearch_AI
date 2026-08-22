"""FastAPI entry point for CCTV and recognition capabilities."""

import logging
import re
from time import perf_counter
from uuid import uuid4

from fastapi import FastAPI, Request
from fastapi.responses import Response

from app.config import get_settings
from app.logging_config import configure_logging, correlation_id_context

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
