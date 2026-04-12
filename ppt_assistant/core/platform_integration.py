import os
import subprocess
import sys


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
        "creationflags": getattr(subprocess, "CREATE_NO_WINDOW", 0x08000000) | getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0) | getattr(subprocess, "DETACHED_PROCESS", 0),
    }
    subprocess.Popen(command, **kwargs)


def open_path(path: str) -> None:
    if not path:
        raise ValueError("Path is empty")
    resolved = os.path.abspath(os.path.expanduser(path))
    if not os.path.exists(resolved):
        raise FileNotFoundError(resolved)
    os.startfile(resolved)


def set_run_at_startup(enabled: bool) -> None:
    import winreg
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
