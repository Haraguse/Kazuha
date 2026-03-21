import os
import sys
import subprocess
import json
from PySide6.QtWidgets import QWidget, QApplication
from plugins.interface import AssistantPlugin
from plugins.webview_window_utils import bring_window_to_front, find_window, notify_existing_window
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
        if self.process and self.process.poll() is None:
            if sys.platform == "win32":
                try:
                    hwnd = find_window("Settings", self.process.pid if self.process else None)
                    if hwnd:
                        bring_window_to_front(hwnd)
                        notify_existing_window(hwnd)
                except Exception:
                    pass
                return
            try:
                self.process.terminate()
                self.process.wait(timeout=1)
            except Exception:
                pass
            self.process = None

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
