import os
import json
import sys
import subprocess

from PySide6.QtCore import QTimer

from plugins.interface import AssistantPlugin
from plugins.in_process_window_handle import InProcessWindowHandle
from ppt_assistant.core.config import get_active_settings_path


class OnboardingPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.process = None
        self._window = None
        self._api = None
        self._wv = None
        self._preview = False

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

    def _launch_external_window(self, preview=False):
        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "onboarding.html")
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(base_dir)))
        main_path = os.path.join(root_dir, "main.py")
        env = os.environ.copy()
        env.setdefault("NO_AT_BRIDGE", "1")
        env.setdefault("QT_ACCESSIBILITY", "0")
        _cur = env.get("QTWEBENGINE_CHROMIUM_FLAGS", "")
        if "--disable-renderer-accessibility" not in _cur:
            env["QTWEBENGINE_CHROMIUM_FLAGS"] = (_cur + " --disable-renderer-accessibility").strip()
        env["SETTINGS_PATH"] = get_active_settings_path()
        env["ONBOARDING_PREVIEW"] = "true" if preview else "false"
        title = "Onboarding Preview" if preview else "Onboarding"
        width = "960"
        height = "720"
        if getattr(sys, "frozen", False):
            cmd = [
                sys.executable,
                "--webview-runner",
                html_path,
                title,
                width,
                height,
                "true",
            ]
        else:
            cmd = [
                sys.executable,
                main_path,
                "--webview-runner",
                html_path,
                title,
                width,
                height,
                "true",
            ]
        # Use InProcessWindowHandle for external process to prevent immediate detection of "closed"
        # The external webview runner will create its own process group
        creationflags = (
            getattr(subprocess, "CREATE_NO_WINDOW", 0x08000000)
            | getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0)
            | getattr(subprocess, "DETACHED_PROCESS", 0)
        )
        self.process = subprocess.Popen(cmd, env=env, creationflags=creationflags)
        self._window = None
        self._api = None

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
        settings_path = get_active_settings_path()
        api.settings = self._load_json_file(settings_path)
        api.version = self._load_json_file(version_path)
        api.version["device_uuid"] = wv.get_device_uuid()[:8]

        previous_preview = os.environ.get("ONBOARDING_PREVIEW")
        os.environ["SETTINGS_PATH"] = settings_path
        os.environ["ONBOARDING_PREVIEW"] = "true" if preview else "false"
        try:
            title = "Onboarding Preview" if preview else "Onboarding"
            defer_load = wv._should_defer_initial_load(html_path, title, True)
            theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
            use_native = api.settings.get("General", {}).get("UseNativeTitleBar", False)
            window = wv.MainWindow(
                title, html_path, api, 960, 720, theme_mode, not use_native, defer_load, frameless=not use_native
            )
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
        if not preview:
            if self.process is not None and self.process.poll() is None:
                return
            self._launch_external_window(preview=False)
            return
        if self._window is not None and self._preview != preview:
            self.terminate()
        if self._focus_existing_window():
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
