import os
import json

from PySide6.QtCore import QTimer

from plugins.interface import AssistantPlugin
from plugins.in_process_window_handle import InProcessWindowHandle
from ppt_assistant.core.config import SETTINGS_PATH


class OnboardingPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.process = None
        self._window = None
        self._api = None
        self._wv = None
        self._preview = False
        QTimer.singleShot(1500, self._prewarm_webview)

    def get_name(self):
        return "引导"

    def get_icon(self):
        return "settings.svg"

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

    def _ensure_window(self, preview=False):
        if self._window is not None and self._preview == bool(preview):
            return self._window

        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "onboarding.html")
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(base_dir)))
        version_path = os.path.join(root_dir, "version.json")

        wv = self._ensure_webview_module()
        wv._warmup_webengine()

        api = wv.Api()
        api.set_in_process(True)
        api.settings = self._load_json_file(SETTINGS_PATH)
        api.version = self._load_json_file(version_path)

        previous_preview = os.environ.get("ONBOARDING_PREVIEW")
        os.environ["SETTINGS_PATH"] = SETTINGS_PATH
        os.environ["ONBOARDING_PREVIEW"] = "true" if preview else "false"
        try:
            title = "Onboarding Preview" if preview else "Onboarding"
            defer_load = wv._should_defer_initial_load(html_path, title, True)
            theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
            window = wv.MainWindow(title, html_path, api, 960, 640, theme_mode, False, defer_load)
        finally:
            if previous_preview is None:
                os.environ.pop("ONBOARDING_PREVIEW", None)
            else:
                os.environ["ONBOARDING_PREVIEW"] = previous_preview

        window.destroyed.connect(self._on_window_destroyed)

        self._api = api
        self._window = window
        self._preview = bool(preview)
        self.process = InProcessWindowHandle(window)
        return window

    def execute(self, preview=False):
        preview = bool(preview)
        if self._window is not None and self._preview != preview:
            self.terminate()
        if self._focus_existing_window(show_toast=True):
            return

        self._ensure_window(preview=preview)
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
