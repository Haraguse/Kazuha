import importlib
import json
import os
import re
import threading

import psutil
from PySide6.QtCore import QObject, QRect, QTimer, Qt, QUrl, Signal, Slot
from PySide6.QtGui import QColor, QGuiApplication, QIcon, QRegion
from PySide6.QtQuickWidgets import QQuickWidget
from PySide6.QtWidgets import QWidget

from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.config import cfg
from ppt_assistant.core.icon_helper import get_file_icon_base64
from ppt_assistant.core.i18n import t
from ppt_assistant.core.platform_integration import open_path
from ppt_assistant.core.system import get_system_api
from ppt_assistant.core.theme_data import THEMES


def _thumbnail_source_to_url(source):
    text = str(source or "")
    if text.lower().startswith(("data:", "file:", "http://", "https://", "blob:")):
        return text
    return QUrl.fromLocalFile(text).toString()


_CSS_RGB_RE = re.compile(r"^rgba?\((.*)\)$", re.IGNORECASE)


def _parse_css_channel(value) -> int | None:
    text = str(value or "").strip()
    if not text:
        return None
    try:
        if text.endswith("%"):
            number = float(text[:-1])
            return max(0, min(255, int(round(number * 2.55))))
        number = float(text)
        return max(0, min(255, int(round(number))))
    except Exception:
        return None


def _parse_css_alpha(value) -> int | None:
    text = str(value or "").strip()
    if not text:
        return None
    try:
        if text.endswith("%"):
            alpha = float(text[:-1]) / 100.0
        else:
            alpha = float(text)
        return max(0, min(255, int(round(alpha * 255))))
    except Exception:
        return None


def _qml_color(value, fallback: str = "#000000") -> str:
    """Return a color string that Qt/QML accepts reliably.

    QColor does not parse CSS rgba(...) strings in the PySide versions used by
    this project, and invalid QML colors visually fall back to opaque black.
    """
    text = str(value or "").strip()
    match = _CSS_RGB_RE.match(text)
    if match:
        inner = match.group(1).strip()
        alpha_part = None
        if "/" in inner:
            inner, alpha_part = inner.rsplit("/", 1)
            parts = [p for p in re.split(r"[\s,]+", inner.strip()) if p]
        else:
            parts = [p.strip() for p in inner.split(",")]
            if len(parts) == 1:
                parts = [p for p in re.split(r"\s+", inner) if p]
            if len(parts) >= 4:
                alpha_part = parts[3]
                parts = parts[:3]
        if len(parts) >= 3:
            channels = [_parse_css_channel(part) for part in parts[:3]]
            alpha = _parse_css_alpha(alpha_part) if alpha_part is not None else 255
            if all(channel is not None for channel in channels) and alpha is not None:
                red, green, blue = channels
                if alpha >= 255:
                    return f"#{red:02X}{green:02X}{blue:02X}"
                return f"#{alpha:02X}{red:02X}{green:02X}{blue:02X}"

    color = QColor(text)
    if color.isValid():
        if color.alpha() >= 255:
            return color.name(QColor.HexRgb).upper()
        return color.name(QColor.HexArgb).upper()

    fallback_text = str(fallback or "").strip()
    if fallback_text and fallback_text != text:
        return _qml_color(fallback_text, "#000000")
    return "#000000"


def _normalize_theme_mode(raw_theme) -> str:
    from qfluentwidgets import Theme, isDarkTheme

    if isinstance(raw_theme, Theme):
        if raw_theme == Theme.DARK:
            return "dark"
        if raw_theme == Theme.LIGHT:
            return "light"
        return "dark" if isDarkTheme() else "light"
    value = str(raw_theme or "").strip().lower()
    if value in ("light", "dark"):
        return value
    return "dark" if isDarkTheme() else "light"


def _get_theme_palette(theme_id: str, theme_mode: str) -> dict:
    theme_variants = THEMES.get(theme_id) or THEMES.get("default", {})
    palette = theme_variants.get(theme_mode)
    if palette is None:
        fallback_variants = THEMES.get("default", {})
        palette = fallback_variants.get(theme_mode) or fallback_variants.get(
            "light", {}
        )
    return dict(palette)


def _dialog_palette(theme_id: str, theme_mode: str, accent: str) -> dict:
    is_dark = theme_mode == "dark"
    palette = {
        "dialogMask": "rgba(0, 0, 0, 0.2)" if not is_dark else "rgba(0, 0, 0, 0.35)",
        "dialogBg": "rgba(255, 255, 255, 0.98)"
        if not is_dark
        else "rgba(24, 24, 24, 0.96)",
        "dialogBorder": "rgba(0, 0, 0, 0.08)"
        if not is_dark
        else "rgba(255, 255, 255, 0.12)",
        "dialogTitle": "#1a1c1e" if not is_dark else "#FFFFFF",
        "dialogText": "rgba(0, 0, 0, 0.65)"
        if not is_dark
        else "rgba(255, 255, 255, 0.68)",
        "dialogPrimary": accent,
        "dialogButtonText": "#666666" if not is_dark else "#909090",
    }
    if theme_id == "year-of-horse":
        if is_dark:
            palette.update(
                {
                    "dialogBg": "#451212",
                    "dialogBorder": "rgba(255, 69, 0, 0.3)",
                    "dialogTitle": "#FFD700",
                    "dialogText": "#FFB347",
                    "dialogButtonText": "#FFB347",
                }
            )
        else:
            palette.update(
                {
                    "dialogBg": "#FFEBEE",
                    "dialogBorder": "rgba(211, 47, 47, 0.25)",
                    "dialogTitle": "#B71C1C",
                    "dialogText": "rgba(183, 28, 28, 0.68)",
                    "dialogButtonText": "#B71C1C",
                }
            )
    return palette


def _web_default_overlay_palette(theme_mode: str) -> dict:
    if theme_mode == "dark":
        return {
            "accent": "#4A85F6",
            "toolbar_bg": "rgba(40, 40, 40, 0.95)",
            "toolbar_border": "rgba(255, 255, 255, 0.15)",
            "toolbar_line": "rgba(255, 255, 255, 0.15)",
            "toolbar_fg": "#FFFFFF",
            "btn_hover_bg": "rgba(255, 255, 255, 0.1)",
            "btn_active_bg": "rgba(255, 255, 255, 0.2)",
            "status_bg": "rgba(0, 0, 0, 0.85)",
            "status_fg": "#FFFFFF",
            "status_sep": "rgba(255, 255, 255, 0.2)",
            "pageflip_bg": "rgba(40, 40, 40, 0.95)",
            "pageflip_border": "rgba(255, 255, 255, 0.15)",
            "pageflip_fg": "#FFFFFF",
            "pageflip_hint": "rgba(255, 255, 255, 0.6)",
            "pageflip_hover": "rgba(255, 255, 255, 0.1)",
            "popup_bg": "rgba(30, 30, 30, 0.98)",
            "popup_border": "rgba(255, 255, 255, 0.1)",
            "popup_fg": "#FFFFFF",
            "control_bg": "rgba(255, 255, 255, 0.1)",
            "control_hover": "rgba(255, 255, 255, 0.15)",
            "control_active": "rgba(255, 255, 255, 0.2)",
            "text_primary": "#FFFFFF",
            "text_secondary": "rgba(255, 255, 255, 0.6)",
            "thumb_bg": "#FFFFFF",
            "thumb_icon": "#000000",
        }
    return {
        "accent": "#3275F5",
        "toolbar_bg": "rgba(255, 255, 255, 0.95)",
        "toolbar_border": "rgba(0, 0, 0, 0.08)",
        "toolbar_line": "rgba(0, 0, 0, 0.15)",
        "toolbar_fg": "#333333",
        "btn_hover_bg": "rgba(0, 0, 0, 0.06)",
        "btn_active_bg": "rgba(0, 0, 0, 0.12)",
        "status_bg": "rgba(255, 255, 255, 0.98)",
        "status_fg": "#333333",
        "status_sep": "rgba(0, 0, 0, 0.1)",
        "pageflip_bg": "rgba(255, 255, 255, 0.95)",
        "pageflip_border": "rgba(0, 0, 0, 0.08)",
        "pageflip_fg": "#333333",
        "pageflip_hint": "rgba(0, 0, 0, 0.5)",
        "pageflip_hover": "rgba(0, 0, 0, 0.06)",
        "popup_bg": "rgba(255, 255, 255, 0.98)",
        "popup_border": "rgba(0, 0, 0, 0.08)",
        "popup_fg": "#333333",
        "control_bg": "rgba(0, 0, 0, 0.06)",
        "control_hover": "rgba(0, 0, 0, 0.1)",
        "control_active": "rgba(0, 0, 0, 0.15)",
        "text_primary": "#333333",
        "text_secondary": "rgba(0, 0, 0, 0.6)",
        "thumb_bg": "#FFFFFF",
        "thumb_icon": "#333333",
    }


def _tool_text_map() -> dict:
    return {
        "select": "选择",
        "pen": "画笔",
        "eraser": "橡皮",
        "clear": "清屏",
        "spotlight": "聚光灯",
        "board_in_board": "板中板",
        "timer": "计时器",
        "end": "结束放映",
        "apps": "更多",
        "compatibility": t("overlay.compatibility"),
    }


def _parse_quick_launch_apps() -> list[dict]:
    raw_val = cfg.quickLaunchApps.value
    apps_data = []
    if isinstance(raw_val, str):
        try:
            apps_data = json.loads(raw_val)
        except Exception:
            apps_data = []
    elif isinstance(raw_val, list):
        apps_data = raw_val

    parsed = []
    for app in apps_data:
        if isinstance(app, str):
            path = app
            name = os.path.basename(app)
        elif isinstance(app, dict):
            path = str(app.get("path", "") or "")
            name = str(app.get("name", "") or "")
        else:
            continue
        if not path:
            continue
        if not name:
            name = os.path.basename(path)
        parsed.append(
            {
                "path": path,
                "name": name,
                "icon": get_file_icon_base64(path),
            }
        )
    return parsed


class LinuxOverlayBridge(QObject):
    configChanged = Signal("QVariantMap")
    themeChanged = Signal("QVariantMap")
    pageInfoChanged = Signal(int, int)
    systemStatusChanged = Signal("QVariantMap")
    thumbnailReady = Signal(int, str)
    toolStateReset = Signal(str)
    penColorReset = Signal()
    inkPromptVisibilityChanged = Signal(bool)
    restrictionsChanged = Signal(bool, bool)

    def __init__(self, overlay):
        super().__init__()
        self._overlay = overlay

    @Slot()
    def requestInitState(self):
        QTimer.singleShot(0, self._overlay.apply_initial_state)

    @Slot(str)
    def setTool(self, tool_name):
        if tool_name in ("select", "arrow"):
            self._overlay.request_ptr_arrow.emit()
        elif tool_name == "pen":
            self._overlay.request_ptr_pen.emit()
        elif tool_name == "eraser":
            self._overlay.request_ptr_eraser.emit()

    @Slot(int, int, int)
    def setPenColor(self, r, g, b):
        self._overlay.request_pen_color.emit(int(r), int(g), int(b))

    @Slot()
    def prevPage(self):
        self._overlay.request_prev.emit()

    @Slot()
    def nextPage(self):
        self._overlay.request_next.emit()

    @Slot(int)
    def gotoSlide(self, index):
        self._overlay.request_goto.emit(int(index))

    @Slot()
    def clearScreen(self):
        self._overlay.request_clear.emit()

    @Slot()
    def endShow(self):
        self._overlay.request_end.emit()

    @Slot()
    def toggleSpotlight(self):
        self._overlay.execute_plugin("聚光灯")

    @Slot()
    def toggleBoard(self):
        self._overlay.execute_plugin("板中板")

    @Slot()
    def toggleTimer(self):
        self._overlay.execute_plugin("计时器")

    @Slot(str)
    def launchApp(self, path):
        if not path:
            return
        try:
            open_path(path)
        except Exception as exc:
            print(f"[Overlay] Failed to launch app '{path}': {exc}", flush=True)

    @Slot("QVariantList")
    def updateMask(self, rects):
        self._overlay.update_mask(rects)

    @Slot()
    def releaseFocus(self):
        try:
            self._overlay.clearFocus()
        except Exception:
            pass

    @Slot()
    def resizeNudge(self):
        self._overlay.nudge_size()

    @Slot(int)
    def requestThumbnail(self, index):
        self._overlay.request_thumbnail.emit(int(index))

    @Slot(int)
    def startBackgroundThumbnailCaching(self, total_pages):
        self._overlay.start_background_caching.emit(int(total_pages))

    @Slot(bool)
    def inkPromptResult(self, keep):
        self._overlay.ink_prompt_result.emit(bool(keep))
        self.inkPromptVisibilityChanged.emit(False)

    @Slot(str)
    def logMessage(self, message):
        text = str(message or "").strip()
        if text:
            print(f"[OverlayQML] {text}", flush=True)


class LinuxQmlOverlayWindow(QWidget):
    request_next = Signal()
    request_prev = Signal()
    request_goto = Signal(int)
    request_clear = Signal()
    request_end = Signal()
    request_ptr_arrow = Signal()
    request_ptr_pen = Signal()
    request_ptr_eraser = Signal()
    request_pen_color = Signal(int, int, int)
    request_thumbnail = Signal(int)
    start_background_caching = Signal(int)
    ink_prompt_result = Signal(bool)
    thumbnail_ready = Signal(int, str)

    def __init__(self):
        super().__init__()
        self.monitor = None
        self.plugins = []
        self._plugin_instances = {}
        self._slideshow_hwnd = 0
        self._protected_view = False
        self._presentation_readonly = False
        self._active_on_slideshow = False
        self._current_page = 1
        self._total_page = 1
        self._background_thumbnail_timer = None
        self._pending_thumbnails = []
        self._cached_thumbnails = set()
        self._smtc_info = {
            "status": "",
            "title": "",
            "position_ms": 0,
            "duration_ms": 0,
        }
        self._smtc_thread = None
        self._stop_smtc = False
        self._config_bound = False

        self.setWindowFlags(
            Qt.FramelessWindowHint
            | Qt.Window
            | Qt.WindowStaysOnTopHint
            | Qt.WindowDoesNotAcceptFocus
        )
        self.setAttribute(Qt.WA_TranslucentBackground, True)
        self.setAttribute(Qt.WA_NoSystemBackground, True)
        self.setAttribute(Qt.WA_ShowWithoutActivating, True)

        screen = QGuiApplication.primaryScreen()
        if screen:
            self.setGeometry(screen.geometry())
        icon = load_app_icon()
        if isinstance(icon, QIcon) and not icon.isNull():
            self.setWindowIcon(icon)

        self._bridge = LinuxOverlayBridge(self)
        self._view = None
        self._qml_ready = False

        self.start_background_caching.connect(self.on_start_background_caching)
        self.thumbnail_ready.connect(self.on_thumbnail_ready)

        self._status_timer = None
        self._clock_timer = None

        self.bind_config_signals()

        print(
            "[Overlay] Linux QML overlay shell is active; QML scene will load on first show.",
            flush=True,
        )

    def _ensure_qml_runtime(self) -> bool:
        if self._qml_ready:
            return True
        try:
            print("[Overlay] Initializing Linux QML scene...", flush=True)
            self._view = QQuickWidget(self)
            self._view.setResizeMode(QQuickWidget.SizeRootObjectToView)
            self._view.setClearColor(QColor(0, 0, 0, 0))
            self._view.setAttribute(Qt.WA_AlwaysStackOnTop, True)
            self._view.rootContext().setContextProperty("overlayBridge", self._bridge)
            self._view.rootContext().setContextProperty("bridge", self._bridge)
            self._view.setGeometry(self.rect())

            qml_path = os.path.join(
                os.path.dirname(os.path.abspath(__file__)), "LinuxOverlay.qml"
            )
            print(f"[Overlay] Loading Linux QML overlay: {qml_path}", flush=True)
            self._view.setSource(QUrl.fromLocalFile(qml_path))
            if self._view.status() == QQuickWidget.Error:
                errors = [str(err.toString()) for err in self._view.errors()]
                raise RuntimeError(
                    "Failed to load LinuxOverlay.qml: " + " | ".join(errors)
                )

            self._qml_ready = True

            self._status_timer = QTimer(self)
            self._status_timer.timeout.connect(self._update_system_status)
            self._status_timer.start(2000)

            self._clock_timer = QTimer(self)
            self._clock_timer.timeout.connect(self._emit_system_status)
            self._clock_timer.start(1000)

            self._start_smtc_thread()
            self.apply_initial_state()
            print("[Overlay] Linux QML scene ready.", flush=True)
            return True
        except Exception as exc:
            print(f"[Overlay] Failed to initialize Linux QML scene: {exc}", flush=True)
            return False

    def _emit_system_status(self):
        self._bridge.systemStatusChanged.emit(self._collect_system_status())

    def _update_system_status(self):
        self._emit_system_status()

    def _start_smtc_thread(self):
        def smtc_loop():
            api = get_system_api()
            while not self._stop_smtc:
                try:
                    self._smtc_info = api.get_media_info()
                except Exception:
                    pass
                for _ in range(20):
                    if self._stop_smtc:
                        break
                    import time

                    time.sleep(0.1)

        self._smtc_thread = threading.Thread(target=smtc_loop, daemon=True)
        self._smtc_thread.start()

    def _collect_system_status(self) -> dict:
        try:
            battery = psutil.sensors_battery()
            is_desktop = battery is None
            battery_percent = int(battery.percent) if battery else 100
            battery_charging = bool(battery.power_plugged) if battery else False
        except Exception:
            is_desktop = True
            battery_percent = 100
            battery_charging = False

        try:
            network_online = False
            for iface, stats in psutil.net_if_stats().items():
                if stats.isup and "loopback" not in iface.lower():
                    network_online = True
                    break
        except Exception:
            network_online = False

        smtc_status = str(self._smtc_info.get("status", "") or "")
        smtc_title = str(self._smtc_info.get("title", "") or "")
        smtc_position_ms = int(self._smtc_info.get("position_ms", 0) or 0)
        smtc_duration_ms = int(self._smtc_info.get("duration_ms", 0) or 0)

        return {
            "is_desktop": is_desktop,
            "battery_percent": battery_percent,
            "battery_charging": battery_charging,
            "network_online": network_online,
            "volume": -1,
            "smtc_status": smtc_status,
            "smtc_title": smtc_title,
            "smtc_position_ms": max(0, smtc_position_ms),
            "smtc_duration_ms": max(0, smtc_duration_ms),
        }

    def _build_theme_payload(self) -> dict:
        from qfluentwidgets import themeColor

        theme_id = str(cfg.themeId.value or "default")
        theme_mode = _normalize_theme_mode(cfg.themeMode.value)
        palette = _get_theme_palette(theme_id, theme_mode)
        web_palette = _web_default_overlay_palette(theme_mode)
        if theme_id != "default":
            web_palette.update(palette)

        accent = str(themeColor().name() or web_palette.get("accent", "#3275F5"))
        if theme_id != "default":
            accent = str(web_palette.get("accent") or accent)
        dialog = _dialog_palette(theme_id, theme_mode, accent)

        def color(key: str, fallback: str) -> str:
            return _qml_color(web_palette.get(key, fallback), fallback)

        return {
            "themeId": theme_id,
            "themeMode": theme_mode,
            "darkMode": theme_mode == "dark",
            "accentColor": _qml_color(accent, "#3275F5"),
            "toolbarBg": color("toolbar_bg", "#FFFFFF"),
            "toolbarBorder": color("toolbar_border", "rgba(0, 0, 0, 0.08)"),
            "toolbarFg": color("toolbar_fg", "#333333"),
            "toolbarShadow": color("toolbar_shadow", "rgba(0, 0, 0, 0.15)"),
            "toolbarLine": color("toolbar_line", "rgba(0, 0, 0, 0.08)"),
            "statusBg": color("status_bg", "rgba(255, 255, 255, 0.98)"),
            "statusFg": color("status_fg", "#333333"),
            "statusSep": color("status_sep", "rgba(0, 0, 0, 0.1)"),
            "pageBg": color("pageflip_bg", color("toolbar_bg", "#FFFFFF")),
            "pageBorder": color(
                "pageflip_border", color("toolbar_border", "#14000000")
            ),
            "pageFg": color("pageflip_fg", color("toolbar_fg", "#333333")),
            "pageHint": color("pageflip_hint", "rgba(0, 0, 0, 0.5)"),
            "pageHover": color("pageflip_hover", color("btn_hover_bg", "#0F000000")),
            "pageShadow": color("pageflip_shadow", "rgba(0, 0, 0, 0.15)"),
            "popupBg": color("popup_bg", color("toolbar_bg", "#FFFFFF")),
            "popupBorder": color("popup_border", color("toolbar_border", "#14000000")),
            "popupFg": color("popup_fg", color("toolbar_fg", "#333333")),
            "buttonHover": color("btn_hover_bg", "rgba(0, 0, 0, 0.06)"),
            "buttonActive": color("btn_active_bg", "rgba(0, 0, 0, 0.12)"),
            "cardBg": color("card_bg", "rgba(0, 0, 0, 0.03)"),
            "cardBorder": color("card_border", "rgba(0, 0, 0, 0.02)"),
            "itemHover": color("item_hover", "rgba(0, 0, 0, 0.05)"),
            "controlBg": color("control_bg", color("btn_hover_bg", "#0F000000")),
            "controlHover": color("control_hover", color("btn_hover_bg", "#1A000000")),
            "controlActive": color(
                "control_active", color("btn_active_bg", "#26000000")
            ),
            "textPrimary": color("text_primary", color("toolbar_fg", "#333333")),
            "textSecondary": color("text_secondary", "rgba(0, 0, 0, 0.6)"),
            "thumbBg": color("thumb_bg", "#FFFFFF"),
            "thumbIcon": color("thumb_icon", color("toolbar_fg", "#333333")),
            **{key: _qml_color(value, "#000000") for key, value in dialog.items()},
        }

    def _build_config_payload(self) -> dict:
        toolbar_order = cfg.toolbarOrder.value
        if not isinstance(toolbar_order, list):
            toolbar_order = []
        apps_list = _parse_quick_launch_apps()
        if apps_list and "apps" not in toolbar_order:
            toolbar_order = toolbar_order + ["apps"]
        return {
            "showStatusBar": cfg.showStatusBar.value,
            "disableAnimations": cfg.disableAnimations.value,
            "statusBarShowTime": cfg.statusBarShowTime.value,
            "statusBarShowSeconds": cfg.statusBarShowSeconds.value,
            "statusBarShowBattery": cfg.statusBarShowBattery.value,
            "statusBarShowVolume": cfg.statusBarShowVolume.value,
            "statusBarShowNetwork": cfg.statusBarShowNetwork.value,
            "statusBarShowMusic": cfg.statusBarShowMusic.value,
            "statusBarShowMusicProgress": cfg.statusBarShowMusicProgress.value,
            "showToolbarText": cfg.showToolbarText.value,
            "toolbarOrder": toolbar_order,
            "toolbarPosition": cfg.toolbarPosition.value,
            "compatibilityMode": cfg.compatibilityMode.value,
            "flipperPosition": cfg.flipperPosition.value,
            "showClear": cfg.showClear.value,
            "clearMode": cfg.clearMode.value,
            "showSpotlight": cfg.showSpotlight.value,
            "showBoardInBoard": cfg.showBoardInBoard.value,
            "showTimer": cfg.showTimer.value,
            "scale": cfg.scale.value,
            "safeArea": cfg.safeArea.value,
            "popWindowScale": cfg.popWindowScale.value,
            "toolbarOpacity": cfg.toolbarOpacity.value,
            "sidePageOpacity": cfg.sidePageOpacity.value,
            "strictEdgeAlignment": cfg.strictEdgeAlignment.value,
            "texts": _tool_text_map(),
            "apps": apps_list,
            "disabledTools": cfg.disabledTools.value,
        }

    def apply_initial_state(self):
        self.update_theme()
        self.update_config()
        self.update_page_info(self._current_page, self._total_page)
        self._emit_system_status()
        self.reset_pen_color_ui()
        self.reset_tool_state_ui("select")
        self._bridge.restrictionsChanged.emit(
            self._protected_view, self._presentation_readonly
        )

    def nudge_size(self):
        if self._view is not None:
            self._view.resize(self.size())

    def set_monitor(self, monitor):
        self.monitor = monitor
        if monitor and hasattr(monitor, "set_overlay"):
            monitor.set_overlay(self)

    def on_thumbnail_ready(self, index, path):
        try:
            self._cached_thumbnails.add(int(index))
        except Exception:
            pass
        url = _thumbnail_source_to_url(path)
        self._bridge.thumbnailReady.emit(int(index), url)
        self._process_next_background_thumbnail()

    def on_start_background_caching(self, total_pages):
        if self._background_thumbnail_timer is not None:
            return
        self._pending_thumbnails = [
            i
            for i in range(1, int(total_pages) + 1)
            if i not in self._cached_thumbnails
        ]
        self._background_thumbnail_timer = QTimer(self)
        self._background_thumbnail_timer.setSingleShot(False)
        self._background_thumbnail_timer.timeout.connect(
            self._process_next_background_thumbnail
        )
        self._background_thumbnail_timer.start(500)

    def _process_next_background_thumbnail(self):
        if not self._pending_thumbnails:
            if self._background_thumbnail_timer is not None:
                self._background_thumbnail_timer.stop()
                self._background_thumbnail_timer = None
            return
        next_page = self._pending_thumbnails.pop(0)
        if next_page in self._cached_thumbnails:
            return
        self.request_thumbnail.emit(int(next_page))

    def on_slide_changed(self, current, total):
        self.update_page_info(current, total)

    def update_page_info(self, current, total):
        try:
            current = int(current)
            total = int(total)
        except Exception:
            return
        self._current_page = max(1, current)
        self._total_page = max(self._current_page, total)
        self._bridge.pageInfoChanged.emit(self._current_page, self._total_page)

    def update_mask(self, rects_data):
        region = QRegion()
        try:
            for rect_data in rects_data or []:
                if not isinstance(rect_data, dict):
                    continue
                x = int(float(rect_data.get("x", 0)))
                y = int(float(rect_data.get("y", 0)))
                w = int(float(rect_data.get("width", 0)))
                h = int(float(rect_data.get("height", 0)))
                role = str(rect_data.get("role", "") or "")
                if w <= 0 or h <= 0:
                    continue
                if role == "page-selector":
                    x = max(0, x - 8)
                    w = min(self.width() - x, w + 16)
                    y = 0
                    h = self.height()
                region += QRect(x - 1, y - 1, w + 2, h + 2)
        except Exception as exc:
            print(f"[Overlay] Failed to rebuild QML mask: {exc}", flush=True)
        if region.isEmpty():
            self.clearMask()
        else:
            self.setMask(region)

    def update_theme(self):
        self._bridge.themeChanged.emit(self._build_theme_payload())

    def update_config(self):
        self._bridge.configChanged.emit(self._build_config_payload())

    def reset_tool_state_ui(self, tool: str = "select"):
        self._bridge.toolStateReset.emit(str(tool or "select"))

    def reset_pen_color_ui(self):
        self._bridge.penColorReset.emit()

    def show_ink_prompt(self):
        self._bridge.inkPromptVisibilityChanged.emit(True)

    def execute_plugin(self, name):
        name = str(name or "").strip()
        if not name:
            return
        if name in self._plugin_instances:
            plugin = self._plugin_instances[name]
        else:
            plugin_map = {
                "聚光灯": ("plugins.builtins.spotlight.plugin", "SpotlightPlugin"),
                "板中板": ("plugins.builtins.board.plugin", "BoardPlugin"),
                "计时器": ("plugins.builtins.timer.plugin", "TimerPlugin"),
            }
            spec = plugin_map.get(name)
            if spec is None:
                print(
                    f"[Overlay] Unsupported plugin in Linux QML overlay: {name}",
                    flush=True,
                )
                return
            try:
                module = importlib.import_module(spec[0])
                plugin_cls = getattr(module, spec[1], None)
                if plugin_cls is None:
                    return
                plugin = plugin_cls()
                if hasattr(plugin, "set_context"):
                    plugin.set_context(self)
                self._plugin_instances[name] = plugin
            except Exception as exc:
                print(f"[Overlay] Failed to load plugin {name}: {exc}", flush=True)
                return
        try:
            plugin.execute()
        except Exception as exc:
            print(f"[Overlay] Failed to execute plugin {name}: {exc}", flush=True)

    def bind_config_signals(self):
        if self._config_bound:
            return
        self._config_bound = True
        cfg.themeMode.valueChanged.connect(lambda *_: self.update_theme())
        cfg.themeId.valueChanged.connect(lambda *_: self.update_theme())
        cfg.toolbarOrder.valueChanged.connect(lambda *_: self.update_config())
        cfg.toolbarPosition.valueChanged.connect(lambda *_: self.update_config())
        cfg.quickLaunchApps.valueChanged.connect(lambda *_: self.update_config())
        cfg.showToolbarText.valueChanged.connect(lambda *_: self.update_config())
        cfg.showClear.valueChanged.connect(lambda *_: self.update_config())
        cfg.clearMode.valueChanged.connect(lambda *_: self.update_config())
        cfg.showSpotlight.valueChanged.connect(lambda *_: self.update_config())
        cfg.showBoardInBoard.valueChanged.connect(lambda *_: self.update_config())
        cfg.showTimer.valueChanged.connect(lambda *_: self.update_config())
        cfg.scale.valueChanged.connect(lambda *_: self.update_config())
        cfg.safeArea.valueChanged.connect(lambda *_: self.update_config())
        cfg.popWindowScale.valueChanged.connect(lambda *_: self.update_config())
        cfg.toolbarOpacity.valueChanged.connect(lambda *_: self.update_config())
        cfg.sidePageOpacity.valueChanged.connect(lambda *_: self.update_config())
        cfg.strictEdgeAlignment.valueChanged.connect(lambda *_: self.update_config())
        cfg.disabledTools.valueChanged.connect(lambda *_: self.update_config())
        cfg.flipperPosition.valueChanged.connect(lambda *_: self.update_config())
        cfg.compatibilityMode.valueChanged.connect(lambda *_: self.update_config())
        cfg.showStatusBar.valueChanged.connect(lambda *_: self.update_config())
        cfg.disableAnimations.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowTime.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowSeconds.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowBattery.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowVolume.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowNetwork.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowMusic.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowMusicProgress.valueChanged.connect(
            lambda *_: self.update_config()
        )

    def set_active_on_slideshow(self, active: bool, animate: bool = True):
        self._active_on_slideshow = bool(active)
        if self._active_on_slideshow:
            if not self._ensure_qml_runtime():
                return
            if self._view is not None:
                self._view.resize(self.size())
            super().show()
            self.raise_()
        else:
            super().hide()

    def on_slideshow_start_cleanup(self):
        self.reset_pen_color_ui()
        self.reset_tool_state_ui("select")

    def on_slideshow_end_cleanup(self):
        self._bridge.inkPromptVisibilityChanged.emit(False)

    def _mark_ui_alive(self):
        pass

    def bind_monitor_signals(self):
        pass

    def show_reload_mask(self, text=""):
        pass

    def hide_reload_mask(self):
        pass

    def set_slideshow_hwnd(self, hwnd):
        try:
            self._slideshow_hwnd = int(hwnd or 0)
        except Exception:
            self._slideshow_hwnd = 0

    def set_ppt_restrictions(self, protected_view: bool, presentation_readonly: bool):
        self._protected_view = bool(protected_view)
        self._presentation_readonly = bool(presentation_readonly)
        self._bridge.restrictionsChanged.emit(
            self._protected_view, self._presentation_readonly
        )

    def cleanup(self):
        self._stop_smtc = True
        if self._status_timer is not None:
            try:
                self._status_timer.stop()
            except Exception:
                pass
        if self._clock_timer is not None:
            try:
                self._clock_timer.stop()
            except Exception:
                pass
        if self._background_thumbnail_timer is not None:
            try:
                self._background_thumbnail_timer.stop()
            except Exception:
                pass
            self._background_thumbnail_timer = None
        if self._smtc_thread:
            self._smtc_thread.join(timeout=1.0)
        for plugin in self._plugin_instances.values():
            try:
                terminate = getattr(plugin, "terminate", None)
                if callable(terminate):
                    terminate()
            except Exception:
                pass

    def update_geometry(self, rect, screen):
        if screen is not None:
            try:
                self.setGeometry(screen.geometry())
            except Exception:
                pass
        elif rect is not None:
            try:
                self.setGeometry(rect)
            except Exception:
                pass
        if self._view is not None:
            self._view.resize(self.size())

    def resizeEvent(self, event):
        super().resizeEvent(event)
        if self._view is not None:
            self._view.setGeometry(self.rect())
