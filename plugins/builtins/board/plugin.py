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

    def execute(self):
        # Re-create window if closed or create for the first time.
        # A QQuickView that has been closed cannot be re-shown safely
        # because its QML scene graph is destroyed on close.
        if not self.window:
            self.window = BoardWindow()
            self.window.window_fully_closed.connect(self._on_window_fully_closed)

        if self.window.isVisible():
            self.window.requestActivate()
            self.window.raise_()
        else:
            self.window.show()
            self.window.raise_()
            self.window.requestActivate()
            self.window.raise_()

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
