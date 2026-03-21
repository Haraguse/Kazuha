import sys
import ctypes
import os
import json
import base64
import subprocess
import time
import atexit
import queue
import shutil
import threading
from .base import SystemAPI

try:
    import win32gui
    import win32api
    import win32con
    import win32process
    import win32com.client
    from ctypes import wintypes
except ImportError:
    win32gui = None
    win32api = None
    win32con = None
    win32process = None
    win32com = None
    wintypes = None

PRESENTATION_SLIDESHOW_CLASSES = {
    "screenClass",
    "wppSlideShowWindowClass",
    "WPP SlideShow Window",
    "WPP SlideShow Window 8.0",
}
PRESENTATION_PROCESS_NAMES = {
    "powerpnt.exe",
    "wpp.exe",
    "kwpp.exe",
    "yozo_impress.exe",
    "yozopg.exe",
    "yozo_office.exe",
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

class WindowsSystemAPI(SystemAPI):
    def __init__(self):
        self._focus_thread = None
        self._smtc_next_allowed = 0.0
        self._smtc_lock = threading.Lock()
        self._smtc_worker = None
        self._smtc_reader_thread = None
        self._smtc_output_queue = None
        self._smtc_request_id = 0
        atexit.register(self.close)

    def get_media_info(self):
        now = time.monotonic()
        if now < self._smtc_next_allowed:
            return {"title": "", "artist": "", "status": "Stopped"}
        try:
            return self._get_media_info_from_worker()
        except Exception:
            self._restart_smtc_worker()
            self._smtc_next_allowed = now + 5.0
            return {"title": "", "artist": "", "status": "Stopped"}

    def _get_media_info_from_worker(self):
        process, output_queue = self._ensure_smtc_worker()
        request_id = self._next_smtc_request_id()
        with self._smtc_lock:
            if not process or process.poll() is not None or not process.stdin:
                raise RuntimeError("SMTC worker is not available")
            process.stdin.write(f"{request_id}\n")
            process.stdin.flush()
        data = self._wait_for_smtc_response(request_id, output_queue, timeout=1.2)
        title = (data.get("title") or "").strip()
        artist = (data.get("artist") or "").strip()
        display_title = title
        if title and artist:
            display_title = f"{title} - {artist}"
        return {
            "title": display_title,
            "artist": artist,
            "status": data.get("status", "Stopped") or "Stopped"
        }

    def _ensure_smtc_worker(self):
        with self._smtc_lock:
            if self._smtc_worker and self._smtc_worker.poll() is None and self._smtc_output_queue is not None:
                return self._smtc_worker, self._smtc_output_queue
            self._stop_smtc_worker_locked()
            if not self._start_smtc_dotnet_worker_locked():
                self._start_smtc_powershell_worker_locked()
            if not self._smtc_worker or not self._smtc_output_queue:
                raise RuntimeError("Failed to start SMTC worker")
            return self._smtc_worker, self._smtc_output_queue

    def _get_app_root_dir(self):
        if getattr(sys, "frozen", False):
            return os.path.dirname(sys.executable)
        return os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

    def _get_smtc_helper_project_path(self):
        return os.path.join(self._get_app_root_dir(), "scripts", "smtc_helper", "SmtcHelper.csproj")

    def _get_smtc_helper_executable_candidates(self):
        root_dir = self._get_app_root_dir()
        return [
            os.path.join(root_dir, "scripts", "smtc_helper", "bin", "Release", "net8.0-windows10.0.19041.0", "SmtcHelper.exe"),
            os.path.join(root_dir, "scripts", "smtc_helper", "SmtcHelper.exe"),
            os.path.join(root_dir, "smtc_helper", "SmtcHelper.exe"),
        ]

    def _resolve_smtc_helper_executable(self):
        for candidate in self._get_smtc_helper_executable_candidates():
            if os.path.exists(candidate):
                return candidate
        return ""

    def _create_dotnet_build_env(self):
        root_dir = self._get_app_root_dir()
        env = os.environ.copy()
        env["DOTNET_CLI_HOME"] = os.path.join(root_dir, ".dotnet_home")
        env["APPDATA"] = os.path.join(root_dir, ".dotnet_appdata")
        env["LOCALAPPDATA"] = os.path.join(root_dir, ".dotnet_localappdata")
        env["NUGET_PACKAGES"] = os.path.join(root_dir, ".nuget", "packages")
        for key in ("DOTNET_CLI_HOME", "APPDATA", "LOCALAPPDATA", "NUGET_PACKAGES"):
            try:
                os.makedirs(env[key], exist_ok=True)
            except Exception:
                pass
        return env

    def _build_smtc_helper_locked(self):
        project_path = self._get_smtc_helper_project_path()
        if not os.path.exists(project_path):
            return ""
        if not shutil.which("dotnet"):
            return ""
        startupinfo = subprocess.STARTUPINFO()
        startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startupinfo.wShowWindow = 0
        creationflags = subprocess.CREATE_NO_WINDOW
        try:
            result = subprocess.run(
                ["dotnet", "build", project_path, "-c", "Release", "-nologo"],
                capture_output=True,
                text=True,
                creationflags=creationflags,
                startupinfo=startupinfo,
                timeout=90,
                env=self._create_dotnet_build_env()
            )
        except Exception:
            return ""
        if result.returncode != 0:
            return ""
        return self._resolve_smtc_helper_executable()

    def _start_smtc_dotnet_worker_locked(self):
        helper_exe = self._resolve_smtc_helper_executable()
        if not helper_exe:
            helper_exe = self._build_smtc_helper_locked()
        if not helper_exe:
            return False
        try:
            return self._launch_smtc_worker_locked([helper_exe])
        except Exception:
            return False

    def _launch_smtc_worker_locked(self, command):
        startupinfo = subprocess.STARTUPINFO()
        startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startupinfo.wShowWindow = 0
        creationflags = subprocess.CREATE_NO_WINDOW
        process = subprocess.Popen(
            command,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
            encoding="utf-8",
            errors="replace",
            bufsize=1,
            creationflags=creationflags,
            startupinfo=startupinfo
        )
        output_queue = queue.Queue()
        self._smtc_worker = process
        self._smtc_output_queue = output_queue
        self._smtc_reader_thread = threading.Thread(
            target=self._read_smtc_worker_output,
            args=(process, output_queue),
            daemon=True
        )
        self._smtc_reader_thread.start()
        return True

    def _start_smtc_powershell_worker_locked(self):
        script = r'''
$ErrorActionPreference="SilentlyContinue"
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName System.Runtime.WindowsRuntime

function Write-JsonResponse($payload) {
    [Console]::Out.WriteLine(($payload | ConvertTo-Json -Compress))
    [Console]::Out.Flush()
}

$manager = $null
while (($requestId = [Console]::In.ReadLine()) -ne $null) {
    if ($requestId -eq "__EXIT__") { break }
    try {
        if ($manager -eq $null) {
            $manager = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync().GetAwaiter().GetResult()
        }
        $session = $manager.GetCurrentSession()
        if ($session -eq $null) {
            Write-JsonResponse @{request_id=$requestId; status=""; title=""; artist=""}
            continue
        }
        $props = $session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult()
        $statusValue = [int]$session.GetPlaybackInfo().PlaybackStatus
        $state = "Stopped"
        if ($statusValue -eq 4) {
            $state = "Playing"
        } elseif ($statusValue -eq 5) {
            $state = "Paused"
        }
        $title = ""
        $artist = ""
        if ($props -ne $null) {
            $title = $props.Title
            $artist = $props.Artist
        }
        Write-JsonResponse @{request_id=$requestId; status=$state; title=$title; artist=$artist}
    } catch {
        $manager = $null
        Write-JsonResponse @{request_id=$requestId; status="Stopped"; title=""; artist=""}
    }
}
'''
        self._launch_smtc_worker_locked(["powershell", "-NoProfile", "-NonInteractive", "-Command", script])

    def _read_smtc_worker_output(self, process, output_queue):
        try:
            while process.stdout:
                line = process.stdout.readline()
                if not line:
                    break
                output_queue.put(line.strip())
        except Exception:
            pass
        finally:
            output_queue.put(None)

    def _wait_for_smtc_response(self, request_id, output_queue, timeout):
        deadline = time.monotonic() + timeout
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError("Timed out waiting for SMTC worker response")
            item = output_queue.get(timeout=remaining)
            if item is None:
                raise RuntimeError("SMTC worker exited unexpectedly")
            try:
                data = json.loads(item)
            except Exception:
                continue
            if str(data.get("request_id", "")) != str(request_id):
                continue
            return data

    def _next_smtc_request_id(self):
        with self._smtc_lock:
            self._smtc_request_id += 1
            return self._smtc_request_id

    def _restart_smtc_worker(self):
        with self._smtc_lock:
            self._stop_smtc_worker_locked()

    def _stop_smtc_worker_locked(self):
        process = self._smtc_worker
        self._smtc_worker = None
        self._smtc_output_queue = None
        self._smtc_reader_thread = None
        if not process:
            return
        try:
            if process.poll() is None and process.stdin:
                process.stdin.write("__EXIT__\n")
                process.stdin.flush()
        except Exception:
            pass
        try:
            if process.stdin:
                process.stdin.close()
        except Exception:
            pass
        try:
            process.wait(timeout=0.5)
        except Exception:
            try:
                process.kill()
            except Exception:
                pass
            try:
                process.wait(timeout=0.5)
            except Exception:
                pass
        try:
            if process.stdout:
                process.stdout.close()
        except Exception:
            pass

    def close(self):
        with self._smtc_lock:
            self._stop_smtc_worker_locked()

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

        def _get_com_slideshow_hwnd() -> int:
            if not win32com:
                return 0
            for prog_id in ("PowerPoint.Application", "KWPP.Application", "YozoPG.Application", "YozoPG.Application.1"):
                try:
                    app = win32com.client.GetActiveObject(prog_id)
                except Exception:
                    continue
                try:
                    windows = getattr(app, "SlideShowWindows", None)
                    count = int(getattr(windows, "Count", 0) or 0)
                except Exception:
                    count = 0
                for i in range(1, count + 1):
                    try:
                        ss_win = windows(i)
                        hwnd = getattr(ss_win, "HWND", 0)
                        if callable(hwnd):
                            hwnd = hwnd()
                        hwnd = int(hwnd or 0)
                        if hwnd:
                            return hwnd
                    except Exception:
                        continue
            return 0

        def _is_ppt_slideshow(hwnd: int) -> bool:
            try:
                if not hwnd: return False
                if not win32gui.IsWindowVisible(int(hwnd)): return False
                cls_name = win32gui.GetClassName(int(hwnd))
                if cls_name in PRESENTATION_SLIDESHOW_CLASSES:
                    return True
                title = (win32gui.GetWindowText(int(hwnd)) or "").strip().lower()
                if title and any(hint in title for hint in STRICT_PRESENTATION_SLIDESHOW_TITLE_HINTS):
                    return True
                if win32api and win32process:
                    try:
                        _, pid = win32process.GetWindowThreadProcessId(int(hwnd))
                        if pid:
                            handle = win32api.OpenProcess(0x1000, False, pid)
                            try:
                                exe = win32process.GetModuleFileNameEx(handle, 0) or ""
                            finally:
                                try:
                                    win32api.CloseHandle(handle)
                                except Exception:
                                    pass
                            if os.path.basename(exe).strip().lower() in PRESENTATION_PROCESS_NAMES and title and any(hint in title for hint in STRICT_PRESENTATION_SLIDESHOW_TITLE_HINTS):
                                return True
                    except Exception:
                        pass
                return False
            except Exception:
                return False

        if _is_ppt_slideshow(fg):
            return fg

        com_hwnd = _get_com_slideshow_hwnd()
        if com_hwnd:
            return int(com_hwnd)
        
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
