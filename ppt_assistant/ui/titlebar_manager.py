"""
自绘标题栏窗口管理器
支持拖动、最小化、最大化、关闭按钮和Windows 11 snap
"""

import ctypes
import sys
from PySide6.QtWidgets import QWidget
from PySide6.QtCore import Qt, QPoint, QRect, QSize, Signal, QTimer
from PySide6.QtGui import QScreen, QGuiApplication
from PySide6.QtWebEngineWidgets import QWebEngineView


class WindowsWindowManager:
    """Windows 窗口操作管理器"""

    # Windows API 常量
    WM_SYSCOMMAND = 0x0112
    SC_MINIMIZE = 0xF020
    SC_MAXIMIZE = 0xF030
    SC_RESTORE = 0xF120
    WM_NCLBUTTONDOWN = 0xA1
    HT_CAPTION = 0x2

    # Windows 11 Snap 常量
    WM_SIZING = 0x0214
    WMSZ_LEFT = 1
    WMSZ_RIGHT = 2
    WMSZ_TOP = 3
    WMSZ_TOPLEFT = 4
    WMSZ_TOPRIGHT = 5
    WMSZ_BOTTOM = 6
    WMSZ_BOTTOMLEFT = 7
    WMSZ_BOTTOMRIGHT = 8

    @staticmethod
    def minimize_window(hwnd):
        """最小化窗口"""
        if sys.platform == "win32":
            try:
                ctypes.windll.user32.SendMessageW(hwnd, WindowsWindowManager.WM_SYSCOMMAND,
                                                  WindowsWindowManager.SC_MINIMIZE, 0)
                return True
            except Exception:
                pass
        return False

    @staticmethod
    def maximize_window(hwnd):
        """最大化或还原窗口"""
        if sys.platform == "win32":
            try:
                # 先检查窗口是否已最大化
                is_zoomed = ctypes.windll.user32.IsZoomed(hwnd)
                if is_zoomed:
                    # 还原
                    ctypes.windll.user32.SendMessageW(hwnd, WindowsWindowManager.WM_SYSCOMMAND,
                                                      WindowsWindowManager.SC_RESTORE, 0)
                else:
                    # 最大化
                    ctypes.windll.user32.SendMessageW(hwnd, WindowsWindowManager.WM_SYSCOMMAND,
                                                      WindowsWindowManager.SC_MAXIMIZE, 0)
                return True
            except Exception:
                pass
        return False

    @staticmethod
    def close_window(hwnd):
        """关闭窗口"""
        if sys.platform == "win32":
            try:
                ctypes.windll.user32.SendMessageW(hwnd, 0x0010, 0, 0)  # WM_CLOSE
                return True
            except Exception:
                pass
        return False

    @staticmethod
    def start_window_drag(hwnd):
        """开始拖动窗口（会阻塞直到拖动结束）"""
        if sys.platform == "win32":
            try:
                # 模拟在标题栏上按下鼠标左键并拖动
                ctypes.windll.user32.ReleaseCapture()
                ctypes.windll.user32.SendMessageW(hwnd, WindowsWindowManager.WM_NCLBUTTONDOWN,
                                                  WindowsWindowManager.HT_CAPTION, 0)
                return True
            except Exception:
                pass
        return False

    @staticmethod
    def get_window_state(hwnd):
        """获取窗口状态：normal, maximized, minimized"""
        if sys.platform == "win32":
            try:
                if ctypes.windll.user32.IsZoomed(hwnd):
                    return "maximized"
                elif ctypes.windll.user32.IsIconic(hwnd):
                    return "minimized"
                else:
                    return "normal"
            except Exception:
                pass
        return "normal"

    @staticmethod
    def is_window_maximized(hwnd):
        """检查窗口是否已最大化"""
        if sys.platform == "win32":
            try:
                return bool(ctypes.windll.user32.IsZoomed(hwnd))
            except Exception:
                pass
        return False


class FramelessWindowWithTitlebar(QWidget):
    """无框架窗口，带自绘标题栏"""

    # 信号
    titlebar_height_changed = Signal(int)

    def __init__(self, parent=None, titlebar_height=32):
        super().__init__(parent)

        self.titlebar_height = titlebar_height
        self._titlebar_dragging = False
        self._drag_start_pos = QPoint()
        self._drag_start_geometry = QRect()
        self._hwnd = int(self.winId()) if sys.platform == "win32" else None
        self._is_maximized = False

        # 设置窗口属性
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.Window)
        self.setAttribute(Qt.WA_StyledBackground, True)

    def get_hwnd(self):
        """获取Windows窗口句柄"""
        if sys.platform == "win32":
            try:
                return int(self.winId())
            except Exception:
                pass
        return None

    def set_titlebar_height(self, height):
        """设置标题栏高度"""
        self.titlebar_height = height
        self.titlebar_height_changed.emit(height)
        self.update_content_margin()

    def update_content_margin(self):
        """更新内容区域边距（为标题栏留出空间）"""
        # 子类应该实现此方法来调整内容区域
        pass

    def minimize(self):
        """最小化"""
        if self._hwnd:
            return WindowsWindowManager.minimize_window(self._hwnd)
        else:
            self.showMinimized()
            return True

    def maximize(self):
        """切换最大化/还原"""
        if self._hwnd:
            was_maximized = WindowsWindowManager.is_window_maximized(self._hwnd)
            result = WindowsWindowManager.maximize_window(self._hwnd)
            if result:
                self._is_maximized = not was_maximized
            return result
        else:
            if self.isMaximized():
                self.showNormal()
            else:
                self.showMaximized()
            self._is_maximized = not self._is_maximized
            return True

    def close_window(self):
        """关闭窗口"""
        if self._hwnd:
            return WindowsWindowManager.close_window(self._hwnd)
        else:
            self.close()
            return True

    def start_drag(self):
        """开始拖动窗口"""
        if self._hwnd:
            # 异步执行拖动，以避免阻塞UI
            QTimer.singleShot(10, lambda: WindowsWindowManager.start_window_drag(self._hwnd))
        else:
            # Fallback: 手动处理拖动
            self._titlebar_dragging = True
            self._drag_start_pos = QPoint(self.mapFromGlobal(QGuiApplication.primaryScreen().cursor().pos())) if QGuiApplication.primaryScreen() else QPoint()
            self._drag_start_geometry = self.geometry()

    def Mouse移动_titlebar(self, pos):
        """处理标题栏鼠标移动（用于手动拖动）"""
        if self._titlebar_dragging and not self._hwnd:
            global_pos = self.mapToGlobal(pos)
            delta = global_pos - QGuiApplication.screenAt(global_pos).geometry().topLeft() if QGuiApplication.screenAt(global_pos) else QPoint()
            # 计算新位置
            delta_from_drag = pos - self._drag_start_pos
            new_pos = self._drag_start_geometry.topLeft() + delta_from_drag
            self.move(new_pos)

    def end_drag_titlebar(self):
        """结束标题栏拖动"""
        self._titlebar_dragging = False

    def get_window_state(self):
        """获取窗口状态"""
        if self._hwnd:
            return WindowsWindowManager.get_window_state(self._hwnd)
        elif self.isMaximized():
            return "maximized"
        elif self.isMinimized():
            return "minimized"
        else:
            return "normal"

    def get_window_title(self):
        """获取窗口标题"""
        return self.windowTitle()

    def set_window_title(self, title):
        """设置窗口标题"""
        self.setWindowTitle(title)


class TitlebarBridge:
    """HTML/JS 与窗口管理器的通信桥接"""

    def __init__(self, window: FramelessWindowWithTitlebar):
        self.window = window

    def minimize(self):
        """最小化窗口"""
        return self.window.minimize()

    def maximize(self):
        """最大化/还原窗口"""
        return self.window.maximize()

    def close(self):
        """关闭窗口"""
        return self.window.close_window()

    def start_drag(self):
        """开始拖动"""
        return self.window.start_drag()

    def get_window_state(self):
        """获取窗口状态"""
        return self.window.get_window_state()

    def get_window_title(self):
        """获取窗口标题"""
        return self.window.get_window_title()

    def set_window_title(self, title):
        """设置窗口标题"""
        self.window.set_window_title(title)
        return True
