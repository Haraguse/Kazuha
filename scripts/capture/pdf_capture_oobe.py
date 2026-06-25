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
os.environ.setdefault("QTWEBENGINE_CHROMIUM_FLAGS", "--disable-gpu --disable-software-rasterizer=false")

from PySide6.QtCore import QEventLoop, QMarginsF, QTimer, QSize, QSizeF, QUrl  # noqa: E402
from PySide6.QtGui import QColor, QImage, QPageLayout, QPageSize  # noqa: E402
from PySide6.QtPdf import QPdfDocument  # noqa: E402
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
        "Appearance": {"ThemeMode": "Light", "ThemeId": "default"},
        "Overlay": {"Scale": 1.0, "PopWindowScale": 1.0, "SafeArea": 0, "ShowStatusBar": False},
    }
    SETTINGS_PATH.write_text(json.dumps(settings, ensure_ascii=False, indent=2), encoding="utf-8")
    return settings


def wait_ms(ms: int) -> None:
    loop = QEventLoop()
    QTimer.singleShot(ms, loop.quit)
    loop.exec()


def eval_js(window: MainWindow, expression: str):
    loop = QEventLoop()
    box = {"value": None}

    def done(value):
        box["value"] = value
        loop.quit()

    window.page().runJavaScript(expression, done)
    loop.exec()
    return box["value"]


def print_page_to_pdf(window: MainWindow, path: Path) -> None:
    loop = QEventLoop()
    ok_box = {"ok": False}

    def done(output_path: str, success: bool) -> None:
        ok_box["ok"] = bool(success)
        loop.quit()

    page_size = QPageSize(QSizeF(960, 720), QPageSize.Unit.Point, "Luminalium OOBE")
    layout = QPageLayout(
        page_size,
        QPageLayout.Orientation.Portrait,
        QMarginsF(0, 0, 0, 0),
        QPageLayout.Unit.Point,
    )
    window.page().pdfPrintingFinished.connect(done)
    window.page().printToPdf(str(path), layout)
    loop.exec()
    try:
        window.page().pdfPrintingFinished.disconnect(done)
    except Exception:
        pass
    if not ok_box["ok"] or not path.exists() or path.stat().st_size == 0:
        raise RuntimeError(f"printToPdf failed for {path}")


def pdf_to_png(pdf_path: Path, png_path: Path) -> None:
    doc = QPdfDocument()
    status = doc.load(str(pdf_path))
    if status != QPdfDocument.Error.None_:
        raise RuntimeError(f"QPdfDocument.load failed: {status}")
    image = doc.render(0, QSize(1440, 1080))
    if image.isNull():
        raise RuntimeError(f"QPdfDocument.render returned null for {pdf_path}")
    if image.format() != QImage.Format.Format_RGB32:
        image = image.convertToFormat(QImage.Format.Format_RGB32)
    if not image.save(str(png_path), "PNG"):
        raise RuntimeError(f"failed to save {png_path}")


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
        True,
        frameless=False,
    )

    loaded = QEventLoop()
    load_state = {"done": False}

    def on_loaded(ok: bool) -> None:
        load_state["done"] = True
        print(f"LOAD_FINISHED ok={ok}")
        loaded.quit()

    window.loadFinished.connect(on_loaded)
    window.show()
    window.load(QUrl.fromLocalFile(str(html_path)))
    QTimer.singleShot(15000, loaded.quit)
    loaded.exec()
    if not load_state["done"]:
        print("LOAD_TIMEOUT proceeding with current page state")
    wait_ms(2500)

    captures = [
        ("01-welcome", "欢迎页", ""),
        ("02-disclaimer", "免责声明与许可证", "window.next && window.next();"),
        ("03-appearance", "外观风格", """
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
        ("04-default-behavior", "默认行为", "window.next && window.next();"),
        ("05-overlay", "放映界面", "window.next && window.next();"),
        ("06-finish", "准备就绪", "window.next && window.next();"),
    ]

    for stem, label, script in captures:
        if script.strip():
            eval_js(window, script)
            wait_ms(1400)
        text = eval_js(window, "document.body ? document.body.innerText : ''") or ""
        print(f"DOM {label}: {' '.join(str(text).split())[:180]}")
        pdf_path = OUT_DIR / f"{stem}.pdf"
        png_path = OUT_DIR / f"{stem}.png"
        print_page_to_pdf(window, pdf_path)
        pdf_to_png(pdf_path, png_path)
        # Sanity check: non-empty render should not be a uniform image.
        img = QImage(str(png_path))
        sample = QColor(img.pixel(10, 10)).rgb()
        varied = any(QColor(img.pixel(x, y)).rgb() != sample for x in range(0, img.width(), 180) for y in range(0, img.height(), 180))
        print(f"CAPTURE {label}: {png_path} varied={varied} bytes={png_path.stat().st_size}")

    window.close()
    app.quit()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
