import json
import os
import random
import time

from PySide6.QtCore import QEvent, QEventLoop, QObject, QTimer, Qt, Signal, Slot
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


# 用户在 2 秒内尝试关闭 dialog 时，随机挑一条怼回去
_SCOLD_MESSAGES = [
    "你急什么急？内容都没看就要关？",
    "2秒都等不了？这么浮躁别写代码了！",
    "眼睛长哪去了？内容看完了吗就关！",
    "这么着急关闭，怕看见自己的bug吗？",
    "点这么快干嘛？手抽筋了？",
    "关什么关？老老实实看完再说！",
]

_SCOLD_THRESHOLD = 2.0  # 秒


def _build_scold_js():
    """生成注入到 dialog 页面的 JS：仿照 settings 的蒙层+居中气泡 toast 辱骂用户。"""
    msg_json = json.dumps(random.choice(_SCOLD_MESSAGES), ensure_ascii=False)
    return (
        "(function(){"
        # 注入样式（仅一次）
        "if(!document.getElementById('dialog-scold-style')){"
        "var s=document.createElement('style');"
        "s.id='dialog-scold-style';"
        "s.textContent="
        "'#dialog-scold-overlay{position:fixed;inset:0;background:rgba(0,0,0,0.32);"
        "backdrop-filter:blur(12px);-webkit-backdrop-filter:blur(12px);"
        "z-index:99999;opacity:0;display:flex;align-items:center;justify-content:center;"
        "pointer-events:none;transition:opacity 0.3s cubic-bezier(0.25,1,0.5,1);}'"
        "+'#dialog-scold-overlay.show{opacity:1;}'"
        "+'#dialog-scold-overlay .scold-bubble{color:#fff;font-size:16px;font-weight:500;"
        "text-align:center;padding:12px 28px;border-radius:14px;"
        "background:rgba(30,30,30,0.72);backdrop-filter:blur(20px);"
        "-webkit-backdrop-filter:blur(20px);border:0.5px solid rgba(255,255,255,0.12);"
        "scale:0.92;opacity:0;max-width:80vw;line-height:1.5;"
        "transition:opacity 0.22s cubic-bezier(0.25,1,0.5,1),scale 0.22s cubic-bezier(0.25,1,0.5,1);}'"
        "+'#dialog-scold-overlay.show .scold-bubble{opacity:1;scale:1;}'"
        "+'[data-theme=\"dark\"] #dialog-scold-overlay{background:rgba(0,0,0,0.42);}';"
        "document.head.appendChild(s);"
        "}"
        # 移除旧实例
        "var old=document.getElementById('dialog-scold-overlay');"
        "if(old)old.remove();"
        # 创建蒙层 + 气泡
        "var o=document.createElement('div');"
        "o.id='dialog-scold-overlay';"
        "var b=document.createElement('div');"
        "b.className='scold-bubble';"
        "b.innerText=" + msg_json + ";"
        "o.appendChild(b);"
        "document.body.appendChild(o);"
        # 触发淡入
        "requestAnimationFrame(function(){o.classList.add('show');});"
        # 2 秒后淡出并移除
        "setTimeout(function(){"
        "o.classList.remove('show');"
        "setTimeout(function(){if(o&&o.parentNode)o.remove();},320);"
        "},2000);"
        "})();"
    )


class _DialogCloseGuard(QObject):
    """事件过滤器：2 秒内拦截窗口关闭事件（Alt+F4 / 系统菜单关闭）并辱骂用户。

    仅拦截用户主动触发的关闭；程序化关闭（_finish 之后）通过 _allow_close 放行。
    """

    def __init__(self, api, parent=None):
        super().__init__(parent)
        self._api = api

    def eventFilter(self, obj, event):
        try:
            if event.type() == QEvent.Close:
                # 程序化关闭（确认/取消正常流程）直接放行
                if getattr(self._api, "_allow_close", False):
                    return False
                # 用户主动关闭，2 秒内拦截并辱骂
                if self._api._is_too_soon():
                    self._api._scold_user()
                    return True
        except Exception:
            pass
        return super().eventFilter(obj, event)


def _build_dialog_api(base_api_cls, dialog_data):
    class DialogApi(base_api_cls):
        def __init__(self):
            super().__init__()
            self.set_in_process(True)
            self.dialog_data = dialog_data
            self._signal_bridge = InProcessDialogApiSignalBridge()
            self._completed = False
            # 加载完成后才记录开窗时刻；None 表示页面尚未加载完成
            self._open_time = None
            # 程序化关闭放行标志，避免 _finish 触发的 close 被拦截
            self._allow_close = False

        def _is_too_soon(self):
            # 页面还没加载完，也算"太快"，一律拦截
            if self._open_time is None:
                return True
            return (time.monotonic() - self._open_time) < _SCOLD_THRESHOLD

        def _scold_user(self):
            """通过 JS 注入红色抖动 toast，把用户骂回去。"""
            try:
                window = getattr(self, "_window", None)
                if window is None:
                    return
                page = window.page()
                if page is None:
                    return
                page.runJavaScript(_build_scold_js())
            except Exception:
                pass

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
            # 标记为程序化关闭，让事件过滤器放行
            self._allow_close = True
            self._close_current_window()

        @Slot()
        def on_confirm(self):
            self._finish("DIALOG_CONFIRMED")

        @Slot(str)
        def on_confirm_with_value(self, value):
            self._finish("DIALOG_CONFIRMED", value)

        @Slot()
        def on_cancel(self):
            # 2 秒内点取消，先骂回去，不让关
            if self._is_too_soon():
                self._scold_user()
                return
            self._finish("DIALOG_CANCELLED")

        @Slot()
        def close_window(self):
            # 关闭按钮（X）也在 2 秒内被拦截
            if self._is_too_soon():
                self._scold_user()
                return
            self._allow_close = True
            try:
                if self._window:
                    self._window.close()
            except Exception:
                pass

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
    width=650,
    height=500,
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

    root_dir = os.path.dirname(
        os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    )
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
        width,
        height,
        theme_mode,
        custom_border=True,
        defer_load=False,
        frameless=True,
    )
    window.setAttribute(Qt.WA_DeleteOnClose, True)
    window.setWindowModality(Qt.ApplicationModal)
    # 安装关闭事件过滤器：2 秒内拦截 Alt+F4 / 系统菜单关闭并辱骂用户
    close_guard = _DialogCloseGuard(api)
    window.installEventFilter(close_guard)
    api._close_guard = close_guard  # 防止被 GC 回收
    # 页面加载完成后才开始 2 秒倒计时，避免用户连内容都没看见就被骂
    window.loadFinished.connect(lambda _ok: setattr(api, "_open_time", time.monotonic()))
    window.show()
    try:
        window.raise_()
        window.activateWindow()
    except Exception:
        pass
    return InProcessDialogHandle(window, api)
