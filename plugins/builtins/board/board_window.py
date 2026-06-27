import os
import sys
import json
import ctypes
import ctypes.wintypes
from PySide6.QtQuickWidgets import QQuickWidget
from PySide6.QtQuick import QQuickPaintedItem, QQuickView
from PySide6.QtQml import qmlRegisterType
from PySide6.QtCore import (
    QUrl,
    Qt,
    QEvent,
    Slot,
    QObject,
    QPoint,
    QPointF,
    QTimer,
    Signal,
    Property,
    QEventLoop,
    QSize,
    QRect,
    QRectF,
    QPropertyAnimation,
    QEasingCurve,
    QAbstractNativeEventFilter,
    qInstallMessageHandler,
)
from PySide6.QtGui import QColor, QIcon, QAction, QGuiApplication, QPainter, QImage, QPen
from PySide6.QtWidgets import QApplication, QFileDialog, QWidget, QVBoxLayout
from ppt_assistant.core.config import cfg, SETTINGS_PATH, qconfig
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.theme_data import THEMES
from qfluentwidgets import Theme
from utils.env_utils import get_device_uuid

# ── Windows hit-test constants ──────────────────────────────────────────────
_WM_NCHITTEST   = 0x0084
_WM_NCMOUSEMOVE = 0x00A0
_WM_NCMOUSELEAVE = 0x02A2
_HTCLIENT    = 1
_HTCAPTION   = 2
_HTLEFT      = 10
_HTRIGHT     = 11
_HTTOP       = 12
_HTTOPLEFT   = 13
_HTTOPRIGHT  = 14
_HTBOTTOM    = 15
_HTBOTTOMLEFT  = 16
_HTBOTTOMRIGHT = 17
_HTMAXBUTTON = 9


class TitleBarNativeFilter(QAbstractNativeEventFilter):
    """Intercepts WM_NCHITTEST to enable:
    - Native window drag (HTCAPTION)
    - Resize borders (HTxxxx)
    - Win11 Snap Layouts popup (HTMAXBUTTON)
    - Max-button hover signalling via WM_NCMOUSEMOVE
    """
    TITLEBAR_H   = 32   # logical px
    BTN_W        = 46   # logical px — each caption button width
    RESIZE_BORDER = 6   # logical px

    def __init__(self, window, on_max_hover=None):
        super().__init__()
        self._window = window
        self._on_max_hover = on_max_hover   # callable(bool)
        self._max_hovered = False

    def _logical_client_pos(self, hwnd, x_scr, y_scr):
        pt = ctypes.wintypes.POINT(x_scr, y_scr)
        ctypes.windll.user32.ScreenToClient(hwnd, ctypes.byref(pt))
        try:
            dpr = self._window.devicePixelRatio() or 1.0
        except Exception:
            dpr = 1.0
        return pt.x / dpr, pt.y / dpr

    def _hit_test(self, x, y):
        win_w = self._window.width()
        win_h = self._window.height()
        rb    = self.RESIZE_BORDER
        is_max = bool(self._window.windowState() & Qt.WindowMaximized)

        if not is_max:
            if x < rb and y < rb:                           return _HTTOPLEFT
            if x >= win_w - rb and y < rb:                 return _HTTOPRIGHT
            if x < rb and y >= win_h - rb:                 return _HTBOTTOMLEFT
            if x >= win_w - rb and y >= win_h - rb:        return _HTBOTTOMRIGHT
            if y < rb:                                      return _HTTOP
            if y >= win_h - rb:                             return _HTBOTTOM
            if x < rb:                                      return _HTLEFT
            if x >= win_w - rb:                             return _HTRIGHT

        if y < self.TITLEBAR_H:
            bw = self.BTN_W
            close_x = win_w - bw
            max_x   = win_w - 2 * bw
            min_x   = win_w - 3 * bw
            if x >= close_x: return _HTCLIENT    # close  → QML handles
            if x >= max_x:   return _HTMAXBUTTON # maximize → Win11 snap!
            if x >= min_x:   return _HTCLIENT    # minimize → QML handles
            return _HTCAPTION                    # drag area

        return _HTCLIENT

    # ── QAbstractNativeEventFilter interface ──────────────────────────────
    def nativeEventFilter(self, eventType, message):
        if eventType != b"windows_generic_MSG":
            return False, 0
        try:
            own_hwnd = int(self._window.winId())
        except Exception:
            return False, 0
        try:
            msg = ctypes.cast(int(message),
                              ctypes.POINTER(ctypes.wintypes.MSG)).contents
            if int(msg.hWnd or 0) != own_hwnd:
                return False, 0
        except Exception:
            return False, 0

        if msg.message == _WM_NCHITTEST:
            x_scr = ctypes.c_short(msg.lParam & 0xFFFF).value
            y_scr = ctypes.c_short((msg.lParam >> 16) & 0xFFFF).value
            x, y  = self._logical_client_pos(msg.hWnd, x_scr, y_scr)
            return True, self._hit_test(x, y)

        if msg.message == _WM_NCMOUSEMOVE:
            hov = (msg.wParam == _HTMAXBUTTON)
            if hov != self._max_hovered:
                self._max_hovered = hov
                if callable(self._on_max_hover):
                    self._on_max_hover(hov)

        if msg.message == _WM_NCMOUSELEAVE:
            if self._max_hovered:
                self._max_hovered = False
                if callable(self._on_max_hover):
                    self._on_max_hover(False)

        return False, 0


def _get_app_version_info():
    version = ""
    code_name = ""
    try:
        root_dir = os.path.dirname(
            os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
        )
        version_path = os.path.join(root_dir, "version.json")
        if not os.path.exists(version_path):
            return version, code_name
        with open(version_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        version = str(data.get("version", "")).strip()
        raw_code_name = str(data.get("code_name", "")).strip()
        mapping = {
            "MomokaKawaragi": "Momoka Kawaragi",
            "NinaIseri": "Nina Iseri",
            "SubaruAwa": "Subaru Awa",
            "TomoEbizuka": "Tomo Ebizuka",
            "Momokan": "Momokan",
        }
        code_name = mapping.get(raw_code_name, raw_code_name)
        return version, code_name
    except Exception:
        return version, code_name


def _get_app_version():
    version, _ = _get_app_version_info()
    return version


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
    suffix = parts[-1]
    return suffix in ["1", "2", "3", "4"]


def _load_language():
    try:
        if os.path.exists(SETTINGS_PATH):
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                data = json.load(f)
            return data.get("General", {}).get("Language", "zh-CN")
    except Exception:
        return "zh-CN"
    return "zh-CN"


def _color_to_rgba(color) -> tuple[int, int, int, int]:
    if isinstance(color, QColor):
        c = color
    else:
        c = QColor(color)
    if not c.isValid():
        c = QColor("#000000")
    return c.red(), c.green(), c.blue(), c.alpha()


def _load_settings_data():
    try:
        if os.path.exists(SETTINGS_PATH):
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                data = json.load(f)
            return data if isinstance(data, dict) else {}
    except Exception:
        pass
    return {}


def _read_board_settings():
    position = "bottom"
    background_color = "#202020"
    popup_bg = ""
    popup_border = ""
    eraser_mode = 0  # 0: Point, 1: Stroke
    pen_stroke_enabled = False
    window_enter_animation = True

    # Read settings file once
    settings_data = _load_settings_data()

    # Get ThemeId from settings or fallback to cfg
    theme_id = settings_data.get("Appearance", {}).get("ThemeId", cfg.themeId.value)
    theme_mode = settings_data.get("Appearance", {}).get(
        "ThemeMode", cfg.themeMode.value
    )

    board = settings_data.get("BoardInBoard", {}) or {}

    # Read eraser mode
    mode_str = board.get("EraserMode", "point")
    if mode_str == "stroke":
        eraser_mode = 1
    else:
        eraser_mode = 0
    pen_stroke_enabled = bool(board.get("PenStrokeEnabled", False))
    window_enter_animation = bool(board.get("WindowEnterAnimation", True))

    # Override for year-of-horse theme
    if theme_id == "year-of-horse":
        is_dark = str(theme_mode).lower() == "dark"
        if is_dark:
            background_color = "#451212"
            popup_bg = "#451212"
            popup_border = "rgba(255, 69, 0, 0.3)"
        else:
            background_color = "#FFF0F0"
            popup_bg = "#FFF0F0"
            popup_border = "rgba(230, 0, 0, 0.15)"

        pos = board.get("ToolbarPosition", position)
        if pos in ("top", "bottom", "left", "right"):
            position = pos
        return (
            position,
            background_color,
            popup_bg,
            popup_border,
            eraser_mode,
            pen_stroke_enabled,
            window_enter_animation,
        )

    pos = board.get("ToolbarPosition", position)
    if pos in ("top", "bottom", "left", "right"):
        position = pos
    color = board.get("BackgroundColor", background_color)
    if isinstance(color, str) and len(color) == 7 and color.startswith("#"):
        try:
            int(color[1:], 16)
            background_color = color
        except Exception:
            background_color = "#202020"
    return (
        position,
        background_color,
        popup_bg,
        popup_border,
        eraser_mode,
        pen_stroke_enabled,
        window_enter_animation,
    )


def _resolve_board_theme():
    """Return the current theme palette dict for the board QML overlay styling."""
    settings_data = _load_settings_data()
    appearance = settings_data.get("Appearance", {}) or {}
    theme_id = appearance.get("ThemeId", cfg.themeId.value)
    theme_mode = _normalize_theme_mode(appearance.get("ThemeMode", cfg.themeMode.value))
    theme = THEMES.get(theme_id, THEMES["default"]).get(
        theme_mode, THEMES["default"][theme_mode]
    )
    return theme


def _normalize_theme_mode(raw_theme) -> str:
    if isinstance(raw_theme, Theme):
        if raw_theme == Theme.DARK:
            return "dark"
        if raw_theme == Theme.LIGHT:
            return "light"
        raw_theme = "auto"
    text = str(raw_theme or "auto").lower()
    if text in ("light", "dark"):
        return text
    try:
        if qconfig.theme == Theme.DARK:
            return "dark"
    except Exception:
        pass
    return "light"


def _resolve_save_dialog_palette():
    settings_data = _load_settings_data()
    appearance = settings_data.get("Appearance", {}) or {}
    theme_id = appearance.get("ThemeId", cfg.themeId.value)
    theme_mode = _normalize_theme_mode(appearance.get("ThemeMode", cfg.themeMode.value))
    theme_palette = THEMES.get(theme_id, THEMES["default"]).get(
        theme_mode, THEMES["default"][theme_mode]
    )
    is_dark = theme_mode == "dark"

    palette = {
        "darkMode": is_dark,
        "windowBg": "#181818" if is_dark else "#FFFFFF",
        "dialogBg": "#991E1E1E"
        if is_dark
        else "#99FFFFFF",  # 0.6 opacity -> 0.6 * 255 = 153 ≈ 0x99
        "dialogBorder": "#0AFFFFFF"
        if is_dark
        else "#0A000000",  # 0.04 opacity -> 10 ≈ 0x0A (视觉减弱描边粗度)
        "dialogTitle": "#E5E5E5" if is_dark else "#191919",
        "dialogText": "#E5E5E5" if is_dark else "#191919",
        "textSecondary": "#909090" if is_dark else "#666666",
        "accent": theme_palette.get("accent", "#4A85F6" if is_dark else "#3275F5"),
        "buttonHover": theme_palette.get(
            "item_hover", "#0FFFFFFF" if is_dark else "#0A000000"
        ),  # 0.06 -> 0x0F, 0.04 -> 0x0A
        "buttonActive": theme_palette.get(
            "btn_active_bg", "#1EFFFFFF" if is_dark else "#1E000000"
        ),  # 0.12 -> 0x1E
        "cardShadow": "#26000000"
        if is_dark
        else "#08000000",  # 0.15 -> 0x26, 0.03 -> 0x08
    }

    if theme_id == "year-of-horse":
        palette.update(
            {
                "windowBg": "#3A0E0E" if is_dark else "#FFF0F0",
                "dialogBg": "rgba(255, 69, 0, 0.10)"
                if is_dark
                else "rgba(255, 235, 238, 0.95)",
                "dialogBorder": "rgba(255, 69, 0, 0.30)"
                if is_dark
                else "rgba(211, 47, 47, 0.25)",
                "dialogTitle": "#FFD700" if is_dark else "#B71C1C",
                "dialogText": "#FFB347" if is_dark else "#B71C1C",
                "accent": "#FF4500" if is_dark else "#D32F2F",
                "buttonHover": "rgba(255, 69, 0, 0.22)"
                if is_dark
                else "rgba(255, 0, 0, 0.12)",
                "buttonActive": "rgba(255, 69, 0, 0.30)"
                if is_dark
                else "rgba(211, 47, 47, 0.25)",
                "cardShadow": "rgba(0, 0, 0, 0.40)"
                if is_dark
                else "rgba(180, 0, 0, 0.08)",
            }
        )
    elif theme_id != "default":
        popup_bg = theme_palette.get("popup_bg", "")
        palette.update(
            {
                "windowBg": popup_bg if popup_bg else palette["windowBg"],
                "dialogBg": theme_palette.get("popup_bg", palette["dialogBg"]),
                "dialogBorder": theme_palette.get(
                    "popup_border", palette["dialogBorder"]
                ),
                "dialogTitle": theme_palette.get("popup_fg", palette["dialogTitle"]),
                "dialogText": theme_palette.get("popup_fg", palette["dialogText"]),
                "cardShadow": theme_palette.get(
                    "toolbar_shadow", palette["cardShadow"]
                ),
            }
        )

    return palette


def _resolve_dialog_font_family():
    settings_data = _load_settings_data()
    lang = settings_data.get("General", {}).get("Language", "zh-CN")
    profiles = (settings_data.get("Fonts", {}) or {}).get("Profiles", {}) or {}
    selected = (profiles.get(lang, {}) or {}).get("qt", "")
    if isinstance(selected, str) and selected.strip():
        return selected.strip()
    try:
        app_font = QGuiApplication.font()
        if app_font:
            return app_font.family()
    except Exception:
        pass
    return ""


def _apply_dialog_window_theme(hwnd, is_dark):
    """Apply DWM dark/light mode to give the dialog window the correct title bar colour."""
    import sys

    if sys.platform != "win32" or not hwnd:
        return
    try:
        import ctypes

        dwmapi = ctypes.windll.dwmapi
        uxtheme = ctypes.windll.uxtheme
        user32 = ctypes.windll.user32
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20
        DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19
        DWMWA_CAPTION_COLOR = 35
        DWMWA_TEXT_COLOR = 36
        DWMWA_BORDER_COLOR = 34
        _DWM_COLOR_DEFAULT = 0xFFFFFFFF
        val = ctypes.c_int(1 if is_dark else 0)
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ctypes.byref(val), ctypes.sizeof(val)
        )
        dwmapi.DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
            ctypes.byref(val),
            ctypes.sizeof(val),
        )
        if is_dark:
            caption = ctypes.c_int(0x00181818)
            text_col = ctypes.c_int(0x00FFFFFF)
            border = ctypes.c_int(0x00181818)
        else:
            caption = ctypes.c_int(_DWM_COLOR_DEFAULT)
            text_col = ctypes.c_int(_DWM_COLOR_DEFAULT)
            border = ctypes.c_int(_DWM_COLOR_DEFAULT)
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_CAPTION_COLOR, ctypes.byref(caption), ctypes.sizeof(caption)
        )
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_TEXT_COLOR, ctypes.byref(text_col), ctypes.sizeof(text_col)
        )
        dwmapi.DwmSetWindowAttribute(
            hwnd, DWMWA_BORDER_COLOR, ctypes.byref(border), ctypes.sizeof(border)
        )
        theme = "DarkMode_Explorer" if is_dark else "Explorer"
        uxtheme.SetWindowTheme(hwnd, ctypes.c_wchar_p(theme), None)
        flags = 0x0001 | 0x0002 | 0x0004 | 0x0020
        user32.SetWindowPos(hwnd, 0, 0, 0, 0, 0, flags)
    except Exception:
        pass


def _load_board_toolbar_position():
    position, _, _, _, _, _ = _read_board_settings()
    return position


_TRANSLATIONS = {
    "zh-CN": {
        "watermark.1": "开发中版本",
        "watermark.2": "技术预览版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新评估版本",
        "overlay.dev_watermark": "{type}\n不保证最终品质 （{version}/{codename}/{uuid}）",
        "toolbar.pen_size": "画笔粗细",
        "toolbar.eraser_size": "橡皮粗细",
        "toolbar.eraser_point": "掠区擦除",
        "toolbar.eraser_stroke": "笔画擦除",
        "toolbar.slide_clear": "滑动清屏",
        "toolbar.slide_clear_hint": "滑动以清空屏幕",
        "dialog.save_strokes_title": "提示",
        "dialog.save_strokes_text": "是否保留本次笔迹？",
        "dialog.save_strokes_yes": "保留",
        "dialog.save_strokes_no": "不保留",
        "dialog.cancel": "取消",
        "toolbar.save_page": "保存",
        "toolbar.add_page": "加页",
    },
    "zh-TW": {
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "overlay.dev_watermark": "{type}\n不保證最終品質 （{version}/{codename}/{uuid}）",
        "toolbar.pen_size": "畫筆粗細",
        "toolbar.eraser_size": "橡皮粗細",
        "toolbar.eraser_point": "掠區擦除",
        "toolbar.eraser_stroke": "筆畫擦除",
        "toolbar.slide_clear": "滑動清屏",
        "toolbar.slide_clear_hint": "滑動以清空螢幕",
        "dialog.save_strokes_title": "提示",
        "dialog.save_strokes_text": "是否保留本次筆跡？",
        "dialog.save_strokes_yes": "保留",
        "dialog.save_strokes_no": "不保留",
        "dialog.cancel": "取消",
        "toolbar.save_page": "儲存",
        "toolbar.add_page": "加頁",
    },
    "yue-HK": {
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "overlay.dev_watermark": "{type}\n品質唔包，出事唔好屌我 ({version}/{codename}/{uuid})",
        "toolbar.pen_size": "畫筆粗細",
        "toolbar.eraser_size": "橡皮粗細",
        "toolbar.eraser_point": "掠區擦除",
        "toolbar.eraser_stroke": "筆畫擦除",
        "toolbar.slide_clear": "滑動清屏",
        "toolbar.slide_clear_hint": "滑動以清空螢幕",
        "dialog.save_strokes_title": "提你一提",
        "dialog.save_strokes_text": "要唔要留低呢堆筆跡？",
        "dialog.save_strokes_yes": "留低",
        "dialog.save_strokes_no": "唔留",
        "dialog.cancel": "取消",
        "toolbar.save_page": "儲存",
        "toolbar.add_page": "加頁",
    },
    "en-US": {
        "watermark.1": "Dev Build",
        "watermark.2": "Tech Preview",
        "watermark.3": "Release Preview",
        "watermark.4": "Re-evaluation",
        "overlay.dev_watermark": "{type}\nQuality not guaranteed ({version}/{codename}/{uuid})",
        "toolbar.pen_size": "Pen Size",
        "toolbar.eraser_size": "Eraser Size",
        "toolbar.eraser_point": "Point Eraser",
        "toolbar.eraser_stroke": "Stroke Eraser",
        "toolbar.slide_clear": "Slide to Clear",
        "toolbar.slide_clear_hint": "Slide to clear screen",
        "dialog.save_strokes_title": "Tip",
        "dialog.save_strokes_text": "Keep current strokes?",
        "dialog.save_strokes_yes": "Keep",
        "dialog.save_strokes_no": "Don't Keep",
        "dialog.cancel": "Cancel",
        "toolbar.save_page": "Save",
        "toolbar.add_page": "Add",
    },
    "ja-JP": {
        "watermark.1": "開発中のバージョン",
        "watermark.2": "テクニカルプレビュー",
        "watermark.3": "Release Preview",
        "watermark.4": "再評価バージョン",
        "overlay.dev_watermark": "{type}\n品質は保証されません ({version}/{codename}/{uuid})",
        "toolbar.pen_size": "ペンの太さ",
        "toolbar.eraser_size": "消しゴムの太さ",
        "toolbar.eraser_point": "部分消しゴム",
        "toolbar.eraser_stroke": "ストローク消しゴム",
        "toolbar.slide_clear": "スライドで消去",
        "toolbar.slide_clear_hint": "スライドして画面をクリア",
        "dialog.save_strokes_title": "ヒント",
        "dialog.save_strokes_text": "今回の筆跡を保存しますか？",
        "dialog.save_strokes_yes": "保存する",
        "dialog.save_strokes_no": "保存しない",
        "dialog.cancel": "キャンセル",
        "toolbar.save_page": "保存",
        "toolbar.add_page": "追加",
    },
}


def _t(key: str) -> str:
    lang = _load_language()
    fallback_lang = "zh-TW" if lang == "yue-HK" else "zh-CN"
    table = (
        _TRANSLATIONS.get(lang)
        or _TRANSLATIONS.get(fallback_lang)
        or _TRANSLATIONS["zh-CN"]
    )
    if key in table:
        return table[key]
    default = _TRANSLATIONS.get(fallback_lang) or _TRANSLATIONS["zh-CN"]
    return default.get(key, key)


class ColorEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, QColor):
            return obj.name(QColor.HexArgb)
        return super().default(obj)


def _normalize_board_document(raw):
    if isinstance(raw, list):
        return {
            "currentPage": 1,
            "pages": [{"strokes": raw, "thumb": ""}],
        }

    if isinstance(raw, dict):
        pages = []
        raw_pages = raw.get("pages")
        if isinstance(raw_pages, list):
            for page in raw_pages:
                if isinstance(page, dict):
                    strokes = page.get("strokes", [])
                    thumb = page.get("thumb", "")
                elif isinstance(page, list):
                    strokes = page
                    thumb = ""
                else:
                    strokes = []
                    thumb = ""
                if not isinstance(strokes, list):
                    strokes = []
                if not isinstance(thumb, str):
                    thumb = ""
                pages.append({"strokes": strokes, "thumb": thumb})

        if not pages:
            legacy_strokes = raw.get("strokes")
            if isinstance(legacy_strokes, list):
                pages.append({"strokes": legacy_strokes, "thumb": ""})

        if not pages:
            pages = [{"strokes": [], "thumb": ""}]

        current_page = raw.get("currentPage", 1)
        try:
            current_page = int(current_page)
        except Exception:
            current_page = 1
        current_page = max(1, min(current_page, len(pages)))

        return {
            "currentPage": current_page,
            "pages": pages,
        }

    return {
        "currentPage": 1,
        "pages": [{"strokes": [], "thumb": ""}],
    }


def _board_document_has_content(document):
    pages = document.get("pages", []) if isinstance(document, dict) else []
    for page in pages:
        if (
            isinstance(page, dict)
            and isinstance(page.get("strokes"), list)
            and page["strokes"]
        ):
            return True
    return False


class BoardBackend(QObject):
    windowStateChanged    = Signal()
    maximizeBtnHoveredChanged = Signal(bool)

    def __init__(self, window):
        super().__init__()
        self._window = window
        self._max_btn_hovered = False

    # ── called by TitleBarNativeFilter ───────────────────────────────────
    def _set_max_btn_hovered(self, val: bool):
        if val != self._max_btn_hovered:
            self._max_btn_hovered = val
            self.maximizeBtnHoveredChanged.emit(val)

    @Property(bool, notify=maximizeBtnHoveredChanged)
    def maximizeBtnHovered(self):
        return self._max_btn_hovered

    # ── window control slots ──────────────────────────────────────────────
    @Slot(int, int)
    def moveWindow(self, dx, dy):
        current_pos = self._window.pos()
        self._window.move(current_pos + QPoint(dx, dy))

    @Slot()
    def closeWindow(self):
        self._window.close()

    @Slot()
    def startDrag(self):
        if sys.platform == "win32":
            hwnd = int(self._window.winId())
            ctypes.windll.user32.ReleaseCapture()
            ctypes.windll.user32.SendMessageW(hwnd, 0xA1, 2, 0)
        else:
            # Fallback: QWidget doesn't have startSystemMove
            pass

    @Slot()
    def minimizeWindow(self):
        self._window.showMinimized()

    @Slot()
    def toggleMaximized(self):
        if self.isMaximized:
            self._window.showNormal()
        else:
            self._window.showMaximized()
        self.windowStateChanged.emit()

    @Slot()
    def toggleFullscreen(self):
        self._window.toggle_fullscreen()
        self.windowStateChanged.emit()

    @Slot()
    def saveCurrentPageAsPng(self):
        self._window.save_current_page_png()

    @Slot(int)
    def startResize(self, edge):
        if sys.platform == "win32":
            hwnd = int(self._window.winId())
            # Map Qt.Edge to Windows WM_NCHITTEST codes
            edge_map = {
                1: 12,   # Qt.TopEdge → HTTOP
                2: 11,   # Qt.RightEdge → HTRIGHT
                4: 15,   # Qt.BottomEdge → HTBOTTOM
                8: 10,   # Qt.LeftEdge → HTLEFT
                3: 14,   # TopEdge|RightEdge → HTTOPRIGHT
                5: 13,   # TopEdge|LeftEdge → HTTOPLEFT
                9: 13,   # LeftEdge|TopEdge → HTTOPLEFT
                6: 17,   # RightEdge|BottomEdge → HTBOTTOMRIGHT
                10: 17,  # BottomEdge|RightEdge → HTBOTTOMRIGHT
                12: 16,  # BottomEdge|LeftEdge → HTBOTTOMLEFT
            }
            wparam = edge_map.get(edge, 2)  # default to HTCAPTION
            ctypes.windll.user32.ReleaseCapture()
            ctypes.windll.user32.SendMessageW(hwnd, 0xA1, wparam, 0)

    @Property(bool, notify=windowStateChanged)
    def isMaximized(self):
        return bool(self._window.windowState() & Qt.WindowMaximized)

    @Property(bool, notify=windowStateChanged)
    def isFullscreen(self):
        return bool(self._window.windowState() & Qt.WindowFullScreen)


class NativeBoardItem(QQuickPaintedItem):
    backgroundColorChanged = Signal()
    minSegmentPxChanged = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        # 极致优化：使用Image渲染目标，在某些低端设备上比FBO更快
        self.setRenderTarget(QQuickPaintedItem.Image)
        self.setPerformanceHint(QQuickPaintedItem.FastFBOResizing)
        self.setOpaquePainting(False)
        self._buffer = None
        self._allLines = []
        self._pendingLines = []
        self._background_color = QColor("#202020")
        self._min_segment_px = 1.5
        self._dirty_full = True
        # 极致优化：缓存宽高，避免重复调用
        self._w = 1
        self._h = 1

    @Property(QColor, notify=backgroundColorChanged)
    def backgroundColor(self):
        return self._background_color

    @backgroundColor.setter
    def backgroundColor(self, value):
        color = value if isinstance(value, QColor) else QColor(value)
        if not color.isValid():
            color = QColor("#202020")
        if color == self._background_color:
            return
        self._background_color = color
        self._dirty_full = True
        self.backgroundColorChanged.emit()
        self.update()

    def _set_min_segment_px(self, value):
        try:
            val = float(value)
        except Exception:
            return
        if val <= 0:
            return
        self._min_segment_px = val
        self._dirty_full = True
        self.minSegmentPxChanged.emit()
        self.update()

    @Property(float, notify=minSegmentPxChanged)
    def minSegmentPx(self):
        return float(self._min_segment_px)

    @minSegmentPx.setter
    def minSegmentPx(self, value):
        self._set_min_segment_px(value)

    def _ensure_buffer(self):
        w = self.width()
        h = self.height()
        # 极致优化：更新缓存的宽高
        self._w = w if w > 0 else 1
        self._h = h if h > 0 else 1
        if w <= 0 or h <= 0:
            return False
        scale = self.window().devicePixelRatio() if self.window() else 1.0
        bw = int(w * scale)
        bh = int(h * scale)
        if bw <= 0 or bh <= 0:
            return False
        if self._buffer and self._buffer.width() == bw and self._buffer.height() == bh:
            return True
        new_buf = QImage(bw, bh, QImage.Format_ARGB32_Premultiplied)
        new_buf.setDevicePixelRatio(scale)
        new_buf.fill(Qt.transparent)
        if self._buffer:
            painter = QPainter(new_buf)
            painter.drawImage(0, 0, self._buffer)
            painter.end()
        else:
            self._pendingLines = list(self._allLines)
        self._buffer = new_buf
        return True

    @Slot(float, float, float, float, float, str, bool, float, int)
    def addLine(self, x1, y1, x2, y2, width, colorHex, isEraser, eraserPx, strokeId):
        line = {
            "x1": x1,
            "y1": y1,
            "x2": x2,
            "y2": y2,
            "width": width,
            "color": colorHex,
            "isEraser": isEraser,
            "eraserPx": eraserPx,
            "strokeId": strokeId,
        }
        self._allLines.append(line)
        if self._buffer is not None:
            self._pendingLines.append(line)
        else:
            self._dirty_full = True
        # 极致优化：立即更新，不延迟
        self.update()

    @Slot()
    def requestRepaintAll(self):
        self._dirty_full = True
        self.update()

    @Slot()
    def clearLines(self):
        self._allLines.clear()
        self._pendingLines.clear()
        if self._buffer:
            self._buffer.fill(Qt.transparent)
        self._dirty_full = True
        self.update()

    @Slot(list)
    def setAllLines(self, lines):
        self._allLines = []
        for l in lines:
            if isinstance(l, dict):
                l["x1"] = float(l.get("x1", 0))
                l["y1"] = float(l.get("y1", 0))
                l["x2"] = float(l.get("x2", 0))
                l["y2"] = float(l.get("y2", 0))
                self._allLines.append(l)
        self._dirty_full = True
        self.update()

    @Slot(result="QVariant")
    def getAllLines(self):
        return self._allLines

    @Slot(int)
    def removeStrokeAndRepaint(self, strokeId):
        if not self._allLines:
            return
        self._allLines = [l for l in self._allLines if l.get("strokeId") != strokeId]
        self._dirty_full = True
        self.update()

    @Slot(list)
    def removeStrokesAndRepaint(self, strokeIds):
        if not self._allLines:
            return
        st_ids = set(strokeIds)
        self._allLines = [l for l in self._allLines if l.get("strokeId") not in st_ids]
        self._dirty_full = True
        self.update()

    def geometryChanged(self, newGeometry, oldGeometry):
        super().geometryChanged(newGeometry, oldGeometry)
        self._dirty_full = True
        self.update()

    def paint(self, painter: QPainter):
        if not self._ensure_buffer():
            return
        if self._dirty_full:
            self._buffer.fill(Qt.transparent)
            self._pendingLines = list(self._allLines)
            self._dirty_full = False

        if self._pendingLines:
            buf_painter = QPainter(self._buffer)
            # 极致优化：仅启用必要的渲染标志
            buf_painter.setRenderHint(QPainter.Antialiasing, True)
            # 极致优化：直接绘制，减少函数调用开销
            pending = self._pendingLines
            for line in pending:
                self._drawLine(buf_painter, line)
            buf_painter.end()
            self._pendingLines.clear()

        painter.drawImage(0, 0, self._buffer)

    def _drawLine(self, painter, line):
        # 极致优化：本地变量缓存，减少属性访问
        isEraser = line.get("isEraser", False)
        if isEraser:
            painter.setCompositionMode(QPainter.CompositionMode_DestinationOut)
            pen = QPen(Qt.black)
            pen.setWidthF(max(1.0, float(line.get("eraserPx", 20))) + 8.0)
            pen.setCapStyle(Qt.RoundCap)
            pen.setJoinStyle(Qt.RoundJoin)
            painter.setPen(pen)
        else:
            painter.setCompositionMode(QPainter.CompositionMode_SourceOver)
            pen = QPen(QColor(line.get("color", "#000000")))
            pen.setWidthF(max(0.5, float(line.get("width", 3))))
            pen.setCapStyle(Qt.RoundCap)
            pen.setJoinStyle(Qt.RoundJoin)
            painter.setPen(pen)

        # 极致优化：缓存宽高，减少重复调用
        w = self._w
        h = self._h
        x1 = float(line.get("x1", 0.0)) * w
        y1 = float(line.get("y1", 0.0)) * h
        x2 = float(line.get("x2", 0.0)) * w
        y2 = float(line.get("y2", 0.0)) * h

        dx = x2 - x1
        dy = y2 - y1
        # 极致优化：使用硬编码最小阈值，避免乘法运算
        if (dx * dx + dy * dy) < 2.25:  # 1.5 * 1.5
            painter.drawPoint(QPointF(x1, y1))
            return
        painter.drawLine(QPointF(x1, y1), QPointF(x2, y2))


class SaveStrokesDialogBridge(QObject):
    saveRequested = Signal()
    discardRequested = Signal()
    cancelRequested = Signal()

    @Slot()
    def chooseSave(self):
        self.saveRequested.emit()

    @Slot()
    def chooseDiscard(self):
        self.discardRequested.emit()

    @Slot()
    def chooseCancel(self):
        self.cancelRequested.emit()


class SaveStrokesDialog(QQuickView):
    ResultCancel = 0
    ResultSave = 1
    ResultDiscard = 2

    def __init__(self, owner_window, title, text, save_text, discard_text, cancel_text):
        super().__init__()
        self._window_icon_filter_guard = WindowIconFilterGuard()
        self._window_icon_filter_guard.suspend()
        self._qt_message_handler_guard = QtMessageHandlerGuard()
        self._qt_message_handler_guard.suspend()
        self._owner_window = owner_window
        self._result = self.ResultCancel
        self._loop = None
        self._closing = False
        self.destroyed.connect(lambda *_: self._restore_qml_guards())

        self.setResizeMode(QQuickView.SizeRootObjectToView)
        self.setTitle(title)
        self.setFlags(
            Qt.Dialog
            | Qt.WindowTitleHint
            | Qt.WindowSystemMenuHint
            | Qt.WindowCloseButtonHint
            | Qt.MSWindowsFixedSizeDialogHint
        )
        self.setModality(Qt.ApplicationModal)
        if self._owner_window is not None:
            try:
                self.setTransientParent(self._owner_window)
            except Exception:
                pass
        icon = load_app_icon()
        if not icon.isNull():
            self.setIcon(icon)

        self._bridge = SaveStrokesDialogBridge(self)
        self._bridge.saveRequested.connect(lambda: self._finish(self.ResultSave))
        self._bridge.discardRequested.connect(lambda: self._finish(self.ResultDiscard))
        self._bridge.cancelRequested.connect(lambda: self._finish(self.ResultCancel))

        palette = _resolve_save_dialog_palette()
        self._is_dark = palette["darkMode"]
        context = self.rootContext()
        context.setContextProperty("dialogBridge", self._bridge)
        context.setContextProperty("dialogTitle", title)
        context.setContextProperty("dialogMessage", text)
        context.setContextProperty("dialogConfirmText", save_text)
        context.setContextProperty("dialogDiscardText", discard_text)
        context.setContextProperty("dialogCancelText", cancel_text)
        context.setContextProperty("dialogWindowBg", palette["windowBg"])
        context.setContextProperty("dialogBgApp", palette["dialogBg"])
        context.setContextProperty("dialogTextPrimary", palette["dialogTitle"])
        context.setContextProperty(
            "dialogTextSecondary", palette.get("textSecondary", palette["dialogText"])
        )
        context.setContextProperty("dialogAccent", palette["accent"])
        context.setContextProperty("dialogDivider", palette["dialogBorder"])
        context.setContextProperty("dialogItemHover", palette["buttonHover"])
        context.setContextProperty("dialogButtonActive", palette["buttonActive"])
        context.setContextProperty("dialogCardShadow", palette["cardShadow"])
        context.setContextProperty("dialogDarkMode", palette["darkMode"])
        font_family = _resolve_dialog_font_family()
        if font_family:
            context.setContextProperty("dialogFontFamily", font_family)

        qml_path = os.path.join(
            os.path.dirname(os.path.abspath(__file__)), "SaveStrokesDialog.qml"
        )
        try:
            self.setSource(QUrl.fromLocalFile(qml_path))
        finally:
            self._restore_qt_message_handler()
        root = self.rootObject()
        width = (
            int(root.property("implicitWidth"))
            if root and root.property("implicitWidth")
            else 452
        )
        height = (
            int(root.property("implicitHeight"))
            if root and root.property("implicitHeight")
            else 214
        )
        self.setColor(QColor(palette["windowBg"]))
        self.resize(width, height)
        self.setMinimumSize(QSize(width, height))
        self.setMaximumSize(QSize(width, height))
        self._center_to_owner()

    def _center_to_owner(self):
        if self._owner_window is None:
            return
        geometry = self._owner_window.geometry()
        x = geometry.x() + max(0, (geometry.width() - self.width()) // 2)
        y = geometry.y() + max(0, (geometry.height() - self.height()) // 2)
        self.setPosition(x, y)

    def showEvent(self, event):
        super().showEvent(event)
        self._center_to_owner()
        try:
            hwnd = int(self.winId())
            _apply_dialog_window_theme(hwnd, self._is_dark)
        except Exception:
            pass
        try:
            self.raise_()
            self.requestActivate()
        except Exception:
            pass

    def _finish(self, result):
        if self._closing:
            return
        self._closing = True
        self._result = result
        try:
            self.hide()
            self.close()
        finally:
            self._restore_qml_guards()
            if self._loop is not None and self._loop.isRunning():
                self._loop.quit()

    def closeEvent(self, event):
        if not self._closing:
            self._result = self.ResultCancel
            self._closing = True
            if self._loop is not None and self._loop.isRunning():
                self._loop.quit()
        self._restore_qml_guards()
        super().closeEvent(event)

    def _restore_window_icon_filter(self):
        guard = getattr(self, "_window_icon_filter_guard", None)
        if guard is not None:
            guard.resume()

    def _restore_qt_message_handler(self):
        guard = getattr(self, "_qt_message_handler_guard", None)
        if guard is not None:
            guard.resume()

    def _restore_qml_guards(self):
        self._restore_qt_message_handler()
        self._restore_window_icon_filter()

    @classmethod
    def ask(cls, owner_window, title, text, save_text, discard_text, cancel_text):
        dialog = cls(owner_window, title, text, save_text, discard_text, cancel_text)
        dialog.show()
        dialog._loop = QEventLoop()
        dialog._loop.exec()
        return dialog._result


def _apply_dwm_shadow(hwnd):
    """Restore DWM drop shadow for a frameless window."""
    try:
        class _MARGINS(ctypes.Structure):
            _fields_ = [("left",ctypes.c_int),("right",ctypes.c_int),
                        ("top",ctypes.c_int),("bottom",ctypes.c_int)]
        m = _MARGINS(1, 1, 1, 1)
        ctypes.windll.dwmapi.DwmExtendFrameIntoClientArea(hwnd, ctypes.byref(m))
    except Exception:
        pass


class WindowIconFilterGuard:
    """Temporarily disable the app's top-level icon event filter for QML windows."""

    def __init__(self):
        self._active = False

    def suspend(self):
        if self._active:
            return
        app = QApplication.instance()
        suspend = getattr(app, "suspend_window_icon_filter", None) if app else None
        if callable(suspend):
            suspend()
            self._active = True

    def resume(self):
        if not self._active:
            return
        self._active = False
        app = QApplication.instance()
        resume = getattr(app, "resume_window_icon_filter", None) if app else None
        if callable(resume):
            resume()


class QtMessageHandlerGuard:
    """Temporarily restore Qt's default message handler while QML is active."""

    def __init__(self):
        self._active = False
        self._previous_handler = None

    def suspend(self):
        if self._active:
            return
        try:
            self._previous_handler = qInstallMessageHandler(None)
            self._active = True
        except Exception:
            self._previous_handler = None

    def resume(self):
        if not self._active:
            return
        self._active = False
        try:
            qInstallMessageHandler(self._previous_handler)
        except Exception:
            pass
        self._previous_handler = None


class BoardWindow(QWidget):
    _WINDOW_TITLE = "小黑板 | Luminalium"

    # Emitted after the window has fully closed (animation finished, super().close() called).
    # Listeners should discard their reference so a fresh window is created next time.
    window_fully_closed = Signal()

    def __init__(self):
        super().__init__()
        self._window_icon_filter_guard = WindowIconFilterGuard()
        self._window_icon_filter_guard.suspend()
        self._qt_message_handler_guard = QtMessageHandlerGuard()
        self._qt_message_handler_guard.suspend()
        self._is_closing = False
        self._animation = None
        self._native_filter = None
        self._qml = None
        self.destroyed.connect(lambda *_: self._restore_qml_guards())

        # Create QQuickWidget as child — fills the entire QWidget.
        # QQuickWidget uses QQuickRenderControl internally, which avoids
        # the GPU context deadlock that QQuickView triggers on Windows
        # when QWebEngineView is already using the GPU.
        self._qml = QQuickWidget(self)
        self._qml.setResizeMode(QQuickWidget.SizeRootObjectToView)

        qmlRegisterType(NativeBoardItem, "LuminaliumBoard", 1, 0, "NativeBoardItem")

        self.setWindowTitle(self._WINDOW_TITLE)

        if sys.platform == "win32":
            self.setWindowFlags(Qt.Window)
        else:
            self.setWindowFlags(
                Qt.Window
                | Qt.CustomizeWindowHint
                | Qt.WindowTitleHint
                | Qt.WindowSystemMenuHint
                | Qt.WindowCloseButtonHint
                | Qt.WindowMaximizeButtonHint
            )

        icon = load_app_icon()
        if not icon.isNull():
            self.setWindowIcon(icon)

        self.backend = BoardBackend(self)
        self._qml.rootContext().setContextProperty("backend", self.backend)
        self._qml.rootContext().setContextProperty("windowTitle", self._WINDOW_TITLE)
        self._qml.rootContext().setContextProperty(
            "titleBarHeight", TitleBarNativeFilter.TITLEBAR_H
        )

        # Icons directory
        base_dir = os.path.dirname(
            os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
        )
        icons_dir = os.path.join(base_dir, "icons")
        icons_url = QUrl.fromLocalFile(icons_dir).toString() + "/"
        self._settings_path = SETTINGS_PATH
        self._settings_mtime = None
        self._restore_maximized_after_fullscreen = False
        (
            self._board_toolbar_position,
            self._board_background_color,
            self._board_popup_bg,
            self._board_popup_border,
            self._board_eraser_mode,
            self._board_pen_stroke_enabled,
            self._board_window_enter_animation,
        ) = _read_board_settings()

        self._qml.rootContext().setContextProperty("iconsDir", icons_url)
        self._qml.rootContext().setContextProperty("showToolText", cfg.showToolbarText.value)
        self._qml.rootContext().setContextProperty(
            "boardToolbarPosition", self._board_toolbar_position
        )
        self._qml.rootContext().setContextProperty(
            "boardBackgroundColor", self._board_background_color
        )
        self._qml.rootContext().setContextProperty(
            "boardPopupBackgroundColor", self._board_popup_bg
        )
        self._qml.rootContext().setContextProperty(
            "boardPopupBorderColor", self._board_popup_border
        )
        self._qml.rootContext().setContextProperty(
            "boardEraserMode", self._board_eraser_mode
        )
        self._qml.rootContext().setContextProperty(
            "boardPenStrokeEnabled", self._board_pen_stroke_enabled
        )
        self._board_theme = _resolve_board_theme()
        self._qml.rootContext().setContextProperty("boardTheme", self._board_theme)
        self._qml.rootContext().setContextProperty(
            "boardAccentColor", self._board_theme.get("accent", "#4A85F6")
        )
        self._qml.rootContext().setContextProperty(
            "boardFontFamily", _resolve_dialog_font_family()
        )
        self._qml.rootContext().setContextProperty(
            "boardToolbarOpacity", cfg.toolbarOpacity.value
        )
        self._qml.rootContext().setContextProperty(
            "boardClearMode", cfg.clearMode.value
        )
        self._qml.rootContext().setContextProperty("penText", _t("toolbar.pen"))
        self._qml.rootContext().setContextProperty("eraserText", _t("toolbar.eraser"))
        self._qml.rootContext().setContextProperty("clearText", _t("toolbar.clear"))
        self._qml.rootContext().setContextProperty("undoText", "撤销")
        self._qml.rootContext().setContextProperty("redoText", "重做")
        self._qml.rootContext().setContextProperty("savePageText", _t("toolbar.save_page"))
        self._qml.rootContext().setContextProperty("addPageText", _t("toolbar.add_page"))
        self._qml.rootContext().setContextProperty(
            "eraserPointText", _t("toolbar.eraser_point")
        )
        self._qml.rootContext().setContextProperty(
            "eraserStrokeText", _t("toolbar.eraser_stroke")
        )
        self._qml.rootContext().setContextProperty("penSizeText", _t("toolbar.pen_size"))
        self._qml.rootContext().setContextProperty(
            "slideClearText", _t("toolbar.slide_clear")
        )
        self._qml.rootContext().setContextProperty(
            "slideClearHintText", _t("toolbar.slide_clear_hint")
        )

        pen_colors_row1 = [
            "#FFFFFF",
            "#000000",
            "#E7E6E6",
            "#44546A",
            "#4472C4",
            "#ED7D31",
            "#FF0000",
            "#A5A5A5",
            "#FFC000",
        ]
        pen_colors_row2 = [
            "#5B9BD5",
            "#70AD47",
            "#C00000",
            "#FFFF00",
            "#92D050",
            "#FF69B4",
            "#00BCD4",
            "#002060",
            "#7030A0",
        ]
        self._qml.rootContext().setContextProperty("penColorsRow1", pen_colors_row1)
        self._qml.rootContext().setContextProperty("penColorsRow2", pen_colors_row2)

        cfg.showToolbarText.valueChanged.connect(self._on_show_tool_text_changed)

        qml_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Board.qml")

        # Process pending events before loading QML to keep UI responsive
        QApplication.processEvents()
        
        try:
            self._qml.setSource(QUrl.fromLocalFile(qml_path))
            # Allow event loop to process during QML scene graph initialization
            QApplication.processEvents()
        finally:
            self._restore_qt_message_handler()

        # Check for QML errors
        if self._qml.status() == QQuickWidget.Status.Error:
            errors = [str(e) for e in self._qml.errors()]
            print(f"[BoardWindow] QML errors: {' | '.join(errors)}")

        # Set initial size
        self.resize(800, 600)
        # Center on screen
        if self.screen():
            geometry = self.screen().availableGeometry()
            x = geometry.x() + (geometry.width() - 800) // 2
            y = geometry.y() + (geometry.height() - 600) // 2
            self.move(x, y)

        # Slide-in animation setup
        self._setup_slide_animation()

        version, code_name = _get_app_version_info()
        watermark_text = ""
        show_watermark = False
        if _is_dev_preview_version(version):
            suffix = version.split(".")[-1]
            w_type = _t(f"watermark.{suffix}")
            display_version = _format_version_display(version)
            device_uuid = get_device_uuid()
            watermark_text = _t("overlay.dev_watermark").format(
                type=w_type, version=display_version, codename=code_name, uuid=device_uuid[:8]
            )
            show_watermark = True

        self._qml.rootContext().setContextProperty("watermarkText", watermark_text)
        self._qml.rootContext().setContextProperty("showWatermark", show_watermark)

        # Track window state
        self._last_state = self.windowState()

        # Strokes path
        self.strokes_path = os.path.join(
            os.path.dirname(os.path.abspath(__file__)), "board_strokes.json"
        )
        self._qml.statusChanged.connect(self._on_status_changed)
        self._settings_watch_timer = QTimer(self)
        self._settings_watch_timer.setInterval(1000)
        self._settings_watch_timer.timeout.connect(self._sync_board_settings)
        self._settings_watch_timer.start()

        # Flush pending events so the event loop stays responsive
        QApplication.processEvents()

    def _restore_window_icon_filter(self):
        guard = getattr(self, "_window_icon_filter_guard", None)
        if guard is not None:
            guard.resume()

    def _restore_qt_message_handler(self):
        guard = getattr(self, "_qt_message_handler_guard", None)
        if guard is not None:
            guard.resume()

    def _restore_qml_guards(self):
        self._restore_qt_message_handler()
        self._restore_window_icon_filter()

    def changeEvent(self, event):
        """Override to detect window state changes (QWidget doesn't have windowStateChanged signal)."""
        if event.type() == QEvent.WindowStateChange:
            state = self.windowState()
            if not (state & Qt.WindowFullScreen):
                self._restore_maximized_after_fullscreen = bool(state & Qt.WindowMaximized)
            self.backend.windowStateChanged.emit()
            self._last_state = state
        super().changeEvent(event)

    def resizeEvent(self, event):
        """Keep QQuickWidget in sync with the QWidget size."""
        super().resizeEvent(event)
        if self._qml:
            self._qml.resize(event.size())

    def _sync_board_settings(self):
        try:
            mtime = os.path.getmtime(self._settings_path)
        except Exception:
            return
        if self._settings_mtime == mtime:
            return
        self._settings_mtime = mtime
        (
            position,
            background_color,
            popup_bg,
            popup_border,
            eraser_mode,
            pen_stroke_enabled,
            window_enter_animation,
        ) = _read_board_settings()
        root = self._qml.rootObject()
        if position != self._board_toolbar_position:
            self._board_toolbar_position = position
            if root:
                root.setProperty("toolbarPosition", position)
        if background_color != self._board_background_color:
            self._board_background_color = background_color
            if root:
                root.setProperty("backgroundColor", background_color)
        if popup_bg != self._board_popup_bg:
            self._board_popup_bg = popup_bg
            if root:
                root.setProperty("popupBackgroundColor", popup_bg)
        if popup_border != self._board_popup_border:
            self._board_popup_border = popup_border
            if root:
                root.setProperty("popupBorderColor", popup_border)
        if eraser_mode != self._board_eraser_mode:
            self._board_eraser_mode = eraser_mode
            if root:
                root.setProperty("eraserMode", eraser_mode)
        if pen_stroke_enabled != self._board_pen_stroke_enabled:
            self._board_pen_stroke_enabled = pen_stroke_enabled
            if root:
                root.setProperty("penStrokeEnabled", pen_stroke_enabled)
        if window_enter_animation != self._board_window_enter_animation:
            self._board_window_enter_animation = window_enter_animation

        # Re-evaluate board theme and toolbar opacity so derived QML colors refresh live
        self._board_theme = _resolve_board_theme()
        self._qml.rootContext().setContextProperty("boardTheme", self._board_theme)
        self._qml.rootContext().setContextProperty(
            "boardAccentColor", self._board_theme.get("accent", "#4A85F6")
        )
        self._qml.rootContext().setContextProperty(
            "boardToolbarOpacity", cfg.toolbarOpacity.value
        )

    def _on_status_changed(self, status):
        if status == QQuickWidget.Status.Ready:
            if os.path.exists(self.strokes_path):
                try:
                    with open(self.strokes_path, "r", encoding="utf-8") as f:
                        document = _normalize_board_document(json.load(f))
                    root = self._qml.rootObject()
                    if root and hasattr(root, "setBoardDocument"):
                        root.setBoardDocument(document)
                except Exception as e:
                    print(f"Failed to load strokes: {e}")

    def set_pen_color(self, r, g, b):
        root = self._qml.rootObject()
        if not root:
            return
        canvas = root.findChild(QObject, "canvas")
        if canvas:
            color = QColor(r, g, b)
            canvas.setProperty("drawColor", color)

    def _on_show_tool_text_changed(self, value):
        self._qml.rootContext().setContextProperty("showToolText", value)

    def toggle_fullscreen(self):
        if self.windowState() & Qt.WindowFullScreen:
            if self._restore_maximized_after_fullscreen:
                self.showMaximized()
            else:
                self.showNormal()
            return

        self._restore_maximized_after_fullscreen = bool(
            self.windowState() & Qt.WindowMaximized
        )
        self.showFullScreen()

    def _on_state_changed(self, state):
        if not (state & Qt.WindowFullScreen):
            self._restore_maximized_after_fullscreen = bool(state & Qt.WindowMaximized)
        self.backend.windowStateChanged.emit()
        self._last_state = state

    def _setup_slide_animation(self):
        self._animation = QPropertyAnimation(self, b"pos")
        self._animation.setDuration(450)
        # Use OutQuint for a more distinct non-linear feel
        self._animation.setEasingCurve(QEasingCurve.OutQuint)

    def save_current_page_png(self):
        root = self._qml.rootObject()
        if not root:
            return
        document = {"currentPage": 1, "pages": [{"strokes": []}]}
        try:
            if hasattr(root, "getBoardDocument"):
                raw = root.getBoardDocument()
                if hasattr(raw, "toVariant"):
                    raw = raw.toVariant()
                document = _normalize_board_document(raw)
        except Exception:
            document = _normalize_board_document(document)

        pages = document.get("pages", [])
        if not isinstance(pages, list) or not pages:
            pages = [{"strokes": []}]
        current_page = document.get("currentPage", 1)
        try:
            current_page = int(current_page)
        except Exception:
            current_page = 1
        current_page = max(1, min(current_page, len(pages)))
        page = pages[current_page - 1] if isinstance(pages[current_page - 1], dict) else {}
        strokes = page.get("strokes", [])
        if not isinstance(strokes, list):
            strokes = []

        canvas = root.findChild(QObject, "canvas")
        width = 0
        height = 0
        if canvas:
            try:
                width = int(canvas.property("width") or 0)
                height = int(canvas.property("height") or 0)
            except Exception:
                width = 0
                height = 0
        if width <= 0 or height <= 0:
            width = max(1, int(self.width()))
            height = max(1, int(self.height()))

        bg = root.property("backgroundColor")
        if not isinstance(bg, str) or not bg:
            bg = self._board_background_color
        if not isinstance(bg, str) or not bg:
            bg = "#202020"

        home_dir = os.path.expanduser("~")
        default_name = f"board-page-{current_page}.png"
        default_path = os.path.join(home_dir, default_name)
        save_path, _ = QFileDialog.getSaveFileName(
            None, "保存当前页为 PNG", default_path, "PNG 图片 (*.png)"
        )
        if not save_path:
            return
        if not save_path.lower().endswith(".png"):
            save_path += ".png"

        image = QImage(width, height, QImage.Format_ARGB32_Premultiplied)
        image.fill(QColor(bg))
        painter = QPainter(image)
        painter.setRenderHint(QPainter.Antialiasing, True)
        for line in strokes:
            if not isinstance(line, dict):
                continue
            try:
                x1 = float(line.get("x1", 0.0))
                y1 = float(line.get("y1", 0.0))
                x2 = float(line.get("x2", 0.0))
                y2 = float(line.get("y2", 0.0))
                if abs(x1) > 1.1 or abs(y1) > 1.1 or abs(x2) > 1.1 or abs(y2) > 1.1:
                    x1 /= max(1.0, float(width))
                    y1 /= max(1.0, float(height))
                    x2 /= max(1.0, float(width))
                    y2 /= max(1.0, float(height))
            except Exception:
                continue

            is_eraser = bool(line.get("isEraser", False))
            if is_eraser:
                painter.setCompositionMode(QPainter.CompositionMode_DestinationOut)
                pen = QPen(Qt.black)
                pen.setWidthF(max(1.0, float(line.get("eraserPx", 20))) + 8.0)
            else:
                painter.setCompositionMode(QPainter.CompositionMode_SourceOver)
                pen = QPen(QColor(line.get("color", "#000000")))
                pen.setWidthF(max(0.5, float(line.get("width", 3))))

            pen.setCapStyle(Qt.RoundCap)
            pen.setJoinStyle(Qt.RoundJoin)
            painter.setPen(pen)

            px1 = x1 * width
            py1 = y1 * height
            px2 = x2 * width
            py2 = y2 * height
            dx = px2 - px1
            dy = py2 - py1
            if (dx * dx + dy * dy) < 2.25:
                painter.drawPoint(QPointF(px1, py1))
            else:
                painter.drawLine(QPointF(px1, py1), QPointF(px2, py2))

        painter.end()
        image.save(save_path, "PNG")

    def showEvent(self, event):
        super().showEvent(event)
        self._is_closing = False
        self._force_close = False

        # Resize QQuickWidget to fill the window
        self._qml.resize(self.width(), self.height())

        if self._board_window_enter_animation and self._animation and self._animation.state() != QPropertyAnimation.Running:
            geom = self.geometry()

            # If we were previously closed (moved up), we need to restore
            # the target position first. We'll center it if it's off-screen.
            screen_geom = self.screen().availableGeometry()
            target_y = geom.y()

            # Check if current y is likely the "closed" position (off-screen)
            if target_y < screen_geom.y():
                # Re-center vertically
                target_y = screen_geom.y() + (screen_geom.height() - geom.height()) // 2

            start_y = target_y - geom.height()

            self._animation.stop()
            self._animation.setStartValue(QPoint(geom.x(), start_y))
            self._animation.setEndValue(QPoint(geom.x(), target_y))
            self._animation.start()

    def close(self):
        """Override close to trigger slide-out animation."""
        if self._is_closing or getattr(self, "_force_close", False):
            super().close()
            return

        self._trigger_close_animation()

    def _trigger_close_animation(self):
        if self._is_closing:
            return

        # Check for content first
        try:
            root = self._qml.rootObject()
            document = {"currentPage": 1, "pages": [{"strokes": []}]}
            if root and hasattr(root, "getBoardDocument"):
                document_raw = root.getBoardDocument()
                if hasattr(document_raw, "toVariant"):
                    document = document_raw.toVariant()
                else:
                    document = document_raw
                document = _normalize_board_document(document)

            if _board_document_has_content(document):
                dialog_result = SaveStrokesDialog.ask(
                    self,
                    _t("dialog.save_strokes_title"),
                    _t("dialog.save_strokes_text"),
                    _t("dialog.save_strokes_yes"),
                    _t("dialog.save_strokes_no"),
                    _t("dialog.cancel"),
                )

                if dialog_result == SaveStrokesDialog.ResultSave:
                    with open(self.strokes_path, "w", encoding="utf-8") as f:
                        json.dump(document, f, cls=ColorEncoder)
                elif dialog_result == SaveStrokesDialog.ResultDiscard:
                    if os.path.exists(self.strokes_path):
                        os.remove(self.strokes_path)
                else:
                    # Cancelled, don't close
                    return
            else:
                if os.path.exists(self.strokes_path):
                    os.remove(self.strokes_path)
        except Exception as e:
            print(f"Error in _trigger_close_animation check: {e}")

        # If the window is fullscreen or maximized, animations might look weird or not work well with y property
        if self.windowState() & (Qt.WindowFullScreen | Qt.WindowMaximized):
            self._force_close = True
            super().close()
            self._restore_qml_guards()
            self.window_fully_closed.emit()
            return

        if self._animation:
            self._is_closing = True
            geom = self.geometry()
            self._animation.setStartValue(geom.topLeft())
            self._animation.setEndValue(QPoint(geom.x(), geom.y() - geom.height()))
            try:
                # Disconnect any old connections first to prevent multiple closes
                self._animation.finished.disconnect(self._on_animation_finished)
            except Exception:
                pass
            self._animation.finished.connect(self._on_animation_finished)
            self._animation.start()
        else:
            self._force_close = True
            super().close()
            self._restore_qml_guards()
            self.window_fully_closed.emit()

    def _on_animation_finished(self):
        if self._is_closing:
            self._force_close = True
            super().close()
            # Reset for next time if the object is reused
            self._is_closing = False
            self._restore_qml_guards()
            self.window_fully_closed.emit()

    def closeEvent(self, event):
        if getattr(self, "_force_close", False):
            super().closeEvent(event)
            self._restore_qml_guards()
            return

        # Intercept manual close (X button) to play animation
        event.ignore()
        self._trigger_close_animation()
