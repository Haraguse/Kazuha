
import os
import json
from PySide6.QtQuick import QQuickView, QQuickPaintedItem
from PySide6.QtQml import qmlRegisterType
from PySide6.QtCore import QUrl, Qt, Slot, QObject, QPoint, QPointF, QTimer, Signal, Property, QEventLoop, QSize, QRect, QRectF, QPropertyAnimation, QEasingCurve
from PySide6.QtGui import QColor, QIcon, QAction, QGuiApplication, QPainter, QImage, QPen
from ppt_assistant.core.config import cfg, SETTINGS_PATH, qconfig
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
    eraser_mode = 0 # 0: Point, 1: Stroke
    pen_stroke_enabled = False
    
    # Read settings file once
    settings_data = _load_settings_data()
        
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
        if pos in ("top", "bottom", "left", "right"):
            position = pos
        return position, background_color, popup_bg, popup_border, eraser_mode, pen_stroke_enabled

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
    return position, background_color, popup_bg, popup_border, eraser_mode, pen_stroke_enabled

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
    theme_palette = THEMES.get(theme_id, THEMES["default"]).get(theme_mode, THEMES["default"][theme_mode])
    is_dark = theme_mode == "dark"

    palette = {
        "darkMode": is_dark,
        "windowBg": "#181818" if is_dark else "#FFFFFF",
        "dialogBg": "#991E1E1E" if is_dark else "#99FFFFFF",  # 0.6 opacity -> 0.6 * 255 = 153 ≈ 0x99
        "dialogBorder": "#0AFFFFFF" if is_dark else "#0A000000", # 0.04 opacity -> 10 ≈ 0x0A (视觉减弱描边粗度)
        "dialogTitle": "#E5E5E5" if is_dark else "#191919",
        "dialogText": "#E5E5E5" if is_dark else "#191919",
        "textSecondary": "#909090" if is_dark else "#666666",
        "accent": theme_palette.get("accent", "#4A85F6" if is_dark else "#3275F5"),
        "buttonHover": theme_palette.get("item_hover", "#0FFFFFFF" if is_dark else "#0A000000"), # 0.06 -> 0x0F, 0.04 -> 0x0A
        "buttonActive": theme_palette.get("btn_active_bg", "#1EFFFFFF" if is_dark else "#1E000000"), # 0.12 -> 0x1E
        "cardShadow": "#26000000" if is_dark else "#08000000", # 0.15 -> 0x26, 0.03 -> 0x08
    }

    if theme_id == "year-of-horse":
        palette.update({
            "windowBg": "#3A0E0E" if is_dark else "#FFF0F0",
            "dialogBg": "rgba(255, 69, 0, 0.10)" if is_dark else "rgba(255, 235, 238, 0.95)",
            "dialogBorder": "rgba(255, 69, 0, 0.30)" if is_dark else "rgba(211, 47, 47, 0.25)",
            "dialogTitle": "#FFD700" if is_dark else "#B71C1C",
            "dialogText": "#FFB347" if is_dark else "#B71C1C",
            "accent": "#FF4500" if is_dark else "#D32F2F",
            "buttonHover": "rgba(255, 69, 0, 0.22)" if is_dark else "rgba(255, 0, 0, 0.12)",
            "buttonActive": "rgba(255, 69, 0, 0.30)" if is_dark else "rgba(211, 47, 47, 0.25)",
            "cardShadow": "rgba(0, 0, 0, 0.40)" if is_dark else "rgba(180, 0, 0, 0.08)",
        })
    elif theme_id != "default":
        popup_bg = theme_palette.get("popup_bg", "")
        palette.update({
            "windowBg": popup_bg if popup_bg else palette["windowBg"],
            "dialogBg": theme_palette.get("popup_bg", palette["dialogBg"]),
            "dialogBorder": theme_palette.get("popup_border", palette["dialogBorder"]),
            "dialogTitle": theme_palette.get("popup_fg", palette["dialogTitle"]),
            "dialogText": theme_palette.get("popup_fg", palette["dialogText"]),
            "cardShadow": theme_palette.get("toolbar_shadow", palette["cardShadow"]),
        })

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
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ctypes.byref(val), ctypes.sizeof(val))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ctypes.byref(val), ctypes.sizeof(val))
        if is_dark:
            caption = ctypes.c_int(0x00181818)
            text_col = ctypes.c_int(0x00FFFFFF)
            border = ctypes.c_int(0x00181818)
        else:
            caption = ctypes.c_int(_DWM_COLOR_DEFAULT)
            text_col = ctypes.c_int(_DWM_COLOR_DEFAULT)
            border = ctypes.c_int(_DWM_COLOR_DEFAULT)
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ctypes.byref(caption), ctypes.sizeof(caption))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ctypes.byref(text_col), ctypes.sizeof(text_col))
        dwmapi.DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ctypes.byref(border), ctypes.sizeof(border))
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
        "overlay.dev_watermark": "{type}\n不保证最终品质 （{version}）",
        "toolbar.theme_colors": "主题颜色",
        "toolbar.standard_colors": "标准颜色",
        "toolbar.pen_size": "画笔粗细",
        "toolbar.eraser_size": "橡皮粗细",
        "toolbar.eraser_point": "掠区擦除",
        "toolbar.eraser_stroke": "笔画擦除",
        "dialog.save_strokes_title": "提示",
        "dialog.save_strokes_text": "是否保留本次笔迹？",
        "dialog.save_strokes_yes": "保留",
        "dialog.save_strokes_no": "不保留",
        "dialog.cancel": "取消",
    },
    "zh-TW": {
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "overlay.dev_watermark": "{type}\n不保證最終品質 （{version}）",
        "toolbar.theme_colors": "主題顏色",
        "toolbar.standard_colors": "標準顏色",
        "toolbar.pen_size": "畫筆粗細",
        "toolbar.eraser_size": "橡皮粗細",
        "toolbar.eraser_point": "掠區擦除",
        "toolbar.eraser_stroke": "筆畫擦除",
        "dialog.save_strokes_title": "提示",
        "dialog.save_strokes_text": "是否保留本次筆跡？",
        "dialog.save_strokes_yes": "保留",
        "dialog.save_strokes_no": "不保留",
        "dialog.cancel": "取消",
    },
    "yue-HK": {
        "watermark.1": "開發中版本",
        "watermark.2": "技術預覽版",
        "watermark.3": "Release Preview",
        "watermark.4": "重新評估版本",
        "overlay.dev_watermark": "{type}\n品質唔包，出事唔好屌我 ({version})",
        "toolbar.theme_colors": "主題色",
        "toolbar.standard_colors": "標準色",
        "toolbar.pen_size": "畫筆粗細",
        "toolbar.eraser_size": "橡皮粗細",
        "toolbar.eraser_point": "掠區擦除",
        "toolbar.eraser_stroke": "筆畫擦除",
        "dialog.save_strokes_title": "提你一提",
        "dialog.save_strokes_text": "要唔要留低呢堆筆跡？",
        "dialog.save_strokes_yes": "留低",
        "dialog.save_strokes_no": "唔留",
        "dialog.cancel": "取消",
    },
    "en-US": {
        "watermark.1": "Dev Build",
        "watermark.2": "Tech Preview",
        "watermark.3": "Release Preview",
        "watermark.4": "Re-evaluation",
        "overlay.dev_watermark": "{type}\nQuality not guaranteed ({version})",
        "toolbar.theme_colors": "Theme Colors",
        "toolbar.standard_colors": "Standard Colors",
        "toolbar.pen_size": "Pen Size",
        "toolbar.eraser_size": "Eraser Size",
        "toolbar.eraser_point": "Point Eraser",
        "toolbar.eraser_stroke": "Stroke Eraser",
        "dialog.save_strokes_title": "Tip",
        "dialog.save_strokes_text": "Keep current strokes?",
        "dialog.save_strokes_yes": "Keep",
        "dialog.save_strokes_no": "Don't Keep",
        "dialog.cancel": "Cancel",
    },
    "ja-JP": {
        "watermark.1": "開発中のバージョン",
        "watermark.2": "テクニカルプレビュー",
        "watermark.3": "Release Preview",
        "watermark.4": "再評価バージョン",
        "overlay.dev_watermark": "{type}\n品質は保証されません ({version})",
        "toolbar.theme_colors": "テーマの色",
        "toolbar.standard_colors": "標準の色",
        "toolbar.pen_size": "ペンの太さ",
        "toolbar.eraser_size": "消しゴムの太さ",
        "toolbar.eraser_point": "部分消しゴム",
        "toolbar.eraser_stroke": "ストローク消しゴム",
        "dialog.save_strokes_title": "ヒント",
        "dialog.save_strokes_text": "今回の筆跡を保存しますか？",
        "dialog.save_strokes_yes": "保存する",
        "dialog.save_strokes_no": "保存しない",
        "dialog.cancel": "キャンセル",
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
        if isinstance(page, dict) and isinstance(page.get("strokes"), list) and page["strokes"]:
            return True
    return False

class BoardBackend(QObject):
    windowStateChanged = Signal()

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
        if self.isMaximized:
            self._window.showNormal()
        else:
            self._window.showMaximized()
        self.windowStateChanged.emit()

    @Slot()
    def toggleFullscreen(self):
        self._window.toggle_fullscreen()
        self.windowStateChanged.emit()

    @Slot(int)
    def startResize(self, edge):
        # edge: 1=Top, 2=Bottom, 4=Left, 8=Right
        # Combined: 5=TopLeft, 6=BottomLeft, 9=TopRight, 10=BottomRight
        self._window.startSystemResize(Qt.Edge(edge))

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
        self.setRenderTarget(QQuickPaintedItem.Image)
        self.setPerformanceHint(QQuickPaintedItem.FastFBOResizing)
        self.setOpaquePainting(False)
        self._buffer = None
        self._allLines = []
        self._pendingLines = []
        self._background_color = QColor("#202020")
        self._min_segment_px = 1.5
        self._dirty_full = True

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
            "x1": x1, "y1": y1, "x2": x2, "y2": y2,
            "width": width, "color": colorHex,
            "isEraser": isEraser, "eraserPx": eraserPx,
            "strokeId": strokeId
        }
        self._allLines.append(line)
        if self._buffer is not None:
            self._pendingLines.append(line)
        else:
            self._dirty_full = True
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
            buf_painter.setRenderHint(QPainter.Antialiasing)
            for line in self._pendingLines:
                self._drawLine(buf_painter, line)
            buf_painter.end()
            self._pendingLines.clear()

        painter.drawImage(0, 0, self._buffer)

    def _drawLine(self, painter, line):
        isEraser = line.get("isEraser", False)
        width = float(line.get("width", 3))
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
            pen.setWidthF(max(0.5, width))
            pen.setCapStyle(Qt.RoundCap)
            pen.setJoinStyle(Qt.RoundJoin)
            painter.setPen(pen)

        w = self.width()
        h = self.height()
        x1 = float(line.get("x1", 0.0)) * w
        y1 = float(line.get("y1", 0.0)) * h
        x2 = float(line.get("x2", 0.0)) * w
        y2 = float(line.get("y2", 0.0)) * h

        dx = x2 - x1
        dy = y2 - y1
        if (dx * dx + dy * dy) < (self._min_segment_px * self._min_segment_px):
            painter.drawPoint(QPointF(x1, y1))
            return
        painter.drawLine(QPointF(x1, y1), QPointF(x2, y2))

# Register the native board item as a QML type
qmlRegisterType(NativeBoardItem, "KazuhaBoard", 1, 0, "NativeBoardItem")

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
        self._owner_window = owner_window
        self._result = self.ResultCancel
        self._loop = None
        self._closing = False

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
        context.setContextProperty("dialogTextSecondary", palette.get("textSecondary", palette["dialogText"]))
        context.setContextProperty("dialogAccent", palette["accent"])
        context.setContextProperty("dialogDivider", palette["dialogBorder"])
        context.setContextProperty("dialogItemHover", palette["buttonHover"])
        context.setContextProperty("dialogButtonActive", palette["buttonActive"])
        context.setContextProperty("dialogCardShadow", palette["cardShadow"])
        context.setContextProperty("dialogDarkMode", palette["darkMode"])
        font_family = _resolve_dialog_font_family()
        if font_family:
            context.setContextProperty("dialogFontFamily", font_family)

        qml_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "SaveStrokesDialog.qml")
        self.setSource(QUrl.fromLocalFile(qml_path))
        root = self.rootObject()
        width = int(root.property("implicitWidth")) if root and root.property("implicitWidth") else 452
        height = int(root.property("implicitHeight")) if root and root.property("implicitHeight") else 214
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
            if self._loop is not None and self._loop.isRunning():
                self._loop.quit()

    def closeEvent(self, event):
        if not self._closing:
            self._result = self.ResultCancel
            self._closing = True
            if self._loop is not None and self._loop.isRunning():
                self._loop.quit()
        super().closeEvent(event)

    @classmethod
    def ask(cls, owner_window, title, text, save_text, discard_text, cancel_text):
        dialog = cls(owner_window, title, text, save_text, discard_text, cancel_text)
        dialog.show()
        dialog._loop = QEventLoop()
        dialog._loop.exec()
        return dialog._result

class BoardWindow(QQuickView):
    def __init__(self):
        super().__init__()
        self._is_closing = False
        self._animation = None
        
        # Ensure the native board item is registered specifically for this window's engine
        qmlRegisterType(NativeBoardItem, "KazuhaBoard", 1, 0, "NativeBoardItem")
        
        self.setTitle("小黑板 - Luminalium")
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
        self._restore_maximized_after_fullscreen = False
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
        self.rootContext().setContextProperty("undoText", "撤销")
        self.rootContext().setContextProperty("redoText", "重做")
        self.rootContext().setContextProperty("themeColorsText", _t("toolbar.theme_colors"))
        self.rootContext().setContextProperty("standardColorsText", _t("toolbar.standard_colors"))
        self.rootContext().setContextProperty("eraserPointText", _t("toolbar.eraser_point"))
        self.rootContext().setContextProperty("eraserStrokeText", _t("toolbar.eraser_stroke"))
        self.rootContext().setContextProperty("penSizeText", _t("toolbar.pen_size"))
        self.rootContext().setContextProperty("eraserSizeText", _t("toolbar.eraser_size"))
        
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

        # Slide-in animation setup
        self._setup_slide_animation()

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
            if os.path.exists(self.strokes_path):
                try:
                    with open(self.strokes_path, "r", encoding="utf-8") as f:
                        document = _normalize_board_document(json.load(f))
                    root = self.rootObject()
                    if root and hasattr(root, "setBoardDocument"):
                        root.setBoardDocument(document)
                except Exception as e:
                    print(f"Failed to load strokes: {e}")

    def _on_show_tool_text_changed(self, value):
        self.rootContext().setContextProperty("showToolText", value)

    def toggle_fullscreen(self):
        if self.windowState() & Qt.WindowFullScreen:
            if self._restore_maximized_after_fullscreen:
                self.showMaximized()
            else:
                self.showNormal()
            return

        self._restore_maximized_after_fullscreen = bool(self.windowState() & Qt.WindowMaximized)
        self.showFullScreen()

    def _on_state_changed(self, state):
        if not (state & Qt.WindowFullScreen):
            self._restore_maximized_after_fullscreen = bool(state & Qt.WindowMaximized)
        self.backend.windowStateChanged.emit()
        self._last_state = state

    def _setup_slide_animation(self):
        self._animation = QPropertyAnimation(self, b"y")
        self._animation.setDuration(450)
        # Use OutQuint for a more distinct non-linear feel
        self._animation.setEasingCurve(QEasingCurve.OutQuint)

    def showEvent(self, event):
        super().showEvent(event)
        # Reset closing flags for reuse
        self._is_closing = False
        self._force_close = False
        
        if self._animation and self._animation.state() != QPropertyAnimation.Running:
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
            self._animation.setStartValue(start_y)
            self._animation.setEndValue(target_y)
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
            root = self.rootObject()
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
            return

        if self._animation:
            self._is_closing = True
            geom = self.geometry()
            self._animation.setStartValue(geom.y())
            self._animation.setEndValue(geom.y() - geom.height())
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

    def _on_animation_finished(self):
        if self._is_closing:
            self._force_close = True
            super().close()
            # Reset for next time if the object is reused
            self._is_closing = False

    def closeEvent(self, event):
        if getattr(self, "_force_close", False):
            super().closeEvent(event)
            return

        # Intercept manual close (X button) to play animation
        event.ignore()
        self._trigger_close_animation()
