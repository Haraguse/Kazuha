import os
import sys
import math
import json
import ctypes
import tempfile
import importlib.util
from typing import Optional
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtCore import QObject, Slot, Signal, Qt, QUrl, QTimer, QRect
from PySide6.QtGui import QColor, QRegion, QGuiApplication
from PySide6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QLabel
from ppt_assistant.core.config import cfg, ROOT_DIR
from qfluentwidgets import Theme, isDarkTheme, MessageBox, themeColor
from ppt_assistant.core.i18n import t
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.icon_helper import get_file_icon_base64
from ppt_assistant.core.platform_integration import open_path
from ppt_assistant.core.system import get_system_api
import psutil
import threading

PLUGIN_DIR = os.path.join(ROOT_DIR, "plugins", "builtins")


def _skip_webengine_import_at_startup() -> bool:
    if sys.platform != "linux":
        return False
    force_webengine = (
        str(os.environ.get("LUMINALIUM_ENABLE_WEBENGINE_OVERLAY", "")).strip().lower()
    )
    if force_webengine in ("1", "true", "yes", "on"):
        return False
    if not os.environ.get("DISPLAY"):
        return False
    qpa = str(os.environ.get("QT_QPA_PLATFORM", "")).strip().lower()
    marker = str(os.environ.get("LUMINALIUM_XWAYLAND_SESSION", "")).strip().lower()
    return qpa.startswith("xcb") and marker in ("1", "true", "yes", "on")


if _skip_webengine_import_at_startup():
    QWebEngineView = QWidget
    _WEBENGINE_IMPORT_SKIPPED = True
else:
    from PySide6.QtWebEngineWidgets import QWebEngineView

    _WEBENGINE_IMPORT_SKIPPED = False


def _thumbnail_source_to_url(source):
    text = str(source or "")
    if text.lower().startswith(("data:", "file:", "http://", "https://", "blob:")):
        return text
    return QUrl.fromLocalFile(text).toString()


def _qt_platform_name() -> str:
    app = QGuiApplication.instance()
    if app is not None:
        try:
            return str(app.platformName() or "").strip().lower()
        except Exception:
            return ""
    return ""


def _is_wayland_backend() -> bool:
    platform_name = _qt_platform_name()
    if platform_name:
        if platform_name.startswith("wayland"):
            return True
        if platform_name.startswith("xcb"):
            return False
    qpa = str(os.environ.get("QT_QPA_PLATFORM", "")).strip().lower()
    if qpa:
        return qpa.startswith("wayland")
    return bool(os.environ.get("WAYLAND_DISPLAY"))


def _is_wayland_compositor_session() -> bool:
    marker = str(os.environ.get("LUMINALIUM_XWAYLAND_SESSION", "")).strip().lower()
    if marker in ("1", "true", "yes", "on"):
        return True
    platform_name = _qt_platform_name()
    if platform_name.startswith("wayland"):
        return True
    if bool(os.environ.get("WAYLAND_DISPLAY")):
        return True
    qpa = str(os.environ.get("QT_QPA_PLATFORM", "")).strip().lower()
    return qpa.startswith("wayland")


def _should_use_webengine_overlay() -> bool:
    if not _is_wayland_backend():
        return True
    value = (
        str(os.environ.get("LUMINALIUM_ENABLE_WEBENGINE_OVERLAY", "")).strip().lower()
    )
    return value in ("1", "true", "yes", "on")


def _should_use_linux_compat_overlay_page() -> bool:
    if sys.platform != "linux":
        return False
    if not _is_wayland_compositor_session():
        return False
    value = (
        str(os.environ.get("LUMINALIUM_FORCE_FULL_OVERLAY_HTML", "")).strip().lower()
    )
    return value not in ("1", "true", "yes", "on")


def _should_use_linux_widget_overlay() -> bool:
    if sys.platform != "linux":
        return False
    if not bool(os.environ.get("DISPLAY")):
        return False
    if not _is_wayland_compositor_session():
        return False
    force_webengine = (
        str(os.environ.get("LUMINALIUM_ENABLE_WEBENGINE_OVERLAY", "")).strip().lower()
    )
    return force_webengine not in ("1", "true", "yes", "on")


def _linux_overlay_backend() -> str:
    value = str(os.environ.get("LUMINALIUM_LINUX_OVERLAY_BACKEND", "")).strip().lower()
    if value in ("widget", "qwidget", "compat", "safe"):
        return "widget"
    return "qml"


class OverlayBridge(QObject):
    def __init__(self, overlay):
        super().__init__()
        self._overlay = overlay

    @Slot()
    def requestInitState(self):
        QTimer.singleShot(0, self._overlay.apply_initial_state)

    @Slot(int, int, int)
    def setPenColor(self, r, g, b):
        self._overlay.request_pen_color.emit(r, g, b)

    @Slot(str)
    def setTool(self, tool_name):
        if tool_name == "select" or tool_name == "arrow":
            self._overlay.request_ptr_arrow.emit()
        elif tool_name == "pen":
            self._overlay.request_ptr_pen.emit()
        elif tool_name == "eraser":
            self._overlay.request_ptr_eraser.emit()

    @Slot()
    def prevPage(self):
        self._overlay.request_prev.emit()

    @Slot()
    def nextPage(self):
        self._overlay.request_next.emit()

    @Slot(int)
    def gotoSlide(self, index):
        self._overlay.request_goto.emit(index)

    @Slot()
    def clearScreen(self):
        self._overlay.request_clear.emit()

    @Slot()
    def endShow(self):
        print("[Bridge] endShow() called, emitting request_end", flush=True)
        self._overlay.request_end.emit()

    @Slot()
    def mediaPlayPause(self):
        try:
            import ctypes
            ctypes.windll.user32.keybd_event(0xB3, 0, 0, 0)
            ctypes.windll.user32.keybd_event(0xB3, 0, 2, 0)
        except Exception as e:
            print(f"mediaPlayPause error: {e}")

    @Slot()
    def mediaPrev(self):
        try:
            import ctypes
            ctypes.windll.user32.keybd_event(0xB1, 0, 0, 0)
            ctypes.windll.user32.keybd_event(0xB1, 0, 2, 0)
        except Exception as e:
            print(f"mediaPrev error: {e}")

    @Slot()
    def mediaNext(self):
        try:
            import ctypes
            ctypes.windll.user32.keybd_event(0xB0, 0, 0, 0)
            ctypes.windll.user32.keybd_event(0xB0, 0, 2, 0)
        except Exception as e:
            print(f"mediaNext error: {e}")

    @Slot(str)
    def inkPromptResult(self, result):
        # Defer emission to allow the WebChannel return handshake to complete
        QTimer.singleShot(0, lambda: self._emit_ink_prompt_result(result))

    def _emit_ink_prompt_result(self, result):
        # Convert string result to boolean
        if result == "true":
            self._overlay.ink_prompt_result.emit(True)
        else:
            self._overlay.ink_prompt_result.emit(False)

    @Slot()
    def toggleSpotlight(self):
        self._overlay.execute_plugin("聚光灯")

    @Slot()
    def toggleBoard(self):
        self._overlay.execute_plugin("板中板")

    @Slot()
    def toggleTimer(self):
        self._overlay.execute_plugin("计时器")

    @Slot(str)
    def launchApp(self, path):
        if not path:
            return
        try:
            open_path(path)
        except Exception as e:
            print(f"Failed to launch app {path}: {e}")

    @Slot("QVariantList")
    def updateMask(self, rects):
        self._overlay.update_mask(rects)

    @Slot()
    def releaseFocus(self):
        try:
            self._overlay.clearFocus()
        except Exception:
            pass

    @Slot()
    def resizeNudge(self):
        try:
            self._overlay.nudge_size()
        except Exception:
            pass

    @Slot(int)
    def requestThumbnail(self, index):
        self._overlay.request_thumbnail.emit(index)

    @Slot(int)
    def startBackgroundThumbnailCaching(self, total_pages):
        self._overlay.start_background_caching.emit(total_pages)

    @Slot(result=str)
    def getTimerState(self):
        """Get current timer state as JSON string."""
        if not self._overlay._timer_manager:
            return '{"isRunning": false, "remainingSeconds": 0, "totalSeconds": 0, "displayText": ""}'

        try:
            timer_mgr = self._overlay._timer_manager
            is_running = timer_mgr.is_running
            remaining_seconds = max(0, int(timer_mgr.remaining_seconds))
            total_seconds = max(0, int(timer_mgr.total_seconds))

            # Format time display as MM:SS
            minutes = remaining_seconds // 60
            seconds = remaining_seconds % 60
            display_text = f"{minutes:02d}:{seconds:02d}" if total_seconds > 0 else ""

            import json
            return json.dumps({
                "isRunning": is_running,
                "remainingSeconds": remaining_seconds,
                "totalSeconds": total_seconds,
                "displayText": display_text
            })
        except Exception as e:
            print(f"[Bridge] Error getting timer state: {e}", file=sys.stderr)
            return '{"isRunning": false, "remainingSeconds": 0, "totalSeconds": 0, "displayText": ""}'


class InkPromptWindow(QWidget):
    """Independent QFluentWidgets-based dialog for ink annotation prompt."""
    result = Signal(bool)  # True (keep) or False (discard)

    def __init__(self, texts, parent=None):
        # NOTE: Do NOT pass parent here — creating a child widget of QWebEngineView
        # (a DWM layered window) and then closing it invalidates the parent's alpha
        # compositing layer, causing the overlay background to turn solid black.
        super().__init__(None)
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.Tool | Qt.WindowStaysOnTopHint)
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_DeleteOnClose)

        # Make it full screen
        screen = QGuiApplication.primaryScreen()
        if screen:
            self.setGeometry(screen.geometry())

        from ppt_assistant.core.platform_integration import remove_window_border
        remove_window_border(self.winId())

        # Semi-transparent black background
        self.bg_color = QColor(0, 0, 0, 140)

        # Create the dialog
        self._dialog = self._create_dialog(texts)
        
    def _create_dialog(self, texts):
        """Create a standard dialog with two buttons using qfluentwidgets Dialog."""
        from qfluentwidgets import Dialog
        
        dialog = Dialog(texts["title"], texts["text"], self)
        
        dialog.yesButton.setText(texts.get("keep", "保留"))
        dialog.cancelButton.setText(texts.get("discard", "不保留"))
        
        dialog.yesButton.clicked.connect(lambda: self._on_result(True))
        dialog.cancelButton.clicked.connect(lambda: self._on_result(False))
        
        if hasattr(dialog, "maskWidget"):
            dialog.maskWidget.deleteLater()
            dialog.maskWidget = None
        
        return dialog

    def paintEvent(self, event):
        from PySide6.QtGui import QPainter

        painter = QPainter(self)
        painter.fillRect(self.rect(), self.bg_color)

    def showEvent(self, event):
        super().showEvent(event)
        self._play_error_sound()
        self._dialog.show()
        self._dialog.raise_()
        self._dialog.activateWindow()
        center_x = (self.width() - self._dialog.width()) // 2
        center_y = (self.height() - self._dialog.height()) // 2
        self._dialog.move(center_x, center_y)

    def _play_error_sound(self):
        """Play Windows error sound using Windows API."""
        try:
            import ctypes
            from ctypes import wintypes

            # Load winmm.dll
            winmm = ctypes.WinDLL('winmm.dll')

            # Define PlaySound function signature
            # PlaySound(lpzSound, hmod, fdwSound)
            # lpzSound: sound name (can be filename or system event alias)
            # hmod: module handle (0 for NULL)
            # fdwSound: flags
            SND_FILENAME = 0x00020000  # Name is a file name
            SND_ASYNC = 0x0001  # Play asynchronously
            SND_NODEFAULT = 0x0002  # Do not use default sound

            # Play the Windows Error sound
            sound_path = r"C:\Windows\Media\Windows Error.wav"
            result = winmm.PlaySoundW(sound_path, 0, SND_FILENAME | SND_ASYNC | SND_NODEFAULT)

            if result == 0:
                print(f"[InkPrompt] Failed to play sound: {sound_path}", flush=True)
        except Exception as e:
            print(f"[InkPrompt] Error playing sound: {e}", flush=True)

    def _on_result(self, result):
        self.result.emit(result)
        # Close the dialog first, then close the parent window
        if hasattr(self, '_dialog') and self._dialog:
            self._dialog.close()
        self.close()


class WaylandFallbackOverlayWindow(QWidget):
    request_next = Signal()
    request_prev = Signal()
    request_goto = Signal(int)
    request_clear = Signal()
    request_end = Signal()
    request_ptr_arrow = Signal()
    request_ptr_pen = Signal()
    request_ptr_eraser = Signal()
    request_pen_color = Signal(int, int, int)
    request_thumbnail = Signal(int)
    start_background_caching = Signal(int)
    ink_prompt_result = Signal(bool)
    thumbnail_ready = Signal(int, str)

    def __init__(self):
        super().__init__()
        self.monitor = None
        self.plugins = []
        self._icon_cache = {}
        self._is_light = False
        self._slideshow_hwnd = 0
        self._protected_view = False
        self._presentation_readonly = False
        self._active_on_slideshow = False
        self._warned_visibility = False
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.Window)
        screen = QGuiApplication.primaryScreen()
        if screen:
            self.setGeometry(screen.geometry())
        icon = load_app_icon()
        if not icon.isNull():
            self.setWindowIcon(icon)
        print(
            "[Overlay] WebEngine overlay disabled on Wayland. Set LUMINALIUM_ENABLE_WEBENGINE_OVERLAY=1 to force-enable it."
        )

    def _log_visibility_skip(self):
        if self._warned_visibility:
            return
        self._warned_visibility = True
        print(
            "[Overlay] Wayland fallback overlay is active; overlay window rendering is disabled to avoid native crashes."
        )

    def apply_initial_state(self):
        pass

    def nudge_size(self):
        pass

    def set_monitor(self, monitor):
        self.monitor = monitor
        if monitor and hasattr(monitor, "set_overlay"):
            monitor.set_overlay(self)

    def on_thumbnail_ready(self, index, path):
        pass

    def on_start_background_caching(self, total_pages):
        pass

    def on_slide_changed(self, current, total):
        pass

    def update_page_info(self, current, total):
        pass

    def update_mask(self, rects_data):
        pass

    def update_theme(self):
        pass

    def update_config(self):
        pass

    def update_accent_color(self, hex_color):
        pass

    def reset_tool_state_ui(self, tool: str = "select"):
        pass

    def reset_pen_color_ui(self):
        pass

    def show_ink_prompt(self):
        self.ink_prompt_result.emit(False)

    def execute_plugin(self, name):
        pass

    def bind_config_signals(self):
        pass

    def show(self):
        self._log_visibility_skip()

    def hide(self):
        pass

    def raise_(self):
        pass

    def isVisible(self):
        return False

    def set_active_on_slideshow(self, active: bool, animate: bool = True):
        self._active_on_slideshow = bool(active)
        if active:
            self._log_visibility_skip()

    def on_slideshow_start_cleanup(self):
        pass

    def on_slideshow_end_cleanup(self):
        pass

    def _mark_ui_alive(self):
        pass

    def bind_monitor_signals(self):
        pass

    def show_reload_mask(self, text=""):
        pass

    def hide_reload_mask(self):
        pass

    def set_slideshow_hwnd(self, hwnd):
        try:
            self._slideshow_hwnd = int(hwnd or 0)
        except Exception:
            self._slideshow_hwnd = 0

    def set_ppt_restrictions(self, protected_view: bool, presentation_readonly: bool):
        self._protected_view = bool(protected_view)
        self._presentation_readonly = bool(presentation_readonly)

    def cleanup(self):
        pass

    def update_geometry(self, rect, screen):
        pass


class OverlayWindow(QWebEngineView):
    request_next = Signal()
    request_prev = Signal()
    request_goto = Signal(int)
    request_clear = Signal()
    request_end = Signal()
    request_ptr_arrow = Signal()
    request_ptr_pen = Signal()
    request_ptr_eraser = Signal()
    request_pen_color = Signal(int, int, int)
    request_thumbnail = Signal(int)
    start_background_caching = Signal(int)  # total_pages
    ink_prompt_result = Signal(bool)
    thumbnail_ready = Signal(int, str)

    def __init__(self):
        super().__init__()

        self._page_ready = False
        self._pending_scripts = []
        self._runtime_initialized = False
        self._post_load_initialized = False
        self._webengine_env_override = None
        self._wayland_compatible_mode = _is_wayland_compositor_session()
        self._linux_compat_page_mode = _should_use_linux_compat_overlay_page()

        # Background thumbnail caching
        self._background_thumbnail_timer = None
        self._pending_thumbnails = []
        self._cached_thumbnails = set()
        self._zorder_timer = None

        if self._wayland_compatible_mode:
            self.setWindowFlags(Qt.FramelessWindowHint | Qt.Window)
            self.setAttribute(Qt.WA_TranslucentBackground, False)
            self.setAttribute(Qt.WA_NoSystemBackground, False)
            self.setStyleSheet("background: #101010;")
            print(
                "[Overlay] Wayland/XWayland compatibility mode enabled: "
                "using conservative window flags and opaque background."
            )
        else:
            if cfg.allowRecording.value:
                self.setWindowFlags(
                    Qt.FramelessWindowHint
                    | Qt.Window
                    | Qt.WindowStaysOnTopHint
                )
            else:
                self.setWindowFlags(
                    Qt.FramelessWindowHint
                    | Qt.WindowDoesNotAcceptFocus
                    | Qt.Tool
                    | Qt.WindowStaysOnTopHint
                )
            self.setAttribute(Qt.WA_TranslucentBackground)
            self.setAttribute(Qt.WA_NoSystemBackground)

        if sys.platform == "win32":
            from ppt_assistant.core.platform_integration import remove_window_border_delayed
            remove_window_border_delayed(self)

        self.monitor = None
        self._timer_manager = None
        self.plugins = []
        self._icon_cache = {}
        self._is_light = False
        self._slideshow_hwnd = 0
        self._protected_view = False
        self._presentation_readonly = False
        self._active_on_slideshow = False
        self._current_ink_dialog = None

        self._smtc_info = {
            "status": "",
            "title": "",
            "position_ms": 0,
            "duration_ms": 0,
        }
        self._last_sent_smtc_artwork_data_url = None
        self._smtc_thread = None
        self._stop_smtc = False
        self.status_timer = None

        screen = QGuiApplication.primaryScreen()
        if screen:
            self.setGeometry(screen.geometry())

        icon = load_app_icon()
        if not icon.isNull():
            self.setWindowIcon(icon)

        # Crash recovery tracking
        self._render_crash_count = 0
        self._max_reload_attempts = 3
        self._crash_recovery_timer = None
        self._memory_timer = None

    def update_accent_color(self, hex_color):
        if self.page():
            self.page().runJavaScript(f"if(window.updateAccentColor) updateAccentColor('{hex_color}');")

    def _resolve_theme_path(self) -> str:
        theme_name = cfg.overlayTheme.value
        root_dir = ROOT_DIR

        def resolve_user_theme_path(name: str) -> Optional[str]:
            # Support both "user" and "users" folder names
            theme_dir = os.path.join(root_dir, "user", "themes", name)
            if not os.path.isdir(theme_dir):
                theme_dir = os.path.join(root_dir, "users", "themes", name)

            if not os.path.isdir(theme_dir):
                return None
            manifest_path = os.path.join(theme_dir, "manifest.json")
            preview_png = os.path.join(theme_dir, "preview.png")
            preview_jpg = os.path.join(theme_dir, "preview.jpg")
            html_path = os.path.join(theme_dir, "index.html")
            if not os.path.exists(html_path):
                return None
            if not os.path.exists(manifest_path):
                return None
            if not (os.path.exists(preview_png) or os.path.exists(preview_jpg)):
                return None
            try:
                with open(manifest_path, "r", encoding="utf-8-sig") as f:
                    data = json.load(f)
                # Remove strict name check to allow more flexible theme naming
                # if data.get("name") != name:
                #     return None
            except Exception:
                return None
            return html_path

        theme_path = resolve_user_theme_path(theme_name)
        if not theme_path:
            theme_path = resolve_user_theme_path("default")
        if not theme_path:
            theme_path = os.path.join(root_dir, "themes", theme_name, "index.html")
        if not os.path.exists(theme_path):
            theme_path = os.path.join(root_dir, "themes", "default", "index.html")
        if not os.path.exists(theme_path):
            theme_path = os.path.join(
                os.path.dirname(os.path.abspath(__file__)), "overlay.html"
            )
        return theme_path

    def _resolve_runtime_page_path(self) -> str:
        if self._linux_compat_page_mode:
            compat_path = os.path.join(
                os.path.dirname(os.path.abspath(__file__)), "overlay_linux_compat.html"
            )
            if os.path.exists(compat_path):
                return compat_path
        return self._resolve_theme_path()

    def _inject_theme_scripts(self):
        try:
            from PySide6.QtWebEngineCore import QWebEngineScript

            try:
                from plugins.webview_runner import _get_unified_theme_js
            except ImportError:
                _plugins_dir = os.path.join(ROOT_DIR, "plugins")
                if _plugins_dir not in sys.path:
                    sys.path.insert(0, _plugins_dir)
                from plugins.webview_runner import _get_unified_theme_js

            mode = cfg.themeMode.value
            if isinstance(mode, Theme):
                if mode == Theme.AUTO:
                    is_dark = isDarkTheme()
                else:
                    is_dark = mode == Theme.DARK
            else:
                mode_str = str(mode).lower()
                if mode_str == "dark":
                    is_dark = True
                elif mode_str == "light":
                    is_dark = False
                else:
                    is_dark = isDarkTheme()

            theme_id = cfg.themeId.value or "default"
            settings_dict = {
                "Appearance": {
                    "ResolvedIsDark": is_dark,
                    "ThemeId": theme_id,
                    "ThemeMode": "Dark" if is_dark else "Light",
                }
            }
            settings_json = json.dumps(settings_dict, ensure_ascii=False)

            settings_script = QWebEngineScript()
            settings_script.setSourceCode(f"window.initialSettings = {settings_json};")
            settings_script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
            settings_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
            self.page().scripts().insert(settings_script)

            theme_script = QWebEngineScript()
            theme_script.setSourceCode(_get_unified_theme_js())
            theme_script.setInjectionPoint(QWebEngineScript.InjectionPoint.DocumentCreation)
            theme_script.setWorldId(QWebEngineScript.ScriptWorldId.MainWorld)
            self.page().scripts().insert(theme_script)

            print("[Overlay] Theme scripts injected.", flush=True)
        except Exception as e:
            print(f"[Overlay] Failed to inject theme scripts: {e}", file=sys.stderr)

    def _ensure_runtime_initialized(self):
        if self._runtime_initialized:
            return
        self._runtime_initialized = True
        print("[Overlay] Initializing WebEngine runtime...", flush=True)
        try:
            profile = self.page().profile()
            cache_path = os.path.join(tempfile.gettempdir(), "luminalium_overlay_cache")

            if os.path.exists(cache_path):
                try:
                    import shutil

                    cache_size = sum(
                        os.path.getsize(os.path.join(dirpath, filename))
                        for dirpath, dirnames, filenames in os.walk(cache_path)
                        for filename in filenames
                    )
                    if cache_size > 500 * 1024 * 1024:
                        print(
                            f"[Overlay] Clearing corrupted cache ({cache_size / 1024 / 1024:.1f}MB)",
                            file=sys.stderr,
                        )
                        shutil.rmtree(cache_path, ignore_errors=True)
                except Exception as e:
                    print(f"[Overlay] Error checking cache: {e}", file=sys.stderr)

            try:
                profile.setCachePath(cache_path)
                profile.setPersistentStoragePath(cache_path)
                profile.setHttpCacheType(profile.HttpCacheType.DiskHttpCache)
                profile.setHttpCacheMaximumSize(50 * 1024 * 1024)
            except Exception as e:
                print(f"[Overlay] Failed to set disk cache, falling back to memory: {e}")
                profile.setHttpCacheType(profile.HttpCacheType.MemoryHttpCache)

            settings = self.page().settings()
            from PySide6.QtWebEngineCore import QWebEngineSettings

            for attr_name, enabled in [
                ("LocalStorageEnabled", False),
                ("SessionStorageEnabled", False),
                ("WebGLEnabled", False),
                ("Accelerated2dCanvasEnabled", False),
                ("ScrollAnimatorEnabled", not cfg.disableAnimations.value),
                ("LocalContentCanAccessFileUrls", True),
                ("LocalContentCanAccessRemoteUrls", True),
            ]:
                attr = getattr(QWebEngineSettings.WebAttribute, attr_name, None)
                if attr is not None:
                    settings.setAttribute(attr, enabled)
        except Exception as e:
            print(f"[Overlay] Error configuring profile: {e}", file=sys.stderr)

        if self._wayland_compatible_mode:
            self.page().setBackgroundColor(QColor("#101010"))
        else:
            self.page().setBackgroundColor(Qt.transparent)

        self.channel = QWebChannel()
        self.bridge = OverlayBridge(self)
        self.channel.registerObject("bridge", self.bridge)
        self.page().setWebChannel(self.channel)
        print("[Overlay] WebChannel ready.", flush=True)

        self._inject_theme_scripts()

        self.loadFinished.connect(self._on_load_finished)
        self.renderProcessTerminated.connect(self._on_render_process_terminated)

        theme_path = self._resolve_runtime_page_path()
        self._apply_linux_webengine_env_override()
        if self._linux_compat_page_mode:
            print(
                "[Overlay] Linux compatibility overlay page enabled for XWayland stability.",
                flush=True,
            )
        print(f"[Overlay] Loading overlay page: {theme_path}", flush=True)
        self.load(QUrl.fromLocalFile(theme_path))

    def _apply_linux_webengine_env_override(self):
        if self._webengine_env_override is not None:
            return
        if sys.platform != "linux":
            return
        platform_name = _qt_platform_name()
        if not platform_name.startswith("xcb"):
            return
        override = {}
        if "WAYLAND_DISPLAY" in os.environ:
            override["WAYLAND_DISPLAY"] = os.environ.get("WAYLAND_DISPLAY")
            os.environ.pop("WAYLAND_DISPLAY", None)
        current_session_type = os.environ.get("XDG_SESSION_TYPE")
        if current_session_type != "x11":
            override["XDG_SESSION_TYPE"] = current_session_type
            os.environ["XDG_SESSION_TYPE"] = "x11"
        if not override:
            return
        self._webengine_env_override = override
        print(
            "[Overlay] Applied temporary X11 environment override for WebEngine runtime.",
            flush=True,
        )

    def _restore_linux_webengine_env_override(self):
        if self._webengine_env_override is None:
            return
        override = dict(self._webengine_env_override)
        self._webengine_env_override = None
        for key, previous in override.items():
            if previous is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = previous
        print(
            "[Overlay] Restored environment after WebEngine startup.",
            flush=True,
        )

    def _run_javascript(self, script: str):
        if not script:
            return
        try:
            page = self.page()
        except RuntimeError:
            print(f"[Overlay] _run_javascript: RuntimeError getting page", flush=True)
            return
        if page is None:
            print(f"[Overlay] _run_javascript: page is None", flush=True)
            return
        if not self._page_ready:
            print(f"[Overlay] _run_javascript: page not ready, caching script", flush=True)
            self._pending_scripts.append(script)
            return
        print(f"[Overlay] _run_javascript: executing script", flush=True)
        page.runJavaScript(script)

    def _flush_pending_scripts(self):
        if not self._page_ready or not self._pending_scripts:
            return
        pending = self._pending_scripts
        self._pending_scripts = []
        for script in pending:
            try:
                self.page().runJavaScript(script)
            except RuntimeError:
                break

    def _on_load_finished(self, ok: bool):
        self._restore_linux_webengine_env_override()
        self._page_ready = bool(ok)
        print(f"[Overlay] loadFinished ok={bool(ok)}", flush=True)
        if not self._wayland_compatible_mode:
            try:
                self.page().setBackgroundColor(Qt.transparent)
            except Exception:
                pass
        if self._page_ready:
            self._ensure_post_load_initialized()
            self._flush_pending_scripts()

    def _ensure_post_load_initialized(self):
        if self._post_load_initialized:
            return
        self._post_load_initialized = True
        print("[Overlay] Initializing post-load services...", flush=True)
        self._start_smtc_thread()
        self.load_plugins()
        self.bind_config_signals()
        if self.status_timer is None:
            self.status_timer = QTimer(self)
            self.status_timer.timeout.connect(self._update_system_status)
            self.status_timer.start(2000)
        self.apply_initial_state()
        print("[Overlay] Post-load services ready.", flush=True)

    def apply_initial_state(self):
        if self.monitor:
            pass
        self.reset_pen_color_ui()
        self.reset_tool_state_ui()
        self.update_theme()
        self.update_config()
        self._apply_zorder_timer()

    def _ensure_topmost(self):
        if sys.platform != "win32":
            return
        try:
            user32 = ctypes.windll.user32
            hwnd = int(self.winId())
            if not hwnd:
                return
            HWND_TOPMOST = -1
            SWP_NOMOVE = 0x0002
            SWP_NOSIZE = 0x0001
            SWP_NOACTIVATE = 0x0010
            SWP_SHOWWINDOW = 0x0040

            if cfg.uiAccessTopmost.value:
                GWL_EXSTYLE = -20
                WS_EX_TOPMOST = 0x00000008
                current_style = user32.GetWindowLongW(hwnd, GWL_EXSTYLE)
                user32.SetWindowLongW(hwnd, GWL_EXSTYLE, current_style | WS_EX_TOPMOST)

            result = user32.SetWindowPos(
                hwnd,
                HWND_TOPMOST,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW,
            )
        except Exception as e:
            print(f"[Overlay] Error in _ensure_topmost: {e}")

    def _apply_zorder_timer(self):
        interval = cfg.zOrderCheckInterval.value
        if interval > 0:
            if self._zorder_timer is None:
                self._zorder_timer = QTimer(self)
                self._zorder_timer.timeout.connect(self._ensure_topmost)
            self._zorder_timer.start(interval)
        else:
            if self._zorder_timer is not None:
                self._zorder_timer.stop()

    def _on_render_process_terminated(self, status, exit_code):
        self._restore_linux_webengine_env_override()
        status_name = ""
        try:
            status_name = str(getattr(status, "name", status))
        except Exception:
            status_name = str(status)
        print(
            f"[Overlay] Render process terminated: status={status}, exit_code={exit_code}"
        )
        if "NormalTerminationStatus" in status_name and int(exit_code or 0) == 0:
            print("[Overlay] Renderer ended normally; skipping crash recovery.")
            return
        self._render_crash_count += 1
        print(
            f"[Overlay] Crash #{self._render_crash_count}/{self._max_reload_attempts}"
        )

        # If too many crashes, disable GPU and retry once, then give up
        if self._render_crash_count > self._max_reload_attempts:
            print(
                f"[Overlay] Too many crashes ({self._render_crash_count}). Giving up on recovery."
            )
            return

        # Use exponential backoff: 100ms, 500ms, 1500ms
        delay = min(100 * (2 ** (self._render_crash_count - 1)), 2000)
        print(f"[Overlay] Scheduling reload in {delay}ms")

        # If this is the second crash, try disabling GPU acceleration
        if self._render_crash_count == 2:
            try:
                print("[Overlay] Disabling GPU acceleration due to repeated crashes")
                settings = self.page().settings()
                from PySide6.QtWebEngineCore import QWebEngineSettings

                settings.setAttribute(
                    QWebEngineSettings.WebAttribute.Accelerated2dCanvasEnabled, False
                )
                settings.setAttribute(
                    QWebEngineSettings.WebAttribute.WebGLEnabled, False
                )
            except Exception as e:
                print(f"[Overlay] Error disabling GPU: {e}")

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

    def nudge_size(self):
        try:
            w = self.width()
            h = self.height()
            self.resize(w + 1, h)
            QTimer.singleShot(0, lambda: self.resize(w, h))
        except Exception:
            pass

    def set_monitor(self, monitor):
        self.monitor = monitor
        if monitor and hasattr(monitor, "set_overlay"):
            monitor.set_overlay(self)
        self.monitor.slide_changed.connect(self.on_slide_changed)
        self.thumbnail_ready.connect(self.on_thumbnail_ready)
        self.start_background_caching.connect(self.on_start_background_caching)
        if hasattr(self.monitor, "animation_step_changed"):
            self.monitor.animation_step_changed.connect(self.on_animation_step_changed)

    def set_timer_manager(self, timer_manager):
        """Set the timer manager for status bar updates."""
        self._timer_manager = timer_manager
        if timer_manager:
            # Connect to timer updates to refresh status bar
            timer_manager.updated.connect(self._on_timer_updated)
            timer_manager.state_changed.connect(self._on_timer_state_changed)

    def _on_timer_updated(self, remaining_ms):
        """Triggered when timer is updated."""
        try:
            script = "if (typeof updateTimerStatus === 'function') updateTimerStatus();"
            self.page().runJavaScript(script)
        except Exception as e:
            print(f"[Overlay] Error updating timer status: {e}", file=sys.stderr)

    def _on_timer_state_changed(self, is_running):
        """Triggered when timer state changes."""
        try:
            script = "if (typeof updateTimerStatus === 'function') updateTimerStatus();"
            self.page().runJavaScript(script)
        except Exception as e:
            print(f"[Overlay] Error updating timer state: {e}", file=sys.stderr)

    def on_thumbnail_ready(self, index, path):
        # Mark as cached
        self._cached_thumbnails.add(index)

        url = _thumbnail_source_to_url(path)
        script = (
            "if (typeof updatePageThumbnail === 'function') "
            f"updatePageThumbnail({int(index)}, {json.dumps(url)});"
        )
        self._run_javascript(script)

        # Start next background caching task if available
        self._process_next_background_thumbnail()

    def on_start_background_caching(self, total_pages):
        """Start background caching of thumbnails"""
        if self._background_thumbnail_timer is not None:
            return  # Already running

        # Build list of pages to cache (excluding already cached ones)
        self._pending_thumbnails = [
            i for i in range(1, total_pages + 1) if i not in self._cached_thumbnails
        ]

        # Start timer for background caching
        self._background_thumbnail_timer = QTimer(self)
        self._background_thumbnail_timer.setSingleShot(False)
        self._background_thumbnail_timer.timeout.connect(
            self._process_next_background_thumbnail
        )
        self._background_thumbnail_timer.start(
            500
        )  # 500ms interval between generations

    def _process_next_background_thumbnail(self):
        """Process the next thumbnail in the background queue"""
        # Stop if queue is empty
        if not self._pending_thumbnails:
            if self._background_thumbnail_timer:
                self._background_thumbnail_timer.stop()
                self._background_thumbnail_timer = None
            return

        # Get next page to process
        next_page = self._pending_thumbnails.pop(0)

        # Skip if already cached (might have been requested by user)
        if next_page in self._cached_thumbnails:
            return

        # Request this thumbnail
        self.request_thumbnail.emit(next_page)

    def on_slide_changed(self, current, total):
        # Reset animation state first so the previous slide's pending-animation
        # flag never leaks into the new slide's button state.  The real value
        # will arrive via animation_step_changed within the next poll cycle (~200ms).
        reset = "if (typeof updateAnimationInfo === 'function') updateAnimationInfo(false);"
        update = f"if (typeof updatePageInfo === 'function') updatePageInfo({current}, {total});"
        self._run_javascript(reset + " " + update)

    def on_animation_step_changed(self, click_index, click_count):
        """Called when PPT animation click index/count changes.
        click_index: how many animation steps have been triggered (GetClickIndex).
        click_count: total animation steps on the current slide (GetClickCount).
        When click_index < click_count, there are still pending animations.
        -1 means the info is unavailable (WPS/Yozo or degraded mode).
        """
        has_remaining = False
        if click_index >= 0 and click_count >= 0:
            has_remaining = click_index < click_count
        js_bool = "true" if has_remaining else "false"
        script = f"if (typeof updateAnimationInfo === 'function') updateAnimationInfo({js_bool});"
        self._run_javascript(script)

    def update_page_info(self, current, total):
        try:
            current = int(current)
            total = int(total)
        except Exception:
            return
        script = f"if (typeof updatePageInfo === 'function') updatePageInfo({current}, {total});"
        self._run_javascript(script)

    def update_mask(self, rects_data):
        region = QRegion()

        # Always include page selector area if it's visible (detected by rect)
        # We need to detect if any rect corresponds to the page selector sidebar
        # The page selector is 360px wide, full height, on the right

        has_sidebar = False
        sidebar_rect = None

        w_win = self.width()
        h_win = self.height()

        for r in rects_data:
            x = math.floor(r["x"])
            y = math.floor(r["y"])
            w = math.ceil(r["x"] + r["width"]) - x
            h = math.ceil(r["y"] + r["height"]) - y

            # Heuristic to detect the sidebar (now island style)
            # It should be roughly 260px wide and occupy most of the height
            # and positioned near the right edge OR left edge
            is_near_right = x >= (w_win - w - 50)
            is_near_left = x <= 50

            if w >= 250 and h >= (h_win * 0.8) and (is_near_right or is_near_left):
                has_sidebar = True
                sidebar_rect = QRect(
                    x, 0, w, h_win
                )  # Force full height for interaction safety

            rect = QRect(x - 1, y - 1, w + 2, h + 2)
            region += rect

        if has_sidebar and sidebar_rect:
            region += sidebar_rect

        if not region.isEmpty():
            self.setMask(region)
        else:
            if self.isVisible():
                pass
            pass

    def update_theme(self):
        mode = cfg.themeMode.value
        is_light = False

        if isinstance(mode, Theme):
            if mode == Theme.AUTO:
                is_light = not isDarkTheme()
            else:
                is_light = mode == Theme.LIGHT
        else:
            mode_str = str(mode).lower()
            if mode_str == "light":
                is_light = True
            elif mode_str == "dark":
                is_light = False
            else:
                is_light = not isDarkTheme()

        self._is_light = is_light

        t_color = themeColor()
        color_str = t_color.name()

        theme_id = cfg.themeId.value
        js = (
            f"if (typeof setTheme === 'function') setTheme({'false' if is_light else 'true'}, '{color_str}', '{theme_id}');"
            f"if(typeof window.__applyUnifiedTheme==='function')window.__applyUnifiedTheme();"
        )
        self._run_javascript(js)

    def _start_smtc_thread(self):
        def smtc_loop():
            api = get_system_api()
            while not self._stop_smtc:
                try:
                    info = api.get_media_info()
                    self._smtc_info = info
                except Exception:
                    pass
                for _ in range(20):
                    if self._stop_smtc:
                        break
                    import time

                    time.sleep(0.1)

        self._smtc_thread = threading.Thread(target=smtc_loop, daemon=True)
        self._smtc_thread.start()

    def _update_system_status(self):
        try:
            # Battery
            battery = psutil.sensors_battery()
            is_desktop = False
            battery_percent = 100
            battery_charging = False

            if battery:
                battery_percent = int(battery.percent)
                battery_charging = battery.power_plugged
            else:
                is_desktop = True

            # Network
            net_stats = psutil.net_if_stats()
            network_online = False
            for iface, stats in net_stats.items():
                if stats.isup and "loopback" not in iface.lower():
                    network_online = True
                    break

            volume = -1

            smtc_status = self._smtc_info.get("status", "")
            smtc_title = self._smtc_info.get("title", "")
            smtc_artist = self._smtc_info.get("artist", "")
            smtc_position_ms = int(self._smtc_info.get("position_ms", 0) or 0)
            smtc_duration_ms = int(self._smtc_info.get("duration_ms", 0) or 0)
            smtc_artwork_data_url = self._smtc_info.get("artwork_data_url", "") or ""

            print(f"[Overlay] SMTC: status={smtc_status}, title={smtc_title}, artist={smtc_artist}")

            data = {
                "is_desktop": is_desktop,
                "battery_percent": battery_percent,
                "battery_charging": battery_charging,
                "network_online": network_online,
                "volume": volume,
                "smtc_status": smtc_status,
                "smtc_title": smtc_title,
                "smtc_artist": smtc_artist,
                "smtc_position_ms": max(0, smtc_position_ms),
                "smtc_duration_ms": max(0, smtc_duration_ms),
            }

            if smtc_artwork_data_url != self._last_sent_smtc_artwork_data_url:
                data["smtc_artwork_data_url"] = smtc_artwork_data_url
                self._last_sent_smtc_artwork_data_url = smtc_artwork_data_url

            js = f"if(window.updateSystemStatus) window.updateSystemStatus({json.dumps(data)});"
            self._run_javascript(js)
        except Exception as e:
            print(f"Status update error: {e}")

    def _on_status_bar_visibility_changed(self, visible):
        js = f"if(window.toggleStatusBar) window.toggleStatusBar({'true' if visible else 'false'});"
        self._run_javascript(js)

    def update_config(self):
        try:
            # Check if C++ object is still valid
            if not self.page():
                return
        except RuntimeError:
            return
        try:
            from PySide6.QtWebEngineCore import QWebEngineSettings

            attr = getattr(
                QWebEngineSettings.WebAttribute, "ScrollAnimatorEnabled", None
            )
            if attr is not None:
                self.page().settings().setAttribute(
                    attr, not cfg.disableAnimations.value
                )
        except Exception:
            pass

        trans_map = {
            "select": "选择",
            "pen": "画笔",
            "eraser": "橡皮",
            "clear": "清屏",
            "spotlight": "聚光灯",
            "board_in_board": "板中板",
            "timer": "计时器",
            "end": "结束放映",
            "apps": "更多",
            "compatibility": t("overlay.compatibility"),
        }

        apps_list = []
        if hasattr(cfg, "quickLaunchApps"):
            # quickLaunchApps.value is a string (JSON), need to parse if not list
            raw_val = cfg.quickLaunchApps.value
            apps_data = []
            if isinstance(raw_val, str):
                try:
                    apps_data = json.loads(raw_val)
                except Exception:
                    apps_data = []
            elif isinstance(raw_val, list):
                apps_data = raw_val

            for app in apps_data:
                if isinstance(app, str):  # Handle list of strings (paths) if any
                    path = app
                    name = os.path.basename(app)
                    if name.lower().endswith(".exe"):
                        name = name[:-4]
                elif isinstance(app, dict):
                    path = app.get("path", "")
                    name = app.get("name", "")
                else:
                    continue

                if not path:
                    continue

                # Get icon (cached)
                icon_data = None
                if path in self._icon_cache:
                    icon_data = self._icon_cache[path]
                else:
                    icon_data = get_file_icon_base64(path)
                    if icon_data:
                        self._icon_cache[path] = icon_data

                apps_list.append({"name": name, "path": path, "icon": icon_data})

        toolbar_order = cfg.toolbarOrder.value
        if not isinstance(toolbar_order, list):
            toolbar_order = []
        if apps_list and "apps" not in toolbar_order:
            toolbar_order = toolbar_order + ["apps"]

        config_data = {
            "showStatusBar": cfg.showStatusBar.value,
            "disableAnimations": cfg.disableAnimations.value,
            "statusBarShowTime": cfg.statusBarShowTime.value,
            "statusBarShowSeconds": cfg.statusBarShowSeconds.value,
            "statusBarShowBattery": cfg.statusBarShowBattery.value,
            "statusBarShowVolume": cfg.statusBarShowVolume.value,
            "statusBarShowNetwork": cfg.statusBarShowNetwork.value,
            "statusBarShowMusic": cfg.statusBarShowMusic.value,
            "showToolbarText": cfg.showToolbarText.value,
            "toolbarOrder": toolbar_order,
            "toolbarPosition": cfg.toolbarPosition.value,
            "compatibilityMode": cfg.compatibilityMode.value,
            "flipperPosition": cfg.flipperPosition.value,
            "showClear": cfg.showClear.value,
            "clearMode": cfg.clearMode.value,
            "showSpotlight": cfg.showSpotlight.value,
            "showBoardInBoard": cfg.showBoardInBoard.value,
            "showTimer": cfg.showTimer.value,
            "scale": cfg.scale.value,
            "safeArea": cfg.safeArea.value,
            "popWindowScale": cfg.popWindowScale.value,
            "toolbarOpacity": cfg.toolbarOpacity.value,
            "sidePageOpacity": cfg.sidePageOpacity.value,
            "strictEdgeAlignment": cfg.strictEdgeAlignment.value,
            "toolbarAutoHalfCollapse": cfg.toolbarAutoHalfCollapse.value,
            "uiAccessTopmost": cfg.uiAccessTopmost.value,
            "allowRecording": cfg.allowRecording.value,
            "zOrderCheckInterval": cfg.zOrderCheckInterval.value,
            "texts": trans_map,
            "apps": apps_list,
            "disabledTools": cfg.disabledTools.value,
        }

        js = f"if(window.updateConfig) window.updateConfig({json.dumps(config_data)});"
        try:
            self._run_javascript(js)
        except RuntimeError:
            pass

    def reset_tool_state_ui(self, tool: str = "select"):
        try:
            tool = (tool or "select").replace("'", "")
            js = f"if (window.resetToolState) resetToolState('{tool}');"
            self._run_javascript(js)
        except RuntimeError:
            pass

    def reset_pen_color_ui(self):
        try:
            self._run_javascript("if (window.resetPenColorState) resetPenColorState();")
        except RuntimeError:
            pass

    def show_ink_prompt(self):
        # Using the independent QFluentWidgets-based dialog for better reliability
        print(f"[Overlay] show_ink_prompt called, creating InkPromptWindow", flush=True)
        texts = self._get_ink_prompt_texts()
        
        # Create as a top-level window (no parent) to avoid corrupting the overlay's
        # DWM layered-window alpha compositing when this dialog is later closed.
        self._ink_prompt_window = InkPromptWindow(texts, parent=None)
        self._ink_prompt_window.result.connect(self._on_ink_prompt_window_result)
        self._ink_prompt_window.show()
        
    def _on_ink_prompt_window_result(self, result):
        print(f"[Overlay] InkPromptWindow result: {result}", flush=True)
        if hasattr(self, '_ink_prompt_window') and self._ink_prompt_window:
            self._ink_prompt_window.close()
            self._ink_prompt_window = None
        # Emit the result first, then schedule transparency restoration.
        self.ink_prompt_result.emit(result)
        from PySide6.QtCore import QTimer
        QTimer.singleShot(50, self._restore_transparency_after_ink_prompt)

    def _restore_transparency_after_ink_prompt(self):
        """Restore overlay window transparency attributes after ink prompt dialog closes.

        On Windows, DWM may reset the alpha channel of the overlay's layered window
        after any compositing event (e.g. a child/sibling window closing).  The most
        reliable way to re-establish full transparency is to hide the window and
        re-show it so that DWM rebuilds the compositing surface from scratch.
        """
        try:
            if self._wayland_compatible_mode:
                return
            # Re-assert Qt transparency attributes (no-op if already set, but safe).
            self.setAttribute(Qt.WA_TranslucentBackground)
            self.setAttribute(Qt.WA_NoSystemBackground)
            try:
                self.page().setBackgroundColor(Qt.transparent)
            except Exception:
                pass
            if sys.platform == "win32":
                # On Windows a hide→show cycle forces DWM to recompose the layered
                # window surface, which is the only reliable way to clear the black
                # alpha artefact after a peer/child window has been dismissed.
                was_visible = self.isVisible()
                if was_visible:
                    self.hide()
                    from ppt_assistant.core.platform_integration import remove_window_border
                    win_id = self.winId()
                    if win_id:
                        remove_window_border(win_id)
                    self.show()
                else:
                    self.show()
            else:
                if not self.isVisible():
                    self.show()
                self.repaint()
            print("[Overlay] Transparency restored after ink prompt", flush=True)
        except Exception as e:
            print(f"[Overlay] Error restoring transparency: {e}", flush=True)

    def _get_ink_prompt_texts(self):
        return {
            "title": "是否保留墨迹注释？",
            "text": "检测到放映期间添加了墨迹注释，是否保留到幻灯片中？",
            "keep": "保留",
            "discard": "不保留",
        }

    def load_plugins(self):
        if not os.path.exists(PLUGIN_DIR):
            return
        for entry in os.listdir(PLUGIN_DIR):
            plugin_dir = os.path.join(PLUGIN_DIR, entry)
            if not os.path.isdir(plugin_dir):
                continue
            manifest_path = os.path.join(plugin_dir, "manifest.json")
            if not os.path.exists(manifest_path):
                continue
            try:
                with open(manifest_path, "r", encoding="utf-8-sig") as f:
                    manifest = json.load(f)
                entry_point = manifest.get("entry")
                if not entry_point:
                    continue
                module_name, class_name = entry_point.rsplit(".", 1)
                module_path = os.path.join(plugin_dir, module_name + ".py")
                if not os.path.exists(module_path):
                    continue
                spec = importlib.util.spec_from_file_location(
                    f"plugins.builtins.{entry}.{module_name}", module_path
                )
                module = importlib.util.module_from_spec(spec)
                spec.loader.exec_module(module)
                plugin_cls = getattr(module, class_name, None)
                if plugin_cls is None:
                    continue
                plugin_instance = plugin_cls()
                plugin_instance.manifest = manifest
                if hasattr(plugin_instance, "set_context"):
                    plugin_instance.set_context(self)
                self.plugins.append(plugin_instance)
                print(f"Loaded plugin: {plugin_instance.get_name()}")
            except Exception as e:
                print(f"Failed to load plugin {entry}: {e}")

    def execute_plugin(self, name):
        for plugin in self.plugins:
            if hasattr(plugin, "get_name") and plugin.get_name() == name:
                if hasattr(plugin, "execute"):
                    plugin.execute()
                return

    def bind_config_signals(self):
        cfg.toolbarOrder.valueChanged.connect(lambda *_: self.update_config())
        cfg.toolbarPosition.valueChanged.connect(lambda *_: self.update_config())
        cfg.flipperPosition.valueChanged.connect(lambda *_: self.update_config())
        cfg.quickLaunchApps.valueChanged.connect(lambda *_: self.update_config())
        cfg.showToolbarText.valueChanged.connect(lambda *_: self.update_config())
        cfg.showClear.valueChanged.connect(lambda *_: self.update_config())
        cfg.clearMode.valueChanged.connect(lambda *_: self.update_config())
        cfg.showSpotlight.valueChanged.connect(lambda *_: self.update_config())
        cfg.showBoardInBoard.valueChanged.connect(lambda *_: self.update_config())
        cfg.showTimer.valueChanged.connect(lambda *_: self.update_config())
        cfg.scale.valueChanged.connect(lambda *_: self.update_config())
        cfg.safeArea.valueChanged.connect(lambda *_: self.update_config())
        cfg.popWindowScale.valueChanged.connect(lambda *_: self.update_config())
        cfg.toolbarOpacity.valueChanged.connect(lambda *_: self.update_config())
        cfg.sidePageOpacity.valueChanged.connect(lambda *_: self.update_config())
        cfg.strictEdgeAlignment.valueChanged.connect(lambda *_: self.update_config())
        cfg.toolbarAutoHalfCollapse.valueChanged.connect(lambda *_: self.update_config())
        cfg.uiAccessTopmost.valueChanged.connect(lambda *_: self._ensure_topmost())
        cfg.allowRecording.valueChanged.connect(lambda *_: self.update_config())
        cfg.zOrderCheckInterval.valueChanged.connect(lambda *_: self._apply_zorder_timer())
        cfg.disabledTools.valueChanged.connect(lambda *_: self.update_config())

    def showEvent(self, event):
        self._ensure_runtime_initialized()
        print(f"[Overlay] showEvent called, window visible: {self.isVisible()}")
        super().showEvent(event)
        if self._wayland_compatible_mode:
            self.page().setBackgroundColor(QColor("#101010"))
        else:
            self.page().setBackgroundColor(Qt.transparent)
        self.update_theme()
        self._start_memory_timer()
        print(f"[Overlay] After showEvent, window visible: {self.isVisible()}")

    def _start_memory_timer(self):
        try:
            if self._memory_timer is not None:
                return
            from PySide6.QtCore import QTimer
            self._memory_timer = QTimer(self)
            self._memory_timer.setInterval(120000)
            self._memory_timer.timeout.connect(self._on_memory_tick)
            self._memory_timer.start()
        except Exception:
            pass

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
            gc.collect(2)

            page = self.page()
            if page is not None:
                try:
                    page.clearMemoryCaches()
                except Exception:
                    pass

            if sys.platform == "win32":
                try:
                    kernel32 = ctypes.windll.kernel32
                    PROCESS_SET_QUOTA = 0x0100
                    PROCESS_QUERY_INFORMATION = 0x0400
                    handle = kernel32.OpenProcess(
                        PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION,
                        False,
                        os.getpid(),
                    )
                    if handle:
                        try:
                            kernel32.SetProcessWorkingSetSize(handle, -1, -1)
                        finally:
                            kernel32.CloseHandle(handle)
                except Exception:
                    pass
        except Exception:
            pass

    def closeEvent(self, event):
        self._stop_memory_timer()
        self._stop_smtc = True
        if self._crash_recovery_timer is not None:
            try:
                self._crash_recovery_timer.stop()
            except Exception:
                pass
            self._crash_recovery_timer = None
        if self._zorder_timer is not None:
            try:
                self._zorder_timer.stop()
            except Exception:
                pass
            self._zorder_timer = None
        if self.status_timer is not None:
            try:
                self.status_timer.stop()
            except Exception:
                pass
        super().closeEvent(event)

    def set_active_on_slideshow(self, active: bool, animate: bool = True):
        print(f"[Overlay] set_active_on_slideshow({active}, animate={animate})")
        self._active_on_slideshow = bool(active)
        if self._active_on_slideshow:
            try:
                self._ensure_runtime_initialized()
                print(f"[Overlay] Calling show(), isVisible before: {self.isVisible()}")
                self.show()
                if not self._wayland_compatible_mode:
                    self.setAttribute(Qt.WA_TranslucentBackground)
                    self.setAttribute(Qt.WA_NoSystemBackground)
                    try:
                        self.page().setBackgroundColor(Qt.transparent)
                    except Exception:
                        pass
                print(f"[Overlay] After show(), isVisible: {self.isVisible()}")
                self.raise_()
                self._ensure_topmost()
                print(f"[Overlay] After raise/topmost, isVisible: {self.isVisible()}")
            except Exception as e:
                print(f"[Overlay] Error in set_active_on_slideshow(True): {e}")
                import traceback

                traceback.print_exc()
        else:
            try:
                print("[Overlay] Calling hide()")
                self.hide()
            except Exception as e:
                print(f"[Overlay] Error in set_active_on_slideshow(False): {e}")

    def on_slideshow_start_cleanup(self):
        self.reset_pen_color_ui()
        self.reset_tool_state_ui("select")

    def on_slideshow_end_cleanup(self):
        pass

    def _mark_ui_alive(self):
        pass

    def bind_monitor_signals(self):
        pass

    def show_reload_mask(self, text=""):
        pass

    def hide_reload_mask(self):
        pass

    def set_slideshow_hwnd(self, hwnd):
        try:
            self._slideshow_hwnd = int(hwnd or 0)
        except Exception:
            self._slideshow_hwnd = 0

    def set_ppt_restrictions(self, protected_view: bool, presentation_readonly: bool):
        self._protected_view = bool(protected_view)
        self._presentation_readonly = bool(presentation_readonly)

    def cleanup(self):
        self._stop_memory_timer()
        self._stop_smtc = True
        if self._smtc_thread:
            self._smtc_thread.join(timeout=1.0)
        if self._zorder_timer is not None:
            try:
                self._zorder_timer.stop()
            except Exception:
                pass
            self._zorder_timer = None

    def update_geometry(self, rect, screen_or_metadata):
        # screen_or_metadata can be a QScreen object or a metadata dict
        if isinstance(screen_or_metadata, dict):
            # Coordinates from monitor are already logical or handled by the OS
            if rect:
                self.setGeometry(rect)
        elif hasattr(screen_or_metadata, "geometry"):
            # It's a real QScreen object
            self.setGeometry(screen_or_metadata.geometry())
        elif rect:
            # Fallback if screen_or_metadata is None or unexpected
            self.setGeometry(rect)


def create_overlay_window():
    if _should_use_linux_widget_overlay():
        backend = _linux_overlay_backend()
        if backend == "qml":
            try:
                from ppt_assistant.ui.linux_qml_overlay import LinuxQmlOverlayWindow

                print(
                    "[Overlay] Linux overlay backend: QML "
                    "(experimental; set LUMINALIUM_LINUX_OVERLAY_BACKEND=widget for safe mode).",
                    flush=True,
                )
                return LinuxQmlOverlayWindow()
            except Exception as exc:
                print(
                    f"[Overlay] Failed to initialize Linux QML overlay, "
                    f"falling back to QWidget compatibility overlay: {exc}",
                    flush=True,
                )
        else:
            print(
                "[Overlay] Linux overlay backend: QWidget compatibility "
                "(set LUMINALIUM_LINUX_OVERLAY_BACKEND=qml or unset it to use QML).",
                flush=True,
            )
        from ppt_assistant.ui.linux_widget_overlay import LinuxCompatOverlayWindow

        return LinuxCompatOverlayWindow()
    if _should_use_webengine_overlay():
        return OverlayWindow()
    return WaylandFallbackOverlayWindow()
