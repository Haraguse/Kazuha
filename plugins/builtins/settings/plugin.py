import os
import json
import sys
import gc
import subprocess

from PySide6.QtCore import QTimer
from PySide6.QtWidgets import QApplication

from plugins.interface import AssistantPlugin
from plugins.webview_window_utils import bring_window_to_front
from ppt_assistant.core.config import get_active_settings_path, cfg


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
    env["SETTINGS_PATH"] = get_active_settings_path()
    if sys.platform != "linux":
        return env
    if env.get("DISPLAY"):
        env["QT_QPA_PLATFORM"] = "xcb"
    env["QTWEBENGINE_DISABLE_SANDBOX"] = "1"
    env.setdefault("NO_AT_BRIDGE", "1")
    env.setdefault("QT_ACCESSIBILITY", "0")
    _cur = env.get("QTWEBENGINE_CHROMIUM_FLAGS", "")
    if "--disable-renderer-accessibility" not in _cur:
	    env["QTWEBENGINE_CHROMIUM_FLAGS"] = (_cur + " --disable-renderer-accessibility").strip()
    env["DEFER_WEBENGINE_LOAD"] = "1"
    return env


class SettingsPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.process = None
        self._window = None
        self._api = None
        self._wv = None
        self._context = None
        self._hidden = False
        self._close_blocked = False

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

    def prewarm(self, shell=True):
        """Warm shared Chromium; optionally keep a hidden defer-load settings shell."""
        if _use_external_webview_process():
            return
        try:
            wv = self._ensure_webview_module()
            wv._warmup_webengine(retain_placeholder=True)
            if not shell:
                return
            if self._window is not None:
                if not self._hidden:
                    return
                return
            wv._release_warmup_placeholder()
            self._ensure_window(show_if_hidden=False)
            if self._window is not None:
                try:
                    if not self._window.isVisible():
                        self._window.hide()
                except Exception:
                    pass
            self._hidden = True
            self._close_blocked = False
        except Exception:
            pass

    def prewarm_load_content(self):
        """Background-load settings.html into the hidden shell."""
        if _use_external_webview_process():
            return
        if self._window is None or not self._hidden:
            return
        try:
            pending = getattr(self._window, "_pending_url", None)
            if pending is None:
                return
            self._ensure_webview_module()._release_warmup_placeholder()
            self._window._ensure_pending_load()
            print("[Settings] Background content prewarm started", flush=True)
        except Exception:
            pass

    def _ensure_webview_module(self):
        if self._wv is None:
            import plugins.webview_runner as webview_runner

            self._wv = webview_runner
        return self._wv

    def _should_hide_on_close(self):
        try:
            return bool(cfg.hideOnClose.value)
        except Exception:
            return True

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
            bring_window_to_front(int(self._window.winId()))
            return True
        except RuntimeError:
            self._window = None
            self._api = None
            self.process = None
            self._hidden = False
            return False

    def _on_window_destroyed(self, *_args):
        if self._close_blocked:
            return
        self._window = None
        self._api = None
        self.process = None
        self._hidden = False

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
            self._window.setWindowOpacity(1.0)
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
            pending = getattr(self._window, "_pending_url", None)
            # Ensure background color is applied before showing to avoid white flash
            try:
                self._window.setWindowOpacity(0.0)
                self._window._apply_page_background()
            except Exception:
                pass
            if self._window.isMinimized():
                self._window.showNormal()
            else:
                self._window.show()
            if pending is not None and getattr(self._window, "_pending_url", None) is not None:
                try:
                    self._window._ensure_pending_load()
                except Exception:
                    pass
            self._window.raise_()
            self._window.activateWindow()
            bring_window_to_front(int(self._window.winId()))
            # Restore opacity after a short delay to let Chromium render
            from PySide6.QtCore import QTimer as _QTimer
            _QTimer.singleShot(80, lambda: self._window.setWindowOpacity(1.0))
        except RuntimeError:
            self._window = None
            self._api = None
            self._hidden = False

    def _release_webengine_resources(self):
        # Only trim process working set on hide; keep page content for fast restore.
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

    def _launch_external_window(self):
        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "settings.html")
        use_native = self._get_use_native_title_bar()
        cmd = _build_webview_runner_command(html_path, "Settings", 1256, 734, not use_native)
        env = _build_linux_webview_env()
        self.process = subprocess.Popen(cmd, env=env, close_fds=True)
        return self.process

    def _get_use_native_title_bar(self):
        settings = self._load_json_file(get_active_settings_path())
        return settings.get("General", {}).get("UseNativeTitleBar", False)

    def _ensure_window(self, show_if_hidden=True):
        if self._window is not None:
            if self._hidden and show_if_hidden:
                self._restore_window()
            return self._window

        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "settings.html")
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(base_dir)))
        version_path = os.path.join(root_dir, "version.json")

        wv = self._ensure_webview_module()
        wv._warmup_webengine(retain_placeholder=False)

        api = wv.Api()
        api.set_in_process(True)
        api.settings = self._load_json_file(get_active_settings_path())
        api.version = self._load_json_file(version_path)
        api.platform = sys.platform

        api.trigger_resource_alert = lambda: self.trigger_resource_alert()
        api.quit_app_for_update = lambda: self.quit_app_for_update()

        theme_mode = api.settings.get("Appearance", {}).get("ThemeMode", "Auto")
        defer_load = wv._should_defer_initial_load(html_path, "Settings", True)
        use_native = api.settings.get("General", {}).get("UseNativeTitleBar", False)
        frameless = not use_native
        window = wv.MainWindow(
            "Settings",
            html_path,
            api,
            1256,
            734,
            theme_mode,
            frameless,
            defer_load,
            frameless=frameless,
            defer_until_show=True,
        )
        window.setMinimumWidth(1099)

        original_close_event = window.closeEvent

        def patched_close_event(event):
            if self._should_hide_on_close():
                self._intercept_close(event)
            else:
                self._close_blocked = False
                original_close_event(event)

        window.closeEvent = patched_close_event
        window.destroyed.connect(self._on_window_destroyed)

        self._api = api
        self._window = window
        self.process = None
        self._hidden = False
        self._close_blocked = False
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
        try:
            self._window.setWindowOpacity(0.0)
            self._window._apply_page_background()
        except Exception:
            pass
        self._window.show()
        try:
            self._window.raise_()
            self._window.activateWindow()
            bring_window_to_front(int(self._window.winId()))
        except Exception:
            pass
        from PySide6.QtCore import QTimer as _QTimer
        _QTimer.singleShot(80, lambda: self._window.setWindowOpacity(1.0))

    def terminate(self):
        self._close_blocked = False
        self._hidden = False
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
