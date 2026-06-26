import fnmatch
import os
import shutil
import subprocess
import sys


STATIC_PPT_ASSET_EXTENSIONS = {".html", ".qml", ".ogg"}
BASE_EXCLUDED_MODULES = [
    "PyQt5",
    "PyQt5-Frameless-Window",
    "PyQt6",
    "setuptools",
    "pkg_resources",
    # The desktop app does not use the experimental rendering package in production.
    "numpy",
    "scipy",
    "ppt_assistant.rendering",
]
UNUSED_QT_DLL_PATTERNS = [
    "Qt6Pdf*.dll",
    "Qt6Charts*.dll",
    "Qt6Graphs*.dll",
    "Qt6Location*.dll",
    "Qt6DataVisualization*.dll",
    "Qt63D*.dll",
    "Qt6Quick3D*.dll",
    "Qt6RemoteObjects*.dll",
]
UNUSED_QT_RESOURCE_PATTERNS = [
    "qtwebengine_devtools_resources*.pak",
    "*.debug.pak",
    "*.debug.bin",
]


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


def _collect_data_entries(root_dir):
    data_entries = [
        ("version.json", "."),
        ("config", "config"),
        ("plugins", "plugins"),
        ("icons", "icons"),
        ("fonts", "fonts"),
        ("user", "user"),
        ("ppt_assistant/assets", "ppt_assistant/assets"),
    ]
    ui_dir = os.path.join(root_dir, "ppt_assistant", "ui")
    if os.path.isdir(ui_dir):
        for name in os.listdir(ui_dir):
            src = os.path.join(ui_dir, name)
            if not os.path.isfile(src):
                continue
            if os.path.splitext(name)[1].lower() not in STATIC_PPT_ASSET_EXTENSIONS:
                continue
            data_entries.append((os.path.relpath(src, root_dir), f"ppt_assistant/ui"))
    return [
        (src, dst)
        for src, dst in data_entries
        if os.path.exists(os.path.join(root_dir, src))
    ]


def _remove_matching_files(root_dir, patterns):
    for dirpath, _, filenames in os.walk(root_dir):
        for name in filenames:
            if not any(fnmatch.fnmatch(name, pattern) for pattern in patterns):
                continue
            try:
                os.remove(os.path.join(dirpath, name))
            except OSError:
                pass


def _remove_matching_directories(root_dir, names):
    target_names = {name.lower() for name in names}
    for dirpath, dirnames, _ in os.walk(root_dir):
        for name in list(dirnames):
            if name.lower() not in target_names:
                continue
            shutil.rmtree(os.path.join(dirpath, name), ignore_errors=True)
            dirnames.remove(name)


def _prune_unused_qt_binaries(root_dir):
    pyside_dir = os.path.join(root_dir, "_internal", "PySide6")
    if not os.path.isdir(pyside_dir):
        return
    _remove_matching_files(pyside_dir, UNUSED_QT_DLL_PATTERNS)
    _remove_matching_files(
        os.path.join(pyside_dir, "resources"), UNUSED_QT_RESOURCE_PATTERNS
    )
    _remove_matching_directories(os.path.join(pyside_dir, "plugins"), {"qmltooling"})


def _prune_pyinstaller_metadata(root_dir):
    _remove_matching_directories(root_dir, {"__pycache__", "Pythonwin"})
    for dirpath, dirnames, _ in os.walk(root_dir):
        for name in list(dirnames):
            lower = name.lower()
            if not lower.endswith(".dist-info"):
                continue
            shutil.rmtree(os.path.join(dirpath, name), ignore_errors=True)
            dirnames.remove(name)


def _build_exclude_args():
    args = []
    for module_name in BASE_EXCLUDED_MODULES:
        args += ["--exclude-module", module_name]
    return args


def run():
    # __file__ points to scripts/build/build_pyinstaller.py; go up 2 levels to project root.
    root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
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
    data_entries = _collect_data_entries(root_dir)
    add_data = []
    for src, dst in data_entries:
        add_data.append(f"{src}{data_sep}{dst}")

    cmd = [
        python_exe,
        "-m",
        "PyInstaller",
        "--noconfirm",
        "--clean",
        "--optimize",
        "1",
        "--hidden-import",
        "PySide6.QtXml",
        "--hidden-import",
        "winrt.windows.data.xml.dom",
        "--hidden-import",
        "winrt.windows.ui.notifications",
        "--onedir",
        "--windowed",
        "--name",
        "Luminalium",
    ]
    cmd += _build_exclude_args()
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
        "Luminalium.spec",
        "updater.spec",
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.isdir(extra_path):
            shutil.rmtree(extra_path, ignore_errors=True)
        elif os.path.isfile(extra_path):
            try:
                os.remove(extra_path)
            except OSError:
                pass
    subprocess.check_call(cmd, cwd=root_dir)

    # Build updater.exe into the main app output folder so shipping is one-step.
    updater_py = os.path.join(root_dir, "scripts", "updater", "updater.py")
    if os.path.exists(updater_py):
        updater_cmd = [
            python_exe,
            "-m",
            "PyInstaller",
            "--noconfirm",
            "--clean",
            "--onefile",
            "--windowed",  # Now has GUI, avoid console
            "--optimize", "1",
            "--hidden-import", "PySide6.QtCore",
            "--hidden-import", "PySide6.QtGui",
            "--hidden-import", "PySide6.QtWidgets",
            "--hidden-import", "qfluentwidgets",
            "--hidden-import", "winrt.windows.data.xml.dom",
            "--hidden-import", "winrt.windows.ui.notifications",
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
        updater_cmd += _build_exclude_args()
        subprocess.check_call(updater_cmd, cwd=root_dir)

    output_dir = os.path.join(dist_dir, "Luminalium")
    if os.path.isdir(output_dir):
        _remove_pdb_files(output_dir)
        _prune_qt_translations(output_dir)
        _prune_qtwebengine_locales(output_dir)
        _prune_unused_qt_binaries(output_dir)
        _prune_pyinstaller_metadata(output_dir)

    if os.path.isdir(build_dir):
        shutil.rmtree(build_dir, ignore_errors=True)
    for extra in [
        "main.build",
        "main.dist",
        "Luminalium.build",
        "Luminalium.dist",
        "Luminalium.spec",
        "updater.spec",
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.isdir(extra_path):
            shutil.rmtree(extra_path, ignore_errors=True)
        elif os.path.isfile(extra_path):
            try:
                os.remove(extra_path)
            except OSError:
                pass
    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        pyinstaller_cache = os.path.join(local_app_data, "pyinstaller")
        if os.path.isdir(pyinstaller_cache):
            shutil.rmtree(pyinstaller_cache, ignore_errors=True)


if __name__ == "__main__":
    run()
