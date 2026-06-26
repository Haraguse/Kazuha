import importlib.util
import os
import subprocess
import sys
import time
from base64 import b64decode
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = Path(__file__).resolve().parent
HELPER = OUT_DIR / "devtools_capture_oobe.py"
PORT = 9340
PYTHON = Path(os.environ.get("LOCALAPPDATA", "")) / "Programs" / "Python" / "Python313" / "python.exe"

spec = importlib.util.spec_from_file_location("devtools_capture_oobe", HELPER)
helper = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(helper)


def main() -> int:
    env = os.environ.copy()
    env["SETTINGS_PATH"] = str(OUT_DIR / "preview-settings.json")
    env["ONBOARDING_PREVIEW"] = "true"
    env["QTWEBENGINE_REMOTE_DEBUGGING"] = str(PORT)
    env["QTWEBENGINE_CHROMIUM_FLAGS"] = "--disable-gpu --disable-software-rasterizer=false"
    html_path = ROOT / "plugins" / "builtins" / "onboarding" / "onboarding.html"
    process = subprocess.Popen(
        [
            str(PYTHON),
            "main.py",
            "--webview-runner",
            str(html_path),
            "Onboarding Preview",
            "960",
            "720",
            "true",
        ],
        cwd=str(ROOT),
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    client = None
    try:
        pages = helper.wait_http_json(f"http://127.0.0.1:{PORT}/json/list", timeout=30)
        target = next(page for page in pages if page.get("webSocketDebuggerUrl"))
        print(f"TARGET {target.get('url')}")
        client = helper.DevToolsClient(target["webSocketDebuggerUrl"])
        print("Browser.getVersion", client.call("Browser.getVersion", timeout=5))
        client.call("Page.enable", timeout=5)
        time.sleep(3)
        result = client.call("Page.captureScreenshot", {"format": "png", "fromSurface": True}, timeout=15)
        out = OUT_DIR / "probe-welcome.png"
        out.write_bytes(b64decode(result["data"]))
        print(f"WROTE {out} {out.stat().st_size}")
        return 0
    finally:
        if client:
            client.close()
        process.terminate()
        try:
            output, _ = process.communicate(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            output, _ = process.communicate(timeout=5)
        if output:
            print("PROCESS_OUTPUT_BEGIN")
            print(output[-3000:])
            print("PROCESS_OUTPUT_END")


if __name__ == "__main__":
    raise SystemExit(main())
