import sys
from plugins.interface import AssistantPlugin
from .spotlight_window import SpotlightWindow
from PySide6.QtCore import QTimer


class SpotlightPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.window = None

    def get_name(self):
        return "聚光灯"

    def get_icon(self):
        return "spotlight.svg"

    def execute(self):
        if self.window and self.window.isVisible():
            self.terminate()
            return

        self.window = SpotlightWindow()
        # Position on target screen if specified
        target_geo = getattr(self, '_target_screen_geometry', None)
        if target_geo is not None:
            self.window.setGeometry(target_geo)
        self.window.show()
        # Re-capture after positioning on the correct screen
        if target_geo is not None:
            QTimer.singleShot(50, self.window.capture_screen)

    def terminate(self):
        if self.window:
            self.window.close()
            self.window = None
