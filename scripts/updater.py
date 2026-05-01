import os
import sys
import time
import shutil
import zipfile
import argparse
import traceback
import ctypes
import subprocess
from pathlib import Path
import ctypes.wintypes

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

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--zip", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--app-dir", required=True)
    parser.add_argument("--cache-dir", required=True)
    parser.add_argument("--parent-pid", type=int, default=0)
    args = parser.parse_args()
    
    app_dir = Path(args.app_dir)
    cache_dir = Path(args.cache_dir)
    zip_path = Path(args.zip)
    version = args.version
    
    bak_dir = app_dir / ".update_bak"
    log_file = app_dir / "update_error.log"
    unpack_dir = cache_dir / "unpack"
    
    try:
        # 1. Ensure main process is gone. If still alive, force kill and re-check.
        if args.parent_pid:
            if not wait_for_process_exit(args.parent_pid, timeout=60):
                force_kill_process_tree(args.parent_pid)
                if not wait_for_process_exit(args.parent_pid, timeout=15):
                    raise Exception("Timeout waiting for Luminalium process to exit.")
        elif not wait_for_mutex("Global\\Luminalium_Mutex", timeout=60):
            raise Exception("Timeout waiting for Luminalium to exit.")
            
        # 2. Backup
        backup_app(app_dir, bak_dir)
        
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
                    
        # 9. Write .version
        if not internal_dir.exists():
            internal_dir.mkdir(parents=True)
        with open(internal_dir / ".version", "w", encoding="utf-8") as vf:
            vf.write(version)
            
        # 10. Start new Luminalium.exe
        if exe_path.exists():
            ctypes.windll.shell32.ShellExecuteW(None, "open", str(exe_path), None, str(app_dir), 1)
            
    except Exception as e:
        err_msg = traceback.format_exc()
        with open(log_file, "a", encoding="utf-8") as f:
            f.write(f"[{time.ctime()}] Update failed: {err_msg}\n")
        restore_backup(app_dir, bak_dir)
        notify_failure("更新失败，已自动回滚。请查看日志。")
        
        # Try to restart old exe
        old_exe = app_dir / "Luminalium.exe"
        if old_exe.exists():
            ctypes.windll.shell32.ShellExecuteW(None, "open", str(old_exe), None, str(app_dir), 1)
            
    finally:
        # Cleanup unpack dir
        if unpack_dir.exists():
            shutil.rmtree(unpack_dir, ignore_errors=True)
        sys.exit(0)

if __name__ == "__main__":
    main()
