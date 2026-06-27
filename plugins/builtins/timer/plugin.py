import os
import json
import sys
import gc
import threading
import subprocess

from PySide6.QtWidgets import QWidget, QApplication
from PySide6.QtCore import Signal, Slot, QTimer

from plugins.interface import AssistantPlugin
from plugins.in_process_window_handle import InProcessWindowHandle
from plugins.webview_window_utils import bring_window_to_front
from ppt_assistant.core.config import SETTINGS_PATH, cfg


def _use_external_webview_process() -> bool:
    return sys.platform == "linux"


def _build_webview_runner_command(html_path, title, width, height, custom_border=True):
    root_dir = os.path.dirname(
        os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    )
    args = [
        html_path,
        title,
        str(int(width)),
        str(int(height)),
        "true" if custom_border else "false",
    ]
    if getattr(sys, "frozen", False):
        return [sys.executable, "--webview-runner", *args]
    main_path = os.path.join(root_dir, "main.py")
    return [sys.executable, main_path, "--webview-runner", *args]


def _build_linux_webview_env(extra_env=None):
    env = os.environ.copy()
    env["SETTINGS_PATH"] = SETTINGS_PATH
    if sys.platform == "linux":
        if env.get("DISPLAY"):
            env["QT_QPA_PLATFORM"] = "xcb"
        env["QTWEBENGINE_DISABLE_SANDBOX"] = "1"
        env.setdefault("NO_AT_BRIDGE", "1")
        env.setdefault("QT_ACCESSIBILITY", "0")
        _cur = env.get("QTWEBENGINE_CHROMIUM_FLAGS", "")
        if "--disable-renderer-accessibility" not in _cur:
	        env["QTWEBENGINE_CHROMIUM_FLAGS"] = (_cur + " --disable-renderer-accessibility").strip()
        env["DEFER_WEBENGINE_LOAD"] = "1"
    if extra_env:
        env.update(extra_env)
    return env


class _InProcessTimerApiMixin:
    def __init__(self, plugin, assets_path):
        self._timer_plugin = plugin
        self._timer_assets_path = assets_path

    @Slot(result=str)
    def get_assets_path(self):
        return self._timer_assets_path

    @Slot(result="QVariant")
    def get_timer_state(self):
        manager = self._timer_plugin._timer_manager
        return {
            "remaining": int(manager.remaining_seconds),
            "total": int(manager.total_seconds),
            "is_running": bool(manager.is_running),
        }

    @Slot(int)
    def start_timer(self, seconds):
        self._timer_plugin.start_requested.emit(int(seconds))

    @Slot()
    def pause_timer(self):
        self._timer_plugin.pause_requested.emit()

    @Slot()
    def resume_timer(self):
        self._timer_plugin.resume_requested.emit()

    @Slot()
    def stop_timer(self):
        self._timer_plugin.stop_requested.emit()

    @Slot()
    def finish_timer(self):
        self._timer_plugin.finish_requested.emit()

    @Slot(int)
    def add_time(self, seconds):
        self._timer_plugin.add_time_requested.emit(int(seconds))

    @Slot(int)
    def update_timer(self, total_seconds):
        self._timer_plugin.update_time_requested.emit(int(total_seconds))


def _build_timer_api(base_api_cls, plugin, assets_path):
    class InProcessTimerApi(_InProcessTimerApiMixin, base_api_cls):
        def __init__(self):
            base_api_cls.__init__(self)
            _InProcessTimerApiMixin.__init__(self, plugin, assets_path)

    return InProcessTimerApi()


class TimerPlugin(AssistantPlugin):
    start_requested = Signal(int)
    pause_requested = Signal()
    resume_requested = Signal()
    stop_requested = Signal()
    finish_requested = Signal()
    background_mode_entered = Signal()
    add_time_requested = Signal(int)
    update_time_requested = Signal(int)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.process = None
        self._window = None
        self._api = None
        self._wv = None
        self._hidden = False
        self._close_blocked = False
        self._timer_manager = None  # Will be set when context is assigned
        self.start_requested.connect(self._on_start)
        self.pause_requested.connect(self._on_pause)
        self.resume_requested.connect(self._on_resume)
        self.stop_requested.connect(self._on_stop)
        self.finish_requested.connect(self._on_finish)
        self.add_time_requested.connect(self._on_add_time)
        self.update_time_requested.connect(self._on_update_time)

    def get_name(self):
        return "计时器"

    def get_icon(self):
        return "timer.svg"

    def set_context(self, context):
        super().set_context(context)
        # Use the main app's TimerManager to avoid creating a duplicate
        if hasattr(context, "_timer_manager"):
            self._timer_manager = context._timer_manager

    def _on_start(self, seconds):
        if self._timer_manager:
            self._timer_manager.start(seconds)

    def _on_pause(self):
        if self._timer_manager:
            self._timer_manager.pause()

    def _on_resume(self):
        if self._timer_manager:
            self._timer_manager.resume()

    def _on_stop(self):
        if self._timer_manager:
            self._timer_manager.stop()

    def _on_finish(self):
        if self._timer_manager:
            self._timer_manager.finish()

    def _on_add_time(self, seconds):
        if self._timer_manager:
            self._timer_manager.add_time(seconds)

    def _on_update_time(self, total_seconds):
        if self._timer_manager:
            self._timer_manager.update_time(total_seconds)

    def _prewarm_webview(self):
        try:
            self._ensure_webview_module()
            if self._wv is not None:
                self._wv._warmup_webengine()
        except Exception:
            pass

    def prewarm(self, shell=False):
        """Warm shared Chromium only; timer window stays on-demand."""
        if _use_external_webview_process():
            return
        try:
            wv = self._ensure_webview_module()
            wv._warmup_webengine()
            if not shell:
                return
            self._ensure_window()
            if self._window is not None:
                try:
                    self._window.hide()
                except Exception:
                    pass
            self._hidden = True
            self._close_blocked = True
        except Exception:
            pass

    def _should_hide_on_close(self):
        try:
            return bool(cfg.hideOnClose.value)
        except Exception:
            return True

    def _ensure_webview_module(self):
        if self._wv is None:
            import plugins.webview_runner as webview_runner

            self._wv = webview_runner
        return self._wv

    def _load_json_file(self, path):
        if not path or not os.path.exists(path):
            return {}
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
            return data if isinstance(data, dict) else {}
        except Exception:
            return {}

    def _focus_existing_window(self):
        if self._window is None:
            return False
        try:
            if self._window.isMinimized():
                self._window.showNormal()
            self._window.show()
            self._window.raise_()
            self._window.activateWindow()
            return True
        except RuntimeError:
            self._window = None
            self._api = None
            self.process = None
            return False

    def _on_window_destroyed(self, *_args):
        if self._close_blocked:
            return
        self._window = None
        self._api = None
        self.process = None
        if self._timer_manager and self._timer_manager.is_running:
            self.background_mode_entered.emit()

    def _intercept_close(self, event):
        if not self._should_hide_on_close():
            self._close_blocked = False
            event.accept()
            return
        if self._close_blocked:
            event.accept()
            return
        self._hide_window()
        event.ignore()

    def _hide_window(self):
        if self._window is None:
            return
        self._close_blocked = True
        try:
            self._window.hide()
        except Exception:
            pass
        self._hidden = True
        self._release_webengine_resources()
        QApplication.processEvents()

    def _restore_window(self):
        if self._window is None or not self._hidden:
            return
        self._close_blocked = False
        self._hidden = False
        try:
            self._ensure_window()
            if self._window.isMinimized():
                self._window.showNormal()
            else:
                self._window.show()
            self._window.raise_()
            self._window.activateWindow()
            bring_window_to_front(int(self._window.winId()))
        except RuntimeError:
            self._window = None
            self._api = None
            self._hidden = False

    def _release_webengine_resources(self):
        try:
            for gen in range(3):
                gc.collect(gen)
            gc.collect()
        except Exception:
            pass
        if sys.platform == "win32":
            try:
                import ctypes
                handle = ctypes.windll.kernel32.GetCurrentProcess()
                ctypes.windll.kernel32.SetProcessWorkingSetSize(handle, -1, -1)
            except Exception:
                pass

    def _handle_external_timer_line(self, line: str):
        text = str(line or "").strip()
        if not text:
            return
        try:
            if text.startswith("TIMER_START:"):
                self.start_requested.emit(int(text.split(":", 1)[1]))
            elif text == "TIMER_PAUSE":
                self.pause_requested.emit()
            elif text == "TIMER_RESUME":
                self.resume_requested.emit()
            elif text == "TIMER_STOP":
                self.stop_requested.emit()
            elif text == "TIMER_FINISH":
                self.finish_requested.emit()
            elif text.startswith("TIMER_ADD_TIME:"):
                self.add_time_requested.emit(int(text.split(":", 1)[1]))
            elif text.startswith("TIMER_UPDATE:"):
                self.update_time_requested.emit(int(text.split(":", 1)[1]))
        except Exception:
            pass

    def _watch_external_timer_process(self, process):
        try:
            stream = getattr(process, "stdout", None)
            if stream is not None:
                for line in stream:
                    self._handle_external_timer_line(line)
        except Exception:
            pass
        try:
            process.wait(timeout=1.0)
        except Exception:
            pass
        if self.process is process:
            self.process = None
            if self._timer_manager and self._timer_manager.is_running:
                self.background_mode_entered.emit()

    def _launch_external_window(self, html_path, width, height, assets_path):
        env = _build_linux_webview_env(
            {
                "ASSETS_PATH": assets_path,
                "TIMER_REMAINING": str(
                    int(max(0, self._timer_manager.remaining_seconds if self._timer_manager else 0))
                ),
                "TIMER_TOTAL": str(int(max(0, self._timer_manager.total_seconds if self._timer_manager else 0))),
                "TIMER_IS_RUNNING": "true"
                if (self._timer_manager and self._timer_manager.is_running)
                else "false",
            }
        )
        settings = self._load_json_file(SETTINGS_PATH)
        use_native = settings.get("General", {}).get("UseNativeTitleBar", False)
        cmd = _build_webview_runner_command(
            html_path,
            "Luminalium Timer Plugin",
            width,
            height,
            not use_native,
        )
        process = subprocess.Popen(
            cmd,
            env=env,
            close_fds=True,
            stdout=subprocess.PIPE,
            stdin=subprocess.DEVNULL,
            text=True,
            encoding="utf-8",
            errors="replace",
            bufsize=1,
        )
        self.process = process
        watcher = threading.Thread(
            target=self._watch_external_timer_process,
            args=(process,),
            daemon=True,
        )
        watcher.start()
        return process

    def _ensure_window(self):
        if self._window is not None:
            return self._window

        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "timer.html")
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(base_dir)))
        version_path = os.path.join(root_dir, "version.json")
        assets_path = os.path.join(base_dir, "assets", "timer_ring.ogg")

        screen = QApplication.primaryScreen()
        screen_geo = screen.geometry() if screen else QWidget().screen().geometry()
        width_val = int(
            min(max(600, screen_geo.width() * 0.35), screen_geo.width() * 0.5)
        )
        height_val = int(
            min(max(500, screen_geo.height() * 0.45), screen_geo.height() * 0.6)
        )
        width = int(min(width_val + 350, screen_geo.width()))
        height = int(min(height_val + 150, screen_geo.height()))

        if _use_external_webview_process():
            self._launch_external_window(html_path, width, height, assets_path)
            return None

        wv = self._ensure_webview_module()
        wv._warmup_webengine()

        os.environ["SETTINGS_PATH"] = SETTINGS_PATH

        api = _build_timer_api(wv.Api, self, assets_path)
        api.set_in_process(True)
        api.settings = self._load_json_file(SETTINGS_PATH)
        api.version = self._load_json_file(version_path)
        api.version["device_uuid"] = wv.get_device_uuid()[:8]

        theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
        defer_load = wv._should_defer_initial_load(
            html_path, "Luminalium Timer Plugin", True
        )
        use_native = api.settings.get("General", {}).get("UseNativeTitleBar", False)
        window = wv.MainWindow(
            "Luminalium Timer Plugin",
            html_path,
            api,
            width,
            height,
            theme_mode,
            not use_native,
            defer_load,
            frameless=not use_native,
            defer_until_show=True,
        )
        window.destroyed.connect(self._on_window_destroyed)

        original_close_event = window.closeEvent

        def patched_close_event(event):
            if self._should_hide_on_close():
                self._intercept_close(event)
            else:
                self._close_blocked = False
                original_close_event(event)

        window.closeEvent = patched_close_event

        self._api = api
        self._window = window
        self.process = InProcessWindowHandle(window)
        return window

    def execute(self):
        if _use_external_webview_process():
            if self.process is not None and self.process.poll() is None:
                return
            self._launch_external_window()
            return

        if self._hidden:
            self._restore_window()
            return

        if self._focus_existing_window():
            return

        self._ensure_window()
        self._window.show()
        try:
            self._window.raise_()
            self._window.activateWindow()
            bring_window_to_front(int(self._window.winId()))
        except Exception:
            pass

    def terminate(self):
        self._close_blocked = False
        self._hidden = False
        if self.process and self.process.poll() is None:
            self.process.terminate()
        self._window = None
        self._api = None
        self.process = None
