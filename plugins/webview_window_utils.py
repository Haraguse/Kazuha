import ctypes
import sys
from ctypes import wintypes

_EXISTING_WINDOW_NOTIFY_MESSAGE = "Luminalium.WebView.NotifyExistingWindow"


def find_window(title, pid=None):
    if sys.platform != "win32":
        return None
    user32 = ctypes.windll.user32
    hwnd = user32.FindWindowW(None, title)
    if hwnd:
        return hwnd
    if not pid:
        return None
    return find_window_by_pid(pid)


def find_window_by_pid(pid):
    if sys.platform != "win32" or not pid:
        return None
    user32 = ctypes.windll.user32
    result = {"hwnd": None}

    @ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
    def enum_proc(hwnd, _lparam):
        if not user32.IsWindowVisible(hwnd):
            return True
        proc_id = wintypes.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(proc_id))
        if proc_id.value == pid:
            result["hwnd"] = hwnd
            return False
        return True

    user32.EnumWindows(enum_proc, 0)
    return result["hwnd"]


def bring_window_to_front(hwnd):
    if sys.platform != "win32" or not hwnd:
        return False
    user32 = ctypes.windll.user32
    kernel32 = ctypes.windll.kernel32
    fg_hwnd = user32.GetForegroundWindow()
    fg_thread = user32.GetWindowThreadProcessId(fg_hwnd, None)
    cur_thread = kernel32.GetCurrentThreadId()
    attached = False
    if fg_thread and fg_thread != cur_thread:
        attached = bool(user32.AttachThreadInput(cur_thread, fg_thread, True))
    user32.ShowWindow(hwnd, 9)
    user32.BringWindowToTop(hwnd)
    user32.SetForegroundWindow(hwnd)
    if attached:
        user32.AttachThreadInput(cur_thread, fg_thread, False)
    return True


def notify_existing_window(hwnd):
    if sys.platform != "win32" or not hwnd:
        return False
    try:
        user32 = ctypes.windll.user32
        message = user32.RegisterWindowMessageW(_EXISTING_WINDOW_NOTIFY_MESSAGE)
        if not message:
            return False
        user32.PostMessageW(hwnd, message, 0, 0)
        return True
    except Exception:
        return False
