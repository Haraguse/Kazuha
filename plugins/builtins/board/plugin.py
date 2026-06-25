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
        print(f"[BoardPlugin] execute() called, window={self.window}", flush=True)
        if self.window:
            print(f"[BoardPlugin] window exists, activating", flush=True)
            self._activate_window()
            return

        # Defer window creation to next event loop iteration so the UI
        # doesn't freeze while the QML scene graph loads.
        print(
            f"[BoardPlugin] Deferring window creation via QTimer.singleShot", flush=True
        )
        QTimer.singleShot(0, self._create_and_show)

    def _create_and_show(self):
        from PySide6.QtCore import qInstallMessageHandler

        original_handler = qInstallMessageHandler(None)
        print(f"[BoardPlugin] _create_and_show() called", flush=True)
        if self.window:
            print(f"[BoardPlugin] window already exists, aborting create", flush=True)
            return
        try:
            print(f"[BoardPlugin] Creating BoardWindow...", flush=True)
            self.window = BoardWindow()
            print(f"[BoardPlugin] BoardWindow created, connecting signal", flush=True)
            self.window.window_fully_closed.connect(self._on_window_fully_closed)
            print(f"[BoardPlugin] Processing events...", flush=True)
            QApplication.processEvents()
            print(f"[BoardPlugin] Showing window...", flush=True)
            self.window.show()
            self.window.raise_()
            self.window.activateWindow()
            self.window.raise_()
            print(f"[BoardPlugin] Window shown successfully", flush=True)
        except Exception as e:
            print(f"[BoardPlugin] Failed to create board window: {e}", flush=True)
            import traceback

            traceback.print_exc()
            if self.window is not None:
                try:
                    self.window.window_fully_closed.disconnect(
                        self._on_window_fully_closed
                    )
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
