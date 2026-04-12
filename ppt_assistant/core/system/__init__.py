import sys
from .base import SystemAPI

_instance = None

def get_system_api() -> SystemAPI:
    global _instance
    if _instance is None:
        if sys.platform == "win32":
            from .windows import WindowsSystemAPI

            _instance = WindowsSystemAPI()
        else:
            from .linux import LinuxSystemAPI

            _instance = LinuxSystemAPI()
    return _instance
