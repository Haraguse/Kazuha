import sys
import os

CHROMIUM_FLAGS = "--disable-web-security --allow-file-access-from-files --allow-running-insecure-content --disable-features=IsolateOrigins,site-per-process"
existing_flags = os.environ.get("QTWEBENGINE_CHROMIUM_FLAGS", "")
if CHROMIUM_FLAGS not in existing_flags:
    os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = (existing_flags + " " + CHROMIUM_FLAGS).strip()

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


from PySide6.QtWidgets import QApplication, QFileDialog, QWidget, QLabel, QVBoxLayout, QHBoxLayout, QGraphicsOpacityEffect, QPushButton
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
    Signal,
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
    QEvent,
)
from PySide6.QtGui import QColor, QImage, QGuiApplication, QIcon, QFont, QPixmap
from utils.env_utils import get_device_uuid
from ppt_assistant.core.icon_helper import get_file_icon_base64
from ppt_assistant.core.platform_integration import (
    get_quick_launch_dialog_filter,
    set_run_at_startup as platform_set_run_at_startup,
)
from ppt_assistant.core.windows_notifications import (
    create_windows_notification_shortcut,
    get_windows_notification_app_name,
)

DWMWA_WINDOW_CORNER_PREFERENCE = 33
DWMWCP_ROUND = 2
DWMWA_USE_IMMERSIVE_DARK_MODE = 20
DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19
DWMWA_BORDER_COLOR = 34
DWMWA_CAPTION_COLOR = 35
DWMWA_TEXT_COLOR = 36
_DWM_COLOR_DEFAULT = 0xFFFFFFFF
_SHARED_PROFILE = None
_WEBENGINE_WARMUP_PAGE = None
_WEBENGINE_WARMUP_DONE = False
_WEBENGINE_WARMUP_RETAINED = False

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
    # PyInstaller: resources may be in sys._MEIPASS
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        meipass = sys._MEIPASS
        candidates.insert(0, os.path.join(meipass, "icons", "logo.svg"))
        candidates.insert(1, os.path.join(meipass, "icons", "banner.png"))
    # Nuitka: resources may be in _internal subfolder
    if getattr(sys, "frozen", False):
        exe_dir = os.path.dirname(sys.executable)
        internal_dir = os.path.join(exe_dir, "_internal")
        candidates.insert(0, os.path.join(internal_dir, "icons", "logo.svg"))
        candidates.insert(1, os.path.join(internal_dir, "icons", "banner.png"))
        candidates.insert(2, os.path.join(exe_dir, "icons", "logo.svg"))
        candidates.insert(3, os.path.join(exe_dir, "icons", "banner.png"))
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
    # PyInstaller: resources may be in sys._MEIPASS
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        meipass = sys._MEIPASS
        candidates.insert(0, os.path.join(meipass, "fonts", "MiSansVF.ttf"))
        candidates.insert(1, os.path.join(meipass, "fonts", "MiSansTCVF.ttf"))
        candidates.insert(2, os.path.join(meipass, "fonts", "MiSansJapaneseVF.ttf"))
    # Nuitka: resources may be in _internal subfolder
    if getattr(sys, "frozen", False):
        exe_dir = os.path.dirname(sys.executable)
        internal_dir = os.path.join(exe_dir, "_internal")
        candidates.insert(0, os.path.join(internal_dir, "fonts", "MiSansVF.ttf"))
        candidates.insert(1, os.path.join(internal_dir, "fonts", "MiSansTCVF.ttf"))
        candidates.insert(2, os.path.join(internal_dir, "fonts", "MiSansJapaneseVF.ttf"))
        candidates.insert(3, os.path.join(exe_dir, "fonts", "MiSansVF.ttf"))
        candidates.insert(4, os.path.join(exe_dir, "fonts", "MiSansTCVF.ttf"))
        candidates.insert(5, os.path.join(exe_dir, "fonts", "MiSansJapaneseVF.ttf"))
    for path in candidates:
        if path and os.path.exists(path):
            return path
    return None


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


def _list_user_splashes() -> list[dict]:
    root_dir = _get_user_root_dir()
    splash_dir = os.path.join(root_dir, "user", "splash")
    if not os.path.isdir(splash_dir):
        splash_dir = os.path.join(root_dir, "users", "splash")

    results: list[dict] = []
    if not os.path.isdir(splash_dir):
        return results
    for name in os.listdir(splash_dir):
        theme_dir = os.path.join(splash_dir, name)
        if not os.path.isdir(theme_dir):
            continue
        manifest_path = os.path.join(theme_dir, "manifest.json")
        preview_png = os.path.join(theme_dir, "preview.png")
        preview_jpg = os.path.join(theme_dir, "preview.jpg")
        splash_py = os.path.join(theme_dir, "splash.py")
        if not os.path.exists(splash_py):
            continue
        if not os.path.exists(manifest_path):
            continue
        if not (os.path.exists(preview_png) or os.path.exists(preview_jpg)):
            continue
        try:
            with open(manifest_path, "r", encoding="utf-8-sig") as f:
                data = json.load(f)
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


def _apply_window_theme(hwnd, is_dark):
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
        if is_dark:
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


def _animations_disabled(settings) -> bool:
    if not isinstance(settings, dict):
        return False
    general = settings.get("General")
    if not isinstance(general, dict):
        return False
    return bool(general.get("DisableAnimations"))


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


def _is_win11():
    try:
        v = sys.getwindowsversion()
        return v.major >= 10 and v.build >= 22000
    except Exception:
        return False


def _configure_profile(profile):
    if profile is None:
        return None

    try:
        profile.setHttpUserAgent(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36"
        )
    except Exception:
        pass

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
            cache_path = os.path.join(cache_root, f"pid-{os.getpid()}")
            os.makedirs(cache_path, exist_ok=True)
            profile.setCachePath(cache_path)
            profile.setHttpCacheType(QWebEngineProfile.HttpCacheType.DiskHttpCache)
            profile.setHttpCacheMaximumSize(10 * 1024 * 1024)
        else:
            profile.setHttpCacheType(QWebEngineProfile.HttpCacheType.MemoryHttpCache)

        try:
            settings = profile.settings()
            if settings is not None:
                try:
                    settings.setAttribute(
                        QWebEngineSettings.WebAttribute.LocalStorageEnabled, True
                    )
                    settings.setAttribute(
                        QWebEngineSettings.WebAttribute.LocalContentCanAccessRemoteUrls, True
                    )
                    settings.setAttribute(
                        QWebEngineSettings.WebAttribute.LocalContentCanAccessFileUrls, True
                    )
                    settings.setAttribute(
                        QWebEngineSettings.WebAttribute.AllowRunningInsecureContent, True
                    )
                    settings.setAttribute(
                        QWebEngineSettings.WebAttribute.JavascriptCanAccessClipboard, True
                    )
                    settings.setDefaultTextEncoding("utf-8")

                    try:
                        app = QCoreApplication.instance()
                        if app is not None:
                            font = app.font()
                            family = font.family()
                            settings.setFontFamily(QWebEngineSettings.FontFamily.StandardFont, family)
                            settings.setFontFamily(QWebEngineSettings.FontFamily.SansSerifFont, family)
                            settings.setFontFamily(QWebEngineSettings.FontFamily.SerifFont, family)
                            settings.setFontFamily(QWebEngineSettings.FontFamily.FixedFont, family)
                    except Exception:
                        pass
                except Exception:
                    pass
        except Exception:
            pass

        try:
            profile.clearHttpCache()
        except Exception:
            pass
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


def _cleanup_shared_profile():
    """Clear shared profile caches to free memory."""
    global _SHARED_PROFILE
    if _SHARED_PROFILE is not None:
        try:
            _SHARED_PROFILE.clearHttpCache()
        except Exception:
            pass
        try:
            _SHARED_PROFILE.clearAllVisitedLinks()
        except Exception:
            pass


def _trim_webengine_memory():
    try:
        import gc
        for gen in range(3):
            gc.collect(gen)
        if sys.platform == "win32":
            try:
                handle = ctypes.windll.kernel32.GetCurrentProcess()
                ctypes.windll.kernel32.SetProcessWorkingSetSize(handle, -1, -1)
            except Exception:
                pass
    except Exception:
        pass


def _release_warmup_placeholder():
    """Drop the retained placeholder page once a real view takes over."""
    global _WEBENGINE_WARMUP_PAGE, _WEBENGINE_WARMUP_RETAINED
    page = _WEBENGINE_WARMUP_PAGE
    _WEBENGINE_WARMUP_PAGE = None
    _WEBENGINE_WARMUP_RETAINED = False
    if page is None:
        return
    try:
        page.clearMemoryCaches()
    except Exception:
        pass
    try:
        page.deleteLater()
    except Exception:
        pass


def _warmup_webengine(retain_placeholder=False):
    global _WEBENGINE_WARMUP_PAGE, _WEBENGINE_WARMUP_DONE, _WEBENGINE_WARMUP_RETAINED
    if _WEBENGINE_WARMUP_DONE and not _WEBENGINE_WARMUP_RETAINED:
        return
    if _WEBENGINE_WARMUP_RETAINED and _WEBENGINE_WARMUP_PAGE is not None:
        return
    try:
        profile = _get_shared_profile()
        app = QCoreApplication.instance()
        if profile is None or app is None:
            return
        page = QWebEnginePage(profile, app)
        page.setBackgroundColor(Qt.transparent)

        def _finish(*_args):
            global _WEBENGINE_WARMUP_PAGE, _WEBENGINE_WARMUP_DONE, _WEBENGINE_WARMUP_RETAINED
            if _WEBENGINE_WARMUP_DONE and not _WEBENGINE_WARMUP_RETAINED:
                return
            _WEBENGINE_WARMUP_DONE = True
            if retain_placeholder:
                _WEBENGINE_WARMUP_RETAINED = True
                return
            _WEBENGINE_WARMUP_PAGE = None
            _WEBENGINE_WARMUP_RETAINED = False
            try:
                page.clearMemoryCaches()
            except Exception:
                pass
            try:
                page.deleteLater()
            except Exception:
                pass
            _trim_webengine_memory()

        page.loadFinished.connect(_finish)
        QTimer.singleShot(800, _finish)
        page.setHtml(
            "<!doctype html><html><head></head><body></body></html>",
            QUrl("about:blank"),
        )
        _WEBENGINE_WARMUP_PAGE = page
    except Exception:
        _WEBENGINE_WARMUP_DONE = True
        _WEBENGINE_WARMUP_RETAINED = False


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
        create_windows_notification_shortcut(
            target_path=target_path,
            shortcut_path=shortcut_path,
            work_dir=work_dir,
            icon_path=icon_path,
            args=args,
            app_name=get_windows_notification_app_name(),
        )
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
    storage_info_ready = Signal(dict)
    _ICON_CACHE_MAX = 32

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
        self._storage_cache = None
        self._dir_sizes_cache = {}
        self._dir_sizes_cache_path = None
        self.storage_info_ready.connect(self._on_storage_info_ready)

    def set_window(self, window):
        self._window = window

    def _cache_file_icon(self, path, icon_data):
        if not path or not icon_data:
            return
        if len(self._icon_cache) >= self._ICON_CACHE_MAX and path not in self._icon_cache:
            try:
                self._icon_cache.pop(next(iter(self._icon_cache)))
            except StopIteration:
                pass
        self._icon_cache[path] = icon_data

    def _on_storage_info_ready(self, full):
        self._storage_cache = full
        if self._window:
            page = self._window.page()
            if page:
                page.runJavaScript(f"window._onStorageFullInfo({json.dumps(full)})")

    def _ensure_cache_dir(self):
        profiles_dir = self._get_profiles_dir()
        cache_dir = os.path.dirname(profiles_dir)
        return cache_dir

    def _get_cache_path(self):
        if self._dir_sizes_cache_path is None:
            cache_dir = self._ensure_cache_dir()
            self._dir_sizes_cache_path = os.path.join(cache_dir, "_dir_sizes_cache.json")
        return self._dir_sizes_cache_path

    def _load_dir_sizes_cache(self):
        path = self._get_cache_path()
        try:
            if os.path.exists(path):
                with open(path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if isinstance(data, dict):
                    return data
        except Exception:
            pass
        return {}

    def _save_dir_sizes_cache(self, data):
        path = self._get_cache_path()
        try:
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2)
        except Exception:
            pass

    @Slot()
    def start_window_drag(self):
        try:
            if self._window:
                hwnd = int(self._window.winId())
                ctypes.windll.user32.ReleaseCapture()
                ctypes.windll.user32.SendMessageW(hwnd, 0x0112, 0xF012, 0)
        except Exception:
            pass

    @Slot()
    def minimize_window(self):
        try:
            if self._window:
                hwnd = int(self._window.winId())
                ctypes.windll.user32.SendMessageW(hwnd, 0x0112, 0xF020, 0)
        except Exception:
            pass

    @Slot()
    def toggle_maximize(self):
        try:
            if self._window:
                hwnd = int(self._window.winId())
                if self._window.isMaximized():
                    ctypes.windll.user32.SendMessageW(hwnd, 0x0112, 0xF120, 0)
                else:
                    ctypes.windll.user32.SendMessageW(hwnd, 0x0112, 0xF030, 0)
        except Exception:
            pass

    @Slot()
    def close_window(self):
        try:
            if self._window:
                self._window.close()
        except Exception:
            pass

    @Slot(result=str)
    def get_window_title(self):
        try:
            if self._window:
                return self._window.windowTitle()
        except Exception:
            pass
        return ""

    @Slot(result=str)
    def get_window_icon_path(self):
        try:
            ico_path = _resolve_logo_ico_path()
            if ico_path:
                return ico_path.replace("\\", "/")
            svg_path = _resolve_logo_svg_path()
            if svg_path:
                return svg_path.replace("\\", "/")
        except Exception:
            pass
        return ""

    def set_in_process(self, enabled=True):
        self._in_process = bool(enabled)

    def _close_current_window(self):
        if self._window:
            try:
                self._window.close()
            except Exception:
                pass

    @Slot()
    def force_close_window(self):
        """强制关闭当前窗口，用于 onboarding 等场景"""
        try:
            if self._window:
                hwnd = self._get_window_hwnd()
                # 先尝试正常关闭
                self._window.close()
                # 启动独立进程确保窗口被关闭
                if hwnd:
                    QTimer.singleShot(300, lambda: self._spawn_window_killer(hwnd))
                else:
                    QTimer.singleShot(500, self._force_destroy_window)
        except Exception:
            pass

    def _spawn_window_killer(self, hwnd):
        """启动独立进程强制关闭窗口"""
        try:
            import subprocess
            # 使用独立的 Python 进程发送 WM_CLOSE
            kill_script = f"""
import ctypes
import time
time.sleep(0.5)
hwnd = {hwnd}
ctypes.windll.user32.SendMessageW(hwnd, 0x0010, 0, 0)
"""
            subprocess.Popen(
                [sys.executable, "-c", kill_script.strip()],
                creationflags=0x08000000,
            )
        except Exception:
            pass
        # 同时尝试销毁
        self._force_destroy_window()

    def _force_destroy_window(self):
        """强制销毁窗口的后备方案"""
        try:
            if self._window:
                self._window.deleteLater()
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
            js = f"if (typeof updateTheme === 'function') updateTheme({json.dumps(theme_mode)}, {json.dumps(theme_id)});if(typeof window.__applyUnifiedTheme==='function')window.__applyUnifiedTheme();"
            self._window.page().runJavaScript(js)
            try:
                _apply_window_theme(
                    int(self._window.winId()),
                    _resolve_theme_dark(theme_mode),
                )
            except Exception:
                pass

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
    def get_self_pen_settings(self):
        try:
            from ppt_assistant.core.config import cfg

            return {
                "PenWidth": cfg.selfPenWidth.value,
                "HighlightWidth": cfg.selfHighlightWidth.value,
                "EraserWidth": cfg.selfEraserWidth.value,
                "HighlightOpacity": cfg.selfHighlightOpacity.value,
                "PenColor": cfg.selfPenColor.value,
                "HighlightColor": cfg.selfHighlightColor.value,
                "FrameRateMode": cfg.selfPenFrameRateMode.value,
                "PenEffect": cfg.selfPenPenEffect.value,
                "PalmErase": cfg.selfPenPalmErase.value,
                "PenWidthPresetIndex": cfg.selfPenWidthPresetIndex.value,
                "EraserWidthPresetIndex": cfg.selfEraserWidthPresetIndex.value,
                "CustomPenColors": cfg.selfCustomPenColors.value,
                "CustomHighlightColors": cfg.selfCustomHighlightColors.value,
            }
        except Exception as e:
            print(f"get_self_pen_settings error: {e}", file=sys.stderr)
            return {}

    @Slot(result="QVariant")
    def get_version(self):
        return self.version

    @Slot(result="QVariant")
    def get_diagnostic_info(self):
        try:
            from plugins.builtins.settings.diagnostic_info import collect_diagnostic_info
            return collect_diagnostic_info()
        except Exception as e:
            return {"error": str(e)}

    @Slot(result=str)
    def get_platform(self):
        return str(sys.platform or "")

    @Slot(result="QVariant")
    def get_overlay_themes(self):
        return _list_user_themes()

    @Slot(result="QVariant")
    def get_splash_styles(self):
        return _list_user_splashes()

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
            if getattr(sys, "frozen", False):
                settings_path = os.path.join(os.path.dirname(sys.executable), "settings.json")
            else:
                base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
                settings_path = os.path.join(base_dir, "settings.json")
        return settings_path

    def _get_active_settings_path(self):
        active_path = self._get_active_profile_path()
        if os.path.exists(active_path):
            try:
                with open(active_path, "r", encoding="utf-8") as f:
                    name = f.read().strip()
                if name and name != "default":
                    profile_path = os.path.join(self._get_profiles_dir(), name + ".json")
                    if os.path.exists(profile_path):
                        return profile_path
            except Exception:
                pass
        return self._get_settings_path()

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
                    self._cache_file_icon(path, icon_data)
            if icon_data:
                app["icon"] = icon_data
        return apps

    @Slot(result="QVariant")
    def get_quick_launch_apps(self):
        settings_path = self._get_active_settings_path()
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
        settings_path = self._get_active_settings_path()
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
        settings_path = self._get_active_settings_path()
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
        settings_path = self._get_active_settings_path()
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
        active_path = self._get_active_settings_path()
        try:
            data = {}
            if os.path.exists(active_path):
                with open(active_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except JSONDecodeError:
                        data = {}
            if category not in data:
                data[category] = {}
            data[category][key] = value
            os.makedirs(os.path.dirname(active_path), exist_ok=True)
            with open(active_path, "w", encoding="utf-8") as f:
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
            elif category == "Linkage" and key == "SecRandomEnabled":
                try:
                    from ppt_assistant.core.config import cfg

                    cfg.secRandomEnabled.value = bool(value)
                except Exception:
                    pass
            elif category == "SelfPen":
                try:
                    from ppt_assistant.core.config import cfg

                    _SELF_PEN_INT_KEYS = {
                        "PenWidth": "selfPenWidth",
                        "HighlightWidth": "selfHighlightWidth",
                        "EraserWidth": "selfEraserWidth",
                        "PenWidthPresetIndex": "selfPenWidthPresetIndex",
                        "EraserWidthPresetIndex": "selfEraserWidthPresetIndex",
                    }
                    _SELF_PEN_FLOAT_KEYS = {"HighlightOpacity": "selfHighlightOpacity"}
                    _SELF_PEN_STR_KEYS = {
                        "PenColor": "selfPenColor",
                        "HighlightColor": "selfHighlightColor",
                        "FrameRateMode": "selfPenFrameRateMode",
                        "PenEffect": "selfPenPenEffect",
                        "CustomPenColors": "selfCustomPenColors",
                        "CustomHighlightColors": "selfCustomHighlightColors",
                    }
                    _SELF_PEN_BOOL_KEYS = {
                        "PalmErase": "selfPenPalmErase",
                    }
                    attr = None
                    casted = None
                    if key in _SELF_PEN_INT_KEYS:
                        attr = _SELF_PEN_INT_KEYS[key]
                        casted = int(value)
                    elif key in _SELF_PEN_FLOAT_KEYS:
                        attr = _SELF_PEN_FLOAT_KEYS[key]
                        casted = float(value)
                    elif key in _SELF_PEN_STR_KEYS:
                        attr = _SELF_PEN_STR_KEYS[key]
                        casted = str(value)
                    elif key in _SELF_PEN_BOOL_KEYS:
                        attr = _SELF_PEN_BOOL_KEYS[key]
                        casted = bool(value)
                    if attr is not None and getattr(cfg, attr).value != casted:
                        getattr(cfg, attr).value = casted
                except Exception as e:
                    print(f"SelfPen save_setting sync error: {e}", file=sys.stderr)

            if category == "Appearance" and key in ("ThemeMode", "ThemeId"):
                self.update_settings(data)
        except Exception as e:
            print(f"Error saving settings: {e}", file=sys.stderr)

    @Slot(str, str, bool)
    def preview_theme(self, theme_mode, theme_id, is_dark):
        if not self._window:
            return
        theme_mode = str(theme_mode or "Auto")
        theme_id = str(theme_id or "default")
        is_dark = bool(is_dark)
        if not isinstance(self.settings, dict):
            self.settings = {}
        appearance = self.settings.setdefault("Appearance", {})
        appearance["ThemeMode"] = theme_mode
        appearance["ThemeId"] = theme_id
        appearance["ResolvedIsDark"] = is_dark
        self._window._theme_mode = theme_mode
        self._window._theme_dark_override = is_dark
        try:
            self._window._apply_page_background()
            _apply_window_theme(int(self._window.winId()), is_dark)
        except Exception:
            pass

    def _get_profiles_dir(self):
        settings_path = self._get_settings_path()
        return os.path.dirname(settings_path)

    def _get_active_profile_path(self):
        return os.path.join(self._get_profiles_dir(), "_active")

    def _sanitize_profile_name(self, name):
        import re
        name = name.strip()
        name = re.sub(r'[<>:"/\\|?*]', '_', name)
        name = name.replace('..', '')
        if not name or name.startswith('_'):
            name = "profile_" + name.lstrip('_') if name.startswith('_') else "unnamed"
        return name[:64]

    def _get_profiles_registry_path(self):
        return os.path.join(self._get_profiles_dir(), "_profiles")

    def _read_profiles_registry(self):
        reg_path = self._get_profiles_registry_path()
        try:
            if os.path.exists(reg_path):
                with open(reg_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if isinstance(data, list):
                    return data
        except Exception:
            pass
        return []

    def _write_profiles_registry(self, names):
        reg_path = self._get_profiles_registry_path()
        try:
            with open(reg_path, "w", encoding="utf-8") as f:
                json.dump(sorted(set(names)), f, ensure_ascii=False)
        except Exception:
            pass

    @Slot(result="QVariant")
    def list_profiles(self):
        profiles_dir = self._get_profiles_dir()
        profiles = []
        try:
            active_name = "default"
            active_path = self._get_active_profile_path()
            if os.path.exists(active_path):
                try:
                    with open(active_path, "r", encoding="utf-8") as f:
                        active_name = f.read().strip() or "default"
                except Exception:
                    pass

            for pname in self._read_profiles_registry():
                fpath = os.path.join(profiles_dir, pname + ".json")
                if not os.path.exists(fpath):
                    continue
                try:
                    mtime = os.path.getmtime(fpath)
                    fsize = os.path.getsize(fpath)
                    profiles.append({
                        "name": pname,
                        "active": pname == active_name,
                        "mtime": mtime,
                        "size": fsize,
                    })
                except Exception:
                    continue
            profiles.sort(key=lambda p: p["name"].lower())
        except Exception as e:
            print(f"Error listing profiles: {e}", file=sys.stderr)
        return {"profiles": profiles, "active": active_name}

    @Slot(str, result="QVariant")
    def create_profile(self, name):
        name = self._sanitize_profile_name(name)
        profiles_dir = self._get_profiles_dir()
        profile_path = os.path.join(profiles_dir, name + ".json")
        if os.path.exists(profile_path):
            return {"success": False, "error": "profile_exists"}
        try:
            settings_path = self._get_active_settings_path()
            data = {}
            if os.path.exists(settings_path):
                with open(settings_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except Exception:
                        data = {}
            for k in ("_restart_pending", "_open_settings_pending", "_quit_pending"):
                data.pop(k, None)
            with open(profile_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            registry = self._read_profiles_registry()
            if name not in registry:
                registry.append(name)
                self._write_profiles_registry(registry)
            return {"success": True, "name": name}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(str, result="QVariant")
    def switch_profile(self, name):
        name = self._sanitize_profile_name(name)
        active_path = self._get_active_profile_path()

        if name == "default":
            try:
                with open(active_path, "w", encoding="utf-8") as f:
                    f.write("default")
                settings_path = self._get_settings_path()
                if os.path.exists(settings_path):
                    with open(settings_path, "r", encoding="utf-8") as f:
                        self.settings = json.load(f)
                return {"success": True, "name": "default"}
            except Exception as e:
                return {"success": False, "error": str(e)}

        profiles_dir = self._get_profiles_dir()
        profile_path = os.path.join(profiles_dir, name + ".json")
        if not os.path.exists(profile_path):
            return {"success": False, "error": "not_found"}
        try:
            with open(profile_path, "r", encoding="utf-8") as f:
                new_data = json.load(f)
            with open(active_path, "w", encoding="utf-8") as f:
                f.write(name)
            self.settings = new_data
            return {"success": True, "name": name}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(str, result="QVariant")
    def delete_profile(self, name):
        name = self._sanitize_profile_name(name)
        profiles_dir = self._get_profiles_dir()
        profile_path = os.path.join(profiles_dir, name + ".json")
        active_name = "default"
        active_path = self._get_active_profile_path()
        if os.path.exists(active_path):
            try:
                with open(active_path, "r", encoding="utf-8") as f:
                    active_name = f.read().strip() or "default"
            except Exception:
                pass
        if name == active_name:
            return {"success": False, "error": "cannot_delete_active"}
        if not os.path.exists(profile_path):
            return {"success": False, "error": "not_found"}
        try:
            os.remove(profile_path)
            registry = self._read_profiles_registry()
            if name in registry:
                registry.remove(name)
                self._write_profiles_registry(registry)
            return {"success": True}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(str, str, result="QVariant")
    def rename_profile(self, old_name, new_name):
        old_name = self._sanitize_profile_name(old_name)
        new_name = self._sanitize_profile_name(new_name)
        profiles_dir = self._get_profiles_dir()
        old_path = os.path.join(profiles_dir, old_name + ".json")
        new_path = os.path.join(profiles_dir, new_name + ".json")
        if not os.path.exists(old_path):
            return {"success": False, "error": "not_found"}
        if os.path.exists(new_path):
            return {"success": False, "error": "name_exists"}
        try:
            os.rename(old_path, new_path)
            active_name = "default"
            active_path = self._get_active_profile_path()
            if os.path.exists(active_path):
                try:
                    with open(active_path, "r", encoding="utf-8") as f:
                        active_name = f.read().strip() or "default"
                except Exception:
                    pass
            if active_name == old_name:
                with open(active_path, "w", encoding="utf-8") as f:
                    f.write(new_name)
            registry = self._read_profiles_registry()
            if old_name in registry:
                registry.remove(old_name)
            if new_name not in registry:
                registry.append(new_name)
            self._write_profiles_registry(registry)
            return {"success": True, "name": new_name}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(result="QVariant")
    def get_active_profile(self):
        active_path = self._get_active_profile_path()
        try:
            if os.path.exists(active_path):
                with open(active_path, "r", encoding="utf-8") as f:
                    name = f.read().strip() or "default"
                return {"name": name}
        except Exception:
            pass
        return {"name": "default"}

    def _get_backups_dir(self):
        profiles_dir = self._get_profiles_dir()
        backups_dir = os.path.join(profiles_dir, "_backups")
        os.makedirs(backups_dir, exist_ok=True)
        return backups_dir

    def _get_backups_registry_path(self):
        return os.path.join(self._get_backups_dir(), "_registry")

    def _read_backups_registry(self):
        reg_path = self._get_backups_registry_path()
        try:
            if os.path.exists(reg_path):
                with open(reg_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if isinstance(data, list):
                    return data
        except Exception:
            pass
        return []

    def _write_backups_registry(self, names):
        reg_path = self._get_backups_registry_path()
        try:
            with open(reg_path, "w", encoding="utf-8") as f:
                json.dump(sorted(set(names)), f, ensure_ascii=False)
        except Exception:
            pass

    @Slot(result="QVariant")
    def list_backups(self):
        backups_dir = self._get_backups_dir()
        backups = []
        try:
            for bname in self._read_backups_registry():
                bpath = os.path.join(backups_dir, bname + ".json")
                if not os.path.exists(bpath):
                    continue
                try:
                    mtime = os.path.getmtime(bpath)
                    fsize = os.path.getsize(bpath)
                    meta = {}
                    meta_path = os.path.join(backups_dir, bname + ".meta")
                    if os.path.exists(meta_path):
                        with open(meta_path, "r", encoding="utf-8") as mf:
                            meta = json.load(mf)
                    backups.append({
                        "name": bname,
                        "mtime": mtime,
                        "size": fsize,
                        "sourceProfile": meta.get("sourceProfile", ""),
                        "note": meta.get("note", ""),
                    })
                except Exception:
                    continue
            backups.sort(key=lambda b: b["mtime"], reverse=True)
        except Exception as e:
            print(f"Error listing backups: {e}", file=sys.stderr)
        return {"backups": backups}

    @Slot(str, str, str, result="QVariant")
    def create_backup(self, profile_name, backup_name, note=""):
        import shutil as _shutil
        import time as _time
        backup_name = self._sanitize_profile_name(backup_name)
        backups_dir = self._get_backups_dir()
        backup_path = os.path.join(backups_dir, backup_name + ".json")
        meta_path = os.path.join(backups_dir, backup_name + ".meta")
        if os.path.exists(backup_path):
            return {"success": False, "error": "backup_exists"}
        try:
            if profile_name == "default":
                source_path = self._get_settings_path()
            else:
                source_path = os.path.join(self._get_profiles_dir(), profile_name + ".json")
            if not os.path.exists(source_path):
                return {"success": False, "error": "profile_not_found"}
            _shutil.copy2(source_path, backup_path)
            meta = {
                "sourceProfile": profile_name,
                "note": note,
                "createdAt": _time.time(),
            }
            with open(meta_path, "w", encoding="utf-8") as f:
                json.dump(meta, f, indent=2, ensure_ascii=False)
            registry = self._read_backups_registry()
            if backup_name not in registry:
                registry.append(backup_name)
                self._write_backups_registry(registry)
            return {"success": True, "name": backup_name}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(str, str, result="QVariant")
    def restore_backup(self, backup_name, target_profile):
        backups_dir = self._get_backups_dir()
        backup_path = os.path.join(backups_dir, backup_name + ".json")
        if not os.path.exists(backup_path):
            return {"success": False, "error": "backup_not_found"}
        try:
            with open(backup_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            for k in ("_restart_pending", "_open_settings_pending", "_quit_pending"):
                data.pop(k, None)
            if target_profile == "default":
                target_path = self._get_settings_path()
            else:
                target_path = os.path.join(self._get_profiles_dir(), target_profile + ".json")
            with open(target_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            active_name = "default"
            active_path = self._get_active_profile_path()
            if os.path.exists(active_path):
                try:
                    with open(active_path, "r", encoding="utf-8") as f:
                        active_name = f.read().strip() or "default"
                except Exception:
                    pass
            if target_profile == active_name:
                self.settings = data
            return {"success": True, "targetProfile": target_profile}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(str, result="QVariant")
    def delete_backup(self, backup_name):
        backups_dir = self._get_backups_dir()
        backup_path = os.path.join(backups_dir, backup_name + ".json")
        meta_path = os.path.join(backups_dir, backup_name + ".meta")
        if not os.path.exists(backup_path):
            return {"success": False, "error": "not_found"}
        try:
            os.remove(backup_path)
            if os.path.exists(meta_path):
                os.remove(meta_path)
            registry = self._read_backups_registry()
            if backup_name in registry:
                registry.remove(backup_name)
                self._write_backups_registry(registry)
            return {"success": True}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(str, str, result="QVariant")
    def rename_backup(self, old_name, new_name):
        old_name = self._sanitize_profile_name(old_name)
        new_name = self._sanitize_profile_name(new_name)
        backups_dir = self._get_backups_dir()
        old_path = os.path.join(backups_dir, old_name + ".json")
        old_meta = os.path.join(backups_dir, old_name + ".meta")
        new_path = os.path.join(backups_dir, new_name + ".json")
        new_meta = os.path.join(backups_dir, new_name + ".meta")
        if not os.path.exists(old_path):
            return {"success": False, "error": "not_found"}
        if os.path.exists(new_path):
            return {"success": False, "error": "name_exists"}
        try:
            os.rename(old_path, new_path)
            if os.path.exists(old_meta):
                os.rename(old_meta, new_meta)
            registry = self._read_backups_registry()
            if old_name in registry:
                registry.remove(old_name)
            if new_name not in registry:
                registry.append(new_name)
            self._write_backups_registry(registry)
            return {"success": True, "name": new_name}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @Slot(result=str)
    def import_settings(self):
        """Import settings from a user-selected JSON file"""
        import json as json_module

        def _make_result(**payload):
            return json_module.dumps(payload, ensure_ascii=False)

        try:
            file_path, _ = QFileDialog.getOpenFileName(
                self._window, "选择配置文件", "", "JSON 配置文件 (*.json);;所有文件 (*)"
            )

            if not file_path:
                print("No file selected", file=sys.stderr)
                return _make_result(cancelled=True)

            print(f"Importing from: {file_path}", file=sys.stderr)

            try:
                with open(file_path, "rb") as f:
                    raw = f.read()
            except Exception as e:
                print(f"Failed to read config file: {e}", file=sys.stderr)
                return _make_result(
                    error="read_failed", message=f"无法读取配置文件：{e}"
                )

            if not raw.strip():
                print("Config file is empty", file=sys.stderr)
                return _make_result(
                    error="empty_file", message="配置文件为空，请选择有效的配置文件。"
                )

            config_data = None
            parse_error = None
            for encoding in ("utf-8-sig", "utf-8", "gbk", "gb18030"):
                try:
                    text = raw.decode(encoding)
                except UnicodeDecodeError:
                    continue
                try:
                    candidate = json_module.loads(text)
                except Exception as e:
                    parse_error = e
                    continue
                if isinstance(candidate, dict):
                    config_data = candidate
                    break
                return _make_result(
                    error="invalid_format",
                    message="配置文件格式无效，根对象必须是 JSON 对象。",
                )

            if config_data is None:
                print(f"Failed to parse config data: {parse_error}", file=sys.stderr)
                return _make_result(
                    error="parse_error",
                    message="配置文件解析失败，请确认文件是有效的 JSON 配置文件。",
                )

            print(f"Config data loaded: {list(config_data.keys())}", file=sys.stderr)

            if not isinstance(config_data, dict):
                print("Config is not a dict", file=sys.stderr)
                return _make_result(
                    error="invalid_format",
                    message="配置文件格式无效，根对象必须是 JSON 对象。",
                )

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
            if not imported_config:
                print("No supported config entries found", file=sys.stderr)
                return _make_result(
                    error="unsupported_config",
                    message="未检测到可导入的基础设置项，请确认这是 Luminalium 的配置文件。",
                )

            preview = "\n".join(preview_lines)

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
            return _make_result(
                error="unexpected_error",
                message=f"导入配置时发生错误：{e}",
            )

    def _get_pending_action_path(self):
        return os.path.join(self._get_profiles_dir(), "_pending_action")

    def _write_runtime_flag(self, flags):
        active_path = self._get_active_settings_path()
        base_path = self._get_settings_path()
        for path in (active_path, base_path):
            try:
                data = {}
                if os.path.exists(path):
                    with open(path, "r", encoding="utf-8") as f:
                        try:
                            data = json.load(f)
                        except Exception:
                            data = {}
                for k, v in flags.items():
                    if v is None:
                        data.pop(k, None)
                    else:
                        data[k] = v
                with open(path, "w", encoding="utf-8") as f:
                    json.dump(data, f, indent=4, ensure_ascii=False)
            except Exception:
                pass
        pending_path = self._get_pending_action_path()
        try:
            data = {}
            if os.path.exists(pending_path):
                with open(pending_path, "r", encoding="utf-8") as f:
                    try:
                        data = json.load(f)
                    except Exception:
                        data = {}
            for k, v in flags.items():
                if v is None:
                    data.pop(k, None)
                else:
                    data[k] = v
            with open(pending_path, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False)
        except Exception:
            pass

    @Slot()
    def restart_app(self):
        self._write_runtime_flag({"_restart_pending": True})

    @Slot()
    def restart_and_open_settings(self):
        self._write_runtime_flag({"_quit_pending": None, "_restart_pending": True, "_open_settings_pending": True})

    @Slot()
    def quit_app(self):
        self._write_runtime_flag({"_restart_pending": None, "_open_settings_pending": None, "_quit_pending": True})

    @Slot()
    def clear_onboarding_pending_actions(self):
        self._write_runtime_flag({"_restart_pending": None, "_open_settings_pending": None, "_quit_pending": None})

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
        settings_path = self._get_active_settings_path()
        base_path = self._get_settings_path()
        reset_marker = self._get_settings_reset_marker_path()
        try:
            if os.path.exists(settings_path):
                os.remove(settings_path)
            if base_path != settings_path and os.path.exists(base_path):
                os.remove(base_path)
            active_path = self._get_active_profile_path()
            if os.path.exists(active_path):
                os.remove(active_path)
            reg_path = self._get_profiles_registry_path()
            if os.path.exists(reg_path):
                os.remove(reg_path)
            pending_path = self._get_pending_action_path()
            if os.path.exists(pending_path):
                os.remove(pending_path)
        except Exception as e:
            print(f"Error deleting settings: {e}", file=sys.stderr)
        try:
            with open(reset_marker, "w", encoding="utf-8") as f:
                f.write("reset")
        except Exception as e:
            print(f"Error writing reset marker: {e}", file=sys.stderr)

    @Slot(int)
    def ensure_animation_smoothness(self, duration_ms):
        try:
            from PySide6.QtCore import QEventLoop, QTimer
            loop = QEventLoop()
            QTimer.singleShot(duration_ms, loop.quit)
            loop.exec(QEventLoop.ProcessEventsFlag.ExcludeUserInputEvents)
        except Exception as e:
            pass

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
        if getattr(sys, "frozen", False):
            subprocess.Popen(
                [sys.executable, "--dialog", temp_path], creationflags=0x08000000
            )
        else:
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
        if getattr(sys, "frozen", False):
            subprocess.Popen(
                [sys.executable, "--dialog", temp_path], creationflags=0x08000000
            )
        else:
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

    @Slot(int, str, "QVariant", result="QVariant")
    def get_logs_since(self, since_index=0, search_text="", levels=None):
        try:
            from ppt_assistant.core.log_manager import get_log_manager

            manager = get_log_manager()

            if levels is None:
                levels = ["debug", "info", "warn", "error"]
            elif isinstance(levels, str):
                levels = [levels]

            return manager.get_logs_since(
                since_index=since_index, levels=levels, search_text=search_text
            )
        except Exception as e:
            print(f"Error getting logs since: {e}", file=sys.stderr)
            return {"logs": [], "total_count": 0}

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

    @staticmethod
    def _get_dir_size(path, max_files=10000, skip_dirs=None):
        total = 0
        file_count = [0]
        skip = set(skip_dirs) if skip_dirs else set()
        if not os.path.exists(path):
            return 0
        try:
            for entry in os.scandir(path):
                if max_files > 0 and file_count[0] >= max_files:
                    break
                try:
                    if entry.is_file(follow_symlinks=False):
                        total += entry.stat().st_size
                        file_count[0] += 1
                    elif entry.is_dir(follow_symlinks=False):
                        if entry.name in skip:
                            continue
                        sub_total, sub_count = Api._get_dir_size_with_count(entry.path, max_files - file_count[0], skip)
                        total += sub_total
                        file_count[0] += sub_count
                        if max_files > 0 and file_count[0] >= max_files:
                            break
                except (OSError, PermissionError):
                    pass
        except (OSError, PermissionError):
            pass
        return total

    @staticmethod
    def _get_dir_size_with_count(path, max_files=10000, skip_dirs=None):
        total = 0
        count = 0
        skip = set(skip_dirs) if skip_dirs else set()
        if not os.path.exists(path):
            return 0, 0
        try:
            for entry in os.scandir(path):
                if max_files > 0 and count >= max_files:
                    break
                try:
                    if entry.is_file(follow_symlinks=False):
                        total += entry.stat().st_size
                        count += 1
                    elif entry.is_dir(follow_symlinks=False):
                        if entry.name in skip:
                            continue
                        sub_total, sub_count = Api._get_dir_size_with_count(entry.path, max_files - count, skip)
                        total += sub_total
                        count += sub_count
                        if max_files > 0 and count >= max_files:
                            break
                except (OSError, PermissionError):
                    pass
        except (OSError, PermissionError):
            pass
        return total, count

    @staticmethod
    def _get_all_disks_info():
        try:
            total_all = 0
            free_all = 0
            if sys.platform == "win32":
                import ctypes
                buf = ctypes.create_unicode_buffer(256)
                ctypes.windll.kernel32.GetLogicalDriveStringsW(256, buf)
                drives = []
                i = 0
                while i < 256 and buf[i] != '\x00':
                    s = ''
                    while i < 256 and buf[i] != '\x00':
                        s += buf[i]
                        i += 1
                    if s:
                        drives.append(s)
                    i += 1
                for drive in drives:
                    free_bytes = ctypes.c_ulonglong(0)
                    total_bytes = ctypes.c_ulonglong(0)
                    ret = ctypes.windll.kernel32.GetDiskFreeSpaceExW(
                        drive, None, ctypes.pointer(total_bytes), ctypes.pointer(free_bytes)
                    )
                    if ret:
                        total_all += total_bytes.value
                        free_all += free_bytes.value
            else:
                for mnt in os.listdir("/mnt") + ["/"]:
                    if os.path.ismount(mnt):
                        st = os.statvfs(mnt)
                        total_all += st.f_blocks * st.f_frsize
                        free_all += st.f_bavail * st.f_frsize
            return {"total": total_all, "free": free_all, "used": total_all - free_all}
        except Exception:
            return {"total": 0, "free": 0, "used": 0}

    @Slot(bool, result="QVariant")
    def get_storage_info(self, force=False):
        import time
        if not force and self._storage_cache is not None:
            return self._storage_cache

        try:
            local_app_data = os.getenv("LOCALAPPDATA", os.path.expanduser("~"))
            if getattr(sys, "frozen", False):
                app_dir = os.path.dirname(sys.executable)
            else:
                app_dir = ROOT_DIR

            local_luminalium_path = os.path.join(local_app_data, "Luminalium")
            update_bak_path = os.path.join(app_dir, ".update_bak")
            update_cache_path = os.path.join(local_luminalium_path, "update_cache")

            disk_info = self._get_all_disks_info()

            # Load persistent cache
            if not self._dir_sizes_cache:
                self._dir_sizes_cache = self._load_dir_sizes_cache()

            version_str = str(self.version if isinstance(self.version, str) else self.version.get("version", ""))
            cache_key = f"{app_dir}|{version_str}"
            cached_app_size = self._dir_sizes_cache.get(cache_key) if not force else None

            skip = {"node_modules", ".git", "__pycache__", ".venv", "venv", ".mypy_cache", ".pytest_cache", ".idea", "target", "build", "dist", ".update_bak"}

            partial = {
                "localLuminalium": {
                    "path": local_luminalium_path,
                    "size": 0,
                    "exists": os.path.exists(local_luminalium_path),
                },
                "updateBak": {
                    "path": update_bak_path,
                    "size": 0,
                    "exists": os.path.exists(update_bak_path),
                },
                "appDir": {
                    "path": app_dir,
                    "size": cached_app_size or 0,
                },
                "updateCache": {
                    "path": update_cache_path,
                    "size": 0,
                    "exists": os.path.exists(update_cache_path),
                },
                "disk": disk_info,
                "partial": True,
                "updatedAt": time.time(),
            }

            def _worker():
                try:
                    local_size = Api._get_dir_size(local_luminalium_path, skip_dirs=skip)
                    bak_size = Api._get_dir_size(update_bak_path)
                    app_size = cached_app_size
                    if app_size is None:
                        app_size = Api._get_dir_size(app_dir, skip_dirs=skip) or 0
                    cache_size = Api._get_dir_size(update_cache_path)

                    full = {
                        "localLuminalium": {
                            "path": local_luminalium_path,
                            "size": local_size,
                            "exists": os.path.exists(local_luminalium_path),
                        },
                        "updateBak": {
                            "path": update_bak_path,
                            "size": bak_size,
                            "exists": os.path.exists(update_bak_path),
                        },
                        "appDir": {
                            "path": app_dir,
                            "size": app_size or 0,
                        },
                        "updateCache": {
                            "path": update_cache_path,
                            "size": cache_size,
                            "exists": os.path.exists(update_cache_path),
                        },
                        "disk": disk_info,
                        "partial": False,
                        "updatedAt": time.time(),
                    }

                    # Persistently cache app_dir size
                    if cached_app_size is None and app_size:
                        self._dir_sizes_cache[cache_key] = app_size
                        self._save_dir_sizes_cache(self._dir_sizes_cache)

                    # Emit signal — automatically delivered to main thread via Qt queued connection
                    self.storage_info_ready.emit(full)
                except Exception as e:
                    print(f"Background storage info error: {e}")

            import threading
            t = threading.Thread(target=_worker, daemon=True)
            t.start()

            return partial
        except Exception as e:
            print(f"Storage info error: {e}")
            return self._storage_cache if self._storage_cache else {}

    @Slot(str, result="QVariant")
    def clean_directory(self, target):
        import shutil
        try:
            local_app_data = os.getenv("LOCALAPPDATA", os.path.expanduser("~"))
            if getattr(sys, "frozen", False):
                app_dir = os.path.dirname(sys.executable)
            else:
                app_dir = ROOT_DIR

            target_map = {
                "update_bak": os.path.join(app_dir, ".update_bak"),
                "update_cache": os.path.join(local_app_data, "Luminalium", "update_cache"),
            }

            path = target_map.get(target)
            if not path:
                return {"success": False, "error": "Invalid target"}

            if not os.path.exists(path):
                return {"success": True, "freed": 0}

            size_before = self._get_dir_size(path)
            shutil.rmtree(path, ignore_errors=True)
            size_after = self._get_dir_size(path) if os.path.exists(path) else 0
            freed = size_before - size_after

            self._storage_cache = None
            return {"success": True, "freed": freed}
        except Exception as e:
            return {"success": False, "error": str(e)}

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


def _get_unified_theme_js():
    """Returns JavaScript that injects unified theme CSS variables
    and sets data-theme/variant/id attributes from window.initialSettings.
    All theme colors are controlled by webview_runner exclusively.
    """
    import json as _json

    themes = {
        "default": {
            "light": {
                "--bg-body": "transparent",
                "--bg-app": "#f7f8f9",
                "--bg-surface": "#ffffff",
                "--bg-color": "#ffffff",
                "--sidebar-bg": "#ffffff",
                "--border-color": "rgba(0,0,0,0.08)",
                "--text-primary": "#1a1c1e",
                "--text-secondary": "#5e6368",
                "--accent-blue": "#3275F5",
                "--divider": "rgba(0,0,0,0.08)",
                "--card-bg": "rgba(0,0,0,0.03)",
                "--card-border": "rgba(0,0,0,0.02)",
                "--item-hover": "rgba(0,0,0,0.05)",
                "--shadow-color": "rgba(0,0,0,0.06)",
                "--shadow-main": "0 4px 12px var(--shadow-color)",
                "--logo-glow": "rgba(50,117,245,0.1)",
                "--hyperos-g1": "#e0f2fe",
                "--hyperos-g2": "#fef9c3",
                "--hyperos-g3": "#fce7f3",
                "--hyperos-g4": "#d1fae5",
                "--hyperos-g5": "#ffffff",
                "--font-stack": '"Google Sans Flex","MiSans VF","Segoe UI",system-ui,-apple-system,sans-serif',
                "--font-weight-override": "400",
                "--overlay-toolbar-bg": "#FFFFFF",
                "--overlay-toolbar-border": "rgba(0,0,0,0.08)",
                "--overlay-toolbar-line": "rgba(0,0,0,0.08)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.15)",
                "--overlay-toolbar-fg": "#191919",
                "--overlay-button-hover": "rgba(0,0,0,0.06)",
                "--overlay-button-active": "rgba(0,0,0,0.12)",
                "--overlay-status-bg": "rgba(0,0,0,0.25)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.3)",
                "--overlay-popup-bg": "#FFFFFF",
                "--overlay-popup-border": "rgba(0,0,0,0.12)",
                "--overlay-popup-fg": "#191919",
                "--overlay-page-bg": "#FFFFFF",
                "--overlay-page-border": "rgba(0,0,0,0.08)",
                "--overlay-page-fg": "#191919",
                "--overlay-page-hint": "rgba(0,0,0,0.5)",
                "--overlay-page-hover": "rgba(0,0,0,0.05)",
                "--overlay-page-shadow": "rgba(0,0,0,0.15)",
                "--overlay-reload-mask": "rgba(0,0,0,0.43)",
                "--overlay-reload-card": "rgba(30,30,30,0.86)",
                "--overlay-reload-text": "rgba(255,255,255,0.92)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#FFFFFF",
                "--ring-bg": "rgba(0,0,0,0.05)",
                "--dialog-overlay": "rgba(0,0,0,0.4)",
                "--dialog-icon-bg": "#F2F7FF",
            },
            "dark": {
                "--bg-app": "#151515",
                "--bg-surface": "#1E1E1E",
                "--bg-color": "#1E1E1E",
                "--sidebar-bg": "#1E1E1E",
                "--border-color": "rgba(255,255,255,0.08)",
                "--text-primary": "#E5E5E5",
                "--text-secondary": "#909090",
                "--accent-blue": "#4A85F6",
                "--divider": "rgba(255,255,255,0.08)",
                "--card-bg": "rgba(255,255,255,0.03)",
                "--card-border": "rgba(255,255,255,0.02)",
                "--item-hover": "rgba(255,255,255,0.05)",
                "--shadow-color": "rgba(0,0,0,0.3)",
                "--shadow-main": "0 8px 32px var(--shadow-color)",
                "--logo-glow": "rgba(255,255,255,0.15)",
                "--hyperos-g1": "#1e293b",
                "--hyperos-g2": "#334155",
                "--hyperos-g3": "#1e1b4b",
                "--hyperos-g4": "#064e3b",
                "--hyperos-g5": "#0f172a",
                "--overlay-toolbar-bg": "#202020",
                "--overlay-toolbar-border": "rgba(255,255,255,0.08)",
                "--overlay-toolbar-line": "rgba(255,255,255,0.15)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.31)",
                "--overlay-toolbar-fg": "#FFFFFF",
                "--overlay-button-hover": "rgba(255,255,255,0.08)",
                "--overlay-button-active": "rgba(255,255,255,0.15)",
                "--overlay-status-bg": "rgba(0,0,0,0.25)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.3)",
                "--overlay-popup-bg": "#202020",
                "--overlay-popup-border": "rgba(255,255,255,0.18)",
                "--overlay-popup-fg": "#FFFFFF",
                "--overlay-page-bg": "#202020",
                "--overlay-page-border": "rgba(255,255,255,0.08)",
                "--overlay-page-fg": "#FFFFFF",
                "--overlay-page-hint": "rgba(255,255,255,0.6)",
                "--overlay-page-hover": "rgba(255,255,255,0.08)",
                "--overlay-page-shadow": "rgba(0,0,0,0.31)",
                "--overlay-reload-mask": "rgba(0,0,0,0.43)",
                "--overlay-reload-card": "rgba(30,30,30,0.86)",
                "--overlay-reload-text": "rgba(255,255,255,0.92)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#FFFFFF",
                "--ring-bg": "rgba(255,255,255,0.1)",
                "--dialog-overlay": "rgba(0,0,0,0.6)",
                "--dialog-icon-bg": "rgba(255,255,255,0.05)",
            },
        },
        "material-you": {
            "light": {
                "--bg-app": "#F6F0FF",
                "--bg-surface": "#FFF7FF",
                "--bg-color": "#FFF7FF",
                "--sidebar-bg": "#FFF7FF",
                "--border-color": "rgba(42,26,61,0.1)",
                "--text-primary": "#2B153E",
                "--text-secondary": "#5F4B74",
                "--accent-blue": "#7A3BDB",
                "--divider": "rgba(42,26,61,0.1)",
                "--card-bg": "rgba(122,59,219,0.1)",
                "--card-border": "rgba(122,59,219,0.2)",
                "--item-hover": "rgba(122,59,219,0.14)",
                "--shadow-color": "rgba(122,59,219,0.18)",
                "--shadow-main": "0 6px 22px var(--shadow-color)",
                "--logo-glow": "rgba(122,59,219,0.24)",
                "--hyperos-g1": "#F6F0FF",
                "--hyperos-g2": "#EAD9FF",
                "--hyperos-g3": "#F1E6FF",
                "--hyperos-g4": "#C9A7FF",
                "--hyperos-g5": "#FFF7FF",
                "--overlay-toolbar-bg": "#FFF7FF",
                "--overlay-toolbar-border": "rgba(42,26,61,0.1)",
                "--overlay-toolbar-line": "rgba(42,26,61,0.1)",
                "--overlay-toolbar-shadow": "rgba(42,26,61,0.2)",
                "--overlay-toolbar-fg": "#2B153E",
                "--overlay-button-hover": "rgba(122,59,219,0.14)",
                "--overlay-button-active": "rgba(122,59,219,0.22)",
                "--overlay-status-bg": "rgba(35,20,50,0.28)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#FFF7FF",
                "--overlay-popup-border": "rgba(42,26,61,0.14)",
                "--overlay-popup-fg": "#2B153E",
                "--overlay-page-bg": "#FFF7FF",
                "--overlay-page-border": "rgba(42,26,61,0.14)",
                "--overlay-page-fg": "#2B153E",
                "--overlay-page-hint": "rgba(43,21,62,0.55)",
                "--overlay-page-hover": "rgba(122,59,219,0.14)",
                "--overlay-page-shadow": "rgba(42,26,61,0.2)",
                "--overlay-reload-mask": "rgba(36,22,54,0.48)",
                "--overlay-reload-card": "rgba(52,32,78,0.9)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--ring-bg": "rgba(122,59,219,0.16)",
                "--dialog-overlay": "rgba(36,22,54,0.42)",
                "--dialog-icon-bg": "rgba(122,59,219,0.16)",
            },
            "dark": {
                "--bg-app": "#1C1329",
                "--bg-surface": "#2A1D3B",
                "--bg-color": "#2A1D3B",
                "--sidebar-bg": "#2A1D3B",
                "--border-color": "rgba(226,210,255,0.14)",
                "--text-primary": "#F0E7FF",
                "--text-secondary": "#D2C2EA",
                "--accent-blue": "#CDA7FF",
                "--divider": "rgba(226,210,255,0.14)",
                "--card-bg": "rgba(205,167,255,0.18)",
                "--card-border": "rgba(205,167,255,0.28)",
                "--item-hover": "rgba(205,167,255,0.22)",
                "--shadow-color": "rgba(0,0,0,0.5)",
                "--shadow-main": "0 12px 34px var(--shadow-color)",
                "--logo-glow": "rgba(205,167,255,0.28)",
                "--hyperos-g1": "#1C1329",
                "--hyperos-g2": "#2A1D3B",
                "--hyperos-g3": "#3B2A55",
                "--hyperos-g4": "#4A366A",
                "--hyperos-g5": "#2A1D3B",
                "--overlay-toolbar-bg": "#2A1D3B",
                "--overlay-toolbar-border": "rgba(226,210,255,0.16)",
                "--overlay-toolbar-line": "rgba(226,210,255,0.2)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.38)",
                "--overlay-toolbar-fg": "#F0E7FF",
                "--overlay-button-hover": "rgba(205,167,255,0.22)",
                "--overlay-button-active": "rgba(205,167,255,0.3)",
                "--overlay-status-bg": "rgba(0,0,0,0.3)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#2A1D3B",
                "--overlay-popup-border": "rgba(226,210,255,0.24)",
                "--overlay-popup-fg": "#F0E7FF",
                "--overlay-page-bg": "#2A1D3B",
                "--overlay-page-border": "rgba(226,210,255,0.18)",
                "--overlay-page-fg": "#F0E7FF",
                "--overlay-page-hint": "rgba(226,210,255,0.68)",
                "--overlay-page-hover": "rgba(205,167,255,0.22)",
                "--overlay-page-shadow": "rgba(0,0,0,0.38)",
                "--overlay-reload-mask": "rgba(0,0,0,0.48)",
                "--overlay-reload-card": "rgba(49,31,74,0.92)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#1C1329",
                "--ring-bg": "rgba(205,167,255,0.2)",
                "--dialog-overlay": "rgba(0,0,0,0.62)",
                "--dialog-icon-bg": "rgba(205,167,255,0.2)",
            },
        },
        "red-sunrise": {
            "light": {
                "--bg-app": "#FFF5F3",
                "--bg-surface": "#FFFFFF",
                "--bg-color": "#FFFFFF",
                "--sidebar-bg": "#FFFFFF",
                "--border-color": "rgba(97,20,18,0.12)",
                "--text-primary": "#3A0B0B",
                "--text-secondary": "#7A3C35",
                "--accent-blue": "#E5523C",
                "--divider": "rgba(97,20,18,0.12)",
                "--card-bg": "rgba(229,82,60,0.1)",
                "--card-border": "rgba(229,82,60,0.2)",
                "--item-hover": "rgba(229,82,60,0.14)",
                "--shadow-color": "rgba(229,82,60,0.18)",
                "--shadow-main": "0 6px 22px var(--shadow-color)",
                "--logo-glow": "rgba(229,82,60,0.22)",
                "--hyperos-g1": "#FFF1EC",
                "--hyperos-g2": "#FFD6C8",
                "--hyperos-g3": "#FFE7DB",
                "--hyperos-g4": "#FFB8A4",
                "--hyperos-g5": "#FFFFFF",
                "--overlay-toolbar-bg": "#FFF7F4",
                "--overlay-toolbar-border": "rgba(97,20,18,0.12)",
                "--overlay-toolbar-line": "rgba(97,20,18,0.12)",
                "--overlay-toolbar-shadow": "rgba(97,20,18,0.2)",
                "--overlay-toolbar-fg": "#3A0B0B",
                "--overlay-button-hover": "rgba(229,82,60,0.14)",
                "--overlay-button-active": "rgba(229,82,60,0.22)",
                "--overlay-status-bg": "rgba(56,18,12,0.28)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#FFF7F4",
                "--overlay-popup-border": "rgba(97,20,18,0.14)",
                "--overlay-popup-fg": "#3A0B0B",
                "--overlay-page-bg": "#FFF7F4",
                "--overlay-page-border": "rgba(97,20,18,0.14)",
                "--overlay-page-fg": "#3A0B0B",
                "--overlay-page-hint": "rgba(58,11,11,0.55)",
                "--overlay-page-hover": "rgba(229,82,60,0.14)",
                "--overlay-page-shadow": "rgba(97,20,18,0.2)",
                "--overlay-reload-mask": "rgba(54,18,12,0.48)",
                "--overlay-reload-card": "rgba(64,26,20,0.9)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--ring-bg": "rgba(229,82,60,0.18)",
                "--dialog-overlay": "rgba(56,18,12,0.42)",
                "--dialog-icon-bg": "rgba(229,82,60,0.16)",
            },
            "dark": {
                "--bg-app": "#1A0B0A",
                "--bg-surface": "#2B1512",
                "--bg-color": "#2B1512",
                "--sidebar-bg": "#2B1512",
                "--border-color": "rgba(255,210,200,0.14)",
                "--text-primary": "#FFEDE8",
                "--text-secondary": "#F1BEB3",
                "--accent-blue": "#FF907B",
                "--divider": "rgba(255,210,200,0.14)",
                "--card-bg": "rgba(255,144,123,0.18)",
                "--card-border": "rgba(255,144,123,0.28)",
                "--item-hover": "rgba(255,144,123,0.22)",
                "--shadow-color": "rgba(0,0,0,0.5)",
                "--shadow-main": "0 12px 34px var(--shadow-color)",
                "--logo-glow": "rgba(255,144,123,0.28)",
                "--hyperos-g1": "#1A0B0A",
                "--hyperos-g2": "#2B1512",
                "--hyperos-g3": "#3C1D18",
                "--hyperos-g4": "#4D241E",
                "--hyperos-g5": "#2B1512",
                "--overlay-toolbar-bg": "#2B1512",
                "--overlay-toolbar-border": "rgba(255,210,200,0.18)",
                "--overlay-toolbar-line": "rgba(255,210,200,0.22)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.38)",
                "--overlay-toolbar-fg": "#FFEDE8",
                "--overlay-button-hover": "rgba(255,144,123,0.22)",
                "--overlay-button-active": "rgba(255,144,123,0.3)",
                "--overlay-status-bg": "rgba(0,0,0,0.3)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#2B1512",
                "--overlay-popup-border": "rgba(255,210,200,0.24)",
                "--overlay-popup-fg": "#FFEDE8",
                "--overlay-page-bg": "#2B1512",
                "--overlay-page-border": "rgba(255,210,200,0.2)",
                "--overlay-page-fg": "#FFEDE8",
                "--overlay-page-hint": "rgba(255,210,200,0.68)",
                "--overlay-page-hover": "rgba(255,144,123,0.22)",
                "--overlay-page-shadow": "rgba(0,0,0,0.38)",
                "--overlay-reload-mask": "rgba(0,0,0,0.48)",
                "--overlay-reload-card": "rgba(51,22,18,0.92)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#1A0B0A",
                "--ring-bg": "rgba(255,144,123,0.22)",
                "--dialog-overlay": "rgba(0,0,0,0.62)",
                "--dialog-icon-bg": "rgba(255,144,123,0.2)",
            },
        },
        "emptiness-color": {
            "light": {
                "--bg-app": "#F5F6F8",
                "--bg-surface": "#FFFFFF",
                "--bg-color": "#FFFFFF",
                "--sidebar-bg": "#FFFFFF",
                "--border-color": "rgba(27,31,35,0.08)",
                "--text-primary": "#1B1F23",
                "--text-secondary": "#5B636B",
                "--accent-blue": "#6B7280",
                "--divider": "rgba(27,31,35,0.08)",
                "--card-bg": "rgba(27,31,35,0.03)",
                "--card-border": "rgba(27,31,35,0.06)",
                "--item-hover": "rgba(27,31,35,0.06)",
                "--shadow-color": "rgba(27,31,35,0.08)",
                "--shadow-main": "0 4px 12px var(--shadow-color)",
                "--logo-glow": "rgba(27,31,35,0.08)",
                "--hyperos-g1": "#F3F4F6",
                "--hyperos-g2": "#E5E7EB",
                "--hyperos-g3": "#F9FAFB",
                "--hyperos-g4": "#D1D5DB",
                "--hyperos-g5": "#FFFFFF",
                "--overlay-toolbar-bg": "#FFFFFF",
                "--overlay-toolbar-border": "rgba(27,31,35,0.08)",
                "--overlay-toolbar-line": "rgba(27,31,35,0.08)",
                "--overlay-toolbar-shadow": "rgba(27,31,35,0.16)",
                "--overlay-toolbar-fg": "#1B1F23",
                "--overlay-button-hover": "rgba(27,31,35,0.06)",
                "--overlay-button-active": "rgba(27,31,35,0.12)",
                "--overlay-status-bg": "rgba(27,31,35,0.26)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#FFFFFF",
                "--overlay-popup-border": "rgba(27,31,35,0.12)",
                "--overlay-popup-fg": "#1B1F23",
                "--overlay-page-bg": "#FFFFFF",
                "--overlay-page-border": "rgba(27,31,35,0.12)",
                "--overlay-page-fg": "#1B1F23",
                "--overlay-page-hint": "rgba(27,31,35,0.5)",
                "--overlay-page-hover": "rgba(27,31,35,0.06)",
                "--overlay-page-shadow": "rgba(27,31,35,0.16)",
                "--overlay-reload-mask": "rgba(27,31,35,0.43)",
                "--overlay-reload-card": "rgba(30,30,30,0.86)",
                "--overlay-reload-text": "rgba(255,255,255,0.92)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--ring-bg": "rgba(27,31,35,0.08)",
                "--dialog-overlay": "rgba(27,31,35,0.38)",
                "--dialog-icon-bg": "rgba(27,31,35,0.12)",
            },
            "dark": {
                "--bg-app": "#101113",
                "--bg-surface": "#1B1C1F",
                "--bg-color": "#1B1C1F",
                "--sidebar-bg": "#1B1C1F",
                "--border-color": "rgba(230,233,238,0.12)",
                "--text-primary": "#F1F3F5",
                "--text-secondary": "#C3C7CC",
                "--accent-blue": "#A1A6AD",
                "--divider": "rgba(230,233,238,0.12)",
                "--card-bg": "rgba(161,166,173,0.14)",
                "--card-border": "rgba(161,166,173,0.22)",
                "--item-hover": "rgba(161,166,173,0.2)",
                "--shadow-color": "rgba(0,0,0,0.4)",
                "--shadow-main": "0 10px 30px var(--shadow-color)",
                "--logo-glow": "rgba(161,166,173,0.2)",
                "--hyperos-g1": "#101113",
                "--hyperos-g2": "#1B1C1F",
                "--hyperos-g3": "#23252A",
                "--hyperos-g4": "#2C2F36",
                "--hyperos-g5": "#1B1C1F",
                "--overlay-toolbar-bg": "#1B1C1F",
                "--overlay-toolbar-border": "rgba(230,233,238,0.16)",
                "--overlay-toolbar-line": "rgba(230,233,238,0.2)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.32)",
                "--overlay-toolbar-fg": "#F1F3F5",
                "--overlay-button-hover": "rgba(161,166,173,0.2)",
                "--overlay-button-active": "rgba(161,166,173,0.28)",
                "--overlay-status-bg": "rgba(0,0,0,0.28)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#1B1C1F",
                "--overlay-popup-border": "rgba(230,233,238,0.2)",
                "--overlay-popup-fg": "#F1F3F5",
                "--overlay-page-bg": "#1B1C1F",
                "--overlay-page-border": "rgba(230,233,238,0.18)",
                "--overlay-page-fg": "#F1F3F5",
                "--overlay-page-hint": "rgba(230,233,238,0.62)",
                "--overlay-page-hover": "rgba(161,166,173,0.2)",
                "--overlay-page-shadow": "rgba(0,0,0,0.32)",
                "--overlay-reload-mask": "rgba(0,0,0,0.46)",
                "--overlay-reload-card": "rgba(26,27,30,0.9)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#101113",
                "--ring-bg": "rgba(161,166,173,0.2)",
                "--dialog-overlay": "rgba(0,0,0,0.6)",
                "--dialog-icon-bg": "rgba(161,166,173,0.2)",
            },
        },
        "mung-bean": {
            "light": {
                "--bg-app": "#F3FFF7",
                "--bg-surface": "#FFFFFF",
                "--bg-color": "#FFFFFF",
                "--sidebar-bg": "#FFFFFF",
                "--border-color": "rgba(18,63,44,0.1)",
                "--text-primary": "#0F3D2A",
                "--text-secondary": "#3F6B57",
                "--accent-blue": "#45B97C",
                "--divider": "rgba(18,63,44,0.1)",
                "--card-bg": "rgba(69,185,124,0.1)",
                "--card-border": "rgba(69,185,124,0.2)",
                "--item-hover": "rgba(69,185,124,0.14)",
                "--shadow-color": "rgba(69,185,124,0.18)",
                "--shadow-main": "0 6px 22px var(--shadow-color)",
                "--logo-glow": "rgba(69,185,124,0.22)",
                "--hyperos-g1": "#ECFFF4",
                "--hyperos-g2": "#CFF5DF",
                "--hyperos-g3": "#E4FFF0",
                "--hyperos-g4": "#B5EBCB",
                "--hyperos-g5": "#FFFFFF",
                "--overlay-toolbar-bg": "#FFFFFF",
                "--overlay-toolbar-border": "rgba(18,63,44,0.1)",
                "--overlay-toolbar-line": "rgba(18,63,44,0.1)",
                "--overlay-toolbar-shadow": "rgba(18,63,44,0.2)",
                "--overlay-toolbar-fg": "#0F3D2A",
                "--overlay-button-hover": "rgba(69,185,124,0.14)",
                "--overlay-button-active": "rgba(69,185,124,0.22)",
                "--overlay-status-bg": "rgba(16,50,35,0.28)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#FFFFFF",
                "--overlay-popup-border": "rgba(18,63,44,0.14)",
                "--overlay-popup-fg": "#0F3D2A",
                "--overlay-page-bg": "#FFFFFF",
                "--overlay-page-border": "rgba(18,63,44,0.14)",
                "--overlay-page-fg": "#0F3D2A",
                "--overlay-page-hint": "rgba(15,61,42,0.52)",
                "--overlay-page-hover": "rgba(69,185,124,0.14)",
                "--overlay-page-shadow": "rgba(18,63,44,0.2)",
                "--overlay-reload-mask": "rgba(16,50,35,0.48)",
                "--overlay-reload-card": "rgba(28,58,45,0.9)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--ring-bg": "rgba(69,185,124,0.18)",
                "--dialog-overlay": "rgba(16,50,35,0.42)",
                "--dialog-icon-bg": "rgba(69,185,124,0.16)",
            },
            "dark": {
                "--bg-app": "#0D1A14",
                "--bg-surface": "#16231C",
                "--bg-color": "#16231C",
                "--sidebar-bg": "#16231C",
                "--border-color": "rgba(199,240,219,0.14)",
                "--text-primary": "#E7FFF1",
                "--text-secondary": "#B7E6CD",
                "--accent-blue": "#7FE3B1",
                "--divider": "rgba(199,240,219,0.14)",
                "--card-bg": "rgba(127,227,177,0.18)",
                "--card-border": "rgba(127,227,177,0.28)",
                "--item-hover": "rgba(127,227,177,0.22)",
                "--shadow-color": "rgba(0,0,0,0.5)",
                "--shadow-main": "0 12px 34px var(--shadow-color)",
                "--logo-glow": "rgba(127,227,177,0.26)",
                "--hyperos-g1": "#0D1A14",
                "--hyperos-g2": "#16231C",
                "--hyperos-g3": "#1C2E25",
                "--hyperos-g4": "#24392F",
                "--hyperos-g5": "#16231C",
                "--overlay-toolbar-bg": "#16231C",
                "--overlay-toolbar-border": "rgba(199,240,219,0.18)",
                "--overlay-toolbar-line": "rgba(199,240,219,0.22)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.36)",
                "--overlay-toolbar-fg": "#E7FFF1",
                "--overlay-button-hover": "rgba(127,227,177,0.22)",
                "--overlay-button-active": "rgba(127,227,177,0.3)",
                "--overlay-status-bg": "rgba(0,0,0,0.3)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#16231C",
                "--overlay-popup-border": "rgba(199,240,219,0.24)",
                "--overlay-popup-fg": "#E7FFF1",
                "--overlay-page-bg": "#16231C",
                "--overlay-page-border": "rgba(199,240,219,0.2)",
                "--overlay-page-fg": "#E7FFF1",
                "--overlay-page-hint": "rgba(199,240,219,0.68)",
                "--overlay-page-hover": "rgba(127,227,177,0.22)",
                "--overlay-page-shadow": "rgba(0,0,0,0.36)",
                "--overlay-reload-mask": "rgba(0,0,0,0.48)",
                "--overlay-reload-card": "rgba(21,40,32,0.92)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#0D1A14",
                "--ring-bg": "rgba(127,227,177,0.22)",
                "--dialog-overlay": "rgba(0,0,0,0.62)",
                "--dialog-icon-bg": "rgba(127,227,177,0.2)",
            },
        },
        "orange-wish": {
            "light": {
                "--bg-app": "#FFF6EE",
                "--bg-surface": "#FFFFFF",
                "--bg-color": "#FFFFFF",
                "--sidebar-bg": "#FFFFFF",
                "--border-color": "rgba(80,40,12,0.12)",
                "--text-primary": "#4A2A11",
                "--text-secondary": "#7B5A3E",
                "--accent-blue": "#FF8A3D",
                "--divider": "rgba(80,40,12,0.12)",
                "--card-bg": "rgba(255,138,61,0.1)",
                "--card-border": "rgba(255,138,61,0.2)",
                "--item-hover": "rgba(255,138,61,0.14)",
                "--shadow-color": "rgba(255,138,61,0.18)",
                "--shadow-main": "0 6px 22px var(--shadow-color)",
                "--logo-glow": "rgba(255,138,61,0.24)",
                "--hyperos-g1": "#FFF1E4",
                "--hyperos-g2": "#FFD9BF",
                "--hyperos-g3": "#FFEBDD",
                "--hyperos-g4": "#FFC69B",
                "--hyperos-g5": "#FFFFFF",
                "--overlay-toolbar-bg": "#FFFFFF",
                "--overlay-toolbar-border": "rgba(80,40,12,0.12)",
                "--overlay-toolbar-line": "rgba(80,40,12,0.12)",
                "--overlay-toolbar-shadow": "rgba(80,40,12,0.2)",
                "--overlay-toolbar-fg": "#4A2A11",
                "--overlay-button-hover": "rgba(255,138,61,0.14)",
                "--overlay-button-active": "rgba(255,138,61,0.22)",
                "--overlay-status-bg": "rgba(62,32,12,0.28)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#FFFFFF",
                "--overlay-popup-border": "rgba(80,40,12,0.14)",
                "--overlay-popup-fg": "#4A2A11",
                "--overlay-page-bg": "#FFFFFF",
                "--overlay-page-border": "rgba(80,40,12,0.14)",
                "--overlay-page-fg": "#4A2A11",
                "--overlay-page-hint": "rgba(74,42,17,0.52)",
                "--overlay-page-hover": "rgba(255,138,61,0.14)",
                "--overlay-page-shadow": "rgba(80,40,12,0.2)",
                "--overlay-reload-mask": "rgba(62,32,12,0.48)",
                "--overlay-reload-card": "rgba(70,40,20,0.9)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--ring-bg": "rgba(255,138,61,0.18)",
                "--dialog-overlay": "rgba(62,32,12,0.42)",
                "--dialog-icon-bg": "rgba(255,138,61,0.16)",
            },
            "dark": {
                "--bg-app": "#1C1108",
                "--bg-surface": "#2A190D",
                "--bg-color": "#2A190D",
                "--sidebar-bg": "#2A190D",
                "--border-color": "rgba(255,220,195,0.16)",
                "--text-primary": "#FFEFE3",
                "--text-secondary": "#F2C8A6",
                "--accent-blue": "#FFB677",
                "--divider": "rgba(255,220,195,0.16)",
                "--card-bg": "rgba(255,182,119,0.2)",
                "--card-border": "rgba(255,182,119,0.3)",
                "--item-hover": "rgba(255,182,119,0.24)",
                "--shadow-color": "rgba(0,0,0,0.5)",
                "--shadow-main": "0 12px 34px var(--shadow-color)",
                "--logo-glow": "rgba(255,182,119,0.28)",
                "--hyperos-g1": "#1C1108",
                "--hyperos-g2": "#2A190D",
                "--hyperos-g3": "#3A2314",
                "--hyperos-g4": "#4A2C19",
                "--hyperos-g5": "#2A190D",
                "--overlay-toolbar-bg": "#2A190D",
                "--overlay-toolbar-border": "rgba(255,220,195,0.2)",
                "--overlay-toolbar-line": "rgba(255,220,195,0.24)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.36)",
                "--overlay-toolbar-fg": "#FFEFE3",
                "--overlay-button-hover": "rgba(255,182,119,0.24)",
                "--overlay-button-active": "rgba(255,182,119,0.32)",
                "--overlay-status-bg": "rgba(0,0,0,0.3)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#2A190D",
                "--overlay-popup-border": "rgba(255,220,195,0.26)",
                "--overlay-popup-fg": "#FFEFE3",
                "--overlay-page-bg": "#2A190D",
                "--overlay-page-border": "rgba(255,220,195,0.22)",
                "--overlay-page-fg": "#FFEFE3",
                "--overlay-page-hint": "rgba(255,220,195,0.7)",
                "--overlay-page-hover": "rgba(255,182,119,0.24)",
                "--overlay-page-shadow": "rgba(0,0,0,0.36)",
                "--overlay-reload-mask": "rgba(0,0,0,0.48)",
                "--overlay-reload-card": "rgba(46,28,16,0.92)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#1C1108",
                "--ring-bg": "rgba(255,182,119,0.24)",
                "--dialog-overlay": "rgba(0,0,0,0.62)",
                "--dialog-icon-bg": "rgba(255,182,119,0.22)",
            },
        },
        "year-of-horse": {
            "light": {
                "--bg-app": "#FFF0F0",
                "--bg-surface": "#FFFFFF",
                "--bg-color": "#FFFFFF",
                "--sidebar-bg": "#FFFFFF",
                "--border-color": "rgba(230,0,0,0.1)",
                "--text-primary": "#990000",
                "--text-secondary": "#CC3333",
                "--accent-blue": "#E60000",
                "--divider": "rgba(230,0,0,0.1)",
                "--card-bg": "rgba(230,0,0,0.08)",
                "--card-border": "rgba(230,0,0,0.15)",
                "--item-hover": "rgba(230,0,0,0.12)",
                "--shadow-color": "rgba(200,0,0,0.15)",
                "--shadow-main": "0 6px 22px var(--shadow-color)",
                "--logo-glow": "rgba(230,0,0,0.2)",
                "--hyperos-g1": "#FFF0F0",
                "--hyperos-g2": "#FFE0E0",
                "--hyperos-g3": "#FFCCCC",
                "--hyperos-g4": "#FF9999",
                "--hyperos-g5": "#FFFFFF",
                "--overlay-toolbar-bg": "#FFF0F0",
                "--overlay-toolbar-border": "rgba(230,0,0,0.1)",
                "--overlay-toolbar-line": "rgba(230,0,0,0.1)",
                "--overlay-toolbar-shadow": "rgba(200,0,0,0.15)",
                "--overlay-toolbar-fg": "#990000",
                "--overlay-button-hover": "rgba(230,0,0,0.12)",
                "--overlay-button-active": "rgba(230,0,0,0.2)",
                "--overlay-status-bg": "rgba(200,0,0,0.3)",
                "--overlay-status-fg": "#FFFFFF",
                "--overlay-status-sep": "rgba(255,255,255,0.32)",
                "--overlay-popup-bg": "#FFF0F0",
                "--overlay-popup-border": "rgba(230,0,0,0.12)",
                "--overlay-popup-fg": "#990000",
                "--overlay-page-bg": "#FFF0F0",
                "--overlay-page-border": "rgba(230,0,0,0.12)",
                "--overlay-page-fg": "#990000",
                "--overlay-page-hint": "rgba(200,0,0,0.5)",
                "--overlay-page-hover": "rgba(230,0,0,0.12)",
                "--overlay-page-shadow": "rgba(200,0,0,0.15)",
                "--overlay-reload-mask": "rgba(80,10,10,0.48)",
                "--overlay-reload-card": "rgba(200,0,0,0.9)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(200,0,0,0.47)",
                "--ring-bg": "rgba(211,47,47,0.15)",
                "--dialog-overlay": "rgba(50,10,10,0.6)",
                "--dialog-icon-bg": "rgba(211,47,47,0.15)",
            },
            "dark": {
                "--bg-app": "#2A0505",
                "--bg-surface": "#451212",
                "--bg-color": "#451212",
                "--sidebar-bg": "#451212",
                "--border-color": "rgba(255,69,0,0.3)",
                "--text-primary": "#FFD700",
                "--text-secondary": "#FFB300",
                "--accent-blue": "#FF4500",
                "--divider": "rgba(255,69,0,0.3)",
                "--card-bg": "rgba(255,69,0,0.15)",
                "--card-border": "rgba(255,69,0,0.25)",
                "--item-hover": "rgba(255,69,0,0.22)",
                "--shadow-color": "rgba(0,0,0,0.5)",
                "--shadow-main": "0 12px 34px var(--shadow-color)",
                "--logo-glow": "rgba(255,69,0,0.28)",
                "--hyperos-g1": "#2A0505",
                "--hyperos-g2": "#451212",
                "--hyperos-g3": "#5E1A1A",
                "--hyperos-g4": "#7A2222",
                "--hyperos-g5": "#451212",
                "--overlay-toolbar-bg": "#451212",
                "--overlay-toolbar-border": "rgba(255,69,0,0.3)",
                "--overlay-toolbar-line": "rgba(255,69,0,0.3)",
                "--overlay-toolbar-shadow": "rgba(0,0,0,0.4)",
                "--overlay-toolbar-fg": "#FFD700",
                "--overlay-button-hover": "rgba(255,69,0,0.22)",
                "--overlay-button-active": "rgba(255,69,0,0.3)",
                "--overlay-status-bg": "rgba(255,213,79,0.8)",
                "--overlay-status-fg": "#3E2723",
                "--overlay-status-sep": "rgba(62,39,35,0.32)",
                "--overlay-popup-bg": "#451212",
                "--overlay-popup-border": "rgba(255,69,0,0.3)",
                "--overlay-popup-fg": "#FFD700",
                "--overlay-page-bg": "#451212",
                "--overlay-page-border": "rgba(255,69,0,0.3)",
                "--overlay-page-fg": "#FFD700",
                "--overlay-page-hint": "rgba(255,215,0,0.68)",
                "--overlay-page-hover": "rgba(255,69,0,0.22)",
                "--overlay-page-shadow": "rgba(0,0,0,0.4)",
                "--overlay-reload-mask": "rgba(0,0,0,0.5)",
                "--overlay-reload-card": "rgba(51,22,18,0.92)",
                "--overlay-reload-text": "rgba(255,255,255,0.94)",
                "--overlay-dev-watermark": "rgba(255,255,255,0.47)",
                "--text-inverse": "#2A0A0A",
                "--ring-bg": "rgba(255,69,0,0.22)",
                "--dialog-overlay": "rgba(0,0,0,0.62)",
                "--dialog-icon-bg": "rgba(255,69,0,0.2)",
            },
        },
    }

    theme_json = _json.dumps(themes, ensure_ascii=False, separators=(",", ":"))
    return f"""\n(function(){{var TD={theme_json};function AT(){{var r=document.documentElement;if(!r)return;var s=window.initialSettings||{{}};var a=s.Appearance||{{}};var d=a.ResolvedIsDark;var tid=a.ThemeId||'default';var v=d?'dark':'light';var b=TD['default'].light;for(var k in b)r.style.setProperty(k,b[k]);if(d){{var db=TD['default'].dark;for(var k in db)r.style.setProperty(k,db[k]);}}if(tid!=='default'&&TD[tid]&&TD[tid][v]){{var t=TD[tid][v];for(var k in t)r.style.setProperty(k,t[k]);}}r.style.setProperty('--accent-color','var(--accent-blue)');r.style.setProperty('--overlay-dialog-mask',d?'rgba(0,0,0,0.35)':'rgba(0,0,0,0.2)');r.style.setProperty('--overlay-dialog-shadow',d?'0 16px 40px rgba(0,0,0,0.45)':'0 12px 30px rgba(0,0,0,0.18)');r.style.setProperty('--overlay-dialog-bg','var(--overlay-popup-bg)');r.style.setProperty('--overlay-dialog-border','var(--overlay-popup-border)');r.style.setProperty('--overlay-dialog-title','var(--text-primary)');r.style.setProperty('--overlay-dialog-text','var(--text-secondary)');r.style.setProperty('--overlay-control-bg','var(--card-bg)');r.style.setProperty('--overlay-control-hover','var(--item-hover)');r.style.setProperty('--overlay-control-active','var(--overlay-button-active)');r.style.setProperty('--overlay-text-primary','var(--text-primary)');r.style.setProperty('--overlay-text-secondary','var(--text-secondary)');r.style.setProperty('--overlay-popup-shadow','none');r.style.setProperty('--overlay-thumb-bg',d?'var(--accent-blue)':'#FFFFFF');r.style.setProperty('--overlay-thumb-icon',d?'var(--bg-app)':'var(--accent-blue)');r.setAttribute('data-theme',d?'dark':'light');r.setAttribute('data-theme-variant',v);r.setAttribute('data-theme-id',tid);}}if(document.documentElement){{AT();}}else{{document.addEventListener('DOMContentLoaded',AT);}}window.__applyUnifiedTheme=AT;}})();"""


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
        frameless=False,
        defer_until_show=False,
    ):
        super().__init__()
        self.setPage(QWebEnginePage(_get_shared_profile(), self))
        self.setWindowTitle(title)

        self._frameless = frameless

        if frameless:
            self.setWindowFlags(Qt.FramelessWindowHint | Qt.Window)
            if sys.platform == "win32":
                from ppt_assistant.core.platform_integration import remove_window_border_delayed
                remove_window_border_delayed(self)
        else:
            self.setWindowFlag(Qt.Window, True)
            self.setWindowFlag(Qt.WindowCloseButtonHint, True)
            self.setWindowFlag(Qt.WindowMinMaxButtonsHint, True)

        self.resize(width, height)

        try:
            if "Onboarding" in str(title):
                if not frameless:
                    self.setWindowFlag(Qt.WindowMaximizeButtonHint, False)
            self._center_on_screen()
        except Exception:
            self._center_on_screen()
        self._centered = False
        self._theme_mode = theme_mode
        self._theme_dark_override = None
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
        self._render_crash_count = 0
        self._max_reload_attempts = 3
        self._crash_recovery_timer = None
        self._aero_enabled = False
        self._disable_animations = _animations_disabled(getattr(api, "settings", {}))
        self._memory_timer = None
        self.api = api
        self.api.set_window(self)
        self.windowTitleChanged.connect(self._on_window_title_changed)
        self.winId()
        self._apply_page_background()
        settings = self.page().settings()
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.LocalContentCanAccessRemoteUrls, True
        )
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.LocalContentCanAccessFileUrls, True
        )
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.AllowRunningInsecureContent, True
        )
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.JavascriptCanAccessClipboard, True
        )
        settings.setAttribute(
            QWebEngineSettings.WebAttribute.ScrollAnimatorEnabled,
            not self._disable_animations,
        )
        settings.setAttribute(QWebEngineSettings.WebAttribute.AutoLoadImages, True)
        settings.setAttribute(QWebEngineSettings.WebAttribute.PluginsEnabled, False)
        settings.setAttribute(QWebEngineSettings.WebAttribute.FullScreenSupportEnabled, True)

        try:
            profile = self.page().profile()
            if profile is not None:
                profile.setHttpUserAgent(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36"
                )
        except Exception:
            pass
        _configure_profile(self.page().profile())
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
        appearance = settings_dict["Appearance"]
        mode = appearance.get("ThemeMode", theme_mode)
        appearance["ResolvedIsDark"] = _resolve_theme_dark(mode)

        settings_json = json.dumps(settings_dict, ensure_ascii=False)
        settings_script = QWebEngineScript()
        settings_script.setSourceCode(f"window.initialSettings = {settings_json};")
        settings_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        settings_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(settings_script)
        theme_script = QWebEngineScript()
        theme_script.setSourceCode(_get_unified_theme_js())
        theme_script.setInjectionPoint(
            QWebEngineScript.InjectionPoint.DocumentCreation
        )
        theme_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(theme_script)
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
        self.loadStarted.connect(self._on_load_started)
        self.loadFinished.connect(self._on_load_finished)
        _url_str = str(url).strip()
        if os.path.isabs(_url_str) or (
            not _url_str.startswith(("http://", "https://", "file://", "data:", "about:", "qrc:"))
            and (_url_str.startswith("/") or (_url_str[0:1].isalpha() and len(_url_str) > 1 and _url_str[1:2] == ":"))
        ):
            target_url = QUrl.fromLocalFile(_url_str)
        else:
            target_url = QUrl.fromUserInput(_url_str)
        if self._defer_load:
            self._pending_url = target_url
            if not defer_until_show:
                # Fallback: if showEvent doesn't fire, still kick the initial load
                # to avoid a stuck onboarding window.
                self._pending_load_timer = QTimer(self)
                self._pending_load_timer.setSingleShot(True)
                self._pending_load_timer.timeout.connect(self._ensure_pending_load)
                self._pending_load_timer.start(200)
        else:
            self.load(target_url)
        self.loadFinished.connect(
            lambda *_: (
                self._apply_page_background(),
                self._apply_backdrop(),
                QTimer.singleShot(300, self._force_refresh),
                self._inject_title_bar_html() if self._frameless else None,
                self._inject_title_bar_js() if self._frameless else None,
                self._push_maximized_state() if self._frameless else None,
            )
        )
        self._apply_backdrop()

        self.renderProcessTerminated.connect(self._on_render_process_terminated)

    def _setup_memory_timer(self):
        try:
            if self._memory_timer is not None:
                return
            import gc
            self._memory_timer = QTimer(self)
            self._memory_timer.setInterval(60000)
            self._memory_timer.timeout.connect(self._on_memory_tick)
            self._memory_timer.start()
        except Exception as e:
            print(f"[WV] Failed to setup memory timer: {e}")

    def _stop_memory_timer(self):
        try:
            if self._memory_timer is not None:
                self._memory_timer.stop()
                self._memory_timer.deleteLater()
                self._memory_timer = None
        except Exception:
            pass

    def _on_memory_tick(self):
        try:
            import gc

            gc.collect(1)

            page = self.page()
            if page is not None:
                try:
                    page.clearMemoryCaches()
                except Exception:
                    pass
                profile = page.profile()
                if profile is not None:
                    try:
                        profile.clearHttpCache()
                        profile.clearAllVisitedLinks()
                    except Exception:
                        pass

            if sys.platform == "win32":
                pass
        except Exception:
            pass

    def _ensure_pending_load(self):
        if self._pending_url is None:
            return
        try:
            self.load(self._pending_url)
        finally:
            self._pending_url = None

    def _on_load_started(self):
        pass

    def _on_load_finished(self, _ok):
        self._render_crash_count = 0
        QTimer.singleShot(500, self._on_memory_tick)

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

    def _effective_is_dark(self):
        override = getattr(self, "_theme_dark_override", None)
        if override is not None:
            return bool(override)
        return _resolve_theme_dark(self._theme_mode)

    def _apply_page_background(self):
        if self._mini_mode:
            self.page().setBackgroundColor(Qt.transparent)
            return

        if self._frameless:
            try:
                self.setAutoFillBackground(True)
                palette = self.palette()
                is_dark = self._effective_is_dark()
                bg_color = QColor(24, 24, 24) if is_dark else QColor(255, 255, 255)
                palette.setColor(self.backgroundRole(), bg_color)
                self.setPalette(palette)
            except Exception:
                pass
            _safe_set_widget_attr(self, getattr(Qt, "WA_OpaquePaintEvent", None), True)
            _safe_set_widget_attr(self, Qt.WA_TranslucentBackground, False)
            _safe_set_widget_attr(self, getattr(Qt, "WA_NoSystemBackground", None), False)
            is_dark = self._effective_is_dark()
            if is_dark:
                self.page().setBackgroundColor(QColor(24, 24, 24))
            else:
                self.page().setBackgroundColor(QColor(255, 255, 255))
            return

        settings = {}
        api = getattr(self, "api", None)
        if api is not None:
            settings = getattr(api, "settings", {}) or {}

        try:
            self.setAutoFillBackground(True)
            palette = self.palette()
            is_dark = self._effective_is_dark()
            bg_color = QColor(24, 24, 24) if is_dark else QColor(255, 255, 255)
            palette.setColor(self.backgroundRole(), bg_color)
            self.setPalette(palette)
        except Exception:
            pass
        _safe_set_widget_attr(self, getattr(Qt, "WA_OpaquePaintEvent", None), True)
        _safe_set_widget_attr(self, Qt.WA_TranslucentBackground, False)
        _safe_set_widget_attr(self, getattr(Qt, "WA_NoSystemBackground", None), False)
        is_dark = self._effective_is_dark()
        if is_dark:
            self.page().setBackgroundColor(QColor(24, 24, 24))
        else:
            self.page().setBackgroundColor(QColor(255, 255, 255))

    def _inject_custom_border(self):
        if self._frameless:
            css = """\
html, body {
    height: 100%;
    margin: 0;
    padding: 0;
}
body {
    box-sizing: border-box;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    align-items: stretch;
    justify-content: flex-start;
}
.title-bar {
    display: none;
}
:root[data-frameless="true"] .title-bar {
    display: flex;
    align-items: center;
    height: 32px;
    background: var(--bg-app);
    user-select: none;
    flex-shrink: 0;
    position: relative;
    z-index: 100;
}
:root[data-frameless="true"][data-maximized="true"] .title-bar {
    height: 40px;
    margin: -7px -7px 0 -7px;
    padding-right: 7px;
}
:root[data-frameless="true"][data-maximized="true"] body {
    padding: 7px 7px 7px 7px;
}
.title-bar-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    margin-left: 12px;
    margin-right: 8px;
    color: var(--text-primary);
    opacity: 0.7;
    z-index: 1;
    cursor: pointer;
    position: relative;
    -webkit-app-region: no-drag;
}
.title-bar-icon img {
    width: 16px;
    height: 16px;
}
.title-bar-icon-close-toast {
    position: absolute;
    left: calc(100% + 10px);
    top: 50%;
    transform: translateY(-50%);
    white-space: nowrap;
    background: rgba(30, 30, 30, 0.92);
    color: #fff;
    font-size: 12px;
    padding: 5px 14px;
    border-radius: 6px;
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.25s ease;
    z-index: 200;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.18);
}
:root[data-theme="dark"] .title-bar-icon-close-toast {
    background: rgba(60, 60, 60, 0.94);
}
.title-bar-icon-close-toast.show {
    opacity: 1;
}
.title-bar-title {
    position: absolute;
    left: 0;
    right: 0;
    text-align: center;
    font-size: 12px;
    color: var(--text-primary);
    opacity: 0.8;
    cursor: default;
    pointer-events: none;
}
.title-bar-controls {
    display: flex;
    margin-left: auto;
    height: 100%;
}
.title-bar-btn {
    width: 46px;
    height: 100%;
    border: none;
    background: transparent;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    color: var(--text-primary);
    border-radius: 0;
    padding: 0;
    transition: background 0.1s;
    -webkit-app-region: no-drag;
}
.title-bar-btn:hover {
    background: rgba(0, 0, 0, 0.08);
}
:root[data-theme="dark"] .title-bar-btn:hover {
    background: rgba(255, 255, 255, 0.08);
}
.title-bar-btn .title-bar-icon-glyph {
    font-family: "Segoe MDL2 Assets", sans-serif;
    font-size: 10px;
    line-height: 1;
}
:root[data-os="win11"] .title-bar-btn .title-bar-icon-glyph {
    font-family: "Segoe Fluent Icons", "Segoe MDL2 Assets", sans-serif;
}
.title-bar-close:hover {
    background: #C42B1C !important;
    color: #fff !important;
}
.title-bar-btn.title-bar-log {
    min-width: 106px;
    padding: 0 10px;
    font-size: 12px;
    justify-content: center;
    display: inline-flex;
    align-items: center;
    gap: 8px;
}
.title-bar-btn.title-bar-log .title-bar-log-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 20px;
    height: 20px;
}
.title-bar-btn.title-bar-log .title-bar-log-icon svg {
    width: 20px;
    height: 20px;
}
.title-bar-btn.title-bar-log .title-bar-log-label {
    display: inline-block;
}
"""
        else:
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
"""
        os_tag = "win11" if (sys.platform == "win32" and _is_win11()) else "win10"
        js = f"""
(function() {{
    const style = document.createElement('style');
    style.textContent = {json.dumps(css)};
    if (document.documentElement) {{
        document.documentElement.appendChild(style);
        document.documentElement.setAttribute('data-frameless', '{str(self._frameless).lower()}');
        document.documentElement.setAttribute('data-os', '{os_tag}');
    }}
}})();
"""
        script = QWebEngineScript()
        script.setSourceCode(js)
        script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentReady)
        script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(script)

    def _inject_title_bar_html(self):
        if not self._frameless:
            return
        log_button_html = ""
        if getattr(self, '_window_tag', '') == 'settings':
            log_button_html = (
                '<button class="title-bar-btn title-bar-log" id="btn-logs" title="\u6253\u5f00\u65e5\u5fd7" onclick="window.pywebview.api.open_logs_window()">'
                '<span class="title-bar-log-icon">'
                '<svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor">'
                '<path d="M5 3 H15 L19 7 V21 H5 Z"/>'
                '<path d="M15 3 V7 H19" fill="#FFFFFF" opacity="0.4"/>'
                '<rect x="8" y="11" width="8" height="1.4" rx="0.7" fill="#FFFFFF" opacity="0.95"/>'
                '<rect x="8" y="14" width="8" height="1.4" rx="0.7" fill="#FFFFFF" opacity="0.95"/>'
                '<rect x="8" y="17" width="5" height="1.4" rx="0.7" fill="#FFFFFF" opacity="0.95"/>'
                '</svg>'
                '</span>'
                '<span class="title-bar-log-label">\u65e5\u5fd7</span>'
                '</button>'
            )
        title_bar_html = (
            '<div class="title-bar" id="title-bar">'
            '<div class="title-bar-icon" id="title-bar-icon">'
            '<div class="title-bar-icon-close-toast" id="title-bar-icon-close-toast">\u518d\u6b21\u51fb\u94ae\u4ee5\u5173\u95ed\u7a97\u53e3</div>'
            '</div>'
            '<div class="title-bar-title" id="title-bar-text"></div>'
            '<div class="title-bar-controls">'
            + log_button_html +
            '<button class="title-bar-btn" id="btn-minimize" title="\u6700\u5c0f\u5316" onclick="window.pywebview.api.minimize_window()">'
            '<span class="title-bar-icon-glyph">\uE921</span>'
            '</button>'
            '<button class="title-bar-btn" id="btn-maximize" title="\u6700\u5927\u5316" onclick="window.pywebview.api.toggle_maximize()">'
            '<span class="title-bar-icon-glyph">\uE922</span>'
            '</button>'
            '<button class="title-bar-btn title-bar-close" id="btn-close" title="\u5173\u95ed" onclick="window.pywebview.api.close_window()">'
            '<span class="title-bar-icon-glyph">\uE8BB</span>'
            '</button>'
            '</div>'
            '</div>'
        )
        js = (
            "(function(){"
            "var tb=document.getElementById('title-bar');"
            "if(!tb){"
            "var d=document.createElement('div');"
            "d.innerHTML=" + json.dumps(title_bar_html) + ";"
            "var el=d.firstChild;"
            "document.body.insertBefore(el,document.body.firstChild);"
            "}"
            "})()"
        )
        self.page().runJavaScript(js)

    def _inject_title_bar_js(self):
        if not self._frameless:
            return
        js = (
            "(function(){"
            "function loadTitleBarInfo(){"
            "try{"
            "window.pywebview.api.get_window_title().then(function(t){"
            "var el=document.getElementById('title-bar-text');"
            "if(el&&t)el.textContent=t;"
            "});"
            "window.pywebview.api.get_window_icon_path().then(function(p){"
            "var el=document.getElementById('title-bar-icon');"
            "if(el&&p){"
            "var img=document.createElement('img');"
            "img.src='file:///'+p;"
            "img.alt='';"
            "el.appendChild(img);"
            "}"
            "});"
            "}catch(e){}"
            "}"
            "if(window.pywebview&&window.pywebview.api){loadTitleBarInfo();}"
            "else{window.addEventListener('pywebviewready',loadTitleBarInfo);}"
            "var _iconCloseTimer=null;"
            "var _iconClosePending=false;"
            "var _iconCloseIcon=document.getElementById('title-bar-icon');"
            "var _iconCloseToast=document.getElementById('title-bar-icon-close-toast');"
            "if(_iconCloseIcon&&_iconCloseToast){"
            "_iconCloseIcon.addEventListener('click',function(e){"
            "e.stopPropagation();"
            "if(_iconClosePending){"
            "clearTimeout(_iconCloseTimer);"
            "_iconCloseTimer=null;"
            "_iconClosePending=false;"
            "_iconCloseToast.classList.remove('show');"
            "window.pywebview.api.close_window();"
            "}else{"
            "_iconClosePending=true;"
            "_iconCloseToast.classList.add('show');"
            "_iconCloseTimer=setTimeout(function(){"
            "_iconClosePending=false;"
            "_iconCloseToast.classList.remove('show');"
            "_iconCloseTimer=null;"
            "},3000);"
            "}"
            "});"
            "}"
            "})()"
        )
        self.page().runJavaScript(js)

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
            if sys.platform == "win32":
                from ppt_assistant.core.platform_integration import remove_window_border_delayed
                remove_window_border_delayed(self)
            self.resize(340, 400)

            screen = QApplication.primaryScreen()
            if screen:
                geo = screen.availableGeometry()
                x = geo.x() + geo.width() - 340 - 20
                y = geo.y() + 20
                self.move(x, y)

            if self._frameless:
                self._set_custom_title_bar_visible(False)
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

            if self._frameless:
                self._set_custom_title_bar_visible(True)

        self._apply_page_background()
        self.show()

    def _set_custom_title_bar_visible(self, visible):
        display = "flex" if visible else "none"
        js = (
            "(function(){"
            "var tb=document.getElementById('title-bar');"
            "if(tb)tb.style.display=" + json.dumps(display) + ";"
            "})()"
        )
        try:
            self.page().runJavaScript(js)
        except Exception:
            pass

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

    def nativeEvent(self, eventType, message):
        try:
            import ctypes.wintypes
            msg_ptr = int(message)
            if msg_ptr:
                msg = ctypes.wintypes.MSG.from_address(msg_ptr)
                if msg.message == 0x0084:  # WM_NCHITTEST
                    if getattr(self, "_mini_mode", False) or self._frameless:
                        from PySide6.QtGui import QCursor
                        pos = self.mapFromGlobal(QCursor.pos())
                        x, y = pos.x(), pos.y()
                        w, h = self.width(), self.height()

                        title_bar_h = 40 if self.isMaximized() else 32
                        if getattr(self, '_window_tag', '') == 'settings':
                            # settings window has the extra log button.
                            btn_area_left = w - 220
                        else:
                            btn_area_left = w - 138

                        if y < title_bar_h and x < btn_area_left:
                            return True, 2  # HTCAPTION

                        if not self.isMaximized() and not self.isFullScreen():
                            border = 8
                            is_left = x < border
                            is_right = x > w - border
                            is_top = y < border
                            is_bottom = y > h - border

                            if is_top and is_left:
                                return True, 13
                            elif is_top and is_right:
                                return True, 14
                            elif is_bottom and is_left:
                                return True, 16
                            elif is_bottom and is_right:
                                return True, 17
                            elif is_left:
                                return True, 10
                            elif is_right:
                                return True, 11
                            elif is_top:
                                return True, 12
                            elif is_bottom:
                                return True, 15

                if msg.message == 0x0083:  # WM_NCCALCSIZE
                    if self._frameless and msg.wParam:
                        class RECT(ctypes.Structure):
                            _fields_ = [
                                ("left", ctypes.c_long),
                                ("top", ctypes.c_long),
                                ("right", ctypes.c_long),
                                ("bottom", ctypes.c_long),
                            ]

                        class NCCALCSIZE_PARAMS(ctypes.Structure):
                            _fields_ = [
                                ("rgrc", RECT * 3),
                                ("lppos", ctypes.c_void_p),
                            ]

                        params = NCCALCSIZE_PARAMS.from_address(msg.lParam)
                        monitor = ctypes.windll.user32.MonitorFromWindow(
                            msg.hWnd, 1
                        )
                        if monitor:
                            class MONITORINFO(ctypes.Structure):
                                _fields_ = [
                                    ("cbSize", ctypes.c_uint),
                                    ("rcMonitor", RECT),
                                    ("rcWork", RECT),
                                    ("dwFlags", ctypes.c_uint),
                                ]

                            mi = MONITORINFO()
                            mi.cbSize = ctypes.sizeof(MONITORINFO)
                            if ctypes.windll.user32.GetMonitorInfoW(
                                monitor, ctypes.byref(mi)
                            ):
                                new_w = params.rgrc[0].right - params.rgrc[0].left
                                new_h = params.rgrc[0].bottom - params.rgrc[0].top
                                mon_w = mi.rcMonitor.right - mi.rcMonitor.left
                                mon_h = mi.rcMonitor.bottom - mi.rcMonitor.top
                                if new_w >= mon_w and new_h >= mon_h:
                                    params.rgrc[0].left = mi.rcWork.left
                                    params.rgrc[0].top = mi.rcWork.top
                                    params.rgrc[0].right = mi.rcWork.right
                                    params.rgrc[0].bottom = mi.rcWork.bottom
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
            is_dark=self._effective_is_dark(),
        )
        self.update()

    def _force_refresh(self):
        if self._mini_mode:
            return
        self.update()

    def update_theme_mode(self, theme_mode):
        self._theme_mode = theme_mode
        self._apply_page_background()
        self._apply_backdrop()

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

    def showEvent(self, event):
        super().showEvent(event)
        self._setup_memory_timer()
        if not self._centered:
            self._centered = True
            QTimer.singleShot(0, self._center_on_screen)
        self._aero_enabled = False
        if self._pending_url is not None:
            self.load(self._pending_url)
            self._pending_url = None
        if self._pending_load_timer is not None:
            try:
                self._pending_load_timer.stop()
            except Exception:
                pass
            self._pending_load_timer = None
        self._apply_page_background()
        self._apply_backdrop()
        if self._frameless:
            QTimer.singleShot(0, self._enable_frameless_aero)

    def _enable_frameless_aero(self):
        if self._aero_enabled:
            return
        self._aero_enabled = True
        try:
            hwnd = int(self.winId())
            user32 = ctypes.windll.user32
            dwmapi = ctypes.windll.dwmapi

            GWL_STYLE = -16
            style = user32.GetWindowLongW(hwnd, GWL_STYLE)
            style |= 0x00040000  # WS_THICKFRAME
            style |= 0x00C00000  # WS_CAPTION
            style |= 0x00010000  # WS_MAXIMIZEBOX
            style |= 0x00020000  # WS_MINIMIZEBOX
            user32.SetWindowLongW(hwnd, GWL_STYLE, style)

            class MARGINS(ctypes.Structure):
                _fields_ = [
                    ("cxLeftWidth", ctypes.c_int),
                    ("cxRightWidth", ctypes.c_int),
                    ("cyTopHeight", ctypes.c_int),
                    ("cyBottomHeight", ctypes.c_int),
                ]

            margins = MARGINS(1, 1, 1, 1)
            dwmapi.DwmExtendFrameIntoClientArea(hwnd, ctypes.byref(margins))

            SWP_FRAMECHANGED = 0x0020
            SWP_NOMOVE = 0x0002
            SWP_NOSIZE = 0x0001
            SWP_NOZORDER = 0x0004
            SWP_NOOWNERZORDER = 0x0200
            flags = SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER
            user32.SetWindowPos(hwnd, 0, 0, 0, 0, 0, flags)
        except Exception:
            pass

    def _on_window_title_changed(self, title):
        try:
            self.page().runJavaScript(
                "try{var e=document.getElementById('title-bar-text');if(e)e.textContent="
                + json.dumps(title)
                + "}catch(e){}"
            )
        except Exception:
            pass

    def changeEvent(self, event):
        if event.type() == QEvent.Type.WindowStateChange:
            self._push_maximized_state()
        super().changeEvent(event)

    def _push_maximized_state(self):
        try:
            maximized = "true" if self.isMaximized() else "false"
            icon_text = json.dumps("\uE923" if self.isMaximized() else "\uE922")
            self.page().runJavaScript(
                "try{"
                "document.documentElement.setAttribute('data-maximized','" + maximized + "');"
                "var b=document.getElementById('btn-maximize');"
                "if(b){var s=b.querySelector('.title-bar-icon-glyph');"
                "if(s)s.textContent=" + icon_text + ";}"
                "}catch(e){}"
            )
        except Exception:
            pass

    def moveEvent(self, event):
        super().moveEvent(event)

    def resizeEvent(self, event):
        super().resizeEvent(event)

    def hideEvent(self, event):
        self._stop_memory_timer()
        super().hideEvent(event)

    def closeEvent(self, event):
        self._stop_memory_timer()
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
        super().closeEvent(event)


def apply_win11_aesthetics(window, theme_mode=None, settings=None, window_tag="", is_dark=None):
    try:
        hwnd = int(window.winId())
        dwmapi = ctypes.windll.dwmapi
        corner_preference = ctypes.c_int(2)
        dwmapi.DwmSetWindowAttribute(
            hwnd, 33, ctypes.byref(corner_preference), ctypes.sizeof(corner_preference)
        )
        if is_dark is None:
            is_dark = _resolve_theme_dark(theme_mode)
        _apply_window_theme(hwnd, bool(is_dark))
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
    _maybe_add_vxkex_path()
    app = QApplication(sys.argv)
    _warmup_webengine()
    icon = _load_app_icon()
    if not icon.isNull():
        app.setWindowIcon(icon)
    os.environ["QT_AUTO_SCREEN_SCALE_FACTOR"] = "1"

    try:
        from ppt_assistant.core.update_service import start_update_server
        start_update_server(28423)
    except Exception:
        pass

    if "--dialog" in sys.argv or "--crash-file" in sys.argv:
        mode = "--dialog" if "--dialog" in sys.argv else "--crash-file"
        try:
            idx = sys.argv.index(mode)
            file_path = sys.argv[idx + 1]
        except ValueError:
            return
        settings_path = os.environ.get("SETTINGS_PATH")
        if not settings_path:
            if getattr(sys, "frozen", False):
                settings_path = os.path.join(os.path.dirname(sys.executable), "settings.json")
            else:
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
        try:
            base_dir_v = os.path.dirname(os.path.abspath(__file__))
            root_dir_v = os.path.dirname(base_dir_v)
            version_path = os.path.join(root_dir_v, "version.json")
            if os.path.exists(version_path):
                with open(version_path, "r", encoding="utf-8") as f:
                    api.version = json.load(f)
        except Exception:
            api.version = {}
        api.version["device_uuid"] = get_device_uuid()[:8]
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

        settings_dir = os.path.dirname(settings_path)
        active_marker = os.path.join(settings_dir, "_active")
        active_settings_path = settings_path
        if os.path.exists(active_marker):
            try:
                with open(active_marker, "r", encoding="utf-8") as _af:
                    _active_name = _af.read().strip()
                if _active_name and _active_name != "default":
                    _profile_path = os.path.join(settings_dir, _active_name + ".json")
                    if os.path.exists(_profile_path):
                        active_settings_path = _profile_path
            except Exception:
                pass

        api.settings = {}
        if os.path.exists(active_settings_path):
            try:
                with open(active_settings_path, "r", encoding="utf-8") as f:
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
        api.version["device_uuid"] = get_device_uuid()[:8]
        theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
        defer_load = os.environ.get("DEFER_WEBENGINE_LOAD", "").strip().lower() in [
            "1",
            "true",
            "yes",
            "on",
        ]
        defer_load = _should_defer_initial_load(url, title, defer_load)
        window = MainWindow(
            title, url, api, width, height, theme_mode, custom_border, defer_load, frameless=custom_border
        )
        if title == "Settings":
            window.setMinimumWidth(1099)
        window.show()
    else:
        return
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
