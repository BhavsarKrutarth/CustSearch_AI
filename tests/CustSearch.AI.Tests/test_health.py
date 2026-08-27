"""Foundation health and correlation contract tests."""

import httpx
import pytest

from app.main import CORRELATION_HEADER, app


@pytest.mark.asyncio
async def test_live_health_supports_demo_mode() -> None:
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get("/health/live")

    assert response.status_code == 200
    assert response.json() == {
        "service": "CustSearch.AI",
        "status": "Healthy",
        "environment": "Development",
        "demoMode": True,
    }


@pytest.mark.asyncio
async def test_valid_correlation_id_is_returned() -> None:
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get(
            "/health/live", headers={CORRELATION_HEADER: "python-test-123"}
        )

    assert response.headers[CORRELATION_HEADER] == "python-test-123"


@pytest.mark.asyncio
async def test_unsafe_correlation_id_is_replaced() -> None:
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        response = await client.get(
            "/health/live", headers={CORRELATION_HEADER: "unsafe value"}
        )

    replacement = response.headers[CORRELATION_HEADER]
    assert replacement != "unsafe value"
    assert len(replacement) == 32
