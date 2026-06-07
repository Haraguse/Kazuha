import sys
import os

with open('plugins/webview_runner.py', 'r', encoding='utf-8') as f:
    content = f.read()

old_str = """def _apply_chromium_flags():
    _maybe_add_vxkex_path()
    use_software_webengine = _should_use_software_webengine()"""

new_str = """def _apply_chromium_flags():
    _maybe_add_vxkex_path()

    os.environ["QT_OPENGL"] = "desktop"
    os.environ["QT_VULKAN_DISABLE"] = "1"

    use_software_webengine = _should_use_software_webengine()"""

content = content.replace(old_str, new_str)

old_flags = """        flags = [
            "--enable-zero-copy",
            "--enable-features=GpuRasterization",
            "--disable-frame-rate-limit",
            "--disable-gpu-vsync",
            "--disable-renderer-backgrounding",
            "--disable-background-timer-throttling",
            "--disable-backgrounding-occluded-windows",
            "--disable-breakpad",
            "--disable-component-update",
            "--disable-print-preview",
            "--disable-speech-api",
            "--disable-web-security",
            "--wm-window-animations-disabled",
            "--enable-gpu-rasterization",
            "--ignore-gpu-blocklist",
            "--enable-low-end-device-mode",
            "--renderer-process-limit=1",
            "--max-decoded-image-size-bytes=10485760",
            "--disk-cache-size=20971520",
            "--max-active-webgl-contexts=1",
            "--disable-features=BackForwardCache,VaapiVideoDecoder,MediaFoundationVideoCapture,HardwareMediaKeyHandling",
            "--js-flags=--max-old-space-size=128",
            "--num-raster-threads=2",
        ]"""

new_flags = """        flags = [
            "--disable-frame-rate-limit",
            "--disable-gpu-vsync",
            "--enable-gpu-rasterization",
            "--enable-zero-copy",
            "--enable-features=VaapiVideoDecoder,VaapiVideoEncoder",
            "--ignore-gpu-blocklist",
            "--enable-hardware-overlays",
            "--disable-breakpad",
            "--disable-component-update",
            "--disable-print-preview",
            "--disable-speech-api",
            "--disable-web-security",
        ]"""

content = content.replace(old_flags, new_flags)

with open('plugins/webview_runner.py', 'w', encoding='utf-8') as f:
    f.write(content)
