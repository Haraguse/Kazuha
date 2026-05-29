import json
import os
import sys

from PySide6.QtCore import QObject, QTimer, Qt, QRect, Signal, QSize
from PySide6.QtGui import QColor, QCursor, QFont, QIcon, QPainter, QPixmap
from PySide6.QtSvg import QSvgRenderer
from PySide6.QtWidgets import QApplication, QHBoxLayout, QMenu, QSystemTrayIcon, QVBoxLayout, QWidget
from qfluentwidgets import (
    Action,
    BodyLabel,
    FluentIcon as FIF,
    Flyout,
    FlyoutViewBase,
    PrimaryPushButton,
    PushButton,
    RoundMenu,
    SubtitleLabel,
    isDarkTheme,
    themeColor,
)
from qfluentwidgets.components.widgets.menu import MenuActionListWidget, MenuAnimationType
from qframelesswindow import WindowEffect

from ppt_assistant.core.config import SETTINGS_PATH, cfg, ROOT_DIR
from ppt_assistant.core.i18n import get_language, t

ICON_DIR = os.path.join(ROOT_DIR, "icons")
VERSION_PATH = os.path.join(ROOT_DIR, "version.json")


def _apply_app_font(widget):
    font = QFont(QApplication.font())
    if sys.platform == "win32":
        font.setHintingPreference(QFont.PreferNoHinting)
        font.setStyleStrategy(QFont.PreferAntialias)
    widget.setFont(font)


class AcrylicRoundMenu(RoundMenu):
    """RoundMenu with DWM acrylic background."""

    def __init__(self, title="", parent=None):
        super().__init__(title, parent)
        self.windowEffect = WindowEffect(self)
        self.setAttribute(Qt.WA_TranslucentBackground)

        self.view.setStyleSheet("MenuActionListWidget { background: transparent; }")

        # Disable touch events to prevent abnormal expansion on touch screens
        self.setAttribute(Qt.WA_AcceptTouchEvents, False)
        self.view.setAttribute(Qt.WA_AcceptTouchEvents, False)
        if hasattr(self.view, 'viewport') and self.view.viewport():
            self.view.viewport().setAttribute(Qt.WA_AcceptTouchEvents, False)

        _apply_app_font(self)
        if isinstance(self.view, MenuActionListWidget):
            _apply_app_font(self.view)

    def showEvent(self, e):
        super().showEvent(e)
        self._update_acrylic_color()
        from ppt_assistant.core.platform_integration import remove_window_border
        remove_window_border(self.winId())

    def exec_(self, pos, ani=True, aniType=MenuAnimationType.FADE_IN_PULL_UP):
        RoundMenu.exec(self, pos, ani, aniType)

    def _update_acrylic_color(self):
        if sys.platform == "win32":
            if isDarkTheme():
                self.windowEffect.setAcrylicEffect(self.winId(), "20202050", True)
            else:
                self.windowEffect.setAcrylicEffect(self.winId(), "F2F2F250", True)


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

    parts = [
        part for part in [version, f"Build {build}" if build else "", code_name] if part
    ]
    return "  ".join(parts)


def is_system_tray_supported() -> bool:
    try:
        available = bool(QSystemTrayIcon.isSystemTrayAvailable())
    except Exception as e:
        print(f"[Tray] Failed to query system tray availability: {e}")
        return False
    if not available:
        print("[Tray] System tray is not available in the current desktop session.")
    return available


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
    accent = str(
        palette.get("primary") or palette.get("accent") or QColor(themeColor()).name()
    )
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
        "shadow_main": "0 12px 34px var(--shadow-color)"
        if is_dark
        else "0 6px 22px var(--shadow-color)",
    }


def _resolve_theme_base(theme_id: str, variant: str):
    theme_key = theme_id if theme_id in PRESET_THEME_BASES else "default"
    base = PRESET_THEME_BASES.get(theme_key, PRESET_THEME_BASES["default"]).get(
        variant, {}
    )
    if base:
        return dict(base)

    fallback = PRESET_THEME_BASES["default"][variant]
    return dict(fallback)


def _build_theme_tokens(theme_id: str, variant: str):
    is_dark = variant == "dark"
    settings_data = _load_settings_json()
    appearance = (
        settings_data.get("Appearance")
        if isinstance(settings_data.get("Appearance"), dict)
        else {}
    )
    palette = (
        appearance.get("MonetPalette")
        if isinstance(appearance.get("MonetPalette"), dict)
        else None
    )

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
        "text-primary": str(
            base.get("text_primary") or ("#f5f5f5" if is_dark else "#1a1c1e")
        ),
        "text-secondary": str(
            base.get("text_secondary")
            or _rgba("#f5f5f5" if is_dark else "#1a1c1e", 0.70 if is_dark else 0.68)
        ),
        "accent-blue": accent,
        "divider": str(
            base.get("divider")
            or ("rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)")
        ),
        "card-bg": str(base.get("card_bg") or _rgba(accent, 0.18 if is_dark else 0.10)),
        "card-border": str(
            base.get("card_border") or _rgba(accent, 0.28 if is_dark else 0.20)
        ),
        "item-hover": str(
            base.get("item_hover") or _rgba(accent, 0.22 if is_dark else 0.14)
        ),
        "shadow-color": str(
            base.get("shadow_color")
            or ("rgba(0, 0, 0, 0.50)" if is_dark else _rgba(accent, 0.18))
        ),
        "shadow-main": str(
            base.get("shadow_main")
            or (
                "0 12px 34px var(--shadow-color)"
                if is_dark
                else "0 6px 22px var(--shadow-color)"
            )
        ),
        "tray-panel-bg": f"linear-gradient(180deg, {_rgba(bg_surface, 0.98)}, {_rgba(bg_app, 0.98)})",
        "tray-header-bg": _rgba(bg_surface, 0.82 if is_dark else 0.90),
        "tray-header-border": str(
            base.get("divider")
            or ("rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)")
        ),
        "tray-icon-soft": _rgba(accent, 0.16 if is_dark else 0.10),
        "tray-icon-border": _rgba(accent, 0.28 if is_dark else 0.20),
        "tray-danger": danger,
        "tray-danger-soft": _rgba(danger, 0.16 if is_dark else 0.10),
        "tray-close-hover": _rgba(accent, 0.12 if is_dark else 0.08),
        "tray-bg": _mix_color(bg_app, "#ffffff", 0.55 if is_dark else 0.18),
        "tray-content-bg": "#ffffff"
        if not is_dark
        else _mix_color(bg_surface, "#ffffff", 0.06),
        "tray-content-border": "rgba(255, 255, 255, 0.08)"
        if is_dark
        else "rgba(0, 0, 0, 0.04)",
        "tray-footer-bg": _rgba(bg_surface, 0.72 if is_dark else 0.50),
        "tray-footer-border": str(
            base.get("divider")
            or ("rgba(255, 255, 255, 0.12)" if is_dark else "rgba(0, 0, 0, 0.08)")
        ),
        "tray-action-hover": _rgba(accent, 0.10 if is_dark else 0.08),
    }


class TrayFlyoutAnchor(QWidget):
    def __init__(self, anchor_pos):
        super().__init__(None)
        self.setWindowFlags(
            Qt.Tool
            | Qt.FramelessWindowHint
            | Qt.NoDropShadowWindowHint
            | Qt.WindowStaysOnTopHint
        )
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_ShowWithoutActivating)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)
        self.setFixedSize(1, 1)
        self.move(anchor_pos)
        from ppt_assistant.core.platform_integration import remove_window_border
        remove_window_border(self.winId())


class ActionConfirmFlyoutView(FlyoutViewBase):
    confirmed = Signal()
    cancelled = Signal()

    def __init__(self, title: str, body: str, confirm_text: str, parent=None):
        super().__init__(parent)
        self.setObjectName("trayActionConfirmFlyoutView")
        self.setFixedWidth(332)
        
        # Disable touch events to prevent abnormal expansion on touch screens
        self.setAttribute(Qt.WA_AcceptTouchEvents, False)

        text_secondary = (
            "rgba(255, 255, 255, 0.70)" if isDarkTheme() else "rgba(0, 0, 0, 0.60)"
        )

        root_layout = QVBoxLayout(self)
        root_layout.setContentsMargins(20, 18, 20, 18)
        root_layout.setSpacing(14)

        title_label = SubtitleLabel(title, self)
        title_label.setWordWrap(True)
        root_layout.addWidget(title_label)

        body_label = BodyLabel(body, self)
        body_label.setStyleSheet(f"color: {text_secondary};")
        body_label.setWordWrap(True)
        root_layout.addWidget(body_label)

        button_layout = QHBoxLayout()
        button_layout.setContentsMargins(0, 2, 0, 0)
        button_layout.setSpacing(12)
        root_layout.addLayout(button_layout)

        cancel_button = PushButton(t("tray.action.confirm.cancel"), self)
        cancel_button.setFixedHeight(34)
        cancel_button.setMinimumWidth(92)
        cancel_button.clicked.connect(self.cancelled.emit)
        button_layout.addWidget(cancel_button)

        confirm_button = PrimaryPushButton(confirm_text, self)
        confirm_button.setFixedHeight(34)
        confirm_button.setMinimumWidth(108)
        confirm_button.clicked.connect(self.confirmed.emit)
        button_layout.addWidget(confirm_button)

        button_layout.addStretch(1)

    def paintEvent(self, e):
        # Override paintEvent to draw transparent background and a subtle border,
        # allowing the acrylic effect on the Flyout to show through.
        painter = QPainter(self)
        painter.setRenderHints(QPainter.Antialiasing)
        
        painter.setBrush(Qt.transparent)
        is_dark = isDarkTheme()
        border_color = QColor(255, 255, 255, 20) if is_dark else QColor(0, 0, 0, 10)
        painter.setPen(border_color)
        
        painter.drawRoundedRect(self.rect().adjusted(1, 1, -1, -1), 8, 8)


class SystemTray(QObject):
    show_settings = Signal()
    show_board = Signal()
    show_timer = Signal()
    show_spotlight = Signal()
    show_logs = Signal()
    toggle_overlay = Signal()
    open_program_dir = Signal()
    open_user_dir = Signal()
    restart_app = Signal()
    exit_app = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.tray_icon = QSystemTrayIcon(parent)
        self._parent = parent
        self._version_text = _load_version_text()
        self._fallback_menu = None
        self._native_menu = None
        self._confirm_flyout = None
        self._confirm_anchor = None
        self._use_native_menu = sys.platform == "linux"

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
        try:
            color = QColor(245, 245, 245) if isDarkTheme() else QColor(32, 32, 32)
            pixmap = QPixmap(size, size)
            pixmap.fill(Qt.transparent)
            renderer = QSvgRenderer(path)
            if not renderer.isValid():
                return QIcon(path)
            painter = QPainter(pixmap)
            if not painter.isActive():
                return QIcon(path)
            painter.setRenderHint(QPainter.Antialiasing)
            renderer.render(painter)
            painter.setCompositionMode(QPainter.CompositionMode_SourceIn)
            painter.fillRect(pixmap.rect(), color)
            painter.end()
            return QIcon(pixmap)
        except Exception as e:
            print(f"Error rendering menu icon {path}: {e}")
            return QIcon(path)

    def _render_colored_icon(self, path, size=18):
        """Render a colored SVG icon without tinting."""
        if not os.path.exists(path):
            return QIcon()
        try:
            pixmap = QPixmap(size, size)
            pixmap.fill(Qt.transparent)
            renderer = QSvgRenderer(path)
            if not renderer.isValid():
                return QIcon(path)
            painter = QPainter(pixmap)
            if not painter.isActive():
                return QIcon(path)
            painter.setRenderHint(QPainter.Antialiasing)
            renderer.render(painter)
            painter.end()
            return QIcon(pixmap)
        except Exception as e:
            print(f"Error rendering colored icon {path}: {e}")
            return QIcon(path)

    def _build_linux_tray_icon(self) -> QIcon:
        raster_candidates = [
            os.path.join(ICON_DIR, "logo.ico"),
            os.path.join(ICON_DIR, "logo.png"),
        ]
        for candidate in raster_candidates:
            if os.path.exists(candidate):
                icon = QIcon(candidate)
                if not icon.isNull():
                    return icon

        symbolic_path = os.path.join(ICON_DIR, "logo.svg")
        preserve_color = True
        if not os.path.exists(symbolic_path):
            symbolic_path = os.path.join(ICON_DIR, "Pen.svg")
            preserve_color = False

        renderer = QSvgRenderer(symbolic_path)
        if not renderer.isValid():
            fallback_path = os.path.join(ICON_DIR, "logo.ico")
            if os.path.exists(fallback_path):
                return QIcon(fallback_path)
            return QIcon(symbolic_path) if os.path.exists(symbolic_path) else QIcon()

        icon = QIcon()
        color = QColor(248, 249, 250) if isDarkTheme() else QColor(32, 32, 32)
        for size in (16, 18, 20, 22, 24, 32, 48, 64):
            pixmap = QPixmap(size, size)
            pixmap.fill(Qt.transparent)

            painter = QPainter(pixmap)
            painter.setRenderHint(QPainter.Antialiasing)
            renderer.render(painter)
            if not preserve_color:
                painter.setCompositionMode(QPainter.CompositionMode_SourceIn)
                painter.fillRect(pixmap.rect(), color)
            painter.end()

            icon.addPixmap(pixmap)

        return icon

    def _update_timer_text(self):
        if not hasattr(self, "_act_timer") or not self._act_timer:
            return
        timer_text = t("tray.timer")
        try:
            from ppt_assistant.core.timer_manager import TimerManager

            tm = TimerManager()
            if tm.remaining_seconds > 0:
                mins, secs = divmod(int(tm.remaining_seconds), 60)
                hrs, mins = divmod(mins, 60)
                time_str = (
                    f"{hrs:02d}:{mins:02d}:{secs:02d}"
                    if hrs > 0
                    else f"{mins:02d}:{secs:02d}"
                )
                timer_text += f" ({time_str})"
        except Exception:
            pass
        self._act_timer.setText(timer_text)

    def _init_fallback_menu(self):
        old = self._fallback_menu
        self._fallback_menu = AcrylicRoundMenu(parent=self._parent)
        self._fallback_menu.aboutToShow.connect(self._update_timer_text)

        self._fallback_menu.setItemHeight(32)
        self._fallback_menu.view.setGraphicsEffect(None)
        self._fallback_menu.hBoxLayout.setContentsMargins(0, 2, 0, 2)
        view = self._fallback_menu.view
        if isinstance(view, MenuActionListWidget):
            view.setIconSize(QSize(18, 18))
            view.setViewportMargins(0, 2, 0, 2)
            view.setMinimumWidth(200)
            view.setMaximumWidth(200)
            _apply_app_font(view)

        self.tray_icon.setToolTip(t("tray.tooltip"))

        header_icon = self._render_colored_icon(os.path.join(ICON_DIR, "logo.svg"), 18)
        header = Action(header_icon, t("tray.title"), self._fallback_menu)
        self._fallback_menu.addAction(header)

        docs_icon = self._render_menu_icon(
            os.path.join(ICON_DIR, "docs.svg") if os.path.exists(os.path.join(ICON_DIR, "docs.svg"))
            else None,
            size=16
        )
        if docs_icon:
            docs_action = Action(docs_icon, t("tray.docs"), self._fallback_menu)
        else:
            docs_action = Action(t("tray.docs"), self._fallback_menu)
        docs_action.triggered.connect(lambda: os.startfile("https://luminalium.sectl.top/"))
        self._fallback_menu.addAction(docs_action)

        self._fallback_menu.addSeparator()

        # Section 1: Tools (board/timer/spotlight)
        board_icon = self._render_menu_icon(
            os.path.join(ICON_DIR, "board-in-board.svg"), size=18
        )
        act_board = Action(board_icon, t("tray.board"), self._fallback_menu)
        act_board.triggered.connect(self.show_board.emit)
        self._fallback_menu.addAction(act_board)

        timer_icon = self._render_menu_icon(os.path.join(ICON_DIR, "timer.svg"), size=18)
        self._act_timer = Action(timer_icon, t("tray.timer"), self._fallback_menu)
        self._act_timer.triggered.connect(self.show_timer.emit)
        self._fallback_menu.addAction(self._act_timer)

        spotlight_icon = self._render_menu_icon(os.path.join(ICON_DIR, "spotlight.svg"), size=18)
        act_spotlight = Action(spotlight_icon, t("tray.spotlight"), self._fallback_menu)
        act_spotlight.triggered.connect(self.show_spotlight.emit)
        self._fallback_menu.addAction(act_spotlight)

        self._fallback_menu.addSeparator()

        # Section 2: Settings & directories
        act_settings = Action(FIF.SETTING, t("tray.settings"), self._fallback_menu)
        act_settings.triggered.connect(self.show_settings.emit)
        self._fallback_menu.addAction(act_settings)

        act_open_program = Action(FIF.FOLDER, t("tray.open_program"), self._fallback_menu)
        act_open_program.triggered.connect(self.open_program_dir.emit)
        self._fallback_menu.addAction(act_open_program)

        act_open_user = Action(FIF.FOLDER, t("tray.open_user"), self._fallback_menu)
        act_open_user.triggered.connect(self.open_user_dir.emit)
        self._fallback_menu.addAction(act_open_user)

        if cfg.compatibilityMode.value:
            act_toggle = Action(FIF.APPLICATION, t("tray.toggle"), self._fallback_menu)
            act_toggle.triggered.connect(self.toggle_overlay.emit)
            self._fallback_menu.addAction(act_toggle)

        self._fallback_menu.addSeparator()

        # Section 3: Restart/exit
        act_restart = Action(FIF.SYNC, t("tray.restart"), self._fallback_menu)
        act_restart.triggered.connect(self._show_restart_confirm)
        self._fallback_menu.addAction(act_restart)

        act_exit = Action(FIF.POWER_BUTTON, t("tray.exit"), self._fallback_menu)
        act_exit.triggered.connect(self._show_exit_confirm)
        self._fallback_menu.addAction(act_exit)

        # Bind the new menu instance to the tray icon.
        self.tray_icon.setContextMenu(self._fallback_menu)

        # Schedule deletion of the old menu to avoid memory leak.
        if old is not None:
            old.deleteLater()

    def _init_native_menu(self):
        old = self._native_menu
        self._native_menu = QMenu(parent=self._parent)
        self._native_menu.setAttribute(Qt.WA_AcceptTouchEvents, False)
        self._native_menu.aboutToShow.connect(self._update_timer_text)
        _apply_app_font(self._native_menu)

        self.tray_icon.setToolTip(t("tray.tooltip"))

        header = Action(
            QIcon(os.path.join(ICON_DIR, "logo.svg")),
            t("tray.title"),
            self._native_menu,
        )
        header.setEnabled(False)
        self._native_menu.addAction(header)

        docs_icon_path = os.path.join(ICON_DIR, "docs.svg")
        if os.path.exists(docs_icon_path):
            docs_icon = QIcon(docs_icon_path)
            docs_action = Action(docs_icon, t("tray.docs"), self._native_menu)
        else:
            docs_action = Action(t("tray.docs"), self._native_menu)
        docs_action.triggered.connect(lambda: os.startfile("https://luminalium.sectl.top/"))
        self._native_menu.addAction(docs_action)

        self._native_menu.addSeparator()

        # Section 1: Tools (board/timer/spotlight)
        board_icon = self._render_menu_icon(
            os.path.join(ICON_DIR, "board-in-board.svg")
        )
        act_board = Action(board_icon, t("tray.board"), self._native_menu)
        act_board.triggered.connect(self.show_board.emit)
        self._native_menu.addAction(act_board)

        timer_icon = self._render_menu_icon(os.path.join(ICON_DIR, "timer.svg"))
        self._act_timer = Action(timer_icon, t("tray.timer"), self._native_menu)
        self._act_timer.triggered.connect(self.show_timer.emit)
        self._native_menu.addAction(self._act_timer)

        spotlight_icon = self._render_menu_icon(os.path.join(ICON_DIR, "spotlight.svg"))
        act_spotlight = Action(spotlight_icon, t("tray.spotlight"), self._native_menu)
        act_spotlight.triggered.connect(self.show_spotlight.emit)
        self._native_menu.addAction(act_spotlight)

        self._native_menu.addSeparator()

        # Section 2: Settings & directories
        act_settings = Action(FIF.SETTING, t("tray.settings"), self._native_menu)
        act_settings.triggered.connect(self.show_settings.emit)
        self._native_menu.addAction(act_settings)

        act_open_program = Action(FIF.FOLDER, t("tray.open_program"), self._native_menu)
        act_open_program.triggered.connect(self.open_program_dir.emit)
        self._native_menu.addAction(act_open_program)

        act_open_user = Action(FIF.FOLDER, t("tray.open_user"), self._native_menu)
        act_open_user.triggered.connect(self.open_user_dir.emit)
        self._native_menu.addAction(act_open_user)

        if cfg.compatibilityMode.value:
            act_toggle = Action(FIF.APPLICATION, t("tray.toggle"), self._native_menu)
            act_toggle.triggered.connect(self.toggle_overlay.emit)
            self._native_menu.addAction(act_toggle)

        self._native_menu.addSeparator()

        # Section 3: Restart/exit
        act_restart = Action(FIF.SYNC, t("tray.restart"), self._native_menu)
        act_restart.triggered.connect(self.restart_app.emit)
        self._native_menu.addAction(act_restart)

        act_exit = Action(FIF.POWER_BUTTON, t("tray.exit"), self._native_menu)
        act_exit.triggered.connect(self.exit_app.emit)
        self._native_menu.addAction(act_exit)

        self.tray_icon.setContextMenu(self._native_menu)

        if old is not None:
            old.deleteLater()

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
            self._show_restart_confirm()
        elif action_id == "exit":
            self._show_exit_confirm()

    def _cleanup_confirm_flyout(self, *_):
        self._confirm_flyout = None
        anchor = self._confirm_anchor
        self._confirm_anchor = None
        if anchor is not None:
            try:
                anchor.hide()
            except Exception:
                pass
            anchor.deleteLater()

    def _close_confirm_flyout(self):
        flyout = self._confirm_flyout
        self._confirm_flyout = None
        if flyout is not None:
            try:
                flyout.close()
            except Exception:
                pass
        else:
            self._cleanup_confirm_flyout()

    def _confirm_restart(self):
        self._close_confirm_flyout()
        QTimer.singleShot(0, self.restart_app.emit)

    def _confirm_exit(self):
        self._close_confirm_flyout()
        QTimer.singleShot(0, self.exit_app.emit)

    def _show_confirm_flyout(
        self, title: str, body: str, confirm_text: str, confirmed_slot
    ):
        flyout = self._confirm_flyout
        if flyout is not None:
            try:
                flyout.raise_()
                flyout.activateWindow()
                return
            except Exception:
                self._cleanup_confirm_flyout()

        if self._fallback_menu is not None and self._fallback_menu.isVisible():
            self._fallback_menu.hide()

        anchor = TrayFlyoutAnchor(QCursor.pos())
        anchor.show()

        view = ActionConfirmFlyoutView(title, body, confirm_text, anchor)
        self._confirm_anchor = anchor
        self._confirm_flyout = Flyout.make(view, anchor)
        from ppt_assistant.core.platform_integration import remove_window_border
        remove_window_border(self._confirm_flyout.winId())
        self._confirm_flyout.view.setGraphicsEffect(None)
        self._confirm_flyout.hBoxLayout.setContentsMargins(0, 0, 0, 0)
        
        # Disable touch events to prevent abnormal expansion on touch screens
        self._confirm_flyout.setAttribute(Qt.WA_AcceptTouchEvents, False)

        # Make the flyout use acrylic background
        self._confirm_flyout.setAttribute(Qt.WA_TranslucentBackground)
        self._confirm_flyout.windowEffect = WindowEffect(self._confirm_flyout)
        
        if sys.platform == "win32":
            if isDarkTheme():
                self._confirm_flyout.windowEffect.setAcrylicEffect(self._confirm_flyout.winId(), "20202050", True)
            else:
                self._confirm_flyout.windowEffect.setAcrylicEffect(self._confirm_flyout.winId(), "F2F2F250", True)
                
        # Make the view transparent so acrylic shows through
        view.setAttribute(Qt.WA_TranslucentBackground)
        view.setStyleSheet("ActionConfirmFlyoutView { background: transparent; }")

        view.cancelled.connect(self._close_confirm_flyout)
        view.confirmed.connect(confirmed_slot)
        self._confirm_flyout.destroyed.connect(self._cleanup_confirm_flyout)

        def _activate():
            current_flyout = self._confirm_flyout
            if current_flyout is None:
                return
            try:
                current_flyout.raise_()
                current_flyout.activateWindow()
            except Exception:
                pass

        QTimer.singleShot(0, _activate)

    def _show_restart_confirm(self):
        self._show_confirm_flyout(
            t("tray.restart.confirm.title"),
            t("tray.restart.confirm.body"),
            t("tray.restart.confirm.confirm"),
            self._confirm_restart,
        )

    def _show_exit_confirm(self):
        self._show_confirm_flyout(
            t("tray.exit.confirm.title"),
            t("tray.exit.confirm.body"),
            t("tray.exit.confirm.confirm"),
            self._confirm_exit,
        )

    def prepare_shutdown(self):
        self._close_confirm_flyout()
        if self._fallback_menu is not None:
            try:
                self._fallback_menu.hide()
            except Exception:
                pass
        try:
            self.tray_icon.setContextMenu(None)
        except Exception:
            pass
        try:
            self.tray_icon.hide()
        except Exception:
            pass

    def refresh_menu(self):
        if self._use_native_menu:
            self._init_native_menu()
        else:
            self._init_fallback_menu()
        self._update_icon()

    def _show_panel(self):
        if self._use_native_menu:
            return
        self._fallback_menu.exec_(QCursor.pos(), ani=True)

    def _on_activated(self, reason):
        if reason in (QSystemTrayIcon.Trigger, QSystemTrayIcon.Context):
            self._show_panel()

    def _update_icon(self):
        # Prefer .ico for Windows tray, then .svg
        logo_path = os.path.join(ICON_DIR, "logo.ico")
        if not os.path.exists(logo_path):
            logo_path = os.path.join(ICON_DIR, "logo.svg")
        if not os.path.exists(logo_path):
            logo_path = os.path.join(ICON_DIR, "Pen.svg")

        if sys.platform == "win32":
            try:
                if os.path.exists(logo_path):
                    self.tray_icon.setIcon(QIcon(logo_path))
            except Exception as e:
                print(f"Error updating tray icon: {e}")
            return

        if sys.platform == "linux":
            try:
                icon = self._build_linux_tray_icon()
                if not icon.isNull():
                    self.tray_icon.setIcon(icon)
                    return
            except Exception as e:
                print(f"Error building Linux tray icon: {e}")

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
