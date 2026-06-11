import os
import sys
import json
import platform


def _get_root_dir():
    root_dir = os.path.dirname(
        os.path.dirname(
            os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        )
    )
    if getattr(sys, "frozen", False):
        root_dir = os.path.dirname(sys.executable)
    return root_dir


def _get_package_root():
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return sys._MEIPASS
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return _get_root_dir()


def _get_os_version():
    try:
        info = platform.uname()
        return f"{info.system} {info.release} ({info.version})"
    except Exception:
        return platform.platform()


def _get_device_model():
    try:
        vendor = platform.processor() or platform.machine()
    except Exception:
        vendor = platform.machine() or "Unknown"
    return vendor


def _get_device_vendor():
    vendor = "Unknown"
    if sys.platform == "win32":
        import subprocess
        try:
            output = subprocess.check_output(
                ["wmic", "csproduct", "get", "vendor"],
                shell=True,
                timeout=5,
            ).decode("utf-8", errors="ignore")
            lines = [l.strip() for l in output.splitlines() if l.strip() and l.strip() != "Vendor"]
            if lines:
                vendor = lines[0]
        except Exception:
            try:
                import winreg
                with winreg.OpenKey(
                    winreg.HKEY_LOCAL_MACHINE,
                    r"HARDWARE\DESCRIPTION\System\BIOS",
                ) as key:
                    vendor = winreg.QueryValueEx(key, "SystemManufacturer")[0] or vendor
            except Exception:
                pass
    elif sys.platform == "linux":
        try:
            with open("/sys/devices/virtual/dmi/id/sys_vendor", "r") as f:
                v = f.read().strip()
                if v:
                    vendor = v
        except Exception:
            pass
    return vendor


def _get_app_version():
    root_dir = _get_root_dir()
    version_path = os.path.join(root_dir, "version.json")
    if not os.path.exists(version_path):
        version_path = os.path.join(_get_package_root(), "version.json")
    try:
        if os.path.exists(version_path):
            with open(version_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            return data.get("version", "Unknown")
    except Exception:
        pass
    return "Unknown"


def _get_cpu_info():
    cpu = platform.processor() or ""
    if cpu and cpu != "Unknown":
        return cpu
    if sys.platform == "win32":
        import subprocess
        try:
            output = subprocess.check_output(
                ["wmic", "cpu", "get", "name"],
                shell=True,
                timeout=5,
            ).decode("utf-8", errors="ignore")
            lines = [l.strip() for l in output.splitlines() if l.strip() and l.strip() != "Name"]
            if lines:
                return lines[0]
        except Exception:
            pass
    elif sys.platform == "linux":
        try:
            with open("/proc/cpuinfo", "r") as f:
                for line in f:
                    if line.startswith("model name"):
                        return line.split(":", 1)[1].strip()
        except Exception:
            pass
    try:
        return platform.machine() or "Unknown"
    except Exception:
        return "Unknown"


def _get_gpu_info():
    if sys.platform == "win32":
        import subprocess
        try:
            output = subprocess.check_output(
                ["wmic", "path", "win32_videocontroller", "get", "name"],
                shell=True,
                timeout=5,
            ).decode("utf-8", errors="ignore")
            lines = [l.strip() for l in output.splitlines() if l.strip() and l.strip() != "Name"]
            if lines:
                return ", ".join(lines)
        except Exception:
            pass
    elif sys.platform == "linux":
        import subprocess
        try:
            output = subprocess.check_output(
                ["lspci", "-mm", "-nn"],
                timeout=5,
            ).decode("utf-8", errors="ignore")
            gpus = []
            for line in output.splitlines():
                if "VGA" in line or "3D" in line or "Display" in line:
                    parts = line.strip().split('"')
                    names = [p for p in parts if p.strip() and not p.strip().startswith("[")]
                    if len(names) >= 3:
                        gpus.append(names[-1])
            if gpus:
                return ", ".join(gpus)
        except Exception:
            pass
    return "Unknown"


def _get_ram_info():
    try:
        import psutil
        total = psutil.virtual_memory().total
        gb = total / (1024 ** 3)
        return f"{gb:.1f} GB"
    except Exception:
        pass
    if sys.platform == "win32":
        import subprocess
        try:
            output = subprocess.check_output(
                ["wmic", "memorychip", "get", "capacity"],
                shell=True,
                timeout=5,
            ).decode("utf-8", errors="ignore")
            total = 0
            for line in output.splitlines():
                line = line.strip()
                if line.isdigit():
                    total += int(line)
            if total > 0:
                gb = total / (1024 ** 3)
                return f"{gb:.1f} GB"
        except Exception:
            pass
    elif sys.platform == "linux":
        try:
            with open("/proc/meminfo", "r") as f:
                for line in f:
                    if line.startswith("MemTotal"):
                        kb = int(line.split(":")[1].strip().split()[0])
                        gb = kb / (1024 ** 2)
                        return f"{gb:.1f} GB"
        except Exception:
            pass
    return "Unknown"


def _get_memory_kill_count():
    root_dir = _get_root_dir()
    counter_path = os.path.join(root_dir, ".memory_kill_count")
    try:
        if os.path.exists(counter_path):
            with open(counter_path, "r") as f:
                return int(f.read().strip())
    except Exception:
        pass
    return 0


def increment_memory_kill_count():
    root_dir = _get_root_dir()
    counter_path = os.path.join(root_dir, ".memory_kill_count")
    try:
        current = _get_memory_kill_count()
        with open(counter_path, "w") as f:
            f.write(str(current + 1))
    except Exception:
        pass


def collect_diagnostic_info():
    return {
        "OSType": sys.platform,
        "OSVersion": _get_os_version(),
        "DeviceModel": _get_device_model(),
        "DeviceVendor": _get_device_vendor(),
        "CPU": _get_cpu_info(),
        "GPU": _get_gpu_info(),
        "RAM": _get_ram_info(),
        "AppPackageRoot": _get_package_root(),
        "AppRoot": _get_root_dir(),
        "CurrentRunningDirectory": os.getcwd(),
        "AppVersion": _get_app_version(),
        "ByMemoryKillCount": _get_memory_kill_count(),
    }