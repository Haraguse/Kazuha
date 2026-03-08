import sys
import os
import traceback
import tempfile
import subprocess
import json
import importlib
import importlib.util
import time
import warnings

# Delay heavy imports or move them inside if __name__ == "__main__" logic
# to allow --webview-runner to start fast and clean.

if __name__ == "__main__":
    if "--webview-runner" in sys.argv:
        # Minimal imports for webview runner
        idx = sys.argv.index("--webview-runner")
        import plugins.webview_runner as _wv
        # Adjust sys.argv so argparse in webview_runner (if any) sees clean args
        sys.argv = ["webview_runner.py"] + sys.argv[idx + 1 :]
        _wv.main()
        sys.exit(0)

from PySide6.QtWidgets import QApplication, QDialog, QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QTextEdit, QFrame, QGraphicsDropShadowEffect, QProgressBar
from PySide6.QtCore import Qt, QTimer, Slot, QSize, QPoint, QCoreApplication, QEvent, QObject, QUrl
from PySide6.QtGui import QFontDatabase, QFont, QColor, QIcon, QRegion, QPainter, QPen, QBrush, QFontMetrics
from PySide6.QtWebEngineWidgets import QWebEngineView


from ppt_assistant.core.ppt_monitor import PPTMonitor
from ppt_assistant.ui.overlay import OverlayWindow
from plugins.builtins.settings.plugin import SettingsPlugin
from plugins.builtins.timer.plugin import TimerPlugin
from ppt_assistant.ui.tray import SystemTray
from ppt_assistant.core.config import cfg, SETTINGS_PATH, PLUGINS_DIR, reload_cfg, _apply_theme_and_color, Theme, qconfig, FIRST_RUN
from ppt_assistant.core.timer_manager import TimerManager
from ppt_assistant.core.i18n import t
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.win_focus_watcher import WindowsFocusWatcher


class WindowIconEventFilter(QObject):
    def __init__(self, icon: QIcon):
        super().__init__()
        self._icon = icon

    def eventFilter(self, obj, event):
        if self._icon.isNull():
            return False
        try:
            if event.type() in (QEvent.Show, QEvent.Polish):
                if isinstance(obj, QWidget) and obj.isWindow() and obj.windowIcon().isNull():
                    obj.setWindowIcon(self._icon)
        except Exception:
            return False
        return False


SPLASH_I18N = {
    "zh-CN": {
        "initializing": "正在启动",
        "loading_config": "加载配置",
        "loading_fonts": "加载字体",
        "init_monitor": "启动监视器",
        "init_ui": "创建界面",
        "loading_plugins": "加载插件",
        "loading_settings": "加载设置",
        "loading_timer": "加载计时器",
        "init_tray": "创建托盘图标",
        "finalizing": "完成初始化",
        "watermark.1": "开发中版本",
        "watermark.2": "技术预览版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新评估版本",
        "dev_watermark": "{type}\n不保证最终品质 （{version}）"
    },
    "zh-TW": {
        "initializing": "正在初始化",
        "loading_config": "載入設定",
        "loading_fonts": "載入字型",
        "init_monitor": "啟動監視器",
        "init_ui": "建立介面",
        "loading_plugins": "載入插件",
        "loading_settings": "載入設定",
        "loading_timer": "載入計時器",
        "init_tray": "建立系統匣圖示",
        "finalizing": "完成初始化",
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "dev_watermark": "{type}\n不保證最終品質 （{version}）"
    },
    "yue-HK": {
        "initializing": "開工中",
        "loading_config": "撈緊設定",
        "loading_fonts": "撈緊字型",
        "init_monitor": "啟動監視器",
        "init_ui": "砌緊介面",
        "loading_plugins": "載入插件",
        "loading_settings": "載入設定",
        "loading_timer": "載入計時器",
        "init_tray": "整緊托盤圖示",
        "finalizing": "搞掂",
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "dev_watermark": "{type}\n品質唔包，出事唔好屌我 （{version}）"
    },
    "ja-JP": {
        "initializing": "初期化中",
        "loading_config": "設定を読み込み中",
        "loading_fonts": "フォントを読み込み中",
        "init_monitor": "モニターを起動中",
        "init_ui": "UIを作成中",
        "loading_plugins": "プラグインを読み込み中",
        "loading_settings": "設定を読み込み中",
        "loading_timer": "タイマーを読み込み中",
        "init_tray": "トレイアイコンを作成中",
        "finalizing": "初期化完了",
        "watermark.1": "開発中バージョン",
        "watermark.2": "テクニカルプレビュー",
        "watermark.3": "Release Preview",
        "watermark.4": "再評価バージョン",
        "dev_watermark": "{type}\n品質は保証されません （{version}）"
    },
    "en-US": {
        "initializing": "Initializing",
        "loading_config": "Loading config",
        "loading_fonts": "Loading fonts",
        "init_monitor": "Starting monitor",
        "init_ui": "Creating UI",
        "loading_plugins": "Loading plugins",
        "loading_settings": "Loading settings",
        "loading_timer": "Loading timer",
        "init_tray": "Creating system tray",
        "finalizing": "Finalizing",
        "watermark.1": "In-Development",
        "watermark.2": "Technical Preview",
        "watermark.3": "Release Preview",
        "watermark.4": "Re-evaluated Version",
        "dev_watermark": "{type}\nFinal quality not guaranteed ({version})"
    }
}


def _is_windows7():
    if sys.platform != "win32":
        return False
    try:
        v = sys.getwindowsversion()
        return v.major == 6 and v.minor == 1
    except Exception:
        return False


def _get_screen_refresh_rate():
    try:
        import ctypes
        user32 = ctypes.windll.user32
        hdc = user32.GetDC(0)
        rate = ctypes.windll.gdi32.GetDeviceCaps(hdc, 116) # VREFRESH
        user32.ReleaseDC(0, hdc)
        return rate if rate > 1 else 60
    except:
        return 60


def _apply_graphics_settings():
    if sys.platform == "linux":
        # Force software rendering on Linux to avoid compatibility issues with Mesa/drivers
        os.environ["QT_XCB_FORCE_SOFTWARE_OPENGL"] = "1"
        os.environ["QT_QUICK_BACKEND"] = "software"
        os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = "--disable-gpu --disable-software-rasterizer --no-sandbox"
        return

    # Base flags for high performance
    flags = [
        "--disable-frame-rate-limit",
        "--disable-gpu-vsync",
        "--ignore-gpu-blocklist",
    ]
    
    # Try to set a target FPS if possible, but mostly just unlock it.
    # User asked for 3x refresh rate.
    rate = _get_screen_refresh_rate()
    target_fps = rate * 3
    # Chromium doesn't have a direct --limit-fps flag in stable, but we can try --frames-throttled
    # or just rely on disabling the limit.
    # We will just unlock it as that satisfies "solve 60fps cap".
    # And we can set an env var that we might use elsewhere or just for reference.
    os.environ["KAZUHA_TARGET_FPS"] = str(target_fps)

    # Windows 7 Fallback
    if _is_windows7():
        candidates = [
            r"C:\Program Files\VxKex\Kex64",
            r"C:\Program Files\VxKex\Kex86",
            r"C:\Program Files (x86)\VxKex\Kex64",
            r"C:\Program Files (x86)\VxKex\Kex86",
        ]
        existing = os.environ.get("PATH", "")
        for path in candidates:
            dll_path = os.path.join(path, "KxNt.dll")
            if os.path.exists(dll_path):
                if path not in existing.split(os.pathsep):
                    os.environ["PATH"] = path + os.pathsep + existing
                break


    current = os.environ.get("QTWEBENGINE_CHROMIUM_FLAGS", "").strip()
    merged = current.split()
    for flag in flags:
        if flag not in merged:
            merged.append(flag)
    os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = " ".join(merged)
    



def _load_settings_json():
    if not os.path.exists(SETTINGS_PATH):
        return {}
    try:
        with open(SETTINGS_PATH, "rb") as f:
            raw = f.read()
    except Exception:
        return {}
    for enc in ("utf-8", "utf-8-sig", "gbk"):
        try:
            text = raw.decode(enc)
        except UnicodeDecodeError:
            continue
        try:
            data = json.loads(text)
        except Exception:
            continue
        return data if isinstance(data, dict) else {}
    return {}


def _get_current_language():
    data = _load_settings_json()
    return data.get("General", {}).get("Language", "zh-CN")


def _apply_global_font(app: QApplication):
    root_dir = os.path.dirname(os.path.abspath(__file__))
    font_path = os.path.join(root_dir, "fonts", "MiSansVF.ttf")
    selected_family = ""
    data = _load_settings_json()
    lang = data.get("General", {}).get("Language", "zh-CN")
    profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
    v = (profiles.get(lang, {}) or {}).get("qt", "")
    if isinstance(v, str) and v.strip():
        selected_family = v.strip()

    base_family = ""
    if os.path.exists(font_path):
        try:
            font_id = QFontDatabase.addApplicationFont(font_path)
            if font_id != -1:
                families = QFontDatabase.applicationFontFamilies(font_id)
                if families:
                    base_family = families[0]
        except Exception:
            base_family = ""

    preferred_family = "Meiryo UI" if lang == "yue-HK" else ""
    family = selected_family or preferred_family or base_family
    if not family:
        return
    app.setFont(QFont(family))


def _load_version_info():
    root_dir = os.path.dirname(os.path.abspath(__file__))
    version_path = os.path.join(root_dir, "version.json")
    version = ""
    code_name = ""
    code_name_cn = ""
    if os.path.exists(version_path):
        try:
            with open(version_path, "r", encoding="utf-8") as f:
                data = json.load(f)
            version = data.get("version", "")
            raw_code_name = data.get("code_name", "")
            code_name_cn = data.get("code_name_CN", "")
            mapping = {
                "MomokaKawaragi": "Momoka Kawaragi",
                "NinaIseri": "Nina Iseri",
                "SubaruAwa": "Subaru Awa"
            }
            code_name = mapping.get(raw_code_name, raw_code_name)
        except Exception:
            pass
    return version, code_name, code_name_cn


def _format_version_display(version: str) -> str:
    if not version:
        return ""
    parts = str(version).strip().split(".")
    if len(parts) < 2:
        return str(version).strip()
    suffix = parts[-1]
    if suffix in ["5", "6", "7"]:
        patch_num = int(suffix) - 4
        base = ".".join(parts[:-1])
        return f"{base} Patch {patch_num}"
    return str(version).strip()


def _is_dev_preview_version(version: str) -> bool:
    if not version:
        return False
    parts = str(version).strip().split(".")
    if len(parts) < 2:
        return False
    # 除了 .0 是正式版，其他后缀 (.1, .2, .3, .4) 都带水印
    suffix = parts[-1]
    return suffix in ["1", "2", "3", "4"]


class StartupSplash(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self._splash_style = cfg.splashStyle.value
        self._pixmap = None
        self._progress_value = 0
        self._status_text = ""
        
        icon = load_app_icon()
        if not icon.isNull():
            self.setWindowIcon(icon)
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowStaysOnTopHint | Qt.Tool)
        self.setAttribute(Qt.WA_TranslucentBackground)

        self._version_raw, self._code_name_en, self._code_name_cn = _load_version_info()
        self._version_text = _format_version_display(self._version_raw)
        self._language = _get_current_language()
        self._is_first_run = FIRST_RUN
        
        # 确定主题
        theme_val = cfg.themeMode.value
        if theme_val == Theme.AUTO:
            from qfluentwidgets import isDarkTheme
            self._is_dark = isDarkTheme()
        else:
            self._is_dark = theme_val == Theme.DARK

        if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
            self._build_ui_nina()
        else:
            self._container = QFrame(self)
            self._container.setObjectName("splashContainer")
            self._build_ui()
            self._apply_styles()

        self._center_on_screen()
        self.set_progress(0, "initializing")

        if self._splash_style != "nina_iseri_1_2" and not self._is_first_run and _is_dev_preview_version(self._version_raw):
            self._dev_watermark = QLabel(self._container)
            i18n_table = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"])
            suffix = self._version_raw.split(".")[-1]
            w_type = i18n_table.get(f"watermark.{suffix}", "")
            tmpl = i18n_table.get("dev_watermark", "")
            self._dev_watermark.setText(tmpl.format(type=w_type, version=self._version_text))
            font = QFont()
            font.setPixelSize(11)
            self._dev_watermark.setFont(font)
            self._dev_watermark.setAlignment(Qt.AlignRight | Qt.AlignBottom)
            
            watermark_color = "rgba(255, 255, 255, 100)" if self._is_dark else "rgba(0, 0, 0, 100)"
            self._dev_watermark.setStyleSheet(f"color: {watermark_color};")
            
            self._dev_watermark.resize(320, 36)
            self._dev_watermark.move(self._container.width() - self._dev_watermark.width() - 16,
                                     self._container.height() - self._dev_watermark.height() - 12)

    def _build_ui_nina(self):
        icon_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "icons", "1.2_Splash.png")
        if os.path.exists(icon_path):
            from PySide6.QtGui import QPixmap
            original_pixmap = QPixmap(icon_path)
            if not original_pixmap.isNull():
                # For High DPI, we should NOT pre-scale the pixmap if possible, or scale it based on devicePixelRatio.
                # However, QPainter.drawPixmap with SmoothPixmapTransform is usually better than pre-scaling if we want dynamic resizing.
                # But here we are setting a fixed window size.
                
                # Let's keep the original high-res pixmap in memory and only resize the window logic.
                self._pixmap = original_pixmap
                
                # Logic to determine window size:
                # If image is very large, we define a "logical" size for the window (e.g. 860 width)
                # and let the paintEvent draw the high-res image scaled down into that rect.
                
                target_width = 860
                aspect_ratio = original_pixmap.height() / original_pixmap.width()
                target_height = int(target_width * aspect_ratio)
                
                self.resize(target_width, target_height)
            else:
                # Fallback
                self.resize(860, 480)
        else:
            self.resize(860, 480)
            
        # We don't use standard widgets, we paint in paintEvent

    def paintEvent(self, event):
        if self._splash_style == "nina_iseri_1_2" and not self._is_first_run and self._pixmap:
            painter = QPainter(self)
            painter.setRenderHint(QPainter.Antialiasing)
            painter.setRenderHint(QPainter.TextAntialiasing)
            painter.setRenderHint(QPainter.SmoothPixmapTransform)
            
            # Draw Background
            # Use drawPixmap with target rect to ensure it scales to window size
            painter.drawPixmap(self.rect(), self._pixmap)
            
            # Constants
            margin_left = 40
            margin_bottom = 40
            
            # Fonts
            # Use "Microsoft YaHei" explicitly for Chinese/CJK support as primary or fallback
            title_font = QFont("Bahnschrift")
            title_font.setStyleHint(QFont.SansSerif)
            # Add fallback families
            title_font.setFamilies(["Bahnschrift", "Microsoft YaHei", "SimHei", "Segoe UI"])
            title_font.setPixelSize(36)
            title_font.setBold(True)
            
            sub_font = QFont("Bahnschrift")
            sub_font.setStyleHint(QFont.SansSerif)
            sub_font.setFamilies(["Bahnschrift", "Microsoft YaHei", "SimHei", "Segoe UI"])
            sub_font.setPixelSize(14)
            
            # Calculate positions from bottom
            h = self.height()
            w = self.width()
            
            # Reduce width to ~65% to avoid character more aggressively
            content_width = w * 0.65
            
            progress_h = 6
            progress_y = h - margin_bottom - progress_h
            
            # Title "Kazuha"
            painter.setPen(QColor("#000000"))
            painter.setFont(title_font)
            # Calculate exact height to position tighter
            fm_title = QFontMetrics(title_font)
            title_height = fm_title.capHeight()
            
            # Subtitle
            painter.setFont(sub_font)
            fm_sub = QFontMetrics(sub_font)
            sub_height = fm_sub.capHeight()
            
            # Position calculations
            # Gap between Title baseline and Subtitle top: e.g. 8px
            # Gap between Subtitle baseline and Progress bar: e.g. 15px
            
            subtitle_baseline_y = progress_y - 15
            title_baseline_y = subtitle_baseline_y - sub_height - 16 # 20px gap
            
            # Draw Title
            brand_name_map = {
                "zh-CN": "万演",
                "zh-TW": "万演",
                "yue-HK": "萬演",
                "ja-JP": "カズハ",
                "en-US": "Kazuha",
            }
            brand_name = brand_name_map.get(self._language, "Kazuha")
            
            painter.setFont(title_font)
            painter.setPen(QColor("#000000"))
            painter.drawText(margin_left, title_baseline_y, brand_name)
            
            # Draw Subtitle
            # Use Microsoft YaHei for potential fallback if needed, but Bahnschrift is primary
            # QFont combo isn't directly supported in drawText, rely on system fallback or set specific family list
            # "Bahnschrift, Microsoft YaHei"
            painter.setFont(sub_font)
            painter.setPen(QColor("#888888"))
            subtitle = f"{self._version_text} // {self._code_name_en}"
            painter.drawText(margin_left, subtitle_baseline_y, subtitle)
            
            # Status Text (Right aligned relative to content width)
            status_text = f"{self._status_text}"
            status_rect = fm_sub.boundingRect(status_text)
            
            # Align status text to the end of the progress bar
            status_x = margin_left + content_width - status_rect.width()
            painter.drawText(status_x, subtitle_baseline_y, status_text)
            
            # Progress Bar Background
            painter.setPen(Qt.NoPen)
            painter.setBrush(QColor("#E0E0E0"))
            # Width is content_width
            painter.drawRoundedRect(margin_left, progress_y, content_width, progress_h, 3, 3)
            
            # Progress Bar Value
            if self._progress_value > 0:
                painter.setBrush(QColor("#404040"))
                prog_width = content_width * (self._progress_value / 100.0)
                painter.drawRoundedRect(margin_left, progress_y, prog_width, progress_h, 3, 3)
                
            painter.end()
        else:
            super().paintEvent(event)

    def _build_ui(self):
        if self._is_first_run:
            self._container.setFixedSize(960, 540)
            self._container.move(0, 0)
            
            # Center Logo (120x120)
            self._icon_label = QLabel(self._container)
            self._icon_label.setFixedSize(120, 120)
            icon_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "icons", "logo.svg")
            if os.path.exists(icon_path):
                icon = QIcon(icon_path)
                pix = icon.pixmap(120, 120)
                self._icon_label.setPixmap(pix)
            
            # Center it
            cx = (960 - 120) // 2
            cy = (540 - 120) // 2
            self._icon_label.move(cx, cy)
            return

        # Redesigned based on QML spec
        # Width: 678, Height: 255
        self._container.setFixedSize(678, 255)
        self._container.move(24, 16) # Offset for shadow
        
        # Logo (kZHTXT_2.png equivalent) - x: 38, y: 37
        self._icon_label = QLabel(self._container)
        self._icon_label.setFixedSize(64, 64) 
        icon_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "icons", "logo.svg")
        if os.path.exists(icon_path):
            icon = QIcon(icon_path)
            pix = icon.pixmap(64, 64)
            self._icon_label.setPixmap(pix)
        self._icon_label.move(38, 37)

        brand_name_map = {
            "zh-CN": "万演",
            "zh-TW": "万演",
            "yue-HK": "萬演",
            "ja-JP": "カズハ",
            "en-US": "Kazuha",
        }
        brand_name = brand_name_map.get(self._language, "Kazuha")
        self._brand_label = QLabel(brand_name, self._container)
        brand_font_family = "Meiryo UI" if self._language == "yue-HK" else "Yu Gothic UI"
        brand_font = QFont(brand_font_family)
        brand_font.setPixelSize(32)
        brand_font.setWeight(QFont.Black) 
        self._brand_label.setFont(brand_font)
        self._brand_label.setFixedWidth(328)
        self._brand_label.setFixedHeight(32) 
        self._brand_label.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        self._brand_label.move(38, 111)

        # Version Info Container - x: 38, y: 143
        self._version_info_label = QLabel(self._container)
        
        ver_text = self._version_text or ""
        en_text = self._code_name_en or ""
        
        ver_color = "#FFFFFF" if self._is_dark else "#000000"
        en_color = "rgba(255, 255, 255, 0.47)" if self._is_dark else "rgba(0, 0, 0, 0.47)"
        
        html = f"""
        <div style="line-height: 20px;">
            <span style="font-family: 'MiSans'; font-size: 11px; font-weight: 500; color: {ver_color};">{ver_text}</span>
            <span style="font-family: 'MiSans'; font-size: 11px; font-weight: 300; color: {en_color}; margin-left: 2px;">{en_text}</span>
        </div>
        """
        
        self._version_info_label.setText(html)
        self._version_info_label.adjustSize()
        self._version_info_label.move(38, 143) 

        # Loading Spinner (Vector) - x: 38, y: 199
        spinner_color = QColor("#d9d9d9") if self._is_dark else QColor("#666666")
        self._spinner = IndeterminateSpinner(self._container, color=spinner_color)
        self._spinner.move(38, 199)
        self._spinner.start()

        # Status Text (element_2) - x: 76, y: 203
        init_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"])["initializing"]
        self._percent_label = QLabel(f"{init_text} 0%", self._container)
        percent_font_family = "Meiryo UI" if self._language == "yue-HK" else "HarmonyOS Sans SC"
        percent_font = QFont(percent_font_family)
        percent_font.setPixelSize(15)
        percent_font.setBold(True)
        self._percent_label.setFont(percent_font)
        self._percent_label.setFixedWidth(221)
        self._percent_label.setFixedHeight(24)
        self._percent_label.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        self._percent_label.move(76, 200)

        # Progress Bar Foreground (rectangle_31) - y: 247, h: 8, w: 678
        self._progress = QProgressBar(self._container)
        self._progress.setRange(0, 100)
        self._progress.setValue(0)
        self._progress.setTextVisible(False)
        self._progress.setFixedHeight(8)
        self._progress.setFixedWidth(678) 
        self._progress.move(0, 247)

        shadow = QGraphicsDropShadowEffect(self)
        shadow.setBlurRadius(24)
        shadow.setOffset(0, 8)
        shadow.setColor(QColor(0, 0, 0, 60))
        self._container.setGraphicsEffect(shadow)

    def _apply_styles(self):
        if self._is_first_run:
            self.resize(960, 540)
            if self._is_dark:
                bg_color = "#121212"
            else:
                bg_color = "#f2f3f5"
            
            self._container.setStyleSheet(
                f"QFrame#splashContainer {{"
                f"background-color: {bg_color};"
                f"border: none;"
                f"border-radius: 0px;"
                f"}}"
            )
            return

        if self._is_dark:
            bg_color = "rgba(47, 47, 47, 240)"
            border_color = "rgba(255, 255, 255, 0.15)"
            brand_color = "#ffffff"
            percent_color = "#d9d9d9"
            progress_bg = "#454545" 
            chunk_color = "#E1EBFF" 
        else:
            bg_color = "rgba(255, 255, 255, 240)"
            border_color = "rgba(0, 0, 0, 0.08)"
            brand_color = "#000000"
            percent_color = "#666666"
            progress_bg = "#e5e5e5"
            chunk_color = "#3275F5"

        self.resize(678 + 48, 255 + 48) # Increased for shadow
        
        self._container.setStyleSheet(
            f"QFrame#splashContainer {{"
            f"background-color: {bg_color};"
            f"border: 1px solid {border_color};"
            "border-radius: 8px;"
            "}"
        )
        self._brand_label.setStyleSheet(f"color: {brand_color};")
        self._percent_label.setStyleSheet(f"color: {percent_color};")
        
        self._progress.setStyleSheet(
            "QProgressBar {"
            f"background-color: {progress_bg};"
            "border: none;"
            "border-bottom-left-radius: 8px;"
            "border-bottom-right-radius: 8px;"
            "}"
            "QProgressBar::chunk {"
            f"background-color: {chunk_color};"
            "border-bottom-left-radius: 8px;"
            "border-bottom-right-radius: 8px;"
            "}"
        )

    def _center_on_screen(self):
        screen = QApplication.primaryScreen()
        if not screen:
            return
        screen_geo = screen.geometry()
        w = self.width()
        h = self.height()
        x = screen_geo.x() + (screen_geo.width() - w) // 2
        y = screen_geo.y() + (screen_geo.height() - h) // 2
        self.move(x, y)

    def set_progress(self, value, text_key="initializing"):
        value = min(max(value, 0), 100)
        
        # Check if detailed splash is enabled
        if cfg.showDetailedSplash.value:
            display_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"]).get(text_key, text_key)
        else:
            # Always show "initializing" text if details are disabled
            init_key = "initializing"
            display_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"]).get(init_key, init_key)

        full_text = f"{display_text} {value}%"

        # Nina style
        if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
            self._progress_value = value
            self._status_text = full_text
            self.update()
            QApplication.processEvents()
            return

        # For first run splash (simple logo), we don't show progress
        if not hasattr(self, '_progress') or not hasattr(self, '_percent_label'):
            QApplication.processEvents()
            return

        self._progress.setValue(value)
        self._percent_label.setText(full_text)
        
        # Update spinner if needed, or it spins automatically
        QApplication.processEvents()

    def finish(self):
        if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
            self._progress_value = 100
            self._status_text = "100%"
            self.update()
            QTimer.singleShot(250, self.close)
            return

        if hasattr(self, '_progress'):
            self._progress.setValue(100)
        if hasattr(self, '_percent_label'):
            self._percent_label.setText("初始化完成 100%")
        if hasattr(self, '_spinner'):
            self._spinner.stop()
        QTimer.singleShot(250, self.close)

class IndeterminateSpinner(QWidget):
    def __init__(self, parent=None, color=QColor("#d9d9d9")):
        super().__init__(parent)
        self.setFixedSize(27, 27)
        self._angle = 0
        self._color = color
        self._timer = QTimer(self)
        self._timer.timeout.connect(self._rotate)
        self._timer.start(16) # ~60 FPS

    def start(self):
        if not self._timer.isActive():
            self._timer.start()

    def stop(self):
        self._timer.stop()

    def _rotate(self):
        self._angle = (self._angle + 5) % 360
        self.update()

    def paintEvent(self, event):
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing)
        
        rect = self.rect()
        cx, cy = rect.center().x(), rect.center().y()
        
        # Outer ring (static)
        pen = QPen(self._color)
        pen.setWidth(3)
        painter.setPen(pen)
        painter.setBrush(Qt.NoBrush)
        
        # Draw ring. Adjust rect to account for pen width
        ring_radius = 10 
        painter.drawEllipse(QPoint(cx, cy), ring_radius, ring_radius)
        
        # Inner rotating dot
        painter.save()
        painter.translate(cx, cy)
        painter.rotate(self._angle)
        
        # Draw small circle on the orbit
        dot_radius = 2.5
        orbit_radius = ring_radius - 3 - dot_radius + 1 # Fine tuned visual position
        
        painter.setPen(Qt.NoPen)
        painter.setBrush(QBrush(self._color))
        painter.drawEllipse(QPoint(0, -orbit_radius), dot_radius, dot_radius)
        
        painter.restore()
        painter.end()

def show_webview_dialog(title, text, confirm_text="确认", cancel_text="取消", is_error=False, hide_cancel=False, code=None):
    base_dir = os.path.dirname(os.path.abspath(__file__))
    theme = "auto"
    accent = "#3275F5"
    try:
        from ppt_assistant.core.config import cfg, Theme, qconfig
        theme = cfg.themeMode.value.lower() if hasattr(cfg.themeMode, "value") else "auto"
        resolved_theme = theme
        if theme == "auto":
            try:
                if isinstance(qconfig.theme, Theme):
                    resolved_theme = "dark" if qconfig.theme == Theme.DARK else "light"
            except:
                resolved_theme = "light"
        accent = "#E1EBFF" if resolved_theme == "dark" else "#3275F5"
    except:
        pass

    dialog_data = {
        "title": title,
        "text": text,
        "confirmText": confirm_text,
        "cancelText": cancel_text,
        "isError": is_error,
        "hideCancel": hide_cancel,
        "theme": theme,
        "accentColor": accent
    }
    if code is not None:
        dialog_data["code"] = code
    
    with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False, encoding='utf-8') as f:
        json.dump(dialog_data, f)
        temp_path = f.name
        
    root_dir = base_dir
    main_path = os.path.join(root_dir, "main.py")
    if getattr(sys, "frozen", False):
        cmd = [sys.executable, "--webview-runner", "--dialog", temp_path]
    else:
        cmd = [sys.executable, main_path, "--webview-runner", "--dialog", temp_path]
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, text=True)
    return proc

class CrashHandler:
    def __init__(self, app=None):
        self.app = app
        self.app_instance = None
        self._handling = False
        sys.excepthook = self.handle_exception
        import threading
        threading.excepthook = self.handle_thread_exception

    def set_app_instance(self, instance):
        self.app_instance = instance

    def handle_thread_exception(self, args):
        self.handle_exception(args.exc_type, args.exc_value, args.exc_traceback)

    def handle_exception(self, exc_type, exc_value, exc_traceback):
        if self._handling:
            sys.__excepthook__(exc_type, exc_value, exc_traceback)
            return
        self._handling = True
        if issubclass(exc_type, KeyboardInterrupt):
            sys.__excepthook__(exc_type, exc_value, exc_traceback)
            return
        
        error_msg = "".join(traceback.format_exception(exc_type, exc_value, exc_traceback))
        print(f"CRASH DETECTED:\n{error_msg}", file=sys.stderr)
        
        try:
            base_dir = os.path.dirname(os.path.abspath(__file__))
            root_dir = base_dir
            main_path = os.path.join(root_dir, "main.py")
            
            with tempfile.NamedTemporaryFile(mode='w', suffix='.log', delete=False, encoding='utf-8') as f:
                f.write(error_msg)
                temp_path = f.name
            
            creationflags = 0x00000008 # DETACHED_PROCESS
            if getattr(sys, "frozen", False):
                cmd = [sys.executable, "--webview-runner", "--crash-file", temp_path]
            else:
                cmd = [sys.executable, main_path, "--webview-runner", "--crash-file", temp_path]
            subprocess.Popen(cmd, creationflags=creationflags, close_fds=True)
        except Exception as e:
            print(f"Failed to launch crash dialog: {e}", file=sys.stderr)
        
        try:
            if self.app_instance is not None:
                self.app_instance.cleanup()
        except Exception as e:
            print(f"Error during crash cleanup: {e}", file=sys.stderr)
        
        import time
        time.sleep(0.5)
        os._exit(1)

def _handle_multi_instance(app: QApplication):
    try:
        import psutil
    except ImportError:
        return

    current_pid = os.getpid()
    main_path = os.path.abspath(__file__)
    pids = []
    for p in psutil.process_iter(["pid", "cmdline"]):
        if p.info.get("pid") == current_pid: continue
        cmd = p.info.get("cmdline") or []
        
        if "--webview-runner" in cmd:
            continue

        for part in cmd:
            try:
                if os.path.abspath(part) == main_path or os.path.basename(part).lower() == "main.py":
                    pids.append(p.info.get("pid"))
                    break
            except: continue
    
    if not pids: 
        return
    
    proc = show_webview_dialog(
        title="",
        text="",
        confirm_text="",
        cancel_text="",
        is_error=False,
        hide_cancel=False,
        code="multi_instance"
    )
    
    # Wait for the process to exit and check stdout for result
    stdout, _ = proc.communicate()
    
    # Option 1: Close New Instance
    if "CLOSE_NEW" in stdout:
        app.quit()
        sys.exit(0)
    
    # Option 2: Continue New Instance
    elif "CONTINUE_NEW" in stdout:
        return
        
    # Option 3: Restart Existing Instance (Kill old, continue new)
    elif "RESTART_OLD" in stdout:
        for pid in pids:
            try: psutil.Process(pid).terminate()
            except: pass
        return

    # Fallback: If dialog closed or cancelled, exit new instance
    app.quit()
    sys.exit(0)


def _t(key):
    return key # Simple fallback if i18n is missing

class PPTAssistantApp:
    def __init__(self, app: QApplication, splash=None):
        self.app = app
        self.app.setQuitOnLastWindowClosed(False)
        self._splash = splash
        self._timer_manager = TimerManager()
        self._focus_watcher = WindowsFocusWatcher(self.app)
        self._focus_watcher.start()
        self._last_timer_notify_at = 0.0
        self._reloading_overlay = False
        self._slideshow_running = False
        self._last_slideshow_rect = None
        self._last_slideshow_screen = None
        self._reload_timer = QTimer()
        self._reload_timer.setSingleShot(True)
        self._reload_timer.setInterval(150)
        self._reload_timer.timeout.connect(self._reload_overlay)
        
        # Start async initialization
        self._init_gen = self._init_steps()
        QTimer.singleShot(0, self._perform_init_step)

    def _init_steps(self):
        # Step 1: Basic Config
        yield 10, "loading_config"
        _apply_theme_and_color(cfg.themeMode.value)
        
        # Step 2: Fonts
        yield 20, "loading_fonts"
        _apply_global_font(self.app)
        self._current_language = _get_current_language()
        data = _load_settings_json()
        profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
        lang_profile = profiles.get(self._current_language, {}) or {}
        qt_font = lang_profile.get("qt", "")
        overlay_font = lang_profile.get("overlay", "") or qt_font
        self._current_qt_font = qt_font.strip() if isinstance(qt_font, str) else ""
        self._current_overlay_font = overlay_font.strip() if isinstance(overlay_font, str) else ""
        self._overlay_rebuild_at = (data.get("Overlay", {}) or {}).get("RecreateOverlayAt")

        self._settings_mtime = os.path.getmtime(SETTINGS_PATH) if os.path.exists(SETTINGS_PATH) else 0
        self._settings_timer = QTimer()
        self._settings_timer.setInterval(100)
        self._settings_timer.timeout.connect(self._check_settings_changed)
        self._settings_timer.start()

        self.app.aboutToQuit.connect(self.cleanup)

        # Step 3: Monitor (Non-UI logic)
        yield 30, "init_monitor"
        self.monitor = PPTMonitor()
        
        # Step 4: Overlay (UI creation - expensive)
        yield 40, "init_ui"
        # Yield to event loop BEFORE creating heavy UI to prevent freeze
        # We can split Overlay creation if needed, but yielding before is key
        pass 
        
        self.overlay = OverlayWindow()
        
        # Step 5: Plugins (IO/Process - expensive)
        yield 60, "loading_plugins"
        self._load_plugins()
        try:
            if FIRST_RUN and hasattr(self, "onboarding_plugin"):
                p = self.onboarding_plugin
                p.execute(preview=False)
                
                # Wait for onboarding window to appear (heuristic delay)
                start_wait = time.time()
                while time.time() - start_wait < 1.5:
                     QApplication.processEvents()
                     time.sleep(0.05)
                
                # Hide splash screen to handoff focus to onboarding
                if self._splash:
                    self._splash.hide()

                while p.process and p.process.poll() is None:
                    QApplication.processEvents()
                    time.sleep(0.1)
                reload_cfg()
                self.restart()
        except Exception:
            pass
        
        # Step 6: Tray (UI)
        yield 80, "init_tray"
        self.tray = SystemTray()
        
        # Step 7: Finalize connections
        yield 85, "finalizing"
        self.overlay.set_monitor(self.monitor)

        yield 90, "finalizing"
        self._connect_signals()

        yield 95, "finalizing"
        self.monitor.start_monitoring()

        if cfg.compatibilityMode.value:
            self.overlay.show()

        if self._splash is not None:
            self._splash.finish()

    def _perform_init_step(self):
        try:
            progress, text = next(self._init_gen)
            self.update_splash(progress, text)
            # Schedule next step immediately but allow event loop to breathe
            QTimer.singleShot(0, self._perform_init_step)
        except StopIteration:
            pass # Done
        except Exception as e:
            print(f"Initialization error: {e}")
            sys.exit(1)

    def _load_plugins(self):
        """Dynamic plugin loading from builtins and external directory."""
        self.plugins = []
        
        # 1. Load Builtin Plugins
        builtin_plugins = [
            "plugins.builtins.settings.plugin.SettingsPlugin",
            "plugins.builtins.onboarding.plugin.OnboardingPlugin",
            "plugins.builtins.board.plugin.BoardPlugin",
            "plugins.builtins.timer.plugin.TimerPlugin",
            "plugins.builtins.spotlight.plugin.SpotlightPlugin",
            "plugins.builtins.app_launcher.plugin.AppLauncherPlugin"
        ]
        
        for p_path in builtin_plugins:
            try:
                mod_name, cls_name = p_path.rsplit(".", 1)
                mod = importlib.import_module(mod_name)
                cls = getattr(mod, cls_name)
                plugin = cls()
                plugin.set_context(self)
                self.plugins.append(plugin)
                
                # Maintain compatibility with existing code
                if cls_name == "SettingsPlugin":
                    self.settings_plugin = plugin
                elif cls_name == "OnboardingPlugin":
                    self.onboarding_plugin = plugin
                elif cls_name == "BoardPlugin":
                    self.board_plugin = plugin
                elif cls_name == "TimerPlugin":
                    self.timer_plugin = plugin
            except Exception as e:
                print(f"Failed to load builtin plugin {p_path}: {e}")

        # 2. Load External Plugins from PLUGINS_DIR
        if os.path.exists(PLUGINS_DIR):
            for item in os.listdir(PLUGINS_DIR):
                p_dir = os.path.join(PLUGINS_DIR, item)
                if not os.path.isdir(p_dir):
                    continue
                
                # Check for manifest.json
                manifest_path = os.path.join(p_dir, "manifest.json")
                if not os.path.exists(manifest_path):
                    continue
                
                try:
                    with open(manifest_path, "r", encoding="utf-8") as f:
                        manifest = json.load(f)
                    
                    entry_point = manifest.get("entry")
                    if not entry_point:
                        continue
                    
                    # Add plugins dir to sys.path if not present
                    if PLUGINS_DIR not in sys.path:
                        sys.path.insert(0, PLUGINS_DIR)
                    
                    # Import from the specific plugin directory
                    mod_name, cls_name = entry_point.rsplit(".", 1)
                    spec = importlib.util.spec_from_file_location(
                        f"external_plugin_{item}", 
                        os.path.join(p_dir, mod_name + ".py")
                    )
                    mod = importlib.util.module_from_spec(spec)
                    spec.loader.exec_module(mod)
                    
                    # Instantiate the plugin class
                    cls = getattr(mod, cls_name)
                    plugin = cls()
                    plugin.set_context(self)
                    # Set plugin metadata from manifest
                    plugin.manifest = manifest
                    self.plugins.append(plugin)
                    print(f"Loaded external plugin: {manifest.get('name', item)}")
                except Exception as e:
                    print(f"Failed to load external plugin from {p_dir}: {e}")
                    traceback.print_exc()

    def update_splash(self, value, text):
        if self._splash:
            self._splash.set_progress(value, text)

    def _connect_signals(self):
        self.monitor.slideshow_started.connect(self.on_slideshow_start)
        self.monitor.slideshow_ended.connect(self.on_slideshow_end)
        self.monitor.slideshow_started.connect(lambda: self._focus_watcher.set_slideshow_running(True))
        self.monitor.slideshow_ended.connect(lambda: self._focus_watcher.set_slideshow_running(False))
        self.monitor.slideshow_hwnd_changed.connect(self._focus_watcher.set_slideshow_hwnd)
        self._focus_watcher.focus_on_slideshow_changed.connect(self._on_focus_on_slideshow_changed)

        self.overlay.request_next.connect(self.monitor.go_next)
        self.overlay.request_prev.connect(self.monitor.go_previous)
        self.overlay.request_goto.connect(self.monitor.go_to_slide)
        self.overlay.request_clear.connect(self.monitor.clear_screen)
        self.overlay.request_end.connect(self.monitor.end_show)

        self.overlay.request_ptr_arrow.connect(lambda: self.monitor.set_pointer_type(1))
        self.overlay.request_ptr_pen.connect(lambda: self.monitor.set_pointer_type(2))
        self.overlay.request_ptr_eraser.connect(lambda: self.monitor.set_pointer_type(5))
        self.overlay.request_pen_color.connect(self.monitor.set_pen_color)
        self.overlay.request_thumbnail.connect(lambda idx: self.monitor.export_slide_thumbnail(idx, os.path.join(tempfile.gettempdir(), "kazuha_ppt_thumbs", f"thumb_{idx}.png")))

        self.tray.show_settings.connect(self.settings_plugin.execute)
        self.tray.show_board.connect(self.board_plugin.execute)
        self.tray.show_timer.connect(self.timer_plugin.execute)
        self.tray.toggle_overlay.connect(self.toggle_overlay_visibility)
        self.tray.restart_app.connect(self.restart)
        self.tray.exit_app.connect(self.app.quit)

        self.timer_plugin.background_mode_entered.connect(self._on_timer_background_mode)

        self._timer_manager.finished.connect(self._on_timer_finished)

        self.monitor.slide_changed.connect(self.overlay.update_page_info)
        self.monitor.window_geometry_changed.connect(self.overlay.update_geometry)
        self.monitor.window_geometry_changed.connect(self._cache_slideshow_geometry)
        self.monitor.slideshow_hwnd_changed.connect(self.overlay.set_slideshow_hwnd)
        self.monitor.restrictions_changed.connect(self.overlay.set_ppt_restrictions)
        self.monitor.thumbnail_generated.connect(self.overlay.on_thumbnail_ready)

    @Slot()
    def toggle_overlay_visibility(self):
        if self.overlay:
            if self.overlay.isVisible():
                self.overlay.hide()
            else:
                self.overlay.show()
                self.overlay.raise_()
                self.overlay.activateWindow()

    @Slot()
    def _on_timer_finished(self):
        now = time.monotonic()
        if now - self._last_timer_notify_at < 1.0:
            return
        self._last_timer_notify_at = now
        if hasattr(self, "tray") and self.tray:
            self.tray.show_message(t("timer.notify.title"), t("timer.notify.body"))

    @Slot()
    def _on_timer_background_mode(self):
        if hasattr(self, "tray") and self.tray:
            self.tray.show_message(t("timer.background.title"), t("timer.background.body"))
    
    @Slot()
    def on_slideshow_start(self):
        self._slideshow_running = True
        try:
            self.overlay.on_slideshow_start_cleanup()
        except Exception:
            pass
        # Cleanup slide thumbnails from previous session
        temp_dir = os.path.join(tempfile.gettempdir(), "kazuha_ppt_thumbs")
        if os.path.exists(temp_dir):
            try:
                shutil.rmtree(temp_dir)
            except Exception:
                pass
        try:
            self._focus_watcher.set_slideshow_running(True)
        except Exception:
            pass
    
    @Slot()
    def on_slideshow_end(self):
        self._slideshow_running = False
        try:
            self.overlay.on_slideshow_end_cleanup()
        except Exception:
            pass
        try:
            self.overlay.set_active_on_slideshow(False, animate=False)
        except Exception:
            pass
        try:
            self._focus_watcher.set_slideshow_running(False)
        except Exception:
            pass

    @Slot(object, object)
    def _cache_slideshow_geometry(self, rect, screen):
        try:
            if rect is not None and hasattr(rect, "isEmpty") and not rect.isEmpty():
                self._last_slideshow_rect = rect
                self._last_slideshow_screen = screen
        except Exception:
            pass

    @Slot(bool)
    def _on_focus_on_slideshow_changed(self, focused: bool):
        try:
            if not self._slideshow_running or not cfg.autoShowOverlay.value:
                self.overlay.set_active_on_slideshow(False, animate=True)
                return
            if focused and self._last_slideshow_rect is not None:
                try:
                    self.overlay.update_geometry(self._last_slideshow_rect, self._last_slideshow_screen)
                except Exception:
                    pass
            self.overlay.set_active_on_slideshow(bool(focused), animate=True)
        except Exception:
            pass

    def _check_settings_changed(self):
        if not os.path.exists(SETTINGS_PATH):
            return
        mtime = os.path.getmtime(SETTINGS_PATH)
        if mtime != self._settings_mtime:
            self._settings_mtime = mtime
            
            # Check for restart flag
            try:
                with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                    temp_data = json.load(f)
                if temp_data.get("_restart_pending"):
                    del temp_data["_restart_pending"]
                    with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
                        json.dump(temp_data, f, indent=4, ensure_ascii=False)
                    self.restart()
                    return
            except Exception:
                pass
            
            old_theme = cfg.themeMode.value
            old_theme_id = cfg.themeId.value if hasattr(cfg, "themeId") else "default"
            old_lang = getattr(self, "_current_language", "zh-CN")
            old_qt_font = getattr(self, "_current_qt_font", "")
            old_overlay_font = getattr(self, "_current_overlay_font", "")
            old_toolbar_text = cfg.showToolbarText.value
            old_status_bar = cfg.showStatusBar.value
            old_clear = cfg.showClear.value
            old_spotlight = cfg.showSpotlight.value
            old_timer = cfg.showTimer.value
            old_toolbar_order = cfg.toolbarOrder.value
            old_safe_area = cfg.safeArea.value
            old_scale = cfg.scale.value
            old_overlay_screen = cfg.overlayScreen.value
            old_rebuild_at = getattr(self, "_overlay_rebuild_at", None)
            old_compat = cfg.compatibilityMode.value
            # old_layout_mode = cfg.toolbarLayout.value

            reload_cfg()

            data = _load_settings_json()
            if cfg.overlayScreen.value != old_overlay_screen:
                try:
                    self.monitor.force_update_geometry()
                except Exception:
                    pass

            new_lang = (data.get("General", {}) or {}).get("Language", "zh-CN")
            profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
            lang_profile = profiles.get(new_lang, {}) or {}
            qt_font = lang_profile.get("qt", "")
            overlay_font = lang_profile.get("overlay", "") or qt_font
            new_qt_font = qt_font.strip() if isinstance(qt_font, str) else ""
            new_overlay_font = overlay_font.strip() if isinstance(overlay_font, str) else ""
            new_rebuild_at = (data.get("Overlay", {}) or {}).get("RecreateOverlayAt")

            self._current_language = new_lang
            self._current_qt_font = new_qt_font
            self._current_overlay_font = new_overlay_font

            if new_qt_font != old_qt_font:
                _apply_global_font(self.app)
            
            should_reload = (
                new_lang != old_lang
                or new_overlay_font != old_overlay_font
                or cfg.themeMode.value != old_theme
                or (hasattr(cfg, "themeId") and cfg.themeId.value != old_theme_id)
                or cfg.showClear.value != old_clear
                or cfg.showSpotlight.value != old_spotlight
                or cfg.showTimer.value != old_timer
                or cfg.showToolbarText.value != old_toolbar_text
                or cfg.toolbarOrder.value != old_toolbar_order
                or cfg.safeArea.value != old_safe_area
                or cfg.scale.value != old_scale
            )
            
            if should_reload:
                if not self._reloading_overlay:
                    self._reload_timer.start()
            else:
                if self.overlay:
                    if new_rebuild_at and new_rebuild_at != old_rebuild_at:
                        if not self._reloading_overlay:
                            self._reload_timer.start()
                    if cfg.showStatusBar.value != old_status_bar:
                        self.overlay._on_status_bar_visibility_changed(cfg.showStatusBar.value)

                    if cfg.compatibilityMode.value != old_compat:
                        self.overlay.update_config()
                        if cfg.compatibilityMode.value:
                            self.overlay.show()
                        elif not self._slideshow_running:
                            self.overlay.hide()
            
            # Layout mode change is now handled by auto-reload above, no restart prompt needed
            
            if cfg.themeMode.value != old_theme:
                if hasattr(self, 'tray'):
                    self.tray._update_icon()

            if new_lang != old_lang or cfg.compatibilityMode.value != old_compat:
                if hasattr(self, 'tray'):
                    self.tray.refresh_menu()
            
            if new_rebuild_at is not None:
                self._overlay_rebuild_at = new_rebuild_at

    def _reload_overlay(self):
        """Recreate the overlay window to apply language and layout changes."""
        if self._reloading_overlay:
            return
        self._reloading_overlay = True
        was_visible = self.overlay.isVisible()
        
        try:
            # Import overlay again to refresh module-level LANGUAGE
            import ppt_assistant.ui.overlay as overlay_mod
            importlib.reload(overlay_mod)
            from ppt_assistant.ui.overlay import OverlayWindow
            
            # Create new overlay first (prevent crash if creation fails)
            new_overlay = OverlayWindow()
            new_overlay.set_monitor(self.monitor)
            
            # Re-connect signals
            new_overlay.request_next.connect(self.monitor.go_next)
            new_overlay.request_prev.connect(self.monitor.go_previous)
            new_overlay.request_goto.connect(self.monitor.go_to_slide)
            new_overlay.request_clear.connect(self.monitor.clear_screen)
            new_overlay.request_end.connect(self.monitor.end_show)
            new_overlay.request_ptr_arrow.connect(lambda: self.monitor.set_pointer_type(1))
            new_overlay.request_ptr_pen.connect(lambda: self.monitor.set_pointer_type(2))
            new_overlay.request_ptr_eraser.connect(lambda: self.monitor.set_pointer_type(5))
            new_overlay.request_pen_color.connect(self.monitor.set_pen_color)
            new_overlay.request_thumbnail.connect(lambda idx: self.monitor.export_slide_thumbnail(idx, os.path.join(tempfile.gettempdir(), "kazuha_ppt_thumbs", f"thumb_{idx}.png")))
            
            # Disconnect old overlay slots before connecting new ones
            with warnings.catch_warnings():
                warnings.simplefilter("ignore", RuntimeWarning)
                try:
                    self.monitor.slide_changed.disconnect(self.overlay.update_page_info)
                except Exception:
                    pass
                try:
                    self.monitor.window_geometry_changed.disconnect(self.overlay.update_geometry)
                except Exception:
                    pass
                try:
                    self.monitor.slideshow_hwnd_changed.disconnect(self.overlay.set_slideshow_hwnd)
                except Exception:
                    pass
                try:
                    self.monitor.thumbnail_generated.disconnect(self.overlay.on_thumbnail_ready)
                except Exception:
                    pass
            self.monitor.slide_changed.connect(new_overlay.update_page_info)
            self.monitor.window_geometry_changed.connect(new_overlay.update_geometry)
            self.monitor.slideshow_hwnd_changed.connect(new_overlay.set_slideshow_hwnd)
            self.monitor.thumbnail_generated.connect(new_overlay.on_thumbnail_ready)
            
            # Swap overlay
            old_overlay = self.overlay
            self.overlay = new_overlay
            
            # Cleanup old overlay
            old_overlay.cleanup() # Stop threads safely
            old_overlay.hide()
            old_overlay.deleteLater()
            
            if was_visible:
                self.overlay.show()
                self.overlay.raise_()
            
            # Update current page info immediately
            if self.monitor:
                curr, total = self.monitor.get_page_info()
                self.overlay.update_page_info(curr, total)
                self.monitor.force_update_geometry()
                
        except Exception as e:
            print(f"Error reloading overlay: {e}")
            # If failed, keep using the old overlay if it's still alive
            if was_visible and not self.overlay.isVisible():
                 self.overlay.show()
        finally:
            self._reloading_overlay = False

    def restart(self):
        self.cleanup()
        os.execl(sys.executable, sys.executable, *sys.argv)

    def cleanup(self):
        """Cleanup app resources and terminate subprocesses."""
        if hasattr(self, 'monitor'):
            self.monitor.stop_monitoring()
        try:
            if hasattr(self, "_focus_watcher") and self._focus_watcher:
                self._focus_watcher.stop()
        except Exception:
            pass
        if hasattr(self, 'settings_plugin'):
            self.settings_plugin.terminate()
        if hasattr(self, 'overlay'):
            self.overlay.cleanup()

    def run(self):
        # sys.exit(self.app.exec())
        pass


if __name__ == "__main__":
    if sys.platform == "linux":
        # Force Qt to use xcb on Linux to avoid issues with custom platform plugins like dxcb (Deepin)
        if "QT_QPA_PLATFORM" not in os.environ:
            os.environ["QT_QPA_PLATFORM"] = "xcb"
        # Add --no-sandbox to avoid zygote crash on some Linux environments
        if "--no-sandbox" not in sys.argv:
            sys.argv.append("--no-sandbox")

    _apply_graphics_settings()
    QCoreApplication.setAttribute(Qt.AA_ShareOpenGLContexts)
    app = QApplication(sys.argv)
    app_icon = load_app_icon()
    if not app_icon.isNull():
        app.setWindowIcon(app_icon)
        app._window_icon_filter = WindowIconEventFilter(app_icon)
        app.installEventFilter(app._window_icon_filter)
    _apply_global_font(app)
    crash_handler = CrashHandler(app)
    _handle_multi_instance(app)

    show_splash = True
    try:
        mode = cfg.splashMode.value
        if mode == "Never":
            show_splash = False
        elif mode == "HideOnAutoStart":
            args = [a.lower() for a in sys.argv]
            if "--autostart" in args or "-autostart" in args or "--silent" in args:
                show_splash = False
        elif mode == "TimeRange":
            from PySide6.QtCore import QTime
            start_str = cfg.splashStartTime.value
            end_str = cfg.splashEndTime.value
            start_t = QTime.fromString(start_str, "HH:mm")
            end_t = QTime.fromString(end_str, "HH:mm")
            now = QTime.currentTime()
            
            if start_t.isValid() and end_t.isValid():
                if start_t <= end_t:
                    if not (start_t <= now <= end_t):
                        show_splash = False
                else:
                    if not (now >= start_t or now <= end_t):
                        show_splash = False
    except Exception as e:
        print(f"Error determining splash visibility: {e}")
        show_splash = True

    splash = None
    if show_splash:
        splash = StartupSplash()
        splash.show()
        app.processEvents()

    app_instance = PPTAssistantApp(app, splash)
    crash_handler.set_app_instance(app_instance)
    sys.exit(app.exec())
