
import os
import json
from PySide6.QtQuick import QQuickView
from PySide6.QtCore import QUrl, Qt, Slot, QObject, QPoint, QTimer, Signal, Property
from PySide6.QtGui import QColor, QIcon, QAction
from PySide6.QtWidgets import QWidget, QLabel, QVBoxLayout, QApplication, QDialog, QMessageBox
from ppt_assistant.core.config import cfg, SETTINGS_PATH
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.theme_data import THEMES
from qfluentwidgets import Theme

def _get_app_version():
    try:
        root_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
        version_path = os.path.join(root_dir, "version.json")
        if not os.path.exists(version_path):
            return ""
        with open(version_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        return str(data.get("version", "")).strip()
    except Exception:
        return ""

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

def _read_board_settings():
    position = "bottom"
    background_color = "#202020"
    popup_bg = ""
    popup_border = ""
    eraser_mode = 0 # 0: Point, 1: Stroke
    pen_stroke_enabled = False
    
    # Read settings file once
    settings_data = {}
    try:
        if os.path.exists(SETTINGS_PATH):
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                settings_data = json.load(f)
    except Exception:
        pass
        
    # Get ThemeId from settings or fallback to cfg
    theme_id = settings_data.get("Appearance", {}).get("ThemeId", cfg.themeId.value)
    theme_mode = settings_data.get("Appearance", {}).get("ThemeMode", cfg.themeMode.value)
    
    board = settings_data.get("BoardInBoard", {}) or {}
    
    # Read eraser mode
    mode_str = board.get("EraserMode", "point")
    if mode_str == "stroke":
        eraser_mode = 1
    else:
        eraser_mode = 0
    pen_stroke_enabled = bool(board.get("PenStrokeEnabled", False))

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
        if pos in ("top", "bottom"):
            position = pos
        return position, background_color, popup_bg, popup_border, eraser_mode, pen_stroke_enabled

    pos = board.get("ToolbarPosition", position)
    if pos in ("top", "bottom"):
        position = pos
    color = board.get("BackgroundColor", background_color)
    if isinstance(color, str) and len(color) == 7 and color.startswith("#"):
        try:
            int(color[1:], 16)
            background_color = color
        except Exception:
            background_color = "#202020"
    return position, background_color, popup_bg, popup_border, eraser_mode, pen_stroke_enabled

def _load_board_toolbar_position():
    position, _, _, _, _, _ = _read_board_settings()
    return position

_TRANSLATIONS = {
    "zh-CN": {
        "watermark.1": "开发中版本",
        "watermark.2": "技术预览版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新评估版本",
        "overlay.dev_watermark": "{type}\n不保证最终品质 （{version}）",
        "toolbar.theme_colors": "主题颜色",
        "toolbar.standard_colors": "标准颜色",
        "toolbar.eraser_point": "掠区擦除",
        "toolbar.eraser_stroke": "笔画擦除",
        "dialog.save_strokes_title": "提示",
        "dialog.save_strokes_text": "是否保留本次笔迹？",
        "dialog.save_strokes_yes": "保留",
        "dialog.save_strokes_no": "不保留",
    },
    "zh-TW": {
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "overlay.dev_watermark": "{type}\n不保證最終品質 （{version}）",
        "toolbar.theme_colors": "主題顏色",
        "toolbar.standard_colors": "標準顏色",
        "toolbar.eraser_point": "掠區擦除",
        "toolbar.eraser_stroke": "筆畫擦除",
        "dialog.save_strokes_title": "提示",
        "dialog.save_strokes_text": "是否保留本次筆跡？",
        "dialog.save_strokes_yes": "保留",
        "dialog.save_strokes_no": "不保留",
    },
    "yue-HK": {
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "overlay.dev_watermark": "{type}\n品質唔包，出事唔好屌我 ({version})",
        "toolbar.theme_colors": "主題色",
        "toolbar.standard_colors": "標準色",
        "toolbar.eraser_point": "掠區擦除",
        "toolbar.eraser_stroke": "筆畫擦除",
        "dialog.save_strokes_title": "提你一提",
        "dialog.save_strokes_text": "要唔要留低呢堆筆跡？",
        "dialog.save_strokes_yes": "留低",
        "dialog.save_strokes_no": "唔留",
    },
    "en-US": {
        "watermark.1": "Dev Build",
        "watermark.2": "Tech Preview",
        "watermark.3": "Release Preview",
        "watermark.4": "Re-evaluation",
        "overlay.dev_watermark": "{type}\nQuality not guaranteed ({version})",
        "toolbar.theme_colors": "Theme Colors",
        "toolbar.standard_colors": "Standard Colors",
        "toolbar.eraser_point": "Point Eraser",
        "toolbar.eraser_stroke": "Stroke Eraser",
        "dialog.save_strokes_title": "Tip",
        "dialog.save_strokes_text": "Keep current strokes?",
        "dialog.save_strokes_yes": "Keep",
        "dialog.save_strokes_no": "Don't Keep",
    },
    "ja-JP": {
        "watermark.1": "開発中のバージョン",
        "watermark.2": "テクニカルプレビュー",
        "watermark.3": "Release Preview",
        "watermark.4": "再評価バージョン",
        "overlay.dev_watermark": "{type}\n品質は保証されません ({version})",
        "toolbar.theme_colors": "テーマの色",
        "toolbar.standard_colors": "標準の色",
        "toolbar.eraser_point": "部分消しゴム",
        "toolbar.eraser_stroke": "ストローク消しゴム",
        "dialog.save_strokes_title": "ヒント",
        "dialog.save_strokes_text": "今回の筆跡を保存しますか？",
        "dialog.save_strokes_yes": "保存する",
        "dialog.save_strokes_no": "保存しない",
    }
}

def _t(key: str) -> str:
    lang = _load_language()
    fallback_lang = "zh-TW" if lang == "yue-HK" else "zh-CN"
    table = _TRANSLATIONS.get(lang) or _TRANSLATIONS.get(fallback_lang) or _TRANSLATIONS["zh-CN"]
    if key in table:
        return table[key]
    default = _TRANSLATIONS.get(fallback_lang) or _TRANSLATIONS["zh-CN"]
    return default.get(key, key)

class ColorEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, QColor):
            return obj.name(QColor.HexArgb)
        return super().default(obj)

class BoardBackend(QObject):
    maximizedChanged = Signal()

    def __init__(self, window):
        super().__init__()
        self._window = window

    @Slot(int, int)
    def moveWindow(self, dx, dy):
        current_pos = self._window.position()
        self._window.setPosition(current_pos + QPoint(dx, dy))

    @Slot()
    def closeWindow(self):
        self._window.close()
        
    @Slot()
    def startDrag(self):
        self._window.startSystemMove()

    @Slot()
    def minimizeWindow(self):
        self._window.showMinimized()

    @Slot()
    def toggleMaximized(self):
        if self._window.windowState() == Qt.WindowMaximized:
            self._window.showNormal()
        else:
            self._window.showMaximized()
        self.maximizedChanged.emit()

    @Slot(int)
    def startResize(self, edge):
        # edge: 1=Top, 2=Bottom, 4=Left, 8=Right
        # Combined: 5=TopLeft, 6=BottomLeft, 9=TopRight, 10=BottomRight
        self._window.startSystemResize(Qt.Edge(edge))

    @Property(bool, notify=maximizedChanged)
    def isMaximized(self):
        return self._window.windowState() == Qt.WindowMaximized

class BoardWindow(QQuickView):
    def __init__(self):
        super().__init__()
        self.setTitle("板中板 - Kazuha")
        self.setResizeMode(QQuickView.SizeRootObjectToView)
        
        # Native window with restricted flags
        # Allow Close and Maximize. Disallow Minimize.
        # Note: Qt.CustomizeWindowHint hides the title bar unless Qt.WindowTitleHint is present.
        self.setFlags(Qt.Window | Qt.CustomizeWindowHint | Qt.WindowTitleHint | Qt.WindowSystemMenuHint | Qt.WindowCloseButtonHint | Qt.WindowMaximizeButtonHint)

        icon = load_app_icon()
        if not icon.isNull():
            self.setIcon(icon)
        
        self.backend = BoardBackend(self)
        self.rootContext().setContextProperty("backend", self.backend)
        
        # Icons directory
        base_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
        icons_dir = os.path.join(base_dir, "icons")
        icons_url = QUrl.fromLocalFile(icons_dir).toString() + "/"
        self._settings_path = SETTINGS_PATH
        self._settings_mtime = None
        self._board_toolbar_position, self._board_background_color, self._board_popup_bg, self._board_popup_border, self._board_eraser_mode, self._board_pen_stroke_enabled = _read_board_settings()

        self.rootContext().setContextProperty("iconsDir", icons_url)
        self.rootContext().setContextProperty("showToolText", cfg.showToolbarText.value)
        self.rootContext().setContextProperty("boardToolbarPosition", self._board_toolbar_position)
        self.rootContext().setContextProperty("boardBackgroundColor", self._board_background_color)
        self.rootContext().setContextProperty("boardPopupBackgroundColor", self._board_popup_bg)
        self.rootContext().setContextProperty("boardPopupBorderColor", self._board_popup_border)
        self.rootContext().setContextProperty("boardEraserMode", self._board_eraser_mode)
        self.rootContext().setContextProperty("boardPenStrokeEnabled", self._board_pen_stroke_enabled)
        self.rootContext().setContextProperty("penText", _t("toolbar.pen"))
        self.rootContext().setContextProperty("eraserText", _t("toolbar.eraser"))
        self.rootContext().setContextProperty("clearText", _t("toolbar.clear"))
        self.rootContext().setContextProperty("themeColorsText", _t("toolbar.theme_colors"))
        self.rootContext().setContextProperty("standardColorsText", _t("toolbar.standard_colors"))
        self.rootContext().setContextProperty("eraserPointText", _t("toolbar.eraser_point"))
        self.rootContext().setContextProperty("eraserStrokeText", _t("toolbar.eraser_stroke"))
        
        # Colors
        theme_bases = [
            "#FFFFFF", "#000000", "#E7E6E6", "#44546A", "#4472C4",
            "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47"
        ]
        standard_colors = [
            "#C00000", "#FF0000", "#FFC000", "#FFFF00", "#92D050",
            "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0"
        ]
        self.rootContext().setContextProperty("themeColors", theme_bases)
        self.rootContext().setContextProperty("standardColors", standard_colors)

        cfg.showToolbarText.valueChanged.connect(self._on_show_tool_text_changed)
        
        qml_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Board.qml")
        self.setSource(QUrl.fromLocalFile(qml_path))
        
        # Set initial size
        self.resize(800, 600)
        # Center on screen
        if self.screen():
            geometry = self.screen().availableGeometry()
            x = geometry.x() + (geometry.width() - 800) // 2
            y = geometry.y() + (geometry.height() - 600) // 2
            self.setPosition(x, y)

        # Watermark
        version = _get_app_version()
        watermark_text = ""
        show_watermark = False
        if _is_dev_preview_version(version):
            suffix = version.split(".")[-1]
            w_type = _t(f"watermark.{suffix}")
            display_version = _format_version_display(version)
            watermark_text = _t("overlay.dev_watermark").format(type=w_type, version=display_version)
            show_watermark = True
        
        self.rootContext().setContextProperty("watermarkText", watermark_text)
        self.rootContext().setContextProperty("showWatermark", show_watermark)

        # Track window state
        self._last_state = self.windowState()
        self.windowStateChanged.connect(self._on_state_changed)
        
        # Strokes path
        self.strokes_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "board_strokes.json")
        self.statusChanged.connect(self._on_status_changed)
        self._settings_watch_timer = QTimer(self)
        self._settings_watch_timer.setInterval(400)
        self._settings_watch_timer.timeout.connect(self._sync_board_settings)
        self._settings_watch_timer.start()

    def _sync_board_settings(self):
        try:
            mtime = os.path.getmtime(self._settings_path)
        except Exception:
            return
        if self._settings_mtime == mtime:
            return
        self._settings_mtime = mtime
        position, background_color, popup_bg, popup_border, eraser_mode, pen_stroke_enabled = _read_board_settings()
        root = self.rootObject()
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

    def _on_status_changed(self, status):
        if status == QQuickView.Ready:
            # Load strokes
            if os.path.exists(self.strokes_path):
                try:
                    with open(self.strokes_path, "r", encoding="utf-8") as f:
                        strokes = json.load(f)
                    if strokes and isinstance(strokes, list):
                        # Pass to QML
                        # To safely call QML function with complex object, use QMetaObject.invokeMethod
                        # But simpler is to use a QObject wrapper or rely on PySide6's automatic conversion if possible.
                        # Direct attribute access might fail if method is not found on QQuickItem wrapper.
                        # Let's try to find the Canvas child item, as setStrokes is defined in Canvas.
                        # Wait, setStrokes is defined in Canvas (lines 91-99 of Board.qml) but Canvas is nested inside Rectangle (board) inside root Rectangle.
                        # But I defined setStrokes inside Canvas.
                        # The root object is the top-level Rectangle. It does NOT have setStrokes.
                        
                        # We need to find the canvas object.
                        root = self.rootObject()
                        canvas = root.findChild(QObject, "canvas")
                        if canvas:
                            canvas.setStrokes(strokes)
                        else:
                            print("Canvas object not found in QML")

                except Exception as e:
                    print(f"Failed to load strokes: {e}")

    def _on_show_tool_text_changed(self, value):
        self.rootContext().setContextProperty("showToolText", value)

    def _on_state_changed(self, state):
        self.backend.maximizedChanged.emit()
        self._last_state = state

    def closeEvent(self, event):
        if getattr(self, "_force_close", False):
            super().closeEvent(event)
            return

        # Check if there are strokes
        try:
            root = self.rootObject()
            canvas = root.findChild(QObject, "canvas")
            strokes = []
            if canvas:
                strokes_raw = canvas.getStrokes()
                if hasattr(strokes_raw, "toVariant"):
                    strokes = strokes_raw.toVariant()
                else:
                    strokes = strokes_raw
            else:
                print("Canvas object not found in closeEvent")
                
            if strokes and len(strokes) > 0:
                # Show native dialog
                msg_box = QMessageBox()
                msg_box.setWindowTitle(_t("dialog.save_strokes_title"))
                msg_box.setText(_t("dialog.save_strokes_text"))
                yes_btn = msg_box.addButton(_t("dialog.save_strokes_yes"), QMessageBox.YesRole)
                no_btn = msg_box.addButton(_t("dialog.save_strokes_no"), QMessageBox.NoRole)
                cancel_btn = msg_box.addButton(QMessageBox.Cancel)
                
                msg_box.exec()
                
                clicked = msg_box.clickedButton()
                if clicked == yes_btn:
                    # Save strokes
                    with open(self.strokes_path, "w", encoding="utf-8") as f:
                        json.dump(strokes, f, cls=ColorEncoder)
                elif clicked == no_btn:
                    # Clear strokes
                    if os.path.exists(self.strokes_path):
                        os.remove(self.strokes_path)
                else:
                    # Cancel
                    event.ignore()
                    return

            else:
                # No strokes, clear file just in case
                if os.path.exists(self.strokes_path):
                    os.remove(self.strokes_path)
                    
        except Exception as e:
            print(f"Error in closeEvent: {e}")
            
        super().closeEvent(event)
