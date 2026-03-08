import sys
import ctypes
import os
import json
import base64
from .base import SystemAPI

try:
    import win32gui
    import win32api
    import win32con
    import win32com.client
    from ctypes import wintypes
except ImportError:
    win32gui = None
    win32api = None
    win32con = None
    win32com = None
    wintypes = None

try:
    from winsdk.windows.media.control import GlobalSystemMediaTransportControlsSessionManager
    WINSDK_AVAILABLE = True
except ImportError:
    WINSDK_AVAILABLE = False

class WindowsSystemAPI(SystemAPI):
    def __init__(self):
        self._focus_thread = None

    def get_media_info(self):
        if not WINSDK_AVAILABLE:
            return {"title": "", "artist": "", "status": "Stopped"}
        
        # Note: winsdk calls must be awaited or run in async loop if they return coroutines.
        # However, GlobalSystemMediaTransportControlsSessionManager.request_async() returns an IAsyncOperation.
        # In a synchronous context like this, we might need a helper or use the PowerShell fallback if async is tricky.
        # The existing code in overlay.py used a subprocess call to PowerShell for this reason.
        # Let's reuse that approach for simplicity and robustness against async issues in Qt threads.
        return self._get_media_info_from_powershell()

    def _get_media_info_from_powershell(self):
        script = r'''
$ErrorActionPreference="SilentlyContinue"
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$manager=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync().GetAwaiter().GetResult()
$session=$manager.GetCurrentSession()
if ($session -eq $null) { @{status=""; title=""} | ConvertTo-Json -Compress; exit }
$props=$session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult()
$statusValue=[int]$session.GetPlaybackInfo().PlaybackStatus
$title=$props.Title
$artist=$props.Artist
$display=$title
if ($artist) { $display="$title - $artist" }
$state="Stopped"
if ($statusValue -eq 4) { $state="Playing" } elseif ($statusValue -eq 5) { $state="Paused" }
@{status=$state; title=$display} | ConvertTo-Json -Compress
'''
        try:
            startupinfo = subprocess.STARTUPINFO()
            startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
            startupinfo.wShowWindow = 0
            creationflags = subprocess.CREATE_NO_WINDOW
            
            result = subprocess.run(
                ["powershell", "-Command", script],
                capture_output=True,
                text=True,
                creationflags=creationflags,
                startupinfo=startupinfo,
                timeout=2
            )
            if result.returncode == 0 and result.stdout.strip():
                data = json.loads(result.stdout)
                return {
                    "title": data.get("title", ""),
                    "artist": "",
                    "status": data.get("status", "Stopped")
                }
        except Exception:
            pass
        return {"title": "", "artist": "", "status": "Stopped"}

    def get_file_icon(self, path):
        # We can implement the full win32 icon extraction logic here
        # For now, let's just return None and let the fallback handle it
        # or move the logic from icon_helper.py if we want full fidelity.
        # Given the task scope, let's defer to icon_helper but provide the primitives if needed.
        return None

    def get_ppt_slideshow_hwnd(self):
        if not win32gui:
            return 0
        try:
            fg = int(win32gui.GetForegroundWindow() or 0)
        except Exception:
            fg = 0

        def _is_ppt_slideshow(hwnd: int) -> bool:
            try:
                if not hwnd: return False
                if not win32gui.IsWindowVisible(int(hwnd)): return False
                cls_name = win32gui.GetClassName(int(hwnd))
                # PowerPoint slideshow window classes
                if cls_name == "screenClass": return True
                if cls_name == "WPP SlideShow Window 8.0": return True # WPS
                if cls_name == "WPP SlideShow Window": return True # WPS
                return False
            except Exception:
                return False

        if _is_ppt_slideshow(fg):
            return fg
        
        # Search all windows
        found_hwnd = 0
        def _enum_cb(hwnd, ctx):
            nonlocal found_hwnd
            if found_hwnd: return
            if _is_ppt_slideshow(hwnd):
                found_hwnd = hwnd

        try:
            win32gui.EnumWindows(_enum_cb, None)
        except Exception:
            pass
            
        return found_hwnd

    def start_focus_watcher(self, callback):
        # We need to import the watcher class here to avoid circular imports
        # Or implement it directly.
        # Since win_focus_watcher.py is already complex, let's keep it there
        # but make it accessible.
        # Actually, let's return the watcher instance.
        from ...core.win_focus_watcher import WindowsFocusWatcher
        self._focus_watcher = WindowsFocusWatcher()
        self._focus_watcher.foreground_changed.connect(callback)
        self._focus_watcher.start()
        return self._focus_watcher

    def stop_focus_watcher(self):
        if hasattr(self, '_focus_watcher') and self._focus_watcher:
            self._focus_watcher.stop()

    def get_system_fonts(self):
        try:
            import winreg
            keys = [
                (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
                (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
            ]
            fonts = set()
            suffixes = (" (TrueType)", " (OpenType)", " (Type 1)", " (All res)")
            for root, path in keys:
                try:
                    with winreg.OpenKey(root, path) as k:
                        try:
                            count = winreg.QueryInfoKey(k)[1]
                        except Exception:
                            count = 0
                        for i in range(count):
                            try:
                                name, _, _ = winreg.EnumValue(k, i)
                            except Exception:
                                continue
                            if not isinstance(name, str):
                                continue
                            display = name.strip()
                            for suf in suffixes:
                                if display.endswith(suf):
                                    display = display[: -len(suf)].strip()
                                    break
                            if display:
                                fonts.add(display)
                except OSError:
                    continue
            return sorted(fonts, key=lambda s: s.lower())
        except Exception:
            return []
