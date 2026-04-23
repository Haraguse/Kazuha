import shutil
import sys
import os

# Nuitka standalone detection and compatibility
if hasattr(sys, "nuitka_binary"):
    sys.frozen = True

import traceback
import tempfile
import subprocess
import json
import importlib
import importlib.util
from typing import Optional
import time
import warnings

if sys.platform == "linux":
    _HAS_X11_DISPLAY = bool(os.environ.get("DISPLAY"))
    _HAS_WAYLAND_DISPLAY = bool(os.environ.get("WAYLAND_DISPLAY"))
    _HAS_DISPLAY = _HAS_X11_DISPLAY or _HAS_WAYLAND_DISPLAY
    _LINUX_QPA_OVERRIDE = str(os.environ.get("LUMINALIUM_QPA_PLATFORM", "")).strip()
    _FORCE_X11 = str(os.environ.get("LUMINALIUM_FORCE_X11", "")).strip().lower() in (
        "1",
        "true",
        "yes",
    )
    if "QT_QPA_PLATFORM" not in os.environ:
        if _LINUX_QPA_OVERRIDE:
            os.environ["QT_QPA_PLATFORM"] = _LINUX_QPA_OVERRIDE
        elif _HAS_X11_DISPLAY:
            os.environ["QT_QPA_PLATFORM"] = "xcb"
            if _FORCE_X11:
                print(
                    "[Main] LUMINALIUM_FORCE_X11 is set, using X11/XWayland for WPS RPC compatibility"
                )
        elif _HAS_WAYLAND_DISPLAY:
            os.environ["QT_QPA_PLATFORM"] = "wayland"
        else:
            os.environ["QT_QPA_PLATFORM"] = "offscreen"
    _SELECTED_QPA_PLATFORM = str(os.environ.get("QT_QPA_PLATFORM", "")).strip().lower()
    if _SELECTED_QPA_PLATFORM.startswith("xcb") and _HAS_X11_DISPLAY:
        if _HAS_WAYLAND_DISPLAY:
            os.environ["LUMINALIUM_XWAYLAND_SESSION"] = "1"
            original_wayland_display = os.environ.get("WAYLAND_DISPLAY")
            if original_wayland_display:
                os.environ["LUMINALIUM_ORIGINAL_WAYLAND_DISPLAY"] = (
                    original_wayland_display
                )
                os.environ.pop("WAYLAND_DISPLAY", None)
            if "LUMINALIUM_ORIGINAL_XDG_SESSION_TYPE" not in os.environ:
                current_session_type = os.environ.get("XDG_SESSION_TYPE")
                os.environ["LUMINALIUM_ORIGINAL_XDG_SESSION_TYPE"] = (
                    current_session_type if current_session_type is not None else ""
                )
            os.environ["XDG_SESSION_TYPE"] = "x11"
            print(
                "[Main] Normalized Linux environment for QtWebEngine:"
                " xcb session will hide WAYLAND_DISPLAY and force XDG_SESSION_TYPE=x11",
                flush=True,
            )
        else:
            os.environ.pop("LUMINALIUM_XWAYLAND_SESSION", None)
    print(
        "[Main] Linux display detection:"
        f" wayland={_HAS_WAYLAND_DISPLAY}"
        f" x11={_HAS_X11_DISPLAY}"
        f" qpa={os.environ.get('QT_QPA_PLATFORM', '')}"
        f" force_x11={_FORCE_X11}"
    )
    # Add --no-sandbox to avoid zygote crash on some Linux environments
    # This must be done BEFORE any Qt import or QApp creation
    if "--no-sandbox" not in sys.argv:
        sys.argv.append("--no-sandbox")

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

from PySide6.QtWidgets import (
    QApplication,
    QWidget,
    QLabel,
    QFrame,
    QGraphicsDropShadowEffect,
    QProgressBar,
)
from PySide6.QtCore import Qt, QTimer, Slot, QPoint, QCoreApplication, QEvent, QObject
from PySide6.QtGui import (
    QFontDatabase,
    QFont,
    QColor,
    QIcon,
    QPainter,
    QPen,
    QBrush,
    QFontMetrics,
)

from ppt_assistant.core.ppt_monitor import PPTMonitor
from ppt_assistant.ui.overlay import create_overlay_window
from ppt_assistant.ui.tray import SystemTray, is_system_tray_supported
from ppt_assistant.core.config import (
    cfg,
    SETTINGS_PATH,
    PLUGINS_DIR,
    reload_cfg,
    _apply_theme_and_color,
    Theme,
    FIRST_RUN,
    ROOT_DIR,
)
from ppt_assistant.core.timer_manager import TimerManager
from ppt_assistant.core.i18n import t
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.linux_focus_watcher import LinuxFocusWatcher
from ppt_assistant.core.win_focus_watcher import WindowsFocusWatcher
from ppt_assistant.core.resource_monitor import SystemResourceMonitor


class WindowIconEventFilter(QObject):
    def __init__(self, icon: QIcon):
        super().__init__()
        self._icon = icon

    def eventFilter(self, obj, event):
        if self._icon.isNull():
            return False
        try:
            if event.type() in (QEvent.Show, QEvent.Polish):
                if (
                    isinstance(obj, QWidget)
                    and obj.isWindow()
                    and obj.windowIcon().isNull()
                ):
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
        "dev_watermark": "{type}\n不保证最终品质 （{version}）",
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
        "dev_watermark": "{type}\n不保證最終品質 （{version}）",
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
        "dev_watermark": "{type}\n品質唔包，出事唔好屌我 （{version}）",
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
        "dev_watermark": "{type}\n品質は保証されません （{version}）",
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
        "dev_watermark": "{type}\nFinal quality not guaranteed ({version})",
    },
}


def _is_windows7():
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
        rate = ctypes.windll.gdi32.GetDeviceCaps(hdc, 116)  # VREFRESH
        user32.ReleaseDC(0, hdc)
        return rate if rate > 1 else 60
    except:
        return 60


def _env_flag_enabled(name: str, default: bool = False) -> bool:
    raw = os.environ.get(name)
    if raw is None:
        return default
    return str(raw).strip().lower() in {"1", "true", "yes", "on"}


def _is_compatibility_mode_enabled() -> bool:
    try:
        data = _load_settings_json()
        general = data.get("General", {}) if isinstance(data, dict) else {}
        return (
            bool(general.get("CompatibilityMode", False))
            if isinstance(general, dict)
            else False
        )
    except Exception:
        return False


def _apply_graphics_settings():
    if sys.platform == "linux":
        qpa_platform = str(os.environ.get("QT_QPA_PLATFORM", "")).strip().lower()
        has_x11_display = bool(os.environ.get("DISPLAY"))
        os.environ["QTWEBENGINE_DISABLE_SANDBOX"] = "1"
        flags = [
            "--disable-gpu",
            "--disable-gpu-compositing",
            "--enable-software-rasterizer",
            "--no-sandbox",
        ]
        if qpa_platform == "xcb" and has_x11_display:
            flag = "--disable-features=UseOzonePlatform"
            if flag not in flags:
                flags.append(flag)
        current = os.environ.get("QTWEBENGINE_CHROMIUM_FLAGS", "").strip()
        merged = current.split()
        for flag in flags:
            if flag not in merged:
                merged.append(flag)
        os.environ["QTWEBENGINE_CHROMIUM_FLAGS"] = " ".join(merged)
        print(
            "[Main] Linux graphics settings:"
            f" qpa={qpa_platform or 'unset'}"
            f" flags={os.environ.get('QTWEBENGINE_CHROMIUM_FLAGS', '')}",
            flush=True,
        )
        return

    # Configure rendering backend for best performance
    # Use native OpenGL for smooth rendering
    os.environ["QT_OPENGL"] = "desktop"
    os.environ["QT_VULKAN_DISABLE"] = "1"
    # Let Qt automatically choose the best RHI backend
    # Don't force QSG_RHI_BACKEND to allow fallback

    use_software_webengine = _is_compatibility_mode_enabled() or _env_flag_enabled(
        "LUMINALIUM_WEBENGINE_SOFTWARE", False
    )

    if use_software_webengine:
        os.environ["QSG_RHI_BACKEND"] = "software"
        os.environ["QT_QUICK_BACKEND"] = "software"
        os.environ["QT_OPENGL"] = "software"
        os.environ["QTWEBENGINE_DISABLE_GPU"] = "1"
        flags = [
            "--disable-gpu",
            "--disable-gpu-compositing",
            "--disable-gpu-rasterization",
            "--disable-software-rasterizer",
        ]
    else:
        # Base flags tuned for smoother rendering
        flags = [
            "--disable-frame-rate-limit",
            "--disable-gpu-vsync",
            "--enable-gpu-rasterization",
            "--enable-zero-copy",
            "--enable-features=VaapiVideoDecoder,VaapiVideoEncoder",
            "--ignore-gpu-blocklist",
            "--enable-hardware-overlays",
        ]

        # Get refresh rate for target FPS
        rate = _get_screen_refresh_rate()
        target_fps = rate * 3
        os.environ["LUMINALIUM_TARGET_FPS"] = str(target_fps)

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


def _should_enable_system_tray() -> bool:
    value = str(os.environ.get("LUMINALIUM_ENABLE_TRAY", "")).strip().lower()
    if value in ("0", "false", "no", "off"):
        return False
    if value in ("1", "true", "yes", "on"):
        return True
    return is_system_tray_supported()


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


def _create_focus_watcher(parent):
    if sys.platform == "linux":
        return LinuxFocusWatcher(parent)
    return WindowsFocusWatcher(parent)


def _get_settings_reset_marker_path():
    return os.path.join(os.path.dirname(SETTINGS_PATH), "settings.reset")


def _get_restart_marker_path():
    return os.path.join(os.path.dirname(SETTINGS_PATH), "restart.marker")


def _write_restart_marker():
    try:
        path = _get_restart_marker_path()
        data = {"pid": os.getpid(), "ts": time.time()}
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False)
    except Exception:
        pass


def _consume_restart_marker(max_age_seconds: float = 5.0) -> bool:
    path = _get_restart_marker_path()
    if not os.path.exists(path):
        return False
    data = {}
    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception:
        data = {}
    finally:
        try:
            os.remove(path)
        except Exception:
            pass
    try:
        ts = float(data.get("ts", 0))
    except Exception:
        ts = 0.0
    if ts <= 0:
        return False
    return (time.time() - ts) <= max_age_seconds


def _get_current_language():
    data = _load_settings_json()
    return data.get("General", {}).get("Language", "zh-CN")


def _normalize_font_weight_value(value):
    if isinstance(value, bool):
        return None
    if isinstance(value, float):
        if not value.is_integer():
            return None
        value = int(value)
    if isinstance(value, int):
        num = value
    elif isinstance(value, str):
        text = value.strip()
        if not text.isdigit():
            return None
        num = int(text)
    else:
        return None
    if num < 100 or num > 1000 or num % 100 != 0:
        return None
    return num


def _get_font_weight_from_settings(
    data, lang: str, scene: str, fallback_scene: str = ""
):
    fonts = data.get("Fonts", {}) or {}
    weights = fonts.get("Weights", {}) or {}
    lang_weights = weights.get(lang, {}) or {}
    value = _normalize_font_weight_value(lang_weights.get(scene))
    if value is not None:
        return value
    if fallback_scene:
        return _normalize_font_weight_value(lang_weights.get(fallback_scene))
    return None


_BUNDLED_FONT_FILES = {
    "google_sans_flex": "Google Sans Flex.ttf",
    "misans_vf": "MiSansVF.ttf",
    "misans_japanese_vf": "MiSansJapaneseVF.ttf",
    "misans_tc_vf": "MiSansTCVF.ttf",
}


def _dedupe_font_families(families):
    seen = set()
    ordered = []
    for family in families or []:
        if not isinstance(family, str):
            continue
        name = family.strip()
        if not name or name in seen:
            continue
        seen.add(name)
        ordered.append(name)
    return ordered


def _load_bundled_font_families(root_dir: str):
    families = {}
    fonts_dir = os.path.join(root_dir, "fonts")
    for key, file_name in _BUNDLED_FONT_FILES.items():
        font_path = os.path.join(fonts_dir, file_name)
        if not os.path.exists(font_path):
            continue
        try:
            font_id = QFontDatabase.addApplicationFont(font_path)
            if font_id == -1:
                continue
            loaded = QFontDatabase.applicationFontFamilies(font_id)
            if loaded:
                families[key] = loaded[0]
        except Exception:
            continue
    return families


def _get_default_font_family_stack(lang: str, bundled_families=None):
    if bundled_families is None:
        bundled_families = {}
    google = bundled_families.get("google_sans_flex", "Google Sans Flex")
    misans = bundled_families.get("misans_vf", "MiSans VF")
    misans_jp = bundled_families.get("misans_japanese_vf", "MiSans Japanese VF")
    misans_tc = bundled_families.get("misans_tc_vf", "MiSans TC VF")

    if lang in ("zh-TW", "ja-JP"):
        return _dedupe_font_families([google, misans_jp, misans_tc, "Segoe UI"])
    if lang == "ug-CN":
        return _dedupe_font_families([google, "Segoe UI", misans])
    return _dedupe_font_families([google, misans, "Segoe UI"])


def _build_css_font_family_value(families):
    parts = []
    for family in _dedupe_font_families(families):
        safe = family.replace("\\", "\\\\").replace("'", "\\'")
        parts.append(f"'{safe}'")
    if not parts:
        parts.append("sans-serif")
    else:
        parts.append("sans-serif")
    return ", ".join(parts)


def _apply_global_font(app: QApplication):
    root_dir = os.path.dirname(os.path.abspath(__file__))
    selected_family = ""
    data = _load_settings_json()
    lang = data.get("General", {}).get("Language", "zh-CN")
    profiles = (data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
    v = (profiles.get(lang, {}) or {}).get("qt", "")
    if isinstance(v, str) and v.strip():
        selected_family = v.strip()

    bundled_families = _load_bundled_font_families(root_dir)
    default_families = _get_default_font_family_stack(lang, bundled_families)

    if selected_family:
        font = QFont(selected_family)
    else:
        if not default_families:
            return
        font = QFont()
        font.setStyleHint(QFont.SansSerif)
        if default_families:
            font.setFamily(default_families[0])
        font.setFamilies(default_families)
    weight = _get_font_weight_from_settings(data, lang, "qt")
    if weight is not None:
        font.setWeight(weight)
    app.setFont(font)


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
                "SubaruAwa": "Subaru Awa",
                "TomoEbizuka": "Tomo Ebizuka",
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


def _get_user_root_dir() -> str:
    return ROOT_DIR


def _ensure_user_dirs():
    """Ensure user directories exist for themes and splash screens."""
    try:
        root_dir = _get_user_root_dir()
        # Prefer "user" but check for "users"
        user_dir = os.path.join(root_dir, "user")
        if not os.path.exists(user_dir) and os.path.exists(
            os.path.join(root_dir, "users")
        ):
            user_dir = os.path.join(root_dir, "users")

        if not os.path.exists(user_dir):
            os.makedirs(user_dir)

        for sub in ["themes", "splash"]:
            path = os.path.join(user_dir, sub)
            if not os.path.exists(path):
                os.makedirs(path)
    except Exception as e:
        print(f"Error ensuring user directories: {e}")


def _resolve_user_splash_dir(splash_style: str) -> Optional[str]:
    if not splash_style:
        return None
    root_dir = _get_user_root_dir()
    # Support both "user" and "users" folder names
    splash_dir = os.path.join(root_dir, "user", "splash", splash_style)
    if not os.path.exists(splash_dir):
        splash_dir = os.path.join(root_dir, "users", "splash", splash_style)

    if os.path.exists(splash_dir):
        return splash_dir
    return None


def _is_valid_splash_package(splash_dir: str, splash_style: str) -> bool:
    if not splash_dir or not os.path.isdir(splash_dir):
        return False
    manifest_path = os.path.join(splash_dir, "manifest.json")
    preview_png = os.path.join(splash_dir, "preview.png")
    preview_jpg = os.path.join(splash_dir, "preview.jpg")
    splash_path = os.path.join(splash_dir, "splash.py")
    if not os.path.exists(splash_path):
        return False
    if not os.path.exists(manifest_path):
        return False
    if not (os.path.exists(preview_png) or os.path.exists(preview_jpg)):
        return False
    try:
        with open(manifest_path, "r", encoding="utf-8-sig") as f:
            data = json.load(f)
        # Remove strict name check to allow more flexible splash naming
        # if data.get("name") != splash_style:
        #     return False
    except Exception:
        return False
    return True


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

        applied_user = False
        if not (self._splash_style == "nina_iseri_1_2" and self._is_first_run):
            applied_user = self._apply_user_splash()

        if not applied_user:
            if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
                self._build_ui_nina()
            else:
                self._container = QFrame(self)
                self._container.setObjectName("splashContainer")
                self._build_ui()
                self._apply_styles()

        self._center_on_screen()
        self.set_progress(0, "initializing")

        if (
            self._splash_style != "nina_iseri_1_2"
            and not self._is_first_run
            and _is_dev_preview_version(self._version_raw)
        ):
            self._dev_watermark = QLabel(self._container)
            i18n_table = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"])
            suffix = self._version_raw.split(".")[-1]
            w_type = i18n_table.get(f"watermark.{suffix}", "")
            tmpl = i18n_table.get("dev_watermark", "")
            self._dev_watermark.setText(
                tmpl.format(type=w_type, version=self._version_text)
            )
            font = QFont()
            font.setPixelSize(11)
            self._dev_watermark.setFont(font)
            self._dev_watermark.setAlignment(Qt.AlignRight | Qt.AlignBottom)

            watermark_color = (
                "rgba(255, 255, 255, 100)" if self._is_dark else "rgba(0, 0, 0, 100)"
            )
            self._dev_watermark.setStyleSheet(f"color: {watermark_color};")

            self._dev_watermark.resize(320, 36)
            self._dev_watermark.move(
                self._container.width() - self._dev_watermark.width() - 16,
                self._container.height() - self._dev_watermark.height() - 12,
            )

    def _build_ui_nina(self):
        root_dir = _get_user_root_dir()
        icon_path = os.path.join(
            root_dir, "user", "splash", "nina_iseri_1_2", "1.2_Splash.png"
        )
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

    def _apply_user_splash(self) -> bool:
        splash_dir = _resolve_user_splash_dir(self._splash_style)
        if not splash_dir or not _is_valid_splash_package(
            splash_dir, self._splash_style
        ):
            return False
        module_path = os.path.join(splash_dir, "splash.py")
        try:
            module_name = f"user_splash_{self._splash_style}"
            spec = importlib.util.spec_from_file_location(module_name, module_path)
            if not spec or not spec.loader:
                return False
            module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)
            apply_fn = getattr(module, "apply", None)
            if not callable(apply_fn):
                return False
            apply_fn(self)
            return True
        except Exception:
            return False

    def paintEvent(self, event):
        if (
            self._splash_style == "nina_iseri_1_2"
            and not self._is_first_run
            and self._pixmap
        ):
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
            splash_font_families = _get_default_font_family_stack(
                self._language, _load_bundled_font_families(_get_user_root_dir())
            )
            title_font = QFont()
            title_font.setStyleHint(QFont.SansSerif)
            if splash_font_families:
                title_font.setFamily(splash_font_families[0])
            title_font.setFamilies(splash_font_families)
            title_font.setPixelSize(36)
            title_font.setBold(True)

            sub_font = QFont()
            sub_font.setStyleHint(QFont.SansSerif)
            if splash_font_families:
                sub_font.setFamily(splash_font_families[0])
            sub_font.setFamilies(splash_font_families)
            sub_font.setPixelSize(14)

            # Calculate positions from bottom
            h = self.height()
            w = self.width()

            # Reduce width to ~65% to avoid character more aggressively
            content_width = w * 0.65

            progress_h = 6
            progress_y = h - margin_bottom - progress_h

            # Title "Luminalium"
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
            title_baseline_y = subtitle_baseline_y - sub_height - 16  # 20px gap

            # Draw Title
            brand_name_map = {
                "zh-CN": "Luminalium",
                "zh-TW": "Luminalium",
                "yue-HK": "Luminalium",
                "ja-JP": "ルマイナリウム",
                "en-US": "Luminalium",
            }
            brand_name = brand_name_map.get(self._language, "Luminalium")

            painter.setFont(title_font)
            painter.setPen(QColor("#000000"))
            painter.drawText(margin_left, title_baseline_y, brand_name)

            # Draw Subtitle
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
            painter.drawRoundedRect(
                margin_left, progress_y, content_width, progress_h, 3, 3
            )

            # Progress Bar Value
            if self._progress_value > 0:
                painter.setBrush(QColor("#404040"))
                prog_width = content_width * (self._progress_value / 100.0)
                painter.drawRoundedRect(
                    margin_left, progress_y, prog_width, progress_h, 3, 3
                )

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
            icon_path = os.path.join(
                os.path.dirname(os.path.abspath(__file__)), "icons", "logo.svg"
            )
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
        self._container.move(24, 16)  # Offset for shadow

        # Logo (kZHTXT_2.png equivalent) - x: 38, y: 37
        self._icon_label = QLabel(self._container)
        self._icon_label.setFixedSize(64, 64)
        icon_path = os.path.join(
            os.path.dirname(os.path.abspath(__file__)), "icons", "logo.svg"
        )
        if os.path.exists(icon_path):
            icon = QIcon(icon_path)
            pix = icon.pixmap(64, 64)
            self._icon_label.setPixmap(pix)
        self._icon_label.move(38, 37)

        brand_name_map = {
            "zh-CN": "Luminalium",
            "zh-TW": "Luminalium",
            "yue-HK": "Luminalium",
            "ja-JP": "ルマイナリウム",
            "en-US": "Luminalium",
        }
        brand_name = brand_name_map.get(self._language, "Luminalium")
        self._brand_label = QLabel(brand_name, self._container)
        splash_font_families = _get_default_font_family_stack(
            self._language, _load_bundled_font_families(_get_user_root_dir())
        )
        brand_font = QFont()
        if splash_font_families:
            brand_font.setFamily(splash_font_families[0])
        brand_font.setFamilies(splash_font_families)
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
        en_color = (
            "rgba(255, 255, 255, 0.47)" if self._is_dark else "rgba(0, 0, 0, 0.47)"
        )

        version_font_css = _build_css_font_family_value(splash_font_families)
        html = f"""
        <div style="line-height: 20px;">
            <span style="font-family: {version_font_css}; font-size: 11px; font-weight: 500; color: {ver_color};">{ver_text}</span>
            <span style="font-family: {version_font_css}; font-size: 11px; font-weight: 300; color: {en_color}; margin-left: 2px;">{en_text}</span>
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
        init_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"])[
            "initializing"
        ]
        self._percent_label = QLabel(f"{init_text} 0%", self._container)
        percent_font = QFont()
        percent_font.setFamilies(splash_font_families)
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

        self.resize(678 + 48, 255 + 48)  # Increased for shadow

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
            display_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"]).get(
                text_key, text_key
            )
        else:
            # Always show "initializing" text if details are disabled
            init_key = "initializing"
            display_text = SPLASH_I18N.get(self._language, SPLASH_I18N["zh-CN"]).get(
                init_key, init_key
            )

        full_text = f"{display_text} {value}%"

        # Nina style
        if self._splash_style == "nina_iseri_1_2" and not self._is_first_run:
            self._progress_value = value
            self._status_text = full_text
            self.update()
            QApplication.processEvents()
            return

        # For first run splash (simple logo), we don't show progress
        if not hasattr(self, "_progress") or not hasattr(self, "_percent_label"):
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

        if hasattr(self, "_progress"):
            self._progress.setValue(100)
        if hasattr(self, "_percent_label"):
            self._percent_label.setText("初始化完成 100%")
        if hasattr(self, "_spinner"):
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
        self._timer.start(16)  # ~60 FPS

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
        orbit_radius = ring_radius - 3 - dot_radius + 1  # Fine tuned visual position

        painter.setPen(Qt.NoPen)
        painter.setBrush(QBrush(self._color))
        painter.drawEllipse(QPoint(0, -orbit_radius), dot_radius, dot_radius)

        painter.restore()
        painter.end()


def show_webview_dialog(
    title,
    text,
    confirm_text="纭",
    cancel_text="鍙栨秷",
    is_error=False,
    hide_cancel=False,
    code=None,
):
    from ppt_assistant.ui.dialog_runtime import show_webview_dialog_in_process

    return show_webview_dialog_in_process(
        title=title,
        text=text,
        confirm_text=confirm_text,
        cancel_text=cancel_text,
        is_error=is_error,
        hide_cancel=hide_cancel,
        code=code,
    )


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

    def _resolve_crash_action(self):
        mode = "ShowAnalyzer"
        enabled = False
        try:
            if hasattr(cfg, "crashAutoHandleMode"):
                mode = cfg.crashAutoHandleMode.value
        except Exception:
            mode = "ShowAnalyzer"
        try:
            if hasattr(cfg, "crashAutoHandleEnabled"):
                enabled = bool(cfg.crashAutoHandleEnabled.value)
        except Exception:
            enabled = False

        # Legacy fallback: if no explicit toggle exists, treat non-analyzer mode as enabled
        if not enabled and not hasattr(cfg, "crashAutoHandleEnabled"):
            if mode in ("Exit", "RestartSilent", "Toast"):
                enabled = True

        if not enabled:
            return "ShowAnalyzer"
        if mode in ("Exit", "RestartSilent", "Toast"):
            return mode
        return "RestartSilent"

    def _parse_crash_dialog_result(self, stdout: str):
        if not stdout:
            return None
        if "CRASH_DIALOG_IGNORED" in stdout:
            return "ignored"
        if "CRASH_DIALOG_EXIT" in stdout:
            return "exit"
        return None

    def _launch_crash_dialog(self, error_msg: str, wait_for_result: bool = False):
        try:
            base_dir = os.path.dirname(os.path.abspath(__file__))
            root_dir = base_dir
            main_path = os.path.join(root_dir, "main.py")
            env = os.environ.copy()
            env["CRASH_PARENT_PID"] = str(os.getpid())

            with tempfile.NamedTemporaryFile(
                mode="w", suffix=".log", delete=False, encoding="utf-8"
            ) as f:
                f.write(error_msg)
                temp_path = f.name

            creationflags = (
                0x08000000 | 0x00000008
            )  # CREATE_NO_WINDOW | DETACHED_PROCESS

            if getattr(sys, "frozen", False):
                cmd = [sys.executable, "--webview-runner", "--crash-file", temp_path]
            else:
                cmd = [
                    sys.executable,
                    main_path,
                    "--webview-runner",
                    "--crash-file",
                    temp_path,
                ]

            if not wait_for_result:
                subprocess.Popen(
                    cmd, env=env, creationflags=creationflags, close_fds=True
                )
                return None

            proc = subprocess.Popen(
                cmd,
                env=env,
                creationflags=creationflags,
                close_fds=True,
                stdin=subprocess.DEVNULL,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                encoding="utf-8",
                errors="replace",
            )
            try:
                stdout, _ = proc.communicate()
            except Exception:
                return None
            return self._parse_crash_dialog_result(stdout)
        except Exception as e:
            print(f"Failed to launch crash dialog: {e}", file=sys.stderr)
        return None

    def _restart_silent(self):
        try:
            base_dir = os.path.dirname(os.path.abspath(__file__))
            main_path = os.path.join(base_dir, "main.py")
            filtered_args = [
                a
                for a in sys.argv[1:]
                if a not in ("--silent", "--webview-runner", "--dialog", "--crash-file")
            ]
            if "--silent" not in filtered_args:
                filtered_args.append("--silent")

            if getattr(sys, "frozen", False):
                cmd = [sys.executable] + filtered_args
            else:
                cmd = [sys.executable, main_path] + filtered_args

            creationflags = (
                0x08000000 | 0x00000008
            )  # CREATE_NO_WINDOW | DETACHED_PROCESS
            env = os.environ.copy()
            env["LUMINALIUM_RESTART"] = "1"
            env["LUMINALIUM_RESTART_PID"] = str(os.getpid())
            _write_restart_marker()
            subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
        except Exception as e:
            print(f"Failed to restart silently: {e}", file=sys.stderr)

    def _show_crash_toast(self):
        try:
            if (
                self.app_instance is not None
                and hasattr(self.app_instance, "tray")
                and self.app_instance.tray
            ):
                self.app_instance.tray.show_message(
                    t("crash.toast.title"), t("crash.toast.body")
                )
        except Exception as e:
            print(f"Failed to show crash toast: {e}", file=sys.stderr)

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

        error_msg = "".join(
            traceback.format_exception(exc_type, exc_value, exc_traceback)
        )
        print(f"CRASH DETECTED:\n{error_msg}", file=sys.stderr)

        action = self._resolve_crash_action()
        if action == "ShowAnalyzer":
            result = self._launch_crash_dialog(error_msg, wait_for_result=True)
            if result == "ignored":
                self._handling = False
                return
        elif action == "RestartSilent":
            self._restart_silent()
        elif action == "Toast":
            self._show_crash_toast()

        try:
            if self.app_instance is not None:
                self.app_instance.cleanup()
        except Exception as e:
            print(f"Error during crash cleanup: {e}", file=sys.stderr)

        import time

        time.sleep(0.5)
        os._exit(1)


def _handle_multi_instance(app: QApplication):
    if str(os.environ.get("LUMINALIUM_DISABLE_MULTI_INSTANCE", "")).strip().lower() in (
        "1",
        "true",
        "yes",
        "on",
    ):
        print(
            "[Main] Multi-instance check disabled by LUMINALIUM_DISABLE_MULTI_INSTANCE.",
            flush=True,
        )
        return

    try:
        import psutil
    except ImportError:
        return

    restart_flag = os.environ.pop("LUMINALIUM_RESTART", None)
    restart_marker = _consume_restart_marker()

    current_pid = os.getpid()
    parent_pid = os.getppid()
    current_entry = os.path.abspath(
        sys.executable if getattr(sys, "frozen", False) else __file__
    )
    pids = []
    for p in psutil.process_iter(["pid", "cmdline"]):
        pid = p.info.get("pid")
        if pid in (current_pid, parent_pid):
            continue
        cmd = p.info.get("cmdline") or []

        if "--webview-runner" in cmd:
            continue
        if cmd:
            launcher = os.path.basename(str(cmd[0])).lower()
            if launcher in ("uv", "uv.exe") and "run" in cmd:
                continue

        try:
            proc_cwd = p.cwd()
        except Exception:
            proc_cwd = None

        for part in cmd:
            try:
                if not isinstance(part, str) or not part:
                    continue
                if part.startswith("-"):
                    continue
                if os.path.isabs(part):
                    normalized = os.path.abspath(part)
                elif proc_cwd:
                    normalized = os.path.abspath(os.path.join(proc_cwd, part))
                else:
                    continue
                if normalized == current_entry:
                    pids.append(p.info.get("pid"))
                    break
            except Exception:
                continue

    if not pids:
        return

    print(f"[Main] Existing Luminalium instance candidates: {pids}", flush=True)

    if restart_flag or restart_marker:
        try:
            deadline = time.time() + 1.2
            alive = list(pids)
            while time.time() < deadline:
                alive = [pid for pid in alive if psutil.pid_exists(pid)]
                if not alive:
                    return
                time.sleep(0.05)
            for pid in alive:
                try:
                    p_obj = psutil.Process(pid)
                    p_obj.kill()
                except Exception:
                    pass
            return
        finally:
            os.environ.pop("LUMINALIUM_RESTART_PID", None)

    if sys.platform.startswith("linux"):
        print(
            "[Main] Existing instance detected on Linux; continuing without WebView multi-instance dialog.",
            flush=True,
        )
        return

    proc = show_webview_dialog(title="", text="", code="multi_instance")
    stdout, _ = proc.communicate()

    if 'DIALOG_VALUE:"RESTART_OLD"' in stdout:
        for pid in pids:
            try:
                p_obj = psutil.Process(pid)
                p_obj.kill()
            except Exception:
                pass
        time.sleep(0.5)
        return
    elif 'DIALOG_VALUE:"CONTINUE_NEW"' in stdout:
        return
    else:
        sys.exit(0)


def _t(key):
    return key  # Simple fallback if i18n is missing


class PPTAssistantApp:
    def __init__(self, app: QApplication, splash=None):
        self.app = app
        self.app.setQuitOnLastWindowClosed(False)
        self._splash = splash
        self.tray = None
        self._timer_manager = TimerManager()
        self._focus_watcher = _create_focus_watcher(self.app)
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
        self._onboarding_wait_timer = None
        self._onboarding_restart_started = False
        self._resource_monitor = None
        self._open_settings_after_startup = False

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
        qt_weight = _get_font_weight_from_settings(data, self._current_language, "qt")
        self._current_qt_font = qt_font.strip() if isinstance(qt_font, str) else ""
        self._current_overlay_font = (
            overlay_font.strip() if isinstance(overlay_font, str) else ""
        )
        self._current_qt_font_weight = qt_weight
        self._overlay_rebuild_at = (data.get("Overlay", {}) or {}).get(
            "RecreateOverlayAt"
        )

        self._settings_mtime = (
            os.path.getmtime(SETTINGS_PATH) if os.path.exists(SETTINGS_PATH) else 0
        )
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

        print("[Main] Creating overlay window...", flush=True)
        self.overlay = create_overlay_window()
        print(
            f"[Main] Overlay window created: {type(self.overlay).__name__}", flush=True
        )

        # Step 5: Plugins (IO/Process - expensive)
        yield 60, "loading_plugins"
        self._load_plugins()
        try:
            if FIRST_RUN and hasattr(self, "onboarding_plugin"):
                p = self.onboarding_plugin
                p.execute(preview=False)
                if self._splash:
                    QTimer.singleShot(200, self._splash.hide)
                self._start_onboarding_wait_loop()
                return
        except Exception:
            pass

        self._open_settings_after_startup = self._consume_open_settings_pending_flag()

        # Step 6: Tray (UI)
        yield 80, "init_tray"
        print("[Main] Initializing tray...", flush=True)
        if _should_enable_system_tray():
            self.tray = SystemTray()
        else:
            print(
                "[Main] System tray disabled or unavailable. Set LUMINALIUM_ENABLE_TRAY=1 to force-enable it."
            )
        print("[Main] Tray initialization finished.", flush=True)

        # Step 7: Finalize connections
        yield 85, "finalizing"
        print("[Main] Binding overlay to monitor...", flush=True)
        self.overlay.set_monitor(self.monitor)
        if hasattr(self.overlay, "set_timer_manager"):
            self.overlay.set_timer_manager(self._timer_manager)
        print("[Main] Overlay bound to monitor.", flush=True)

        yield 90, "finalizing"
        print("[Main] Connecting app signals...", flush=True)
        self._connect_signals()
        print("[Main] App signals connected.", flush=True)

        yield 95, "finalizing"
        print("[Main] Starting PPT monitor...", flush=True)
        self.monitor.start_monitoring()
        print(
            f"[Main] PPT monitor start requested. compatibilityMode={cfg.compatibilityMode.value}",
            flush=True,
        )
        self._start_resource_monitor()

        if cfg.compatibilityMode.value:
            print("[APP] Showing overlay in compatibility mode")
            self.overlay.show()

        if self._splash is not None:
            print("[Main] Finishing splash...", flush=True)
            self._splash.finish()
            print("[Main] Splash finished.", flush=True)
        if self._open_settings_after_startup and hasattr(self, "settings_plugin"):
            QTimer.singleShot(200, self.settings_plugin.execute)

    def _perform_init_step(self):
        try:
            progress, text = next(self._init_gen)
            self.update_splash(progress, text)
            # Schedule next step immediately but allow event loop to breathe
            QTimer.singleShot(0, self._perform_init_step)
        except StopIteration:
            pass  # Done
        except Exception as e:
            print(f"Initialization error: {e}")
            sys.exit(1)

    def _start_onboarding_wait_loop(self):
        if self._onboarding_wait_timer is not None:
            self._onboarding_wait_timer.stop()
            self._onboarding_wait_timer.deleteLater()
        self._onboarding_wait_timer = QTimer(self.app)
        self._onboarding_wait_timer.setInterval(100)
        self._onboarding_wait_timer.timeout.connect(self._check_onboarding_closed)
        self._onboarding_wait_timer.start()

    def _check_onboarding_closed(self):
        plugin = getattr(self, "onboarding_plugin", None)
        handle = getattr(plugin, "process", None) if plugin is not None else None

        # Wait at least 3 seconds before checking to allow window to fully initialize
        if not hasattr(self, "_onboarding_start_time"):
            self._onboarding_start_time = time.time()
            return

        elapsed = time.time() - self._onboarding_start_time
        if elapsed < 3.0:  # Minimum 3 seconds before checking
            return

        try:
            finished = handle is None or handle.poll() is not None
        except Exception:
            finished = True
        if not finished:
            return
        if self._onboarding_wait_timer is not None:
            self._onboarding_wait_timer.stop()
            self._onboarding_wait_timer.deleteLater()
            self._onboarding_wait_timer = None
        if self._onboarding_restart_started:
            return
        self._onboarding_restart_started = True
        reload_cfg()
        self.restart()

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
            "plugins.builtins.app_launcher.plugin.AppLauncherPlugin",
            "plugins.builtins.logs.plugin.LogsPlugin",
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
                elif cls_name == "LogsPlugin":
                    self.logs_plugin = plugin
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
                    with open(manifest_path, "r", encoding="utf-8-sig") as f:
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
                        f"external_plugin_{item}", os.path.join(p_dir, mod_name + ".py")
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

    def _start_resource_monitor(self):
        """启动系统资源监测线程"""
        try:
            if self._resource_monitor is None:
                # 创建资源监测器，设置回调函数为显示托盘通知
                self._resource_monitor = SystemResourceMonitor(
                    on_alert_callback=self._on_resource_alert
                )
                self._resource_monitor.start()
                print("[APP] Resource monitor started")
        except Exception as e:
            print(f"[APP] Failed to start resource monitor: {e}")

    def _stop_resource_monitor(self):
        """停止系统资源监测线程"""
        try:
            if self._resource_monitor is not None:
                self._resource_monitor.stop()
                self._resource_monitor = None
                print("[APP] Resource monitor stopped")
        except Exception as e:
            print(f"[APP] Error stopping resource monitor: {e}")

    def _on_resource_alert(self, title: str, message: str):
        """资源告警回调 - 显示托盘通知"""
        try:
            if hasattr(self, "tray") and self.tray:
                self.tray.show_message(title, message)
        except Exception as e:
            print(f"[APP] Error sending resource alert: {e}")

    def _connect_signals(self):
        self.monitor.slideshow_started.connect(self.on_slideshow_start)
        self.monitor.slideshow_ended.connect(self.on_slideshow_end)
        self.monitor.slideshow_started.connect(
            lambda: self._focus_watcher.set_slideshow_running(True)
        )
        self.monitor.slideshow_ended.connect(
            lambda: self._focus_watcher.set_slideshow_running(False)
        )
        self.monitor.slideshow_hwnd_changed.connect(
            self._focus_watcher.set_slideshow_hwnd
        )
        self._focus_watcher.focus_on_slideshow_changed.connect(
            self._on_focus_on_slideshow_changed
        )

        self.overlay.request_next.connect(self.monitor.go_next)
        self.overlay.request_prev.connect(self.monitor.go_previous)
        self.overlay.request_goto.connect(self.monitor.go_to_slide)
        self.overlay.request_clear.connect(self.monitor.clear_screen)
        self.overlay.request_end.connect(self.monitor.end_show)

        self.overlay.request_ptr_arrow.connect(lambda: self.monitor.set_pointer_type(1))
        self.overlay.request_ptr_pen.connect(lambda: self.monitor.set_pointer_type(2))
        self.overlay.request_ptr_eraser.connect(
            lambda: self.monitor.set_pointer_type(5)
        )
        self.overlay.request_pen_color.connect(self.monitor.set_pen_color)
        self.overlay.request_thumbnail.connect(
            lambda idx: self.monitor.export_slide_thumbnail(
                idx,
                os.path.join(
                    tempfile.gettempdir(), "luminalium_ppt_thumbs", f"thumb_{idx}.png"
                ),
            )
        )

        if self.tray is not None:
            self.tray.show_settings.connect(self.settings_plugin.execute)
            self.tray.show_board.connect(self.board_plugin.execute)
            self.tray.show_timer.connect(self.timer_plugin.execute)
            self.tray.show_logs.connect(self.logs_plugin.execute)
            self.tray.toggle_overlay.connect(self.toggle_overlay_visibility)
            self.tray.restart_app.connect(self._restart_from_tray)
            self.tray.exit_app.connect(self._exit_from_tray)

        self.timer_plugin.background_mode_entered.connect(
            self._on_timer_background_mode
        )

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

    def _prepare_shutdown(self, restarting=False):
        try:
            # Stop resource monitor
            self._stop_resource_monitor()
        except Exception:
            pass
        try:
            if hasattr(self, "tray") and self.tray:
                self.tray.prepare_shutdown()
        except Exception:
            pass
        if restarting:
            try:
                os.environ["LUMINALIUM_RESTART"] = "1"
                os.environ["LUMINALIUM_RESTART_PID"] = str(os.getpid())
                _write_restart_marker()
            except Exception:
                pass
        try:
            self.app.processEvents()
        except Exception:
            pass

    @Slot()
    def _exit_from_tray(self):
        self._prepare_shutdown(restarting=False)
        self.app.quit()

    @Slot()
    def _restart_from_tray(self):
        self._prepare_shutdown(restarting=True)
        self._launch_new_instance()
        self.app.quit()

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
            self.tray.show_message(
                t("timer.background.title"), t("timer.background.body")
            )

    @Slot()
    def on_slideshow_start(self):
        try:
            print(
                "[Main] slideshow started: "
                f"kind={getattr(self.monitor, '_active_kind', None) or 'unknown'}, "
                f"hwnd={int(getattr(self.monitor, '_slideshow_hwnd', 0) or 0)}",
                flush=True,
            )
        except Exception:
            pass
        self._slideshow_running = True
        print("[APP] Slideshow started")
        try:
            self.overlay.on_slideshow_start_cleanup()
        except Exception as e:
            print(f"[APP] Error in on_slideshow_start_cleanup: {e}")
        # Cleanup slide thumbnails from previous session
        temp_dir = os.path.join(tempfile.gettempdir(), "luminalium_ppt_thumbs")
        if os.path.exists(temp_dir):
            try:
                shutil.rmtree(temp_dir)
            except Exception:
                pass
        try:
            self._focus_watcher.set_slideshow_running(True)
        except Exception:
            pass
        try:
            active_kind = getattr(self.monitor, "_active_kind", None)
            print(
                f"[APP] autoShowOverlay={cfg.autoShowOverlay.value}, compatibilityMode={cfg.compatibilityMode.value}, active_kind={active_kind}"
            )
            if cfg.autoShowOverlay.value and not cfg.compatibilityMode.value:
                print(
                    "[APP] Calling set_active_on_slideshow(True) - autoShowOverlay path"
                )
                if self._last_slideshow_rect is not None:
                    try:
                        self.overlay.update_geometry(
                            self._last_slideshow_rect, self._last_slideshow_screen
                        )
                    except Exception as e:
                        print(f"[APP] Error updating geometry: {e}")
                self.overlay.set_active_on_slideshow(True, animate=False)
            elif active_kind == "yozo" and not cfg.compatibilityMode.value:
                print("[APP] Calling set_active_on_slideshow(True) - yozo path")
                self.overlay.set_active_on_slideshow(True, animate=False)
            else:
                print("[APP] Not showing overlay - conditions not met")
        except Exception as e:
            print(f"[APP] Error in on_slideshow_start: {e}")
            import traceback

            traceback.print_exc()

    @Slot()
    def on_slideshow_end(self):
        try:
            print("[Main] slideshow ended.", flush=True)
        except Exception:
            pass
        self._slideshow_running = False
        try:
            self.overlay.on_slideshow_end_cleanup()
        except Exception:
            pass
        # Check if ink prompt is pending - if so, don't hide the overlay
        # The overlay will be hidden after the user responds to the prompt
        try:
            if hasattr(self.monitor, '_pending_ink_prompt') and self.monitor._pending_ink_prompt:
                print("[Main] Ink prompt pending, keeping overlay visible", flush=True)
            else:
                self.overlay.set_active_on_slideshow(False, animate=False)
        except Exception:
            pass
        try:
            self._focus_watcher.set_slideshow_running(False)
        except Exception:
            pass

    def _should_keep_overlay_visible(self) -> bool:
        if cfg.compatibilityMode.value:
            return True
        if not self._slideshow_running or not cfg.autoShowOverlay.value:
            return False
        try:
            hwnd = int(getattr(self.monitor, "_slideshow_hwnd", 0) or 0)
        except Exception:
            hwnd = 0
        if hwnd:
            return True
        rect = self._last_slideshow_rect
        if rect is not None:
            try:
                if not rect.isEmpty():
                    return True
            except Exception:
                return True
        return getattr(self.monitor, "_active_kind", None) == "yozo"

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
            print(f"[Main] focus_on_slideshow -> {bool(focused)}", flush=True)
        except Exception:
            pass
        try:
            if cfg.compatibilityMode.value:
                return
            if not self._slideshow_running or not cfg.autoShowOverlay.value:
                self.overlay.set_active_on_slideshow(False, animate=True)
                return
            active_kind = getattr(self.monitor, "_active_kind", None)
            if active_kind == "yozo":
                if self._last_slideshow_rect is not None:
                    try:
                        self.overlay.update_geometry(
                            self._last_slideshow_rect, self._last_slideshow_screen
                        )
                    except Exception:
                        pass
                self.overlay.set_active_on_slideshow(True, animate=False)
                return
            if focused and self._last_slideshow_rect is not None:
                try:
                    self.overlay.update_geometry(
                        self._last_slideshow_rect, self._last_slideshow_screen
                    )
                except Exception:
                    pass
            self.overlay.set_active_on_slideshow(bool(focused), animate=True)
        except Exception:
            pass

    def _check_settings_changed(self):
        reset_marker = _get_settings_reset_marker_path()
        if os.path.exists(reset_marker):
            try:
                os.remove(reset_marker)
            except Exception:
                pass
            self._settings_mtime = 0
            self.restart()
            return
        if not os.path.exists(SETTINGS_PATH):
            return
        mtime = os.path.getmtime(SETTINGS_PATH)
        if mtime != self._settings_mtime:
            self._settings_mtime = mtime

            # Check for restart flag
            try:
                with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                    temp_data = json.load(f)
                if temp_data.get("_quit_pending"):
                    del temp_data["_quit_pending"]
                    with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
                        json.dump(temp_data, f, indent=4, ensure_ascii=False)
                    self._prepare_shutdown(restarting=False)
                    self.app.quit()
                    return
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
            old_qt_weight = getattr(self, "_current_qt_font_weight", None)
            old_toolbar_text = cfg.showToolbarText.value
            old_status_bar = cfg.showStatusBar.value
            old_status_bar_show_time = cfg.statusBarShowTime.value
            old_status_bar_show_seconds = cfg.statusBarShowSeconds.value
            old_status_bar_show_battery = cfg.statusBarShowBattery.value
            old_status_bar_show_volume = cfg.statusBarShowVolume.value
            old_status_bar_show_network = cfg.statusBarShowNetwork.value
            old_status_bar_show_music = cfg.statusBarShowMusic.value
            old_status_bar_show_music_progress = cfg.statusBarShowMusicProgress.value
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
            new_qt_weight = _get_font_weight_from_settings(data, new_lang, "qt")
            new_qt_font = qt_font.strip() if isinstance(qt_font, str) else ""
            new_overlay_font = (
                overlay_font.strip() if isinstance(overlay_font, str) else ""
            )
            new_rebuild_at = (data.get("Overlay", {}) or {}).get("RecreateOverlayAt")

            self._current_language = new_lang
            self._current_qt_font = new_qt_font
            self._current_overlay_font = new_overlay_font
            self._current_qt_font_weight = new_qt_weight

            if new_qt_font != old_qt_font or new_qt_weight != old_qt_weight:
                _apply_global_font(self.app)

            should_reload = (
                new_lang != old_lang
                or new_overlay_font != old_overlay_font
                or cfg.themeMode.value != old_theme
                or cfg.compatibilityMode.value != old_compat
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

                    # Check for any status bar config changes
                    status_bar_changed = (
                        cfg.showStatusBar.value != old_status_bar
                        or cfg.statusBarShowTime.value != old_status_bar_show_time
                        or cfg.statusBarShowSeconds.value != old_status_bar_show_seconds
                        or cfg.statusBarShowBattery.value != old_status_bar_show_battery
                        or cfg.statusBarShowVolume.value != old_status_bar_show_volume
                        or cfg.statusBarShowNetwork.value != old_status_bar_show_network
                        or cfg.statusBarShowMusic.value != old_status_bar_show_music
                        or cfg.statusBarShowMusicProgress.value
                        != old_status_bar_show_music_progress
                    )

                    if status_bar_changed:
                        self.overlay.update_config()

                    if cfg.compatibilityMode.value != old_compat:
                        self.overlay.update_config()
                        if cfg.compatibilityMode.value:
                            self.overlay.show()
                        elif not self._slideshow_running:
                            self.overlay.hide()

            # Layout mode change is now handled by auto-reload above, no restart prompt needed

            if cfg.themeMode.value != old_theme:
                if self.tray is not None:
                    self.tray._update_icon()

            if new_lang != old_lang or cfg.compatibilityMode.value != old_compat:
                if self.tray is not None:
                    self.tray.refresh_menu()

            if new_rebuild_at is not None:
                self._overlay_rebuild_at = new_rebuild_at

    def _consume_open_settings_pending_flag(self):
        if not os.path.exists(SETTINGS_PATH):
            return False
        try:
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not data.get("_open_settings_pending"):
                return False
            del data["_open_settings_pending"]
            with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
            return True
        except Exception:
            return False

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
            from ppt_assistant.ui.overlay import create_overlay_window

            # Create new overlay first (prevent crash if creation fails)
            new_overlay = create_overlay_window()
            new_overlay.set_monitor(self.monitor)
            if hasattr(new_overlay, "set_timer_manager"):
                new_overlay.set_timer_manager(self._timer_manager)

            # Re-connect signals
            new_overlay.request_next.connect(self.monitor.go_next)
            new_overlay.request_prev.connect(self.monitor.go_previous)
            new_overlay.request_goto.connect(self.monitor.go_to_slide)
            new_overlay.request_clear.connect(self.monitor.clear_screen)
            new_overlay.request_end.connect(self.monitor.end_show)
            new_overlay.request_ptr_arrow.connect(
                lambda: self.monitor.set_pointer_type(1)
            )
            new_overlay.request_ptr_pen.connect(
                lambda: self.monitor.set_pointer_type(2)
            )
            new_overlay.request_ptr_eraser.connect(
                lambda: self.monitor.set_pointer_type(5)
            )
            new_overlay.request_pen_color.connect(self.monitor.set_pen_color)
            new_overlay.request_thumbnail.connect(
                lambda idx: self.monitor.export_slide_thumbnail(
                    idx,
                    os.path.join(
                        tempfile.gettempdir(),
                        "luminalium_ppt_thumbs",
                        f"thumb_{idx}.png",
                    ),
                )
            )

            # Disconnect old overlay slots before connecting new ones
            with warnings.catch_warnings():
                warnings.simplefilter("ignore", RuntimeWarning)
                try:
                    self.monitor.slide_changed.disconnect(self.overlay.update_page_info)
                except Exception:
                    pass
                try:
                    self.monitor.window_geometry_changed.disconnect(
                        self.overlay.update_geometry
                    )
                except Exception:
                    pass
                try:
                    self.monitor.slideshow_hwnd_changed.disconnect(
                        self.overlay.set_slideshow_hwnd
                    )
                except Exception:
                    pass
                try:
                    self.monitor.thumbnail_generated.disconnect(
                        self.overlay.on_thumbnail_ready
                    )
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
            old_overlay.cleanup()  # Stop threads safely
            old_overlay.hide()
            old_overlay.deleteLater()

            if was_visible and self._should_keep_overlay_visible():
                self.overlay.show()
                self.overlay.raise_()
            else:
                self.overlay.hide()

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

    def _launch_new_instance(self):
        """Start a fresh copy of the application as a detached child process."""
        try:
            base_dir = os.path.dirname(os.path.abspath(__file__))
            main_path = os.path.join(base_dir, "main.py")
            filtered_args = [
                a
                for a in sys.argv[1:]
                if a not in ("--silent", "--webview-runner", "--dialog", "--crash-file")
            ]
            if getattr(sys, "frozen", False):
                cmd = [sys.executable] + filtered_args
            else:
                cmd = [sys.executable, main_path] + filtered_args
            env = os.environ.copy()
            env["LUMINALIUM_RESTART"] = "1"
            env["LUMINALIUM_RESTART_PID"] = str(os.getpid())
            creationflags = (
                0x08000000 | 0x00000008
            )  # CREATE_NO_WINDOW | DETACHED_PROCESS
            subprocess.Popen(cmd, env=env, creationflags=creationflags, close_fds=True)
        except Exception as e:
            print(f"Failed to launch new instance: {e}", file=sys.stderr)

    def restart(self):
        """Restart for internal callers (settings reset, onboarding, etc.)."""
        self._prepare_shutdown(restarting=True)
        self._launch_new_instance()
        self.app.quit()

    def cleanup(self):
        """Cleanup app resources and terminate subprocesses."""
        if hasattr(self, "monitor"):
            self.monitor.stop_monitoring()
        try:
            if hasattr(self, "_focus_watcher") and self._focus_watcher:
                self._focus_watcher.stop()
        except Exception:
            pass
        if hasattr(self, "settings_plugin"):
            self.settings_plugin.terminate()
        if hasattr(self, "overlay"):
            self.overlay.cleanup()

    def run(self):
        # sys.exit(self.app.exec())
        pass


if __name__ == "__main__":
    # Platform settings moved to top of file to ensure they apply before any Qt import

    _ensure_user_dirs()
    _apply_graphics_settings()
    # Use Desktop OpenGL for better compatibility with Qt6
    QCoreApplication.setAttribute(Qt.AA_UseDesktopOpenGL)
    QCoreApplication.setAttribute(Qt.AA_ShareOpenGLContexts)
    print("[Main] Creating QApplication...", flush=True)
    app = QApplication(sys.argv)
    print("[Main] QApplication created.", flush=True)
    app_icon = load_app_icon()
    if not app_icon.isNull():
        app.setWindowIcon(app_icon)
        app._window_icon_filter = WindowIconEventFilter(app_icon)
        app.installEventFilter(app._window_icon_filter)
    _apply_global_font(app)
    print("[Main] Global font applied.", flush=True)
    crash_handler = CrashHandler(app)
    print("[Main] Checking multi-instance state...", flush=True)
    _handle_multi_instance(app)
    print("[Main] Multi-instance check finished.", flush=True)

    # Initialize log manager to capture application logs
    from ppt_assistant.core.log_manager import init_log_manager, get_log_manager

    init_log_manager()
    # Load log level settings from config
    log_manager = get_log_manager()
    log_filters = {
        "debug": cfg.showDebug.value if hasattr(cfg, "showDebug") else True,
        "info": cfg.showInfo.value if hasattr(cfg, "showInfo") else True,
        "warn": cfg.showWarn.value if hasattr(cfg, "showWarn") else True,
        "error": cfg.showError.value if hasattr(cfg, "showError") else True,
    }
    log_manager.set_filters(log_filters)

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
        print("[Main] Creating startup splash...", flush=True)
        splash = StartupSplash()
        splash.show()
        app.processEvents()
        print("[Main] Startup splash shown.", flush=True)

    print("[Main] Creating PPTAssistantApp...", flush=True)
    app_instance = PPTAssistantApp(app, splash)
    print("[Main] PPTAssistantApp created.", flush=True)
    crash_handler.set_app_instance(app_instance)
    sys.exit(app.exec())
