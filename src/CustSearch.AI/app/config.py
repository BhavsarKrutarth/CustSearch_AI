"""Environment-backed settings for the Python AI service."""

from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Controls service identity and safe camera-independent Demo Mode."""

    model_config = SettingsConfigDict(env_prefix="CUSTSEARCH_AI_", case_sensitive=False)

    service_name: str = "CustSearch.AI"
    environment: str = "Development"
    demo_mode: bool = True


@lru_cache
def get_settings() -> Settings:
    """Return one validated settings instance per process."""

    return Settings()
