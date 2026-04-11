
import os
import sys
import math
import json
import ctypes
import tempfile
import importlib.util
from typing import Optional
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtCore import QObject, Slot, Signal, Qt, QUrl, QTimer, QRect, QPoint, QEvent, QByteArray
from PySide6.QtGui import QColor, QRegion, QGuiApplication, QIcon
from PySide6.QtQuick import QQuickView
from PySide6.QtQml import QQmlComponent
from PySide6.QtWidgets import QWidget
from ppt_assistant.core.config import cfg
from ppt_assistant.core.i18n import t
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.icon_helper import get_file_icon_base64
from ppt_assistant.core.platform_integration import open_path
from ppt_assistant.core.system import get_system_api
import psutil
import asyncio
import threading
import subprocess

PLUGIN_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "plugins", "builtins")


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
    value = str(os.environ.get("LUMINALIUM_ENABLE_WEBENGINE_OVERLAY", "")).strip().lower()
    return value in ("1", "true", "yes", "on")


def _should_use_linux_compat_overlay_page() -> bool:
    if sys.platform != "linux":
        return False
    if not _is_wayland_compositor_session():
        return False
    value = str(os.environ.get("LUMINALIUM_FORCE_FULL_OVERLAY_HTML", "")).strip().lower()
    return value not in ("1", "true", "yes", "on")


def _should_use_linux_widget_overlay() -> bool:
    if sys.platform != "linux":
        return False
    if not bool(os.environ.get("DISPLAY")):
        return False
    if not _is_wayland_compositor_session():
        return False
    force_webengine = str(os.environ.get("LUMINALIUM_ENABLE_WEBENGINE_OVERLAY", "")).strip().lower()
    return force_webengine not in ("1", "true", "yes", "on")

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
        if tool_name == 'select' or tool_name == 'arrow':
            self._overlay.request_ptr_arrow.emit()
        elif tool_name == 'pen':
            self._overlay.request_ptr_pen.emit()
        elif tool_name == 'eraser':
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
        self._overlay.request_end.emit()

    @Slot(bool)
    def inkPromptResult(self, keep):
        self._overlay.ink_prompt_result.emit(bool(keep))

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

    @Slot('QVariantList')
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

class InkPromptBridge(QObject):
    result = Signal(bool)

    @Slot()
    def keep(self):
        self.result.emit(True)

    @Slot()
    def discard(self):
        self.result.emit(False)


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
        print("[Overlay] WebEngine overlay disabled on Wayland. Set LUMINALIUM_ENABLE_WEBENGINE_OVERLAY=1 to force-enable it.")

    def _log_visibility_skip(self):
        if self._warned_visibility:
            return
        self._warned_visibility = True
        print("[Overlay] Wayland fallback overlay is active; overlay window rendering is disabled to avoid native crashes.")

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
            self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowDoesNotAcceptFocus | Qt.Tool | Qt.WindowStaysOnTopHint)
            self.setAttribute(Qt.WA_TranslucentBackground)
            self.setAttribute(Qt.WA_NoSystemBackground)

        self.monitor = None
        self.plugins = []
        self._icon_cache = {}
        self._is_light = False
        self._slideshow_hwnd = 0
        self._protected_view = False
        self._presentation_readonly = False
        self._active_on_slideshow = False
        self._ink_prompt_view = None
        self._ink_prompt_bridge = None
        
        self._smtc_info = {"status": "", "title": "", "position_ms": 0, "duration_ms": 0}
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

    def _resolve_theme_path(self) -> str:
        theme_name = cfg.overlayTheme.value
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
        if getattr(sys, "frozen", False):
            root_dir = os.path.dirname(sys.executable)

        def resolve_user_theme_path(name: str) -> Optional[str]:
            theme_dir = os.path.join(root_dir, "user", "themes", name)
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
                with open(manifest_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if data.get("name") != name:
                    return None
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
            theme_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overlay.html")
        return theme_path

    def _resolve_runtime_page_path(self) -> str:
        if self._linux_compat_page_mode:
            compat_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overlay_linux_compat.html")
            if os.path.exists(compat_path):
                return compat_path
        return self._resolve_theme_path()

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

            profile.setCachePath(cache_path)
            profile.setPersistentStoragePath(cache_path)
            profile.setHttpCacheType(profile.HttpCacheType.DiskHttpCache)
            profile.setHttpCacheMaximumSize(50 * 1024 * 1024)

            settings = self.page().settings()
            from PySide6.QtWebEngineCore import QWebEngineSettings
            for attr_name, enabled in [
                ("LocalStorageEnabled", False),
                ("SessionStorageEnabled", False),
                ("WebGLEnabled", False),
                ("Accelerated2dCanvasEnabled", False),
                ("ScrollAnimatorEnabled", not cfg.disableAnimations.value),
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
            return
        if page is None:
            return
        if not self._page_ready:
            self._pending_scripts.append(script)
            return
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
            user32.SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW)
        except Exception:
            pass

    def _on_render_process_terminated(self, status, exit_code):
        self._restore_linux_webengine_env_override()
        status_name = ""
        try:
            status_name = str(getattr(status, "name", status))
        except Exception:
            status_name = str(status)
        print(f"[Overlay] Render process terminated: status={status}, exit_code={exit_code}")
        if "NormalTerminationStatus" in status_name and int(exit_code or 0) == 0:
            print("[Overlay] Renderer ended normally; skipping crash recovery.")
            return
        self._render_crash_count += 1
        print(f"[Overlay] Crash #{self._render_crash_count}/{self._max_reload_attempts}")
        
        # If too many crashes, disable GPU and retry once, then give up
        if self._render_crash_count > self._max_reload_attempts:
            print(f"[Overlay] Too many crashes ({self._render_crash_count}). Giving up on recovery.")
            return
        
        # Use exponential backoff: 100ms, 500ms, 1500ms
        delay = min(100 * (2 ** (self._render_crash_count - 1)), 2000)
        print(f"[Overlay] Scheduling reload in {delay}ms")
        
        # If this is the second crash, try disabling GPU acceleration
        if self._render_crash_count == 2:
            try:
                print(f"[Overlay] Disabling GPU acceleration due to repeated crashes")
                settings = self.page().settings()
                from PySide6.QtWebEngineCore import QWebEngineSettings
                settings.setAttribute(QWebEngineSettings.WebAttribute.Accelerated2dCanvasEnabled, False)
                settings.setAttribute(QWebEngineSettings.WebAttribute.WebGLEnabled, False)
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

    def on_thumbnail_ready(self, index, path):
        # Mark as cached
        self._cached_thumbnails.add(index)
        
        # Path needs to be converted to file URL
        url = QUrl.fromLocalFile(path).toString()
        script = f"if (typeof updatePageThumbnail === 'function') updatePageThumbnail({index}, '{url}');"
        self._run_javascript(script)
        
        # Start next background caching task if available
        self._process_next_background_thumbnail()
    
    def on_start_background_caching(self, total_pages):
        """Start background caching of thumbnails"""
        if self._background_thumbnail_timer is not None:
            return  # Already running
        
        # Build list of pages to cache (excluding already cached ones)
        self._pending_thumbnails = [
            i for i in range(1, total_pages + 1) 
            if i not in self._cached_thumbnails
        ]
        
        # Start timer for background caching
        self._background_thumbnail_timer = QTimer(self)
        self._background_thumbnail_timer.setSingleShot(False)
        self._background_thumbnail_timer.timeout.connect(self._process_next_background_thumbnail)
        self._background_thumbnail_timer.start(500)  # 500ms interval between generations
    
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
        script = f"if (typeof updatePageInfo === 'function') updatePageInfo({current}, {total});"
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
            x = math.floor(r['x'])
            y = math.floor(r['y'])
            w = math.ceil(r['x'] + r['width']) - x
            h = math.ceil(r['y'] + r['height']) - y
            
            # Heuristic to detect the sidebar (now island style)
            # It should be roughly 260px wide and occupy most of the height
            # and positioned near the right edge OR left edge
            is_near_right = (x >= (w_win - w - 50))
            is_near_left = (x <= 50)
            
            if w >= 250 and h >= (h_win * 0.8) and (is_near_right or is_near_left):
                 has_sidebar = True
                 sidebar_rect = QRect(x, 0, w, h_win) # Force full height for interaction safety
            
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
        
        from qfluentwidgets import Theme, isDarkTheme
        
        if isinstance(mode, Theme):
            if mode == Theme.AUTO:
                is_light = not isDarkTheme()
            else:
                is_light = (mode == Theme.LIGHT)
        else:
            mode_str = str(mode).lower()
            if mode_str == "light":
                is_light = True
            elif mode_str == "dark":
                is_light = False
            else:
                is_light = not isDarkTheme()
            
        self._is_light = is_light
        
        from qfluentwidgets import themeColor
        t_color = themeColor()
        color_str = t_color.name()
        
        theme_id = cfg.themeId.value
        js = f"if (typeof setTheme === 'function') setTheme({'false' if is_light else 'true'}, '{color_str}', '{theme_id}');"
        self._run_javascript(js)
        if self._ink_prompt_view:
            self._apply_ink_prompt_context(self._ink_prompt_view.rootContext())

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
            # Check for any active interface (excluding loopback)
            for iface, stats in net_stats.items():
                if stats.isup and 'loopback' not in iface.lower():
                    network_online = True
                    break
            
            # Volume (Placeholder for now as pycaw/comtypes might not be present)
            volume = -1
            
            # SMTC
            smtc_status = self._smtc_info.get("status", "")
            smtc_title = self._smtc_info.get("title", "")
            smtc_position_ms = int(self._smtc_info.get("position_ms", 0) or 0)
            smtc_duration_ms = int(self._smtc_info.get("duration_ms", 0) or 0)
            
            data = {
                "is_desktop": is_desktop,
                "battery_percent": battery_percent,
                "battery_charging": battery_charging,
                "network_online": network_online,
                "volume": volume,
                "smtc_status": smtc_status,
                "smtc_title": smtc_title,
                "smtc_position_ms": max(0, smtc_position_ms),
                "smtc_duration_ms": max(0, smtc_duration_ms),
            }
            
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
            attr = getattr(QWebEngineSettings.WebAttribute, "ScrollAnimatorEnabled", None)
            if attr is not None:
                self.page().settings().setAttribute(attr, not cfg.disableAnimations.value)
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
            "compatibility": t("overlay.compatibility")
        }
        
        apps_list = []
        if hasattr(cfg, 'quickLaunchApps'):
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
                if isinstance(app, str): # Handle list of strings (paths) if any
                    path = app
                    name = os.path.basename(app)
                    if name.lower().endswith('.exe'):
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
                        
                apps_list.append({
                    "name": name, 
                    "path": path,
                    "icon": icon_data
                })

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
            "statusBarShowMusicProgress": cfg.statusBarShowMusicProgress.value,
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
            "texts": trans_map,
            "apps": apps_list,
            "disabledTools": cfg.disabledTools.value
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
        try:
            self._ensure_ink_prompt_view()
            if self._ink_prompt_view:
                self._ink_prompt_view.show()
                self._ink_prompt_view.raise_()
        except Exception:
            pass

    def _ensure_ink_prompt_view(self):
        if not self._ink_prompt_view:
            view = QQuickView()
            view.setColor(Qt.transparent)
            view.setFlags(Qt.FramelessWindowHint | Qt.Tool | Qt.WindowStaysOnTopHint)
            view.setResizeMode(QQuickView.SizeRootObjectToView)
            view.setModality(Qt.ApplicationModal)

            bridge = InkPromptBridge()
            bridge.result.connect(self._on_ink_prompt_result)

            ctx = view.rootContext()
            ctx.setContextProperty("inkBridge", bridge)
            self._ink_prompt_view = view
            self._ink_prompt_bridge = bridge
            self._apply_ink_prompt_context(ctx)

            qml = """
import QtQuick 2.15
import QtQuick.Controls 2.15

Item {
    id: root
    width: screenWidth
    height: screenHeight

    Rectangle {
        anchors.fill: parent
        color: maskColor
    }

    MouseArea {
        anchors.fill: parent
    }

    Rectangle {
        id: card
        width: Math.min(parent.width * 0.6, 460)
        height: content.height + 48
        color: dialogBg
        radius: 12
        border.color: dialogBorder
        border.width: 1
        anchors.centerIn: parent

        Column {
            id: content
            spacing: 20
            width: parent.width - 48
            anchors.centerIn: parent

            Text {
                text: inkTitle
                font.pixelSize: 17
                font.bold: true
                color: titleColor
                width: parent.width
                wrapMode: Text.Wrap
            }

            Text {
                text: inkText
                font.pixelSize: 15
                font.weight: Font.Normal
                color: bodyColor
                width: parent.width
                wrapMode: Text.Wrap
                lineHeight: 1.4
            }

            Item {
                width: parent.width
                height: 4
            }

            Row {
                spacing: 12
                layoutDirection: Qt.RightToLeft
                width: parent.width

                Rectangle {
                    width: 88
                    height: 34
                    radius: 17
                    color: primaryBg
                    border.color: primaryBorder
                    border.width: 1
                    
                    Text {
                        anchors.centerIn: parent
                        text: inkKeep
                        font.pixelSize: 14
                        font.bold: true
                        color: primaryText
                    }
                    MouseArea {
                        anchors.fill: parent
                        cursorShape: Qt.PointingHandCursor
                        onClicked: inkBridge.keep()
                    }
                }

                Rectangle {
                    width: 88
                    height: 34
                    radius: 17
                    color: btnBg
                    border.color: btnBorder
                    border.width: 1
                    
                    Text {
                        anchors.centerIn: parent
                        text: inkDiscard
                        font.pixelSize: 14
                        font.bold: true
                        color: btnText
                    }
                    MouseArea {
                        anchors.fill: parent
                        cursorShape: Qt.PointingHandCursor
                        onClicked: inkBridge.discard()
                    }
                }
            }
        }
    }
}
"""

            component = QQmlComponent(view.engine())
            component.setData(QByteArray(qml.encode("utf-8")), QUrl())
            root = component.create()
            view.setContent(QUrl(), component, root)

        screen = self.screen() or QGuiApplication.primaryScreen()
        if screen:
            self._ink_prompt_view.setGeometry(screen.geometry())
            self._apply_ink_prompt_context(self._ink_prompt_view.rootContext())

    def _apply_ink_prompt_context(self, ctx):
        texts = self._get_ink_prompt_texts()
        palette = self._get_ink_prompt_palette()
        ctx.setContextProperty("inkTitle", texts["title"])
        ctx.setContextProperty("inkText", texts["text"])
        ctx.setContextProperty("inkKeep", texts["keep"])
        ctx.setContextProperty("inkDiscard", texts["discard"])
        ctx.setContextProperty("maskColor", palette["mask"])
        ctx.setContextProperty("dialogBg", palette["bg"])
        ctx.setContextProperty("dialogBorder", palette["border"])
        ctx.setContextProperty("titleColor", palette["title"])
        ctx.setContextProperty("bodyColor", palette["body"])
        ctx.setContextProperty("btnBg", palette["btn_bg"])
        ctx.setContextProperty("btnBorder", palette["btn_border"])
        ctx.setContextProperty("btnText", palette["btn_text"])
        ctx.setContextProperty("primaryBg", palette["primary_bg"])
        ctx.setContextProperty("primaryBorder", palette["primary_border"])
        ctx.setContextProperty("primaryText", palette["primary_text"])
        if self._ink_prompt_view:
            size = self._ink_prompt_view.size()
            ctx.setContextProperty("screenWidth", size.width())
            ctx.setContextProperty("screenHeight", size.height())

    def _get_ink_prompt_texts(self):
        return {
            "title": "是否保留墨迹注释？",
            "text": "检测到放映期间添加了墨迹注释，是否保留到幻灯片中？",
            "keep": "保留",
            "discard": "不保留"
        }

    def _get_ink_prompt_palette(self):
        from qfluentwidgets import themeColor
        accent = themeColor().name()
        if self._is_light:
            return {
                "mask": "rgba(0, 0, 0, 1.0)",
                "bg": "#ffffff",
                "border": "rgba(0, 0, 0, 0.05)",
                "title": "#191919",
                "body": "#191919",
                "btn_bg": "transparent",
                "btn_border": "rgba(0, 0, 0, 0.05)",
                "btn_text": "#666666",
                "primary_bg": "transparent",
                "primary_border": accent,
                "primary_text": accent
            }
        return {
            "mask": "rgba(0, 0, 0, 1.0)",
            "bg": "#2b2b2b",
            "border": "rgba(255, 255, 255, 0.08)",
            "title": "#E5E5E5",
            "body": "#E5E5E5",
            "btn_bg": "transparent",
            "btn_border": "rgba(255, 255, 255, 0.08)",
            "btn_text": "#909090",
            "primary_bg": "transparent",
            "primary_border": accent,
            "primary_text": accent
        }

    def _on_ink_prompt_result(self, keep):
        if self._ink_prompt_view:
            try:
                self._ink_prompt_view.hide()
            except Exception:
                pass
        self.ink_prompt_result.emit(bool(keep))

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
                with open(manifest_path, "r", encoding="utf-8") as f:
                    manifest = json.load(f)
                entry_point = manifest.get("entry")
                if not entry_point:
                    continue
                module_name, class_name = entry_point.rsplit(".", 1)
                module_path = os.path.join(plugin_dir, module_name + ".py")
                if not os.path.exists(module_path):
                    continue
                spec = importlib.util.spec_from_file_location(f"plugins.builtins.{entry}.{module_name}", module_path)
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
        cfg.disabledTools.valueChanged.connect(lambda *_: self.update_config())

    def showEvent(self, event):
        self._ensure_runtime_initialized()
        super().showEvent(event)
        if self._wayland_compatible_mode:
            self.page().setBackgroundColor(QColor("#101010"))
        else:
            self.page().setBackgroundColor(Qt.transparent)
        self.update_theme()
    
    def closeEvent(self, event):
        """Clean up resources when overlay window closes"""
        self._stop_smtc = True
        if self._crash_recovery_timer is not None:
            try:
                self._crash_recovery_timer.stop()
            except Exception:
                pass
            self._crash_recovery_timer = None
        if self.status_timer is not None:
            try:
                self.status_timer.stop()
            except Exception:
                pass
        super().closeEvent(event)

    def set_active_on_slideshow(self, active: bool, animate: bool = True):
        self._active_on_slideshow = bool(active)
        if self._active_on_slideshow:
            try:
                self._ensure_runtime_initialized()
                self.show()
                self.raise_()
                self._ensure_topmost()
            except Exception:
                pass
        else:
            try:
                self.hide()
            except Exception:
                pass

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
        self._stop_smtc = True
        if self._smtc_thread:
            self._smtc_thread.join(timeout=1.0)
        
    def update_geometry(self, rect, screen):
        if screen:
            self.setGeometry(screen.geometry())
        elif rect:
            self.setGeometry(rect)


def create_overlay_window():
    if _should_use_linux_widget_overlay():
        from ppt_assistant.ui.linux_widget_overlay import LinuxCompatOverlayWindow

        return LinuxCompatOverlayWindow()
    if _should_use_webengine_overlay():
        return OverlayWindow()
    return WaylandFallbackOverlayWindow()
