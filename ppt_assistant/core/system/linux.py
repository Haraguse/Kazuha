import subprocess
import shutil
from .base import SystemAPI

try:
    from pywpsrpc.rpcwppapi import createWppRpcInstance, wppapi
    from pywpsrpc import RpcIter, common
    PYWPSRPC_AVAILABLE = True
except ImportError:
    PYWPSRPC_AVAILABLE = False
    createWppRpcInstance = None
    wppapi = None
    RpcIter = None
    common = None


class LinuxSystemAPI(SystemAPI):
    def __init__(self):
        self._rpc = None
        self._rpc_initialized = False

    def _init_rpc(self):
        if not PYWPSRPC_AVAILABLE or createWppRpcInstance is None:
            return False
        if self._rpc is not None:
            return True
        try:
            hr, self._rpc = createWppRpcInstance()
            if hr != 0:
                self._rpc = None
                return False
            self._rpc_initialized = True
            return True
        except Exception:
            self._rpc = None
            return False

    def get_media_info(self):
        if shutil.which("playerctl"):
            try:
                status = subprocess.check_output(["playerctl", "status"], text=True).strip()
                title = subprocess.check_output(["playerctl", "metadata", "title"], text=True).strip()
                artist = subprocess.check_output(["playerctl", "metadata", "artist"], text=True).strip()
                return {
                    "title": title,
                    "artist": artist,
                    "status": status,
                    "position_ms": 0,
                    "duration_ms": 0,
                }
            except Exception:
                pass
        return {
            "title": "",
            "artist": "",
            "status": "Stopped",
            "position_ms": 0,
            "duration_ms": 0,
        }

    def get_file_icon(self, path):
        return None

    def get_ppt_slideshow_hwnd(self):
        return 0

    def is_wps_slideshow_active(self) -> bool:
        if not self._init_rpc():
            return False
        if self._rpc is None:
            return False
        try:
            hr, app = self._rpc.getWppApplication()
            if hr != 0 or app is None:
                return False
            windows = getattr(app, "SlideShowWindows", None)
            if windows is None:
                return False
            count = int(getattr(windows, "Count", 0) or 0)
            return count > 0
        except Exception:
            return False

    def get_wps_slide_info(self) -> tuple[int, int]:
        if not self._init_rpc():
            return 0, 0
        if self._rpc is None:
            return 0, 0
        try:
            hr, app = self._rpc.getWppApplication()
            if hr != 0 or app is None:
                return 0, 0
            windows = getattr(app, "SlideShowWindows", None)
            if windows is None:
                return 0, 0
            count = int(getattr(windows, "Count", 0) or 0)
            if count <= 0:
                return 0, 0
            ss_win = windows(1)
            view = getattr(ss_win, "View", None)
            if view is None:
                return 0, 0
            current = 0
            try:
                value = getattr(view, "CurrentShowPosition", 0)
                if callable(value):
                    value = value()
                current = int(value or 0)  # type: ignore[arg-type]
            except Exception:
                pass
            if current <= 0:
                try:
                    slide = getattr(view, "Slide", None)
                    index = getattr(slide, "SlideIndex", 0) if slide is not None else 0
                    if callable(index):
                        index = index()
                    current = int(index or 0)  # type: ignore[arg-type]
                except Exception:
                    pass
            presentation = getattr(ss_win, "Presentation", None)
            if presentation is None:
                presentation = getattr(app, "ActivePresentation", None)
            total = 0
            if presentation is not None:
                slides = getattr(presentation, "Slides", None)
                total = int(getattr(slides, "Count", 0) or 0)  # type: ignore[arg-type]
            return current, total
        except Exception:
            return 0, 0

    def start_focus_watcher(self, callback):
        pass

    def stop_focus_watcher(self):
        pass

    def get_system_fonts(self):
        if shutil.which("fc-list"):
            try:
                output = subprocess.check_output(["fc-list", ":", "family"], text=True)
                fonts = set()
                for line in output.splitlines():
                    for f in line.split(","):
                        fonts.add(f.strip())
                return sorted(list(fonts))
            except Exception:
                pass
        return []

    def get_display_scale(self):
        return 1.0

    def find_wps_process(self) -> bool:
        try:
            result = subprocess.run(
                ["pgrep", "-x", "wpp"],
                capture_output=True,
                text=True
            )
            if result.returncode == 0 and result.stdout.strip():
                return True
            result = subprocess.run(
                ["pgrep", "-f", "wps"],
                capture_output=True,
                text=True
            )
            return result.returncode == 0 and bool(result.stdout.strip())
        except Exception:
            return False

    def get_active_window_info(self) -> dict:
        info = {
            "title": "",
            "class": "",
            "pid": 0,
            "is_wps": False,
            "is_slideshow": False,
        }
        if shutil.which("xdotool"):
            try:
                result = subprocess.run(
                    ["xdotool", "getactivewindow", "getwindowname"],
                    capture_output=True,
                    text=True
                )
                if result.returncode == 0:
                    info["title"] = result.stdout.strip()
                result = subprocess.run(
                    ["xdotool", "getactivewindow", "getwindowclassname"],
                    capture_output=True,
                    text=True
                )
                if result.returncode == 0:
                    info["class"] = result.stdout.strip()
                result = subprocess.run(
                    ["xdotool", "getactivewindow", "getwindowpid"],
                    capture_output=True,
                    text=True
                )
                if result.returncode == 0:
                    try:
                        info["pid"] = int(result.stdout.strip())
                    except ValueError:
                        pass
            except Exception:
                pass
        title_lower = info["title"].lower()
        slideshow_hints = [
            "slide show", "slideshow", "幻灯片放映", "幻燈片放映",
            "wps presentation", "演示", "投影片放映"
        ]
        info["is_slideshow"] = any(hint in title_lower for hint in slideshow_hints)
        info["is_wps"] = "wps" in title_lower or "wpp" in info["class"].lower()
        return info
