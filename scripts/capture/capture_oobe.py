import json
import os
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = Path(__file__).resolve().parent
SETTINGS_PATH = OUT_DIR / "preview-settings.json"

os.chdir(ROOT)
sys.path.insert(0, str(ROOT))
os.environ["SETTINGS_PATH"] = str(SETTINGS_PATH)
os.environ["ONBOARDING_PREVIEW"] = "true"
os.environ.setdefault("QTWEBENGINE_CHROMIUM_FLAGS", "--disable-gpu")

from PySide6.QtCore import QTimer  # noqa: E402
from PySide6.QtGui import QGuiApplication  # noqa: E402
from PySide6.QtWidgets import QApplication  # noqa: E402

from plugins.webview_runner import Api, MainWindow  # noqa: E402


def write_preview_settings() -> dict:
    settings = {
        "General": {
            "Language": "zh-CN",
            "RunAtStartup": False,
            "DisableAnimations": False,
            "CrashAutoHandleEnabled": False,
            "CrashAutoHandleMode": "ShowAnalyzer",
            "AutoShowOverlay": True,
        },
        "Appearance": {
            "ThemeMode": "Light",
            "ThemeId": "default",
        },
        "Overlay": {
            "Scale": 1.0,
            "PopWindowScale": 1.0,
            "SafeArea": 0,
            "ShowStatusBar": False,
        },
    }
    SETTINGS_PATH.write_text(
        json.dumps(settings, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    return settings


def run_js(page, script: str) -> None:
    page.runJavaScript(script)


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    settings = write_preview_settings()

    app = QApplication(sys.argv)
    api = Api()
    api.settings = settings
    version_path = ROOT / "version.json"
    if version_path.exists():
        api.version = json.loads(version_path.read_text(encoding="utf-8"))
    else:
        api.version = {}

    html_path = ROOT / "plugins" / "builtins" / "onboarding" / "onboarding.html"
    window = MainWindow(
        "Onboarding Preview",
        str(html_path),
        api,
        960,
        720,
        "Light",
        False,
        False,
        frameless=False,
    )
    window.show()
    window.raise_()
    window.activateWindow()

    captures = [
        (
            "01-welcome.png",
            "欢迎页",
            "",
        ),
        (
            "02-disclaimer.png",
            "免责声明与许可证",
            "window.next && window.next();",
        ),
        (
            "03-appearance.png",
            "外观风格",
            """
            (() => {
              const c = document.getElementById('disclaimer-agree');
              if (c) {
                c.checked = true;
                c.dispatchEvent(new Event('change', { bubbles: true }));
              }
              if (window.updateDisclaimerButtonState) window.updateDisclaimerButtonState();
              if (window.next) window.next();
            })();
            """,
        ),
        (
            "05-default-behavior.png",
            "默认行为",
            "window.next && window.next();",
        ),
        (
            "06-overlay.png",
            "放映界面",
            "window.next && window.next();",
        ),
        (
            "07-notification-startup.png",
            "通知与启动",
            "window.next && window.next();",
        ),
        (
            "08-finish.png",
            "准备就绪",
            "window.next && window.next();",
        ),
    ]

    def capture_at(index: int) -> None:
        if index >= len(captures):
            print("CAPTURE_DONE")
            app.quit()
            return

        filename, label, script = captures[index]

        def do_grab() -> None:
            def after_text(text: str) -> None:
                summary = " ".join(str(text or "").split())[:180]
                print(f"DOM {label}: {summary}")
                window.raise_()
                window.activateWindow()
                QGuiApplication.processEvents()
                screen = window.screen() or QGuiApplication.primaryScreen()
                if screen is None:
                    pixmap = window.grab()
                else:
                    geom = window.frameGeometry()
                    pixmap = screen.grabWindow(0, geom.x(), geom.y(), geom.width(), geom.height())
                target = OUT_DIR / filename
                ok = pixmap.save(str(target), "PNG")
                print(f"CAPTURE {index + 1}/{len(captures)} {label}: {target} ok={ok}")
                QTimer.singleShot(900, lambda: capture_at(index + 1))

            window.page().runJavaScript("document.body ? document.body.innerText : ''", after_text)

        if script.strip():
            run_js(window.page(), script)
        QTimer.singleShot(2200, do_grab)

    def on_loaded(ok: bool) -> None:
        print(f"LOAD_FINISHED ok={ok}")
        QTimer.singleShot(1800, lambda: capture_at(0))

    window.loadFinished.connect(on_loaded)
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
