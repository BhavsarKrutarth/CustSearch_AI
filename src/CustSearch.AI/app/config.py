"""Environment-backed settings for the Python AI service."""

from functools import lru_cache

from pydantic import SecretStr, model_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Controls service identity and safe camera-independent Demo Mode."""

    model_config = SettingsConfigDict(env_prefix="CUSTSEARCH_AI_", case_sensitive=False)

    service_name: str = "CustSearch.AI"
    environment: str = "Development"
    demo_mode: bool = True
    api_key: SecretStr = SecretStr("")
    dotnet_event_url: str = ""
    service_id: str = ""
    service_secret: SecretStr = SecretStr("")

    @model_validator(mode="after")
    def reject_production_demo(self) -> "Settings":
        """Demo fixtures must never activate silently in a production process."""

        if self.environment.casefold() == "production" and self.demo_mode:
            raise ValueError("CCTV Demo Mode cannot be enabled in Production")
        return self


@lru_cache
def get_settings() -> Settings:
    """Return one validated settings instance per process."""

    return Settings()
