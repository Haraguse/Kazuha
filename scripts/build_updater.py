import os
import subprocess
from pathlib import Path

def main():
    script_dir = Path(__file__).parent
    updater_py = script_dir / "updater.py"
    
    cmd = [
        "pyinstaller",
        "--noconfirm",
        "--onefile",
        "--noconsole",
        "--clean",
        "--name", "updater",
        "--distpath", str(script_dir.parent), # Put updater.exe in root
        str(updater_py)
    ]
    
    print("Building updater.exe...")
    subprocess.run(cmd, check=True)
    print("Build complete.")

if __name__ == "__main__":
    main()
