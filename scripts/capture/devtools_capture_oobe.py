import base64
import json
import os
import socket
import subprocess
import sys
import time
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = Path(__file__).resolve().parent
SETTINGS_PATH = OUT_DIR / "preview-settings.json"
PORT = 9339
PYTHON = Path(os.environ.get("LOCALAPPDATA", "")) / "Programs" / "Python" / "Python313" / "python.exe"


def port_open(port: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.settimeout(0.25)
        return sock.connect_ex(("127.0.0.1", port)) == 0


def wait_http_json(url: str, timeout: float = 20.0):
    deadline = time.time() + timeout
    last = None
    while time.time() < deadline:
        try:
            with urllib.request.urlopen(url, timeout=1.0) as response:
                return json.loads(response.read().decode("utf-8"))
        except Exception as exc:
            last = exc
            time.sleep(0.25)
    raise RuntimeError(f"timeout waiting for {url}: {last}")


def read_ws_frame(sock: socket.socket, buffered: bytearray | None = None) -> bytes:
    def read_exact(size: int) -> bytes:
        if buffered is not None and buffered:
            chunk = bytes(buffered[:size])
            del buffered[:size]
            if len(chunk) == size:
                return chunk
            return chunk + sock.recv(size - len(chunk))
        return sock.recv(size)

    header = read_exact(2)
    if len(header) < 2:
        raise RuntimeError("websocket closed")
    b1, b2 = header
    masked = bool(b2 & 0x80)
    length = b2 & 0x7F
    if length == 126:
        length = int.from_bytes(read_exact(2), "big")
    elif length == 127:
        length = int.from_bytes(read_exact(8), "big")
    mask = read_exact(4) if masked else b""
    payload = bytearray()
    while len(payload) < length:
        chunk = read_exact(length - len(payload))
        if not chunk:
            raise RuntimeError("websocket payload closed")
        payload.extend(chunk)
    if masked:
        payload = bytearray(byte ^ mask[i % 4] for i, byte in enumerate(payload))
    if b1 & 0x0F == 8:
        raise RuntimeError("websocket close frame")
    return bytes(payload)


class DevToolsClient:
    def __init__(self, ws_url: str):
        import secrets

        ws_url = ws_url.replace("ws://", "")
        host_port, path = ws_url.split("/", 1)
        host, port_s = host_port.split(":")
        self.sock = socket.create_connection((host, int(port_s)), timeout=5)
        key = base64.b64encode(secrets.token_bytes(16)).decode("ascii")
        request = (
            f"GET /{path} HTTP/1.1\r\n"
            f"Host: {host_port}\r\n"
            "Upgrade: websocket\r\n"
            "Connection: Upgrade\r\n"
            f"Sec-WebSocket-Key: {key}\r\n"
            "Sec-WebSocket-Version: 13\r\n"
            "\r\n"
        )
        self.sock.sendall(request.encode("ascii"))
        response = b""
        while b"\r\n\r\n" not in response:
            response += self.sock.recv(4096)
        if b" 101 " not in response:
            raise RuntimeError(response.decode("latin1", "replace"))
        _, _, leftover = response.partition(b"\r\n\r\n")
        self.buffer = bytearray(leftover)
        self.next_id = 1

    def call(self, method: str, params: dict | None = None, timeout: float = 10.0):
        msg_id = self.next_id
        self.next_id += 1
        payload = json.dumps({"id": msg_id, "method": method, "params": params or {}}).encode("utf-8")
        mask = os.urandom(4)
        header = bytearray([0x81])
        length = len(payload)
        if length < 126:
            header.append(0x80 | length)
        elif length < 65536:
            header.append(0x80 | 126)
            header.extend(length.to_bytes(2, "big"))
        else:
            header.append(0x80 | 127)
            header.extend(length.to_bytes(8, "big"))
        header.extend(mask)
        header.extend(byte ^ mask[i % 4] for i, byte in enumerate(payload))
        self.sock.sendall(header)

        deadline = time.time() + timeout
        while time.time() < deadline:
            self.sock.settimeout(max(0.1, deadline - time.time()))
            data = json.loads(read_ws_frame(self.sock, self.buffer).decode("utf-8"))
            if data.get("id") == msg_id:
                if "error" in data:
                    raise RuntimeError(data["error"])
                return data.get("result", {})
        raise RuntimeError(f"timeout waiting for {method}")

    def close(self):
        try:
            self.sock.close()
        except Exception:
            pass


def launch_preview() -> subprocess.Popen:
    if port_open(PORT):
        raise RuntimeError(f"DevTools port {PORT} is already in use")
    env = os.environ.copy()
    env["SETTINGS_PATH"] = str(SETTINGS_PATH)
    env["ONBOARDING_PREVIEW"] = "true"
    env["QTWEBENGINE_REMOTE_DEBUGGING"] = str(PORT)
    env["QTWEBENGINE_CHROMIUM_FLAGS"] = "--disable-gpu --disable-software-rasterizer=false"
    html_path = ROOT / "plugins" / "builtins" / "onboarding" / "onboarding.html"
    return subprocess.Popen(
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


def write_settings() -> None:
    settings = {
        "General": {
            "Language": "zh-CN",
            "RunAtStartup": False,
            "DisableAnimations": False,
            "CrashAutoHandleEnabled": False,
            "CrashAutoHandleMode": "ShowAnalyzer",
            "AutoShowOverlay": True,
        },
        "Appearance": {"ThemeMode": "Light", "ThemeId": "default"},
        "Overlay": {"Scale": 1.0, "PopWindowScale": 1.0, "SafeArea": 0, "ShowStatusBar": False},
    }
    SETTINGS_PATH.write_text(json.dumps(settings, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    write_settings()
    process = launch_preview()
    client = None
    try:
        pages = wait_http_json(f"http://127.0.0.1:{PORT}/json/list", timeout=30)
        target = next(
            (
                page
                for page in pages
                if page.get("webSocketDebuggerUrl")
                and "onboarding.html" in str(page.get("url", ""))
            ),
            None,
        )
        if not target:
            target = next((page for page in pages if page.get("webSocketDebuggerUrl")), None)
        if not target:
            raise RuntimeError(f"no debuggable page: {pages}")
        print(f"DEVTOOLS_TARGET {target.get('title')} {target.get('url')}")
        client = DevToolsClient(target["webSocketDebuggerUrl"])
        client.call("Page.enable")
        client.call("Runtime.enable")
        time.sleep(2.5)

        captures = [
            ("01-welcome.png", "欢迎页", ""),
            ("02-disclaimer.png", "免责声明与许可证", "window.next && window.next();"),
            ("03-appearance.png", "外观风格", """
                (() => {
                  const c = document.getElementById('disclaimer-agree');
                  if (c) {
                    c.checked = true;
                    c.dispatchEvent(new Event('change', { bubbles: true }));
                  }
                  if (window.updateDisclaimerButtonState) window.updateDisclaimerButtonState();
                  if (window.next) window.next();
                })();
            """),
            ("04-default-behavior.png", "默认行为", "window.next && window.next();"),
            ("05-overlay.png", "放映界面", "window.next && window.next();"),
            ("06-finish.png", "准备就绪", "window.next && window.next();"),
        ]

        for filename, label, script in captures:
            if script.strip():
                client.call("Runtime.evaluate", {"expression": script, "awaitPromise": False})
                time.sleep(1.2)
            text_result = client.call(
                "Runtime.evaluate",
                {"expression": "document.body ? document.body.innerText : ''", "returnByValue": True},
            )
            text = text_result.get("result", {}).get("value", "")
            print(f"DOM {label}: {' '.join(text.split())[:180]}")
            result = client.call(
                "Page.captureScreenshot",
                {"format": "png", "fromSurface": True, "captureBeyondViewport": False},
                timeout=15,
            )
            data = base64.b64decode(result["data"])
            path = OUT_DIR / filename
            path.write_bytes(data)
            print(f"CAPTURE {label}: {path} bytes={len(data)}")
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
            print(output[-4000:])
            print("PROCESS_OUTPUT_END")


if __name__ == "__main__":
    raise SystemExit(main())

