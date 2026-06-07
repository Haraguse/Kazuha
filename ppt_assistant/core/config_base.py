import json
import os
from enum import IntEnum
from PySide6.QtCore import QObject, Signal
from PySide6.QtGui import QIcon, QPixmap, QPainter, QColor, QPen
from PySide6.QtWidgets import QApplication, QStyle


class Theme(IntEnum):
    LIGHT = 0
    DARK = 1
    AUTO = 2


class BoolValidator:
    @staticmethod
    def validate(value):
        return bool(value)

    @staticmethod
    def correct(value):
        return bool(value)


class RangeValidator:
    def __init__(self, min_val, max_val):
        self.min = min_val
        self.max = max_val

    def validate(self, value):
        return self.min <= value <= self.max

    def correct(self, value):
        try:
            value = type(self.min)(value)
        except (ValueError, TypeError):
            return self.min
        return max(self.min, min(self.max, value))


class OptionsValidator:
    def __init__(self, options):
        self.options = list(options)

    def validate(self, value):
        return value in self.options

    def correct(self, value):
        if value in self.options:
            return value
        return self.options[0] if self.options else None


class EnumSerializer:
    def __init__(self, enum_cls):
        self.enum_cls = enum_cls

    def serialize(self, value):
        if isinstance(value, self.enum_cls):
            return value.value
        return value

    def deserialize(self, value):
        try:
            return self.enum_cls(value)
        except (ValueError, TypeError):
            return list(self.enum_cls)[0]


class _ConfigSignal(QObject):
    changed = Signal(object)


class ConfigItem:
    def __init__(self, group, name, default, validator=None, serializer=None, restart=False):
        self.group = group
        self.name = name
        self._default = default
        self.validator = validator
        self.serializer = serializer
        self.restart = restart
        self._value = default
        self._signal = _ConfigSignal()

    @property
    def value(self):
        if self.serializer is not None:
            return self.serializer.deserialize(self._value)
        if self.validator is not None:
            return self.validator.correct(self._value)
        return self._value

    @value.setter
    def value(self, v):
        if self.serializer is not None:
            raw = self.serializer.serialize(v)
        elif self.validator is not None:
            raw = self.validator.correct(v)
        else:
            raw = v

        if self._value != raw:
            self._value = raw
            self._signal.changed.emit(self.value)

    @property
    def valueChanged(self):
        return self._signal.changed

    def _set_raw(self, raw):
        self._value = raw

    def _get_raw(self):
        return self._value


class OptionsConfigItem(ConfigItem):
    pass


class RangeConfigItem(ConfigItem):
    pass


class _FluentIcon:
    def __init__(self, name, fallback_char, fallback_style):
        self._name = name
        self._fallback_char = fallback_char
        self._fallback_style = fallback_style
        self._icon_cache = {}

    def icon(self, color=None, theme=None):
        cache_key = (str(color), str(theme))
        if cache_key in self._icon_cache:
            return self._icon_cache[cache_key]

        try:
            from RinUI import IconManager
            rin_icon_name = {
                "ZOOM_IN": "ZoomIn",
                "BRIGHTNESS": "Brightness",
                "SAVE": "Save",
                "CLOSE": "Close",
            }.get(self._name, self._name)
            icon = IconManager().get_icon(rin_icon_name)
            if icon and not icon.isNull():
                self._icon_cache[cache_key] = icon
                return icon
        except Exception:
            pass

        pixmap = QPixmap(20, 20)
        pixmap.fill(QColor(0, 0, 0, 0))
        painter = QPainter(pixmap)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)
        if color is not None:
            painter.setPen(QPen(QColor(color) if isinstance(color, str) else color))
        else:
            style = QApplication.style()
            if style:
                std_icon = style.standardIcon(getattr(QStyle, self._fallback_style, QStyle.StandardPixmap.SP_CustomBase))
                if not std_icon.isNull():
                    painter.end()
                    self._icon_cache[cache_key] = std_icon
                    return std_icon
            painter.setPen(QPen(QColor("#FFFFFF")))

        font = painter.font()
        font.setPixelSize(14)
        painter.setFont(font)
        painter.drawText(pixmap.rect(), 0x0084, self._fallback_char)
        painter.end()

        icon = QIcon(pixmap)
        self._icon_cache[cache_key] = icon
        return icon


class FluentIcon:
    ZOOM_IN = _FluentIcon("ZOOM_IN", "⊕", "SP_CustomBase")
    BRIGHTNESS = _FluentIcon("BRIGHTNESS", "☀", "SP_CustomBase")
    CLOSE = _FluentIcon("CLOSE", "✕", "SP_TitleBarCloseButton")
    SAVE = _FluentIcon("SAVE", "↓", "SP_DialogSaveButton")


class QConfig(QObject):
    configChanged = Signal(object)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._items = {}
        self._file_path = None
        self._collect_items()

    def _collect_items(self):
        for attr_name in dir(self):
            attr = getattr(self, attr_name)
            if isinstance(attr, ConfigItem):
                self._items[attr_name] = attr

    def save(self):
        if not self._file_path:
            return
        data = {}
        if os.path.exists(self._file_path):
            try:
                with open(self._file_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
            except Exception:
                data = {}

        if not isinstance(data, dict):
            data = {}

        for attr_name, item in self._items.items():
            group = item.group
            if group not in data:
                data[group] = {}
            raw = item._get_raw()
            if item.serializer is not None:
                raw = item.serializer.serialize(raw) if hasattr(item.serializer, 'serialize') else raw
            data[group][item.name] = raw

        try:
            with open(self._file_path, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=4, ensure_ascii=False)
        except Exception:
            pass

    def load(self, file_path=None):
        if file_path is not None:
            self._file_path = file_path
        if not self._file_path or not os.path.exists(self._file_path):
            return

        try:
            with open(self._file_path, "r", encoding="utf-8") as f:
                data = json.load(f)
        except Exception:
            return

        if not isinstance(data, dict):
            return

        for attr_name, item in self._items.items():
            group_data = data.get(item.group)
            if isinstance(group_data, dict) and item.name in group_data:
                raw = group_data[item.name]
                if item.serializer is not None and hasattr(item.serializer, 'deserialize'):
                    raw = item.serializer.serialize(
                        item.serializer.deserialize(raw)
                    )
                item._set_raw(raw)

    @property
    def theme(self):
        theme_item = self._items.get("themeMode")
        if theme_item is not None:
            raw = theme_item._get_raw()
            try:
                return Theme(raw)
            except (ValueError, TypeError):
                return Theme.LIGHT
        return Theme.LIGHT

    @theme.setter
    def theme(self, value):
        theme_item = self._items.get("themeMode")
        if theme_item is not None:
            if isinstance(value, Theme):
                theme_item._set_raw(value.value)
            elif isinstance(value, int):
                theme_item._set_raw(value)
            else:
                try:
                    theme_item._set_raw(Theme(value).value)
                except (ValueError, TypeError):
                    theme_item._set_raw(Theme.LIGHT.value)
            theme_item._signal.changed.emit(theme_item.value)


qconfig = QConfig()


def setThemeColor(color: str):
    try:
        from RinUI import ThemeManager
        ThemeManager().set_theme_color(color)
    except Exception:
        pass


def isDarkTheme() -> bool:
    try:
        from RinUI import ThemeManager
        return ThemeManager().is_dark_theme()
    except Exception:
        return False