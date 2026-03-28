import json
import os
import sys
import time

from PySide6.QtCore import QObject, QEvent, QTimer, Qt, QUrl, Signal, Slot
from PySide6.QtGui import QColor, QCursor, QGuiApplication, QIcon, QPainter, QPixmap
from PySide6.QtSvg import QSvgRenderer
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWidgets import QSystemTrayIcon
from qfluentwidgets import Action, FluentIcon as FIF, RoundMenu, isDarkTheme, themeColor

from ppt_assistant.core.config import SETTINGS_PATH, cfg
from ppt_assistant.core.i18n import get_language, t


ROOT_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ICON_DIR = os.path.join(ROOT_DIR, "icons")
HTML_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tray_panel.html")
VERSION_PATH = os.path.join(ROOT_DIR, "version.json")


TRAY_COPY = {
    "zh-CN": {
        "subtitle": "\u6258\u76d8\u5feb\u6377\u5165\u53e3",
        "shortcuts_title": "\u5feb\u6377\u64cd\u4f5c",
        "featured_title": "\u5207\u6362\u5de5\u5177\u680f",
        "open": "\u6253\u5f00",
        "run": "\u6267\u884c",
        "settings_desc": "\u8c03\u6574 Luminalium \u8bbe\u7f6e",
        "board_desc": "\u6253\u5f00\u5c0f\u9ed1\u677f\u5de5\u5177",
        "timer_desc": "\u6253\u5f00\u8ba1\u65f6\u5de5\u5177",
        "toggle_desc": "\u663e\u793a\u6216\u9690\u85cf\u6f14\u793a\u5de5\u5177\u680f",
        "restart_desc": "\u91cd\u65b0\u542f\u52a8 Luminalium",
        "exit_desc": "\u9000\u51fa Luminalium",
    },
    "zh-TW": {
        "subtitle": "\u7cfb\u7d71\u5323\u5feb\u6377\u5165\u53e3",
        "shortcuts_title": "\u5feb\u6377\u64cd\u4f5c",
        "featured_title": "\u5207\u63db\u5de5\u5177\u5217",
        "open": "\u958b\u555f",
        "run": "\u57f7\u884c",
        "settings_desc": "\u8abf\u6574 Luminalium \u8a2d\u5b9a",
        "board_desc": "\u6253\u958b\u5c0f\u9ed1\u677f\u5de5\u5177",
        "timer_desc": "\u6253\u958b\u8a08\u6642\u5de5\u5177",
        "toggle_desc": "\u986f\u793a\u6216\u96b1\u85cf\u6f14\u793a\u5de5\u5177\u5217",
        "restart_desc": "\u91cd\u65b0\u555f\u52d5 Luminalium",
        "exit_desc": "\u7d50\u675f Luminalium",
    },
    "yue-HK": {
        "subtitle": "\u6258\u76e4\u5feb\u6377\u5165\u53e3",
        "shortcuts_title": "\u5feb\u6377\u64cd\u4f5c",
        "featured_title": "\u5207\u63db\u5de5\u5177\u5217",
        "open": "\u6253\u958b",
        "run": "\u57f7\u884c",
        "settings_desc": "\u8abf\u6574 Luminalium \u8a2d\u5b9a",
        "board_desc": "\u6253\u958b\u9ed1\u677f\u5de5\u5177",
        "timer_desc": "\u6253\u958b\u8a08\u6642\u5de5\u5177",
        "toggle_desc": "\u986f\u793a\u6216\u96b1\u85cf\u6f14\u793a\u5de5\u5177\u5217",
        "restart_desc": "\u91cd\u65b0\u555f\u52d5 Luminalium",
        "exit_desc": "\u7d50\u675f Luminalium",
    },
    "en-US": {
        "subtitle": "Tray quick access",
        "shortcuts_title": "Quick Actions",
        "featured_title": "Toolbar Visibility",
        "open": "Open",
        "run": "Run",
        "settings_desc": "Adjust Luminalium settings",
        "board_desc": "Open the board tool",
        "timer_desc": "Open the timer tool",
        "toggle_desc": "Show or hide the toolbar",
        "restart_desc": "Restart Luminalium",
        "exit_desc": "Exit Luminalium",
    },
}


def _icon_url(filename: str) -> str:
    path = os.path.join(ICON_DIR, filename)
    if not os.path.exists(path):
        return ""
    return QUrl.fromLocalFile(path).toString()


def _hex_to_rgb(value: str):
    text = str(value or "").strip().lstrip("#")
    if len(text) == 8:
        text = text[2:]
    if len(text) != 6:
        return None
    try:
        return int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16)
    except ValueError:
        return None


def _rgb_to_hex(red: int, green: int, blue: int) -> str:
    return f"#{max(0, min(255, red)):02x}{max(0, min(255, green)):02x}{max(0, min(255, blue)):02x}"


def _mix_color(base: str, target: str, ratio: float) -> str:
    base_rgb = _hex_to_rgb(base)
    target_rgb = _hex_to_rgb(target)
    if not base_rgb or not target_rgb:
        return base or target or "#000000"
    red = round(base_rgb[0] + (target_rgb[0] - base_rgb[0]) * ratio)
    green = round(base_rgb[1] + (target_rgb[1] - base_rgb[1]) * ratio)
    blue = round(base_rgb[2] + (target_rgb[2] - base_rgb[2]) * ratio)
    return _rgb_to_hex(red, green, blue)


def _rgba(hex_color: str, alpha: float) -> str:
    rgb = _hex_to_rgb(hex_color)
    if not rgb:
        return f"rgba(0, 0, 0, {alpha})"
    return f"rgba({rgb[0]}, {rgb[1]}, {rgb[2]}, {max(0.0, min(1.0, alpha)):.3f})"


def _relative_luminance(hex_color: str) -> float:
    rgb = _hex_to_rgb(hex_color)
    if not rgb:
        return 0.0

    def _channel(value: int) -> float:
        normalized = value / 255.0
        if normalized <= 0.03928:
            return normalized / 12.92
        return ((normalized + 0.055) / 1.055) ** 2.4

    red, green, blue = (_channel(v) for v in rgb)
    return 0.2126 * red + 0.7152 * green + 0.0722 * blue


def _resolve_text_color(preferred: str, background: str, is_dark: bool) -> str:
    if _hex_to_rgb(preferred):
        if is_dark and _relative_luminance(preferred) < 0.35:
            return "#f5f5f5"
        if not is_dark and _relative_luminance(preferred) > 0.92:
            return "#1a1c1e"
        return preferred
    return "#f5f5f5" if is_dark else "#1a1c1e"


def _load_settings_json():
    if not os.path.exists(SETTINGS_PATH):
        return {}
    try:
        with open(SETTINGS_PATH, "rb") as file:
            raw = file.read()
    except Exception:
        return {}

    for encoding in ("utf-8", "utf-8-sig", "gbk", "gb18030", "cp1252"):
        try:
            text = raw.decode(encoding, errors="ignore")
            data = json.loads(text)
        except Exception:
            continue
        return data if isinstance(data, dict) else {}

    return {}


def _localized_copy():
    lang = get_language()
    fallback_lang = "zh-TW" if lang == "yue-HK" else "zh-CN"
    return TRAY_COPY.get(lang) or TRAY_COPY.get(fallback_lang) or TRAY_COPY["en-US"]


def _load_version_text():
    if not os.path.exists(VERSION_PATH):
        return ""

    try:
        with open(VERSION_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception:
        return ""

    version = str(data.get("version", "") or "").strip()
    build = str(data.get("build", "") or "").strip()
    code_name = str(data.get("code_name", "") or "").strip()

    parts = [part for part in [version, f"Build {build}" if build else "", code_name] if part]
    return "  ".join(parts)


PRESET_THEME_BASES = {
    "default": {
        "light": {
            "bg_app": "#f7f8f9",
            "bg_surface": "#ffffff",
            "text_primary": "#1a1c1e",
            "text_secondary": "#5e6368",
            "accent": "#3275f5",
            "divider": "rgba(0, 0, 0, 0.08)",
            "card_bg": "rgba(0, 0, 0, 0.03)",
            "card_border": "rgba(0, 0, 0, 0.02)",
            "item_hover": "rgba(0, 0, 0, 0.05)",
            "shadow_color": "rgba(0, 0, 0, 0.06)",
            "shadow_main": "0 4px 12px var(--shadow-color)",
        },
        "dark": {
            "bg_app": "#151515",
            "bg_surface": "#1e1e1e",
            "text_primary": "#e5e5e5",
            "text_secondary": "#909090",
            "accent": "#4a85f6",
            "divider": "rgba(255, 255, 255, 0.08)",
            "card_bg": "rgba(255, 255, 255, 0.03)",
            "card_border": "rgba(255, 255, 255, 0.02)",
            "item_hover": "rgba(255, 255, 255, 0.05)",
            "shadow_color": "rgba(0, 0, 0, 0.30)",
            "shadow_main": "0 8px 32px var(--shadow-color)",
        },
    },
    "material-you": {
        "light": {
            "bg_app": "#f6f0ff",
            "bg_surface": "#fff7ff",
            "text_primary": "#1d1b20",
            "text_secondary": "#49454f",
            "accent": "#7a3bdb",
        },
        "dark": {
            "bg_app": "#1c1329",
            "bg_surface": "#2a1d3b",
            "text_primary": "#e6e1e5",
            "text_secondary": "#cac4d0",
            "accent": "#cda7ff",
        },
    },
    "red-sunrise": {
        "light": {
            "bg_app": "#fff5f3",
            "bg_surface": "#ffffff",
            "text_primary": "#2b1512",
            "text_secondary": "#5d403a",
            "accent": "#e5523c",
        },
        "dark": {
            "bg_app": "#1a0b0a",
            "bg_surface": "#2b1512",
            "text_primary": "#ffd5d0",
            "text_secondary": "#e7bdb7",
            "accent": "#ff907b",
        },
    },
    "emptiness-color": {
        "light": {
            "bg_app": "#f5f6f8",
            "bg_surface": "#ffffff",
            "text_primary": "#1b1f23",
            "text_secondary": "#5b636b",
            "accent": "#6b7280",
            "divider": "rgba(27, 31, 35, 0.08)",
            "card_bg": "rgba(27, 31, 35, 0.03)",
            "card_border": "rgba(27, 31, 35, 0.06)",
            "item_hover": "rgba(27, 31, 35, 0.06)",
            "shadow_color": "rgba(27, 31, 35, 0.08)",
            "shadow_main": "0 4px 12px var(--shadow-color)",
        },
        "dark": {
            "bg_app": "#101113",
            "bg_surface": "#1b1c1f",
            "text_primary": "#f1f3f5",
            "text_secondary": "#c3c7cc",
            "accent": "#a1a6ad",
            "divider": "rgba(230, 233, 238, 0.12)",
            "card_bg": "rgba(161, 166, 173, 0.14)",
            "card_border": "rgba(161, 166, 173, 0.22)",
            "item_hover": "rgba(161, 166, 173, 0.20)",
            "shadow_color": "rgba(0, 0, 0, 0.40)",
            "shadow_main": "0 10px 30px var(--shadow-color)",
        },
    },
    "mung-bean": {
        "light": {
            "bg_app": "#f3fff7",
            "bg_surface": "#ffffff",
            "text_primary": "#0f3d2a",
            "text_secondary": "#3f6b57",
            "accent": "#45b97c",
        },
        "dark": {
            "bg_app": "#0d1a14",
            "bg_surface": "#16231c",
            "text_primary": "#e7fff1",
            "text_secondary": "#a0a8a4",
            "accent": "#7fe3b1",
        },
    },
    "orange-wish": {
        "light": {
            "bg_app": "#fff6ee",
            "bg_surface": "#ffffff",
            "text_primary": "#361904",
            "text_secondary": "#584232",
            "accent": "#ff8a3d",
        },
        "dark": {
            "bg_app": "#1c1108",
            "bg_surface": "#2a190d",
            "text_primary": "#ffdcc0",
            "text_secondary": "#d6c3b6",
            "accent": "#ffb677",
        },
    },
    "year-of-horse": {
        "light": {
            "bg_app": "#fff0f0",
            "bg_surface": "#ffffff",
            "text_primary": "#410002",
            "text_secondary": "#690005",
            "accent": "#e60000",
            "divider": "rgba(230, 0, 0, 0.10)",
            "card_bg": "rgba(230, 0, 0, 0.08)",
            "card_border": "rgba(230, 0, 0, 0.15)",
            "item_hover": "rgba(230, 0, 0, 0.12)",
            "shadow_color": "rgba(200, 0, 0, 0.15)",
            "shadow_main": "0 6px 22px var(--shadow-color)",
        },
        "dark": {
            "bg_app": "#2a0505",
            "bg_surface": "#451212",
            "text_primary": "#ffd700",
            "text_secondary": "#ffb300",
            "accent": "#ff4500",
            "divider": "rgba(255, 69, 0, 0.30)",
            "card_bg": "rgba(255, 69, 0, 0.15)",
            "card_border": "rgba(255, 69, 0, 0.25)",
            "item_hover": "rgba(255, 69, 0, 0.22)",
            "shadow_color": "rgba(0, 0, 0, 0.50)",
            "shadow_main": "0 12px 34px var(--shadow-color)",
        },
    },
}


def _theme_variant_name() -> str:
    return "dark" if isDarkTheme() else "light"


def _monet_tokens(palette: dict, is_dark: bool):
    accent = str(palette.get("primary") or palette.get("accent") or QColor(themeColor()).name())
    background = str(palette.get("background") or "#f2f3f5")
    surface = str(palette.get("surface") or "#ffffff")
    preferred_text = str(palette.get("text") or "#000000")
    bg_app = _mix_color(accent, "#000000", 0.78) if is_dark else background
    bg_surface = _mix_color(accent, "#000000", 0.70) if is_dark else surface
    text_primary = _resolve_text_color(preferred_text, bg_surface, is_dark)
    text_secondary = _rgba(text_primary, 0.70 if is_dark else 0.68)
    divider = "rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)"
    shadow_color = "rgba(0, 0, 0, 0.50)" if is_dark else _rgba(accent, 0.18)
    return {
        "bg_app": bg_app,
        "bg_surface": bg_surface,
        "text_primary": text_primary,
        "text_secondary": text_secondary,
        "accent": accent,
        "divider": divider,
        "card_bg": _rgba(accent, 0.18 if is_dark else 0.10),
        "card_border": _rgba(accent, 0.28 if is_dark else 0.20),
        "item_hover": _rgba(accent, 0.22 if is_dark else 0.14),
        "shadow_color": shadow_color,
        "shadow_main": "0 12px 34px var(--shadow-color)" if is_dark else "0 6px 22px var(--shadow-color)",
    }


def _resolve_theme_base(theme_id: str, variant: str):
    theme_key = theme_id if theme_id in PRESET_THEME_BASES else "default"
    base = PRESET_THEME_BASES.get(theme_key, PRESET_THEME_BASES["default"]).get(variant, {})
    if base:
        return dict(base)

    fallback = PRESET_THEME_BASES["default"][variant]
    return dict(fallback)


def _build_theme_tokens(theme_id: str, variant: str):
    is_dark = variant == "dark"
    settings_data = _load_settings_json()
    appearance = settings_data.get("Appearance") if isinstance(settings_data.get("Appearance"), dict) else {}
    palette = appearance.get("MonetPalette") if isinstance(appearance.get("MonetPalette"), dict) else None

    if theme_id in ("monet", "custom") and palette:
        base = _monet_tokens(palette, is_dark)
    else:
        base = _resolve_theme_base(theme_id, variant)

    accent = str(base.get("accent") or QColor(themeColor()).name())
    bg_surface = str(base.get("bg_surface") or "#ffffff")
    bg_app = str(base.get("bg_app") or bg_surface)
    danger = "#ff8e8e" if is_dark else "#d14343"

    return {
        "bg-app": bg_app,
        "bg-surface": bg_surface,
        "text-primary": str(base.get("text_primary") or ("#f5f5f5" if is_dark else "#1a1c1e")),
        "text-secondary": str(base.get("text_secondary") or _rgba("#f5f5f5" if is_dark else "#1a1c1e", 0.70 if is_dark else 0.68)),
        "accent-blue": accent,
        "divider": str(base.get("divider") or ("rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)")),
        "card-bg": str(base.get("card_bg") or _rgba(accent, 0.18 if is_dark else 0.10)),
        "card-border": str(base.get("card_border") or _rgba(accent, 0.28 if is_dark else 0.20)),
        "item-hover": str(base.get("item_hover") or _rgba(accent, 0.22 if is_dark else 0.14)),
        "shadow-color": str(base.get("shadow_color") or ("rgba(0, 0, 0, 0.50)" if is_dark else _rgba(accent, 0.18))),
        "shadow-main": str(base.get("shadow_main") or ("0 12px 34px var(--shadow-color)" if is_dark else "0 6px 22px var(--shadow-color)")),
        "tray-panel-bg": f"linear-gradient(180deg, {_rgba(bg_surface, 0.98)}, {_rgba(bg_app, 0.98)})",
        "tray-header-bg": _rgba(bg_surface, 0.82 if is_dark else 0.90),
        "tray-header-border": str(base.get("divider") or ("rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)")),
        "tray-icon-soft": _rgba(accent, 0.16 if is_dark else 0.10),
        "tray-icon-border": _rgba(accent, 0.28 if is_dark else 0.20),
        "tray-danger": danger,
        "tray-danger-soft": _rgba(danger, 0.16 if is_dark else 0.10),
        "tray-close-hover": _rgba(accent, 0.12 if is_dark else 0.08),
        "tray-bg": _mix_color(bg_app, "#ffffff", 0.55 if is_dark else 0.18),
        "tray-content-bg": "#ffffff" if not is_dark else _mix_color(bg_surface, "#ffffff", 0.06),
        "tray-content-border": "rgba(255, 255, 255, 0.08)" if is_dark else "rgba(0, 0, 0, 0.04)",
        "tray-footer-bg": _rgba(bg_surface, 0.72 if is_dark else 0.50),
        "tray-footer-border": str(base.get("divider") or ("rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)")),
        "tray-action-hover": _rgba(accent, 0.10 if is_dark else 0.08),
    }


class TrayPanelBridge(QObject):
    actionRequested = Signal(str)
    closeRequested = Signal()
    syncRequested = Signal()

    @Slot()
    def requestInitState(self):
        self.syncRequested.emit()

    @Slot(str)
    def trigger(self, action_id):
        if not action_id:
            return
        self.closeRequested.emit()
        self.actionRequested.emit(action_id)

    @Slot()
    def closeMenu(self):
        self.closeRequested.emit()


class TrayPanelWindow(QWebEngineView):
    actionRequested = Signal(str)
    panelHidden = Signal()

    def __init__(self):
        super().__init__()
        self._menu_data = {}
        self._theme_data = {}
        self._page_ready = False
        self._size = (388, 548)

        self.setWindowFlags(Qt.Tool | Qt.FramelessWindowHint | Qt.NoDropShadowWindowHint)
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_NoSystemBackground)
        self.setContextMenuPolicy(Qt.NoContextMenu)
        self.page().setBackgroundColor(Qt.transparent)

        self._channel = QWebChannel(self.page())
        self._bridge = TrayPanelBridge()
        self._channel.registerObject("trayBridge", self._bridge)
        self.page().setWebChannel(self._channel)

        self._bridge.actionRequested.connect(self._forward_action)
        self._bridge.closeRequested.connect(self.hide)
        self._bridge.syncRequested.connect(self._push_state_to_page)

        self.loadFinished.connect(self._on_load_finished)
        self.load(QUrl.fromLocalFile(HTML_PATH))

    def _forward_action(self, action_id: str):
        self.actionRequested.emit(action_id)

    def _on_load_finished(self, ok: bool):
        self._page_ready = bool(ok)
        if ok:
            self._push_state_to_page()

    def set_payload(self, menu_data, theme_data):
        self._menu_data = menu_data or {}
        self._theme_data = theme_data or {}

        primary_items = self._menu_data.get("primaryItems") or []
        # titlebar=48, brand=88, gap+padding=56, each card≈76px, footer=58
        height = 48 + 88 + 56 + len(primary_items) * 76
        if self._menu_data.get("footerActions"):
            height += 58
        self._size = (380, max(360, min(620, height)))
        self.resize(*self._size)

        if self._page_ready:
            self._push_state_to_page()

    def _push_state_to_page(self):
        if not self._page_ready:
            return

        menu_json = json.dumps(self._menu_data, ensure_ascii=False)
        theme_json = json.dumps(self._theme_data, ensure_ascii=False)
        script = (
            "if (window.TrayPanel && typeof window.TrayPanel.setState === 'function') {"
            f"window.TrayPanel.setState({menu_json}, {theme_json});"
            "}"
        )
        self.page().runJavaScript(script)

    def show_at(self, anchor_pos):
        width, height = self._size
        self.resize(width, height)

        screen = QGuiApplication.screenAt(anchor_pos) or QGuiApplication.primaryScreen()
        if screen is not None:
            geometry = screen.availableGeometry()
            x = anchor_pos.x() - width + 18
            y = anchor_pos.y() - height - 14

            if y < geometry.top() + 8:
                y = min(anchor_pos.y() + 14, geometry.bottom() - height - 8)

            x = max(geometry.left() + 8, min(x, geometry.right() - width - 8))
            y = max(geometry.top() + 8, min(y, geometry.bottom() - height - 8))
            self.move(x, y)

        self.show()
        self.raise_()
        self.activateWindow()

        def _focus():
            try:
                self.setFocus(Qt.ActiveWindowFocusReason)
            except Exception:
                pass

        QTimer.singleShot(0, _focus)

    def event(self, event):
        if event.type() == QEvent.WindowDeactivate and self.isVisible():
            QTimer.singleShot(0, self.hide)
        return super().event(event)

    def hideEvent(self, event):
        super().hideEvent(event)
        self.panelHidden.emit()


class SystemTray(QObject):
    show_settings = Signal()
    show_board = Signal()
    show_timer = Signal()
    toggle_overlay = Signal()
    restart_app = Signal()
    exit_app = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.tray_icon = QSystemTrayIcon(parent)
        self._parent = parent
        self._version_text = _load_version_text()
        self._fallback_menu = None
        self._panel = None
        self._last_panel_hide_at = 0.0

        self._update_icon()
        self.refresh_menu()

        self.tray_icon.activated.connect(self._on_activated)
        self.tray_icon.show()

        try:
            cfg.themeMode.valueChanged.connect(lambda *_: self.refresh_menu())
            cfg.themeId.valueChanged.connect(lambda *_: self.refresh_menu())
            cfg.compatibilityMode.valueChanged.connect(lambda *_: self.refresh_menu())
        except Exception:
            pass

    def _render_menu_icon(self, path, size=16):
        if not os.path.exists(path):
            return QIcon()
        color = QColor(245, 245, 245) if isDarkTheme() else QColor(32, 32, 32)
        pixmap = QPixmap(size, size)
        pixmap.fill(Qt.transparent)
        renderer = QSvgRenderer(path)
        if not renderer.isValid():
            return QIcon(path)
        painter = QPainter(pixmap)
        renderer.render(painter)
        painter.setCompositionMode(QPainter.CompositionMode_SourceIn)
        painter.fillRect(pixmap.rect(), color)
        painter.end()
        return QIcon(pixmap)

    def _update_timer_text(self):
        if not hasattr(self, '_act_timer') or not self._act_timer: return
        timer_text = t("tray.timer")
        try:
            from ppt_assistant.core.timer_manager import TimerManager
            tm = TimerManager()
            if tm.remaining_seconds > 0:
                mins, secs = divmod(int(tm.remaining_seconds), 60)
                hrs, mins = divmod(mins, 60)
                time_str = f"{hrs:02d}:{mins:02d}:{secs:02d}" if hrs > 0 else f"{mins:02d}:{secs:02d}"
                timer_text += f" ({time_str})"
        except Exception:
            pass
        self._act_timer.setText(timer_text)

    def _init_fallback_menu(self):
        # Always create a fresh RoundMenu to avoid stale height from clear().
        old = self._fallback_menu
        self._fallback_menu = RoundMenu(parent=self._parent)
        self._fallback_menu.aboutToShow.connect(self._update_timer_text)

        self.tray_icon.setToolTip(t("tray.tooltip"))

        header = Action(QIcon(os.path.join(ICON_DIR, "logo.svg")), t("tray.title"), self._fallback_menu)
        self._fallback_menu.addAction(header)
        self._fallback_menu.addSeparator()

        act_settings = Action(FIF.SETTING, t("tray.settings"), self._fallback_menu)
        act_settings.triggered.connect(self.show_settings.emit)
        self._fallback_menu.addAction(act_settings)

        board_icon = self._render_menu_icon(os.path.join(ICON_DIR, "board-in-board.svg"))
        act_board = Action(board_icon, t("tray.board"), self._fallback_menu)
        act_board.triggered.connect(self.show_board.emit)
        self._fallback_menu.addAction(act_board)

        timer_icon = self._render_menu_icon(os.path.join(ICON_DIR, "timer.svg"))
        self._act_timer = Action(timer_icon, t("tray.timer"), self._fallback_menu)
        self._act_timer.triggered.connect(self.show_timer.emit)
        self._fallback_menu.addAction(self._act_timer)

        if cfg.compatibilityMode.value:
            act_toggle = Action(FIF.APPLICATION, t("tray.toggle"), self._fallback_menu)
            act_toggle.triggered.connect(self.toggle_overlay.emit)
            self._fallback_menu.addAction(act_toggle)

        self._fallback_menu.addSeparator()

        act_restart = Action(FIF.SYNC, t("tray.restart"), self._fallback_menu)
        act_restart.triggered.connect(self.restart_app.emit)
        self._fallback_menu.addAction(act_restart)

        act_exit = Action(FIF.POWER_BUTTON, t("tray.exit"), self._fallback_menu)
        act_exit.triggered.connect(self.exit_app.emit)
        self._fallback_menu.addAction(act_exit)

        # Bind the new menu instance to the tray icon.
        self.tray_icon.setContextMenu(self._fallback_menu)

        # Schedule deletion of the old menu to avoid memory leak.
        if old is not None:
            old.deleteLater()

    def _init_panel(self):
        # Using qfluentwidgets RoundMenu as the primary tray menu.
        # Context menu is set inside _init_fallback_menu.
        self._panel = None

    def _build_menu_data(self):
        copy = _localized_copy()
        timer_label = t("tray.timer")
        try:
            from ppt_assistant.core.timer_manager import TimerManager
            tm = TimerManager()
            if tm.remaining_seconds > 0:
                mins, secs = divmod(int(tm.remaining_seconds), 60)
                hrs, mins = divmod(mins, 60)
                time_str = f"{hrs:02d}:{mins:02d}:{secs:02d}" if hrs > 0 else f"{mins:02d}:{secs:02d}"
                timer_label += f" ({time_str})"
        except Exception:
            pass

        primary_items = [
            {
                "id": "settings",
                "label": t("tray.settings"),
                "description": copy["settings_desc"],
                "icon": _icon_url("settings.svg"),
                "actionLabel": copy["open"],
            },
            {
                "id": "board",
                "label": t("tray.board"),
                "description": copy["board_desc"],
                "icon": _icon_url("board-in-board.svg"),
                "actionLabel": copy["open"],
            },
            {
                "id": "timer",
                "label": timer_label,
                "description": copy["timer_desc"],
                "icon": _icon_url("timer.svg"),
                "actionLabel": copy["open"],
            },
        ]

        if cfg.compatibilityMode.value:
            primary_items.append(
                {
                    "id": "toggle",
                    "label": t("tray.toggle"),
                    "description": copy["toggle_desc"],
                    "icon": "",
                    "actionLabel": copy["run"],
                }
            )

        footer_actions = [
            {
                "id": "restart",
                "label": t("tray.restart"),
                "iconKind": "restart",
            },
            {
                "id": "exit",
                "label": t("tray.exit"),
                "iconKind": "exit",
                "danger": True,
            },
        ]

        return {
            "versionText": self._version_text,
            "primaryItems": primary_items,
            "footerActions": footer_actions,
        }

    def _build_theme_data(self):
        theme_id = str(getattr(cfg.themeId, "value", "default") or "default")
        variant = _theme_variant_name()
        return {
            "themeId": theme_id,
            "themeVariant": variant,
            "tokens": _build_theme_tokens(theme_id, variant),
        }

    def _trigger_action(self, action_id: str):
        if action_id == "settings":
            self.show_settings.emit()
        elif action_id == "board":
            self.show_board.emit()
        elif action_id == "timer":
            self.show_timer.emit()
        elif action_id == "toggle":
            self.toggle_overlay.emit()
        elif action_id == "restart":
            self.restart_app.emit()
        elif action_id == "exit":
            self.exit_app.emit()

    def refresh_menu(self):
        self._init_fallback_menu()
        self._update_icon()

    def _on_panel_hidden(self):
        self._last_panel_hide_at = time.monotonic()

    def _show_panel(self):
        if self._panel is None:
            self._fallback_menu.exec(QCursor.pos())
            return

        now = time.monotonic()
        if now - self._last_panel_hide_at < 0.2:
            return

        if self._panel.isVisible():
            self._panel.hide()
            return

        self._panel.set_payload(self._build_menu_data(), self._build_theme_data())
        self._panel.show_at(QCursor.pos())

    def _on_activated(self, reason):
        if reason in (QSystemTrayIcon.Trigger, QSystemTrayIcon.Context):
            self._show_panel()

    def _update_icon(self):
        logo_path = os.path.join(ICON_DIR, "logo.svg")
        if not os.path.exists(logo_path):
            logo_path = os.path.join(ICON_DIR, "Pen.svg")

        if sys.platform == "win32":
            if os.path.exists(logo_path):
                self.tray_icon.setIcon(QIcon(logo_path))
            return

        if sys.platform == "linux" and os.path.exists(logo_path):
            self.tray_icon.setIcon(QIcon(logo_path))

        try:
            color = themeColor()
            pixmap = QPixmap(64, 64)
            pixmap.fill(Qt.transparent)

            painter = QPainter(pixmap)
            painter.setRenderHint(QPainter.Antialiasing)

            renderer = QSvgRenderer(logo_path)
            if not renderer.isValid():
                self.tray_icon.setIcon(QIcon(logo_path))
                painter.end()
                return

            renderer.render(painter)
            painter.setCompositionMode(QPainter.CompositionMode_SourceIn)
            painter.fillRect(pixmap.rect(), color)
            painter.end()

            self.tray_icon.setIcon(QIcon(pixmap))
        except Exception as e:
            print(f"Error updating tray icon: {e}")
            if os.path.exists(logo_path):
                self.tray_icon.setIcon(QIcon(logo_path))

    def show_message(self, title, message):
        self.tray_icon.showMessage(title, message, QSystemTrayIcon.Information, 2000)
