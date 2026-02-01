import os
import sys

from PySide6.QtGui import QIcon


def resolve_project_root_dir() -> str:
    return os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def resolve_icons_dir() -> str:
    return os.path.join(resolve_project_root_dir(), "icons")


def resolve_app_icon_path() -> str | None:
    candidates: list[str] = []

    root_dir = resolve_project_root_dir()
    candidates.append(os.path.join(root_dir, "icons", "logo.ico"))

    if getattr(sys, "frozen", False):
        exe_dir = os.path.dirname(sys.executable)
        candidates.append(os.path.join(exe_dir, "icons", "logo.ico"))
        candidates.append(sys.executable)

    for path in candidates:
        if path and os.path.exists(path):
            return path
    return None


def load_app_icon() -> QIcon:
    path = resolve_app_icon_path()
    if not path:
        return QIcon()
    return QIcon(path)
