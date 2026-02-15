import win32com.client
import pythoncom
from PySide6.QtCore import QObject, Signal, QThread, QTimer, QPoint, QRect, Slot
from PySide6.QtGui import QGuiApplication
import time
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

    def _find_ppt_slideshow_hwnd(self) -> int:
        if not win32gui:
            return 0
        try:
            fg = int(win32gui.GetForegroundWindow() or 0)
        except Exception:
            fg = 0

        def _is_ppt_slideshow(hwnd: int) -> bool:
            try:
                if not hwnd:
                    return False
                if not win32gui.IsWindowVisible(int(hwnd)):
                    return False
                if win32gui.GetClassName(int(hwnd)) != "screenClass":
                    return False
                if win32process and win32api:
                    try:
                        _, pid = win32process.GetWindowThreadProcessId(int(hwnd))
                        if not pid:
                            return False
                        handle = win32api.OpenProcess(0x1000, False, pid)
                        exe = win32process.GetModuleFileNameEx(handle, 0) or ""
                        if not exe.lower().endswith("powerpnt.exe"):
                            return False
                    except Exception:
                        return False
                return True
            except Exception:
                return False

        if _is_ppt_slideshow(fg):
            return fg

        found = 0

        def _enum_cb(hwnd, _):
            nonlocal found
            if found:
                return
            if _is_ppt_slideshow(int(hwnd)):
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
        app = self.ppt_app
        if not app:
            return

        current = 0
        total = 0
        pres = None

        try:
            pres = getattr(app, "ActivePresentation", None)
            if pres is not None:
                total = int(getattr(getattr(pres, "Slides", None), "Count", 0) or 0)
        except Exception:
            pass

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
                if ss_view is not None:
                    current = int(getattr(ss_view, "CurrentShowPosition", 0) or 0)
        except Exception:
            pass

        try:
            active_win = getattr(app, "ActiveWindow", None)
            view = getattr(active_win, "View", None) if active_win is not None else None
            slide = getattr(view, "Slide", None) if view is not None else None
            if not current:
                current = int(getattr(slide, "SlideIndex", 0) or 0) if slide is not None else 0
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
            app = self.ppt_app
            if app is None:
                return
            pres = getattr(app, "ActivePresentation", None)
            if pres is not None:
                try:
                    total = int(getattr(getattr(pres, "Slides", None), "Count", 0) or 0)
                except Exception:
                    total = 0
            if not total:
                pv_windows = getattr(app, "ProtectedViewWindows", None)
                if pv_windows is not None and int(getattr(pv_windows, "Count", 0) or 0) > 0:
                    pv = pv_windows(1)
                    pres = getattr(pv, "Presentation", None)
                    if pres is not None:
                        try:
                            total = int(getattr(getattr(pres, "Slides", None), "Count", 0) or 0)
                        except Exception:
                            total = 0
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
        if self._com_initialized:
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
        # Fallback
        if self.ppt_app: return self.ppt_app
        if self.wps_app: return self.wps_app
        return None

    def _check_ppt_state(self):
        try:
            # 1. Try PowerPoint
            try:
                self.ppt_app = win32com.client.GetActiveObject("PowerPoint.Application")
            except Exception:
                self.ppt_app = None
                self._handle_stop("ppt")
                return

            try:
                protected_view = False
                pv_windows = getattr(self.ppt_app, "ProtectedViewWindows", None)
                if pv_windows is not None:
                    protected_view = int(getattr(pv_windows, "Count", 0) or 0) > 0
                self._update_restrictions(protected_view, self._presentation_readonly)
            except Exception:
                pass

            if self.ppt_app.SlideShowWindows.Count > 0:
                try:
                    # Find the best slide show window (avoiding Presenter View if possible)
                    ss_win = None
                    count = self.ppt_app.SlideShowWindows.Count
                    
                    if count == 1:
                        ss_win = self.ppt_app.SlideShowWindows(1)
                    else:
                        # Try to find the one with class name "screenClass"
                        for i in range(1, count + 1):
                            try:
                                tmp_win = self.ppt_app.SlideShowWindows(i)
                                hwnd = getattr(tmp_win, "HWND", 0)
                                if hwnd:
                                    class_name = win32gui.GetClassName(int(hwnd))
                                    if class_name == "screenClass":
                                        ss_win = tmp_win
                                        break
                            except:
                                continue
                        
                        # Fallback to the first one if not found
                        if ss_win is None:
                            ss_win = self.ppt_app.SlideShowWindows(1)

                    view = ss_win.View
                    state = view.State
                    
                    if state in [1, 2]: # Running or Paused
                        if not self._running:
                            self._running = True
                            self._set_active_kind("ppt")
                            self._control_mode = "com"
                            try:
                                hwnd = int(getattr(ss_win, "HWND", 0) or 0)
                                if hwnd and hwnd != self._slideshow_hwnd:
                                    self._slideshow_hwnd = hwnd
                                    self.slideshow_hwnd_changed.emit(hwnd)
                            except Exception:
                                pass
                            self._slideshow_started_at = time.monotonic()
                            self.slideshow_started.emit()
                        
                        if True:
                            current = 0
                            total = 0
                            presentation = None
                            try:
                                current = int(getattr(view, "CurrentShowPosition", 0) or 0)
                            except Exception:
                                current = 0
                            if not current:
                                try:
                                    current = int(getattr(getattr(view, "Slide", None), "SlideIndex", 0) or 0)
                                except Exception:
                                    current = 0
                            try:
                                presentation = getattr(ss_win, "Presentation", None)
                            except Exception:
                                presentation = None
                            if presentation is not None:
                                try:
                                    total = int(getattr(getattr(presentation, "Slides", None), "Count", 0) or 0)
                                except Exception:
                                    total = 0
                                try:
                                    pres_readonly = bool(getattr(presentation, "ReadOnly", False))
                                    self._update_restrictions(self._protected_view, pres_readonly)
                                except Exception:
                                    pass
                            if not total:
                                total = int(self._total_slides or 0)
                            if current > 0 and total > 0 and (current != self._current_slide or total != self._total_slides):
                                self._current_slide = current
                                self._total_slides = total
                                self.slide_changed.emit(current, total)
                                self._degraded_current = current
                                self._degraded_total = total
                        
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
            return

    def _check_wps_state(self):
        try:
            self.wps_app = win32com.client.GetActiveObject("KWPP.Application")
        except BaseException:
            self.wps_app = None
            self._handle_stop("wps")
            return

        try:
            if self.wps_app.SlideShowWindows.Count > 0:
                ss_win = None
                count = self.wps_app.SlideShowWindows.Count
                
                if count == 1:
                    ss_win = self.wps_app.SlideShowWindows(1)
                else:
                    # Try to find the slideshow window (avoiding presenter view)
                    # WPS slideshow window class is usually "wppSlideShowWindowClass"
                    for i in range(1, count + 1):
                        try:
                            tmp_win = self.wps_app.SlideShowWindows(i)
                            hwnd = getattr(tmp_win, "HWND", 0)
                            if hwnd:
                                class_name = win32gui.GetClassName(int(hwnd))
                                if class_name == "wppSlideShowWindowClass":
                                    ss_win = tmp_win
                                    break
                        except:
                            continue
                    
                    if ss_win is None:
                        ss_win = self.wps_app.SlideShowWindows(1)

                view = ss_win.View
                # WPS State might differ, usually 1=Running
                state = getattr(view, "State", 1)
                
                if state in [1, 2]:
                    if not self._running:
                        self._running = True
                        self._set_active_kind("wps")
                        try:
                            hwnd = int(getattr(ss_win, "HWND", 0) or 0)
                            if hwnd and hwnd != self._slideshow_hwnd:
                                self._slideshow_hwnd = hwnd
                                self.slideshow_hwnd_changed.emit(hwnd)
                        except Exception:
                            pass
                        self._slideshow_started_at = time.monotonic()
                        self.slideshow_started.emit()

                    if True:
                        current = 0
                        total = 0
                        try:
                            current = int(getattr(view, "CurrentShowPosition", 0) or 0)
                        except Exception:
                            current = 0
                        if not current:
                            try:
                                current = int(getattr(getattr(view, "Slide", None), "SlideIndex", 0) or 0)
                            except Exception:
                                current = 0
                        try:
                            presentation = getattr(ss_win, "Presentation", None)
                            total = int(getattr(getattr(presentation, "Slides", None), "Count", 0) or 0) if presentation is not None else 0
                        except Exception:
                            total = 0

                        if not total:
                            total = int(self._total_slides or 0)
                        if current > 0 and total > 0 and (current != self._current_slide or total != self._total_slides):
                            self._current_slide = current
                            self._total_slides = total
                            self.slide_changed.emit(current, total)
                            self._degraded_current = current
                            self._degraded_total = total

                    try:
                        self._update_window_rect(ss_win)
                        self._update_video_state(ss_win)
                    except Exception:
                        pass
                else:
                    self._handle_stop("wps")
            else:
                self._handle_stop("wps")
        except BaseException:
            self._handle_stop("wps")

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
                    hwnd = getattr(ss_win, "HWND", 0)
                    if hwnd:
                        hwnd = int(hwnd)
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
                    hwnd = int(getattr(ss_win, "HWND", 0) or 0)
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
            if win32process and win32api:
                try:
                    _, pid = win32process.GetWindowThreadProcessId(fg)
                    if pid:
                        handle = win32api.OpenProcess(0x1000, False, pid)
                        exe = win32process.GetModuleFileNameEx(handle, 0) or ""
                        exe_lower = exe.lower()
                        if exe_lower.endswith("powerpnt.exe") or exe_lower.endswith("wpp.exe") or exe_lower.endswith("kwpp.exe"):
                            return True
                except Exception:
                    pass
            title = win32gui.GetWindowText(fg) or ""
            title_lower = title.lower()
            if "powerpoint" in title_lower:
                return True
            if any(t in title for t in ["幻灯片放映", "演示文稿"]):
                return True
            return False
        except Exception:
            return False

    def _update_overlay_visibility(self, ss_win, rect):
        try:
            if not win32gui or not win32api or not win32con:
                return
            hwnd = getattr(ss_win, "HWND", 0)
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
        try:
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                app.SlideShowWindows(1).View.Next()
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
        try:
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                app.SlideShowWindows(1).View.Previous()
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
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                ss_win = app.SlideShowWindows(1)
                hwnd = int(getattr(ss_win, "HWND", 0) or 0)
                if hwnd and win32gui:
                    try:
                        win32gui.SetForegroundWindow(hwnd)
                    except Exception:
                        pass
                if win32api and win32con:
                    try:
                        vk = ord("E")
                        win32api.keybd_event(vk, 0, 0, 0)
                        win32api.keybd_event(vk, 0, win32con.KEYEVENTF_KEYUP, 0)
                    except Exception:
                        pass
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
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                if cfg.autoHandleInk.value and self._active_kind == "ppt":
                    if not self._pending_ink_prompt:
                        self._pending_ink_prompt = True
                        self.ink_prompt_requested.emit()
                    return
                app.SlideShowWindows(1).View.Exit()
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
            if app and app.SlideShowWindows.Count > 0:
                ss_win = app.SlideShowWindows(1)
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
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                app.SlideShowWindows(1).View.PointerType = pointer_type
        except Exception as e:
            self._note_error("set_pointer_type", e)

    @Slot(int, int, int)
    def set_pen_color(self, r, g, b):
        try:
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                rgb = r + (g << 8) + (b << 16)
                app.SlideShowWindows(1).View.PointerColor.RGB = rgb
        except Exception as e:
            self._note_error("set_pen_color", e)

    @Slot(int)
    def go_to_slide(self, index):
        try:
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                app.SlideShowWindows(1).View.GotoSlide(index)
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
            app = self._get_active_app()
            if app and app.SlideShowWindows.Count > 0:
                pres = app.SlideShowWindows(1).Presentation
                if 1 <= index <= pres.Slides.Count:
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
