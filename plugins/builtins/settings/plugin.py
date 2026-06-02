import os
import json
import sys
import subprocess

from PySide6.QtCore import QTimer

from plugins.interface import AssistantPlugin
from plugins.webview_window_utils import bring_window_to_front
from ppt_assistant.core.config import SETTINGS_PATH


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


def _build_linux_webview_env():
    env = os.environ.copy()
    env["SETTINGS_PATH"] = SETTINGS_PATH
    if sys.platform != "linux":
        return env
    if env.get("DISPLAY"):
        env["QT_QPA_PLATFORM"] = "xcb"
    # env["QT_OPENGL"] = "software"
    # env["QT_RHI_BACKEND"] = "software"
    # env["QT_VULKAN_DISABLE"] = "1"
    # env["QT_QUICK_BACKEND"] = "software"
    # env["QT_XCB_FORCE_SOFTWARE_OPENGL"] = "1"
    env["QTWEBENGINE_DISABLE_SANDBOX"] = "1"
    env["DEFER_WEBENGINE_LOAD"] = "1"
    flags = [
        "--disable-gpu",
        "--disable-gpu-compositing",
        "--enable-software-rasterizer",
        "--disable-vulkan",
        "--no-sandbox",
    ]
    merged = str(env.get("QTWEBENGINE_CHROMIUM_FLAGS", "")).split()
    for flag in flags:
        if flag not in merged:
            merged.append(flag)
    env["QTWEBENGINE_CHROMIUM_FLAGS"] = " ".join(merged).strip()
    return env


class SettingsPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.process = None
        self._window = None
        self._api = None
        self._wv = None
        self._context = None
        if not _use_external_webview_process():
            QTimer.singleShot(1200, self._prewarm_webview)

    def get_name(self):
        return ""

    def get_icon(self):
        return "settings.svg"

    def set_context(self, context):
        super().set_context(context)
        self._context = context

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
            bring_window_to_front(int(self._window.winId()))
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

    def _launch_external_window(self):
        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "settings.html")
        use_native = self._get_use_native_title_bar()
        cmd = _build_webview_runner_command(html_path, "Settings", 1256, 734, not use_native)
        env = _build_linux_webview_env()
        self.process = subprocess.Popen(cmd, env=env, close_fds=True)
        return self.process

    def _get_use_native_title_bar(self):
        settings = self._load_json_file(SETTINGS_PATH)
        return settings.get("General", {}).get("UseNativeTitleBar", False)

    def _ensure_window(self):
        if self._window is not None:
            return self._window

        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "settings.html")
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(base_dir)))
        version_path = os.path.join(root_dir, "version.json")

        wv = self._ensure_webview_module()
        wv._warmup_webengine()

        api = wv.Api()
        api.set_in_process(True)
        api.settings = self._load_json_file(SETTINGS_PATH)
        api.version = self._load_json_file(version_path)
        api.platform = sys.platform  # Pass platform info to frontend

        # 为api添加trigger_resource_alert方法
        # 使用lambda创建可调用的方法
        api.trigger_resource_alert = lambda: self.trigger_resource_alert()
        api.quit_app_for_update = lambda: self.quit_app_for_update()

        theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
        defer_load = wv._should_defer_initial_load(html_path, "Settings", True)
        use_native = api.settings.get("General", {}).get("UseNativeTitleBar", False)
        frameless = not use_native
        window = wv.MainWindow(
            "Settings", html_path, api, 1256, 734, theme_mode, frameless, defer_load, frameless=frameless
        )
        window.setMinimumWidth(1099)
        window.destroyed.connect(self._on_window_destroyed)

        self._api = api
        self._window = window
        self.process = None
        return window

    def execute(self):
        if _use_external_webview_process():
            if self.process is not None and self.process.poll() is None:
                return
            self._launch_external_window()
            return

        if self._focus_existing_window(show_toast=True):
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
        if self.process is not None and self.process.poll() is None:
            try:
                self.process.terminate()
            except Exception:
                pass
        if self._window is not None:
            try:
                self._window.close()
            except Exception:
                pass
        self._window = None
        self._api = None
        self.process = None

    def trigger_resource_alert(self):
        """调试用：触发资源监测通知"""
        try:
            print("[Settings] Trigger resource alert called")
            if self._context is None:
                print("[Settings] Context not available")
                return

            if not hasattr(self._context, 'tray'):
                print("[Settings] Context has no 'tray' attribute")
                return

            tray = self._context.tray
            if tray is None:
                print("[Settings] Tray is None")
                return

            from ppt_assistant.core.i18n import t
            title = t("resource.monitor.title")
            body = t("resource.monitor.body")
            tray.show_message(title, body)
            print("[Settings] Resource alert triggered (debug)")
        except Exception as e:
            print(f"[Settings] Error triggering resource alert: {e}")
            import traceback
            traceback.print_exc()

    def quit_app_for_update(self):
        try:
            if self._context is None:
                return False
            if hasattr(self._context, "_prepare_shutdown"):
                self._context._prepare_shutdown(restarting=False)
            if hasattr(self._context, "app") and self._context.app is not None:
                QTimer.singleShot(0, self._context.app.quit)
                return True
        except Exception:
            pass
        return False

    def navigate_to_section(self, section_key: str, retries: int = 8):
        try:
            if self._window is not None:
                self._window.page().runJavaScript(
                    f"if(typeof navigateToSection === 'function') {{ navigateToSection('{section_key}'); 'OK'; }} else {{ 'WAIT'; }}",
                    lambda result: QTimer.singleShot(400, lambda: self.navigate_to_section(section_key, retries - 1)) if result == 'WAIT' and retries > 0 else None
                )
        except Exception:
            pass
