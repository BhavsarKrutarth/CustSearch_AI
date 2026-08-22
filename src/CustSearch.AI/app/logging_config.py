"""Structured, correlation-aware diagnostic logging for the AI service."""

import json
import logging
from contextvars import ContextVar
from datetime import UTC, datetime

correlation_id_context: ContextVar[str] = ContextVar("correlation_id", default="")


class JsonFormatter(logging.Formatter):
    """Render safe diagnostic events as one JSON object per line."""

    def format(self, record: logging.LogRecord) -> str:
        payload: dict[str, str | int] = {
            "timestampUtc": datetime.now(UTC).isoformat(),
            "level": record.levelname,
            "application": "CustSearch.AI",
            "correlationId": correlation_id_context.get(),
            "message": record.getMessage(),
        }
        if record.exc_info:
            payload["exception"] = self.formatException(record.exc_info)
        return json.dumps(payload, ensure_ascii=True)


def configure_logging() -> None:
    """Configure a single stdout handler without logging request bodies or credentials."""

    handler = logging.StreamHandler()
    handler.setFormatter(JsonFormatter())
    root_logger = logging.getLogger()
    root_logger.handlers.clear()
    root_logger.addHandler(handler)
    root_logger.setLevel(logging.INFO)
