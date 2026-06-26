import os
import subprocess
from pathlib import Path

def main():
    project_root = Path(__file__).parent.parent
    updater_py = project_root / "updater" / "updater.py"
    
    cmd = [
        "pyinstaller",
        "--noconfirm",
        "--onefile",
        "--noconsole",
        "--clean",
        "--name", "updater",
        "--distpath", str(project_root), # Put updater.exe in project root
        str(updater_py)
    ]
    
    print("Building updater.exe...")
    subprocess.run(cmd, check=True)
    print("Build complete.")

if __name__ == "__main__":
    main()
