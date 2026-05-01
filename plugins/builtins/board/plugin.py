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

    def execute(self):
        # Re-create window if closed or create for the first time
        if not self.window:
            self.window = BoardWindow()
            # If the window is closed, we might want to clear the reference
            # but QQuickView close() just hides it.
            # We'll rely on our showEvent to handle the slide-in.

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
            self.window = None
