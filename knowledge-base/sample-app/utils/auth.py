"""API key authentication middleware."""

import os
import secrets

from fastapi import HTTPException, Security
from fastapi.security import APIKeyHeader

_api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)


def require_api_key(api_key: str = Security(_api_key_header)):
    expected = os.environ.get("API_KEY")
    if not expected:
        raise HTTPException(status_code=500, detail="API_KEY not configured on server")
    if not api_key or not secrets.compare_digest(api_key, expected):
        raise HTTPException(status_code=401, detail="Invalid or missing API key")
