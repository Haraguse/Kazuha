import sys
from .base import SystemAPI
from .windows import WindowsSystemAPI

_instance = None

def get_system_api() -> SystemAPI:
    global _instance
    if _instance is None:
        _instance = WindowsSystemAPI()
    return _instance
