import gc
import os
import sys
import time
import ctypes
import traceback

import psutil

_MEMORY_LIMIT_MB = 256
_SELF_LIMIT_MB = 32
_PARENT_LIMIT_MB = 256
_CHECK_INTERVAL_S = 5
_AGGRESSIVE_INTERVAL_S = 2
_CLEAN_CYCLE_S = 15


def _get_rss_mb(pid: int) -> float:
    try:
        return psutil.Process(pid).memory_info().rss / (1024 * 1024)
    except Exception:
        return 0.0


def _empty_working_set(handle: ctypes.c_void_p):
    try:
        ctypes.windll.kernel32.SetProcessWorkingSetSize(handle, -1, -1)
    except Exception:
        pass


def _trim_pid_windows(pid: int) -> bool:
    if sys.platform != "win32":
        return False
    try:
        PROCESS_SET_QUOTA = 0x0100
        PROCESS_QUERY_INFORMATION = 0x0400
        PROCESS_VM_OPERATION = 0x0008
        kernel32 = ctypes.windll.kernel32
        handle = kernel32.OpenProcess(
            PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION,
            False,
            pid,
        )
        if not handle:
            return False
        try:
            _empty_working_set(handle)

            try:
                ntdll = ctypes.windll.ntdll
                class PROCESS_WORKING_SET_EX(ctypes.Structure):
                    _fields_ = [
                        ("Flags", ctypes.c_ulonglong),
                    ]
                pwse = PROCESS_WORKING_SET_EX()
                pwse.Flags = 0
                ntdll.NtSetInformationProcess(
                    handle,
                    0x25,
                    ctypes.byref(pwse),
                    ctypes.sizeof(pwse),
                )
            except Exception:
                pass

            return True
        finally:
            kernel32.CloseHandle(handle)
    except Exception:
        return False


def _trim_memory_linux() -> bool:
    try:
        libc = ctypes.CDLL("libc.so.6")
        libc.malloc_trim.argtypes = [ctypes.c_size_t]
        libc.malloc_trim.restype = ctypes.c_int
        libc.malloc_trim(0)
        return True
    except Exception:
        return False


def _is_webview_process(proc: psutil.Process) -> bool:
    try:
        cmdline = proc.cmdline()
        for arg in cmdline:
            if "--webview-runner" in arg:
                return True
            if "QtWebEngine" in arg:
                return True
    except Exception:
        pass
    try:
        name = proc.name().lower()
        if "qtwebengine" in name:
            return True
    except Exception:
        pass
    return False


def _get_all_webview_pids(parent_pid: int) -> list[int]:
    pids = []
    try:
        parent = psutil.Process(parent_pid)
        for child in parent.children(recursive=True):
            try:
                if _is_webview_process(child):
                    pids.append(child.pid)
            except Exception:
                pass
    except Exception:
        pass
    return pids


def _force_trim_pids(pids: list[int]) -> int:
    trimmed = 0
    for pid in pids:
        if _trim_pid_windows(pid):
            trimmed += 1
    return trimmed


def _aggressive_gc():
    collected = 0
    for gen in range(3):
        collected += gc.collect(gen)
    gc.collect()
    garbage_before = len(gc.garbage)
    if gc.garbage:
        gc.garbage.clear()
    return collected, garbage_before


def _emergency_self_flush():
    _aggressive_gc()
    if sys.platform == "win32":
        for _ in range(2):
            _trim_pid_windows(os.getpid())
    elif sys.platform == "linux":
        _trim_memory_linux()


def _is_process_alive(pid: int) -> bool:
    try:
        proc = psutil.Process(pid)
        return proc.is_running() and proc.status() != psutil.STATUS_ZOMBIE
    except (psutil.NoSuchProcess, psutil.ZombieProcess):
        return False
    except (psutil.AccessDenied, Exception):
        return True


def run_memory_cleaner(parent_pid: int, interval_seconds: int = 30) -> None:
    gc.set_threshold(700, 10, 10)
    gc.enable()

    check_interval = max(5, interval_seconds)
    aggressive_interval = max(2, interval_seconds // 3)

    print(
        f"[MemoryCleaner] Started, parent_pid={parent_pid}, "
        f"target_total={_MEMORY_LIMIT_MB}MB, self_limit={_SELF_LIMIT_MB}MB, "
        f"interval={check_interval}s",
        flush=True,
    )

    last_clean_cycle_mono = 0.0
    consecutive_over_count = 0

    while True:
        try:
            time.sleep(check_interval)
            now_mono = time.monotonic()

            if not _is_process_alive(parent_pid):
                print("[MemoryCleaner] Parent gone, exiting", flush=True)
                break

            own_mb = _get_rss_mb(os.getpid())
            if own_mb > _SELF_LIMIT_MB:
                print(
                    f"[MemoryCleaner] SELF OVER: {own_mb:.1f}MB > {_SELF_LIMIT_MB}MB",
                    flush=True,
                )
                _emergency_self_flush()
                time.sleep(0.15)
                _emergency_self_flush()

            webview_pids = _get_all_webview_pids(parent_pid)

            parent_mb = _get_rss_mb(parent_pid)

            wv_total_mb = 0.0
            per_process = []
            for pid in webview_pids:
                try:
                    mb = _get_rss_mb(pid)
                    wv_total_mb += mb
                    per_process.append((pid, mb))
                except Exception:
                    pass

            total_mb = parent_mb + wv_total_mb

            top = None
            if per_process:
                per_process.sort(key=lambda x: x[1], reverse=True)
                top = per_process[0]

            if now_mono - last_clean_cycle_mono >= _CLEAN_CYCLE_S or total_mb > _MEMORY_LIMIT_MB:
                last_clean_cycle_mono = now_mono
                collected, garbage = _aggressive_gc()
                trimmed = _force_trim_pids(webview_pids)
                _trim_pid_windows(parent_pid)
                if sys.platform == "linux":
                    _trim_memory_linux()
                if collected > 0 or garbage > 0 or trimmed > 0:
                    top_str = f"top=pid={top[0]}({top[1]:.0f}MB)" if top else ""
                    print(
                        f"[MemoryCleaner] Cycle: collected={collected}, garbage={garbage}, "
                        f"parent={parent_mb:.0f}MB, wv_count={len(webview_pids)}, "
                        f"wv_total={wv_total_mb:.0f}MB, grand_total={total_mb:.0f}MB, "
                        f"{top_str}, trimmed={trimmed}",
                        flush=True,
                    )

            if total_mb > _MEMORY_LIMIT_MB:
                consecutive_over_count += 1
                burst = min(consecutive_over_count * 2, 8)
                print(
                    f"[MemoryCleaner] TOTAL OVER: {total_mb:.0f}MB > {_MEMORY_LIMIT_MB}MB, "
                    f"burst={burst}, consecutive=#{consecutive_over_count}",
                    flush=True,
                )
                for _ in range(burst):
                    _aggressive_gc()
                    _force_trim_pids(webview_pids)
                    _trim_pid_windows(parent_pid)
                    time.sleep(0.08)

                webview_pids = _get_all_webview_pids(parent_pid)
                wv_total_mb = sum(_get_rss_mb(pid) for pid in webview_pids)
                parent_mb = _get_rss_mb(parent_pid)
                total_mb = parent_mb + wv_total_mb

                if total_mb > _MEMORY_LIMIT_MB:
                    print(
                        f"[MemoryCleaner] SUSTAINED: {total_mb:.0f}MB, entering suppression loop...",
                        flush=True,
                    )
                    suppress_rounds = 0
                    while total_mb > _MEMORY_LIMIT_MB:
                        time.sleep(aggressive_interval)
                        _aggressive_gc()
                        _force_trim_pids(webview_pids)
                        _trim_pid_windows(parent_pid)
                        webview_pids = _get_all_webview_pids(parent_pid)
                        if not webview_pids and _get_rss_mb(parent_pid) <= _PARENT_LIMIT_MB:
                            break
                        wv_total_mb = sum(_get_rss_mb(pid) for pid in webview_pids)
                        parent_mb = _get_rss_mb(parent_pid)
                        total_mb = parent_mb + wv_total_mb
                        suppress_rounds += 1
                        if suppress_rounds % 20 == 0:
                            per = []
                            for pid in webview_pids:
                                try:
                                    per.append((pid, _get_rss_mb(pid)))
                                except Exception:
                                    pass
                            per.sort(key=lambda x: x[1], reverse=True)
                            parts = ", ".join(
                                f"pid={p}({m:.0f}MB)" for p, m in per[:5]
                            )
                            print(
                                f"[MemoryCleaner] Suppress #{suppress_rounds}: "
                                f"total={total_mb:.0f}MB, [{parts}]",
                                flush=True,
                            )
                    print(
                        f"[MemoryCleaner] Back under limit: {total_mb:.0f}MB after {suppress_rounds} rounds",
                        flush=True,
                    )
                    consecutive_over_count = 0
            else:
                if consecutive_over_count > 0:
                    consecutive_over_count = max(0, consecutive_over_count - 1)

        except Exception:
            print(f"[MemoryCleaner] Error: {traceback.format_exc()}", flush=True)
            try:
                _emergency_self_flush()
            except Exception:
                pass
            time.sleep(1)


if __name__ == "__main__":
    parent_pid_str = os.environ.get("LUMINALIUM_PARENT_PID", "")
    interval_str = os.environ.get("LUMINALIUM_MEMCLEAN_INTERVAL", "30")
    if not parent_pid_str:
        print("[MemoryCleaner] No LUMINALIUM_PARENT_PID set, exiting", flush=True)
        sys.exit(1)
    try:
        parent_pid = int(parent_pid_str)
        interval = int(interval_str)
    except ValueError:
        print("[MemoryCleaner] Invalid PID or interval", flush=True)
        sys.exit(1)
    run_memory_cleaner(parent_pid, interval)
