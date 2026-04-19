import sys

from PySide6.QtCore import QObject, QTimer, Signal, Slot

from ppt_assistant.core.system.linux import (
    can_use_xdotool,
    format_window_snapshot,
    get_active_window_snapshot,
    snapshot_is_transient,
    window_looks_like_slideshow,
)


class LinuxFocusWatcher(QObject):
    focus_on_slideshow_changed = Signal(bool)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._slideshow_running = False
        self._slideshow_hwnd = 0
        self._last_focus_on_slideshow = None
        self._poll_timer = None
        self._polling_active = False
        self._availability_warned = False
        self._transient_hold_budget = 1

    def _log(self, message: str):
        try:
            print(f"[LinuxFocusWatcher] {message}", flush=True)
        except Exception:
            pass

    def start(self):
        if sys.platform != "linux":
            return
        self._ensure_poll_timer()
        self._log("focus watcher started.")

    def stop(self):
        try:
            if self._poll_timer is not None:
                self._poll_timer.stop()
        except Exception:
            pass
        self._polling_active = False
        self._log("focus watcher stopped.")

    @Slot(bool)
    def set_slideshow_running(self, running: bool):
        previous = bool(self._slideshow_running)
        self._slideshow_running = bool(running)
        self._transient_hold_budget = 1
        if previous != self._slideshow_running:
            self._log(f"slideshow_running -> {self._slideshow_running}")
        self._set_polling_active(self._slideshow_running)
        self._recompute()

    @Slot(int)
    def set_slideshow_hwnd(self, hwnd: int):
        previous = int(self._slideshow_hwnd or 0)
        try:
            self._slideshow_hwnd = int(hwnd or 0)
        except Exception:
            self._slideshow_hwnd = 0
        self._transient_hold_budget = 1
        if previous != self._slideshow_hwnd:
            self._log(f"slideshow_hwnd -> {self._slideshow_hwnd}")
        self._recompute()

    def _ensure_poll_timer(self):
        if self._poll_timer is not None:
            return
        self._poll_timer = QTimer(self)
        self._poll_timer.setInterval(150)
        self._poll_timer.timeout.connect(self._recompute)

    def _set_polling_active(self, active: bool):
        if sys.platform != "linux":
            return
        active = bool(active)
        if active == self._polling_active and self._poll_timer is not None:
            return
        self._ensure_poll_timer()
        if active:
            self._poll_timer.start()
        else:
            self._poll_timer.stop()
        self._polling_active = active

    def _can_track_focus(self) -> bool:
        if sys.platform != "linux":
            return False
        if can_use_xdotool():
            return True
        if not self._availability_warned:
            self._availability_warned = True
            self._log(
                "xdotool focus tracking requires X11/XWayland and xdotool in PATH."
            )
        return False

    def _recompute(self):
        focus_on_slideshow = False
        match_reason = "idle"
        snapshot = {}
        if self._slideshow_running and self._can_track_focus():
            snapshot = get_active_window_snapshot()
            active_window_id = int(snapshot.get("window_id", 0) or 0)
            title = str(snapshot.get("title", "") or "")
            class_name = str(snapshot.get("class", "") or "")

            if self._slideshow_hwnd and active_window_id == self._slideshow_hwnd:
                focus_on_slideshow = True
                match_reason = "target-hwnd"
            elif self._slideshow_hwnd:
                focus_on_slideshow = window_looks_like_slideshow(
                    title=title,
                    class_name=class_name,
                    strict=True,
                )
                match_reason = "strict-match" if focus_on_slideshow else "target-miss"
            else:
                focus_on_slideshow = window_looks_like_slideshow(
                    title=title,
                    class_name=class_name,
                    strict=True,
                )
                match_reason = "strict-match" if focus_on_slideshow else "no-target"

            if (
                not focus_on_slideshow
                and snapshot_is_transient(snapshot)
                and bool(self._last_focus_on_slideshow)
                and self._transient_hold_budget > 0
            ):
                focus_on_slideshow = True
                match_reason = "transient-hold"
                self._transient_hold_budget -= 1
            elif not snapshot_is_transient(snapshot):
                self._transient_hold_budget = 1

        if focus_on_slideshow != self._last_focus_on_slideshow:
            self._last_focus_on_slideshow = focus_on_slideshow
            self._log(
                "focus_on_slideshow -> "
                f"{bool(focus_on_slideshow)} "
                f"(reason={match_reason}, "
                f"target={int(self._slideshow_hwnd or 0)}, "
                f"active={format_window_snapshot(snapshot)})"
            )
            self.focus_on_slideshow_changed.emit(bool(focus_on_slideshow))
