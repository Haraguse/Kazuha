import sys
import ctypes

try:
    from ctypes import wintypes
except ImportError:
    wintypes = None
from typing import Optional

from PySide6.QtCore import QObject, QThread, QTimer, Signal, Slot

PRESENTATION_SLIDESHOW_CLASSES = {
    "screenClass",
    "wppSlideShowWindowClass",
    "WPP SlideShow Window",
    "WPP SlideShow Window 8.0",
}
PRESENTATION_SLIDESHOW_TITLE_HINTS = (
    "slide show",
    "slideshow",
    "wps presentation",
    "wps persentation",
    "yozo slide show",
    "yozo slideshow",
    "yozo presentation",
    "幻灯片放映",
    "幻燈片放映",
    "投影片放映",
    "放映",
)
PRESENTATION_PROCESS_NAMES = {
    "powerpnt.exe",
    "wpp.exe",
    "kwpp.exe",
    "yozo_impress.exe",
    "yozopg.exe",
    "yozo_office.exe",
}
STRICT_PRESENTATION_SLIDESHOW_TITLE_HINTS = (
    "slide show",
    "slideshow",
    "slide-show",
    "幻灯片放映",
    "幻燈片放映",
    "投影片放映",
    "スライド ショー",
    "スライドショー",
    "슬라이드 쇼",
    "슬라이드쇼",
    "diaporama",
    "mode diaporama",
    "bildschirmprasentation",
    "bildschirmpräsentation",
    "presentacion con diapositivas",
    "presentación con diapositivas",
    "apresentacao de slides",
    "apresentação de slides",
)


class _WinEventHookThread(QThread):
    foreground_changed = Signal(int)
    _install_failed = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._hook = None
        self._callback = None
        self._thread_id = 0

    def stop(self):
        if sys.platform != "win32":
            return
        try:
            if not self._thread_id:
                return
            user32 = ctypes.windll.user32
            WM_QUIT = 0x0012
            user32.PostThreadMessageW(int(self._thread_id), WM_QUIT, 0, 0)
        except Exception:
            pass

    def run(self):
        try:
            if sys.platform != "win32":
                self._install_failed.emit()
                return

            user32 = ctypes.windll.user32
            kernel32 = ctypes.windll.kernel32

            WINEVENTPROC = ctypes.WINFUNCTYPE(
                None,
                wintypes.HANDLE,
                wintypes.DWORD,
                wintypes.HWND,
                wintypes.LONG,
                wintypes.LONG,
                wintypes.DWORD,
                wintypes.DWORD,
            )

            EVENT_SYSTEM_FOREGROUND = 0x0003
            EVENT_SYSTEM_MINIMIZESTART = 0x0016
            EVENT_SYSTEM_MINIMIZEEND = 0x0017
            WINEVENT_OUTOFCONTEXT = 0x0000
            WINEVENT_SKIPOWNPROCESS = 0x0002

            def _cb(
                hWinEventHook,
                event,
                hwnd,
                idObject,
                idChild,
                dwEventThread,
                dwmsEventTime,
            ):
                try:
                    if event == EVENT_SYSTEM_FOREGROUND and hwnd:
                        self.foreground_changed.emit(int(hwnd))
                    elif event in (
                        EVENT_SYSTEM_MINIMIZESTART,
                        EVENT_SYSTEM_MINIMIZEEND,
                    ):
                        self.foreground_changed.emit(0)
                except Exception:
                    pass

            self._callback = WINEVENTPROC(_cb)
            self._thread_id = int(kernel32.GetCurrentThreadId())

            user32.SetWinEventHook.argtypes = [
                wintypes.DWORD,
                wintypes.DWORD,
                wintypes.HMODULE,
                WINEVENTPROC,
                wintypes.DWORD,
                wintypes.DWORD,
                wintypes.DWORD,
            ]
            user32.SetWinEventHook.restype = wintypes.HANDLE

            self._hook = user32.SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_MINIMIZEEND,
                0,
                self._callback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS,
            )
            if not self._hook:
                self._install_failed.emit()
                return

            msg = wintypes.MSG()
            while True:
                r = user32.GetMessageW(ctypes.byref(msg), 0, 0, 0)
                if r == 0 or r == -1:
                    break
                user32.TranslateMessage(ctypes.byref(msg))
                user32.DispatchMessageW(ctypes.byref(msg))
        except Exception:
            try:
                self._install_failed.emit()
            except Exception:
                pass
        finally:
            try:
                if self._hook:
                    ctypes.windll.user32.UnhookWinEvent(self._hook)
            except Exception:
                pass
            self._hook = None
            self._callback = None
            self._thread_id = 0


class WindowsFocusWatcher(QObject):
    focus_on_slideshow_changed = Signal(bool)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._slideshow_running = False
        self._slideshow_hwnd = 0
        self._last_focus_on_slideshow = None
        self._poll_timer = None
        self._polling_active = False
        self._hook_failed = False
        self._thread = None

        if sys.platform == "win32":
            self._thread = _WinEventHookThread(self)
            self._thread.foreground_changed.connect(self._on_foreground_changed)
            self._thread._install_failed.connect(self._on_hook_install_failed)

    def start(self):
        if sys.platform != "win32":
            return
        if self._thread and not self._thread.isRunning():
            self._thread.start()

    def stop(self):
        try:
            if self._poll_timer:
                self._poll_timer.stop()
        except Exception:
            pass
        try:
            if self._thread and self._thread.isRunning():
                self._thread.stop()
                self._thread.wait(1500)
        except Exception:
            pass

    @Slot(bool)
    def set_slideshow_running(self, running: bool):
        self._slideshow_running = bool(running)
        self._set_polling_active(self._slideshow_running)
        self._recompute()

    @Slot(int)
    def set_slideshow_hwnd(self, hwnd: int):
        try:
            self._slideshow_hwnd = int(hwnd or 0)
        except Exception:
            self._slideshow_hwnd = 0
        self._recompute()

    @Slot(int)
    def _on_foreground_changed(self, hwnd: int):
        if hwnd:
            self._recompute(foreground_hwnd=int(hwnd))
        else:
            self._recompute()

    def _on_hook_install_failed(self):
        self._hook_failed = True
        self._set_polling_active(self._slideshow_running)

    def _ensure_poll_timer(self):
        if self._poll_timer is not None:
            return
        self._poll_timer = QTimer(self)
        self._poll_timer.setInterval(100)
        self._poll_timer.timeout.connect(self._recompute)

    def _set_polling_active(self, active: bool):
        if sys.platform != "win32":
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

    def _get_foreground_hwnd(self) -> int:
        try:
            user32 = ctypes.windll.user32
            return int(user32.GetForegroundWindow() or 0)
        except Exception:
            return 0

    def _get_class_name(self, hwnd: int) -> str:
        if not hwnd:
            return ""
        try:
            user32 = ctypes.windll.user32
            buf = ctypes.create_unicode_buffer(256)
            if user32.GetClassNameW(wintypes.HWND(hwnd), buf, 256):
                return buf.value or ""
        except Exception:
            pass
        return ""

    def _get_window_title(self, hwnd: int) -> str:
        if not hwnd:
            return ""
        try:
            user32 = ctypes.windll.user32
            buf = ctypes.create_unicode_buffer(512)
            if user32.GetWindowTextW(wintypes.HWND(hwnd), buf, 512):
                return buf.value or ""
        except Exception:
            pass
        return ""

    def _title_looks_like_slideshow(self, title: str) -> bool:
        text = str(title or "").strip().lower()
        if not text:
            return False
        return any(hint in text for hint in STRICT_PRESENTATION_SLIDESHOW_TITLE_HINTS)

    def _is_window_visible(self, hwnd: int) -> bool:
        if not hwnd:
            return False
        try:
            user32 = ctypes.windll.user32
            return bool(user32.IsWindowVisible(wintypes.HWND(hwnd)))
        except Exception:
            return True

    def _is_iconic(self, hwnd: int) -> bool:
        if not hwnd:
            return False
        try:
            user32 = ctypes.windll.user32
            return bool(user32.IsIconic(wintypes.HWND(hwnd)))
        except Exception:
            return False

    def _get_root_owner(self, hwnd: int) -> int:
        if not hwnd:
            return 0
        try:
            user32 = ctypes.windll.user32
            GA_ROOTOWNER = 3
            root = user32.GetAncestor(wintypes.HWND(hwnd), GA_ROOTOWNER)
            return int(root or 0)
        except Exception:
            return 0

    def _get_pid(self, hwnd: int) -> int:
        if not hwnd:
            return 0
        try:
            user32 = ctypes.windll.user32
            pid = wintypes.DWORD()
            user32.GetWindowThreadProcessId(wintypes.HWND(hwnd), ctypes.byref(pid))
            return int(pid.value or 0)
        except Exception:
            return 0

    def _get_process_name(self, hwnd: int) -> str:
        pid = self._get_pid(hwnd)
        if not pid:
            return ""
        try:
            kernel32 = ctypes.windll.kernel32
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
            handle = kernel32.OpenProcess(
                PROCESS_QUERY_LIMITED_INFORMATION, False, int(pid)
            )
            if not handle:
                return ""
            try:
                size = wintypes.DWORD(512)
                buf = ctypes.create_unicode_buffer(512)
                if ctypes.windll.kernel32.QueryFullProcessImageNameW(
                    handle, 0, buf, ctypes.byref(size)
                ):
                    path = (buf.value or "").strip().lower()
                    if path:
                        return path.rsplit("\\", 1)[-1]
            finally:
                kernel32.CloseHandle(handle)
        except Exception:
            pass
        return ""

    def _recompute(self, foreground_hwnd: Optional[int] = None):
        if sys.platform != "win32":
            return
        if foreground_hwnd is None:
            foreground_hwnd = self._get_foreground_hwnd()

        focus_on_slideshow = False

        if self._slideshow_running:
            hwnd = int(self._slideshow_hwnd or 0)
            cls = self._get_class_name(int(foreground_hwnd or 0))
            title = self._get_window_title(int(foreground_hwnd or 0))
            if hwnd:
                if self._is_iconic(hwnd) or not self._is_window_visible(hwnd):
                    focus_on_slideshow = False
                elif int(foreground_hwnd or 0) == hwnd:
                    focus_on_slideshow = True
                else:
                    root = self._get_root_owner(int(foreground_hwnd or 0))
                    focus_on_slideshow = bool(root and root == hwnd)
                    if not focus_on_slideshow and cls in PRESENTATION_SLIDESHOW_CLASSES:
                        focus_on_slideshow = True
                    if not focus_on_slideshow and self._title_looks_like_slideshow(
                        title
                    ):
                        focus_on_slideshow = True
            else:
                focus_on_slideshow = (
                    cls in PRESENTATION_SLIDESHOW_CLASSES
                    or self._title_looks_like_slideshow(title)
                )

        if focus_on_slideshow != self._last_focus_on_slideshow:
            self._last_focus_on_slideshow = focus_on_slideshow
            self.focus_on_slideshow_changed.emit(bool(focus_on_slideshow))
