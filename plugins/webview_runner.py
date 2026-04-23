import sys
import os

# Nuitka standalone detection and compatibility
if hasattr(sys, "nuitka_binary"):
    sys.frozen = True

import json
import ctypes
import tempfile
import subprocess
import base64
from json import JSONDecodeError

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if ROOT_DIR not in sys.path:
    sys.path.insert(0, ROOT_DIR)


from PySide6.QtWidgets import QApplication, QFileDialog
from PySide6.QtQuick import QQuickView
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWebEngineCore import (
    QWebEngineScript,
    QWebEngineSettings,
    QWebEngineProfile,
    QWebEnginePage,
)
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtCore import (
    QObject,
    Slot,
    QUrl,
    QFile,
    QIODevice,
    Qt,
    QTimer,
    QBuffer,
    QByteArray,
    QJsonValue,
    QCoreApplication,
    QStandardPaths,
    QPoint,
)
from PySide6.QtGui import QColor, QImage, QGuiApplication, QIcon
from ppt_assistant.core.icon_helper import get_file_icon_base64
from ppt_assistant.core.platform_integration import (
    get_quick_launch_dialog_filter,
    set_run_at_startup as platform_set_run_at_startup,
)

DWMWA_WINDOW_CORNER_PREFERENCE = 33
DWMWCP_ROUND = 2
DWMWA_USE_IMMERSIVE_DARK_MODE = 20
DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19
DWMWA_BORDER_COLOR = 34
DWMWA_CAPTION_COLOR = 35
DWMWA_TEXT_COLOR = 36
DWMWA_SYSTEMBACKDROP_TYPE = 38
_DWM_COLOR_NONE = 0xFFFFFFFE
_DWM_COLOR_DEFAULT = 0xFFFFFFFF
_EXISTING_WINDOW_NOTIFY_MESSAGE = 0
_SHARED_PROFILE = None
_WEBENGINE_WARMUP_PAGE = None
_WEBENGINE_WARMUP_DONE = False

if sys.platform == "win32":
    try:
        _EXISTING_WINDOW_NOTIFY_MESSAGE = ctypes.windll.user32.RegisterWindowMessageW(
            "Luminalium.WebView.NotifyExistingWindow"
        )
    except Exception:
        _EXISTING_WINDOW_NOTIFY_MESSAGE = 0

DWMSBT_AUTO = 0
DWMSBT_NONE = 1
DWMSBT_MAINWINDOW = 2
DWMSBT_TRANSIENTWINDOW = 3
DWMSBT_TABBEDWINDOW = 4


def _supports_system_backdrop():
    try:
        return sys.getwindowsversion().build >= 22000
    except Exception:
        return False


def _safe_set_widget_attr(widget, attr, enabled):
    if attr is None:
        return
    try:
        widget.setAttribute(attr, enabled)
    except Exception:
        pass


def _resolve_logo_ico_path() -> str | None:
    root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    candidates: list[str] = [
        os.path.join(root_dir, "icons", "logo.ico"),
    ]

    if getattr(sys, "frozen", False):
        exe_dir = os.path.dirname(sys.executable)
        candidates.insert(0, os.path.join(exe_dir, "icons", "logo.ico"))
        candidates.append(sys.executable)

    for path in candidates:
        if path and os.path.exists(path):
            return path
    return None


def _resolve_logo_svg_path() -> str | None:
    root_dir = _get_user_root_dir()
    candidates: list[str] = [
        os.path.join(root_dir, "icons", "logo.svg"),
        os.path.join(root_dir, "icons", "banner.png"),
    ]
    for path in candidates:
        if path and os.path.exists(path):
            return path
    return None


def _resolve_misans_font_path() -> str | None:
    root_dir = _get_user_root_dir()
    candidates: list[str] = [
        os.path.join(root_dir, "fonts", "MiSansVF.ttf"),
        os.path.join(root_dir, "fonts", "MiSansTCVF.ttf"),
        os.path.join(root_dir, "fonts", "MiSansJapaneseVF.ttf"),
    ]
    for path in candidates:
        if path and os.path.exists(path):
            return path
    return None


def _resolve_window_loader_qml_path() -> str | None:
    base_dir = os.path.dirname(os.path.abspath(__file__))
    path = os.path.join(base_dir, "WindowLoadingOverlay.qml")
    return path if os.path.exists(path) else None


def _resolve_window_loader_title(window_tag, title, settings=None):
    language = "zh-cn"
    if isinstance(settings, dict):
        try:
            language = (
                str((settings.get("General", {}) or {}).get("Language", "zh-CN"))
                .strip()
                .lower()
            )
        except Exception:
            language = "zh-cn"
    title_maps = {
        "zh-cn": {
            "settings": "设置",
            "timer": "计时器",
            "onboarding": "欢迎使用",
        },
        "zh-tw": {
            "settings": "設定",
            "timer": "計時器",
            "onboarding": "歡迎使用",
        },
        "yue-hk": {
            "settings": "設定",
            "timer": "計時器",
            "onboarding": "歡迎使用",
        },
        "ja-jp": {
            "settings": "設定",
            "timer": "タイマー",
            "onboarding": "ようこそ",
        },
        "en-us": {
            "settings": "Settings",
            "timer": "Timer",
            "onboarding": "Welcome",
        },
    }
    title_map = title_maps.get(language, title_maps["zh-cn"])
    mapped = title_map.get(str(window_tag or "").strip().lower())
    if mapped:
        return mapped
    fallback = str(title or "").strip()
    return fallback or "Luminalium"


def _get_user_root_dir() -> str:
    root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    if getattr(sys, "frozen", False):
        root_dir = os.path.dirname(sys.executable)
    return root_dir


def _list_user_themes() -> list[dict]:
    root_dir = _get_user_root_dir()
    # Support both "user" and "users" folder names
    themes_dir = os.path.join(root_dir, "user", "themes")
    if not os.path.isdir(themes_dir):
        themes_dir = os.path.join(root_dir, "users", "themes")

    results: list[dict] = []
    if not os.path.isdir(themes_dir):
        return results
    for name in os.listdir(themes_dir):
        theme_dir = os.path.join(themes_dir, name)
        if not os.path.isdir(theme_dir):
            continue
        manifest_path = os.path.join(theme_dir, "manifest.json")
        preview_png = os.path.join(theme_dir, "preview.png")
        preview_jpg = os.path.join(theme_dir, "preview.jpg")
        html_path = os.path.join(theme_dir, "index.html")
        if not os.path.exists(html_path):
            continue
        if not os.path.exists(manifest_path):
            continue
        if not (os.path.exists(preview_png) or os.path.exists(preview_jpg)):
            continue
        try:
            with open(manifest_path, "r", encoding="utf-8-sig") as f:
                data = json.load(f)
            # Remove strict name check to allow more flexible theme naming
            # if data.get("name") != name:
            #     continue
        except Exception:
            continue
        results.append({"id": name, "name": data.get("name", name)})
    return results


def _load_app_icon() -> QIcon:
    path = _resolve_logo_ico_path()
    if not path:
        return QIcon()
    return QIcon(path)


def _get_windows_dark_mode():
    try:
        import winreg

        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
        ) as key:
            val, _ = winreg.QueryValueEx(key, "AppsUseLightTheme")
            return int(val) == 0
    except Exception:
        return False


def _resolve_theme_dark(theme_mode):
    mode = str(theme_mode or "").lower()
    if mode == "dark":
        return True
    if mode == "light":
        return False
    if mode == "auto":
        return _get_windows_dark_mode()
    return False


def _apply_window_theme(hwnd, is_dark, backdrop_type=None):
    if not hwnd:
        return
    try:
        dwmapi = ctypes.windll.dwmapi
        uxtheme = ctypes.windll.uxtheme
        user32 = ctypes.windll.user32
        val = ctypes.c_int(1 if is_dark else 0)
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ctypes.byref(val), ctypes.sizeof(val)
        )
        dwmapi.DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
            ctypes.byref(val),
            ctypes.sizeof(val),
        )
        use_system_caption = backdrop_type is not None and backdrop_type != DWMSBT_NONE
        if use_system_caption:
            border = ctypes.c_int(_DWM_COLOR_DEFAULT)
            caption = ctypes.c_int(_DWM_COLOR_DEFAULT)
            text = ctypes.c_int(_DWM_COLOR_DEFAULT)
        elif is_dark:
            border = ctypes.c_int(0x00202020)
            caption = ctypes.c_int(0x00202020)
            text = ctypes.c_int(0x00FFFFFF)
        else:
            border = ctypes.c_int(_DWM_COLOR_DEFAULT)
            caption = ctypes.c_int(_DWM_COLOR_DEFAULT)
            text = ctypes.c_int(_DWM_COLOR_DEFAULT)
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_BORDER_COLOR, ctypes.byref(border), ctypes.sizeof(border)
        )
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_CAPTION_COLOR, ctypes.byref(caption), ctypes.sizeof(caption)
        )
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_TEXT_COLOR, ctypes.byref(text), ctypes.sizeof(text)
        )
        theme = "DarkMode_Explorer" if is_dark else "Explorer"
        uxtheme.SetWindowTheme(hwnd, ctypes.c_wchar_p(theme), None)
        flags = 0x0001 | 0x0002 | 0x0004 | 0x0020
        user32.SetWindowPos(hwnd, 0, 0, 0, 0, 0, flags)
    except Exception:
        pass


def _resolve_system_backdrop_type(settings, window_tag):
    if window_tag not in ("settings", "timer", "crash"):
        return None
    if not _supports_system_backdrop():
        return DWMSBT_NONE
    if not isinstance(settings, dict):
        return DWMSBT_NONE
    general = (
        settings.get("General") if isinstance(settings.get("General"), dict) else {}
    )
    enabled = bool(general.get("SystemBackdropEnabled"))
    if not enabled:
        return DWMSBT_NONE
    type_value = str(general.get("SystemBackdropType", "Mica"))
    mapping = {
        "Mica": DWMSBT_MAINWINDOW,
        "Acrylic": DWMSBT_TRANSIENTWINDOW,
        "MicaAlt": DWMSBT_TABBEDWINDOW,
        "Opaque": DWMSBT_NONE,
    }
    return mapping.get(type_value, DWMSBT_MAINWINDOW)


def _animations_disabled(settings) -> bool:
    if not isinstance(settings, dict):
        return False
    general = settings.get("General")
    if not isinstance(general, dict):
        return False
    return bool(general.get("DisableAnimations"))


def _apply_system_backdrop(hwnd, backdrop_type):
    if not hwnd or backdrop_type is None:
        return
    try:
        dwmapi = ctypes.windll.dwmapi
        val = ctypes.c_int(int(backdrop_type))
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ctypes.byref(val), ctypes.sizeof(val)
        )
        class MARGINS(ctypes.Structure):
            _fields_ = [
                ("cxLeftWidth", ctypes.c_int),
                ("cxRightWidth", ctypes.c_int),
                ("cyTopHeight", ctypes.c_int),
                ("cyBottomHeight", ctypes.c_int),
            ]

        margins = (
            MARGINS(-1, -1, -1, -1)
            if backdrop_type != DWMSBT_NONE
            else MARGINS(0, 0, 0, 0)
        )
        dwmapi.DwmExtendFrameIntoClientArea(hwnd, ctypes.byref(margins))
    except Exception:
        pass


def _force_dwm_redraw(hwnd):
    if not hwnd:
        return
    try:
        user32 = ctypes.windll.user32
        SWP_NOMOVE = 0x0002
        SWP_NOSIZE = 0x0001
        SWP_NOZORDER = 0x0004
        SWP_NOACTIVATE = 0x0010
        SWP_FRAMECHANGED = 0x0020
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
            ctypes.windll.dwmapi.DwmFlush()
        except Exception:
            pass
        RDW_INVALIDATE = 0x0001
        RDW_ERASE = 0x0004
        RDW_UPDATENOW = 0x0100
        RDW_FRAME = 0x0400
        RDW_ALLCHILDREN = 0x0080
        user32.RedrawWindow(
            hwnd,
            None,
            None,
            RDW_INVALIDATE | RDW_ERASE | RDW_UPDATENOW | RDW_FRAME | RDW_ALLCHILDREN,
        )
    except Exception:
        pass


def _maybe_add_vxkex_path():
    if not _is_windows7():
        return
    candidates = [
        r"C:\Program Files\VxKex\Kex64",
        r"C:\Program Files\VxKex\Kex86",
        r"C:\Program Files (x86)\VxKex\Kex64",
        r"C:\Program Files (x86)\VxKex\Kex86",
    ]
    existing = os.environ.get("PATH", "")
    for path in candidates:
        dll_path = os.path.join(path, "KxNt.dll")
        if os.path.exists(dll_path):
            if path not in existing.split(os.pathsep):
                os.environ["PATH"] = path + os.pathsep + existing
            break


def _is_windows7():
    try:
        v = sys.getwindowsversion()
        return v.major == 6 and v.minor == 1
    except Exception:
        return False


def _get_screen_refresh_rate():
    try:
        import ctypes

        user32 = ctypes.windll.user32
        hdc = user32.GetDC(0)
        rate = ctypes.windll.gdi32.GetDeviceCaps(hdc, 116)  # VREFRESH
        user32.ReleaseDC(0, hdc)
        return rate if rate > 1 else 60
    except:
        return 60


def _configure_profile(profile):
    if profile is None:
        return None
    if getattr(profile, "_luminalium_configured", False):
        return profile

    storage_root = None
    cache_root = None
    storage_candidates = []
    cache_candidates = []
    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        storage_candidates.append(
            os.path.join(local_app_data, "Luminalium", "QtWebEngine", "storage")
        )
        cache_candidates.append(
            os.path.join(local_app_data, "Luminalium", "QtWebEngine", "cache")
        )
    try:
        app_data_location = QStandardPaths.writableLocation(
            QStandardPaths.AppLocalDataLocation
        )
        if app_data_location:
            storage_candidates.append(
                os.path.join(app_data_location, "QtWebEngine", "storage")
            )
        cache_location = QStandardPaths.writableLocation(QStandardPaths.CacheLocation)
        if cache_location:
            cache_candidates.append(
                os.path.join(cache_location, "QtWebEngine", "cache")
            )
    except Exception:
        pass
    storage_candidates.append(
        os.path.join(tempfile.gettempdir(), "Luminalium", "QtWebEngine", "storage")
    )
    cache_candidates.append(
        os.path.join(tempfile.gettempdir(), "Luminalium", "QtWebEngine", "cache")
    )

    for candidate in storage_candidates:
        try:
            os.makedirs(candidate, exist_ok=True)
            storage_root = candidate
            break
        except Exception:
            continue
    for candidate in cache_candidates:
        try:
            os.makedirs(candidate, exist_ok=True)
            cache_root = candidate
            break
        except Exception:
            continue

    try:
        if storage_root:
            profile.setPersistentStoragePath(storage_root)
        if cache_root:
            # Keep cache process-local to avoid Chromium lock/contention issues.
            cache_path = os.path.join(cache_root, f"pid-{os.getpid()}")
            os.makedirs(cache_path, exist_ok=True)
            profile.setCachePath(cache_path)
            profile.setHttpCacheType(QWebEngineProfile.HttpCacheType.DiskHttpCache)
            profile.setHttpCacheMaximumSize(50 * 1024 * 1024)  # 50 MB
        else:
            profile.setHttpCacheType(QWebEngineProfile.HttpCacheType.MemoryHttpCache)
    except Exception:
        pass
    try:
        profile._luminalium_configured = True
    except Exception:
        pass
    return profile


def _get_shared_profile():
    global _SHARED_PROFILE
    if _SHARED_PROFILE is None:
        app = QCoreApplication.instance()
        _SHARED_PROFILE = QWebEngineProfile("LuminaliumSharedProfile", app)
    return _configure_profile(_SHARED_PROFILE)


def _warmup_webengine():
    global _WEBENGINE_WARMUP_PAGE, _WEBENGINE_WARMUP_DONE
    if _WEBENGINE_WARMUP_DONE:
        return
    try:
        profile = _get_shared_profile()
        app = QCoreApplication.instance()
        if profile is None or app is None:
            return
        page = QWebEnginePage(profile, app)
        page.setBackgroundColor(Qt.transparent)

        def _finish(*_args):
            global _WEBENGINE_WARMUP_PAGE, _WEBENGINE_WARMUP_DONE
            if _WEBENGINE_WARMUP_DONE:
                return
            _WEBENGINE_WARMUP_DONE = True
            _WEBENGINE_WARMUP_PAGE = None
            try:
                page.deleteLater()
            except Exception:
                pass

        page.loadFinished.connect(_finish)
        QTimer.singleShot(1500, _finish)
        page.setHtml(
            "<!doctype html><html><head></head><body></body></html>",
            QUrl("about:blank"),
        )
        _WEBENGINE_WARMUP_PAGE = page
    except Exception:
        _WEBENGINE_WARMUP_DONE = True


def _should_defer_initial_load(url, title, explicit_defer=False):
    if explicit_defer:
        return True
    try:
        qurl = QUrl.fromUserInput(str(url))
    except Exception:
        qurl = QUrl()
    title_text = str(title or "").lower()
    if (
        title_text == "settings"
        or "onboarding" in title_text
        or "timer plugin" in title_text
    ):
        return True
    if qurl.isLocalFile():
        try:
            local_path = qurl.toLocalFile()
            if local_path and os.path.exists(local_path):
                size = os.path.getsize(local_path)
                if size >= 80 * 1024:
                    return True
        except Exception:
            pass
    return False


def _apply_chromium_flags():
    _maybe_add_vxkex_path()
    # Configure for smooth GPU rendering
    flags = [
        "--enable-zero-copy",
        "--enable-features=BackForwardCache,GpuRasterization,VaapiVideoDecoder",
        "--disable-frame-rate-limit",
        "--disable-gpu-vsync",
        "--disable-renderer-backgrounding",
        "--disable-background-timer-throttling",
        "--disable-backgrounding-occluded-windows",
        "--disable-breakpad",
        "--disable-component-update",
        "--disable-print-preview",
        "--disable-speech-api",
        "--disable-web-security",
        "--wm-window-animations-disabled",
        "--enable-gpu-rasterization",
        "--enable-hardware-overlays",
        "--ignore-gpu-blocklist",
    ]

    if _is_virtual_gpu():
        flags.extend(
            [
                "--disable-gpu",
                "--disable-gpu-compositing",
            ]
        )

    rate = _get_screen_refresh_rate()
    target_fps = rate * 3
    os.environ["LUMINALIUM_TARGET_FPS"] = str(target_fps)

    current = os.environ.get("QTWEBENGINE_CHROMIUM_FLAGS", "").strip()
    if current:
        merged = current.split()
        if "--disable-gpu-shader-disk-cache" in merged:
            merged.remove("--disable-gpu-shader-disk-cache")
        for flag in flags:
            if flag not in merged:
                merged.append(flag)
        os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = " ".join(merged)
    else:
        os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = " ".join(flags)


_VIRTUAL_GPU = None


def _is_virtual_gpu():
    global _VIRTUAL_GPU
    if _VIRTUAL_GPU is not None:
        return _VIRTUAL_GPU
    names = []
    try:
        from ctypes import wintypes

        class DISPLAY_DEVICEW(ctypes.Structure):
            _fields_ = [
                ("cb", wintypes.DWORD),
                ("DeviceName", wintypes.WCHAR * 32),
                ("DeviceString", wintypes.WCHAR * 128),
                ("StateFlags", wintypes.DWORD),
                ("DeviceID", wintypes.WCHAR * 128),
                ("DeviceKey", wintypes.WCHAR * 128),
            ]

        user32 = ctypes.windll.user32
        i = 0
        while True:
            dd = DISPLAY_DEVICEW()
            dd.cb = ctypes.sizeof(DISPLAY_DEVICEW)
            if not user32.EnumDisplayDevicesW(None, i, ctypes.byref(dd), 0):
                break
            for field in (dd.DeviceString, dd.DeviceID, dd.DeviceName):
                try:
                    if field:
                        names.append(str(field).lower())
                except Exception:
                    continue
            i += 1
    except Exception:
        _VIRTUAL_GPU = False
        return _VIRTUAL_GPU

    hay = " ".join(names)
    keywords = [
        "vmware",
        "virtualbox",
        "vbox",
        "svga",
        "qxl",
        "virtio",
        "parallels",
        "hyper-v",
        "microsoft basic display",
        "basic display adapter",
        "remote display",
        "citrix",
        "xen",
        "bochs",
    ]
    _VIRTUAL_GPU = any(k in hay for k in keywords)
    return _VIRTUAL_GPU


def _get_wallpaper_path():
    try:
        SPI_GETDESKWALLPAPER = 0x0073
        path = ctypes.create_unicode_buffer(260)
        ctypes.windll.user32.SystemParametersInfoW(SPI_GETDESKWALLPAPER, 260, path, 0)
        p = path.value
        if p and os.path.exists(p):
            return p
    except Exception:
        return None
    return None


def _image_path_to_data_url(path):
    if not path or not os.path.exists(path):
        return None
    img = QImage(path)
    if img.isNull():
        return None
    max_side = max(img.width(), img.height())
    if max_side > 800:
        img = img.scaled(800, 800, Qt.KeepAspectRatio, Qt.SmoothTransformation)
    buffer = QBuffer()
    buffer.open(QIODevice.OpenModeFlag.WriteOnly)
    img.save(buffer, "PNG")
    data = bytes(buffer.data())
    if not data:
        return None
    encoded = base64.b64encode(data).decode("utf-8")
    return f"data:image/png;base64,{encoded}"


def _resolve_app_paths():
    if getattr(sys, "frozen", False):
        exe_path = sys.executable
        work_dir = os.path.dirname(exe_path)
        args = ""
        icon_path = exe_path
    else:
        exe_path = sys.executable
        root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        main_py = os.path.join(root_dir, "main.py")
        work_dir = root_dir
        args = f'"{main_py}"'
        icon_path = os.path.join(root_dir, "icons", "logo.ico")
    return exe_path, work_dir, args, icon_path


def _create_shortcut(
    target_path, shortcut_path, work_dir=None, icon_path=None, args=None
):
    try:
        import win32com.client

        shell = win32com.client.Dispatch("WScript.Shell")
        shortcut = shell.CreateShortCut(shortcut_path)
        shortcut.TargetPath = target_path
        if work_dir:
            shortcut.WorkingDirectory = work_dir
        if icon_path:
            shortcut.IconLocation = icon_path
        if args:
            shortcut.Arguments = args
        shortcut.Save()
        return True
    except Exception as e:
        print(f"Error creating shortcut: {e}", file=sys.stderr)
        return False


def _set_run_at_startup(enable):
    import winreg

    key = winreg.HKEY_CURRENT_USER
    sub_key = r"Software\Microsoft\Windows\CurrentVersion\Run"
    app_name = "Luminalium"

    exe_path, work_dir, args, icon_path = _resolve_app_paths()
    cmd = f'"{exe_path}" {args}' if args else f'"{exe_path}"'

    try:
        with winreg.OpenKey(key, sub_key, 0, winreg.KEY_ALL_ACCESS) as k:
            if enable:
                winreg.SetValueEx(k, app_name, 0, winreg.REG_SZ, cmd)
            else:
                try:
                    winreg.DeleteValue(k, app_name)
                except FileNotFoundError:
                    pass
    except Exception as e:
        print(f"Error setting startup: {e}", file=sys.stderr)


def _pin_to_start(enable):
    try:
        programs_path = os.path.join(
            os.environ["APPDATA"], r"Microsoft\Windows\Start Menu\Programs"
        )
        if not os.path.exists(programs_path):
            return
        shortcut_path = os.path.join(programs_path, "Luminalium.lnk")

        if enable:
            exe_path, work_dir, args, icon_path = _resolve_app_paths()
            _create_shortcut(exe_path, shortcut_path, work_dir, icon_path, args)
        else:
            if os.path.exists(shortcut_path):
                try:
                    os.remove(shortcut_path)
                except Exception:
                    pass
    except Exception as e:
        print(f"Error pinning to start: {e}", file=sys.stderr)


def _pin_to_taskbar(enable):
    # Best effort: Create a shortcut on Desktop if requested,
    # as Taskbar pinning is restricted.
    # But user specifically asked for Taskbar.
    # Let's try the verb method, if it works, great.
    try:
        import win32com.client

        shell = win32com.client.Dispatch("Shell.Application")

        exe_path, work_dir, args, icon_path = _resolve_app_paths()

        # We need a shortcut first to pin? Or pin the exe?
        # Usually we pin the exe or a shortcut.
        # Since we might have args (dev mode), we need to pin the shortcut.

        # Let's create a temporary shortcut
        temp_dir = os.path.join(os.environ["TEMP"], "Luminalium_Pin")
        if not os.path.exists(temp_dir):
            os.makedirs(temp_dir)
        shortcut_path = os.path.join(temp_dir, "Luminalium.lnk")
        _create_shortcut(exe_path, shortcut_path, work_dir, icon_path, args)

        folder = shell.Namespace(temp_dir)
        item = folder.ParseName("Luminalium.lnk")

        # Verbs are localized. This is the problem.
        # English: "Pin to Taskbar"
        # Chinese: "固定到任务栏"
        # We can try iterating verbs.

        verbs = item.Verbs()
        taskbar_verb = None
        for v in verbs:
            name = v.Name.replace("&", "").lower()
            if "taskbar" in name or "任务栏" in name or "タスクバー" in name:
                if (
                    enable
                    and ("pin" in name or "固定" in name or "ピン" in name)
                    and (
                        "unpin" not in name
                        and "取消" not in name
                        and "外す" not in name
                    )
                ):
                    taskbar_verb = v
                    break
                elif not enable and (
                    "unpin" in name or "取消" in name or "外す" in name
                ):
                    taskbar_verb = v
                    break

        if taskbar_verb:
            taskbar_verb.DoIt()

        # Cleanup
        # os.remove(shortcut_path) # Keep it? No, delete it.
        # But if we pin the shortcut, the shortcut file must exist?
        # Yes, if we pin a shortcut, the shortcut file must stay.
        # So we should create the shortcut in a permanent place if we want to pin it.
        # Maybe use the Start Menu shortcut?

        if enable:
            # Ensure start menu shortcut exists first
            _pin_to_start(True)
            programs_path = os.path.join(
                os.environ["APPDATA"], r"Microsoft\Windows\Start Menu\Programs"
            )
            shortcut_path = os.path.join(programs_path, "Luminalium.lnk")
            folder = shell.Namespace(programs_path)
            item = folder.ParseName("Luminalium.lnk")
            if item:
                verbs = item.Verbs()
                for v in verbs:
                    name = v.Name.replace("&", "").lower()
                    if "taskbar" in name or "任务栏" in name or "タスクバー" in name:
                        if ("pin" in name or "固定" in name or "ピン" in name) and (
                            "unpin" not in name
                            and "取消" not in name
                            and "外す" not in name
                        ):
                            v.DoIt()
                            break
    except Exception as e:
        print(f"Error pinning to taskbar: {e}", file=sys.stderr)


class Api(QObject):
    def __init__(self, window=None):
        super().__init__()
        self._window = window
        self._in_process = False
        self.settings = {}
        self.version = {}
        self.dialog_data = {}
        self._icon_cache = {}
        self._logs_window = None
        self._logs_api = None

    def set_window(self, window):
        self._window = window

    def set_in_process(self, enabled=True):
        self._in_process = bool(enabled)

    def _close_current_window(self):
        if self._window:
            try:
                self._window.close()
            except Exception:
                pass

    def _get_window_hwnd(self):
        if not self._window:
            return 0
        try:
            return int(self._window.winId())
        except Exception:
            return 0

    def _flash_window(self):
        hwnd = self._get_window_hwnd()
        if not hwnd:
            return
        try:
            from ctypes import wintypes

            class FLASHWINFO(ctypes.Structure):
                _fields_ = [
                    ("cbSize", wintypes.UINT),
                    ("hwnd", wintypes.HWND),
                    ("dwFlags", wintypes.DWORD),
                    ("uCount", wintypes.UINT),
                    ("dwTimeout", wintypes.DWORD),
                ]

            info = FLASHWINFO(
                ctypes.sizeof(FLASHWINFO),
                wintypes.HWND(hwnd),
                3,
                3,
                0,
            )
            ctypes.windll.user32.FlashWindowEx(ctypes.byref(info))
        except Exception:
            pass

    def _show_existing_window_toast(self, message):
        if not self._window:
            return
        toast_text = str(message or "已经存在打开的窗口！")
        js = f"""
(function() {{
    try {{
        const message = {json.dumps(toast_text, ensure_ascii=False)};
        const showExistingToast = () => {{
            const toast = document.getElementById("toast-message");
            const text = document.getElementById("toast-text");
            if (!toast || !text) {{
                return false;
            }}
            text.textContent = message;
            if (window.__luminaliumExistingWindowToastTimer) {{
                clearTimeout(window.__luminaliumExistingWindowToastTimer);
            }}
            toast.classList.add("show");
            window.__luminaliumExistingWindowToastTimer = window.setTimeout(() => {{
                toast.classList.remove("show");
            }}, 2200);
            return true;
        }};
        if (typeof window.showExistingWindowToast === "function") {{
            window.showExistingWindowToast(message);
        }} else if (!showExistingToast()) {{
            let style = document.getElementById("luminalium-existing-window-toast-style");
            if (!style) {{
                style = document.createElement("style");
                style.id = "luminalium-existing-window-toast-style";
                style.textContent = `
                    .luminalium-existing-window-toast {{
                        position: fixed;
                        left: 50%;
                        bottom: 60px;
                        transform: translateX(-50%) translateY(20px);
                        display: inline-flex;
                        align-items: center;
                        gap: 8px;
                        min-width: 220px;
                        max-width: min(calc(100vw - 32px), 420px);
                        padding: 10px 18px;
                        border-radius: 999px;
                        background: rgba(30, 30, 30, 0.88);
                        color: #FFFFFF;
                        font-size: 13px;
                        line-height: 1.4;
                        box-shadow: 0 12px 36px rgba(0, 0, 0, 0.22);
                        opacity: 0;
                        pointer-events: none;
                        transition: opacity 0.2s ease, transform 0.2s ease;
                        z-index: 2147483647;
                    }}
                    .luminalium-existing-window-toast.show {{
                        opacity: 1;
                        transform: translateX(-50%) translateY(0);
                    }}
                    .luminalium-existing-window-toast__icon {{
                        width: 10px;
                        height: 10px;
                        flex: 0 0 auto;
                        border-radius: 50%;
                        background: #3275F5;
                        box-shadow: 0 0 0 4px rgba(50, 117, 245, 0.18);
                    }}
                    .luminalium-existing-window-toast__text {{
                        white-space: nowrap;
                        overflow: hidden;
                        text-overflow: ellipsis;
                    }}
                    [data-theme="dark"] .luminalium-existing-window-toast {{
                        background: rgba(45, 45, 45, 0.92);
                        border: 0.5px solid rgba(255, 255, 255, 0.14);
                    }}
                `;
                (document.head || document.documentElement).appendChild(style);
            }}
            let toast = document.getElementById("luminalium-existing-window-toast");
            if (!toast) {{
                toast = document.createElement("div");
                toast.id = "luminalium-existing-window-toast";
                toast.className = "luminalium-existing-window-toast";
                toast.innerHTML = '<div class="luminalium-existing-window-toast__icon"></div><div class="luminalium-existing-window-toast__text"></div>';
                (document.body || document.documentElement).appendChild(toast);
            }}
            const text = toast.querySelector(".luminalium-existing-window-toast__text");
            if (text) {{
                text.textContent = message;
            }}
            if (window.__luminaliumExistingWindowToastTimer) {{
                clearTimeout(window.__luminaliumExistingWindowToastTimer);
            }}
            toast.classList.add("show");
            window.__luminaliumExistingWindowToastTimer = window.setTimeout(() => {{
                toast.classList.remove("show");
            }}, 2200);
        }}
        try {{
            window.dispatchEvent(new CustomEvent("luminalium:existing-window-toast", {{ detail: {{ message }} }}));
        }} catch (eventError) {{}}
    }} catch (e) {{}}
}})();
"""
        try:
            self._window.page().runJavaScript(js)
        except Exception:
            pass

    @Slot(QJsonValue)
    def update_settings(self, settings):
        if hasattr(settings, "toVariant"):
            settings = settings.toVariant()

        if not isinstance(settings, dict):
            try:
                # Handle possible other wrappers
                if hasattr(settings, "toPython"):
                    settings = settings.toPython()
            except Exception:
                try:
                    if isinstance(settings, str):
                        settings = json.loads(settings)
                except Exception:
                    settings = {}

        if not isinstance(settings, dict):
            settings = {}

        self.settings = settings
        theme_mode = settings.get("Appearance", {}).get("ThemeMode", "Light")
        theme_id = settings.get("Appearance", {}).get("ThemeId", "default")
        if self._window:
            if hasattr(self._window, "update_theme_mode"):
                self._window.update_theme_mode(theme_mode)
            js = f"if (typeof updateTheme === 'function') updateTheme({json.dumps(theme_mode)}, {json.dumps(theme_id)})"
            self._window.page().runJavaScript(js)
            try:
                backdrop_type = _resolve_system_backdrop_type(
                    getattr(self, "settings", {}), getattr(self._window, "_window_tag", "")
                )
                _apply_window_theme(
                    int(self._window.winId()),
                    _resolve_theme_dark(theme_mode),
                    backdrop_type,
                )
            except Exception:
                pass
            if hasattr(self._window, "apply_backdrop_settings"):
                self._window.apply_backdrop_settings()

    @Slot(int)
    def update_timer(self, total_seconds):
        print(f"TIMER_UPDATE:{total_seconds}")
        sys.stdout.flush()

    @Slot(str)
    def set_title(self, title):
        if self._window:
            self._window.setWindowTitle(str(title))

    @Slot(bool)
    def set_mini_mode(self, enabled):
        if self._window:
            self._window.set_mini_mode(enabled)

    @Slot()
    def start_window_drag(self):
        if self._window and hasattr(self._window, "start_window_drag"):
            self._window.start_window_drag()

    @Slot(bool)
    def set_fullscreen(self, enabled):
        if self._window:
            if enabled:
                self._window.showFullScreen()
            else:
                self._window.showNormal()
            try:
                self._window.raise_()
                self._window.activateWindow()
            except Exception:
                pass

    @Slot(bool)
    def set_maximized(self, enabled):
        if self._window:
            if enabled:
                self._window.showMaximized()
            else:
                self._window.showNormal()
            try:
                self._window.raise_()
                self._window.activateWindow()
            except Exception:
                pass

    @Slot(result="QVariant")
    def get_settings(self):
        import copy

        settings_dict = (
            copy.deepcopy(self.settings) if isinstance(self.settings, dict) else {}
        )
        if "Appearance" not in settings_dict:
            settings_dict["Appearance"] = {}

        theme_mode = settings_dict["Appearance"].get("ThemeMode", "Auto")
        settings_dict["Appearance"]["ResolvedIsDark"] = _resolve_theme_dark(theme_mode)

        return settings_dict

    @Slot(result="QVariant")
    def get_version(self):
        return self.version

    @Slot(result=str)
    def get_platform(self):
        return str(sys.platform or "")

    @Slot(result="QVariant")
    def get_overlay_themes(self):
        return _list_user_themes()

    @Slot(str, result=str)
    def get_toolbar_icon(self, icon_name):
        if not icon_name:
            return ""
        icon_map = {
            "select": "Mouse.svg",
            "pen": "Pen.svg",
            "eraser": "Eraser.svg",
            "clear": "Clear.svg",
            "spotlight": "spotlight.svg",
            "board_in_board": "board-in-board.svg",
            "timer": "timer.svg",
            "exit": "Minimize.svg",
        }
        key = str(icon_name).lower()
        icon_file = icon_map.get(key)
        if not icon_file:
            return ""
        root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        icon_path = os.path.join(root_dir, "icons", icon_file)
        if os.path.exists(icon_path):
            return "file:///" + icon_path.replace("\\", "/")
        return ""

    @Slot(result="QVariant")
    def get_system_fonts(self):
        try:
            import winreg

            keys = [
                (
                    winreg.HKEY_LOCAL_MACHINE,
                    r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",
                ),
                (
                    winreg.HKEY_CURRENT_USER,
                    r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",
                ),
            ]
            fonts = set()
            suffixes = (" (TrueType)", " (OpenType)", " (Type 1)", " (All res)")
            for root, path in keys:
                try:
                    with winreg.OpenKey(root, path) as k:
                        try:
                            count = winreg.QueryInfoKey(k)[1]
                        except Exception:
                            count = 0
                        for i in range(count):
                            try:
                                name, _, _ = winreg.EnumValue(k, i)
                            except Exception:
                                continue
                            if not isinstance(name, str):
                                continue
                            display = name.strip()
                            for suf in suffixes:
                                if display.endswith(suf):
                                    display = display[: -len(suf)].strip()
                                    break
                            if display:
                                fonts.add(display)
                except OSError:
                    continue
            return sorted(fonts, key=lambda s: s.lower())
        except Exception:
            return []

    @Slot(str, result=str)
    def get_font_preview_sample(self, lang):
        language = str(lang or "").strip()
        if language in ("zh-CN", "zh-TW", "yue-HK"):
            file_name = "sample.txt"
        elif language == "ja-JP":
            file_name = "sample_jp.txt"
        else:
            file_name = "sample_en.txt"

        settings_dir = os.path.join(
            os.path.dirname(os.path.abspath(__file__)),
            "builtins",
            "settings",
        )
        sample_path = os.path.join(settings_dir, file_name)

        for encoding in ("utf-8-sig", "utf-8"):
            try:
                with open(sample_path, "r", encoding=encoding) as f:
                    return f.read()
            except Exception:
                continue
        return ""

    def _get_settings_path(self):
        settings_path = os.environ.get("SETTINGS_PATH")
        if not settings_path:
            base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            settings_path = os.path.join(base_dir, "settings.json")
        return settings_path

    def _get_settings_reset_marker_path(self):
        settings_path = self._get_settings_path()
        return os.path.join(os.path.dirname(settings_path), "settings.reset")

    def _attach_quick_launch_icons(self, apps):
        if not isinstance(apps, list):
            return apps
        for app in apps:
            if not isinstance(app, dict):
                continue
            path = app.get("path")
            if not path:
                continue
            icon_val = app.get("icon")
            if icon_val:
                continue
            if path in self._icon_cache:
                icon_data = self._icon_cache[path]
            else:
                icon_data = get_file_icon_base64(path)
                if icon_data:
                    self._icon_cache[path] = icon_data
            if icon_data:
                app["icon"] = icon_data
        return apps

    @Slot(result="QVariant")
    def get_quick_launch_apps(self):
        settings_path = self._get_settings_path()
        data = {}
        try:
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            else:
                data = self.settings or {}
        except Exception:
            data = self.settings or {}
        toolbar = data.get("Toolbar") or {}
        apps = toolbar.get("QuickLaunchApps") or []
        if not isinstance(apps, list):
            apps = []
        apps = self._attach_quick_launch_icons(apps)
        self.settings = data
        return apps

    @Slot(result="QVariant")
    def add_quick_launch_app(self):
        file_path = None
        if self._window:
            file_path, _ = QFileDialog.getOpenFileName(
                self._window, "Select Application", "", get_quick_launch_dialog_filter()
            )
        if not file_path:
            return self.get_quick_launch_apps()
        name = os.path.splitext(os.path.basename(file_path))[0]
        settings_path = self._get_settings_path()
        data = {}
        try:
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            toolbar = data.get("Toolbar") or {}
            apps = toolbar.get("QuickLaunchApps") or []
            if not isinstance(apps, list):
                apps = []
            if any(app.get("path") == file_path for app in apps):
                self.settings = data
                return apps
            apps.append({"name": name, "path": file_path, "icon": ""})
            toolbar["QuickLaunchApps"] = apps
            data["Toolbar"] = toolbar
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            self.settings = data
            return self.get_quick_launch_apps()
        except Exception:
            return self.get_quick_launch_apps()

    @Slot(str, str, result="QVariant")
    def rename_quick_launch_app(self, path, new_name):
        if not path or not new_name:
            return self.get_quick_launch_apps()
        settings_path = self._get_settings_path()
        data = {}
        try:
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            toolbar = data.get("Toolbar") or {}
            apps = toolbar.get("QuickLaunchApps") or []
            if not isinstance(apps, list):
                apps = []
            for app in apps:
                if app.get("path") == path:
                    app["name"] = new_name
                    break
            toolbar["QuickLaunchApps"] = apps
            data["Toolbar"] = toolbar
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            self.settings = data
            return self.get_quick_launch_apps()
        except Exception:
            return self.get_quick_launch_apps()

    @Slot(str, result="QVariant")
    def remove_quick_launch_app(self, path):
        settings_path = self._get_settings_path()
        data = {}
        try:
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            toolbar = data.get("Toolbar") or {}
            apps = toolbar.get("QuickLaunchApps") or []
            if not isinstance(apps, list):
                apps = []
            apps = [app for app in apps if app.get("path") != path]
            toolbar["QuickLaunchApps"] = apps
            data["Toolbar"] = toolbar
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            self.settings = data
            return self.get_quick_launch_apps()
        except Exception:
            return self.get_quick_launch_apps()

    @Slot(str, result="QVariant")
    def get_taskbar_preview(self, screen_name):
        screens = QGuiApplication.screens()
        target_screen = QGuiApplication.primaryScreen()

        if screen_name and screen_name != "Auto" and screen_name != "Primary":
            try:
                parts = screen_name.split(" ")
                if len(parts) >= 2 and parts[1].isdigit():
                    idx = int(parts[1]) - 1
                    if 0 <= idx < len(screens):
                        target_screen = screens[idx]
            except Exception:
                pass

        if not target_screen:
            return {"is_visible": False}

        geo = target_screen.geometry()
        avail = target_screen.availableGeometry()

        tb_rect = None
        position = "bottom"
        size_percent = 0.0

        if avail.height() < geo.height():
            diff = geo.height() - avail.height()
            size_percent = diff / geo.height()
            if avail.y() > geo.y():
                position = "top"
                tb_rect = [geo.x(), geo.y(), geo.width(), diff]
            else:
                position = "bottom"
                tb_rect = [geo.x(), geo.height() - diff + geo.y(), geo.width(), diff]
        elif avail.width() < geo.width():
            diff = geo.width() - avail.width()
            size_percent = diff / geo.width()
            if avail.x() > geo.x():
                position = "left"
                tb_rect = [geo.x(), geo.y(), diff, geo.height()]
            else:
                position = "right"
                tb_rect = [geo.width() - diff + geo.x(), geo.y(), diff, geo.height()]
        else:
            return {"is_visible": False}

        try:
            if not tb_rect or tb_rect[2] <= 0 or tb_rect[3] <= 0:
                return {"is_visible": False}
            pixmap = target_screen.grabWindow(
                0, tb_rect[0], tb_rect[1], tb_rect[2], tb_rect[3]
            )
            if pixmap.isNull():
                return {"is_visible": False}

            byte_array = QByteArray()
            buffer = QBuffer(byte_array)
            buffer.open(QIODevice.WriteOnly)
            if not pixmap.save(buffer, "PNG"):
                return {"is_visible": False}
            base64_data = byte_array.toBase64().data().decode()
            data_url = f"data:image/png;base64,{base64_data}"

            return {
                "image": data_url,
                "position": position,
                "size_percent": size_percent,
                "is_visible": True,
            }
        except Exception as e:
            print(f"Taskbar capture error: {e}")
            return {"is_visible": False}

    @Slot(result=str)
    def get_wallpaper_path(self):
        path = _get_wallpaper_path()
        if path:
            return _image_path_to_data_url(path)
        return ""

    @Slot(str, str, QJsonValue)
    def save_setting(self, category, key, value):
        preview_mode = os.environ.get("ONBOARDING_PREVIEW", "").lower() == "true"
        if preview_mode:
            return
        if not isinstance(category, str) or not isinstance(key, str):
            return
        try:
            if hasattr(value, "toVariant"):
                value = value.toVariant()
            elif hasattr(value, "toPython"):
                value = value.toPython()
        except Exception:
            pass
        settings_path = os.environ.get("SETTINGS_PATH")
        if not settings_path:
            if getattr(sys, "frozen", False):
                settings_path = os.path.join(
                    os.path.dirname(sys.executable), "settings.json"
                )
            else:
                base_dir = os.path.dirname(os.path.abspath(__file__))
                settings_path = os.path.join(os.path.dirname(base_dir), "settings.json")
        try:
            data = {}
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            if category not in data:
                data[category] = {}
            data[category][key] = value
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            self.settings = data
            # Hook for system integration settings
            if category == "General":
                if key == "RunAtStartup":
                    platform_set_run_at_startup(bool(value))
                elif key == "PinToTaskbar":
                    _pin_to_taskbar(bool(value))
                elif key == "PinToStart":
                    _pin_to_start(bool(value))

            if category == "Appearance" and key in ("ThemeMode", "ThemeId"):
                self.update_settings(data)
            if category == "General" and key in (
                "SystemBackdropEnabled",
                "SystemBackdropType",
            ):
                if self._window and hasattr(self._window, "apply_backdrop_settings"):
                    self._window.apply_backdrop_settings()
        except Exception as e:
            print(f"Error saving settings: {e}", file=sys.stderr)

    @Slot(result=str)
    def import_settings(self):
        """Import settings from a user-selected JSON file"""
        import json as json_module

        try:
            file_path, _ = QFileDialog.getOpenFileName(
                self._window, "选择配置文件", "", "JSON 配置文件 (*.json);;所有文件 (*)"
            )

            if not file_path:
                print("No file selected", file=sys.stderr)
                return None

            print(f"Importing from: {file_path}", file=sys.stderr)

            # Load the config file
            with open(file_path, "r", encoding="utf-8") as f:
                config_data = json.load(f)

            print(f"Config data loaded: {list(config_data.keys())}", file=sys.stderr)

            if not isinstance(config_data, dict):
                print("Config is not a dict", file=sys.stderr)
                return None

            # Extract relevant config sections
            imported_config = {}

            # Map from file config structure to state.config structure
            general = config_data.get("General", {})
            appearance = config_data.get("Appearance", {})
            overlay = config_data.get("Overlay", {})

            if "Language" in general:
                imported_config["language"] = general["Language"]
            if "ThemeMode" in appearance:
                imported_config["theme"] = appearance["ThemeMode"]
            if "ThemeId" in appearance:
                imported_config["themeId"] = appearance["ThemeId"]
            if "RunAtStartup" in general:
                imported_config["runAtStartup"] = general["RunAtStartup"]
            if "DisableAnimations" in general:
                imported_config["disableAnimations"] = general["DisableAnimations"]
            if "CrashAutoHandleEnabled" in general:
                imported_config["crashAutoHandleEnabled"] = general[
                    "CrashAutoHandleEnabled"
                ]
            if "CrashAutoHandleMode" in general:
                imported_config["crashAutoHandleMode"] = general["CrashAutoHandleMode"]
            if "AutoShowOverlay" in general:
                imported_config["autoShowOverlay"] = general["AutoShowOverlay"]
            if "Scale" in overlay:
                imported_config["scale"] = overlay["Scale"]
            if "PopWindowScale" in overlay:
                imported_config["popWindowScale"] = overlay["PopWindowScale"]
            if "SafeArea" in overlay:
                imported_config["safeArea"] = overlay["SafeArea"]
            if "ShowStatusBar" in overlay:
                imported_config["showStatusBar"] = overlay["ShowStatusBar"]

            # Generate preview text
            preview_lines = []
            for key, value in imported_config.items():
                preview_lines.append(f"• {key}: {value}")
            preview = (
                "\n".join(preview_lines) if preview_lines else "未检测到支持的配置项"
            )

            # Return as JSON string for reliable serialization
            result = {"config": imported_config, "preview": preview}

            result_json = json_module.dumps(result, ensure_ascii=False)
            print(f"Returning JSON string, length: {len(result_json)}", file=sys.stderr)
            print(
                f"Config keys={list(imported_config.keys())}, preview={len(preview)} chars",
                file=sys.stderr,
            )

            return result_json
        except Exception as e:
            print(f"Error importing settings: {e}", file=sys.stderr)
            import traceback

            traceback.print_exc(file=sys.stderr)
            return None

    @Slot()
    def restart_app(self):
        settings_path = self._get_settings_path()
        try:
            data = {}
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            data["_restart_pending"] = True
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
        except Exception as e:
            print(f"Error triggering restart: {e}", file=sys.stderr)

    @Slot()
    def restart_and_open_settings(self):
        settings_path = self._get_settings_path()
        try:
            data = {}
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            data["_restart_pending"] = True
            data["_open_settings_pending"] = True
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
        except Exception as e:
            print(f"Error triggering restart with settings open: {e}", file=sys.stderr)

    @Slot()
    def quit_app(self):
        settings_path = self._get_settings_path()
        try:
            data = {}
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            data["_quit_pending"] = True
            with open(settings_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
        except Exception as e:
            print(f"Error triggering quit: {e}", file=sys.stderr)

    @Slot()
    def restart_from_crash_dialog(self):
        try:
            root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            main_path = os.path.join(root_dir, "main.py")
            if getattr(sys, "frozen", False):
                cmd = [sys.executable, "--silent"]
            else:
                cmd = [sys.executable, main_path, "--silent"]
            creationflags = (
                0x08000000 | 0x00000008
            )  # CREATE_NO_WINDOW | DETACHED_PROCESS
            env = os.environ.copy()
            env["LUMINALIUM_RESTART"] = "1"
            env["LUMINALIUM_RESTART_PID"] = str(os.getpid())
            subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
        except Exception as e:
            print(f"Failed to restart from crash dialog: {e}", file=sys.stderr)

    @Slot()
    def reset_to_pre_onboarding_state(self):
        settings_path = self._get_settings_path()
        reset_marker = self._get_settings_reset_marker_path()
        try:
            if os.path.exists(settings_path):
                os.remove(settings_path)
        except Exception as e:
            print(f"Error deleting settings: {e}", file=sys.stderr)
        try:
            with open(reset_marker, "w", encoding="utf-8") as f:
                f.write("reset")
        except Exception as e:
            print(f"Error writing reset marker: {e}", file=sys.stderr)

    @Slot()
    def show_window(self):
        if self._window:
            if self._window.isMinimized():
                self._window.showNormal()
            self._window.show()
            self._window.raise_()
            self._window.activateWindow()
            self._flash_window()

    @Slot(str)
    def notify_existing_window(self, message):
        self.show_window()
        self._show_existing_window_toast = lambda x: None

    @Slot(str)
    def open_browser(self, url):
        import webbrowser

        webbrowser.open(url)

    @Slot()
    def trigger_crash(self):
        raise RuntimeError("这是一个手动触发的测试崩溃。")

    @Slot()
    def create_dialog(self):
        msg = "傳說中你為愛甘心被擱淺\n我也可以為你潛入海裡面\n怎麼忍心斷絕 忘記我不變的誓言?\n我眼淚斷了線\n現實裡有了我對你的眷戀\n我願意化作雕像 等你出現\n再見 再也不見 心碎了飄蕩在海邊\n你抬頭就看見"
        theme_mode = self.settings.get("Appearance", {}).get("ThemeMode", "Light")
        theme_lower = str(theme_mode).lower()
        if theme_lower == "dark":
            accent = "#E1EBFF"
        elif theme_lower == "auto":
            accent = "#3275F5"
        else:
            accent = "#3275F5"
        dialog_data = {
            "code": "test_dialog",
            "title": "",
            "text": msg,
            "confirmText": "",
            "cancelText": "",
            "theme": theme_lower,
            "accentColor": accent,
        }
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".json", delete=False, encoding="utf-8"
        ) as f:
            json.dump(dialog_data, f)
            temp_path = f.name
        subprocess.Popen(
            [sys.executable, __file__, "--dialog", temp_path], creationflags=0x08000000
        )

    @Slot(str, str)
    def show_font_warning(self, font_name=None, font_lang=None):
        theme_mode = self.settings.get("Appearance", {}).get("ThemeMode", "Light")
        theme_lower = str(theme_mode).lower()
        if theme_lower == "dark":
            accent = "#E1EBFF"
        else:
            accent = "#3275F5"
        dialog_data = {
            "code": "font_warning",
            "title": "",
            "text": "",
            "confirmText": "",
            "hideCancel": True,
            "theme": theme_lower,
            "accentColor": accent,
            "targetLang": font_lang,
        }
        temp_settings = json.loads(json.dumps(self.settings))
        if font_name:
            if "Fonts" not in temp_settings:
                temp_settings["Fonts"] = {}
            if "Profiles" not in temp_settings["Fonts"]:
                temp_settings["Fonts"]["Profiles"] = {}
            lang = font_lang or temp_settings.get("General", {}).get(
                "Language", "zh-CN"
            )
            if lang not in temp_settings["Fonts"]["Profiles"]:
                temp_settings["Fonts"]["Profiles"][lang] = {}
            temp_settings["Fonts"]["Profiles"][lang]["web"] = font_name
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".json", delete=False, encoding="utf-8"
        ) as f:
            json.dump(dialog_data, f)
            temp_path = f.name
        if font_name:
            dialog_data["overrideSettings"] = temp_settings
            with open(temp_path, "w", encoding="utf-8") as f:
                json.dump(dialog_data, f)
        subprocess.Popen(
            [sys.executable, __file__, "--dialog", temp_path], creationflags=0x08000000
        )

    @Slot(result="QVariant")
    def get_dialog_data(self):
        return self.dialog_data

    @Slot()
    def on_confirm(self):
        print("DIALOG_CONFIRMED")
        sys.stdout.flush()
        self._close_current_window()
        if not self._in_process:
            sys.exit(0)

    @Slot(str)
    def on_confirm_with_value(self, value):
        try:
            payload = json.dumps(value, ensure_ascii=False)
        except Exception:
            payload = json.dumps(str(value), ensure_ascii=False)
        print(f"DIALOG_VALUE:{payload}")
        print("DIALOG_CONFIRMED")
        sys.stdout.flush()
        self._close_current_window()
        if not self._in_process:
            sys.exit(0)

    @Slot()
    def on_cancel(self):
        print("DIALOG_CANCELLED")
        sys.stdout.flush()
        self._close_current_window()
        if not self._in_process:
            sys.exit(0)

    @Slot(str, result=bool)
    def copy_text_to_clipboard(self, text):
        try:
            clipboard = QGuiApplication.clipboard()
            clipboard.setText(str(text or ""))
            print("CRASH_LOG_COPIED")
            sys.stdout.flush()
            return True
        except Exception:
            return False

    @Slot()
    def ignore_crash_dialog(self):
        print("CRASH_DIALOG_IGNORED")
        sys.stdout.flush()
        self._close_current_window()
        if not self._in_process:
            sys.exit(0)

    @Slot()
    def exit_from_crash_dialog(self):
        try:
            parent_pid = int((self.dialog_data or {}).get("parentPid") or 0)
        except Exception:
            parent_pid = 0
        if parent_pid > 0 and parent_pid != os.getpid():
            try:
                subprocess.run(
                    ["taskkill", "/PID", str(parent_pid), "/T", "/F"],
                    check=False,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    creationflags=0x08000000,
                )
            except Exception:
                pass
        print("CRASH_DIALOG_EXIT")
        sys.stdout.flush()
        self._close_current_window()
        if not self._in_process:
            sys.exit(0)

    @Slot()
    def open_onboarding_preview(self):
        base_dir = os.path.dirname(os.path.abspath(__file__))
        onboarding_html = os.path.join(
            base_dir, "builtins", "onboarding", "onboarding.html"
        )
        root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        main_path = os.path.join(root_dir, "main.py")
        env = os.environ.copy()
        sp = env.get("SETTINGS_PATH")
        if not sp:
            env["SETTINGS_PATH"] = os.path.join(root_dir, "settings.json")
        env["ONBOARDING_PREVIEW"] = "true"
        width = "960"
        height = "640"
        if getattr(sys, "frozen", False):
            subprocess.Popen(
                [
                    sys.executable,
                    "--webview-runner",
                    onboarding_html,
                    "Onboarding Preview",
                    width,
                    height,
                    "true",
                ],
                env=env,
                creationflags=0x08000000,
            )
        else:
            subprocess.Popen(
                [
                    sys.executable,
                    main_path,
                    "--webview-runner",
                    onboarding_html,
                    "Onboarding Preview",
                    width,
                    height,
                    "true",
                ],
                env=env,
                creationflags=0x08000000,
            )

    @Slot()
    def open_logs_window(self):
        try:
            if self._logs_window is not None:
                try:
                    if self._logs_window.isMinimized():
                        self._logs_window.showNormal()
                    self._logs_window.show()
                    self._logs_window.raise_()
                    self._logs_window.activateWindow()
                    self._show_existing_window_toast("已经存在打开的窗口！")
                    return
                except RuntimeError:
                    self._logs_window = None
                    self._logs_api = None
            base_dir = os.path.dirname(os.path.abspath(__file__))
            logs_html = os.path.join(base_dir, "builtins", "logs", "logs.html")
            api = Api()
            api.set_in_process(True)
            api.settings = self.settings
            api.version = self.version
            theme_mode = (
                (self.settings or {}).get("Appearance", {}).get("ThemeMode", "Auto")
            )
            defer_load = _should_defer_initial_load(logs_html, "Logs", True)
            window = MainWindow(
                "Logs", logs_html, api, 1000, 700, theme_mode, True, defer_load
            )
            window.setMinimumWidth(800)

            def _clear_logs_window(*_):
                self._logs_window = None
                self._logs_api = None

            window.destroyed.connect(_clear_logs_window)
            self._logs_api = api
            self._logs_window = window
            window.show()
            window.raise_()
            window.activateWindow()
        except Exception as e:
            print(f"Error opening logs window: {e}", file=sys.stderr)

    @Slot()
    def open_license(self):
        root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        license_path = os.path.join(root_dir, "LICENSE")
        if os.path.exists(license_path):
            try:
                import webbrowser

                webbrowser.open("file:///" + license_path.replace("\\", "/"))
            except Exception:
                pass

    @Slot(result=str)
    def get_assets_path(self):
        return os.environ.get("ASSETS_PATH", "")

    @Slot(str, "QVariant", result="QVariant")
    def get_logs(self, search_text="", levels=None):
        """获取应用日志"""
        try:
            from ppt_assistant.core.log_manager import get_log_manager

            manager = get_log_manager()

            if levels is None:
                levels = ["debug", "info", "warn", "error"]
            elif isinstance(levels, str):
                levels = [levels]

            return manager.get_logs(levels=levels, search_text=search_text)
        except Exception as e:
            print(f"Error getting logs: {e}", file=sys.stderr)
            return []

    @Slot(result="QVariant")
    def get_log_stats(self):
        """获取日志统计"""
        try:
            from ppt_assistant.core.log_manager import get_log_manager

            manager = get_log_manager()
            return manager.get_stats()
        except Exception as e:
            print(f"Error getting log stats: {e}", file=sys.stderr)
            return {"debug": 0, "info": 0, "warn": 0, "error": 0}

    @Slot(result="QVariant")
    def get_log_filters(self):
        """获取日志级别过滤设置"""
        try:
            from ppt_assistant.core.log_manager import get_log_manager

            manager = get_log_manager()
            return manager.get_filters()
        except Exception as e:
            print(f"Error getting log filters: {e}", file=sys.stderr)
            return {"debug": True, "info": True, "warn": True, "error": True}

    @Slot(str)
    def set_log_filters(self, filters_json):
        """设置日志级别过滤"""
        try:
            import json
            from ppt_assistant.core.log_manager import get_log_manager

            manager = get_log_manager()
            filters = json.loads(filters_json)
            manager.set_filters(filters)
        except Exception as e:
            print(f"Error setting log filters: {e}", file=sys.stderr)

    @Slot()
    def clear_logs(self):
        """清空日志"""
        try:
            from ppt_assistant.core.log_manager import get_log_manager

            manager = get_log_manager()
            manager.clear_logs()
        except Exception as e:
            print(f"Error clearing logs: {e}", file=sys.stderr)

    @Slot(result="QVariant")
    def get_system_info(self):
        """Get system information for the logs viewer"""
        import platform

        try:
            version_data = self._load_json_file(os.environ.get("VERSION_PATH", ""))
            version = version_data.get("version", "Unknown")
        except Exception:
            version = "Unknown"

        return {
            "app_version": version,
            "python_version": platform.python_version(),
            "platform": platform.system(),
            "platform_version": platform.release(),
            "processor": platform.processor() or "Unknown",
            "architecture": platform.machine(),
        }

    def _load_json_file(self, path):
        """Helper to load JSON files"""
        if not path or not os.path.exists(path):
            return {}
        try:
            with open(path, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            return {}

    @Slot(result="QVariant")
    def get_timer_state(self):
        return {
            "remaining": int(os.environ.get("TIMER_REMAINING", 0)),
            "total": int(os.environ.get("TIMER_TOTAL", 0)),
            "is_running": os.environ.get("TIMER_IS_RUNNING", "false") == "true",
        }

    @Slot(int)
    def start_timer(self, seconds):
        print(f"TIMER_START:{seconds}")
        sys.stdout.flush()

    @Slot()
    def pause_timer(self):
        print("TIMER_PAUSE")
        sys.stdout.flush()

    @Slot()
    def resume_timer(self):
        print("TIMER_RESUME")
        sys.stdout.flush()

    @Slot()
    def stop_timer(self):
        print("TIMER_STOP")
        sys.stdout.flush()

    @Slot()
    def finish_timer(self):
        print("TIMER_FINISH")
        sys.stdout.flush()

    @Slot(int)
    def add_time(self, seconds):
        print(f"TIMER_ADD_TIME:{seconds}")
        sys.stdout.flush()

    @Slot(QJsonValue)
    def select_item(self, item):
        if hasattr(item, "toVariant"):
            item = item.toVariant()
        print(f"SELECTED_ITEM:{json.dumps(item, ensure_ascii=False)}")
        sys.stdout.flush()
        if self._window:
            self._window.close()
        sys.exit(0)

    @Slot(result="QVariant")
    def get_monet_colors(self):
        try:
            path = _get_wallpaper_path()
            if not path:
                return {}
            image_data = _image_path_to_data_url(path)
            if not image_data:
                return {"path": path}
            return {"path": path, "image": image_data}
        except Exception as e:
            print(f"Monet error: {e}")
            return {}

    @Slot(result="QVariant")
    def get_screen_list(self):
        screens = []
        try:
            qt_screens = QGuiApplication.screens() or []
            primary = QGuiApplication.primaryScreen()
            for i, s in enumerate(qt_screens):
                key = (s.name() or "").replace("\x00", "").strip()
                name = ""
                try:
                    manufacturer = (s.manufacturer() or "").replace("\x00", "").strip()
                    model = (s.model() or "").replace("\x00", "").strip()
                    if manufacturer or model:
                        name = f"{manufacturer} {model}".strip()
                    else:
                        name = (s.name() or "").replace("\x00", "").strip()
                except Exception:
                    name = (s.name() or "").replace("\x00", "").strip()
                if not name:
                    name = f"Display {i + 1}"
                screens.append(
                    {
                        "id": i,
                        "key": key,
                        "name": name,
                        "is_primary": (primary is not None and s == primary),
                    }
                )
        except Exception as e:
            print(f"Error getting screens: {e}", file=sys.stderr)
        return screens


_QWEBCHANNEL_JS_CACHE = None


def _get_qwebchannel_js():
    global _QWEBCHANNEL_JS_CACHE
    if _QWEBCHANNEL_JS_CACHE is not None:
        return _QWEBCHANNEL_JS_CACHE

    js = ""
    f = QFile(":/qtwebchannel/qwebchannel.js")
    if f.open(QIODevice.OpenModeFlag.ReadOnly):
        try:
            # PySide6: readAll returns QByteArray, convert to bytes then string
            js = f.readAll().data().decode("utf-8")
        except Exception:
            try:
                # Fallback for older versions
                js = str(f.readAll(), "utf-8")
            except Exception:
                pass
        f.close()

    shim_js = """
    new QWebChannel(qt.webChannelTransport, function(channel) {
        window.pywebview = {
            api: new Proxy(channel.objects.api, {
                get: function(target, prop) {
                    return function(...args) {
                        return new Promise((resolve, reject) => {
                            target[prop](...args, function(result) {
                                resolve(result);
                            });
                        });
                    }
                }
            })
        };
        window.dispatchEvent(new Event('pywebviewready'));
    });
    """
    _QWEBCHANNEL_JS_CACHE = js + shim_js
    return _QWEBCHANNEL_JS_CACHE


class MainWindow(QWebEngineView):
    def __init__(
        self,
        title,
        url,
        api,
        width,
        height,
        theme_mode="auto",
        custom_border=False,
        defer_load=False,
    ):
        super().__init__()
        self.setPage(QWebEnginePage(_get_shared_profile(), self))
        self.setWindowTitle(title)
        self.resize(width, height)

        # 确保窗口可调整大小 - 设置基础窗口标志
        self.setWindowFlag(Qt.Window, True)
        self.setWindowFlag(Qt.WindowCloseButtonHint, True)
        self.setWindowFlag(Qt.WindowMinMaxButtonsHint, True)

        try:
            if "Onboarding" in str(title):
                self.setWindowFlag(Qt.WindowMaximizeButtonHint, False)
            self._center_on_screen()
        except Exception:
            self._center_on_screen()
        self._theme_mode = theme_mode
        self._custom_border = custom_border
        self._defer_load = defer_load
        self._mini_mode = False
        self._pre_mini_geometry = None
        self._pre_mini_was_maximized = False
        self._pre_mini_was_fullscreen = False
        self._pending_url = None
        self._pending_load_timer = None
        self._did_hard_refresh = False
        self._window_tag = self._detect_window_tag(url, title)
        self._loading_overlay = None
        self._loading_overlay_hide_timer = None
        # Disable loading overlay for onboarding to avoid QML/OpenGL issues
        self._loading_overlay_enabled = self._window_tag in ("settings", "timer")
        self._render_crash_count = 0
        self._max_reload_attempts = 3
        self._crash_recovery_timer = None
        self._disable_animations = _animations_disabled(getattr(api, "settings", {}))
        self._apply_page_background()
        settings = self.page().settings()
        allow_gpu = (not _is_windows7()) and (not _is_virtual_gpu())
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.Accelerated2dCanvasEnabled, allow_gpu
        )
        settings.setAttribute(QWebEngineSettings.WebAttribute.WebGLEnabled, allow_gpu)
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.LocalContentCanAccessRemoteUrls, True
        )
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.LocalContentCanAccessFileUrls, True
        )
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.ScrollAnimatorEnabled,
            not self._disable_animations,
        )
        settings.setAttribute(QWebEngineSettings.WebAttribute.AutoLoadImages, True)
        settings.setAttribute(QWebEngineSettings.WebAttribute.PluginsEnabled, False)
        _configure_profile(self.page().profile())
        self.api = api
        self.api.set_window(self)
        self._apply_page_background()
        self.channel = QWebChannel()
        self.channel.registerObject("api", api)
        self.page().setWebChannel(self.channel)

        script = QWebEngineScript()
        script.setSourceCode(_get_qwebchannel_js())
        script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
        script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(script)
        import copy

        settings_dict = (
            copy.deepcopy(api.settings) if isinstance(api.settings, dict) else {}
        )
        if "Appearance" not in settings_dict:
            settings_dict["Appearance"] = {}
        settings_dict["Appearance"]["ResolvedIsDark"] = _resolve_theme_dark(theme_mode)

        settings_json = json.dumps(settings_dict, ensure_ascii=False)
        settings_script = QWebEngineScript()
        settings_script.setSourceCode(f"window.initialSettings = {settings_json};")
        settings_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        settings_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(settings_script)
        motion_script = QWebEngineScript()
        motion_script.setSourceCode(
            "try {"
            f"window.__LUMINALIUM_DISABLE_ANIMATIONS = {json.dumps(self._disable_animations)};"
            f"document.documentElement.setAttribute('data-disable-animations', '{'true' if self._disable_animations else 'false'}');"
            "} catch (e) {}"
        )
        motion_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        motion_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(motion_script)
        supports_backdrop = _supports_system_backdrop()
        support_script = QWebEngineScript()
        support_script.setSourceCode(
            f"window.__SYSTEM_BACKDROP_SUPPORTED = {json.dumps(supports_backdrop)};"
        )
        support_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        support_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(support_script)
        try:
            general = (
                api.settings.get("General", {})
                if isinstance(api.settings, dict)
                else {}
            )
        except Exception:
            general = {}
        enabled = bool(general.get("SystemBackdropEnabled")) and self._window_tag in (
            "settings",
            "timer",
        )
        mode = "off"
        if enabled:
            mode = "system" if supports_backdrop else "fallback"
        backdrop_attr_script = QWebEngineScript()
        backdrop_attr_script.setSourceCode(
            "try {"
            f"document.documentElement.setAttribute('data-system-backdrop', '{'true' if enabled else 'false'}');"
            f"document.documentElement.setAttribute('data-system-backdrop-mode', '{mode}');"
            "} catch (e) {}"
        )
        backdrop_attr_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        backdrop_attr_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(backdrop_attr_script)
        preview_flag = os.environ.get("ONBOARDING_PREVIEW", "").lower() == "true"
        preview_script = QWebEngineScript()
        preview_script.setSourceCode(
            f"window.__ONBOARDING_PREVIEW = {json.dumps(preview_flag)};"
        )
        preview_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        preview_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(preview_script)
        if self._custom_border:
            self._inject_custom_border()
        if self._loading_overlay_enabled:
            self._setup_loading_overlay(title)
        self.loadStarted.connect(self._on_load_started)
        self.loadFinished.connect(self._on_load_finished)
        target_url = QUrl.fromUserInput(url)
        if self._defer_load:
            self._pending_url = target_url
            # Fallback: if showEvent doesn't fire, still kick the initial load
            # to avoid a stuck onboarding window.
            self._pending_load_timer = QTimer(self)
            self._pending_load_timer.setSingleShot(True)
            self._pending_load_timer.timeout.connect(self._ensure_pending_load)
            self._pending_load_timer.start(200)
        else:
            self.load(target_url)
        self.loadFinished.connect(
            lambda *_: (self._apply_page_background(), self._schedule_backdrop_apply())
        )
        self._schedule_backdrop_apply()

        self.renderProcessTerminated.connect(self._on_render_process_terminated)

    def _setup_loading_overlay(self, title):
        qml_path = _resolve_window_loader_qml_path()
        if not qml_path:
            return
        overlay = QQuickView()
        overlay.setResizeMode(QQuickView.SizeRootObjectToView)
        overlay.setColor(Qt.transparent)
        overlay.setFlags(Qt.FramelessWindowHint | Qt.Tool | Qt.WindowDoesNotAcceptFocus)
        try:
            overlay.setSource(QUrl.fromLocalFile(qml_path))
        except Exception as exc:
            print(f"[WebView] Failed to create loading overlay: {exc}", file=sys.stderr)
            try:
                overlay.deleteLater()
            except Exception:
                pass
            return
        if overlay.status() == QQuickView.Error:
            try:
                errors = "; ".join(str(err) for err in overlay.errors())
                print(f"[WebView] Loading overlay QML error: {errors}", file=sys.stderr)
            except Exception:
                pass
        root = overlay.rootObject()
        if root is None:
            try:
                overlay.deleteLater()
            except Exception:
                pass
            return
        logo_path = _resolve_logo_svg_path()
        font_path = _resolve_misans_font_path()
        root.setProperty(
            "screenTitle",
            _resolve_window_loader_title(
                self._window_tag, title, getattr(self.api, "settings", {})
            ),
        )
        root.setProperty("brandText", "Luminalium")
        root.setProperty(
            "logoSource", QUrl.fromLocalFile(logo_path).toString() if logo_path else ""
        )
        root.setProperty(
            "fontSource", QUrl.fromLocalFile(font_path).toString() if font_path else ""
        )
        root.setProperty("darkMode", bool(_resolve_theme_dark(self._theme_mode)))
        root.setProperty("animationsEnabled", not self._disable_animations)
        root.setProperty("loading", True)
        self._loading_overlay = overlay
        self._loading_overlay_hide_timer = QTimer(self)
        self._loading_overlay_hide_timer.setSingleShot(True)
        self._loading_overlay_hide_timer.timeout.connect(
            self._hide_loading_overlay_window
        )
        self._sync_loading_overlay_geometry()
        overlay.show()
        self._raise_loading_overlay()

    def _ensure_pending_load(self):
        if self._pending_url is None:
            return
        try:
            self.load(self._pending_url)
        finally:
            self._pending_url = None

    def _sync_loading_overlay_geometry(self):
        if self._loading_overlay is None:
            return
        try:
            top_left = self.mapToGlobal(QPoint(0, 0))
            overlay = self._loading_overlay
            if self.windowHandle() is not None:
                try:
                    overlay.setTransientParent(self.windowHandle())
                except Exception:
                    pass
                try:
                    overlay.setScreen(self.windowHandle().screen())
                except Exception:
                    pass
            overlay.setPosition(top_left)
            overlay.resize(self.size())
            self._raise_loading_overlay()
        except Exception:
            pass

    def _raise_loading_overlay(self):
        if self._loading_overlay is None:
            return
        try:
            raise_method = getattr(self._loading_overlay, "raise_", None)
            if callable(raise_method):
                raise_method()
                return
        except Exception:
            pass
        try:
            raise_method = getattr(self._loading_overlay, "raise", None)
            if callable(raise_method):
                raise_method()
        except Exception:
            pass

    def _set_loading_overlay_visible(self, loading):
        if self._loading_overlay is None:
            return
        if self._loading_overlay_hide_timer is not None:
            self._loading_overlay_hide_timer.stop()
        overlay = self._loading_overlay
        root = overlay.rootObject()
        if root is None:
            return
        if loading:
            self._sync_loading_overlay_geometry()
            overlay.show()
            self._raise_loading_overlay()
        root.setProperty("darkMode", bool(_resolve_theme_dark(self._theme_mode)))
        root.setProperty("loading", bool(loading))
        if not loading and self._loading_overlay_hide_timer is not None:
            self._loading_overlay_hide_timer.start(620)

    def _hide_loading_overlay_window(self):
        if self._loading_overlay is None:
            return
        try:
            self._loading_overlay.hide()
        except Exception:
            pass

    def _on_load_started(self):
        if self._loading_overlay_enabled:
            self._set_loading_overlay_visible(True)

    def _on_load_finished(self, _ok):
        self._render_crash_count = 0
        if not self._loading_overlay_enabled:
            return
        QTimer.singleShot(120, lambda: self._set_loading_overlay_visible(False))

    def _on_render_process_terminated(self, status, exit_code):
        status_name = ""
        try:
            status_name = str(getattr(status, "name", status))
        except Exception:
            status_name = str(status)
        print(
            f"[WebView] Render process terminated: status={status}, exit_code={exit_code}",
            file=sys.stderr,
        )
        if "NormalTerminationStatus" in status_name and int(exit_code or 0) == 0:
            print(
                "[WebView] Renderer ended normally; skipping crash recovery.",
                file=sys.stderr,
            )
            return
        self._render_crash_count += 1
        print(
            f"[WebView] Crash #{self._render_crash_count}/{self._max_reload_attempts}",
            file=sys.stderr,
        )

        # If too many crashes, disable GPU and retry once, then give up
        if self._render_crash_count > self._max_reload_attempts:
            print(
                f"[WebView] Too many crashes ({self._render_crash_count}). Giving up on recovery.",
                file=sys.stderr,
            )
            return

        # Use exponential backoff: 100ms, 500ms, 1500ms
        delay = min(100 * (2 ** (self._render_crash_count - 1)), 2000)
        print(f"[WebView] Scheduling reload in {delay}ms", file=sys.stderr)

        # If this is the second crash, try disabling GPU acceleration
        if self._render_crash_count == 2:
            try:
                print(
                    "[WebView] Disabling GPU acceleration due to repeated crashes",
                    file=sys.stderr,
                )
                settings = self.page().settings()
                settings.setAttribute(
                    QWebEngineSettings.WebAttribute.Accelerated2dCanvasEnabled, False
                )
                settings.setAttribute(
                    QWebEngineSettings.WebAttribute.WebGLEnabled, False
                )
            except Exception as e:
                print(f"[WebView] Error disabling GPU: {e}", file=sys.stderr)

        # Cancel any pending reload timer
        if self._crash_recovery_timer is not None:
            try:
                self._crash_recovery_timer.stop()
            except Exception:
                pass
            self._crash_recovery_timer = None

        # Schedule reload with delay
        self._crash_recovery_timer = QTimer(self)
        self._crash_recovery_timer.setSingleShot(True)
        self._crash_recovery_timer.timeout.connect(self.reload)
        self._crash_recovery_timer.start(delay)

    def _apply_page_background(self):
        if self._mini_mode:
            self.page().setBackgroundColor(Qt.transparent)
            return

        settings = {}
        api = getattr(self, "api", None)
        if api is not None:
            settings = getattr(api, "settings", {}) or {}
        backdrop_type = _resolve_system_backdrop_type(
            settings, getattr(self, "_window_tag", "")
        )
        if backdrop_type is not None and backdrop_type != DWMSBT_NONE:
            try:
                self.setAutoFillBackground(False)
                palette = self.palette()
                palette.setColor(self.backgroundRole(), Qt.transparent)
                self.setPalette(palette)
            except Exception:
                pass
            _safe_set_widget_attr(self, getattr(Qt, "WA_OpaquePaintEvent", None), False)
            _safe_set_widget_attr(self, Qt.WA_TranslucentBackground, True)
            _safe_set_widget_attr(self, getattr(Qt, "WA_NoSystemBackground", None), True)
            self.page().setBackgroundColor(Qt.transparent)
            return

        try:
            self.setAutoFillBackground(True)
            palette = self.palette()
            is_dark = _resolve_theme_dark(self._theme_mode)
            bg_color = QColor(24, 24, 24) if is_dark else QColor(255, 255, 255)
            palette.setColor(self.backgroundRole(), bg_color)
            self.setPalette(palette)
        except Exception:
            pass
        _safe_set_widget_attr(self, getattr(Qt, "WA_OpaquePaintEvent", None), True)
        _safe_set_widget_attr(self, Qt.WA_TranslucentBackground, False)
        _safe_set_widget_attr(self, getattr(Qt, "WA_NoSystemBackground", None), False)
        is_dark = _resolve_theme_dark(self._theme_mode)
        if is_dark:
            self.page().setBackgroundColor(QColor(24, 24, 24))
        else:
            self.page().setBackgroundColor(QColor(255, 255, 255))

    def _is_system_backdrop_enabled(self):
        settings = {}
        api = getattr(self, "api", None)
        if api is not None:
            settings = getattr(api, "settings", {}) or {}
        backdrop_type = _resolve_system_backdrop_type(
            settings, getattr(self, "_window_tag", "")
        )
        return backdrop_type is not None and backdrop_type != DWMSBT_NONE

    def _force_webview_transparent(self):
        try:
            self.setStyleSheet("background: transparent;")
        except Exception:
            pass
        try:
            self.page().setBackgroundColor(Qt.transparent)
        except Exception:
            pass
        try:
            self.page().runJavaScript(
                "try{"
                "if(document.documentElement){document.documentElement.style.background='transparent';}"
                "if(document.body){document.body.style.background='transparent';}"
                "}catch(e){}"
            )
        except Exception:
            pass

    def _force_webview_repaint(self):
        try:
            z = self.page().zoomFactor()
            self.page().setZoomFactor(z + 0.001)
            QTimer.singleShot(0, lambda: self.page().setZoomFactor(z))
        except Exception:
            pass

    def _inject_custom_border(self):
        css = """
html, body {
    height: 100%;
}
body {
    box-sizing: border-box;
    border: 1px solid var(--divider, rgba(0, 0, 0, 0.12));
    border-radius: 12px;
    overflow: hidden;
}
:root[data-system-backdrop="true"] body {
    border: none;
    border-radius: 0;
}
"""
        js = f"""
(function() {{
    const style = document.createElement('style');
    style.textContent = {json.dumps(css)};
    if (document.documentElement) {{
        document.documentElement.appendChild(style);
    }}
}})();
"""
        script = QWebEngineScript()
        script.setSourceCode(js)
        script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
        script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(script)

    def _detect_window_tag(self, url, title):
        try:
            url_text = str(url).lower()
        except Exception:
            url_text = ""
        title_text = str(title).lower() if title is not None else ""
        if "settings.html" in url_text or title_text == "settings":
            return "settings"
        if "timer.html" in url_text or "timer plugin" in title_text:
            return "timer"
        if "crash_dialog.html" in url_text or "crash" in title_text:
            return "crash"
        if "onboarding.html" in url_text or "onboarding" in title_text:
            return "onboarding"
        return ""

    def set_mini_mode(self, enabled):
        if self._mini_mode == enabled:
            return
        self._mini_mode = enabled
        if enabled:
            self._pre_mini_was_maximized = bool(self.isMaximized())
            self._pre_mini_was_fullscreen = bool(self.isFullScreen())
            try:
                if self._pre_mini_was_maximized or self._pre_mini_was_fullscreen:
                    self._pre_mini_geometry = self.normalGeometry()
                else:
                    self._pre_mini_geometry = self.geometry()
            except Exception:
                self._pre_mini_geometry = None
            self.setWindowFlags(
                Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool
            )
            self.setAttribute(Qt.WA_TranslucentBackground)
            self.resize(340, 400)

            # Move to top-right corner
            screen = QApplication.primaryScreen()
            if screen:
                geo = screen.availableGeometry()
                x = geo.x() + geo.width() - 340 - 20
                y = geo.y() + 20
                self.move(x, y)
        else:
            self.setWindowFlags(Qt.Window)
            self.setAttribute(Qt.WA_TranslucentBackground, False)
            restored = False
            geo = self._pre_mini_geometry
            if geo is not None:
                try:
                    if geo.width() > 0 and geo.height() > 0:
                        self.setGeometry(geo)
                        restored = True
                except Exception:
                    restored = False
            if self._pre_mini_was_maximized:
                self.showMaximized()
                restored = True
            elif self._pre_mini_was_fullscreen:
                self.showFullScreen()
                restored = True
            elif not restored:
                self.resize(800, 600)
                self._center_on_screen()
            self._pre_mini_geometry = None
            self._pre_mini_was_maximized = False
            self._pre_mini_was_fullscreen = False

        self._apply_page_background()
        self.show()

    def start_window_drag(self):
        # Reliable drag entrypoint for frameless mini windows.
        try:
            handle = self.windowHandle()
            if handle is not None and hasattr(handle, "startSystemMove"):
                if handle.startSystemMove():
                    return
        except Exception:
            pass
        try:
            hwnd = int(self.winId())
            user32 = ctypes.windll.user32
            user32.ReleaseCapture()
            user32.SendMessageW(hwnd, 0x00A1, 0x0002, 0)
        except Exception:
            pass

    def mouseMoveEvent(self, event):
        """Handle mouse move to display resize cursor at window edges."""
        if self.isMaximized() or self.isFullScreen():
            super().mouseMoveEvent(event)
            return

        pos = event.pos()
        edge_margin = 5  # pixels from edge to show resize cursor
        w = self.width()
        h = self.height()

        # Determine which edge(s) the cursor is near
        near_left = pos.x() < edge_margin
        near_right = pos.x() > w - edge_margin
        near_top = pos.y() < edge_margin
        near_bottom = pos.y() > h - edge_margin

        # Set cursor based on edge position
        if (near_top and near_left) or (near_bottom and near_right):
            self.setCursor(Qt.SizeFDiagCursor)
        elif (near_top and near_right) or (near_bottom and near_left):
            self.setCursor(Qt.SizeBDiagCursor)
        elif near_left or near_right:
            self.setCursor(Qt.SizeHorCursor)
        elif near_top or near_bottom:
            self.setCursor(Qt.SizeVerCursor)
        else:
            self.setCursor(Qt.ArrowCursor)

        super().mouseMoveEvent(event)

    def _center_on_screen(self):
        screen = QApplication.primaryScreen()
        if not screen:
            return
        geo = screen.availableGeometry()
        x = geo.x() + (geo.width() - self.width()) // 2
        y = geo.y() + (geo.height() - self.height()) // 2
        self.move(x, y)

    def _handle_existing_window_notification(self):
        api = getattr(self, "api", None)
        if api is not None and hasattr(api, "notify_existing_window"):
            api.notify_existing_window("已经存在打开的窗口！")
            return
        if self.isMinimized():
            self.showNormal()
        self.show()
        self.raise_()
        self.activateWindow()

    def nativeEvent(self, eventType, message):
        if _EXISTING_WINDOW_NOTIFY_MESSAGE:
            try:
                msg_ptr = int(message)
                if msg_ptr:
                    msg = ctypes.wintypes.MSG.from_address(msg_ptr)
                    if msg.message == _EXISTING_WINDOW_NOTIFY_MESSAGE:
                        QTimer.singleShot(0, self._handle_existing_window_notification)
                        return True, 0
            except Exception:
                pass
        return super().nativeEvent(eventType, message)

    def _apply_backdrop(self):
        self._apply_page_background()
        apply_win11_aesthetics(
            self,
            self._theme_mode,
            getattr(self.api, "settings", {}),
            getattr(self, "_window_tag", ""),
        )
        try:
            _force_dwm_redraw(int(self.winId()))
        except Exception:
            pass
        if self._is_system_backdrop_enabled():
            self._force_webview_transparent()
            self._force_webview_repaint()
        self.update()

    def _force_refresh(self):
        if self._mini_mode or self.isMaximized() or self.isFullScreen():
            return
        w, h = self.width(), self.height()
        if w <= 10 or h <= 10:
            return

        # Nudge both size and position
        self.resize(w + 1, h + 1)
        orig_pos = self.pos()
        self.move(orig_pos.x(), orig_pos.y() + 1)

        def _restore():
            self.resize(w, h)
            self.move(orig_pos)
            _force_dwm_redraw(int(self.winId()))
            self._force_webview_repaint()

        QTimer.singleShot(200, _restore)

    def _hard_resize_nudge(self):
        if self._mini_mode or self.isMaximized() or self.isFullScreen():
            return
        if self._did_hard_refresh:
            return
        self._force_refresh()
        self._did_hard_refresh = True

    def _schedule_backdrop_apply(self):
        for delay in (0, 200, 800):
            QTimer.singleShot(delay, self._apply_backdrop)

    def update_theme_mode(self, theme_mode):
        self._theme_mode = theme_mode
        self._apply_page_background()
        if self._loading_overlay is not None:
            root = self._loading_overlay.rootObject()
            if root is not None:
                root.setProperty(
                    "darkMode", bool(_resolve_theme_dark(self._theme_mode))
                )
        self._schedule_backdrop_apply()

    def apply_backdrop_settings(self):
        self._apply_page_background()
        if self._loading_overlay is not None:
            root = self._loading_overlay.rootObject()
            if root is not None:
                root.setProperty(
                    "darkMode", bool(_resolve_theme_dark(self._theme_mode))
                )
        self._schedule_backdrop_apply()

    def apply_animation_preference(self, disabled):
        self._disable_animations = bool(disabled)
        try:
            self.page().settings().setAttribute(
                QWebEngineSettings.WebAttribute.ScrollAnimatorEnabled,
                not self._disable_animations,
            )
        except Exception:
            pass
        try:
            state = "true" if self._disable_animations else "false"
            self.page().runJavaScript(
                "try {"
                f"window.__LUMINALIUM_DISABLE_ANIMATIONS = {state};"
                f"document.documentElement.setAttribute('data-disable-animations', '{state}');"
                "} catch (e) {}"
            )
        except Exception:
            pass
        if self._loading_overlay is not None:
            try:
                root = self._loading_overlay.rootObject()
                if root is not None:
                    root.setProperty("animationsEnabled", not self._disable_animations)
            except Exception:
                pass

    def showEvent(self, event):
        super().showEvent(event)
        if self._pending_url is not None:
            self.load(self._pending_url)
            self._pending_url = None
        if self._pending_load_timer is not None:
            try:
                self._pending_load_timer.stop()
            except Exception:
                pass
            self._pending_load_timer = None
        self._sync_loading_overlay_geometry()
        if self._loading_overlay is not None:
            try:
                root = self._loading_overlay.rootObject()
                if root is not None and bool(root.property("loading")):
                    self._loading_overlay.show()
                    self._raise_loading_overlay()
            except Exception:
                pass
        self._apply_page_background()
        self._schedule_backdrop_apply()
        if self._is_system_backdrop_enabled():
            # Consolidate refreshes to a single robust nudge
            QTimer.singleShot(300, self._hard_resize_nudge)
            QTimer.singleShot(500, self._force_webview_transparent)
            QTimer.singleShot(600, self._force_webview_repaint)

    def moveEvent(self, event):
        super().moveEvent(event)
        self._sync_loading_overlay_geometry()

    def resizeEvent(self, event):
        super().resizeEvent(event)
        self._sync_loading_overlay_geometry()

    def hideEvent(self, event):
        if self._loading_overlay is not None:
            try:
                self._loading_overlay.hide()
            except Exception:
                pass
        super().hideEvent(event)

    def closeEvent(self, event):
        """Clean up timers and resources when window closes"""
        if self._crash_recovery_timer is not None:
            try:
                self._crash_recovery_timer.stop()
            except Exception:
                pass
            self._crash_recovery_timer = None
        if self._pending_load_timer is not None:
            try:
                self._pending_load_timer.stop()
            except Exception:
                pass
            self._pending_load_timer = None
        if self._loading_overlay_hide_timer is not None:
            try:
                self._loading_overlay_hide_timer.stop()
            except Exception:
                pass
            self._loading_overlay_hide_timer = None
        if self._loading_overlay is not None:
            try:
                self._loading_overlay.close()
            except Exception:
                pass
            try:
                self._loading_overlay.deleteLater()
            except Exception:
                pass
            self._loading_overlay = None
        super().closeEvent(event)


def apply_win11_aesthetics(window, theme_mode=None, settings=None, window_tag=""):
    try:
        hwnd = int(window.winId())
        dwmapi = ctypes.windll.dwmapi
        corner_preference = ctypes.c_int(2)
        dwmapi.DwmSetWindowAttribute(
            hwnd, 33, ctypes.byref(corner_preference), ctypes.sizeof(corner_preference)
        )
        backdrop_type = _resolve_system_backdrop_type(settings, window_tag)
        _apply_window_theme(hwnd, _resolve_theme_dark(theme_mode), backdrop_type)
        _apply_system_backdrop(hwnd, backdrop_type)
        if backdrop_type is not None and backdrop_type != DWMSBT_NONE:
            border = ctypes.c_int(_DWM_COLOR_NONE)
            dwmapi.DwmSetWindowAttribute(
                hwnd, DWMWA_BORDER_COLOR, ctypes.byref(border), ctypes.sizeof(border)
            )
        icon_path = _resolve_logo_ico_path()
        if icon_path and os.path.exists(icon_path):
            user32 = ctypes.windll.user32
            IMAGE_ICON = 1
            LR_LOADFROMFILE = 0x00000010
            WM_SETICON = 0x0080
            ICON_SMALL = 0
            ICON_BIG = 1

            hicon_small = user32.LoadImageW(
                0, icon_path, IMAGE_ICON, 16, 16, LR_LOADFROMFILE
            )
            hicon_big = user32.LoadImageW(
                0, icon_path, IMAGE_ICON, 32, 32, LR_LOADFROMFILE
            )
            if hicon_small:
                user32.SendMessageW(hwnd, WM_SETICON, ICON_SMALL, hicon_small)
            if hicon_big:
                user32.SendMessageW(hwnd, WM_SETICON, ICON_BIG, hicon_big)
    except Exception:
        pass


def main():
    _apply_chromium_flags()
    app = QApplication(sys.argv)
    _warmup_webengine()
    icon = _load_app_icon()
    if not icon.isNull():
        app.setWindowIcon(icon)
    os.environ["QT_AUTO_SCREEN_SCALE_FACTOR"] = "1"
    if "--dialog" in sys.argv or "--crash-file" in sys.argv:
        mode = "--dialog" if "--dialog" in sys.argv else "--crash-file"
        try:
            idx = sys.argv.index(mode)
            file_path = sys.argv[idx + 1]
        except ValueError:
            return
        settings_path = os.environ.get("SETTINGS_PATH")
        if not settings_path:
            base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            settings_path = os.path.join(base_dir, "settings.json")
        default_theme = "auto"
        default_accent = "#3275F5"
        settings = {}
        if os.path.exists(settings_path):
            try:
                with open(settings_path, "r", encoding="utf-8") as f:
                    settings = json.load(f)
                    default_theme = (
                        settings.get("Appearance", {}).get("ThemeMode", "Auto").lower()
                    )
                    if default_theme == "dark":
                        default_accent = "#E1EBFF"
                    else:
                        default_accent = "#3275F5"
            except Exception:
                settings = {}
        dialog_data = {}
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                if mode == "--dialog":
                    dialog_data = json.load(f)
                    if "theme" not in dialog_data or dialog_data["theme"] == "auto":
                        dialog_data["theme"] = default_theme
                    if "accentColor" not in dialog_data:
                        dialog_data["accentColor"] = default_accent
                else:
                    error_msg = f.read()
                    try:
                        parent_pid = int(os.environ.get("CRASH_PARENT_PID", "0") or 0)
                    except Exception:
                        parent_pid = 0
                    dialog_data = {
                        "title": "程序崩溃了 (´；ω；`) ",
                        "text": error_msg,
                        "isError": True,
                        "confirmText": "关闭",
                        "cancelText": "复制错误",
                        "theme": default_theme,
                        "accentColor": default_accent,
                        "parentPid": parent_pid,
                    }
        except Exception:
            pass
        try:
            os.remove(file_path)
        except Exception:
            pass
        api = Api()
        api.dialog_data = dialog_data
        if "overrideSettings" in dialog_data:
            api.settings = dialog_data["overrideSettings"]
        else:
            api.settings = settings
        base_dir = os.path.dirname(os.path.abspath(__file__))
        if mode == "--crash-file":
            html_path = os.path.join(
                os.path.dirname(base_dir), "ppt_assistant", "ui", "crash_dialog.html"
            )
            win_width = 900
            win_height = 600
        else:
            html_path = os.path.join(
                os.path.dirname(base_dir), "ppt_assistant", "ui", "dialog.html"
            )
            win_width = 650
            win_height = 500
        defer_load = os.environ.get("DEFER_WEBENGINE_LOAD", "").strip().lower() in [
            "1",
            "true",
            "yes",
            "on",
        ]
        defer_load = _should_defer_initial_load(
            html_path, dialog_data.get("title", "Dialog"), defer_load
        )
        window = MainWindow(
            dialog_data.get("title", "Dialog"),
            html_path,
            api,
            win_width,
            win_height,
            dialog_data.get("theme", default_theme),
            defer_load=defer_load,
        )
        window.show()
    elif len(sys.argv) >= 5:
        url = sys.argv[1]
        title = sys.argv[2]
        width = int(sys.argv[3])
        height = int(sys.argv[4])
        custom_border = False
        if len(sys.argv) >= 6:
            custom_border = str(sys.argv[5]).strip().lower() in [
                "1",
                "true",
                "yes",
                "on",
            ]
        api = Api()
        settings_path = os.environ.get("SETTINGS_PATH")
        if not settings_path:
            if getattr(sys, "frozen", False):
                settings_path = os.path.join(
                    os.path.dirname(sys.executable), "settings.json"
                )
            else:
                base_dir = os.path.dirname(os.path.abspath(__file__))
                settings_path = os.path.join(os.path.dirname(base_dir), "settings.json")
        api.settings = {}
        if settings_path and os.path.exists(settings_path):
            try:
                with open(settings_path, "r", encoding="utf-8") as f:
                    api.settings = json.load(f)
            except Exception:
                pass
        try:
            base_dir = os.path.dirname(os.path.abspath(__file__))
            root_dir = os.path.dirname(base_dir)
            version_path = os.path.join(root_dir, "version.json")
            if os.path.exists(version_path):
                with open(version_path, "r", encoding="utf-8") as f:
                    api.version = json.load(f)
        except Exception:
            api.version = {}
        theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
        defer_load = os.environ.get("DEFER_WEBENGINE_LOAD", "").strip().lower() in [
            "1",
            "true",
            "yes",
            "on",
        ]
        defer_load = _should_defer_initial_load(url, title, defer_load)
        window = MainWindow(
            title, url, api, width, height, theme_mode, custom_border, defer_load
        )
        if title == "Settings":
            window.setMinimumWidth(1099)
        window.show()
    else:
        return
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
