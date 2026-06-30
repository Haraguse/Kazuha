import os
import subprocess
from pathlib import Path

def main():
    project_root = Path(__file__).parent
    updater_py = project_root / "scripts" / "updater" / "updater.py"
    
    cmd = [
        "pyinstaller",
        "--noconfirm",
        "--onefile",
        "--noconsole",
        "--clean",
        "--name", "updater",
        "--paths", str(project_root),
        "--distpath", str(project_root),
        str(updater_py)
    ]
    
    print("Building updater.exe...")
    subprocess.run(cmd, check=True)
    print("Build complete.")

if __name__ == "__main__":
    main()
