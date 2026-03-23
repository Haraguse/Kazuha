import sys
import os
import json
import ctypes
import tempfile
import subprocess
import base64
from json import JSONDecodeError

if sys.platform == "linux" and "QT_QPA_PLATFORM" not in os.environ:
    os.environ["QT_QPA_PLATFORM"] = "xcb"

from PySide6.QtWidgets import QApplication, QFileDialog
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWebEngineCore import QWebEngineScript, QWebEngineSettings, QWebEngineProfile
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtCore import QObject, Slot, QUrl, QFile, QIODevice, Qt, QTimer, QBuffer, QByteArray, QJsonValue, QCoreApplication
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

if sys.platform == "win32":
    try:
        _EXISTING_WINDOW_NOTIFY_MESSAGE = ctypes.windll.user32.RegisterWindowMessageW(
            "Kazuha.WebView.NotifyExistingWindow"
        )
    except Exception:
        _EXISTING_WINDOW_NOTIFY_MESSAGE = 0

DWMSBT_AUTO = 0
DWMSBT_NONE = 1
DWMSBT_MAINWINDOW = 2
DWMSBT_TRANSIENTWINDOW = 3
DWMSBT_TABBEDWINDOW = 4

def _supports_system_backdrop():
    if sys.platform != "win32":
        return False
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


def _get_user_root_dir() -> str:
    root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    if getattr(sys, "frozen", False):
        root_dir = os.path.dirname(sys.executable)
    return root_dir


def _list_user_themes() -> list[dict]:
    root_dir = _get_user_root_dir()
    themes_dir = os.path.join(root_dir, "user", "themes")
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
            with open(manifest_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            if data.get("name") != name:
                continue
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
    if sys.platform != "win32":
        return False
    try:
        import winreg
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize") as key:
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
    if sys.platform != "win32" or not hwnd:
        return
    try:
        dwmapi = ctypes.windll.dwmapi
        uxtheme = ctypes.windll.uxtheme
        user32 = ctypes.windll.user32
        val = ctypes.c_int(1 if is_dark else 0)
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ctypes.byref(val), ctypes.sizeof(val))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ctypes.byref(val), ctypes.sizeof(val))
        if is_dark:
            border = ctypes.c_int(0x00202020)
            caption = ctypes.c_int(0x00202020)
            text = ctypes.c_int(0x00FFFFFF)
        else:
            border = ctypes.c_int(_DWM_COLOR_DEFAULT)
            caption = ctypes.c_int(_DWM_COLOR_DEFAULT)
            text = ctypes.c_int(_DWM_COLOR_DEFAULT)
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ctypes.byref(border), ctypes.sizeof(border))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ctypes.byref(caption), ctypes.sizeof(caption))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ctypes.byref(text), ctypes.sizeof(text))
        theme = "DarkMode_Explorer" if is_dark else "Explorer"
        uxtheme.SetWindowTheme(hwnd, ctypes.c_wchar_p(theme), None)
        flags = 0x0001 | 0x0002 | 0x0004 | 0x0020
        user32.SetWindowPos(hwnd, 0, 0, 0, 0, 0, flags)
    except Exception:
        pass

def _resolve_system_backdrop_type(settings, window_tag):
    if sys.platform != "win32":
        return None
    if window_tag not in ("settings", "timer"):
        return None
    if not _supports_system_backdrop():
        return DWMSBT_NONE
    if not isinstance(settings, dict):
        return DWMSBT_NONE
    general = settings.get("General") if isinstance(settings.get("General"), dict) else {}
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

def _apply_system_backdrop(hwnd, backdrop_type):
    if sys.platform != "win32" or not hwnd or backdrop_type is None:
        return
    try:
        dwmapi = ctypes.windll.dwmapi
        val = ctypes.c_int(int(backdrop_type))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ctypes.byref(val), ctypes.sizeof(val))
    except Exception:
        pass

def _force_dwm_redraw(hwnd):
    if sys.platform != "win32" or not hwnd:
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


def _get_screen_refresh_rate():
    try:
        import ctypes
        user32 = ctypes.windll.user32
        hdc = user32.GetDC(0)
        rate = ctypes.windll.gdi32.GetDeviceCaps(hdc, 116) # VREFRESH
        user32.ReleaseDC(0, hdc)
        return rate if rate > 1 else 60
    except:
        return 60

def _apply_chromium_flags():
    _maybe_add_vxkex_path()
    flags = [
        "--enable-gpu",
        "--ignore-gpu-blocklist",
        "--enable-zero-copy",
        "--enable-features=BackForwardCache",
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
    ]
    
    rate = _get_screen_refresh_rate()
    target_fps = rate * 3
    os.environ["KAZUHA_TARGET_FPS"] = str(target_fps)


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

def _is_windows7():
    if sys.platform != "win32":
        return False
    try:
        v = sys.getwindowsversion()
        return v.major == 6 and v.minor == 1
    except Exception:
        return False

def _get_wallpaper_path():
    if sys.platform != "win32":
        return None
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

def _create_shortcut(target_path, shortcut_path, work_dir=None, icon_path=None, args=None):
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
    if sys.platform != "win32": return
    import winreg
    key = winreg.HKEY_CURRENT_USER
    sub_key = r"Software\Microsoft\Windows\CurrentVersion\Run"
    app_name = "Kazuha"
    
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
    if sys.platform != "win32": return
    try:
        programs_path = os.path.join(os.environ["APPDATA"], r"Microsoft\Windows\Start Menu\Programs")
        if not os.path.exists(programs_path):
            return
        shortcut_path = os.path.join(programs_path, "Kazuha.lnk")
        
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
    if sys.platform != "win32": return
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
        temp_dir = os.path.join(os.environ["TEMP"], "Kazuha_Pin")
        if not os.path.exists(temp_dir):
            os.makedirs(temp_dir)
        shortcut_path = os.path.join(temp_dir, "Kazuha.lnk")
        _create_shortcut(exe_path, shortcut_path, work_dir, icon_path, args)
        
        folder = shell.Namespace(temp_dir)
        item = folder.ParseName("Kazuha.lnk")
        
        # Verbs are localized. This is the problem.
        # English: "Pin to Taskbar"
        # Chinese: "固定到任务栏"
        # We can try iterating verbs.
        
        verbs = item.Verbs()
        taskbar_verb = None
        for v in verbs:
            name = v.Name.replace("&", "").lower()
            if "taskbar" in name or "任务栏" in name or "タスクバー" in name:
                if enable and ("pin" in name or "固定" in name or "ピン" in name) and ("unpin" not in name and "取消" not in name and "外す" not in name):
                    taskbar_verb = v
                    break
                elif not enable and ("unpin" in name or "取消" in name or "外す" in name):
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
             programs_path = os.path.join(os.environ["APPDATA"], r"Microsoft\Windows\Start Menu\Programs")
             shortcut_path = os.path.join(programs_path, "Kazuha.lnk")
             folder = shell.Namespace(programs_path)
             item = folder.ParseName("Kazuha.lnk")
             if item:
                 verbs = item.Verbs()
                 for v in verbs:
                    name = v.Name.replace("&", "").lower()
                    if "taskbar" in name or "任务栏" in name or "タスクバー" in name:
                        if ("pin" in name or "固定" in name or "ピン" in name) and ("unpin" not in name and "取消" not in name and "外す" not in name):
                             v.DoIt()
                             break
    except Exception as e:
        print(f"Error pinning to taskbar: {e}", file=sys.stderr)

class Api(QObject):
    def __init__(self, window=None):
        super().__init__()
        self._window = window
        self.settings = {}
        self.version = {}
        self.dialog_data = {}
        self._icon_cache = {}

    def set_window(self, window):
        self._window = window

    def _get_window_hwnd(self):
        if not self._window:
            return 0
        try:
            return int(self._window.winId())
        except Exception:
            return 0

    def _flash_window(self):
        hwnd = self._get_window_hwnd()
        if sys.platform != "win32" or not hwnd:
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
            if (window.__kazuhaExistingWindowToastTimer) {{
                clearTimeout(window.__kazuhaExistingWindowToastTimer);
            }}
            toast.classList.add("show");
            window.__kazuhaExistingWindowToastTimer = window.setTimeout(() => {{
                toast.classList.remove("show");
            }}, 2200);
            return true;
        }};
        if (typeof window.showExistingWindowToast === "function") {{
            window.showExistingWindowToast(message);
        }} else if (!showExistingToast()) {{
            let style = document.getElementById("kazuha-existing-window-toast-style");
            if (!style) {{
                style = document.createElement("style");
                style.id = "kazuha-existing-window-toast-style";
                style.textContent = `
                    .kazuha-existing-window-toast {{
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
                    .kazuha-existing-window-toast.show {{
                        opacity: 1;
                        transform: translateX(-50%) translateY(0);
                    }}
                    .kazuha-existing-window-toast__icon {{
                        width: 10px;
                        height: 10px;
                        flex: 0 0 auto;
                        border-radius: 50%;
                        background: #3275F5;
                        box-shadow: 0 0 0 4px rgba(50, 117, 245, 0.18);
                    }}
                    .kazuha-existing-window-toast__text {{
                        white-space: nowrap;
                        overflow: hidden;
                        text-overflow: ellipsis;
                    }}
                    [data-theme="dark"] .kazuha-existing-window-toast {{
                        background: rgba(45, 45, 45, 0.92);
                        border: 0.5px solid rgba(255, 255, 255, 0.14);
                    }}
                `;
                (document.head || document.documentElement).appendChild(style);
            }}
            let toast = document.getElementById("kazuha-existing-window-toast");
            if (!toast) {{
                toast = document.createElement("div");
                toast.id = "kazuha-existing-window-toast";
                toast.className = "kazuha-existing-window-toast";
                toast.innerHTML = '<div class="kazuha-existing-window-toast__icon"></div><div class="kazuha-existing-window-toast__text"></div>';
                (document.body || document.documentElement).appendChild(toast);
            }}
            const text = toast.querySelector(".kazuha-existing-window-toast__text");
            if (text) {{
                text.textContent = message;
            }}
            if (window.__kazuhaExistingWindowToastTimer) {{
                clearTimeout(window.__kazuhaExistingWindowToastTimer);
            }}
            toast.classList.add("show");
            window.__kazuhaExistingWindowToastTimer = window.setTimeout(() => {{
                toast.classList.remove("show");
            }}, 2200);
        }}
        try {{
            window.dispatchEvent(new CustomEvent("kazuha:existing-window-toast", {{ detail: {{ message }} }}));
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
                _apply_window_theme(int(self._window.winId()), _resolve_theme_dark(theme_mode))
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
        return self.settings

    @Slot(result="QVariant")
    def get_version(self):
        return self.version

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
            "exit": "Minimize.svg"
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
        if sys.platform != "win32":
            return []
        try:
            import winreg
            keys = [
                (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
                (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
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
                self._window,
                "Select Application",
                "",
                get_quick_launch_dialog_filter()
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
            pixmap = target_screen.grabWindow(0, tb_rect[0], tb_rect[1], tb_rect[2], tb_rect[3])
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
                "is_visible": True
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
                settings_path = os.path.join(os.path.dirname(sys.executable), "settings.json")
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
            if category == "General" and key in ("SystemBackdropEnabled", "SystemBackdropType"):
                if self._window and hasattr(self._window, "apply_backdrop_settings"):
                    self._window.apply_backdrop_settings()
        except Exception as e:
            print(f"Error saving settings: {e}", file=sys.stderr)

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
        self._show_existing_window_toast(message)

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
            "accentColor": accent
        }
        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False, encoding="utf-8") as f:
            json.dump(dialog_data, f)
            temp_path = f.name
        subprocess.Popen([sys.executable, __file__, "--dialog", temp_path])

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
            "targetLang": font_lang
        }
        temp_settings = json.loads(json.dumps(self.settings))
        if font_name:
            if "Fonts" not in temp_settings:
                temp_settings["Fonts"] = {}
            if "Profiles" not in temp_settings["Fonts"]:
                temp_settings["Fonts"]["Profiles"] = {}
            lang = font_lang or temp_settings.get("General", {}).get("Language", "zh-CN")
            if lang not in temp_settings["Fonts"]["Profiles"]:
                temp_settings["Fonts"]["Profiles"][lang] = {}
            temp_settings["Fonts"]["Profiles"][lang]["web"] = font_name
        with tempfile.NamedTemporaryFile(mode="w", suffix=".json", delete=False, encoding="utf-8") as f:
            json.dump(dialog_data, f)
            temp_path = f.name
        if font_name:
            dialog_data["overrideSettings"] = temp_settings
            with open(temp_path, "w", encoding="utf-8") as f:
                json.dump(dialog_data, f)
        subprocess.Popen([sys.executable, __file__, "--dialog", temp_path])

    @Slot(result="QVariant")
    def get_dialog_data(self):
        return self.dialog_data

    @Slot()
    def on_confirm(self):
        print("DIALOG_CONFIRMED")
        sys.stdout.flush()
        if self._window:
            self._window.close()
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
        if self._window:
            self._window.close()
        sys.exit(0)

    @Slot()
    def on_cancel(self):
        print("DIALOG_CANCELLED")
        sys.stdout.flush()
        if self._window:
            self._window.close()
        sys.exit(0)
    @Slot()
    def open_onboarding_preview(self):
        base_dir = os.path.dirname(os.path.abspath(__file__))
        onboarding_html = os.path.join(base_dir, "builtins", "onboarding", "onboarding.html")
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
            subprocess.Popen([sys.executable, "--webview-runner", onboarding_html, "Onboarding Preview", width, height, "true"], env=env)
        else:
            subprocess.Popen([sys.executable, main_path, "--webview-runner", onboarding_html, "Onboarding Preview", width, height, "true"], env=env)
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
    @Slot(result="QVariant")
    def import_settings(self):
        preview_mode = os.environ.get("ONBOARDING_PREVIEW", "").lower() == "true"
        target_path = self._get_settings_path()
        file_path = None
        try:
            file_path, _ = QFileDialog.getOpenFileName(self._window, "选择设置文件", "", "JSON (*.json);;所有文件 (*.*)")
        except Exception:
            file_path = None
        if not file_path:
            return self.settings
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not isinstance(data, dict):
                return self.settings
            if not preview_mode:
                with open(target_path, "w", encoding="utf-8") as wf:
                    json.dump(data, wf, indent=4, ensure_ascii=False)
            self.settings = data
            self.update_settings(data)
            return data
        except Exception:
            return self.settings

    @Slot(result=str)
    def get_assets_path(self):
        return os.environ.get("ASSETS_PATH", "")

    @Slot(result="QVariant")
    def get_timer_state(self):
        return {
            "remaining": int(os.environ.get("TIMER_REMAINING", 0)),
            "is_running": os.environ.get("TIMER_IS_RUNNING", "false") == "true"
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

        if sys.platform == "win32":
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
            js = f.readAll().data().decode('utf-8')
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
    def __init__(self, title, url, api, width, height, theme_mode="auto", custom_border=False, defer_load=False):
        super().__init__()
        self.setWindowTitle(title)
        self.resize(width, height)
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
        self._pending_url = None
        self._did_hard_refresh = False
        self._window_tag = self._detect_window_tag(url, title)
        self._render_crash_count = 0
        self._max_reload_attempts = 3
        self._crash_recovery_timer = None
        self._apply_page_background()
        settings = self.page().settings()
        allow_gpu = not _is_windows7()
        settings.setAttribute(QWebEngineSettings.WebAttribute.Accelerated2dCanvasEnabled, allow_gpu)
        settings.setAttribute(QWebEngineSettings.WebAttribute.WebGLEnabled, allow_gpu)
        settings.setAttribute(QWebEngineSettings.WebAttribute.LocalContentCanAccessRemoteUrls, True)
        settings.setAttribute(QWebEngineSettings.WebAttribute.LocalContentCanAccessFileUrls, True)
        settings.setAttribute(QWebEngineSettings.WebAttribute.ScrollAnimatorEnabled, True)
        settings.setAttribute(QWebEngineSettings.WebAttribute.AutoLoadImages, True)
        settings.setAttribute(QWebEngineSettings.WebAttribute.PluginsEnabled, False)

        # Configure disk cache
        profile = self.page().profile()
        cache_path = os.path.join(tempfile.gettempdir(), "kazuha_webengine_cache")
        profile.setCachePath(cache_path)
        profile.setPersistentStoragePath(cache_path)
        profile.setHttpCacheType(QWebEngineProfile.HttpCacheType.DiskHttpCache)
        profile.setHttpCacheMaximumSize(50 * 1024 * 1024)  # 50 MB
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
        settings_json = json.dumps(api.settings, ensure_ascii=False)
        settings_script = QWebEngineScript()
        settings_script.setSourceCode(f"window.initialSettings = {settings_json};")
        settings_script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
        settings_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(settings_script)
        supports_backdrop = _supports_system_backdrop()
        support_script = QWebEngineScript()
        support_script.setSourceCode(f"window.__SYSTEM_BACKDROP_SUPPORTED = {json.dumps(supports_backdrop)};")
        support_script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
        support_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(support_script)
        try:
            general = api.settings.get("General", {}) if isinstance(api.settings, dict) else {}
        except Exception:
            general = {}
        enabled = bool(general.get("SystemBackdropEnabled")) and self._window_tag in ("settings", "timer")
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
        backdrop_attr_script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
        backdrop_attr_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(backdrop_attr_script)
        preview_flag = os.environ.get("ONBOARDING_PREVIEW", "").lower() == "true"
        preview_script = QWebEngineScript()
        preview_script.setSourceCode(f"window.__ONBOARDING_PREVIEW = {json.dumps(preview_flag)};")
        preview_script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
        preview_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
        self.page().scripts().insert(preview_script)
        if self._custom_border:
            self._inject_custom_border()
        target_url = QUrl.fromUserInput(url)
        if self._defer_load:
            self._pending_url = target_url
        else:
            self.load(target_url)
        self.loadFinished.connect(lambda *_: (self._apply_page_background(), self._schedule_backdrop_apply()))
        self._schedule_backdrop_apply()
        
        self.renderProcessTerminated.connect(self._on_render_process_terminated)

    def _on_render_process_terminated(self, status, exit_code):
        self._render_crash_count += 1
        print(f"[WebView] Render process terminated: status={status}, exit_code={exit_code}", file=sys.stderr)
        print(f"[WebView] Crash #{self._render_crash_count}/{self._max_reload_attempts}", file=sys.stderr)
        
        # If too many crashes, disable GPU and retry once, then give up
        if self._render_crash_count > self._max_reload_attempts:
            print(f"[WebView] Too many crashes ({self._render_crash_count}). Giving up on recovery.", file=sys.stderr)
            return
        
        # Use exponential backoff: 100ms, 500ms, 1500ms
        delay = min(100 * (2 ** (self._render_crash_count - 1)), 2000)
        print(f"[WebView] Scheduling reload in {delay}ms", file=sys.stderr)
        
        # If this is the second crash, try disabling GPU acceleration
        if self._render_crash_count == 2:
            try:
                print(f"[WebView] Disabling GPU acceleration due to repeated crashes", file=sys.stderr)
                settings = self.page().settings()
                settings.setAttribute(QWebEngineSettings.WebAttribute.Accelerated2dCanvasEnabled, False)
                settings.setAttribute(QWebEngineSettings.WebAttribute.WebGLEnabled, False)
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
        backdrop_type = _resolve_system_backdrop_type(settings, getattr(self, "_window_tag", ""))
        if backdrop_type is not None and backdrop_type != DWMSBT_NONE:
            try:
                self.setAutoFillBackground(True)
                palette = self.palette()
                palette.setColor(self.backgroundRole(), Qt.transparent)
                self.setPalette(palette)
            except Exception:
                pass
            _safe_set_widget_attr(self, getattr(Qt, "WA_OpaquePaintEvent", None), False)
            _safe_set_widget_attr(self, Qt.WA_TranslucentBackground, True)
            _safe_set_widget_attr(self, getattr(Qt, "WA_NoSystemBackground", None), False)
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
        backdrop_type = _resolve_system_backdrop_type(settings, getattr(self, "_window_tag", ""))
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
        return ""

    def set_mini_mode(self, enabled):
        if self._mini_mode == enabled:
            return
        self._mini_mode = enabled
        if enabled:
            self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool)
            self.setAttribute(Qt.WA_TranslucentBackground)
            self.resize(220, 220)
            
            # Move to top-right corner
            screen = QApplication.primaryScreen()
            if screen:
                geo = screen.availableGeometry()
                x = geo.x() + geo.width() - 220 - 20
                y = geo.y() + 20
                self.move(x, y)
        else:
            self.setWindowFlags(Qt.Window)
            self.setAttribute(Qt.WA_TranslucentBackground, False)
            self.resize(800, 600)
            self._center_on_screen()
        
        self._apply_page_background()
        self.show()

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
        if sys.platform == "win32" and _EXISTING_WINDOW_NOTIFY_MESSAGE:
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
        apply_win11_aesthetics(self, self._theme_mode, getattr(self.api, "settings", {}), getattr(self, "_window_tag", ""))
        try:
            _force_dwm_redraw(int(self.winId()))
        except Exception:
            pass
        if self._is_system_backdrop_enabled():
            self._force_webview_transparent()
            self._force_webview_repaint()
        self.update()

    def _force_refresh(self):
        if self._mini_mode:
            return
        if self.isMaximized() or self.isFullScreen():
            return
        w = self.width()
        h = self.height()
        if w <= 2 or h <= 2:
            return
        self.resize(w - 1, h - 1)
        QTimer.singleShot(0, lambda: self.resize(w, h))
        QTimer.singleShot(0, lambda: _force_dwm_redraw(int(self.winId())))

    def _hard_resize_nudge(self):
        if self._mini_mode or self.isMaximized() or self.isFullScreen():
            return
        if self._did_hard_refresh:
            return
        w = self.width()
        h = self.height()
        if w <= 1 or h <= 1:
            return
        try:
            self.setUpdatesEnabled(False)
        except Exception:
            pass
        target_w = 1
        target_h = 1
        self.resize(target_w, target_h)
        def _restore():
            self.resize(w, h)
            try:
                self.setUpdatesEnabled(True)
            except Exception:
                pass
            try:
                _force_dwm_redraw(int(self.winId()))
            except Exception:
                pass
        QTimer.singleShot(0, _restore)
        self._did_hard_refresh = True

    def _schedule_backdrop_apply(self):
        if sys.platform != "win32":
            return
        for delay in (0, 200, 800):
            QTimer.singleShot(delay, self._apply_backdrop)

    def update_theme_mode(self, theme_mode):
        self._theme_mode = theme_mode
        self._apply_page_background()
        self._schedule_backdrop_apply()

    def apply_backdrop_settings(self):
        self._apply_page_background()
        self._schedule_backdrop_apply()

    def showEvent(self, event):
        super().showEvent(event)
        if self._pending_url is not None:
            self.load(self._pending_url)
            self._pending_url = None
        self._apply_page_background()
        self._schedule_backdrop_apply()
        QTimer.singleShot(50, self._force_refresh)
        QTimer.singleShot(120, lambda: _force_dwm_redraw(int(self.winId())))
        if self._is_system_backdrop_enabled():
            QTimer.singleShot(180, self._force_webview_transparent)
            QTimer.singleShot(220, self._force_webview_repaint)
            QTimer.singleShot(260, self._hard_resize_nudge)
    
    def closeEvent(self, event):
        """Clean up timers and resources when window closes"""
        if self._crash_recovery_timer is not None:
            try:
                self._crash_recovery_timer.stop()
            except Exception:
                pass
            self._crash_recovery_timer = None
        super().closeEvent(event)

def apply_win11_aesthetics(window, theme_mode=None, settings=None, window_tag=""):
    if sys.platform == "win32":
        try:
            hwnd = int(window.winId())
            dwmapi = ctypes.windll.dwmapi
            corner_preference = ctypes.c_int(2)
            dwmapi.DwmSetWindowAttribute(
                hwnd,
                33,
                ctypes.byref(corner_preference),
                ctypes.sizeof(corner_preference)
            )
            _apply_window_theme(hwnd, _resolve_theme_dark(theme_mode))
            backdrop_type = _resolve_system_backdrop_type(settings, window_tag)
            _apply_system_backdrop(hwnd, backdrop_type)
            if backdrop_type is not None and backdrop_type != DWMSBT_NONE:
                border = ctypes.c_int(_DWM_COLOR_NONE)
                dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ctypes.byref(border), ctypes.sizeof(border))
            icon_path = _resolve_logo_ico_path()
            if icon_path and os.path.exists(icon_path):
                user32 = ctypes.windll.user32
                IMAGE_ICON = 1
                LR_LOADFROMFILE = 0x00000010
                WM_SETICON = 0x0080
                ICON_SMALL = 0
                ICON_BIG = 1

                hicon_small = user32.LoadImageW(0, icon_path, IMAGE_ICON, 16, 16, LR_LOADFROMFILE)
                hicon_big = user32.LoadImageW(0, icon_path, IMAGE_ICON, 32, 32, LR_LOADFROMFILE)
                if hicon_small:
                    user32.SendMessageW(hwnd, WM_SETICON, ICON_SMALL, hicon_small)
                if hicon_big:
                    user32.SendMessageW(hwnd, WM_SETICON, ICON_BIG, hicon_big)
        except Exception:
            pass

def main():
    _apply_chromium_flags()
    QCoreApplication.setAttribute(Qt.AA_UseHighDpiPixmaps, True)
    app = QApplication(sys.argv)
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
                    default_theme = settings.get("Appearance", {}).get("ThemeMode", "Auto").lower()
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
                    dialog_data = {
                        "title": "程序崩溃了 (´；ω；`) ",
                        "text": error_msg,
                        "isError": True,
                        "confirmText": "关闭",
                        "cancelText": "复制错误",
                        "theme": default_theme,
                        "accentColor": default_accent
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
            html_path = os.path.join(os.path.dirname(base_dir), "ppt_assistant", "ui", "crash_dialog.html")
            win_width = 900
            win_height = 600
        else:
            html_path = os.path.join(os.path.dirname(base_dir), "ppt_assistant", "ui", "dialog.html")
            win_width = 650
            win_height = 500
        defer_load = os.environ.get("DEFER_WEBENGINE_LOAD", "").strip().lower() in ["1", "true", "yes", "on"]
        window = MainWindow(
            dialog_data.get("title", "Dialog"),
            html_path,
            api,
            win_width,
            win_height,
            dialog_data.get("theme", default_theme),
            defer_load=defer_load
        )
        window.show()
    elif len(sys.argv) >= 5:
        url = sys.argv[1]
        title = sys.argv[2]
        width = int(sys.argv[3])
        height = int(sys.argv[4])
        custom_border = False
        if len(sys.argv) >= 6:
            custom_border = str(sys.argv[5]).strip().lower() in ["1", "true", "yes", "on"]
        api = Api()
        settings_path = os.environ.get("SETTINGS_PATH")
        if not settings_path:
            if getattr(sys, "frozen", False):
                settings_path = os.path.join(os.path.dirname(sys.executable), "settings.json")
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
        defer_load = os.environ.get("DEFER_WEBENGINE_LOAD", "").strip().lower() in ["1", "true", "yes", "on"]
        window = MainWindow(title, url, api, width, height, theme_mode, custom_border, defer_load)
        if title == "Settings":
            window.setMinimumWidth(1099)
        window.show()
    else:
        return
    sys.exit(app.exec())

if __name__ == "__main__":
    main()
