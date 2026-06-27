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
from aiohttp import web, ClientSession, ClientTimeout

CHROME_UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36"

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
    try:
        CACHE_DIR.mkdir(parents=True, exist_ok=True)
        with sqlite3.connect(DB_PATH) as conn:
            conn.execute("CREATE TABLE IF NOT EXISTS update_progress(id INTEGER PRIMARY KEY, progress INTEGER, status TEXT)")
            conn.execute("INSERT OR REPLACE INTO update_progress(id, progress, status) VALUES (1, 0, 'idle')")
    except Exception as e:
        print(f"[UpdateService] init_db failed: {e}")

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


def _parse_version(version: str) -> tuple:
    parts = version.strip().lstrip("v").split(".")
    result = []
    for p in parts:
        try:
            result.append(int(p))
        except ValueError:
            result.append(0)
    while len(result) < 4:
        result.append(0)
    return tuple(result)


def _is_newer(remote: str, current: str) -> bool:
    return _parse_version(remote) > _parse_version(current)


GITHUB_MIRRORS = [
    ("github", "https://api.github.com"),
    ("ghproxy", "https://ghproxy.net/https://api.github.com"),
    ("moeyy", "https://github.moeyy.xyz/https://api.github.com"),
    ("ghproxy2", "https://mirror.ghproxy.com/https://api.github.com"),
    ("999proxy", "https://gh.api.99988866.xyz/https://api.github.com"),
    ("kkgithub", "https://api.kkgithub.com"),
]

def _normalize_download_url(url: str, mirror_name: str) -> str:
    if not url or not url.startswith("https://github.com"):
        return url
    if mirror_name == "ghproxy":
        return "https://ghproxy.net/" + url
    if mirror_name == "moeyy":
        return "https://github.moeyy.xyz/" + url
    if mirror_name == "ghproxy2":
        return "https://mirror.ghproxy.com/" + url
    if mirror_name == "999proxy":
        return "https://gh.api.99988866.xyz/" + url
    if mirror_name == "kkgithub":
        return url.replace("https://github.com", "https://kkgithub.com")
    return url

async def check_update_impl(force: bool = False):
    if is_dev_env():
        return {"available": False, "reason": "DEV_MODE"}
    
    current_version = "0.0.0"
    version_file = APP_DIR / "version.json"
    if version_file.exists():
        try:
            with open(version_file, "r", encoding="utf-8") as f:
                v_data = json.load(f)
                current_version = v_data.get("versionnm", "0.0.0")
        except:
            pass

    timeout = ClientTimeout(total=15, connect=10)
    headers = {"User-Agent": CHROME_UA, "Accept": "application/vnd.github.v3+json"}
    
    for mirror_name, api_base in GITHUB_MIRRORS:
        url = f"{api_base}/repos/{GITHUB_REPO}/releases/latest"
        try:
            async with ClientSession(headers=headers, timeout=timeout) as session:
                async with session.get(url) as resp:
                    if resp.status != 200:
                        print(f"[UpdateService] Mirror {mirror_name} returned HTTP {resp.status}, trying next...")
                        continue
                    data = await resp.json()
                    version = data.get("tag_name", "")
                    assets = data.get("assets", [])
                    changelog = data.get("body", "")
                    
                    target_asset = None
                    for asset in assets:
                        name = asset.get("name", "")
                        if name.endswith(".exe") or name.endswith(".zip") or name.endswith(".7z"):
                            target_asset = asset
                            break
                    
                    if target_asset:
                        clean_ver = version.lstrip("v")
                        download_url = target_asset.get("browser_download_url")
                        if force or _is_newer(clean_ver, current_version):
                            return {
                                "available": True,
                                "version": clean_ver,
                                "changelog": changelog,
                                "download_url": _normalize_download_url(download_url, mirror_name),
                                "size": target_asset.get("size"),
                                "forced": bool(force),
                            }
                        else:
                            return {"available": False, "reason": "UP_TO_DATE"}
        except Exception as e:
            print(f"[UpdateService] Mirror {mirror_name} failed: {e}")
            continue
    return {"available": False, "reason": "ERROR"}

async def download_update_impl(download_url: str, version: str):
    set_progress(0, "downloading")
    version_dir = CACHE_DIR / version
    version_dir.mkdir(parents=True, exist_ok=True)
    zip_path = version_dir / "update.zip"
    
    timeout = ClientTimeout(total=None, connect=30, sock_read=60)
    headers = {"User-Agent": CHROME_UA}
    retries = 3
    for attempt in range(retries):
        try:
            async with ClientSession(headers=headers, timeout=timeout) as session:
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
                            if now - last_update >= 0.5 and total_size > 0:
                                progress = int((downloaded / total_size) * 100)
                                set_progress(progress, "downloading")
                                last_update = now
                    
                    try:
                        sha256_url = download_url + ".sha256"
                        async with session.get(sha256_url) as sha_resp:
                            if sha_resp.status == 200:
                                expected_hash = (await sha_resp.text()).strip().split()[0]
                                
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
                        pass

                    set_progress(100, "download_complete")
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
        updater_exe = APP_DIR / "scripts" / "updater" / "updater.py" # For dev/testing
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
async def handle_health(request):
    return web.json_response({"status": "ok", "port": request.transport.get_extra_info('sockname')[1] if request.transport else 0})

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
    url = f"https://api.github.com/repos/{GITHUB_REPO}/releases/tags/v{ver}"
    timeout = ClientTimeout(total=10, connect=5)
    headers = {"User-Agent": CHROME_UA}
    try:
        async with ClientSession(headers=headers, timeout=timeout) as session:
            async with session.get(url) as resp:
                if resp.status == 200:
                    data = await resp.json()
                    return web.json_response({"changelog": data.get("body", "")})
    except:
        pass
    return web.json_response({"changelog": "No changelog available."})

async def handle_open_settings(request):
    open_flag = APP_DIR / "_internal" / ".open_settings"
    open_flag.parent.mkdir(exist_ok=True)
    open_flag.touch()
    return web.json_response({"status": "ok"})

async def handle_protocol_url(request):
    url = request.query.get("url", "")
    if not url:
        return web.json_response({"error": "Missing url param"}, status=400)
    protocol_flag = APP_DIR / "_internal" / ".protocol_url"
    protocol_flag.parent.mkdir(exist_ok=True)
    try:
        with open(protocol_flag, "w", encoding="utf-8") as f:
            f.write(url)
    except Exception as e:
        return web.json_response({"error": str(e)}, status=500)
    return web.json_response({"status": "ok"})

ECHO_CAVE_BASE = "https://appwrite.sectl.cn/api/echo-cave"

async def handle_echo_cave_proxy(request):
    params = []
    for key in ("mode", "limit", "id", "mine", "author", "count", "leaderboard"):
        val = request.query.get(key)
        if val is not None:
            params.append(f"{key}={val}")
    query_string = "&".join(params)
    url = f"{ECHO_CAVE_BASE}?{query_string}" if query_string else ECHO_CAVE_BASE
    timeout = ClientTimeout(total=10, connect=5)
    headers = {"User-Agent": CHROME_UA}
    try:
        async with ClientSession(headers=headers, timeout=timeout) as session:
            async with session.get(url) as resp:
                data = await resp.json()
                return web.json_response(data, status=resp.status)
    except asyncio.TimeoutError:
        return web.json_response({"success": False, "error": "timeout"}, status=504)
    except Exception as e:
        return web.json_response({"success": False, "error": str(e)}, status=502)

def start_update_server(port: int = 28423):
    print(f"[UpdateService] Starting update server on port {port}...", flush=True)
    try:
        init_db()
        print("[UpdateService] init_db OK", flush=True)
    except Exception as e:
        print(f"[UpdateService] init_db FAILED (server will still start): {e}", flush=True)
    
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
    app.router.add_get('/api/update/health', handle_health)
    app.router.add_get('/api/update/check', handle_check)
    app.router.add_post('/api/update/download', handle_download)
    app.router.add_get('/api/update/progress', handle_progress)
    app.router.add_get('/api/update/changelog', handle_changelog)
    app.router.add_get('/api/update/open_settings', handle_open_settings)
    app.router.add_get('/api/protocol/handle', handle_protocol_url)
    app.router.add_get('/api/echo-cave', handle_echo_cave_proxy)
    
    runner = web.AppRunner(app)
    
    def run_server():
        print("[UpdateService] Server thread started", flush=True)
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        try:
            print("[UpdateService] Setting up AppRunner...", flush=True)
            loop.run_until_complete(runner.setup())
            print("[UpdateService] AppRunner setup OK, binding to port...", flush=True)
            site = web.TCPSite(runner, '127.0.0.1', port)
            loop.run_until_complete(site.start())
            print(f"[UpdateService] Started on http://127.0.0.1:{port}", flush=True)
            loop.run_forever()
        except OSError as e:
            # WinError 10048: port already in use (likely another instance already started service)
            if getattr(e, "errno", None) == 10048:
                print(
                    f"[UpdateService] Port {port} already in use, reuse existing local update service.", flush=True
                )
            else:
                print(f"[UpdateService] Failed to start on port {port}: {e}", flush=True)
        except Exception as e:
            print(f"[UpdateService] Unexpected server error: {e}", flush=True)
            import traceback
            traceback.print_exc()
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
    print(f"[UpdateService] Server thread spawned (port={port})", flush=True)
    return port
