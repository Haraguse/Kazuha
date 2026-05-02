import os
import sys
import time
import shutil
import zipfile
import argparse
import traceback
import ctypes
import subprocess
import threading
from pathlib import Path
import ctypes.wintypes

# --- GUI Dependencies ---
from PySide6.QtCore import Qt, QTimer, Signal, QObject
from PySide6.QtGui import QIcon, QFont
from PySide6.QtWidgets import QApplication, QVBoxLayout, QHBoxLayout, QLabel, QWidget
from qfluentwidgets import IndeterminateProgressRing, ProgressBar, setTheme, Theme

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

# Mutex handling
def wait_for_mutex(mutex_name, timeout=30):
    kernel32 = ctypes.windll.kernel32
    SYNCHRONIZE = 0x00100000
    start = time.time()
    while time.time() - start < timeout:
        # Try to open the mutex
        hMutex = kernel32.OpenMutexW(SYNCHRONIZE, False, mutex_name)
        if not hMutex:
            # Mutex does not exist, main process has exited
            return True
        else:
            kernel32.CloseHandle(hMutex)
        time.sleep(1)
    return False


def wait_for_process_exit(pid: int, timeout=30):
    if not pid or pid <= 0:
        return False
    kernel32 = ctypes.windll.kernel32
    SYNCHRONIZE = 0x00100000
    WAIT_OBJECT_0 = 0x00000000
    WAIT_TIMEOUT = 0x00000102
    handle = kernel32.OpenProcess(SYNCHRONIZE, False, int(pid))
    if not handle:
        # Process not found, treat as already exited.
        return True
    try:
        result = kernel32.WaitForSingleObject(handle, int(timeout * 1000))
        if result == WAIT_OBJECT_0:
            return True
        if result == WAIT_TIMEOUT:
            return False
        return False
    finally:
        kernel32.CloseHandle(handle)


def force_kill_process_tree(pid: int):
    if not pid or pid <= 0:
        return
    try:
        # /T kills child process tree, /F forces termination.
        subprocess.run(
            ["taskkill", "/PID", str(pid), "/T", "/F"],
            check=False,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            creationflags=0x08000000,
        )
    except Exception:
        pass

def backup_app(app_dir, bak_dir):
    if bak_dir.exists():
        shutil.rmtree(bak_dir, ignore_errors=True)
    bak_dir.mkdir(parents=True)
    
    # Backup executable
    exe_path = app_dir / "Luminalium.exe"
    if exe_path.exists():
        shutil.copy2(exe_path, bak_dir / "Luminalium.exe")
        
    # Backup _internal
    internal_dir = app_dir / "_internal"
    if internal_dir.exists():
        shutil.copytree(internal_dir, bak_dir / "_internal", dirs_exist_ok=True)

def restore_backup(app_dir, bak_dir):
    if not bak_dir.exists():
        return
    try:
        # Restore executable
        bak_exe = bak_dir / "Luminalium.exe"
        if bak_exe.exists():
            shutil.copy2(bak_exe, app_dir / "Luminalium.exe")
            
        # Restore _internal
        bak_internal = bak_dir / "_internal"
        if bak_internal.exists():
            shutil.copytree(bak_internal, app_dir / "_internal", dirs_exist_ok=True)
    except Exception as e:
        pass

def notify_failure(msg):
    try:
        from winrt.windows.ui.notifications import ToastNotificationManager, ToastNotification
        from winrt.windows.data.xml.dom import XmlDocument
        xml = XmlDocument()
        xml.load_xml(f"""
        <toast>
            <visual>
                <binding template="ToastGeneric">
                    <text>更新失败</text>
                    <text>{msg}</text>
                </binding>
            </visual>
        </toast>
        """)
        notifier = ToastNotificationManager.create_toast_notifier("Luminalium")
        notifier.show(ToastNotification(xml))
    except:
        pass

class WorkerSignals(QObject):
    progress = Signal(int)
    text = Signal(str)
    finished = Signal(bool, str) # success, msg

class UpdaterWindow(QWidget):
    def __init__(self, args):
        super().__init__()
        self.args = args
        self.setWindowTitle("Luminalium 更新程序")
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool)
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.resize(360, 160)
        self._setup_ui()
        self._center()
        
        self.signals = WorkerSignals()
        self.signals.progress.connect(self._on_progress)
        self.signals.text.connect(self._on_text)
        self.signals.finished.connect(self._on_finished)
        
        setTheme(Theme.AUTO)
        
        # Start logic after UI is shown
        QTimer.singleShot(500, self._start_worker)

    def _setup_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)
        layout.setSpacing(16)
        
        # Make a card background
        self.card = QWidget(self)
        self.card.setObjectName("card")
        self.card.setStyleSheet("""
            #card {
                background: var(--bg-color);
                border: 1px solid var(--border-color);
                border-radius: 8px;
            }
        """)
        
        card_layout = QVBoxLayout(self.card)
        card_layout.setContentsMargins(20, 20, 20, 20)
        card_layout.setSpacing(12)
        
        # Title and Ring
        top_layout = QHBoxLayout()
        self.ring = IndeterminateProgressRing(self)
        self.ring.setFixedSize(24, 24)
        top_layout.addWidget(self.ring)
        
        self.title_label = QLabel("请稍后 Luminalium 正在部署更新...", self)
        font = self.title_label.font()
        font.setPixelSize(14)
        font.setBold(True)
        self.title_label.setFont(font)
        top_layout.addWidget(self.title_label)
        top_layout.addStretch(1)
        
        card_layout.addLayout(top_layout)
        
        # Status text
        self.status_label = QLabel("准备就绪...", self)
        self.status_label.setStyleSheet("color: gray;")
        card_layout.addWidget(self.status_label)
        
        # Progress Bar
        self.progress_bar = ProgressBar(self)
        self.progress_bar.setRange(0, 100)
        self.progress_bar.setValue(0)
        card_layout.addWidget(self.progress_bar)
        
        layout.addWidget(self.card)
        
        # Basic dynamic colors (very simple fallback if qfluentwidgets theme takes over)
        self.setStyleSheet("""
            QWidget {
                --bg-color: #ffffff;
                --border-color: #e5e5e5;
            }
            @media (prefers-color-scheme: dark) {
                QWidget {
                    --bg-color: #2b2b2b;
                    --border-color: #3d3d3d;
                    color: white;
                }
            }
        """)

    def _center(self):
        screen = QApplication.primaryScreen().geometry()
        size = self.geometry()
        self.move(
            (screen.width() - size.width()) // 2,
            (screen.height() - size.height()) // 2,
        )

    def _on_progress(self, val):
        self.progress_bar.setValue(val)

    def _on_text(self, text):
        self.status_label.setText(text)

    def _on_finished(self, success, msg):
        if not success:
            self.title_label.setText("更新失败")
            self.ring.stop()
            self.ring.hide()
            self.progress_bar.setStyleSheet("QProgressBar::chunk { background-color: #ff4d4f; }")
            QTimer.singleShot(3000, QApplication.quit)
        else:
            QApplication.quit()

    def _start_worker(self):
        t = threading.Thread(target=self._run_update_logic, daemon=True)
        t.start()

    def _run_update_logic(self):
        args = self.args
        app_dir = Path(args.app_dir)
        cache_dir = Path(args.cache_dir)
        zip_path = Path(args.zip)
        version = args.version
        
        bak_dir = app_dir / ".update_bak"
        log_file = app_dir / "update_error.log"
        unpack_dir = cache_dir / "unpack"
        
        try:
            self.signals.text.emit("正在终止正在运行的实例...")
            self.signals.progress.emit(5)
            # 1. Ensure main process is gone. If still alive, force kill and re-check.
            if args.parent_pid:
                force_kill_process_tree(args.parent_pid)
                if not wait_for_process_exit(args.parent_pid, timeout=5):
                    raise Exception("Timeout waiting for Luminalium process to exit.")
            
            # Also kill any other Luminalium.exe just in case
            try:
                subprocess.run(
                    ["taskkill", "/IM", "Luminalium.exe", "/T", "/F"],
                    check=False,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    creationflags=0x08000000,
                )
            except Exception:
                pass
            
            # Additional small wait
            time.sleep(1)
            
            self.signals.text.emit("备份当前版本...")
            self.signals.progress.emit(15)
            # 2. Backup
            backup_app(app_dir, bak_dir)
            
            self.signals.text.emit("解压更新包...")
            self.signals.progress.emit(30)
            # 3. Clean unpack dir
            if unpack_dir.exists():
                shutil.rmtree(unpack_dir, ignore_errors=True)
            unpack_dir.mkdir(parents=True)
            
            # 4. Extract zip
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(unpack_dir)
                
            # If the zip contains a single folder (e.g. Luminalium-windows), adjust unpack_dir
            extracted_items = list(unpack_dir.iterdir())
            if len(extracted_items) == 1 and extracted_items[0].is_dir():
                actual_unpack = extracted_items[0]
            else:
                actual_unpack = unpack_dir
                
            self.signals.text.emit("清理旧文件...")
            self.signals.progress.emit(50)
            # 5. Delete old Luminalium.exe
            exe_path = app_dir / "Luminalium.exe"
            if exe_path.exists():
                exe_path.unlink()
                
            # 6. Delete _internal except user dir
            internal_dir = app_dir / "_internal"
            if internal_dir.exists():
                for item in internal_dir.iterdir():
                    if item.name == "user":
                        continue
                    if item.is_dir():
                        shutil.rmtree(item, ignore_errors=True)
                    else:
                        item.unlink()
                        
            self.signals.text.emit("应用新文件...")
            self.signals.progress.emit(70)
            # 7. Copy new Luminalium.exe
            new_exe = actual_unpack / "Luminalium.exe"
            if new_exe.exists():
                shutil.copy2(new_exe, exe_path)
                
            # 8. Merge _internal
            new_internal = actual_unpack / "_internal"
            if new_internal.exists():
                for root, dirs, files in os.walk(new_internal):
                    rel_path = Path(root).relative_to(new_internal)
                    target_root = internal_dir / rel_path
                    target_root.mkdir(parents=True, exist_ok=True)
                    
                    # Check if this path is under 'user'
                    if rel_path.parts and rel_path.parts[0] == "user":
                        continue # Skip overwriting user data
                        
                    for f in files:
                        src_f = Path(root) / f
                        dst_f = target_root / f
                        if dst_f.exists() and "user" in rel_path.parts:
                            continue
                        shutil.copy2(src_f, dst_f)
                        
            self.signals.text.emit("完成配置...")
            self.signals.progress.emit(90)
            # 9. Write .version
            if not internal_dir.exists():
                internal_dir.mkdir(parents=True)
            with open(internal_dir / ".version", "w", encoding="utf-8") as vf:
                vf.write(version)
                
            self.signals.progress.emit(100)
            self.signals.text.emit("正在启动新版本...")
            
            # 10. Start new Luminalium.exe
            if exe_path.exists():
                # We are admin, but we want to start app as normal user if possible
                # But typically ShellExecute as admin will launch it as admin.
                # For simplicity, we just launch it.
                ctypes.windll.shell32.ShellExecuteW(None, "open", str(exe_path), None, str(app_dir), 1)
                
            self.signals.finished.emit(True, "")
                
        except Exception as e:
            err_msg = traceback.format_exc()
            with open(log_file, "a", encoding="utf-8") as f:
                f.write(f"[{time.ctime()}] Update failed: {err_msg}\n")
            restore_backup(app_dir, bak_dir)
            notify_failure("更新失败，已自动回滚。请查看日志。")
            self.signals.text.emit("更新失败，已回滚")
            
            # Try to restart old exe
            old_exe = app_dir / "Luminalium.exe"
            if old_exe.exists():
                ctypes.windll.shell32.ShellExecuteW(None, "open", str(old_exe), None, str(app_dir), 1)
                
            self.signals.finished.emit(False, str(e))
                
        finally:
            # Cleanup unpack dir
            if unpack_dir.exists():
                shutil.rmtree(unpack_dir, ignore_errors=True)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--zip", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--app-dir", required=True)
    parser.add_argument("--cache-dir", required=True)
    parser.add_argument("--parent-pid", type=int, default=0)
    args = parser.parse_args()
    
    # Request Admin privileges if not running as admin
    if not is_admin():
        # Re-run the program with admin rights
        # Join arguments into a single string for ShellExecuteW
        # Avoid passing the first element (the script/exe name)
        params = " ".join([f'"{arg}"' for arg in sys.argv[1:]])
        try:
            # 1: SW_SHOWNORMAL
            ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, params, None, 1)
        except Exception as e:
            pass
        sys.exit(0)

    # We are admin, show GUI and run logic
    app = QApplication(sys.argv)
    window = UpdaterWindow(args)
    window.show()
    sys.exit(app.exec())

if __name__ == "__main__":
    main()
