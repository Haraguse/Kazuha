import json
import os

from PySide6.QtCore import QEventLoop, QObject, QTimer, Qt, Signal, Slot
from PySide6.QtWidgets import QApplication


def _load_json_file(path):
    if not path or not os.path.exists(path):
        return {}
    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
        return data if isinstance(data, dict) else {}
    except Exception:
        return {}


def _resolve_theme_payload():
    theme = "auto"
    accent = "#3275F5"
    try:
        from ppt_assistant.core.config import cfg, Theme, qconfig

        raw_theme = cfg.themeMode.value if hasattr(cfg.themeMode, "value") else "auto"
        if isinstance(raw_theme, Theme):
            if raw_theme == Theme.DARK:
                theme = "dark"
            elif raw_theme == Theme.LIGHT:
                theme = "light"
            else:
                theme = "auto"
        else:
            theme = str(raw_theme).lower()

        resolved_theme = theme
        if theme == "auto":
            try:
                if isinstance(qconfig.theme, Theme):
                    resolved_theme = "dark" if qconfig.theme == Theme.DARK else "light"
            except Exception:
                resolved_theme = "light"
        accent = "#E1EBFF" if resolved_theme == "dark" else "#3275F5"
    except Exception:
        pass
    return theme, accent


def build_dialog_data(
    title,
    text,
    confirm_text="确认",
    cancel_text="取消",
    is_error=False,
    hide_cancel=False,
    code=None,
    input_type=None,
    placeholder="",
):
    theme, accent = _resolve_theme_payload()
    dialog_data = {
        "title": title,
        "text": text,
        "confirmText": confirm_text,
        "cancelText": cancel_text,
        "isError": is_error,
        "hideCancel": hide_cancel,
        "theme": theme,
        "accentColor": accent,
    }
    if code is not None:
        dialog_data["code"] = code
    if input_type is not None:
        dialog_data["inputType"] = input_type
        dialog_data["inputPlaceholder"] = placeholder
    return dialog_data


class InProcessDialogApiSignalBridge(QObject):
    finished = Signal(str)


class InProcessDialogApi:
    pass


def _build_dialog_api(base_api_cls, dialog_data):
    class DialogApi(base_api_cls):
        def __init__(self):
            super().__init__()
            self.set_in_process(True)
            self.dialog_data = dialog_data
            self._signal_bridge = InProcessDialogApiSignalBridge()
            self._completed = False

        def _finish(self, status_line, value=None):
            if self._completed:
                return
            self._completed = True
            lines = []
            if value is not None:
                try:
                    payload = json.dumps(value, ensure_ascii=False)
                except Exception:
                    payload = json.dumps(str(value), ensure_ascii=False)
                lines.append(f"DIALOG_VALUE:{payload}")
            lines.append(status_line)
            stdout = "\n".join(lines)
            if stdout:
                stdout += "\n"
            self._signal_bridge.finished.emit(stdout)
            self._close_current_window()

        @Slot()
        def on_confirm(self):
            self._finish("DIALOG_CONFIRMED")

        @Slot(str)
        def on_confirm_with_value(self, value):
            self._finish("DIALOG_CONFIRMED", value)

        @Slot()
        def on_cancel(self):
            self._finish("DIALOG_CANCELLED")

    return DialogApi()


class InProcessDialogHandle(QObject):
    def __init__(self, window, api):
        super().__init__()
        self._window = window
        self._api = api
        self._stdout = ""
        self._done = False
        self._exit_code = None
        self._loop = None
        self._api._signal_bridge.finished.connect(self._on_finished)
        self._window.destroyed.connect(self._on_destroyed)

    def _finish(self, stdout):
        if self._done:
            return
        self._done = True
        self._stdout = stdout or ""
        self._exit_code = 0
        if self._loop is not None and self._loop.isRunning():
            self._loop.quit()

    def _on_finished(self, stdout):
        self._finish(stdout)

    def _on_destroyed(self, *_args):
        if self._done:
            return
        self._finish("DIALOG_CANCELLED\n")
        self._window = None

    @property
    def pid(self):
        return None

    def poll(self):
        return None if not self._done else self._exit_code

    def wait(self, timeout=None):
        if self._done:
            return self._exit_code
        app = QApplication.instance()
        if app is None:
            raise RuntimeError("QApplication has not been created")
        self._loop = QEventLoop()
        timeout_timer = None
        if timeout is not None:
            timeout_timer = QTimer()
            timeout_timer.setSingleShot(True)
            timeout_timer.timeout.connect(self._loop.quit)
            timeout_timer.start(int(timeout * 1000))
        self._loop.exec()
        if timeout_timer is not None:
            timeout_timer.stop()
        self._loop = None
        return self._exit_code

    def communicate(self, timeout=None):
        self.wait(timeout=timeout)
        return self._stdout, ""

    def terminate(self):
        if self._window is not None:
            try:
                self._window.close()
            except Exception:
                pass


def show_webview_dialog_in_process(
    title,
    text,
    confirm_text="确认",
    cancel_text="取消",
    is_error=False,
    hide_cancel=False,
    code=None,
    input_type=None,
    placeholder="",
):
    app = QApplication.instance()
    if app is None:
        raise RuntimeError("QApplication has not been created")

    import plugins.webview_runner as webview_runner

    dialog_data = build_dialog_data(
        title=title,
        text=text,
        confirm_text=confirm_text,
        cancel_text=cancel_text,
        is_error=is_error,
        hide_cancel=hide_cancel,
        code=code,
        input_type=input_type,
        placeholder=placeholder,
    )

    root_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    html_path = os.path.join(root_dir, "ppt_assistant", "ui", "dialog.html")

    webview_runner._warmup_webengine()

    api = _build_dialog_api(webview_runner.Api, dialog_data)
    try:
        from ppt_assistant.core.config import SETTINGS_PATH

        api.settings = _load_json_file(SETTINGS_PATH)
    except Exception:
        api.settings = {}
    api.version = _load_json_file(os.path.join(root_dir, "version.json"))
    theme_mode = dialog_data.get("theme", "auto")
    window = webview_runner.MainWindow(
        dialog_data.get("title", "Dialog"),
        html_path,
        api,
        650,
        500,
        theme_mode,
        defer_load=False,
    )
    window.setAttribute(Qt.WA_DeleteOnClose, True)
    window.setWindowModality(Qt.ApplicationModal)
    window.show()
    try:
        window.raise_()
        window.activateWindow()
    except Exception:
        pass
    return InProcessDialogHandle(window, api)
