import os
import sys
import threading
from typing import Optional

_env_lock = threading.RLock()
_original_environ: dict[str, str] = {}
_device_uuid_cache: Optional[str] = None


def _capture_original_environ() -> None:
    global _original_environ
    with _env_lock:
        if not _original_environ:
            _original_environ = dict(os.environ)


def safe_environ_get(key: str, default: Optional[str] = None) -> Optional[str]:
    with _env_lock:
        return os.environ.get(key, default)


def safe_environ_get_original(key: str, default: Optional[str] = None) -> Optional[str]:
    with _env_lock:
        return _original_environ.get(key, default)


def safe_environ_pop(key: str, default: Optional[str] = None) -> Optional[str]:
    with _env_lock:
        return os.environ.pop(key, default)


def safe_environ_set(key: str, value: str) -> None:
    with _env_lock:
        os.environ[key] = value


def safe_environ_setdefault(key: str, default: str) -> str:
    with _env_lock:
        return os.environ.setdefault(key, default)


def safe_environ_update(updates: dict[str, str]) -> None:
    with _env_lock:
        os.environ.update(updates)


def safe_environ_contains(key: str) -> bool:
    with _env_lock:
        return key in os.environ


def get_original_environ_copy() -> dict[str, str]:
    with _env_lock:
        return _original_environ.copy()


def get_device_uuid() -> str:
    global _device_uuid_cache
    if _device_uuid_cache is not None:
        return _device_uuid_cache
    uuid_str = ""
    try:
        if sys.platform == "win32":
            import winreg
            key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Cryptography", 0, winreg.KEY_READ | winreg.KEY_WOW64_64KEY)
            uuid_str = winreg.QueryValueEx(key, "MachineGuid")[0]
            winreg.CloseKey(key)
        elif sys.platform == "linux":
            machine_id_paths = ["/etc/machine-id", "/var/lib/dbus/machine-id"]
            for path in machine_id_paths:
                if os.path.exists(path):
                    with open(path, "r", encoding="utf-8") as f:
                        uuid_str = f.read().strip()
                    if uuid_str:
                        break
        if not uuid_str:
            import uuid
            uuid_str = str(uuid.getnode())
    except Exception:
        import uuid
        uuid_str = str(uuid.uuid4())
    uuid_str = str(uuid_str).strip().lower()
    _device_uuid_cache = uuid_str
    return _device_uuid_cache
