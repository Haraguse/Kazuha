import sys
import os
import winreg
import win32com.client
import win32gui
from PyQt6.QtWidgets import QApplication, QWidget, QMenu, QSystemTrayIcon
from PyQt6.QtCore import QTimer, Qt
from PyQt6.QtGui import QAction
from qfluentwidgets import setTheme, Theme

from widgets import ToolBarWidget, PageNavWidget, SpotlightOverlay, IconFactory
from utils import play_click_sound


class MainController(QWidget):
    def __init__(self):
        super().__init__()
        setTheme(Theme.DARK)
        self.setWindowFlags(Qt.WindowType.Tool | Qt.WindowType.FramelessWindowHint)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setFixedSize(1, 1)
        self.move(-100, -100)
        self.ppt_app = None
        self.current_view = None
        self.toolbar = ToolBarWidget()
        self.nav_left = PageNavWidget(self, is_right=False)
        self.nav_right = PageNavWidget(self, is_right=True)
        self.spotlight = SpotlightOverlay()
        self.setup_connections()
        self.setup_tray()
        self.timer = QTimer(self)
        self.timer.timeout.connect(self.check_state)
        self.timer.start(500)
        self.widgets_visible = False
        self.nav_anchor = "bottom"

    def setup_connections(self):
        self.toolbar.btn_arrow.clicked.connect(lambda: self.set_pointer(1))
        self.toolbar.btn_pen.clicked.connect(lambda: self.set_pointer(2))
        self.toolbar.btn_eraser.clicked.connect(lambda: self.set_pointer(5))
        self.toolbar.request_exit.connect(self.exit_slideshow)
        self.toolbar.request_spotlight.connect(self.toggle_spotlight)
        self.toolbar.request_pen_color.connect(self.set_pen_color)
        self.toolbar.request_clear_ink.connect(self.clear_ink)
        for nav in [self.nav_left, self.nav_right]:
            nav.btn_prev.clicked.connect(self.go_prev)
            nav.btn_next.clicked.connect(self.go_next)
            nav.request_slide_jump.connect(self.jump_to_slide)

    def has_ink(self):
        try:
            view = self.get_ppt_view()
            if not view:
                return False
            slide = view.Slide
            if slide.Shapes.Count == 0:
                return False
            for shape in slide.Shapes:
                if shape.Type == 22:
                    return True
            return False
        except:
            return True

    def show_warning(self, target, message):
        title = "PPT助手提示"
        self.tray_icon.showMessage(title, message, QSystemTrayIcon.MessageIcon.Warning, 2000)

    def setup_tray(self):
        self.tray_icon = QSystemTrayIcon(self)
        self.tray_icon.setIcon(IconFactory.draw_arrow("#00cc7a", 'right'))
        self.tray_icon.setToolTip("PPT演示助手")
        menu = QMenu()
        self.autorun_action = QAction("开机自启动", self)
        self.autorun_action.setCheckable(True)
        self.autorun_action.setChecked(self.is_autorun())
        self.autorun_action.triggered.connect(self.toggle_autorun)
        self.autorun_action.triggered.connect(lambda checked: play_click_sound())
        menu.addAction(self.autorun_action)
        self.quit_action = QAction("退出", self)
        self.quit_action.triggered.connect(QApplication.instance().quit)
        self.quit_action.triggered.connect(lambda checked: play_click_sound())
        menu.addAction(self.quit_action)
        self.tray_icon.setContextMenu(menu)
        self.tray_icon.show()

    def is_autorun(self):
        try:
            key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Run", 0, winreg.KEY_READ)
            winreg.QueryValueEx(key, "SeiraiPPTAssistant")
            winreg.CloseKey(key)
            return True
        except WindowsError:
            return False

    def toggle_autorun(self, checked):
        app_path = os.path.abspath(sys.argv[0])
        if app_path.endswith('.py'):
            python_exe = sys.executable.replace("python.exe", "pythonw.exe")
            if not os.path.exists(python_exe):
                python_exe = sys.executable
            cmd = f'"{python_exe}" "{app_path}"'
        else:
            cmd = f'"{sys.executable}"'
        key_path = r"Software\Microsoft\Windows\CurrentVersion\Run"
        try:
            key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_ALL_ACCESS)
            if checked:
                winreg.SetValueEx(key, "SeiraiPPTAssistant", 0, winreg.REG_SZ, cmd)
            else:
                try:
                    winreg.DeleteValue(key, "SeiraiPPTAssistant")
                except WindowsError:
                    pass
            winreg.CloseKey(key)
        except Exception as e:
            print(f"Error setting autorun: {e}")

    def get_ppt_view(self):
        try:
            self.ppt_app = win32com.client.GetActiveObject("PowerPoint.Application")
            if self.ppt_app.SlideShowWindows.Count > 0:
                return self.ppt_app.SlideShowWindows(1).View
            else:
                return None
        except Exception:
            return None

    def check_state(self):
        view = self.get_ppt_view()
        if view:
            if not self.widgets_visible:
                self.show_widgets()
            if self.ppt_app:
                self.nav_left.ppt_app = self.ppt_app
                self.nav_right.ppt_app = self.ppt_app
            self.sync_state(view)
            self.update_page_num(view)
        else:
            if self.widgets_visible:
                self.hide_widgets()

    def show_widgets(self):
        self.toolbar.show()
        self.nav_left.show()
        self.nav_right.show()
        self.adjust_positions()
        self.widgets_visible = True

    def hide_widgets(self):
        self.toolbar.hide()
        self.nav_left.hide()
        self.nav_right.hide()
        self.widgets_visible = False

    def adjust_positions(self):
        screen = QApplication.primaryScreen().geometry()
        MARGIN = 20
        tb_w = self.toolbar.sizeHint().width()
        tb_h = self.toolbar.sizeHint().height()
        self.toolbar.setGeometry(
            (screen.width() - tb_w) // 2,
            screen.height() - tb_h - MARGIN,
            tb_w, tb_h
        )
        nav_w = self.nav_left.sizeHint().width()
        nav_h = self.nav_left.sizeHint().height()
        if self.nav_anchor == "top":
            y = MARGIN
        elif self.nav_anchor == "middle":
            y = max(MARGIN, (screen.height() - nav_h) // 2)
        else:
            y = screen.height() - nav_h - MARGIN
        self.nav_left.setGeometry(MARGIN, y, nav_w, nav_h)
        self.nav_right.setGeometry(screen.width() - nav_w - MARGIN, y, nav_w, nav_h)

    def sync_nav_position(self, anchor, target_y):
        screen = QApplication.primaryScreen().geometry()
        MARGIN = 20
        self.nav_anchor = anchor
        try:
            self.nav_left.anchor = anchor
            self.nav_right.anchor = anchor
            self.nav_left.update_orientation()
            self.nav_right.update_orientation()
            # Force size recalculation after orientation change
            self.nav_left.adjustSize()
            self.nav_right.adjustSize()
        except Exception:
            pass
        nav_w = self.nav_left.sizeHint().width()
        nav_h = self.nav_left.sizeHint().height()
        if anchor == "top":
            y = MARGIN
        elif anchor == "middle":
            y = max(MARGIN, (screen.height() - nav_h) // 2)
        else:
            y = screen.height() - nav_h - MARGIN
        self.nav_left.setGeometry(MARGIN, y, nav_w, nav_h)
        self.nav_right.setGeometry(screen.width() - nav_w - MARGIN, y, nav_w, nav_h)

    def sync_state(self, view):
        try:
            pt = view.PointerType
            if pt == 1:
                self.toolbar.btn_arrow.setChecked(True)
            elif pt == 2:
                self.toolbar.btn_pen.setChecked(True)
            elif pt == 5:
                self.toolbar.btn_eraser.setChecked(True)
        except:
            pass

    def update_page_num(self, view):
        try:
            current = view.Slide.SlideIndex
            total = self.ppt_app.ActivePresentation.Slides.Count
            self.nav_left.update_page(current, total)
            self.nav_right.update_page(current, total)
        except:
            pass

    def go_prev(self):
        view = self.get_ppt_view()
        if view:
            try:
                view.Previous()
            except:
                pass

    def go_next(self):
        view = self.get_ppt_view()
        if view:
            try:
                view.Next()
            except:
                pass

    def jump_to_slide(self, index):
        view = self.get_ppt_view()
        if view:
            try:
                view.GotoSlide(index)
            except:
                pass

    def set_pointer(self, type_id):
        view = self.get_ppt_view()
        if view:
            try:
                if type_id == 5:
                    if not self.has_ink():
                        self.show_warning(None, "当前页没有笔迹")
                view.PointerType = type_id
                self.activate_ppt_window()
            except:
                pass

    def set_pen_color(self, color):
        view = self.get_ppt_view()
        if view:
            try:
                view.PointerType = 2
                view.PointerColor.RGB = color
                self.activate_ppt_window()
            except:
                pass

    def activate_ppt_window(self):
        try:
            hwnd = self.ppt_app.SlideShowWindows(1).HWND
            win32gui.SetForegroundWindow(hwnd)
        except:
            pass

    def clear_ink(self):
        view = self.get_ppt_view()
        if view:
            try:
                if not self.has_ink():
                    self.show_warning(None, "当前页没有笔迹")
                view.EraseDrawing()
            except:
                pass

    def toggle_spotlight(self):
        if self.spotlight.isVisible():
            self.spotlight.hide()
        else:
            self.spotlight.showFullScreen()

    def exit_slideshow(self):
        view = self.get_ppt_view()
        if view:
            try:
                view.Exit()
            except:
                pass
