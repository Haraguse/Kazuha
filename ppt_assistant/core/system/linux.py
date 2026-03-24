import os
import subprocess
import shutil
from .base import SystemAPI

class LinuxSystemAPI(SystemAPI):
    def get_media_info(self):
        # Can use `playerctl` if installed
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
        # On Linux, rely on Qt's icon provider
        return None

    def get_ppt_slideshow_hwnd(self):
        # No concept of HWND on Linux in the same way, or at least not accessible easily
        # Maybe use xdotool to find LibreOffice Impress presentation window
        return 0

    def start_focus_watcher(self, callback):
        # Stub
        pass

    def stop_focus_watcher(self):
        # Stub
        pass

    def get_system_fonts(self):
        # Use fc-list if available
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
