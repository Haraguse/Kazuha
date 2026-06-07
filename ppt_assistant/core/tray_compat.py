from PySide6.QtWidgets import (
    QLabel,
    QPushButton,
    QWidget,
    QMenu,
    QDialog,
    QVBoxLayout,
    QHBoxLayout,
    QStackedWidget,
    QApplication,
    QProgressBar,
)
from PySide6.QtCore import Qt, Signal, QPoint
from PySide6.QtGui import QAction as QtAction, QIcon, QFont
from ppt_assistant.core.config_base import isDarkTheme, setThemeColor, FluentIcon, Theme


def themeColor():
    try:
        from RinUI import ThemeManager
        return ThemeManager().get_theme_color()
    except Exception:
        return "#3275F5"


class BodyLabel(QLabel):
    pass


class SubtitleLabel(QLabel):
    def __init__(self, text="", parent=None):
        super().__init__(text, parent)
        font = self.font()
        font.setPointSize(font.pointSize() + 2)
        font.setBold(True)
        self.setFont(font)


class PushButton(QPushButton):
    pass


class PrimaryPushButton(QPushButton):
    def __init__(self, text="", parent=None):
        super().__init__(text, parent)
        is_dark = isDarkTheme()
        theme = themeColor()
        self.setStyleSheet(
            f"QPushButton {{ background-color: {theme}; color: white; border: none; "
            f"border-radius: 4px; padding: 6px 16px; font-weight: bold; }}"
            f"QPushButton:hover {{ opacity: 0.9; }}"
            f"QPushButton:pressed {{ opacity: 0.8; }}"
        )


class MenuAnimationType:
    FADE_IN_PULL_UP = 0
    DROP_DOWN = 1


class MenuActionListWidget(QWidget):
    pass


class Action(QtAction):
    def __init__(self, icon, text, parent=None):
        if isinstance(icon, type(FluentIcon.ZOOM_IN)):
            qicon = icon.icon()
        elif isinstance(icon, QIcon):
            qicon = icon
        else:
            qicon = QIcon()
        super().__init__(qicon, text, parent)


class RoundMenu(QMenu):
    def __init__(self, title="", parent=None):
        super().__init__(title, parent)
        self.view = self

    def exec(self, pos, ani=True, aniType=0):
        super().exec_(pos)

    def exec_(self, pos, ani=True, aniType=0):
        if isinstance(pos, QPoint):
            super().exec_(pos)
        else:
            super().exec_(pos)


class FlyoutViewBase(QWidget):
    confirmed = Signal()
    cancelled = Signal()


class Flyout:
    @staticmethod
    def make(view, anchor):
        return view


class Dialog(QDialog):
    def __init__(self, title, text, parent=None):
        super().__init__(parent)
        self.setWindowTitle(title)
        self.setMinimumWidth(320)

        layout = QVBoxLayout(self)
        layout.setContentsMargins(20, 18, 20, 18)
        layout.setSpacing(14)

        title_label = SubtitleLabel(title, self)
        layout.addWidget(title_label)

        body_label = BodyLabel(text, self)
        layout.addWidget(body_label)

        btn_layout = QHBoxLayout()
        btn_layout.setSpacing(12)
        btn_layout.addStretch(1)

        self.yesButton = PrimaryPushButton("Yes", self)
        self.yesButton.setMinimumWidth(100)
        btn_layout.addWidget(self.yesButton)

        self.cancelButton = PushButton("Cancel", self)
        self.cancelButton.setMinimumWidth(100)
        btn_layout.addWidget(self.cancelButton)

        layout.addLayout(btn_layout)


class DisplayLabel(QLabel):
    def __init__(self, text="", parent=None):
        super().__init__(text, parent)
        font = QFont()
        font.setPointSize(28)
        font.setBold(True)
        self.setFont(font)


class SegmentedWidget(QWidget):
    currentItemChanged = Signal(object)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._layout = QHBoxLayout(self)
        self._layout.setContentsMargins(0, 0, 0, 0)
        self._layout.setSpacing(2)
        self._items = {}
        self._current_key = None

    def addItem(self, routeKey, text, onClick=None):
        btn = QPushButton(text, self)
        btn.setCheckable(True)
        btn.setFixedHeight(32)
        btn.setMinimumWidth(80)
        btn.clicked.connect(lambda checked, k=routeKey: self._on_item_clicked(k))
        self._layout.addWidget(btn)
        self._items[routeKey] = btn

    def _on_item_clicked(self, key):
        for k, btn in self._items.items():
            btn.setChecked(k == key)
        self._current_key = key
        self.currentItemChanged.emit(key)

    def setCurrentItem(self, routeKey):
        if routeKey in self._items:
            self._items[routeKey].click()

    def setCurrentIndex(self, index):
        keys = list(self._items.keys())
        if 0 <= index < len(keys):
            self.setCurrentItem(keys[index])


class FluentWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.navigationInterface = QWidget(self)
        self.navigationInterface.hide()
        self.stackedWidget = QStackedWidget(self)

        main_layout = QVBoxLayout(self)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.addWidget(self.stackedWidget)

        app = QApplication.instance()
        if app:
            font = QFont(app.font())
            font.setHintingPreference(QFont.PreferNoHinting)
            font.setStyleStrategy(QFont.PreferAntialias)
            self.setFont(font)


class CardWidget(QWidget):
    pass


class ProgressBar(QProgressBar):
    pass


class IndeterminateProgressRing(QProgressBar):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setRange(0, 0)


def setTheme(theme_value):
    try:
        from RinUI import ThemeManager
        theme_map = {
            Theme.LIGHT: "light",
            Theme.DARK: "dark",
            Theme.AUTO: "auto",
        }
        ThemeManager().set_theme(theme_map.get(theme_value, "auto"))
    except Exception:
        pass