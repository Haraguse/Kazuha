from PySide6.QtWidgets import QSystemTrayIcon
from PySide6.QtGui import QIcon, QPainter, QPixmap, QCursor
from PySide6.QtCore import Signal, QObject, Qt
from PySide6.QtSvg import QSvgRenderer
import os
from qfluentwidgets import RoundMenu, Action, themeColor, FluentIcon as FIF
from ppt_assistant.core.i18n import t
from ppt_assistant.core.config import cfg

ICON_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "icons")

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
        self._update_icon()
        self.menu = RoundMenu(parent=parent)
        self._init_menu()
        
        self.tray_icon.setContextMenu(self.menu)
        self.tray_icon.activated.connect(self._on_activated)
        
        self.tray_icon.show()

    def _init_menu(self):
        self.menu.clear()
        
        self.tray_icon.setToolTip(t("tray.tooltip"))
        
        self.act_header = Action(QIcon(os.path.join(ICON_DIR, "logo.svg")), t("tray.title"), self.menu)
        self.menu.addAction(self.act_header)
        
        self.menu.addSeparator()
        
        self.act_settings = Action(FIF.SETTING, t("tray.settings"), self.menu)
        self.act_settings.triggered.connect(self.show_settings.emit)
        self.menu.addAction(self.act_settings)
        
        self.act_board = Action(QIcon(os.path.join(ICON_DIR, "board-in-board.svg")), t("tray.board"), self.menu)
        self.act_board.triggered.connect(self.show_board.emit)
        self.menu.addAction(self.act_board)

        self.act_timer = Action(QIcon(os.path.join(ICON_DIR, "timer.svg")), t("tray.timer"), self.menu)
        self.act_timer.triggered.connect(self.show_timer.emit)
        self.menu.addAction(self.act_timer)
        
        if cfg.compatibilityMode.value:
            self.act_toggle = Action(FIF.APPLICATION, t("tray.toggle"), self.menu)
            self.act_toggle.triggered.connect(self.toggle_overlay.emit)
            self.menu.addAction(self.act_toggle)
        
        self.menu.addSeparator()
        
        self.act_restart = Action(FIF.SYNC, t("tray.restart"), self.menu)
        self.act_restart.triggered.connect(self.restart_app.emit)
        self.menu.addAction(self.act_restart)
        
        self.act_exit = Action(FIF.POWER_BUTTON, t("tray.exit"), self.menu)
        self.act_exit.triggered.connect(self.exit_app.emit)
        self.menu.addAction(self.act_exit)

    def refresh_menu(self):
        self._init_menu()

    def _on_activated(self, reason):
        if reason == QSystemTrayIcon.Trigger:
            self.menu.exec(QCursor.pos())

    def _update_icon(self):
        import sys
        logo_path = os.path.join(ICON_DIR, "logo.svg")
        if not os.path.exists(logo_path):
             logo_path = os.path.join(ICON_DIR, "Pen.svg")
        if sys.platform == "win32":
            if os.path.exists(logo_path):
                self.tray_icon.setIcon(QIcon(logo_path))
            return
        
        # On Linux, try to use a simple approach first if things are flaky
        if sys.platform == "linux":
            # Just try setting the icon directly first
            if os.path.exists(logo_path):
                 self.tray_icon.setIcon(QIcon(logo_path))
                 # If we want to tint it, we can continue, but often direct icon is safer
                 # Return to skip complex tinting if we want simple stability
                 # return 

        try:
            color = themeColor()
            
            pixmap = QPixmap(64, 64)
            pixmap.fill(Qt.transparent)
            
            painter = QPainter(pixmap)
            painter.setRenderHint(QPainter.Antialiasing)
            
            renderer = QSvgRenderer(logo_path)
            if not renderer.isValid():
                # Fallback to direct icon load
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
