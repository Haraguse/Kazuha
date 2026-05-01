import pytest
import asyncio
import os
import sys
from pathlib import Path
from unittest.mock import patch, MagicMock

# Add project root to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from ppt_assistant.core import update_service

@pytest.mark.asyncio
async def test_check_update_dev_mode():
    with patch("ppt_assistant.core.update_service.is_dev_env", return_value=True):
        res = await update_service.check_update_impl()
        assert res["available"] is False
        assert res["reason"] == "DEV_MODE"

@pytest.mark.asyncio
async def test_check_update_api_parsing():
    mock_resp = MagicMock()
    mock_resp.status = 200
    mock_resp.json = asyncio.coroutine(lambda: {
        "tag_name": "v2.0.0",
        "body": "New release",
        "assets": [
            {"name": "Luminalium-Windows-x64.zip", "browser_download_url": "http://dl.zip", "size": 1024}
        ]
    })
    
    with patch("ppt_assistant.core.update_service.is_dev_env", return_value=False):
        with patch("aiohttp.ClientSession.get") as mock_get:
            mock_get.return_value.__aenter__.return_value = mock_resp
            res = await update_service.check_update_impl()
            assert res["available"] is True
            assert res["version"] == "2.0.0"
            assert res["changelog"] == "New release"
            assert res["download_url"] == "http://dl.zip"

def test_is_dev_env():
    # Because tests are run from python, sys.frozen is False, python.exe is in sys.executable
    assert update_service.is_dev_env() is True
