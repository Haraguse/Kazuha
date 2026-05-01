import os
import sys
import shutil
from pathlib import Path

# Add project root to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from scripts.updater import backup_app, restore_backup

def test_backup_and_restore(tmp_path):
    app_dir = tmp_path / "app"
    app_dir.mkdir()
    bak_dir = tmp_path / "bak"
    
    # Create fake executable and _internal
    exe = app_dir / "Luminalium.exe"
    exe.write_text("fake_exe")
    internal = app_dir / "_internal"
    internal.mkdir()
    (internal / "test.py").write_text("print('test')")
    (internal / "user").mkdir()
    (internal / "user" / "config.json").write_text("{}")
    
    # Perform backup
    backup_app(app_dir, bak_dir)
    
    assert (bak_dir / "Luminalium.exe").exists()
    assert (bak_dir / "_internal" / "test.py").exists()
    
    # Delete app
    shutil.rmtree(app_dir)
    app_dir.mkdir()
    
    # Perform restore
    restore_backup(app_dir, bak_dir)
    
    assert (app_dir / "Luminalium.exe").exists()
    assert (app_dir / "_internal" / "test.py").exists()
    assert (app_dir / "_internal" / "user" / "config.json").exists()
