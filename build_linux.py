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
        # Fallback for Linux venv structure
        venv_python = os.path.join(root_dir, ".venv", "bin", "python")
        if os.path.exists(venv_python):
            python_exe = venv_python
        else:
             # Just try 'python3' from PATH
            python_exe = "python3"

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
        "setuptools",
        "--exclude-module",
        "pkg_resources",
        "--hidden-import",
        "PySide6.QtXml",
        "--onedir",
        "--windowed",
        "--name",
        "Kazuha",
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
    
    # Clean up previous build artifacts
    for extra in [
        "main.build",
        "main.dist",
        "Kazuha.build",
        "Kazuha.dist",
        "Kazuha.spec"
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.exists(extra_path):
            if os.path.isdir(extra_path):
                shutil.rmtree(extra_path, ignore_errors=True)
            else:
                os.remove(extra_path)
                
    print(f"Running command: {' '.join(cmd)}")
    subprocess.check_call(cmd, cwd=root_dir)

    output_dir = os.path.join(dist_dir, "Kazuha")
    if os.path.isdir(output_dir):
        _remove_pdb_files(output_dir)
        _prune_qt_translations(output_dir)
        _prune_qtwebengine_locales(output_dir)
        
        # Strip binaries on Linux
        if sys.platform.startswith("linux"):
            print("Stripping binaries...")
            strip_cmd = shutil.which("strip")
            if strip_cmd:
                count = 0
                for root, dirs, files in os.walk(output_dir):
                    for file in files:
                        file_path = os.path.join(root, file)
                        # Strip shared libraries and the main executable
                        if file.endswith(".so") or (os.access(file_path, os.X_OK) and "." not in file):
                            try:
                                subprocess.run([strip_cmd, file_path], check=False, stderr=subprocess.DEVNULL)
                                count += 1
                            except Exception:
                                pass
                print(f"Stripped {count} files.")

    if os.path.isdir(build_dir):
        shutil.rmtree(build_dir, ignore_errors=True)
    
    # Clean up spec file
    spec_path = os.path.join(root_dir, "Kazuha.spec")
    if os.path.exists(spec_path):
        os.remove(spec_path)


if __name__ == "__main__":
    run()
