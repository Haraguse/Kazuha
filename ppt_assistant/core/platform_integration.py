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
        "creationflags": getattr(subprocess, "CREATE_NO_WINDOW", 0x08000000)
        | getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0)
        | getattr(subprocess, "DETACHED_PROCESS", 0),
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


def remove_window_border(win_id) -> None:
    """Remove the default 1px border added by Windows 11 DWM."""
    if sys.platform != "win32" or not win_id:
        return
    try:
        import ctypes

        hwnd = int(win_id)

        DWMWA_BORDER_COLOR = 34
        DWMWA_COLOR_NONE = 0xFFFFFFFE

        val = ctypes.c_int(DWMWA_COLOR_NONE)
        ctypes.windll.dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_BORDER_COLOR, ctypes.byref(val), ctypes.sizeof(val)
        )

        DWMWA_USE_IMMERSIVE_DARK_MODE = 20
        dark_val = ctypes.c_int(1)
        ctypes.windll.dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ctypes.byref(dark_val), ctypes.sizeof(dark_val)
        )

        DWMWA_WINDOW_CORNER_PREFERENCE = 33
        corner_pref = ctypes.c_int(2)
        ctypes.windll.dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ctypes.byref(corner_pref), ctypes.sizeof(corner_pref)
        )
    except Exception:
        pass


def make_window_borderless_popup(win_id) -> None:
    """Force a window into a true borderless popup/tool window on Windows."""
    if sys.platform != "win32" or not win_id:
        return

    try:
        import ctypes

        hwnd = int(win_id)
        user32 = ctypes.windll.user32
        dwmapi = ctypes.windll.dwmapi

        GWL_STYLE = -16
        GWL_EXSTYLE = -20

        WS_BORDER = 0x00800000
        WS_CAPTION = 0x00C00000
        WS_DLGFRAME = 0x00400000
        WS_SYSMENU = 0x00080000
        WS_THICKFRAME = 0x00040000
        WS_MINIMIZEBOX = 0x00020000
        WS_MAXIMIZEBOX = 0x00010000
        WS_POPUP = 0x80000000

        WS_EX_APPWINDOW = 0x00040000
        WS_EX_TOOLWINDOW = 0x00000080

        SWP_NOSIZE = 0x0001
        SWP_NOMOVE = 0x0002
        SWP_NOZORDER = 0x0004
        SWP_NOACTIVATE = 0x0010
        SWP_FRAMECHANGED = 0x0020

        DWMWA_NCRENDERING_POLICY = 2
        DWMNCRP_DISABLED = 1
        DWMWA_WINDOW_CORNER_PREFERENCE = 33
        DWMWCP_DONOTROUND = 1
        DWMWA_BORDER_COLOR = 34
        DWMWA_COLOR_NONE = 0xFFFFFFFE

        style = user32.GetWindowLongW(hwnd, GWL_STYLE)
        style_without_frame = style & ~(
            WS_BORDER
            | WS_CAPTION
            | WS_DLGFRAME
            | WS_SYSMENU
            | WS_THICKFRAME
            | WS_MINIMIZEBOX
            | WS_MAXIMIZEBOX
        )
        new_style = style_without_frame | WS_POPUP

        ex_style = user32.GetWindowLongW(hwnd, GWL_EXSTYLE)
        new_ex_style = (ex_style | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW

        if new_style != style:
            user32.SetWindowLongW(hwnd, GWL_STYLE, new_style)
        if new_ex_style != ex_style:
            user32.SetWindowLongW(hwnd, GWL_EXSTYLE, new_ex_style)

        user32.SetWindowPos(
            hwnd,
            0,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED,
        )

        try:
            nc_policy = ctypes.c_int(DWMNCRP_DISABLED)
            dwmapi.DwmSetWindowAttribute(
                hwnd,
                DWMWA_NCRENDERING_POLICY,
                ctypes.byref(nc_policy),
                ctypes.sizeof(nc_policy),
            )
        except Exception:
            pass

        try:
            corner_pref = ctypes.c_int(DWMWCP_DONOTROUND)
            dwmapi.DwmSetWindowAttribute(
                hwnd,
                DWMWA_WINDOW_CORNER_PREFERENCE,
                ctypes.byref(corner_pref),
                ctypes.sizeof(corner_pref),
            )
        except Exception:
            pass

        try:
            border_color = ctypes.c_int(DWMWA_COLOR_NONE)
            dwmapi.DwmSetWindowAttribute(
                hwnd,
                DWMWA_BORDER_COLOR,
                ctypes.byref(border_color),
                ctypes.sizeof(border_color),
            )
        except Exception:
            pass
    except Exception:
        pass


def remove_window_border_delayed(widget) -> None:
    """Remove window border with delayed execution to ensure window is fully created."""
    if sys.platform != "win32":
        return

    def _do_remove():
        try:
            win_id = widget.winId()
            if win_id:
                remove_window_border(win_id)
        except Exception:
            pass

    from PySide6.QtCore import QTimer
    QTimer.singleShot(0, _do_remove)


# ---------------------------------------------------------------------------
#  原生窗口动画  ( AnimateWindow  )
# ---------------------------------------------------------------------------

_AW_HOR_POSITIVE = 0x00000001
_AW_HOR_NEGATIVE = 0x00000002
_AW_VER_POSITIVE = 0x00000004
_AW_VER_NEGATIVE = 0x00000008
_AW_CENTER = 0x00000010
_AW_HIDE = 0x00010000
_AW_ACTIVATE = 0x00020000
_AW_SLIDE = 0x00040000
_AW_BLEND = 0x00080000


def animate_window_show(widget, duration_ms: int = 200) -> bool:
    """
    Windows 原生窗口显示动画 —— 淡入效果。
    成功返回 True，非 Windows / 失败返回 False，调用方应回退到 Qt 动画。
    """
    if sys.platform != "win32":
        return False
    try:
        import ctypes

        hwnd = int(widget.winId())
        user32 = ctypes.windll.user32

        # 先把窗口在 Win32 层面隐藏，再让 AnimateWindow 动画显示
        # Qt 层已经 show() 过了（widget.isVisible() == True），所以不影响 Qt 状态
        user32.ShowWindow(hwnd, 0)  # SW_HIDE
        user32.AnimateWindow(hwnd, int(duration_ms), _AW_BLEND)
        widget.setWindowOpacity(1.0)
        return True
    except Exception:
        return False


def animate_window_hide(widget, duration_ms: int = 150) -> bool:
    """
    Windows 原生窗口隐藏动画 —— 淡出效果。
    成功返回 True，调用后窗口已隐藏，无需再调 hide()。
    """
    if sys.platform != "win32":
        return False
    try:
        import ctypes

        hwnd = int(widget.winId())
        user32 = ctypes.windll.user32
        user32.AnimateWindow(hwnd, int(duration_ms), _AW_BLEND | _AW_HIDE)
        return True
    except Exception:
        return False
