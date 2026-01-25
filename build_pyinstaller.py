import os
import sys
import shutil
import subprocess


def run():
    root_dir = os.path.dirname(os.path.abspath(__file__))
    dist_dir = os.path.join(root_dir, "dist")
    build_dir = os.path.join(root_dir, "build")

    venv_python = os.path.join(root_dir, ".venv", "Scripts", "python.exe")
    if not os.path.exists(venv_python):
        raise FileNotFoundError("未找到 .venv\\Scripts\\python.exe")
    python_exe = venv_python

    icons_dir = os.path.join(root_dir, "icons")
    logo_ico = os.path.join(icons_dir, "logo.ico")

    data_sep = os.pathsep
    add_data = [
        f"version.json{data_sep}.",
        f"config{data_sep}config",
        f"plugins{data_sep}plugins",
        f"icons{data_sep}icons",
        f"ppt_assistant{data_sep}ppt_assistant",
        f"fonts{data_sep}fonts",
    ]

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
        "--onefile",
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
    for extra in [
        "main.build",
        "main.dist",
        "Kazuha.build",
        "Kazuha.dist",
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.isdir(extra_path):
            shutil.rmtree(extra_path, ignore_errors=True)
    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        pyinstaller_cache = os.path.join(local_app_data, "pyinstaller")
        if os.path.isdir(pyinstaller_cache):
            shutil.rmtree(pyinstaller_cache, ignore_errors=True)

    subprocess.check_call(cmd, cwd=root_dir)

    exe_name = "Kazuha.exe"
    src_exe = os.path.join(dist_dir, exe_name)
    dst_exe = os.path.join(root_dir, exe_name)

    if os.path.exists(src_exe):
        if os.path.exists(dst_exe):
            os.remove(dst_exe)
        shutil.move(src_exe, dst_exe)

    if os.path.isdir(dist_dir):
        shutil.rmtree(dist_dir, ignore_errors=True)
    if os.path.isdir(build_dir):
        shutil.rmtree(build_dir, ignore_errors=True)
    for extra in [
        "main.build",
        "main.dist",
        "Kazuha.build",
        "Kazuha.dist",
    ]:
        extra_path = os.path.join(root_dir, extra)
        if os.path.isdir(extra_path):
            shutil.rmtree(extra_path, ignore_errors=True)


if __name__ == "__main__":
    run()
