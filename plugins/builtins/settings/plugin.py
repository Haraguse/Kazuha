import os
import sys
import subprocess
import json
from PySide6.QtWidgets import QWidget, QApplication
from plugins.interface import AssistantPlugin
from ppt_assistant.core.config import SETTINGS_PATH
from ppt_assistant.ui.dialog import show_webview_input_dialog, show_webview_dialog

class SettingsPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.process = None

    def get_name(self):
        return ""

    def get_icon(self):
        return "settings.svg" 

    def execute(self):
        settings_data = {}
        try:
            if os.path.exists(SETTINGS_PATH):
                with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                    settings_data = json.load(f)
        except Exception:
            settings_data = {}
        security = settings_data.get("Security") or {}
        enabled = bool(security.get("EnablePassword"))
        password = security.get("Password")
        if enabled and isinstance(password, str) and password:
            entered = show_webview_input_dialog("", "", confirm_text="", cancel_text="")
            if entered is None:
                return
            if entered != password:
                proc = show_webview_dialog("密码错误", "设置密码不正确", confirm_text="关闭", cancel_text="", is_error=True, hide_cancel=True)
                proc.communicate()
                return

        if self.process and self.process.poll() is None:
            if sys.platform == "win32":
                try:
                    import ctypes
                    from ctypes import wintypes
                    hwnd = ctypes.windll.user32.FindWindowW(None, "Settings")
                    if hwnd:
                        ctypes.windll.user32.ShowWindow(hwnd, 9)
                        ctypes.windll.user32.SetForegroundWindow(hwnd)
                        class FLASHWINFO(ctypes.Structure):
                            _fields_ = [
                                ("cbSize", wintypes.UINT),
                                ("hwnd", wintypes.HWND),
                                ("dwFlags", wintypes.DWORD),
                                ("uCount", wintypes.UINT),
                                ("dwTimeout", wintypes.DWORD),
                            ]
                        info = FLASHWINFO(
                            ctypes.sizeof(FLASHWINFO),
                            wintypes.HWND(hwnd),
                            3,
                            3,
                            0,
                        )
                        ctypes.windll.user32.FlashWindowEx(ctypes.byref(info))
                except:
                    pass
            return

        base_dir = os.path.dirname(os.path.abspath(__file__))
        html_path = os.path.join(base_dir, "settings.html")
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(base_dir)))
        main_path = os.path.join(root_dir, "main.py")
        
        # Sizing
        width = str(1256)
        height = str(734)

        env = os.environ.copy()
        env["SETTINGS_PATH"] = SETTINGS_PATH

        if getattr(sys, "frozen", False):
            cmd = [
                sys.executable,
                "--webview-runner",
                html_path,
                "Settings",
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
                "Settings",
                width,
                height,
                "true",
            ]

        self.process = subprocess.Popen(cmd, env=env)

    def terminate(self):
        if self.process and self.process.poll() is None:
            self.process.terminate()
            self.process = None
