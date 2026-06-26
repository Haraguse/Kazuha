"""
线程安全的环境变量访问工具

在多线程环境下保护 os.environ 访问，防止数据竞争。
同时保存启动时的原始环境变量，避免重启时丢失环境信息。
"""
import os
import threading
from typing import Optional

_env_lock = threading.RLock()

# 保存启动时的原始环境变量快照
# 用于应对启动时修改环境变量（如 pop WAYLAND_DISPLAY）导致重启后丢失的问题
_original_environ: dict[str, str] = {}


def _capture_original_environ() -> None:
    """捕获启动时的原始环境变量（仅在启动时调用一次）"""
    global _original_environ
    with _env_lock:
        if not _original_environ:
            _original_environ = dict(os.environ)


def safe_environ_get(key: str, default: Optional[str] = None) -> Optional[str]:
    """线程安全地获取环境变量"""
    with _env_lock:
        return os.environ.get(key, default)


def safe_environ_get_original(key: str, default: Optional[str] = None) -> Optional[str]:
    """获取启动时的原始环境变量值（不受运行时修改影响）"""
    with _env_lock:
        return _original_environ.get(key, default)


def safe_environ_pop(key: str, default: Optional[str] = None) -> Optional[str]:
    """线程安全地删除并返回环境变量"""
    with _env_lock:
        return os.environ.pop(key, default)


def safe_environ_set(key: str, value: str) -> None:
    """线程安全地设置环境变量"""
    with _env_lock:
        os.environ[key] = value


def safe_environ_setdefault(key: str, default: str) -> str:
    """线程安全地设置默认环境变量"""
    with _env_lock:
        return os.environ.setdefault(key, default)


def safe_environ_update(updates: dict[str, str]) -> None:
    """线程安全地批量更新环境变量"""
    with _env_lock:
        os.environ.update(updates)


def safe_environ_contains(key: str) -> bool:
    """线程安全地检查环境变量是否存在"""
    with _env_lock:
        return key in os.environ


def get_original_environ_copy() -> dict[str, str]:
    """获取启动时原始环境变量的副本（用于重启等场景）"""
    with _env_lock:
        return _original_environ.copy()
