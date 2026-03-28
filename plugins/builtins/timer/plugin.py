import os
import json

from PySide6.QtWidgets import QWidget, QApplication
from PySide6.QtCore import Signal, Slot, QTimer

from plugins.interface import AssistantPlugin
from plugins.in_process_window_handle import InProcessWindowHandle
from ppt_assistant.core.config import SETTINGS_PATH
from ppt_assistant.core.timer_manager import TimerManager


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
        self._timer_manager = TimerManager()
        self.start_requested.connect(self._timer_manager.start)
        self.pause_requested.connect(self._timer_manager.pause)
        self.resume_requested.connect(self._timer_manager.resume)
        self.stop_requested.connect(self._timer_manager.stop)
        self.finish_requested.connect(self._timer_manager.finish)
        self.add_time_requested.connect(self._timer_manager.add_time)
        self.update_time_requested.connect(self._timer_manager.update_time)
        QTimer.singleShot(1500, self._prewarm_webview)

    def get_name(self):
        return "计时器"

    def get_icon(self):
        return "timer.svg"

    def _prewarm_webview(self):
        try:
            self._ensure_webview_module()
            if self._wv is not None:
                self._wv._warmup_webengine()
        except Exception:
            pass

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

    def _focus_existing_window(self, show_toast=False):
        if self._window is None:
            return False
        try:
            if self._window.isMinimized():
                self._window.showNormal()
            self._window.show()
            self._window.raise_()
            self._window.activateWindow()
            if show_toast and self._api is not None:
                self._api.notify_existing_window("已经存在打开的窗口！")
            return True
        except RuntimeError:
            self._window = None
            self._api = None
            self.process = None
            return False

    def _on_window_destroyed(self, *_args):
        self._window = None
        self._api = None
        self.process = None
        if self._timer_manager.is_running:
            self.background_mode_entered.emit()

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
        width_val = int(min(max(600, screen_geo.width() * 0.35), screen_geo.width() * 0.5))
        height_val = int(min(max(500, screen_geo.height() * 0.45), screen_geo.height() * 0.6))
        width = int(min(width_val + 350, screen_geo.width()))
        height = int(min(height_val + 150, screen_geo.height()))

        wv = self._ensure_webview_module()
        wv._warmup_webengine()

        os.environ["SETTINGS_PATH"] = SETTINGS_PATH

        api = _build_timer_api(wv.Api, self, assets_path)
        api.set_in_process(True)
        api.settings = self._load_json_file(SETTINGS_PATH)
        api.version = self._load_json_file(version_path)

        theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
        defer_load = wv._should_defer_initial_load(html_path, "Luminalium Timer Plugin", True)
        window = wv.MainWindow("Luminalium Timer Plugin", html_path, api, width, height, theme_mode, True, defer_load)
        window.destroyed.connect(self._on_window_destroyed)

        self._api = api
        self._window = window
        self.process = InProcessWindowHandle(window)
        return window

    def execute(self):
        if self._focus_existing_window(show_toast=True):
            return

        self._ensure_window()
        self._window.show()
        try:
            self._window.raise_()
            self._window.activateWindow()
        except Exception:
            pass

    def terminate(self):
        if self.process and self.process.poll() is None:
            self.process.terminate()
        self._window = None
        self._api = None
        self.process = None
