import subprocess
import shutil
from .base import SystemAPI


class LinuxSystemAPI(SystemAPI):
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
