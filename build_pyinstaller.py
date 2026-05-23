import os
import sys
import shutil
import subprocess


def _remove_pdb_files(root_dir):
    for dirpath, _, filenames in os.walk(root_dir):
        for name in filenames:
            if name.lower().endswith(".pdb"):
                try:
                    os.remove(os.path.join(dirpath, name))
                except OSError:
                    pass


def _prune_qt_translations(root_dir):
    keep_suffixes = ("_zh_cn.qm", "_en.qm", "_en_us.qm")
    for dirpath, _, filenames in os.walk(root_dir):
        if os.path.basename(dirpath).lower() == "translations":
            for name in filenames:
                lower = name.lower()
                if lower.endswith(".qm") and not lower.endswith(keep_suffixes):
                    try:
                        os.remove(os.path.join(dirpath, name))
                    except OSError:
                        pass


def _prune_qtwebengine_locales(root_dir):
    keep_locales = {"zh-CN.pak", "en-US.pak"}
    for dirpath, _, filenames in os.walk(root_dir):
        if os.path.basename(dirpath) == "qtwebengine_locales":
            for name in filenames:
                if name not in keep_locales:
                    try:
                        os.remove(os.path.join(dirpath, name))
                    except OSError:
                        pass


def run():
    root_dir = os.path.dirname(os.path.abspath(__file__))
    dist_dir = os.path.join(root_dir, "dist")
    build_dir = os.path.join(root_dir, "build")

    python_exe = sys.executable
    if not os.path.exists(python_exe):
        # Fallback for some reason if sys.executable is weird, though unlikely
        venv_python = os.path.join(root_dir, ".venv", "Scripts", "python.exe")
        if os.path.exists(venv_python):
            python_exe = venv_python
        else:
             # Just try 'python' from PATH
            python_exe = "python"

    icons_dir = os.path.join(root_dir, "icons")
    logo_ico = os.path.join(icons_dir, "logo.ico")

    data_sep = os.pathsep
    data_entries = [
        ("version.json", "."),
        ("config", "config"),
        ("plugins", "plugins"),
        ("icons", "icons"),
        ("ppt_assistant", "ppt_assistant"),
        ("fonts", "fonts"),
        ("user", "user"),
        ("scripts", "scripts"),
        ("Luminalium2WPS/protocol", "Luminalium2WPS/protocol"),
    ]
    add_data = []
    for src, dst in data_entries:
        if os.path.exists(os.path.join(root_dir, src)):
            add_data.append(f"{src}{data_sep}{dst}")

    cmd = [
        python_exe,
        "-m",
        "PyInstaller",
        "--noconfirm",
        "--clean",
        "--exclude-module",
        "PyQt5",
        "--exclude-module",
        "PyQt5-Frameless-Window",
        "--exclude-module",
        "setuptools",
        "--exclude-module",
        "pkg_resources",
        "--hidden-import",
        "PySide6.QtXml",
        "--onedir",
        "--windowed",
        "--name",
        "Luminalium",
    ]
    if os.path.exists(logo_ico):
        cmd += ["--icon", logo_ico]
    for entry in add_data:
        cmd += ["--add-data", entry]
    cmd += ["main.py"]

    if os.path.isdir(dist_dir):
        shutil.rmtree(dist_dir, ignore_errors=True)
    if os.path.isdir(build_dir):
        shutil.rmtree(build_dir, ignore_errors=True)
    for extra in [
        "main.build",
        "main.dist",
        "Luminalium.build",
        "Luminalium.dist",
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.isdir(extra_path):
            shutil.rmtree(extra_path, ignore_errors=True)
    subprocess.check_call(cmd, cwd=root_dir)

    # Build updater.exe into the main app output folder so shipping is one-step.
    updater_py = os.path.join(root_dir, "scripts", "updater.py")
    if os.path.exists(updater_py):
        updater_cmd = [
            python_exe,
            "-m",
            "PyInstaller",
            "--noconfirm",
            "--clean",
            "--onefile",
            "--windowed",  # Now has GUI, avoid console
            "--hidden-import", "PySide6.QtCore",
            "--hidden-import", "PySide6.QtGui",
            "--hidden-import", "PySide6.QtWidgets",
            "--hidden-import", "qfluentwidgets",
            "--exclude-module", "PyQt5",
            "--exclude-module", "PyQt5-Frameless-Window",
            "--exclude-module", "PyQt6",
            "--name",
            "updater",
            "--distpath",
            os.path.join(dist_dir, "Luminalium"),
            "--workpath",
            os.path.join(build_dir, "updater"),
            "--specpath",
            os.path.join(build_dir, "updater"),
            updater_py,
        ]
        subprocess.check_call(updater_cmd, cwd=root_dir)

    output_dir = os.path.join(dist_dir, "Luminalium")
    if os.path.isdir(output_dir):
        _remove_pdb_files(output_dir)
        _prune_qt_translations(output_dir)
        _prune_qtwebengine_locales(output_dir)

    if os.path.isdir(build_dir):
        shutil.rmtree(build_dir, ignore_errors=True)
    for extra in [
        "main.build",
        "main.dist",
        "Luminalium.build",
        "Luminalium.dist",
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.isdir(extra_path):
            shutil.rmtree(extra_path, ignore_errors=True)
    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        pyinstaller_cache = os.path.join(local_app_data, "pyinstaller")
        if os.path.isdir(pyinstaller_cache):
            shutil.rmtree(pyinstaller_cache, ignore_errors=True)


if __name__ == "__main__":
    run()
