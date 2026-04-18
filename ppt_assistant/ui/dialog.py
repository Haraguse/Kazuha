import json

from ppt_assistant.ui.dialog_runtime import show_webview_dialog_in_process


def show_webview_dialog(
    title,
    text,
    confirm_text="确认",
    cancel_text="取消",
    is_error=False,
    hide_cancel=False,
    code=None,
):
    return show_webview_dialog_in_process(
        title=title,
        text=text,
        confirm_text=confirm_text,
        cancel_text=cancel_text,
        is_error=is_error,
        hide_cancel=hide_cancel,
        code=code,
    )


def show_webview_input_dialog(
    title,
    text,
    confirm_text="确认",
    cancel_text="取消",
    input_type="password",
    placeholder="",
    hide_cancel=False,
):
    proc = show_webview_dialog_in_process(
        title=title,
        text=text,
        confirm_text=confirm_text,
        cancel_text=cancel_text,
        hide_cancel=hide_cancel,
        code="password_verify",
        input_type=input_type,
        placeholder=placeholder,
    )
    stdout, _ = proc.communicate()
    if "DIALOG_CONFIRMED" not in stdout:
        return None
    value = None
    for line in stdout.splitlines():
        if line.startswith("DIALOG_VALUE:"):
            raw = line[len("DIALOG_VALUE:") :]
            try:
                value = json.loads(raw)
            except Exception:
                value = raw
            break
    return value


class CustomDialog:
    """Wrapper class for compatibility with existing code."""

    def __init__(self, title, text, icon_path=None, parent=None, is_error=False):
        self.title = title
        self.text = text
        self.is_error = is_error
        self.btn_confirm_text = "确定"
        self.btn_cancel_text = "取消"

    def exec(self):
        proc = show_webview_dialog(
            self.title,
            self.text,
            confirm_text=self.btn_confirm_text,
            cancel_text=self.btn_cancel_text,
            is_error=self.is_error,
        )
        stdout, _ = proc.communicate()
        from PySide6.QtWidgets import QDialog

        return QDialog.Accepted if "DIALOG_CONFIRMED" in stdout else QDialog.Rejected

    @property
    def btn_confirm(self):
        class BtnWrapper:
            def __init__(self, outer):
                self.outer = outer

            def setText(self, text):
                self.outer.btn_confirm_text = text

        return BtnWrapper(self)

    @property
    def btn_cancel(self):
        class BtnWrapper:
            def __init__(self, outer):
                self.outer = outer

            def setText(self, text):
                self.outer.btn_cancel_text = text

        return BtnWrapper(self)
