import importlib
import json
import os

import psutil
from PySide6.QtCore import QTimer, Qt, Signal
from PySide6.QtGui import QGuiApplication, QIcon, QRegion
from PySide6.QtWidgets import (
    QBoxLayout,
    QFrame,
    QGridLayout,
    QHBoxLayout,
    QLabel,
    QPushButton,
    QScrollArea,
    QSizePolicy,
    QVBoxLayout,
    QWidget,
)

from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.config import cfg
from ppt_assistant.core.i18n import t
from ppt_assistant.core.platform_integration import open_path


class LinuxCompatOverlayWindow(QWidget):
    request_next = Signal()
    request_prev = Signal()
    request_goto = Signal(int)
    request_clear = Signal()
    request_end = Signal()
    request_ptr_arrow = Signal()
    request_ptr_pen = Signal()
    request_ptr_highlighter = Signal()
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
        self._is_light = False
        self._slideshow_hwnd = 0
        self._protected_view = False
        self._presentation_readonly = False
        self._active_on_slideshow = False
        self._current_page = 1
        self._total_page = 1
        self._current_tool = "select"
        self._current_pen_color = (255, 0, 0)
        self._quick_launch_apps = []
        self._toolbar_buttons = {}
        self._app_buttons = []
        self._color_buttons = []
        self._page_buttons = []
        self._selector_open = False
        self._config_bound = False
        self._warned_ink_prompt = False

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

        self._build_ui()

        self._clock_timer = QTimer(self)
        self._clock_timer.timeout.connect(self._update_clock)
        self._clock_timer.start(1000)

        self._status_timer = QTimer(self)
        self._status_timer.timeout.connect(self._update_system_status)
        self._status_timer.start(2000)

        self.bind_config_signals()
        self.apply_initial_state()
        self._layout_panels()
        self._rebuild_mask()

        print(
            "[Overlay] Linux QWidget compatibility overlay is active; "
            "WebEngine overlay disabled for XWayland stability.",
            flush=True,
        )

    def _build_ui(self):
        self._status_frame = self._create_panel()
        status_layout = QHBoxLayout(self._status_frame)
        status_layout.setContentsMargins(12, 10, 12, 10)
        status_layout.setSpacing(10)
        self._time_label = QLabel("--:--", self._status_frame)
        self._music_label = QLabel("", self._status_frame)
        self._volume_label = QLabel("VOL --", self._status_frame)
        self._network_label = QLabel("NET --", self._status_frame)
        self._battery_label = QLabel("PWR --", self._status_frame)
        for label in (
            self._time_label,
            self._music_label,
            self._volume_label,
            self._network_label,
            self._battery_label,
        ):
            label.setSizePolicy(QSizePolicy.Maximum, QSizePolicy.Preferred)
            status_layout.addWidget(label)

        self._compat_frame = self._create_panel()
        compat_layout = QHBoxLayout(self._compat_frame)
        compat_layout.setContentsMargins(12, 8, 12, 8)
        compat_layout.setSpacing(8)
        self._compat_label = QLabel(
            "Luminalium Compatibility Overlay", self._compat_frame
        )
        compat_layout.addWidget(self._compat_label)

        self._nav_frame = self._create_panel()
        nav_layout = QHBoxLayout(self._nav_frame)
        nav_layout.setContentsMargins(10, 10, 10, 10)
        nav_layout.setSpacing(8)
        self._prev_button = QPushButton("Prev", self._nav_frame)
        self._page_button = QPushButton("1 / 1", self._nav_frame)
        self._next_button = QPushButton("Next", self._nav_frame)
        self._prev_button.clicked.connect(self.request_prev.emit)
        self._next_button.clicked.connect(self.request_next.emit)
        self._page_button.clicked.connect(self._toggle_page_selector)
        nav_layout.addWidget(self._prev_button)
        nav_layout.addWidget(self._page_button)
        nav_layout.addWidget(self._next_button)

        self._toolbar_frame = self._create_panel()
        self._toolbar_layout = QBoxLayout(QBoxLayout.LeftToRight, self._toolbar_frame)
        self._toolbar_layout.setContentsMargins(10, 10, 10, 10)
        self._toolbar_layout.setSpacing(8)

        self._page_selector_frame = self._create_panel()
        selector_layout = QVBoxLayout(self._page_selector_frame)
        selector_layout.setContentsMargins(12, 12, 12, 12)
        selector_layout.setSpacing(8)
        self._page_selector_title = QLabel("Slides", self._page_selector_frame)
        selector_layout.addWidget(self._page_selector_title)
        self._page_selector_scroll = QScrollArea(self._page_selector_frame)
        self._page_selector_scroll.setWidgetResizable(True)
        self._page_selector_scroll.setFrameShape(QFrame.NoFrame)
        self._page_selector_scroll.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self._page_selector_scroll.setVerticalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self._page_selector_content = QWidget(self._page_selector_scroll)
        self._page_selector_grid = QGridLayout(self._page_selector_content)
        self._page_selector_grid.setContentsMargins(0, 0, 0, 0)
        self._page_selector_grid.setSpacing(8)
        self._page_selector_scroll.setWidget(self._page_selector_content)
        selector_layout.addWidget(self._page_selector_scroll)
        self._page_selector_frame.hide()

    def _create_panel(self) -> QFrame:
        panel = QFrame(self)
        panel.setObjectName("compatPanel")
        return panel

    def _clear_layout(self, layout):
        while layout.count():
            item = layout.takeAt(0)
            widget = item.widget()
            child_layout = item.layout()
            if widget is not None:
                widget.deleteLater()
            elif child_layout is not None:
                self._clear_layout(child_layout)

    def _tool_text(self, key: str) -> str:
        trans_map = {
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
        return str(trans_map.get(key, key))

    def _parse_quick_launch_apps(self):
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
            parsed.append({"path": path, "name": name})
        return parsed

    def _set_button_style(
        self, button: QPushButton, *, active: bool = False, accent: bool = False
    ):
        palette = self._palette()
        bg = palette["accent_bg"] if accent or active else palette["button_bg"]
        fg = palette["accent_fg"] if accent or active else palette["text"]
        border = palette["accent"] if active else palette["border"]
        button.setStyleSheet(
            f"""
            QPushButton {{
                background: {bg};
                color: {fg};
                border: 1px solid {border};
                border-radius: 13px;
                padding: 8px 14px;
                font-size: 13px;
                font-weight: 600;
            }}
            QPushButton:hover {{
                border-color: {palette["accent"]};
            }}
            """
        )

    def _set_color_button_style(
        self, button: QPushButton, color_hex: str, active: bool
    ):
        palette = self._palette()
        border = palette["accent"] if active else palette["border"]
        shadow = palette["accent"] if active else "transparent"
        button.setStyleSheet(
            f"""
            QPushButton {{
                min-width: 24px;
                max-width: 24px;
                min-height: 24px;
                max-height: 24px;
                border-radius: 12px;
                border: 2px solid {border};
                background: {color_hex};
            }}
            QPushButton:hover {{
                border-color: {palette["accent"]};
            }}
            """
        )
        button.setProperty("compatShadow", shadow)

    def _set_page_button_style(self, button: QPushButton, active: bool):
        self._set_button_style(button, active=active)
        button.setMinimumHeight(36)

    def _palette(self):
        from qfluentwidgets import Theme, isDarkTheme, themeColor

        mode = cfg.themeMode.value
        is_light = False
        if isinstance(mode, Theme):
            if mode == Theme.AUTO:
                is_light = not isDarkTheme()
            else:
                is_light = mode == Theme.LIGHT
        else:
            mode_str = str(mode).lower()
            if mode_str == "light":
                is_light = True
            elif mode_str == "dark":
                is_light = False
            else:
                is_light = not isDarkTheme()
        self._is_light = is_light
        accent = themeColor().name()
        if is_light:
            return {
                "panel_bg": "rgba(250, 250, 250, 235)",
                "panel_border": "rgba(0, 0, 0, 0.10)",
                "text": "#171717",
                "muted": "#666666",
                "button_bg": "rgba(0, 0, 0, 0.05)",
                "border": "rgba(0, 0, 0, 0.10)",
                "accent": accent,
                "accent_bg": "rgba(255, 255, 255, 0.96)",
                "accent_fg": accent,
            }
        return {
            "panel_bg": "rgba(20, 20, 20, 235)",
            "panel_border": "rgba(255, 255, 255, 0.10)",
            "text": "#F5F5F5",
            "muted": "#B8B8B8",
            "button_bg": "rgba(255, 255, 255, 0.08)",
            "border": "rgba(255, 255, 255, 0.10)",
            "accent": accent,
            "accent_bg": "rgba(255, 255, 255, 0.06)",
            "accent_fg": accent,
        }

    def _apply_frame_styles(self):
        palette = self._palette()
        frame_style = (
            "QFrame#compatPanel {"
            f"background: {palette['panel_bg']};"
            f"border: 1px solid {palette['panel_border']};"
            "border-radius: 18px;"
            "}"
            f"QLabel {{ color: {palette['text']}; }}"
        )
        for frame in (
            self._status_frame,
            self._compat_frame,
            self._nav_frame,
            self._toolbar_frame,
            self._page_selector_frame,
        ):
            frame.setStyleSheet(frame_style)
        for label in (
            self._music_label,
            self._volume_label,
            self._network_label,
            self._battery_label,
            self._page_selector_title,
            self._compat_label,
        ):
            label.setStyleSheet(f"color: {palette['muted']};")
        self._time_label.setStyleSheet(
            f"color: {palette['text']}; font-size: 18px; font-weight: 700;"
        )

    def _refresh_button_styles(self):
        for name, button in self._toolbar_buttons.items():
            self._set_button_style(
                button, active=name == self._current_tool, accent=name == "end"
            )
        for color_hex, button in self._color_buttons:
            self._set_color_button_style(
                button,
                color_hex,
                active=color_hex.lower()
                == self._rgb_to_hex(self._current_pen_color).lower(),
            )
        for index, button in self._page_buttons:
            self._set_page_button_style(button, active=index == self._current_page)
        for button in self._app_buttons:
            self._set_button_style(button, active=False)
        for button in (self._prev_button, self._next_button, self._page_button):
            self._set_button_style(button, active=False)

    def _rgb_to_hex(self, rgb):
        r, g, b = rgb
        return f"#{int(r):02X}{int(g):02X}{int(b):02X}"

    def _rebuild_toolbar(self):
        self._clear_layout(self._toolbar_layout)
        self._toolbar_buttons = {}
        self._app_buttons = []
        self._color_buttons = []

        position = str(cfg.toolbarPosition.value or "bottom").lower()
        if position in ("left", "right"):
            self._toolbar_layout.setDirection(QBoxLayout.TopToBottom)
        else:
            self._toolbar_layout.setDirection(QBoxLayout.LeftToRight)

        order = cfg.toolbarOrder.value
        if not isinstance(order, list) or not order:
            order = [
                "select",
                "pen",
                "eraser",
                "clear",
                "spotlight",
                "board_in_board",
                "timer",
                "end",
            ]
        if "end" not in order:
            order = list(order) + ["end"]

        disabled_tools = (
            cfg.disabledTools.value if isinstance(cfg.disabledTools.value, list) else []
        )
        disabled = {str(item) for item in disabled_tools}

        for tool in order:
            if tool in disabled:
                continue
            if tool == "clear" and not cfg.showClear.value:
                continue
            if tool == "spotlight" and not cfg.showSpotlight.value:
                continue
            if tool == "board_in_board" and not cfg.showBoardInBoard.value:
                continue
            if tool == "timer" and not cfg.showTimer.value:
                continue
            if tool == "apps":
                continue

            button = QPushButton(self._tool_text(tool), self._toolbar_frame)
            button.clicked.connect(
                lambda _checked=False, key=tool: self._handle_tool_action(key)
            )
            self._toolbar_layout.addWidget(button)
            self._toolbar_buttons[tool] = button

        self._quick_launch_apps = self._parse_quick_launch_apps()
        for app in self._quick_launch_apps:
            button = QPushButton(app["name"], self._toolbar_frame)
            button.clicked.connect(
                lambda _checked=False, path=app["path"]: self._launch_app(path)
            )
            self._toolbar_layout.addWidget(button)
            self._app_buttons.append(button)

        color_container = QWidget(self._toolbar_frame)
        color_layout = QHBoxLayout(color_container)
        color_layout.setContentsMargins(2, 2, 2, 2)
        color_layout.setSpacing(6)
        for color_hex in (
            "#FF0000",
            "#FFC000",
            "#FFFF00",
            "#00B050",
            "#00B0F0",
            "#FFFFFF",
            "#000000",
        ):
            button = QPushButton("", color_container)
            button.clicked.connect(
                lambda _checked=False, hex_value=color_hex: (
                    self._set_pen_color_from_hex(hex_value)
                )
            )
            color_layout.addWidget(button)
            self._color_buttons.append((color_hex, button))
        self._toolbar_layout.addWidget(color_container)
        self._refresh_button_styles()

    def _rebuild_page_selector(self):
        self._page_buttons = []
        self._clear_layout(self._page_selector_grid)
        columns = 4
        for index in range(1, max(1, self._total_page) + 1):
            button = QPushButton(str(index), self._page_selector_content)
            button.clicked.connect(
                lambda _checked=False, value=index: self._goto_slide(value)
            )
            row = (index - 1) // columns
            col = (index - 1) % columns
            self._page_selector_grid.addWidget(button, row, col)
            self._page_buttons.append((index, button))
        self._refresh_button_styles()

    def _handle_tool_action(self, tool: str):
        if tool == "select":
            self._current_tool = "select"
            self.request_ptr_arrow.emit()
        elif tool == "pen":
            self._current_tool = "pen"
            self.request_ptr_pen.emit()
        elif tool == "highlight":
            self._current_tool = "highlight"
            self.request_ptr_highlighter.emit()
        elif tool == "eraser":
            self._current_tool = "eraser"
            self.request_ptr_eraser.emit()
        elif tool == "clear":
            self.request_clear.emit()
        elif tool == "spotlight":
            self.execute_plugin("聚光灯")
        elif tool == "board_in_board":
            self.execute_plugin("板中板")
        elif tool == "timer":
            self.execute_plugin("计时器")
        elif tool == "end":
            self.request_end.emit()
        self._refresh_button_styles()

    def _set_pen_color_from_hex(self, color_hex: str):
        raw = str(color_hex or "").strip().lstrip("#")
        if len(raw) != 6:
            return
        rgb = tuple(int(raw[i : i + 2], 16) for i in (0, 2, 4))
        self._current_pen_color = rgb
        self._current_tool = "pen"
        self.request_pen_color.emit(*rgb)
        self.request_ptr_pen.emit()
        self._refresh_button_styles()

    def _launch_app(self, path: str):
        if not path:
            return
        try:
            open_path(path)
        except Exception as exc:
            print(f"[Overlay] Failed to launch app '{path}': {exc}", flush=True)

    def _goto_slide(self, index: int):
        self._selector_open = False
        self._page_selector_frame.hide()
        self.request_goto.emit(int(index))
        self._layout_panels()
        self._rebuild_mask()

    def _toggle_page_selector(self):
        self._selector_open = not self._selector_open
        self._page_selector_frame.setVisible(self._selector_open)
        self._layout_panels()
        self._rebuild_mask()

    def _layout_panels(self):
        if self.width() <= 0 or self.height() <= 0:
            return
        margin = 16

        for frame in (
            self._status_frame,
            self._compat_frame,
            self._nav_frame,
            self._toolbar_frame,
        ):
            frame.adjustSize()

        compat_size = self._compat_frame.sizeHint()
        self._compat_frame.setGeometry(
            margin, margin, compat_size.width(), compat_size.height()
        )

        if cfg.showStatusBar.value:
            status_size = self._status_frame.sizeHint()
            self._status_frame.setVisible(True)
            self._status_frame.setGeometry(
                self.width() - margin - status_size.width(),
                margin,
                status_size.width(),
                status_size.height(),
            )
        else:
            self._status_frame.hide()

        nav_size = self._nav_frame.sizeHint()
        self._nav_frame.setGeometry(
            margin,
            self.height() - margin - nav_size.height(),
            nav_size.width(),
            nav_size.height(),
        )

        toolbar_size = self._toolbar_frame.sizeHint()
        position = str(cfg.toolbarPosition.value or "bottom").lower()
        if position == "top":
            x = max(margin, (self.width() - toolbar_size.width()) // 2)
            y = margin
        elif position == "left":
            x = margin
            y = max(margin, (self.height() - toolbar_size.height()) // 2)
        elif position == "right":
            x = max(margin, self.width() - margin - toolbar_size.width())
            y = max(margin, (self.height() - toolbar_size.height()) // 2)
        else:
            x = max(margin, (self.width() - toolbar_size.width()) // 2)
            y = max(margin, self.height() - margin - toolbar_size.height())
        self._toolbar_frame.setGeometry(
            x, y, toolbar_size.width(), toolbar_size.height()
        )

        if self._selector_open:
            selector_width = 240
            selector_height = min(max(220, self.height() - 120), 420)
            selector_x = (
                margin
                if position == "right"
                else self.width() - margin - selector_width
            )
            selector_y = max(
                margin + compat_size.height() + 12,
                (self.height() - selector_height) // 2,
            )
            self._page_selector_frame.setGeometry(
                selector_x, selector_y, selector_width, selector_height
            )
            self._page_selector_frame.show()
        else:
            self._page_selector_frame.hide()

    def _rebuild_mask(self):
        region = QRegion()
        for widget in (
            self._compat_frame,
            self._status_frame,
            self._nav_frame,
            self._toolbar_frame,
            self._page_selector_frame,
        ):
            if widget.isVisible():
                region += QRegion(widget.geometry().adjusted(-1, -1, 1, 1))
        if region.isEmpty():
            self.clearMask()
        else:
            self.setMask(region)

    def _update_clock(self):
        if not cfg.statusBarShowTime.value:
            self._time_label.hide()
            return
        self._time_label.show()
        from PySide6.QtCore import QTime

        fmt = "HH:mm:ss" if cfg.statusBarShowSeconds.value else "HH:mm"
        self._time_label.setText(QTime.currentTime().toString(fmt))

    def _update_system_status(self):
        try:
            battery = psutil.sensors_battery()
            if cfg.statusBarShowBattery.value:
                self._battery_label.show()
                if battery is None:
                    self._battery_label.setText("PWR Desktop")
                else:
                    suffix = " +" if battery.power_plugged else ""
                    self._battery_label.setText(f"PWR {int(battery.percent)}%{suffix}")
            else:
                self._battery_label.hide()
        except Exception:
            self._battery_label.hide()

        try:
            if cfg.statusBarShowNetwork.value:
                online = False
                for iface, stats in psutil.net_if_stats().items():
                    if stats.isup and "loopback" not in iface.lower():
                        online = True
                        break
                self._network_label.show()
                self._network_label.setText("NET Online" if online else "NET Offline")
            else:
                self._network_label.hide()
        except Exception:
            self._network_label.hide()

        if cfg.statusBarShowVolume.value:
            self._volume_label.show()
            self._volume_label.setText("VOL --")
        else:
            self._volume_label.hide()

        if cfg.statusBarShowMusic.value:
            self._music_label.show()
            self._music_label.setText("")
        else:
            self._music_label.hide()

        self._layout_panels()
        self._rebuild_mask()

    def apply_initial_state(self):
        self.reset_pen_color_ui()
        self.reset_tool_state_ui("select")
        self.update_theme()
        self.update_config()
        self._update_clock()
        self._update_system_status()

    def nudge_size(self):
        self._layout_panels()
        self._rebuild_mask()

    def set_monitor(self, monitor):
        self.monitor = monitor
        if monitor and hasattr(monitor, "set_overlay"):
            monitor.set_overlay(self)

    def on_thumbnail_ready(self, index, path):
        pass

    def on_start_background_caching(self, total_pages):
        pass

    def on_slide_changed(self, current, total):
        self.update_page_info(current, total)

    def update_page_info(self, current, total):
        try:
            current_int = max(1, int(current))
            total_int = max(current_int, int(total))
        except Exception:
            return
        rebuild = total_int != self._total_page
        self._current_page = current_int
        self._total_page = total_int
        self._page_button.setText(f"{self._current_page} / {self._total_page}")
        if rebuild:
            self._rebuild_page_selector()
        else:
            self._refresh_button_styles()

    def update_mask(self, rects_data):
        self._rebuild_mask()

    def update_theme(self):
        self._apply_frame_styles()
        self._refresh_button_styles()

    def update_config(self):
        self._rebuild_toolbar()
        self._page_selector_title.setText("Slides")
        self._layout_panels()
        self._rebuild_mask()

    def reset_tool_state_ui(self, tool: str = "select"):
        self._current_tool = str(tool or "select")
        self._refresh_button_styles()

    def reset_pen_color_ui(self):
        self._current_pen_color = (255, 0, 0)
        self._refresh_button_styles()

    def show_ink_prompt(self):
        if not self._warned_ink_prompt:
            print(
                "[Overlay] Ink prompt is not implemented in Linux compatibility overlay; "
                "defaulting to discard.",
                flush=True,
            )
            self._warned_ink_prompt = True
        self.ink_prompt_result.emit(False)

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
                    f"[Overlay] Unsupported plugin in Linux compatibility overlay: {name}",
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
        cfg.toolbarOrder.valueChanged.connect(lambda *_: self.update_config())
        cfg.toolbarPosition.valueChanged.connect(lambda *_: self.update_config())
        cfg.quickLaunchApps.valueChanged.connect(lambda *_: self.update_config())
        cfg.showToolbarText.valueChanged.connect(lambda *_: self.update_config())
        cfg.showClear.valueChanged.connect(lambda *_: self.update_config())
        cfg.showSpotlight.valueChanged.connect(lambda *_: self.update_config())
        cfg.showBoardInBoard.valueChanged.connect(lambda *_: self.update_config())
        cfg.showTimer.valueChanged.connect(lambda *_: self.update_config())
        cfg.disabledTools.valueChanged.connect(lambda *_: self.update_config())
        cfg.showStatusBar.valueChanged.connect(lambda *_: self.update_config())
        cfg.statusBarShowTime.valueChanged.connect(lambda *_: self._update_clock())
        cfg.statusBarShowSeconds.valueChanged.connect(lambda *_: self._update_clock())
        cfg.statusBarShowBattery.valueChanged.connect(
            lambda *_: self._update_system_status()
        )
        cfg.statusBarShowVolume.valueChanged.connect(
            lambda *_: self._update_system_status()
        )
        cfg.statusBarShowNetwork.valueChanged.connect(
            lambda *_: self._update_system_status()
        )
        cfg.statusBarShowMusic.valueChanged.connect(
            lambda *_: self._update_system_status()
        )

    def set_active_on_slideshow(self, active: bool, animate: bool = True):
        self._active_on_slideshow = bool(active)
        if self._active_on_slideshow:
            self._layout_panels()
            self._rebuild_mask()
            super().show()
            self.raise_()
        else:
            super().hide()

    def on_slideshow_start_cleanup(self):
        self.reset_pen_color_ui()
        self.reset_tool_state_ui("select")

    def on_slideshow_end_cleanup(self):
        self._selector_open = False
        self._page_selector_frame.hide()
        self._layout_panels()
        self._rebuild_mask()

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

    def cleanup(self):
        try:
            self._clock_timer.stop()
        except Exception:
            pass
        try:
            self._status_timer.stop()
        except Exception:
            pass
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
        self._layout_panels()
        self._rebuild_mask()

    def resizeEvent(self, event):
        super().resizeEvent(event)
        self._layout_panels()
        self._rebuild_mask()
