import os
import sys
import subprocess
from datetime import datetime

from PyQt6.QtWidgets import QApplication

from crash_handler import CrashInfo, CrashWindow, CrashHandler, _default_log_path


def log_watchdog(msg):
    try:
        log_path = _default_log_path()
        with open(log_path, "a", encoding="utf-8") as f:
            f.write(f"[WATCHDOG] {datetime.now()}: {msg}\n")
    except Exception:
        pass


def start_main_process():
    base_dir = getattr(sys, "_MEIPASS", os.path.dirname(os.path.abspath(sys.argv[0])))
    main_path = os.path.join(base_dir, "main.py")
    if not os.path.exists(main_path):
        log_watchdog(f"main.py not found in {base_dir}")
        return None
    cmd = [sys.executable, main_path]
    try:
        proc = subprocess.Popen(cmd, cwd=base_dir, close_fds=True)
        log_watchdog(f"Started main process pid={proc.pid}")
        return proc
    except Exception as e:
        log_watchdog(f"Failed to start main process: {e}")
        return None


def show_crash_window(exit_code):
    try:
        log_path = _default_log_path()
        if os.path.exists(log_path):
            with open(log_path, "r", encoding="utf-8") as f:
                details = f.read()
        else:
            details = f"Process exited with code {exit_code}"
    except Exception:
        details = f"Process exited with code {exit_code}"
    crash = CrashInfo(title="Kazuha 崩溃啦！", details=details)
    app = QApplication.instance()
    if app is None:
        app = QApplication(sys.argv[:1])
    win = CrashWindow(crash, parent=None)
    handler = CrashHandler(log_path=_default_log_path())

    def do_exit():
        try:
            app.quit()
        finally:
            os._exit(1)

    def do_copy():
        clipboard = app.clipboard()
        if clipboard is not None:
            clipboard.setText(win.details_text())

    def do_restart():
        handler.restart()

    win.exit_btn.clicked.connect(do_exit)
    win.copy_btn.clicked.connect(do_copy)
    win.restart_btn.clicked.connect(do_restart)
    win.exec()


def main():
    log_watchdog("Watchdog started")
    proc = start_main_process()
    if proc is None:
        return
    code = proc.wait()
    log_watchdog(f"Main process exited with code {code}")
    if code != 0:
        show_crash_window(code)


if __name__ == "__main__":
    main()

