
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
        if self.window and self.window.isVisible():
            self.window.requestActivate()
            self.window.raise_()
            return

        # Re-create window if closed, or create for the first time
        if not self.window:
            self.window = BoardWindow()
            # Handle window closing to cleanup reference? 
            # QQuickView doesn't emit close signal easily compatible with cleanup, 
            # but we can check isVisible in execute.
            # Actually, if the user closes the window, the object might still exist.
            # We can connect to closing signal if we subclass properly or just check isVisible.
            
            # To properly handle cleanup when window is closed by user (via UI):
            # We can expose a signal from backend or just rely on re-show.
        
        self.window.show()

    def terminate(self):
        if self.window:
            self.window.close()
            self.window = None
