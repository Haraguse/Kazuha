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
from PySide6.QtGui import QIcon, QFont, QColor
from PySide6.QtWidgets import QApplication, QVBoxLayout, QHBoxLayout, QLabel, QWidget, QGraphicsDropShadowEffect
from qfluentwidgets import (
    IndeterminateProgressRing,
    ProgressBar,
    setTheme,
    Theme,
    CardWidget,
    SubtitleLabel,
    BodyLabel,
    isDarkTheme,
)

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
        # 移除透明背景，设为标准的不可关闭对话框样式
        self.setWindowFlags(Qt.Window | Qt.CustomizeWindowHint | Qt.WindowTitleHint | Qt.WindowStaysOnTopHint)
        self.resize(540, 240)
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
        # 整体采用白色/深色实心背景
        self.setObjectName("updaterWindow")
        self.setStyleSheet("""
            #updaterWindow {
                background: #ffffff;
            }
            @media (prefers-color-scheme: dark) {
                #updaterWindow {
                    background: #2b2b2b;
                }
            }
        """)

        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)

        # 顶部主内容区
        content_widget = QWidget(self)
        content_layout = QVBoxLayout(content_widget)
        content_layout.setContentsMargins(32, 32, 32, 32)
        content_layout.setSpacing(16)

        # 标题：请稍候
        title_label = QLabel("请稍候", self)
        font = title_label.font()
        font.setPixelSize(22)
        font.setBold(True)
        title_label.setFont(font)
        content_layout.addWidget(title_label)

        # 副标题
        subtitle_label = BodyLabel("Luminalium 正在部署更新。\n此操作可能需要几分钟，恭请您坐和放宽。", self)
        subtitle_label.setWordWrap(True)
        font_sub = subtitle_label.font()
        font_sub.setPixelSize(14)
        subtitle_label.setFont(font_sub)
        content_layout.addWidget(subtitle_label)

        content_layout.addSpacing(20)

        # 进度条与百分比
        progress_layout = QHBoxLayout()
        self.progress_bar = ProgressBar(self)
        self.progress_bar.setRange(0, 100)
        self.progress_bar.setValue(0)
        self.progress_bar.setFixedHeight(4)
        progress_layout.addWidget(self.progress_bar, 1)

        self.percent_label = BodyLabel("0%", self)
        self.percent_label.setFixedWidth(40)
        self.percent_label.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        progress_layout.addWidget(self.percent_label)

        content_layout.addLayout(progress_layout)
        content_layout.addStretch(1)

        main_layout.addWidget(content_widget, 1)

        # 底部状态栏（灰色背景）
        footer_widget = QWidget(self)
        footer_widget.setObjectName("footerWidget")
        footer_widget.setStyleSheet("""
            #footerWidget {
                background: #f3f3f3;
                border-top: 1px solid #e5e5e5;
            }
            @media (prefers-color-scheme: dark) {
                #footerWidget {
                    background: #202020;
                    border-top: 1px solid #1a1a1a;
                }
            }
        """)
        footer_layout = QVBoxLayout(footer_widget)
        footer_layout.setContentsMargins(32, 16, 32, 16)
        
        self.status_label = BodyLabel("准备就绪...", self)
        self.status_label.setWordWrap(False)
        self.status_label.setStyleSheet("color: var(--TextSecondaryColor);")
        # 缩略显示超长路径
        font_status = self.status_label.font()
        font_status.setPixelSize(12)
        self.status_label.setFont(font_status)
        footer_layout.addWidget(self.status_label)

        main_layout.addWidget(footer_widget, 0)

    def _center(self):
        screen = QApplication.primaryScreen().geometry()
        size = self.geometry()
        self.move(
            (screen.width() - size.width()) // 2,
            (screen.height() - size.height()) // 2,
        )

    def _on_progress(self, val):
        self.progress_bar.setValue(val)
        self.percent_label.setText(f"{val}%")

    def _on_text(self, text):
        self.status_label.setText(text)

    def _on_finished(self, success, msg):
        if not success:
            self.status_label.setText("更新失败: " + msg)
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
            self.signals.progress.emit(2)
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
            
            self.signals.text.emit("正在准备备份当前版本...")
            self.signals.progress.emit(5)
            # 2. Backup
            backup_app(app_dir, bak_dir)
            
            self.signals.text.emit("正在清理旧的解压缓存...")
            self.signals.progress.emit(10)
            # 3. Clean unpack dir
            if unpack_dir.exists():
                shutil.rmtree(unpack_dir, ignore_errors=True)
            unpack_dir.mkdir(parents=True)
            
            self.signals.text.emit("正在解压更新包...")
            self.signals.progress.emit(15)
            # 4. Extract zip
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                # Get total files to extract
                members = zip_ref.infolist()
                total_members = len(members)
                for i, member in enumerate(members):
                    zip_ref.extract(member, unpack_dir)
                    if i % 10 == 0:
                        # map 15% -> 40%
                        progress = 15 + int((i / total_members) * 25)
                        self.signals.progress.emit(progress)
                        self.signals.text.emit(f"解压: {member.filename}")
                
            # If the zip contains a single folder (e.g. Luminalium-windows), adjust unpack_dir
            extracted_items = list(unpack_dir.iterdir())
            if len(extracted_items) == 1 and extracted_items[0].is_dir():
                actual_unpack = extracted_items[0]
            else:
                actual_unpack = unpack_dir
                
            self.signals.text.emit("清理旧文件...")
            self.signals.progress.emit(40)
            # 5. Delete old Luminalium.exe
            exe_path = app_dir / "Luminalium.exe"
            if exe_path.exists():
                self.signals.text.emit("删除: Luminalium.exe")
                exe_path.unlink()
                
            # 6. Delete _internal except user dir
            internal_dir = app_dir / "_internal"
            if internal_dir.exists():
                # Count files to delete for progress
                files_to_delete = []
                for root, dirs, files in os.walk(internal_dir):
                    rel_path = Path(root).relative_to(internal_dir)
                    if rel_path.parts and rel_path.parts[0] == "user":
                        continue
                    for f in files:
                        files_to_delete.append(Path(root) / f)
                
                total_del = len(files_to_delete)
                for i, file_path in enumerate(files_to_delete):
                    try:
                        self.signals.text.emit(f"删除: {file_path.relative_to(app_dir)}")
                        file_path.unlink()
                    except Exception:
                        pass
                    if i % 10 == 0 and total_del > 0:
                        # map 40% -> 60%
                        progress = 40 + int((i / total_del) * 20)
                        self.signals.progress.emit(progress)
                
                # Cleanup empty dirs
                for item in internal_dir.iterdir():
                    if item.name == "user":
                        continue
                    if item.is_dir():
                        shutil.rmtree(item, ignore_errors=True)
                        
            self.signals.text.emit("应用新文件...")
            self.signals.progress.emit(60)
            # 7. Copy new Luminalium.exe
            new_exe = actual_unpack / "Luminalium.exe"
            if new_exe.exists():
                self.signals.text.emit("复制: Luminalium.exe")
                shutil.copy2(new_exe, exe_path)
                
            # 8. Merge _internal
            new_internal = actual_unpack / "_internal"
            if new_internal.exists():
                files_to_copy = []
                for root, dirs, files in os.walk(new_internal):
                    for f in files:
                        files_to_copy.append(Path(root) / f)
                
                total_copy = len(files_to_copy)
                for i, src_f in enumerate(files_to_copy):
                    rel_path = src_f.relative_to(new_internal)
                    target_root = internal_dir / rel_path.parent
                    target_root.mkdir(parents=True, exist_ok=True)
                    
                    if rel_path.parts and rel_path.parts[0] == "user":
                        continue # Skip overwriting user data
                        
                    dst_f = internal_dir / rel_path
                    if dst_f.exists() and "user" in rel_path.parts:
                        continue
                    
                    self.signals.text.emit(f"复制: _internal\\{rel_path}")
                    shutil.copy2(src_f, dst_f)
                    
                    if i % 10 == 0 and total_copy > 0:
                        # map 60% -> 90%
                        progress = 60 + int((i / total_copy) * 30)
                        self.signals.progress.emit(progress)
                        
            self.signals.text.emit("完成配置...")
            self.signals.progress.emit(95)
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
