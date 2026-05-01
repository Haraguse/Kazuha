import os
import sys
import sqlite3
import asyncio
import threading
import json
import hashlib
import tempfile
import subprocess
from pathlib import Path
from aiohttp import web, ClientSession

# Constants
GITHUB_REPO = "SECTL/Luminalium"
LOCAL_APP_DATA = Path(os.getenv("LOCALAPPDATA", os.path.expanduser("~")))
CACHE_DIR = LOCAL_APP_DATA / "Luminalium" / "update_cache"
DB_PATH = CACHE_DIR / "update.db"
APP_DIR = Path(sys.executable).parent if getattr(sys, "frozen", False) else Path(__file__).parent.parent.parent
DEV_MARKER = APP_DIR / ".dev"

def is_dev_env():
    if DEV_MARKER.exists():
        return True
    if "python.exe" in sys.executable.lower():
        return True
    if not getattr(sys, "frozen", False):
        return True
    return False

def init_db():
    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    with sqlite3.connect(DB_PATH) as conn:
        conn.execute("CREATE TABLE IF NOT EXISTS update_progress(id INTEGER PRIMARY KEY, progress INTEGER, status TEXT)")
        conn.execute("INSERT OR REPLACE INTO update_progress(id, progress, status) VALUES (1, 0, 'idle')")

def set_progress(progress: int, status: str):
    try:
        with sqlite3.connect(DB_PATH) as conn:
            conn.execute("UPDATE update_progress SET progress = ?, status = ? WHERE id = 1", (progress, status))
    except Exception as e:
        print(f"[UpdateService] DB Error: {e}")

def get_progress():
    try:
        with sqlite3.connect(DB_PATH) as conn:
            cursor = conn.execute("SELECT progress, status FROM update_progress WHERE id = 1")
            row = cursor.fetchone()
            if row:
                return {"progress": row[0], "status": row[1]}
    except Exception as e:
        print(f"[UpdateService] DB Error: {e}")
    return {"progress": 0, "status": "idle"}

def _is_truthy(value) -> bool:
    if value is None:
        return False
    return str(value).strip().lower() in {"1", "true", "yes", "on"}


async def check_update_impl(force: bool = False):
    if is_dev_env():
        return {"available": False, "reason": "DEV_MODE"}
    
    url = f"https://api.github.com/repos/{GITHUB_REPO}/releases/latest"
    try:
        async with ClientSession() as session:
            async with session.get(url, timeout=10) as resp:
                if resp.status == 200:
                    data = await resp.json()
                    version = data.get("tag_name", "")
                    assets = data.get("assets", [])
                    changelog = data.get("body", "")
                    
                    target_asset = None
                    for asset in assets:
                        name = asset.get("name", "")
                        if "Windows" in name and name.endswith(".zip"):
                            target_asset = asset
                            break
                    
                    if target_asset:
                        # Check current version
                        current_version = "0.0.0"
                        version_file = APP_DIR / "version.json"
                        if version_file.exists():
                            try:
                                with open(version_file, "r", encoding="utf-8") as f:
                                    v_data = json.load(f)
                                    current_version = v_data.get("versionnm", "0.0.0")
                            except:
                                pass
                        
                        # Compare versions (simplified, assume tag is like v1.0.0)
                        clean_ver = version.lstrip("v")
                        if clean_ver != current_version or force:
                            return {
                                "available": True,
                                "version": clean_ver,
                                "changelog": changelog,
                                "download_url": target_asset.get("browser_download_url"),
                                "size": target_asset.get("size"),
                                "forced": bool(force),
                            }
                        else:
                            return {"available": False, "reason": "UP_TO_DATE"}
    except Exception as e:
        print(f"[UpdateService] Check update error: {e}")
    return {"available": False, "reason": "ERROR"}

async def download_update_impl(download_url: str, version: str):
    set_progress(0, "downloading")
    version_dir = CACHE_DIR / version
    version_dir.mkdir(parents=True, exist_ok=True)
    zip_path = version_dir / "update.zip"
    
    retries = 3
    for attempt in range(retries):
        try:
            async with ClientSession() as session:
                async with session.get(download_url) as resp:
                    if resp.status >= 400:
                        raise Exception(f"HTTP Error {resp.status}")
                    
                    total_size = int(resp.headers.get("Content-Length", 0))
                    downloaded = 0
                    
                    with open(zip_path, "wb") as f:
                        last_update = asyncio.get_event_loop().time()
                        async for chunk in resp.content.iter_chunked(8192):
                            f.write(chunk)
                            downloaded += len(chunk)
                            
                            now = asyncio.get_event_loop().time()
                            # Update >= 2Hz (every 0.5s)
                            if now - last_update >= 0.5 and total_size > 0:
                                progress = int((downloaded / total_size) * 100)
                                set_progress(progress, "downloading")
                                last_update = now
                    
                    # Verify SHA-256 if possible (try to fetch .sha256 file)
                    try:
                        sha256_url = download_url + ".sha256"
                        async with session.get(sha256_url) as sha_resp:
                            if sha_resp.status == 200:
                                expected_hash = (await sha_resp.text()).strip().split()[0]
                                
                                # Calculate actual hash
                                sha256 = hashlib.sha256()
                                with open(zip_path, "rb") as f:
                                    for block in iter(lambda: f.read(65536), b""):
                                        sha256.update(block)
                                actual_hash = sha256.hexdigest()
                                
                                if actual_hash != expected_hash:
                                    raise Exception(f"SHA-256 mismatch! Expected {expected_hash}, got {actual_hash}")
                    except Exception as sha_err:
                        if "mismatch" in str(sha_err):
                            raise sha_err
                        # Ignore if .sha256 is not available
                        pass

                    set_progress(100, "download_complete")
                    # Trigger updater subprocess
                    trigger_updater(zip_path, version)
                    return
        except Exception as e:
            print(f"[UpdateService] Download error attempt {attempt+1}: {e}")
            if attempt == retries - 1:
                set_progress(0, "failed")
                return
            await asyncio.sleep(2)

def trigger_updater(zip_path: Path, version: str):
    updater_exe = APP_DIR / "updater.exe"
    if not updater_exe.exists():
        updater_exe = APP_DIR / "scripts" / "updater.py" # For dev/testing
        if not updater_exe.exists():
            set_progress(0, "updater_missing")
            return
            
    if getattr(sys, "frozen", False):
        cmd = [str(updater_exe)]
    else:
        cmd = [sys.executable, str(updater_exe)]
        
    cmd.extend([
        "--zip", str(zip_path),
        "--version", version,
        "--app-dir", str(APP_DIR),
        "--cache-dir", str(CACHE_DIR),
        "--parent-pid", str(os.getpid()),
    ])
    
    # DETACHED_PROCESS = 0x00000008
    subprocess.Popen(cmd, creationflags=0x00000008 if sys.platform == "win32" else 0)

# API Handlers
async def handle_check(request):
    force = _is_truthy(request.query.get("force"))
    res = await check_update_impl(force=force)
    return web.json_response(res)

async def handle_download(request):
    if is_dev_env():
        return web.json_response({"error": "DEV_MODE"}, status=403)
        
    data = await request.json()
    url = data.get("download_url")
    version = data.get("version")
    if not url or not version:
        return web.json_response({"error": "Missing params"}, status=400)
        
    asyncio.create_task(download_update_impl(url, version))
    return web.json_response({"status": "started"})

async def handle_progress(request):
    return web.json_response(get_progress())

async def handle_changelog(request):
    ver = request.query.get("ver", "")
    # In a real app, you might fetch from GitHub again or cache it
    # For now, we just return a simple check
    url = f"https://api.github.com/repos/{GITHUB_REPO}/releases/tags/v{ver}"
    try:
        async with ClientSession() as session:
            async with session.get(url, timeout=10) as resp:
                if resp.status == 200:
                    data = await resp.json()
                    return web.json_response({"changelog": data.get("body", "")})
    except:
        pass
    return web.json_response({"changelog": "No changelog available."})

async def handle_open_settings(request):
    # This will be called by a new instance to tell the running instance to open settings
    # We can trigger it by writing to a file or using a callback
    # For now, let's write to a flag file that the main app can poll
    open_flag = APP_DIR / "_internal" / ".open_settings"
    open_flag.parent.mkdir(exist_ok=True)
    open_flag.touch()
    return web.json_response({"status": "ok"})

def start_update_server(port: int = 28423):
    init_db()
    
    @web.middleware
    async def cors_middleware(request, handler):
        response = await handler(request)
        response.headers['Access-Control-Allow-Origin'] = '*'
        response.headers['Access-Control-Allow-Methods'] = 'GET, POST, OPTIONS'
        response.headers['Access-Control-Allow-Headers'] = 'Content-Type'
        return response

    app = web.Application(middlewares=[cors_middleware])
    
    # Handle OPTIONS for CORS
    async def handle_options(request):
        return web.Response(text="")
    
    app.router.add_options('/{tail:.*}', handle_options)
    app.router.add_get('/api/update/check', handle_check)
    app.router.add_post('/api/update/download', handle_download)
    app.router.add_get('/api/update/progress', handle_progress)
    app.router.add_get('/api/update/changelog', handle_changelog)
    app.router.add_get('/api/update/open_settings', handle_open_settings)
    
    runner = web.AppRunner(app)
    
    def run_server():
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            loop.run_until_complete(runner.setup())
            site = web.TCPSite(runner, '127.0.0.1', port)
            loop.run_until_complete(site.start())
            print(f"[UpdateService] Started on http://127.0.0.1:{port}")
            loop.run_forever()
        except OSError as e:
            # WinError 10048: port already in use (likely another instance already started service)
            if getattr(e, "errno", None) == 10048:
                print(
                    f"[UpdateService] Port {port} already in use, reuse existing local update service."
                )
            else:
                print(f"[UpdateService] Failed to start on port {port}: {e}")
        except Exception as e:
            print(f"[UpdateService] Unexpected server error: {e}")
        finally:
            try:
                loop.run_until_complete(runner.cleanup())
            except Exception:
                pass
            try:
                loop.close()
            except Exception:
                pass
        
    t = threading.Thread(target=run_server, daemon=True)
    t.start()
    return port
