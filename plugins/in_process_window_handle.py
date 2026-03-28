import time

from PySide6.QtWidgets import QApplication


class InProcessWindowHandle:
    def __init__(self, window):
        self._window = window
        self._closed = False
        try:
            window.destroyed.connect(self._mark_closed)
        except Exception:
            pass

    def _mark_closed(self, *_args):
        self._closed = True
        self._window = None

    @property
    def pid(self):
        return None

    def poll(self):
        return None if not self._closed else 0

    def terminate(self):
        if self._closed or self._window is None:
            return
        try:
            self._window.close()
        except Exception:
            pass

    def wait(self, timeout=None):
        start = time.time()
        while self.poll() is None:
            app = QApplication.instance()
            if app is not None:
                app.processEvents()
            time.sleep(0.01)
            if timeout is not None and (time.time() - start) >= timeout:
                raise TimeoutError("Window did not close before timeout")
        return 0
