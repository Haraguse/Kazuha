from PySide6.QtCore import QTimer
from PySide6.QtWidgets import QApplication
from plugins.interface import AssistantPlugin
from .board_window import BoardWindow


class BoardPlugin(AssistantPlugin):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.window = None

    def get_name(self):
        return "板中板"

    def get_icon(self):
        return "board-in-board.svg"

    def _on_window_fully_closed(self):
        """Discard the window reference after it has fully closed.

        QQuickView destroys its QML scene graph on close.  Re-showing a
        closed QQuickView causes a crash (segfault / ASSERT failures in
        the Qt scene-graph code).  By clearing the reference we force a
        brand-new BoardWindow to be created on the next execute() call.
        """
        if self.window is not None:
            self.window.window_fully_closed.disconnect(self._on_window_fully_closed)
            self.window.deleteLater()
        self.window = None

    def _activate_window(self):
        if not self.window:
            return
        try:
            self.window.activateWindow()
            self.window.raise_()
        except Exception:
            pass

    def execute(self):
        if self.window:
            self._activate_window()
            return

        # Defer window creation to next event loop iteration so the UI
        # doesn't freeze while the QML scene graph loads.
        QTimer.singleShot(0, self._create_and_show)

    def _create_and_show(self):
        if self.window:
            return
        try:
            self.window = BoardWindow()
            self.window.window_fully_closed.connect(self._on_window_fully_closed)

            target_geo = getattr(self, '_target_screen_geometry', None)
            if target_geo is not None and not target_geo.isEmpty():
                window_width = self.window.width()
                window_height = self.window.height()
                if window_width <= 0:
                    window_width = 800
                if window_height <= 0:
                    window_height = 600
                x = target_geo.x() + (target_geo.width() - window_width) // 2
                y = target_geo.y() + (target_geo.height() - window_height) // 2
                self.window.move(x, y)

            QApplication.processEvents()
            self.window.show()
            self.window.raise_()
            self.window.activateWindow()
            self.window.raise_()
        except Exception as e:
            print(f"[BoardPlugin] Failed to create board window: {e}")
            import traceback
            traceback.print_exc()
            if self.window is not None:
                try:
                    self.window.window_fully_closed.disconnect(self._on_window_fully_closed)
                    self.window.deleteLater()
                except Exception:
                    pass
                self.window = None

    def set_pen_color(self, r, g, b):
        if self.window:
            self.window.set_pen_color(r, g, b)

    def terminate(self):
        if self.window:
            # Force immediate close without animation for cleanup
            self.window._force_close = True
            self.window.close()
            self.window.window_fully_closed.disconnect(self._on_window_fully_closed)
            self.window = None
