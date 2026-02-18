from PySide6.QtCore import QObject, QTimer, Signal, Slot, QThread

class TimerWorker(QObject):
    updated = Signal(int)
    finished = Signal()
    
    def __init__(self):
        super().__init__()
        self.timer = None
        self.remaining_seconds = 0.0
        self.is_running = False

    @Slot(int)
    def start(self, seconds):
        self.remaining_seconds = float(seconds)
        self.is_running = True
        if not self.timer:
            self.timer = QTimer(self)
            self.timer.setInterval(100)
            self.timer.timeout.connect(self._tick)
        self.timer.start()
        self.updated.emit(int(self.remaining_seconds))

    @Slot()
    def pause(self):
        self.is_running = False
        if self.timer:
            self.timer.stop()

    @Slot()
    def resume(self):
        if self.remaining_seconds > 0:
            self.is_running = True
            if not self.timer:
                self.timer = QTimer(self)
                self.timer.setInterval(100)
                self.timer.timeout.connect(self._tick)
            self.timer.start()

    @Slot()
    def stop(self):
        self.remaining_seconds = 0.0
        self.is_running = False
        if self.timer:
            self.timer.stop()
        self.updated.emit(0)

    def _tick(self):
        if self.remaining_seconds > 0:
            self.remaining_seconds -= 0.1
            if self.remaining_seconds <= 0:
                self.remaining_seconds = 0
                self.is_running = False
                if self.timer:
                    self.timer.stop()
                self.finished.emit()
            else:
                self.updated.emit(int(self.remaining_seconds))
        else:
            self.stop()

class TimerManager(QObject):
    updated = Signal(int)
    finished = Signal()
    state_changed = Signal(bool) # is_running

    # Signals to control worker
    _request_start = Signal(int)
    _request_pause = Signal()
    _request_resume = Signal()
    _request_stop = Signal()

    _instance = None

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super(TimerManager, cls).__new__(cls)
            cls._instance._initialized = False
        return cls._instance

    def __init__(self):
        if self._initialized:
            return
        super().__init__()
        self._initialized = True
        
        # Create worker thread
        self._thread = QThread()
        self._worker = TimerWorker()
        self._worker.moveToThread(self._thread)
        
        # Connect control signals
        self._request_start.connect(self._worker.start)
        self._request_pause.connect(self._worker.pause)
        self._request_resume.connect(self._worker.resume)
        self._request_stop.connect(self._worker.stop)
        
        # Connect feedback signals
        self._worker.updated.connect(self.updated)
        self._worker.finished.connect(self.finished)
        
        # Start thread
        self._thread.start()

    @property
    def remaining_seconds(self):
        # Accessing worker state from main thread is not strictly thread-safe without mutex
        # But for reading a float/bool it's usually fine in Python due to GIL
        return self._worker.remaining_seconds

    @property
    def is_running(self):
        return self._worker.is_running

    @Slot(int)
    def start(self, seconds):
        self._request_start.emit(seconds)
        self.state_changed.emit(True)

    @Slot()
    def pause(self):
        self._request_pause.emit()
        self.state_changed.emit(False)

    @Slot()
    def resume(self):
        self._request_resume.emit()
        self.state_changed.emit(True)

    @Slot()
    def stop(self):
        self._request_stop.emit()
        self.state_changed.emit(False)

    @Slot()
    def finish(self):
        # finish logic usually just stops
        self.stop()
        self.finished.emit()

    def get_remaining_time_str(self):
        val = int(self.remaining_seconds)
        hours = val // 3600
        minutes = (val % 3600) // 60
        seconds = val % 60
        if hours > 0:
            return f"{hours:02}:{minutes:02}:{seconds:02}"
        return f"{minutes:02}:{seconds:02}"

    def __del__(self):
        if hasattr(self, '_thread'):
            self._thread.quit()
            self._thread.wait()
