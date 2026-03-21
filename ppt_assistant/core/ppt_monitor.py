try:
    import win32com.client
    import pythoncom
except ImportError:
    win32com = None
    pythoncom = None
from PySide6.QtCore import QObject, Signal, QThread, QTimer, QPoint, QRect, Slot
from PySide6.QtGui import QGuiApplication
import time
import os
from ppt_assistant.core.config import cfg

try:
    import win32gui
    import win32api
    import win32con
    import win32process
except ImportError:
    win32gui = None
    win32api = None
    win32con = None
    win32process = None
try:
    import pywintypes
except ImportError:
    pywintypes = None

PPT_SLIDESHOW_WINDOW_CLASSES = {"screenClass"}
WPS_SLIDESHOW_WINDOW_CLASSES = {
    "wppSlideShowWindowClass",
    "WPP SlideShow Window",
    "WPP SlideShow Window 8.0",
}
YOZO_SLIDESHOW_WINDOW_CLASSES = set()
ALL_SLIDESHOW_WINDOW_CLASSES = (
    PPT_SLIDESHOW_WINDOW_CLASSES
    | WPS_SLIDESHOW_WINDOW_CLASSES
    | YOZO_SLIDESHOW_WINDOW_CLASSES
)
SLIDESHOW_WINDOW_TITLE_HINTS = {
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
}
PPT_PROCESS_NAMES = {"powerpnt.exe"}
WPS_PROCESS_NAMES = {"wpp.exe", "kwpp.exe"}
YOZO_PROCESS_NAMES = {"yozo_impress.exe", "yozopg.exe", "yozo_office.exe"}
ALL_PRESENTATION_PROCESS_NAMES = PPT_PROCESS_NAMES | WPS_PROCESS_NAMES | YOZO_PROCESS_NAMES
YOZO_COM_PROG_IDS = ("YozoPG.Application", "YozoPG.Application.1")
STRICT_SLIDESHOW_WINDOW_TITLE_HINTS = {
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
}

class PPTWorker(QObject):
    """
    Worker thread for PPT COM operations to prevent blocking the main UI.
    """
    # Signals to Main Thread
    slideshow_started = Signal()
    slideshow_ended = Signal()
    slide_changed = Signal(int, int) # current, total
    window_geometry_changed = Signal(object, object) # QRect, QScreen
    overlay_visibility_changed = Signal(bool)
    video_state_changed = Signal(float, float, float) # ratio, pos, length
    thumbnail_generated = Signal(int, str) # index, path
    finished = Signal()
    slideshow_hwnd_changed = Signal(int)
    restrictions_changed = Signal(bool, bool) # protected_view, presentation_readonly
    active_kind_changed = Signal(str)
    ink_prompt_requested = Signal()

    def __init__(self):
        super().__init__()
        self.ppt_app = None
        self.wps_app = None
        self.yozo_app = None
        self._running = False
        self._current_slide = 0
        self._total_slides = 0
        self._last_win_rect = (0, 0, 0, 0)
        self._active_kind = None
        self._last_screen = None
        self._overlay_visible = None
        self._timer = None
        self._com_initialized = False
        self._slideshow_hwnd = 0
        self._slideshow_started_at = 0.0
        self._protected_view = False
        self._presentation_readonly = False
        self._control_mode = "com"
        self._last_error_by_key = {}
        self._degraded_current = 0
        self._degraded_total = 0
        self._pending_ink_prompt = False

    def _set_active_kind(self, kind):
        if kind == self._active_kind:
            return
        self._active_kind = kind
        try:
            self.active_kind_changed.emit(kind or "")
        except Exception:
            pass

    def _update_restrictions(self, protected_view: bool, presentation_readonly: bool):
        protected_view = bool(protected_view)
        presentation_readonly = bool(presentation_readonly)
        if protected_view == self._protected_view and presentation_readonly == self._presentation_readonly:
            return
        self._protected_view = protected_view
        self._presentation_readonly = presentation_readonly
        self.restrictions_changed.emit(protected_view, presentation_readonly)

    def _normalize_process_name(self, exe_path: str) -> str:
        try:
            return os.path.basename(str(exe_path or "")).strip().lower()
        except Exception:
            return ""

    def _get_window_process_name(self, hwnd: int) -> str:
        if not hwnd or not win32process or not win32api:
            return ""
        try:
            _, pid = win32process.GetWindowThreadProcessId(int(hwnd))
            if not pid:
                return ""
            access = 0x1000
            if hasattr(win32con, "PROCESS_QUERY_LIMITED_INFORMATION"):
                access = int(getattr(win32con, "PROCESS_QUERY_LIMITED_INFORMATION"))
            handle = win32api.OpenProcess(access, False, pid)
            try:
                exe = win32process.GetModuleFileNameEx(handle, 0) or ""
            finally:
                try:
                    win32api.CloseHandle(handle)
                except Exception:
                    pass
            return self._normalize_process_name(exe)
        except Exception:
            return ""

    def _title_looks_like_slideshow(self, title: str) -> bool:
        title_lower = str(title or "").strip().lower()
        if not title_lower:
            return False
        return any(hint in title_lower for hint in STRICT_SLIDESHOW_WINDOW_TITLE_HINTS)

    def _class_matches_kind(self, cls_name: str, kind: str | None) -> bool:
        cls = str(cls_name or "")
        if kind == "ppt":
            return cls in PPT_SLIDESHOW_WINDOW_CLASSES
        if kind == "wps":
            return cls in WPS_SLIDESHOW_WINDOW_CLASSES
        if kind == "yozo":
            return cls in YOZO_SLIDESHOW_WINDOW_CLASSES
        return cls in ALL_SLIDESHOW_WINDOW_CLASSES

    def _process_matches_kind(self, process_name: str, kind: str | None) -> bool:
        name = self._normalize_process_name(process_name)
        if not name:
            return False
        if kind == "ppt":
            return name in PPT_PROCESS_NAMES
        if kind == "wps":
            return name in WPS_PROCESS_NAMES
        if kind == "yozo":
            return name in YOZO_PROCESS_NAMES
        return name in ALL_PRESENTATION_PROCESS_NAMES

    def _kind_from_window(self, hwnd: int) -> str | None:
        cls_name = ""
        try:
            if hwnd and win32gui:
                cls_name = win32gui.GetClassName(int(hwnd)) or ""
        except Exception:
            cls_name = ""
        if cls_name in PPT_SLIDESHOW_WINDOW_CLASSES:
            return "ppt"
        if cls_name in WPS_SLIDESHOW_WINDOW_CLASSES:
            return "wps"
        if cls_name in YOZO_SLIDESHOW_WINDOW_CLASSES:
            return "yozo"
        process_name = self._get_window_process_name(hwnd)
        if process_name in PPT_PROCESS_NAMES:
            return "ppt"
        if process_name in WPS_PROCESS_NAMES:
            return "wps"
        if process_name in YOZO_PROCESS_NAMES:
            return "yozo"
        return None

    def _is_slideshow_hwnd(self, hwnd: int, preferred_kind: str | None = None) -> bool:
        try:
            if not hwnd or not win32gui:
                return False
            if not win32gui.IsWindowVisible(int(hwnd)):
                return False
            cls_name = win32gui.GetClassName(int(hwnd)) or ""
            if self._class_matches_kind(cls_name, preferred_kind):
                return True
            process_name = self._get_window_process_name(hwnd)
            if self._process_matches_kind(process_name, preferred_kind):
                title = ""
                try:
                    title = win32gui.GetWindowText(int(hwnd)) or ""
                except Exception:
                    title = ""
                if self._title_looks_like_slideshow(title):
                    return True
            return False
        except Exception:
            return False

    def _safe_count(self, collection) -> int:
        try:
            return int(getattr(collection, "Count", 0) or 0)
        except Exception:
            return 0

    def _iter_slideshow_windows(self, app):
        try:
            windows = getattr(app, "SlideShowWindows", None)
        except Exception:
            windows = None
        count = self._safe_count(windows)
        for i in range(1, count + 1):
            try:
                yield windows(i)
            except Exception:
                continue

    def _safe_hwnd_from_ss_win(self, ss_win) -> int:
        try:
            val = getattr(ss_win, "HWND", 0)
            if callable(val):
                val = val()
            return int(val or 0)
        except Exception:
            return 0

    def _pick_best_slideshow_window(self, app, kind: str | None = None):
        windows = list(self._iter_slideshow_windows(app))
        if not windows:
            return None
        if len(windows) == 1:
            return windows[0]

        preferred_classes = PPT_SLIDESHOW_WINDOW_CLASSES if kind == "ppt" else WPS_SLIDESHOW_WINDOW_CLASSES if kind == "wps" else ALL_SLIDESHOW_WINDOW_CLASSES

        for ss_win in windows:
            hwnd = self._safe_hwnd_from_ss_win(ss_win)
            if not hwnd or not win32gui:
                continue
            try:
                if (win32gui.GetClassName(int(hwnd)) or "") in preferred_classes:
                    return ss_win
            except Exception:
                continue

        for ss_win in windows:
            hwnd = self._safe_hwnd_from_ss_win(ss_win)
            if hwnd and self._is_slideshow_hwnd(hwnd, kind):
                return ss_win

        return windows[0]

    def _get_presentation_from_ss_win(self, ss_win, app=None):
        candidates = []
        try:
            candidates.append(getattr(ss_win, "Presentation", None))
        except Exception:
            pass
        try:
            view = getattr(ss_win, "View", None)
            candidates.append(getattr(view, "Presentation", None) if view is not None else None)
        except Exception:
            pass
        if app is not None:
            try:
                candidates.append(getattr(app, "ActivePresentation", None))
            except Exception:
                pass
            try:
                presentations = getattr(app, "Presentations", None)
                if presentations is not None and self._safe_count(presentations) > 0:
                    candidates.append(presentations(1))
            except Exception:
                pass

        for candidate in candidates:
            if candidate is not None:
                return candidate
        return None

    def _extract_slide_position(self, view) -> int:
        if view is None:
            return 0
        try:
            value = getattr(view, "CurrentShowPosition", 0)
            if callable(value):
                value = value()
            current = int(value or 0)
            if current > 0:
                return current
        except Exception:
            pass
        try:
            slide = getattr(view, "Slide", None)
            index = getattr(slide, "SlideIndex", 0) if slide is not None else 0
            if callable(index):
                index = index()
            return int(index or 0)
        except Exception:
            return 0

    def _extract_slide_total(self, presentation) -> int:
        if presentation is None:
            return 0
        try:
            slides = getattr(presentation, "Slides", None)
            return int(getattr(slides, "Count", 0) or 0)
        except Exception:
            return 0

    def _find_ppt_slideshow_hwnd(self) -> int:
        if not win32gui:
            return 0
        try:
            fg = int(win32gui.GetForegroundWindow() or 0)
        except Exception:
            fg = 0

        preferred_kind = self._active_kind or None

        if self._is_slideshow_hwnd(fg, preferred_kind):
            return fg

        found = 0

        def _enum_cb(hwnd, _):
            nonlocal found
            if found:
                return
            if self._is_slideshow_hwnd(int(hwnd), preferred_kind):
                found = int(hwnd)

        try:
            win32gui.EnumWindows(_enum_cb, None)
        except Exception:
            return 0
        return int(found or 0)

    def _update_window_rect_hwnd(self, hwnd: int):
        try:
            if not win32gui:
                return
            hwnd = int(hwnd or 0)
            if not hwnd:
                return
            left, top, right, bottom = win32gui.GetWindowRect(hwnd)
            w, h = right - left, bottom - top
            final_rect = (left, top, w, h)
            dpi = 0
            if win32api:
                try:
                    dpi = int(win32api.GetDpiForWindow(int(hwnd)) or 0)
                except Exception:
                    try:
                        import ctypes

                        user32 = ctypes.windll.user32
                        user32.GetDpiForWindow.argtypes = [ctypes.c_void_p]
                        user32.GetDpiForWindow.restype = ctypes.c_uint
                        dpi = int(user32.GetDpiForWindow(ctypes.c_void_p(int(hwnd))) or 0)
                    except Exception:
                        dpi = 0
            if final_rect != self._last_win_rect:
                self._last_win_rect = final_rect
                self.window_geometry_changed.emit(QRect(*final_rect), {"raw_is_physical": True, "dpi": dpi})
            if self._overlay_visible is not True:
                self._overlay_visible = True
                self.overlay_visibility_changed.emit(True)
        except Exception:
            pass

    def _send_vk_to_slideshow(self, vk: int) -> bool:
        try:
            if not win32api or not win32con:
                return False
            hwnd = int(self._slideshow_hwnd or 0)
            if not hwnd:
                hwnd = self._find_ppt_slideshow_hwnd()
                if hwnd:
                    self._slideshow_hwnd = hwnd
                    self.slideshow_hwnd_changed.emit(hwnd)
            if not hwnd:
                return False
            if win32gui:
                try:
                    win32gui.SetForegroundWindow(int(hwnd))
                except Exception:
                    pass
            win32api.keybd_event(int(vk), 0, 0, 0)
            win32api.keybd_event(int(vk), 0, win32con.KEYEVENTF_KEYUP, 0)
            return True
        except Exception:
            return False

    def _note_error(self, key: str, exc: Exception):
        try:
            now = time.monotonic()
            last = float(self._last_error_by_key.get(key, 0.0) or 0.0)
            if now - last < 2.0:
                return
            self._last_error_by_key[key] = now
            try:
                print(f"[ppt_monitor] {key} failed: {type(exc).__name__}: {exc}")
            except Exception:
                pass
        except Exception:
            pass

    def _try_emit_page_info_from_ppt_app(self):
        app = self._get_active_app()
        if not app:
            return

        current = 0
        total = 0
        pres = self._get_primary_presentation(app)

        total = self._extract_slide_total(pres)

        if not total:
            try:
                pv_windows = getattr(app, "ProtectedViewWindows", None)
                if pv_windows is not None and int(getattr(pv_windows, "Count", 0) or 0) > 0:
                    pv = pv_windows(1)
                    pres = getattr(pv, "Presentation", None)
                    if pres is not None:
                        total = int(getattr(getattr(pres, "Slides", None), "Count", 0) or 0)
            except Exception:
                pass

        try:
            if pres is not None:
                ss_win = getattr(pres, "SlideShowWindow", None)
                ss_view = getattr(ss_win, "View", None) if ss_win is not None else None
                current = self._extract_slide_position(ss_view)
        except Exception:
            pass

        try:
            active_win = getattr(app, "ActiveWindow", None)
            view = getattr(active_win, "View", None) if active_win is not None else None
            if not current:
                current = self._extract_slide_position(view)
        except Exception:
            pass

        if not total:
            total = int(self._total_slides or 0)

        if current > 0 and total > 0:
            if current != self._current_slide or total != self._total_slides:
                self._current_slide = current
                self._total_slides = total
                self.slide_changed.emit(current, total)

        if total > 0:
            self._degraded_total = total
        if current > 0:
            self._degraded_current = current

    def _init_degraded_page_info(self):
        total = 0
        try:
            app = self._get_active_app()
            if app is None:
                return
            pres = self._get_primary_presentation(app)
            total = self._extract_slide_total(pres)
            if not total:
                pv_windows = getattr(app, "ProtectedViewWindows", None)
                if pv_windows is not None and int(getattr(pv_windows, "Count", 0) or 0) > 0:
                    pv = pv_windows(1)
                    pres = getattr(pv, "Presentation", None)
                    total = self._extract_slide_total(pres)
        except Exception:
            return

        if total > 0:
            self._degraded_total = total
            if self._degraded_current <= 0:
                self._degraded_current = 1
            if self._degraded_current != self._current_slide or total != self._total_slides:
                self._current_slide = self._degraded_current
                self._total_slides = total
                self.slide_changed.emit(self._degraded_current, total)

    @Slot()
    def start(self):
        if not pythoncom:
            return

        if not self._com_initialized:
            pythoncom.CoInitialize()
            self._com_initialized = True
            
        self._timer = QTimer(self)
        self._timer.timeout.connect(self._check_ppt_state)
        self._timer.start(200)

    @Slot()
    def stop(self):
        if self._timer:
            self._timer.stop()
            self._timer.deleteLater()
            self._timer = None
        if self._com_initialized and pythoncom:
            try:
                pythoncom.CoUninitialize()
            except Exception:
                pass
            self._com_initialized = False
        self.finished.emit()

    def _get_active_app(self):
        # Helper to get the currently tracked app
        if self._active_kind == "ppt" and self.ppt_app: return self.ppt_app
        if self._active_kind == "wps" and self.wps_app: return self.wps_app
        if self._active_kind == "yozo" and self.yozo_app: return self.yozo_app
        # Fallback
        if self.ppt_app: return self.ppt_app
        if self.wps_app: return self.wps_app
        if self.yozo_app: return self.yozo_app
        return None

    def _safe_get_active_object(self, prog_id: str):
        if not win32com:
            return None
        try:
            return win32com.client.GetActiveObject(prog_id)
        except Exception:
            return None

    def _safe_get_active_object_any(self, prog_ids):
        for prog_id in prog_ids:
            app = self._safe_get_active_object(prog_id)
            if app is not None:
                return app
        return None

    def _update_slide_info_from_ss_win(self, ss_win, app=None, kind: str | None = None):
        current = 0
        total = 0
        presentation = None

        try:
            view = getattr(ss_win, "View", None)
        except Exception:
            view = None

        current = self._extract_slide_position(view)
        presentation = self._get_presentation_from_ss_win(ss_win, app)
        total = self._extract_slide_total(presentation)

        pres_readonly = False
        if presentation is not None:
            try:
                pres_readonly = bool(getattr(presentation, "ReadOnly", False))
            except Exception:
                pres_readonly = False

        self._update_restrictions(False if kind in {"wps", "yozo"} else self._protected_view, pres_readonly)

        if not total:
            total = int(self._total_slides or 0)
        if current > 0 and total > 0 and (current != self._current_slide or total != self._total_slides):
            self._current_slide = current
            self._total_slides = total
            self.slide_changed.emit(current, total)
            self._degraded_current = current
            self._degraded_total = total

    def _get_primary_presentation(self, app):
        if app is None:
            return None
        try:
            presentation = getattr(app, "ActivePresentation", None)
            if presentation is not None:
                return presentation
        except Exception:
            pass
        try:
            presentations = getattr(app, "Presentations", None)
            if presentations is not None and self._safe_count(presentations) > 0:
                return presentations(1)
        except Exception:
            pass
        return None

    def _get_active_slideshow_window(self):
        app = self._get_active_app()
        if app is None:
            return None
        if self._safe_count(getattr(app, "SlideShowWindows", None)) <= 0:
            return None
        return self._pick_best_slideshow_window(app, self._active_kind or None)

    def _check_ppt_state(self):
        try:
            if not win32com:
                return
            if self._active_kind == "wps":
                self._check_wps_state()
                if not self._running:
                    self._check_yozo_state()
                return
            if self._active_kind == "yozo":
                self._check_yozo_state()
                return

            # 1. Try PowerPoint
            self.ppt_app = self._safe_get_active_object("PowerPoint.Application")
            if not self.ppt_app:
                self._handle_stop("ppt")
                self._check_wps_state()
                return

            try:
                protected_view = False
                pv_windows = getattr(self.ppt_app, "ProtectedViewWindows", None)
                if pv_windows is not None:
                    protected_view = int(getattr(pv_windows, "Count", 0) or 0) > 0
                self._update_restrictions(protected_view, self._presentation_readonly)
            except Exception:
                pass

            if self._safe_count(getattr(self.ppt_app, "SlideShowWindows", None)) > 0:
                try:
                    ss_win = self._pick_best_slideshow_window(self.ppt_app, "ppt")
                    view = getattr(ss_win, "View", None) if ss_win is not None else None
                    state = getattr(view, "State", 1) if view is not None else 1

                    if ss_win is not None and state in [1, 2]:
                        if not self._running:
                            self._running = True
                            self._set_active_kind("ppt")
                            self._control_mode = "com"
                            try:
                                hwnd = self._safe_hwnd_from_ss_win(ss_win)
                                if hwnd and hwnd != self._slideshow_hwnd:
                                    self._slideshow_hwnd = hwnd
                                    self.slideshow_hwnd_changed.emit(hwnd)
                            except Exception:
                                pass
                            self._slideshow_started_at = time.monotonic()
                            self.slideshow_started.emit()

                        self._update_slide_info_from_ss_win(ss_win, self.ppt_app, "ppt")
                        try:
                            self._update_window_rect(ss_win)
                            self._update_video_state(ss_win)
                        except Exception:
                            pass
                    else:
                        self._handle_stop("ppt")
                except Exception:
                    pass
            else:
                hwnd = self._find_ppt_slideshow_hwnd()
                if hwnd:
                    if not self._running:
                        self._running = True
                        self._set_active_kind("ppt")
                        self._control_mode = "win32"
                        if hwnd != self._slideshow_hwnd:
                            self._slideshow_hwnd = int(hwnd)
                            self.slideshow_hwnd_changed.emit(int(hwnd))
                        self._slideshow_started_at = time.monotonic()
                        self.slideshow_started.emit()
                        self._init_degraded_page_info()
                    self._update_window_rect_hwnd(hwnd)
                    self._try_emit_page_info_from_ppt_app()
                else:
                    self._handle_stop("ppt")

        except Exception:
            pass
            
        # If not running PPT, check WPS
        if not self._running:
            self._check_wps_state()
            if not self._running:
                self._check_yozo_state()
            return

    def _check_wps_state(self):
        if not win32com:
            return
        try:
            self.wps_app = self._safe_get_active_object("KWPP.Application")
        except BaseException:
            self.wps_app = None
            self._handle_stop("wps")
            return

        try:
            if self._safe_count(getattr(self.wps_app, "SlideShowWindows", None)) > 0:
                ss_win = self._pick_best_slideshow_window(self.wps_app, "wps")
                if ss_win is None:
                    self._handle_stop("wps")
                    return
                if ss_win is not None and not self._running:
                    self._running = True
                    self._set_active_kind("wps")
                    self._control_mode = "com"
                    try:
                        hwnd = self._safe_hwnd_from_ss_win(ss_win)
                        if hwnd and hwnd != self._slideshow_hwnd:
                            self._slideshow_hwnd = hwnd
                            self.slideshow_hwnd_changed.emit(hwnd)
                    except Exception:
                        pass
                    self._slideshow_started_at = time.monotonic()
                    self.slideshow_started.emit()

                if ss_win is not None:
                    self._update_slide_info_from_ss_win(ss_win, self.wps_app, "wps")

                try:
                    self._update_window_rect(ss_win)
                    self._update_video_state(ss_win)
                except Exception:
                    pass
            else:
                self._handle_stop("wps")
        except BaseException:
            self._handle_stop("wps")

    def _check_yozo_state(self):
        if not win32com:
            return
        try:
            self.yozo_app = self._safe_get_active_object_any(YOZO_COM_PROG_IDS)
        except BaseException:
            self.yozo_app = None
            self._handle_stop("yozo")
            return

        try:
            if self._safe_count(getattr(self.yozo_app, "SlideShowWindows", None)) > 0:
                ss_win = self._pick_best_slideshow_window(self.yozo_app, "yozo")
                if ss_win is None:
                    self._handle_stop("yozo")
                    return
                if ss_win is not None and not self._running:
                    self._running = True
                    self._set_active_kind("yozo")
                    self._control_mode = "com"
                    try:
                        hwnd = self._safe_hwnd_from_ss_win(ss_win)
                        if hwnd and hwnd != self._slideshow_hwnd:
                            self._slideshow_hwnd = hwnd
                            self.slideshow_hwnd_changed.emit(hwnd)
                    except Exception:
                        pass
                    self._slideshow_started_at = time.monotonic()
                    self.slideshow_started.emit()

                if ss_win is not None:
                    self._update_slide_info_from_ss_win(ss_win, self.yozo_app, "yozo")

                try:
                    self._update_window_rect(ss_win)
                    self._update_video_state(ss_win)
                except Exception:
                    pass
            else:
                self._handle_stop("yozo")
        except BaseException:
            self._handle_stop("yozo")

    def _handle_stop(self, kind):
        if self._running and (self._active_kind == kind or self._active_kind is None):
            self._running = False
            self._set_active_kind(None)
            self._pending_ink_prompt = False
            self.slideshow_ended.emit()
            try:
                self._update_restrictions(False, False)
            except Exception:
                pass
            self._degraded_current = 0
            self._degraded_total = 0
            if not cfg.compatibilityMode.value:
                if self._overlay_visible is not False:
                    self._overlay_visible = False
                    self.overlay_visibility_changed.emit(False)
            if self._slideshow_hwnd:
                self._slideshow_hwnd = 0
                self.slideshow_hwnd_changed.emit(0)
            self._slideshow_started_at = 0.0

    def _update_window_rect(self, ss_win):
        try:
            l_left, l_top, l_width, l_height = 0, 0, 0, 0
            screen = None
            success = False
            raw_is_physical = None
            dpi = 0
            
            # 1. Try Win32 API
            if win32gui:
                try:
                    hwnd = self._safe_hwnd_from_ss_win(ss_win)
                    if hwnd:
                        left, top, right, bottom = win32gui.GetWindowRect(hwnd)
                        w, h = right - left, bottom - top
                        cx, cy = left + w // 2, top + h // 2
                        
                        # Note: QGuiApplication calls are not thread-safe if they access GUI
                        # But screenAt/primaryScreen are generally okay.
                        # However, strictly we should calculate rect here and let Main Thread determine Screen.
                        # To be safe, we just emit the Rect and let Main Thread handle Screen resolution if possible.
                        # OR: We trust QGuiApplication read-only methods.
                        
                        # Optimization: Just send raw rect, let UI thread figure out DPI/Screen
                        # But existing logic does DPI scaling here. 
                        # We will assume DPI unawareness in worker and let Main Thread handle scaling if needed?
                        # Actually, raw pixels are better.
                        
                        # REVERTING to existing logic but being careful.
                        # Accessing QGuiApplication in thread is risky for some operations.
                        # Let's try to get screen in main thread.
                        # But wait, we need screen for DPI.
                        
                        # Let's emit raw global coords and let main thread map it.
                        final_rect = (left, top, w, h)
                        success = True
                        raw_is_physical = True
                        if win32api:
                            try:
                                dpi = int(win32api.GetDpiForWindow(int(hwnd)) or 0)
                            except Exception:
                                try:
                                    import ctypes

                                    user32 = ctypes.windll.user32
                                    user32.GetDpiForWindow.argtypes = [ctypes.c_void_p]
                                    user32.GetDpiForWindow.restype = ctypes.c_uint
                                    dpi = int(user32.GetDpiForWindow(ctypes.c_void_p(int(hwnd))) or 0)
                                except Exception:
                                    dpi = 0
                except Exception:
                    pass

            # 2. Fallback to COM
            if not success:
                try:
                    l_left = int(getattr(ss_win, "Left", 0))
                    l_top = int(getattr(ss_win, "Top", 0))
                    l_width = int(getattr(ss_win, "Width", 0))
                    l_height = int(getattr(ss_win, "Height", 0))
                    final_rect = (l_left, l_top, l_width, l_height)
                    success = True
                    raw_is_physical = False
                except Exception:
                    pass

            if success:
                try:
                    hwnd = self._safe_hwnd_from_ss_win(ss_win)
                    if hwnd and hwnd != self._slideshow_hwnd:
                        self._slideshow_hwnd = hwnd
                        self.slideshow_hwnd_changed.emit(hwnd)
                except Exception:
                    pass
                if final_rect != self._last_win_rect:
                    self._last_win_rect = final_rect
                    # We send RAW rect (x, y, w, h). Main thread converts to QRect and finds Screen.
                    self.window_geometry_changed.emit(QRect(*final_rect), {"raw_is_physical": raw_is_physical, "dpi": dpi})
                self._update_overlay_visibility(ss_win, final_rect)
                    
        except Exception:
            pass

    def _is_foreground_presentation(self):
        try:
            if not win32gui:
                return False
            fg = win32gui.GetForegroundWindow()
            if not fg:
                return False
            if self._is_slideshow_hwnd(int(fg), None):
                return True
            title = win32gui.GetWindowText(fg) or ""
            if self._title_looks_like_slideshow(title):
                return True
            if win32process and win32api:
                try:
                    _, pid = win32process.GetWindowThreadProcessId(fg)
                    if pid:
                        handle = win32api.OpenProcess(0x1000, False, pid)
                        exe = win32process.GetModuleFileNameEx(handle, 0) or ""
                        exe_lower = exe.lower()
                        if any(
                            exe_lower.endswith(name)
                            for name in ("powerpnt.exe", "wpp.exe", "kwpp.exe", "yozo_impress.exe", "yozopg.exe", "yozo_office.exe")
                        ) and self._title_looks_like_slideshow(title):
                            return True
                except Exception:
                    pass
            return False
            title = win32gui.GetWindowText(fg) or ""
            if any(t in title for t in ["幻灯片放映", "演示文稿"]):
                return True
            return False
        except Exception:
            return False

    def _update_overlay_visibility(self, ss_win, rect):
        if cfg.compatibilityMode.value:
            return
        try:
            if not win32gui or not win32api or not win32con:
                return
            hwnd = self._safe_hwnd_from_ss_win(ss_win)
            if not hwnd:
                return
            visible = True
            if visible != self._overlay_visible:
                self._overlay_visible = visible
                self.overlay_visibility_changed.emit(bool(visible))
        except Exception:
            pass

    def _update_video_state(self, ss_win):
        try:
            view = ss_win.View
            try:
                slide = view.Slide
                shapes = slide.Shapes
                count = shapes.Count
                found_video = False
                for i in range(1, count + 1):
                    shape = shapes.Item(i)
                    media = getattr(shape, "MediaFormat", None)
                    if media is None: continue
                    
                    length = getattr(media, "Length", 0)
                    position = getattr(media, "Position", 0)
                    
                    if length and float(length) > 0:
                        l = float(length)
                        p = float(position or 0.0)
                        ratio = p / l if l > 0 else 0
                        self.video_state_changed.emit(ratio, p, l)
                        found_video = True
                        break
                
                if not found_video:
                    self.video_state_changed.emit(0.0, 0.0, 0.0)
            except Exception:
                pass
        except Exception:
            pass

    # --- Control Slots ---
    @Slot()
    def go_next(self):
        if cfg.compatibilityMode.value:
            try:
                hwnd = int(self._slideshow_hwnd or 0) or self._find_ppt_slideshow_hwnd()
                if hwnd and win32gui:
                    try:
                        win32gui.SetForegroundWindow(int(hwnd))
                    except Exception:
                        pass
                if win32api and win32con:
                    win32api.keybd_event(win32con.VK_DOWN, 0, 0, 0)
                    win32api.keybd_event(win32con.VK_DOWN, 0, win32con.KEYEVENTF_KEYUP, 0)
            except Exception:
                pass
            return

        try:
            ss_win = self._get_active_slideshow_window()
            view = getattr(ss_win, "View", None) if ss_win is not None else None
            if view is not None:
                view.Next()
                self._control_mode = "com"
                return
        except Exception as e:
            self._note_error("go_next_com", e)
        try:
            self._control_mode = "win32"
            ok = self._send_vk_to_slideshow(win32con.VK_NEXT if win32con else 0x22)
            if ok and self._degraded_total > 0:
                if self._degraded_current <= 0:
                    self._degraded_current = 1
                if self._degraded_current < self._degraded_total:
                    self._degraded_current += 1
                if self._degraded_current != self._current_slide or self._degraded_total != self._total_slides:
                    self._current_slide = self._degraded_current
                    self._total_slides = self._degraded_total
                    self.slide_changed.emit(self._degraded_current, self._degraded_total)
        except Exception:
            pass

    @Slot()
    def go_previous(self):
        if cfg.compatibilityMode.value:
            try:
                hwnd = int(self._slideshow_hwnd or 0) or self._find_ppt_slideshow_hwnd()
                if hwnd and win32gui:
                    try:
                        win32gui.SetForegroundWindow(int(hwnd))
                    except Exception:
                        pass
                if win32api and win32con:
                    win32api.keybd_event(win32con.VK_UP, 0, 0, 0)
                    win32api.keybd_event(win32con.VK_UP, 0, win32con.KEYEVENTF_KEYUP, 0)
            except Exception:
                pass
            return

        try:
            ss_win = self._get_active_slideshow_window()
            view = getattr(ss_win, "View", None) if ss_win is not None else None
            if view is not None:
                view.Previous()
                self._control_mode = "com"
                return
        except Exception as e:
            self._note_error("go_previous_com", e)
        try:
            self._control_mode = "win32"
            ok = self._send_vk_to_slideshow(win32con.VK_PRIOR if win32con else 0x21)
            if ok and self._degraded_total > 0:
                if self._degraded_current <= 0:
                    self._degraded_current = 1
                if self._degraded_current > 1:
                    self._degraded_current -= 1
                if self._degraded_current != self._current_slide or self._degraded_total != self._total_slides:
                    self._current_slide = self._degraded_current
                    self._total_slides = self._degraded_total
                    self.slide_changed.emit(self._degraded_current, self._degraded_total)
        except Exception:
            pass

    @Slot()
    def clear_screen(self):
        try:
            ss_win = self._get_active_slideshow_window()
            if ss_win is not None:
                try:
                    hwnd = self._safe_hwnd_from_ss_win(ss_win)
                except Exception:
                    hwnd = 0

                if hwnd and win32gui:
                    try:
                        win32gui.SetForegroundWindow(hwnd)
                    except Exception:
                        pass
                
                # Send 'E' key via keyboard event as fallback/primary if no COM method exists for "Erase All"
                # View.EraseDrawing() exists?
                try:
                    view = ss_win.View
                    if hasattr(view, "EraseDrawing"):
                        view.EraseDrawing()
                    else:
                        # Fallback to key
                        if win32api and win32con:
                            vk = ord("E")
                            win32api.keybd_event(vk, 0, 0, 0)
                            win32api.keybd_event(vk, 0, win32con.KEYEVENTF_KEYUP, 0)
                except Exception:
                     # Fallback to key
                    if win32api and win32con:
                        vk = ord("E")
                        win32api.keybd_event(vk, 0, 0, 0)
                        win32api.keybd_event(vk, 0, win32con.KEYEVENTF_KEYUP, 0)

                self._control_mode = "com"
                return
        except Exception as e:
            self._note_error("clear_screen_com", e)
        try:
            self._control_mode = "win32"
            hwnd = int(self._slideshow_hwnd or 0) or self._find_ppt_slideshow_hwnd()
            if hwnd and win32gui:
                try:
                    win32gui.SetForegroundWindow(int(hwnd))
                except Exception:
                    pass
            if win32api and win32con:
                vk = ord("E")
                win32api.keybd_event(vk, 0, 0, 0)
                win32api.keybd_event(vk, 0, win32con.KEYEVENTF_KEYUP, 0)
        except Exception:
            pass

    def _apply_ink_keep(self, ss_win):
        try:
            view = ss_win.View
        except Exception:
            return
        try:
            annotations = getattr(view, "InkAnnotations", None)
            if annotations is not None:
                save_method = getattr(annotations, "Save", None)
                if callable(save_method):
                    save_method()
        except Exception:
            pass

    def _apply_ink_discard(self, ss_win):
        try:
            view = ss_win.View
        except Exception:
            return
        try:
            erase_method = getattr(view, "EraseDrawing", None)
            if callable(erase_method):
                erase_method()
        except Exception:
            pass
        try:
            annotations = getattr(view, "InkAnnotations", None)
            if annotations is not None:
                clear_method = getattr(annotations, "Clear", None) or getattr(annotations, "Delete", None)
                if callable(clear_method):
                    clear_method()
        except Exception:
            pass

    @Slot()
    def end_show(self):
        try:
            ss_win = self._get_active_slideshow_window()
            view = getattr(ss_win, "View", None) if ss_win is not None else None
            if view is not None:
                if cfg.autoHandleInk.value and self._active_kind in {"ppt", "wps", "yozo"}:
                    if not self._pending_ink_prompt:
                        self._pending_ink_prompt = True
                        self.ink_prompt_requested.emit()
                    return
                view.Exit()
                self._control_mode = "com"
                return
        except Exception as e:
            self._note_error("end_show_com", e)
        try:
            self._control_mode = "win32"
            self._send_vk_to_slideshow(win32con.VK_ESCAPE if win32con else 0x1B)
        except Exception:
            pass

    @Slot(bool)
    def end_show_with_ink_choice(self, keep):
        self._pending_ink_prompt = False
        try:
            app = self._get_active_app()
            ss_win = self._get_active_slideshow_window()
            if app and ss_win is not None:
                original_alerts = None
                try:
                    original_alerts = app.DisplayAlerts
                except Exception:
                    original_alerts = None
                try:
                    app.DisplayAlerts = 2
                except Exception:
                    pass
                if keep:
                    self._apply_ink_keep(ss_win)
                else:
                    self._apply_ink_discard(ss_win)
                ss_win.View.Exit()
                try:
                    if original_alerts is not None:
                        app.DisplayAlerts = original_alerts
                    else:
                        app.DisplayAlerts = -1
                except Exception:
                    pass
                self._control_mode = "com"
                return
        except Exception as e:
            self._note_error("end_show_ink", e)
        try:
            self._control_mode = "win32"
            self._send_vk_to_slideshow(win32con.VK_ESCAPE if win32con else 0x1B)
        except Exception:
            pass

    @Slot(int)
    def set_pointer_type(self, pointer_type):
        try:
            ss_win = self._get_active_slideshow_window()
            view = getattr(ss_win, "View", None) if ss_win is not None else None
            if view is not None:
                view.PointerType = pointer_type
        except Exception as e:
            self._note_error("set_pointer_type", e)

    @Slot(int, int, int)
    def set_pen_color(self, r, g, b):
        try:
            ss_win = self._get_active_slideshow_window()
            view = getattr(ss_win, "View", None) if ss_win is not None else None
            if view is not None:
                rgb = r + (g << 8) + (b << 16)
                view.PointerColor.RGB = rgb
        except Exception as e:
            self._note_error("set_pen_color", e)

    @Slot(int)
    def go_to_slide(self, index):
        try:
            ss_win = self._get_active_slideshow_window()
            view = getattr(ss_win, "View", None) if ss_win is not None else None
            if view is not None:
                view.GotoSlide(index)
        except Exception as e:
            self._note_error("go_to_slide", e)
        try:
            index = int(index)
            if self._control_mode == "win32" and self._degraded_total > 0 and 1 <= index <= self._degraded_total:
                self._degraded_current = index
                if self._degraded_current != self._current_slide or self._degraded_total != self._total_slides:
                    self._current_slide = self._degraded_current
                    self._total_slides = self._degraded_total
                    self.slide_changed.emit(self._degraded_current, self._degraded_total)
        except Exception:
            pass
            
    @Slot(int, str)
    def export_slide_thumbnail(self, index, path):
        try:
            # Ensure directory exists
            directory = os.path.dirname(path)
            if directory and not os.path.exists(directory):
                os.makedirs(directory, exist_ok=True)
                
            app = self._get_active_app()
            ss_win = self._get_active_slideshow_window()
            pres = self._get_presentation_from_ss_win(ss_win, app) if ss_win is not None else self._get_primary_presentation(app)
            total = self._extract_slide_total(pres)
            if pres is not None and 1 <= index <= total:
                pres.Slides(index).Export(path, "PNG", 320, 180)
                self.thumbnail_generated.emit(index, path)
        except Exception as e:
            self._note_error("export_slide_thumbnail", e)


class PPTMonitor(QObject):
    """
    Facade for PPTWorker. Runs worker in a separate thread.
    """
    slideshow_started = Signal()
    slideshow_ended = Signal()
    slide_changed = Signal(int, int)
    window_geometry_changed = Signal(object, object)
    overlay_visibility_changed = Signal(bool)
    slideshow_hwnd_changed = Signal(int)
    video_state_changed = Signal(float, float, float)
    thumbnail_generated = Signal(int, str)
    restrictions_changed = Signal(bool, bool)
    
    # Internal signals to worker
    _req_start = Signal()
    _req_stop = Signal()
    _req_next = Signal()
    _req_prev = Signal()
    _req_clear = Signal()
    _req_end = Signal()
    _req_end_with_ink = Signal(bool)
    _req_ptr_type = Signal(int)
    _req_pen_color = Signal(int, int, int)
    _req_goto = Signal(int)
    _req_export = Signal(int, str)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._thread = QThread()
        self._worker = PPTWorker()
        self._worker.moveToThread(self._thread)

        # Wire up signals (Worker -> Self)
        self._worker.slideshow_started.connect(self.slideshow_started)
        self._worker.slideshow_ended.connect(self.slideshow_ended)
        self._worker.slide_changed.connect(self._on_slide_changed)
        self._worker.window_geometry_changed.connect(self._on_geometry_changed)
        self._worker.overlay_visibility_changed.connect(self.overlay_visibility_changed)
        self._worker.slideshow_hwnd_changed.connect(self.slideshow_hwnd_changed)
        self.slideshow_hwnd_changed.connect(self._on_slideshow_hwnd_changed)
        self._worker.video_state_changed.connect(self.video_state_changed)
        self._worker.video_state_changed.connect(self._update_local_video_state)
        self._worker.thumbnail_generated.connect(self.thumbnail_generated)
        self._worker.restrictions_changed.connect(self.restrictions_changed)
        self._worker.active_kind_changed.connect(self._on_active_kind_changed)
        self._worker.ink_prompt_requested.connect(self._on_ink_prompt_requested)
        self._worker.finished.connect(self._thread.quit)
        self._thread.finished.connect(self._worker.deleteLater)

        # Wire up requests (Self -> Worker)
        self._req_start.connect(self._worker.start)
        self._req_stop.connect(self._worker.stop)
        self._req_next.connect(self._worker.go_next)
        self._req_prev.connect(self._worker.go_previous)
        self._req_clear.connect(self._worker.clear_screen)
        self._req_end.connect(self._worker.end_show)
        self._req_end_with_ink.connect(self._worker.end_show_with_ink_choice)
        self._req_ptr_type.connect(self._worker.set_pointer_type)
        self._req_pen_color.connect(self._worker.set_pen_color)
        self._req_goto.connect(self._worker.go_to_slide)
        self._req_export.connect(self._worker.export_slide_thumbnail)
        
        # Local state cache (for synchronous getters if needed)
        self._current = 0
        self._total = 0
        self._video_ratio = 0.0
        self._video_pos = 0.0
        self._video_len = 0.0
        self._last_rect_raw = None
        self._slideshow_hwnd = 0
        self._overlay = None
        self._active_kind = None
        self._pending_ink_prompt = False
        
        self._thread.start()

    def __del__(self):
        self.stop_monitoring()

    def start_monitoring(self):
        self._req_start.emit()

    def stop_monitoring(self):
        if self._thread.isRunning():
            self._req_stop.emit()
            self._thread.wait(2000) # Wait for worker to stop and thread to quit
            if self._thread.isRunning():
                self._thread.terminate()
                self._thread.wait()

    def set_overlay(self, overlay):
        if self._overlay is overlay:
            return
        if self._overlay:
            try:
                self._overlay.ink_prompt_result.disconnect(self._on_ink_prompt_result)
            except Exception:
                pass
        self._overlay = overlay
        if overlay:
            try:
                overlay.ink_prompt_result.connect(self._on_ink_prompt_result)
            except Exception:
                pass

    # --- Public API (Async) ---
    def go_next(self):
        self._req_next.emit()

    def go_previous(self):
        self._req_prev.emit()

    def clear_screen(self):
        self._req_clear.emit()

    def end_show(self):
        self._req_end.emit()

    def set_pointer_type(self, ptr_type):
        self._req_ptr_type.emit(ptr_type)

    def set_pen_color(self, r, g, b):
        self._req_pen_color.emit(r, g, b)

    def go_to_slide(self, index):
        self._req_goto.emit(index)

    def export_slide_thumbnail(self, index, path):
        self._req_export.emit(index, path)
        
    def force_update_geometry(self):
        rect = self._last_rect_raw
        if rect is None or rect.isEmpty():
            return
        self._on_geometry_changed(QRect(rect), None)

    def _on_active_kind_changed(self, kind):
        self._active_kind = kind or None

    def _on_ink_prompt_requested(self):
        if self._pending_ink_prompt:
            return
        self._pending_ink_prompt = True
        if self._overlay:
            self._overlay.show_ink_prompt()
        else:
            self._pending_ink_prompt = False
            self._req_end_with_ink.emit(True)

    def _on_ink_prompt_result(self, keep):
        if not self._pending_ink_prompt:
            return
        self._pending_ink_prompt = False
        self._req_end_with_ink.emit(bool(keep))

    # --- State Handling ---
    def _on_slide_changed(self, current, total):
        self._current = current
        self._total = total
        self.slide_changed.emit(current, total)

    def _on_slideshow_hwnd_changed(self, hwnd):
        try:
            self._slideshow_hwnd = int(hwnd or 0)
        except Exception:
            self._slideshow_hwnd = 0

    def _on_geometry_changed(self, rect_raw, meta):
        if rect_raw and not rect_raw.isEmpty():
            self._last_rect_raw = QRect(rect_raw)
        x, y, w, h = rect_raw.x(), rect_raw.y(), rect_raw.width(), rect_raw.height()
        cx, cy = x + w // 2, y + h // 2
        raw_is_qt_units_override = None
        raw_is_physical = None
        meta_dpi = 0
        try:
            if isinstance(meta, dict) and "raw_is_physical" in meta:
                raw_is_physical = bool(meta.get("raw_is_physical"))
                raw_is_qt_units_override = not raw_is_physical
                meta_dpi = int(meta.get("dpi") or 0)
        except Exception:
            raw_is_qt_units_override = None
            raw_is_physical = None
            meta_dpi = 0

        target_mode = cfg.overlayScreen.value
        screens = QGuiApplication.screens()

        ppt_screen = None
        try:
            hmonitor = win32api.MonitorFromPoint((cx, cy), win32con.MONITOR_DEFAULTTONEAREST)
            m_info = win32api.GetMonitorInfo(hmonitor)
            m_name = m_info['Device']
            for s in screens:
                if s.name() == m_name:
                    ppt_screen = s
                    break
            if not ppt_screen:
                for s in screens:
                    s_name = s.name().replace('\x00', '').strip()
                    m_name_clean = m_name.replace('\x00', '').strip()
                    if s_name == m_name_clean:
                        ppt_screen = s
                        break
        except Exception:
            pass

        if not ppt_screen:
            ppt_screen = QGuiApplication.primaryScreen()

        display_screen = None
        if target_mode == "Primary":
            display_screen = QGuiApplication.primaryScreen()
        elif isinstance(target_mode, str) and target_mode and not target_mode.startswith("Screen ") and target_mode != "Auto":
            try:
                for s in screens:
                    if s.name() == target_mode:
                        display_screen = s
                        break
                if display_screen is None:
                    cleaned = target_mode.replace("\x00", "").strip()
                    for s in screens:
                        if s.name().replace("\x00", "").strip() == cleaned:
                            display_screen = s
                            break
            except Exception:
                pass
        elif target_mode.startswith("Screen "):
            try:
                idx = int(target_mode.split(" ")[1]) - 1
                if 0 <= idx < len(screens):
                    display_screen = screens[idx]
            except:
                pass

        if not display_screen or target_mode == "Auto":
            display_screen = ppt_screen

        if display_screen:
            rect_logical = display_screen.geometry()
            self.window_geometry_changed.emit(rect_logical, display_screen)
        else:
            self.window_geometry_changed.emit(rect_raw, None)

    def _update_local_video_state(self, ratio, pos, length):
        self._video_ratio = ratio
        self._video_pos = pos
        self._video_len = length

    # --- Getters (Cached) ---
    def get_page_info(self):
        return self._current, self._total

    def get_total_slides(self):
        return self._total
        
    def get_video_progress(self):
        return self._video_ratio, self._video_pos, self._video_len
