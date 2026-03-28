import configparser
import os
import re
import shlex
import subprocess
import sys

from ppt_assistant.core.app_icon import resolve_app_icon_path


_QUICK_LAUNCH_MEDIA_PATTERNS = [
    "*.mp3",
    "*.wav",
    "*.mp4",
    "*.mkv",
    "*.png",
    "*.jpg",
    "*.jpeg",
    "*.gif",
]
_QUICK_LAUNCH_WINDOWS_PATTERNS = ["*.exe", "*.lnk"]
_QUICK_LAUNCH_LINUX_PATTERNS = ["*.desktop", "*.AppImage", "*.appimage", "*.sh", "*.bin", "*.run"]
_DESKTOP_ENTRY_FIELD_CODE_RE = re.compile(r"%[fFuUdDnNickvm]")


def _get_project_root_dir() -> str:
    return os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _get_main_script_path() -> str:
    return os.path.join(_get_project_root_dir(), "main.py")


def get_launch_command_args(autostart: bool = False) -> list[str]:
    args: list[str]
    if getattr(sys, "frozen", False):
        args = [sys.executable]
    else:
        args = [sys.executable, _get_main_script_path()]
    if autostart:
        args.append("--autostart")
    return args


def get_launch_work_dir() -> str:
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return _get_project_root_dir()


def get_quick_launch_dialog_filter() -> str:
    app_patterns = list(_QUICK_LAUNCH_WINDOWS_PATTERNS)
    if sys.platform.startswith("linux"):
        app_patterns.extend(_QUICK_LAUNCH_LINUX_PATTERNS)
    fixed_patterns = app_patterns + _QUICK_LAUNCH_MEDIA_PATTERNS
    return (
        f"Fixed Items ({' '.join(fixed_patterns)});;"
        f"Applications ({' '.join(app_patterns)});;"
        f"Media ({' '.join(_QUICK_LAUNCH_MEDIA_PATTERNS)});;"
        "All Files (*.*)"
    )


def _spawn_detached(command: list[str], cwd: str | None = None) -> None:
    kwargs = {
        "cwd": cwd,
        "stdin": subprocess.DEVNULL,
        "stdout": subprocess.DEVNULL,
        "stderr": subprocess.DEVNULL,
        "start_new_session": True,
    }
    if sys.platform == "win32":
        kwargs["creationflags"] = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0) | getattr(subprocess, "DETACHED_PROCESS", 0)
    subprocess.Popen(command, **kwargs)


def _linux_open_with_system(path: str) -> None:
    candidates = (
        ["xdg-open", path],
        ["gio", "open", path],
        ["kioclient5", "exec", path],
        ["kioclient", "exec", path],
    )
    for command in candidates:
        try:
            _spawn_detached(command)
            return
        except FileNotFoundError:
            continue
    raise FileNotFoundError("No supported file opener found (xdg-open/gio/kioclient)")


def _is_probably_linux_executable(path: str) -> bool:
    lower = path.lower()
    return (
        os.access(path, os.X_OK)
        or lower.endswith((".appimage", ".run", ".bin", ".sh"))
    )


def _load_desktop_entry_exec(path: str) -> list[str] | None:
    parser = configparser.ConfigParser(interpolation=None)
    parser.optionxform = str
    try:
        parser.read(path, encoding="utf-8")
    except Exception:
        return None
    if not parser.has_section("Desktop Entry"):
        return None
    try:
        exec_line = parser.get("Desktop Entry", "Exec", fallback="").strip()
    except Exception:
        exec_line = ""
    if not exec_line:
        return None
    exec_line = exec_line.replace("%%", "%")
    exec_line = _DESKTOP_ENTRY_FIELD_CODE_RE.sub("", exec_line).strip()
    try:
        tokens = shlex.split(exec_line, posix=True)
    except ValueError:
        return None
    return [token for token in tokens if token]


def open_path(path: str) -> None:
    if not path:
        raise ValueError("Path is empty")
    resolved = os.path.abspath(os.path.expanduser(path))
    if not os.path.exists(resolved):
        raise FileNotFoundError(resolved)
    if sys.platform == "win32":
        os.startfile(resolved)
        return
    if sys.platform.startswith("linux"):
        lower = resolved.lower()
        if lower.endswith(".desktop"):
            command = _load_desktop_entry_exec(resolved)
            if command:
                _spawn_detached(command, cwd=os.path.dirname(resolved) or None)
                return
        if os.path.isdir(resolved):
            _linux_open_with_system(resolved)
            return
        if _is_probably_linux_executable(resolved):
            _spawn_detached([resolved], cwd=os.path.dirname(resolved) or None)
            return
        _linux_open_with_system(resolved)
        return
    _spawn_detached([resolved], cwd=os.path.dirname(resolved) or None)


def _desktop_escape_arg(value: str) -> str:
    escaped = value.replace("\\", "\\\\")
    escaped = escaped.replace('"', '\\"')
    escaped = escaped.replace("`", "\\`")
    escaped = escaped.replace("$", "\\$")
    escaped = escaped.replace(" ", "\\ ")
    return escaped


def _linux_autostart_file_path() -> str:
    config_home = os.environ.get("XDG_CONFIG_HOME") or os.path.join(os.path.expanduser("~"), ".config")
    return os.path.join(config_home, "autostart", "Luminalium.desktop")


def _set_linux_run_at_startup(enabled: bool) -> None:
    desktop_path = _linux_autostart_file_path()
    desktop_dir = os.path.dirname(desktop_path)
    if enabled:
        os.makedirs(desktop_dir, exist_ok=True)
        icon_path = resolve_app_icon_path() or ""
        exec_line = " ".join(_desktop_escape_arg(arg) for arg in get_launch_command_args(autostart=True))
        lines = [
            "[Desktop Entry]",
            "Type=Application",
            "Version=1.0",
            "Name=Luminalium",
            "Comment=Luminalium presentation assistant",
            f"Exec={exec_line}",
            f"Path={_desktop_escape_arg(get_launch_work_dir())}",
            "Terminal=false",
            "StartupNotify=false",
            "X-GNOME-Autostart-enabled=true",
        ]
        if icon_path:
            lines.append(f"Icon={_desktop_escape_arg(icon_path)}")
        with open(desktop_path, "w", encoding="utf-8", newline="\n") as file:
            file.write("\n".join(lines) + "\n")
        return
    if os.path.exists(desktop_path):
        os.remove(desktop_path)


def _set_windows_run_at_startup(enabled: bool) -> None:
    try:
        import winreg
    except ImportError:
        return
    app_name = "Luminalium"
    command = subprocess.list2cmdline(get_launch_command_args(autostart=True))
    try:
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Run",
            0,
            winreg.KEY_SET_VALUE | winreg.KEY_QUERY_VALUE,
        ) as key:
            if enabled:
                winreg.SetValueEx(key, app_name, 0, winreg.REG_SZ, command)
            else:
                try:
                    winreg.DeleteValue(key, app_name)
                except FileNotFoundError:
                    pass
    except Exception:
        pass


def set_run_at_startup(enabled: bool) -> None:
    if sys.platform == "win32":
        _set_windows_run_at_startup(bool(enabled))
        return
    if sys.platform.startswith("linux"):
        _set_linux_run_at_startup(bool(enabled))
