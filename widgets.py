import os
import hashlib
import time
from PyQt6.QtWidgets import (QWidget, QHBoxLayout, QVBoxLayout, QButtonGroup,
                             QLabel, QFrame, QScrollArea, QGridLayout,
                             QPushButton, QBoxLayout)
from PyQt6.QtCore import Qt, QSize, QPoint, QPointF, pyqtSignal, QEvent, QRect
from PyQt6.QtGui import (QIcon, QPainter, QColor, QPixmap, QPen, QBrush,
                         QPolygon, QPolygonF, QMouseEvent)
from qfluentwidgets import (PushButton as FluentPushButton, TransparentToolButton,
                            ToolButton, ToolTipFilter, ToolTipPosition,
                            Flyout, FlyoutView, FlyoutAnimationType, MessageBox)
from qfluentwidgets.components.material import AcrylicFlyout
from utils import icon_path, play_click_sound


class IconFactory:
    @staticmethod
    def draw_cursor(color):
        pixmap = QPixmap(32, 32)
        pixmap.fill(Qt.GlobalColor.transparent)
        painter = QPainter(pixmap)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)
        path = QPolygonF([
            QPointF(10, 6),
            QPointF(10, 26),
            QPointF(15, 21),
            QPointF(22, 28),
            QPointF(24, 26),
            QPointF(17, 19),
            QPointF(24, 19)
        ])
        painter.setPen(QPen(QColor("white"), 1.5, Qt.PenStyle.SolidLine, Qt.PenCapStyle.RoundCap, Qt.PenJoinStyle.RoundJoin))
        painter.setBrush(QBrush(QColor(color)))
        painter.drawPolygon(path)
        painter.end()
        return QIcon(pixmap)

    @staticmethod
    def draw_arrow(color, direction='left'):
        pixmap = QPixmap(32, 32)
        pixmap.fill(Qt.GlobalColor.transparent)
        painter = QPainter(pixmap)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)
        pen = QPen(QColor(color))
        pen.setWidth(3)
        pen.setCapStyle(Qt.PenCapStyle.RoundCap)
        pen.setJoinStyle(Qt.PenJoinStyle.RoundJoin)
        painter.setPen(pen)
        if direction == 'left':
            points = [QPoint(20, 8), QPoint(12, 16), QPoint(20, 24)]
            painter.drawPolyline(points)
        elif direction == 'right':
            points = [QPoint(12, 8), QPoint(20, 16), QPoint(12, 24)]
            painter.drawPolyline(points)
        elif direction == 'up':
            points = [QPoint(8, 20), QPoint(16, 12), QPoint(24, 20)]
            painter.drawPolyline(points)
        elif direction == 'down':
            points = [QPoint(8, 12), QPoint(16, 20), QPoint(24, 12)]
            painter.drawPolyline(points)
        painter.end()
        return QIcon(pixmap)


class SlidePreviewCard(QWidget):
    clicked = pyqtSignal(int)

    def __init__(self, index, image_path, parent=None):
        super().__init__(parent)
        self.index = index
        self.setFixedSize(200, 140)
        self.setCursor(Qt.CursorShape.PointingHandCursor)
        layout = QVBoxLayout(self)
        layout.setContentsMargins(5, 5, 5, 5)
        layout.setSpacing(5)
        self.img_label = QLabel()
        self.img_label.setFixedSize(190, 107)
        self.img_label.setStyleSheet("background-color: #333333; border-radius: 6px; border: 1px solid #444444;")
        self.img_label.setScaledContents(True)
        if image_path and os.path.exists(image_path):
            self.img_label.setPixmap(QPixmap(image_path))
        self.txt_label = QLabel(f"{index}")
        self.txt_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.txt_label.setStyleSheet("font-size: 14px; color: #dddddd; font-weight: bold;")
        layout.addWidget(self.txt_label)
        layout.addWidget(self.img_label)

    def mousePressEvent(self, event):
        play_click_sound()
        self.clicked.emit(self.index)


class SlideSelectorFlyout(QWidget):
    slide_selected = pyqtSignal(int)

    def __init__(self, ppt_app, parent=None):
        super().__init__(parent)
        self.ppt_app = ppt_app
        self.setStyleSheet("background-color: rgba(30, 30, 30, 240); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 12px;")
        layout = QVBoxLayout(self)
        layout.setContentsMargins(15, 15, 15, 15)
        title = QLabel("幻灯片预览")
        title.setStyleSheet("font-size: 18px; font-weight: bold; margin-bottom: 10px; color: white; border: none; background: transparent;")
        layout.addWidget(title)
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFixedSize(450, 500)
        scroll.setStyleSheet("""
            QScrollArea { border: none; background-color: rgba(30, 30, 30, 240); }
            QScrollBar:vertical {
                border: none;
                background: transparent;
                width: 8px;
                margin: 0;
            }
            QScrollBar::handle:vertical {
                background: rgba(255, 255, 255, 0.2);
                min-height: 20px;
                border-radius: 0px;
            }
            QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical {
                height: 0px;
            }
        """)
        container = QWidget()
        container.setStyleSheet("background-color: rgba(30, 30, 30, 240);")
        self.grid = QGridLayout(container)
        self.grid.setSpacing(15)
        self.load_slides()
        scroll.setWidget(container)
        layout.addWidget(scroll)

    def get_cache_dir(self, presentation_path):
        path_hash = hashlib.md5(presentation_path.encode('utf-8')).hexdigest()
        cache_dir = os.path.join(os.environ['APPDATA'], 'PPTAssistant', 'Cache', path_hash)
        if not os.path.exists(cache_dir):
            os.makedirs(cache_dir)
        return cache_dir

    def load_slides(self):
        try:
            presentation = self.ppt_app.ActivePresentation
            slides_count = presentation.Slides.Count
            presentation_path = presentation.FullName
            cache_dir = self.get_cache_dir(presentation_path)
            for i in range(1, slides_count + 1):
                slide = presentation.Slides(i)
                thumb_path = os.path.join(cache_dir, f"slide_{i}.jpg")
                if not os.path.exists(thumb_path):
                    try:
                        slide.Export(thumb_path, "JPG", 640, 360)
                    except:
                        pass
                card = SlidePreviewCard(i, thumb_path)
                card.clicked.connect(self.on_card_clicked)
                row = (i - 1) // 2
                col = (i - 1) % 2
                self.grid.addWidget(card, row, col)
        except Exception as e:
            print(f"Error loading slides: {e}")

    def on_card_clicked(self, index):
        self.slide_selected.emit(index)
        parent = self.parent()
        while parent:
            if isinstance(parent, Flyout):
                parent.close()
                break
            parent = parent.parent()


class PenSettingsFlyout(QWidget):
    color_selected = pyqtSignal(int)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setStyleSheet("background-color: rgba(30, 30, 30, 240); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 12px;")
        layout = QVBoxLayout(self)
        layout.setContentsMargins(15, 15, 15, 15)
        title = QLabel("笔颜色")
        title.setStyleSheet("font-size: 16px; font-weight: bold; margin-bottom: 10px; color: white; border: none; background: transparent;")
        layout.addWidget(title)
        grid_widget = QWidget()
        grid = QGridLayout(grid_widget)
        grid.setSpacing(10)
        colors = [
            (255, 0, 0), (0, 255, 0), (0, 0, 255), (255, 255, 0),
            (255, 0, 255), (0, 255, 255), (0, 0, 0), (255, 255, 255),
            (255, 165, 0), (128, 0, 128)
        ]
        for i, rgb in enumerate(colors):
            btn = QPushButton()
            btn.setFixedSize(40, 40)
            color_hex = f"#{rgb[0]:02x}{rgb[1]:02x}{rgb[2]:02x}"
            btn.setStyleSheet(f"""
                QPushButton {{
                    background-color: {color_hex};
                    border: 2px solid #555;
                    border-radius: 0px;
                }}
                QPushButton:hover {{
                    border: 2px solid white;
                }}
            """)
            ppt_color = rgb[0] + (rgb[1] << 8) + (rgb[2] << 16)
            btn.clicked.connect(lambda checked, c=ppt_color: self.on_color_clicked(c))
            row = i // 5
            col = i % 5
            grid.addWidget(btn, row, col)
        layout.addWidget(grid_widget)

    def on_color_clicked(self, color):
        play_click_sound()
        self.color_selected.emit(color)
        parent = self.parent()
        while parent:
            if isinstance(parent, Flyout):
                parent.close()
                break
            parent = parent.parent()


class EraserSettingsFlyout(QWidget):
    clear_all_clicked = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setStyleSheet("background-color: rgba(30, 30, 30, 240); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 12px;")
        layout = QVBoxLayout(self)
        layout.setContentsMargins(15, 15, 15, 15)
        btn = FluentPushButton("清除当前页笔迹")
        btn.setFixedSize(200, 40)
        btn.clicked.connect(self.on_clicked)
        layout.addWidget(btn)

    def on_clicked(self):
        play_click_sound()
        self.clear_all_clicked.emit()
        parent = self.parent()
        while parent:
            if isinstance(parent, Flyout):
                parent.close()
                break
            parent = parent.parent()


class SpotlightOverlay(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowFlags(Qt.WindowType.FramelessWindowHint | Qt.WindowType.WindowStaysOnTopHint | Qt.WindowType.Tool)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        from PyQt6.QtWidgets import QApplication
        self.setGeometry(QApplication.primaryScreen().geometry())
        self.selection_rect = QRect()
        self.is_selecting = False
        self.has_selection = False
        self.btn_close = QPushButton("X", self)
        self.btn_close.setFixedSize(30, 30)
        self.btn_close.setStyleSheet("background-color: red; color: white; border-radius: 0px; font-weight: bold;")
        self.btn_close.hide()
        self.btn_close.clicked.connect(self.close)

    def mousePressEvent(self, event: QMouseEvent):
        if event.button() == Qt.MouseButton.RightButton:
            self.close()
        elif event.button() == Qt.MouseButton.LeftButton:
            self.selection_rect.setTopLeft(event.pos())
            self.selection_rect.setBottomRight(event.pos())
            self.is_selecting = True
            self.has_selection = False
            self.btn_close.hide()
            self.update()

    def mouseMoveEvent(self, event: QMouseEvent):
        if self.is_selecting:
            self.selection_rect.setBottomRight(event.pos())
            self.update()

    def mouseReleaseEvent(self, event: QMouseEvent):
        if self.is_selecting:
            self.is_selecting = False
            self.has_selection = True
            normalized_rect = self.selection_rect.normalized()
            self.btn_close.move(normalized_rect.topRight() + QPoint(10, -15))
            self.btn_close.show()
            self.update()

    def paintEvent(self, event):
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)
        painter.setBrush(QColor(0, 0, 0, 180))
        painter.setPen(Qt.PenStyle.NoPen)
        painter.drawRect(self.rect())
        if self.has_selection or self.is_selecting:
            painter.setCompositionMode(QPainter.CompositionMode.CompositionMode_Clear)
            painter.setBrush(Qt.GlobalColor.transparent)
            painter.setPen(Qt.PenStyle.NoPen)
            painter.drawRoundedRect(self.selection_rect, 10, 10)
            painter.setCompositionMode(QPainter.CompositionMode.CompositionMode_SourceOver)
            painter.setBrush(Qt.BrushStyle.NoBrush)
            pen = QPen(QColor("#00cc7a"))
            pen.setWidth(3)
            painter.setPen(pen)
            painter.drawRoundedRect(self.selection_rect, 10, 10)


class PageNavWidget(QWidget):
    request_slide_jump = pyqtSignal(int)

    def __init__(self, parent=None, is_right=False):
        super().__init__(parent)
        self.ppt_app = None
        self.is_right = is_right
        self.anchor = "bottom"
        self.dragging = False
        self.press_time = 0.0
        self.drag_start_global = QPoint()
        self.window_start_pos = QPoint()
        self.long_press_threshold_ms = 300
        self.setWindowFlags(Qt.WindowType.FramelessWindowHint | Qt.WindowType.WindowStaysOnTopHint | Qt.WindowType.Tool)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.layout = QHBoxLayout()
        self.layout.setContentsMargins(0, 0, 0, 0)
        self.layout.setSpacing(0)
        self.container = QWidget()
        self.container.setStyleSheet("""
            QWidget#Container {
                background-color: rgba(30, 30, 30, 240);
                border: 1px solid rgba(255, 255, 255, 0.1);
                border-bottom: 1px solid rgba(255, 255, 255, 0.1);
                border-radius: 12px;
            }
            QLabel {
                font-family: "Segoe UI", "Microsoft YaHei";
                color: white;
            }
        """)
        self.container.setObjectName("Container")
        self.base_min_height = 52
        self.base_min_width = 0
        self.inner_layout = QHBoxLayout(self.container)
        self.inner_layout.setContentsMargins(8, 6, 8, 6)
        self.inner_layout.setSpacing(15)
        self.container.setMinimumHeight(self.base_min_height)
        self.btn_prev = TransparentToolButton(parent=self)
        self.btn_prev.setIcon(QIcon(icon_path("Previous.svg")))
        self.btn_prev.setFixedSize(36, 36)
        self.btn_prev.setIconSize(QSize(18, 18))
        self.btn_prev.setToolTip("上一页")
        self.btn_prev.installEventFilter(ToolTipFilter(self.btn_prev, 1000, ToolTipPosition.TOP))
        self.btn_prev.clicked.connect(play_click_sound)
        self.style_nav_btn(self.btn_prev)
        self.btn_next = TransparentToolButton(parent=self)
        self.btn_next.setIcon(QIcon(icon_path("Next.svg")))
        self.btn_next.setFixedSize(36, 36)
        self.btn_next.setIconSize(QSize(18, 18))
        self.btn_next.setToolTip("下一页")
        self.btn_next.installEventFilter(ToolTipFilter(self.btn_next, 1000, ToolTipPosition.TOP))
        self.btn_next.clicked.connect(play_click_sound)
        self.style_nav_btn(self.btn_next)
        self.page_info_widget = QWidget()
        self.page_info_widget.setCursor(Qt.CursorShape.PointingHandCursor)
        self.page_info_widget.installEventFilter(self)
        self.info_layout = QVBoxLayout(self.page_info_widget)
        self.info_layout.setContentsMargins(10, 0, 10, 0)
        self.info_layout.setSpacing(2)
        self.info_layout.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.page_num_style_large = "font-size: 18px; font-weight: bold; color: white;"
        self.page_num_style_small = "font-size: 14px; font-weight: bold; color: white;"
        self.page_text_style_large = "font-size: 12px; color: #aaaaaa;"
        self.page_text_style_small = "font-size: 10px; color: #aaaaaa;"
        self.lbl_page_num = QLabel("1/--")
        self.lbl_page_num.setStyleSheet(self.page_num_style_large)
        self.lbl_page_text = QLabel("页码")
        self.lbl_page_text.setStyleSheet(self.page_text_style_large)
        self.info_layout.addWidget(self.lbl_page_num, 0, Qt.AlignmentFlag.AlignCenter)
        self.info_layout.addWidget(self.lbl_page_text, 0, Qt.AlignmentFlag.AlignCenter)
        self.inner_layout.addWidget(self.btn_prev)
        self.line1 = QFrame()
        self.line1.setFrameShape(QFrame.Shape.VLine)
        self.line1.setStyleSheet("color: rgba(255, 255, 255, 0.2);")
        self.inner_layout.addWidget(self.line1)
        self.inner_layout.addWidget(self.page_info_widget)
        self.line2 = QFrame()
        self.line2.setFrameShape(QFrame.Shape.VLine)
        self.line2.setStyleSheet("color: rgba(255, 255, 255, 0.2);")
        self.inner_layout.addWidget(self.line2)
        self.inner_layout.addWidget(self.btn_next)
        self.layout.addWidget(self.container)
        self.setLayout(self.layout)
        self.update_orientation()

    def style_nav_btn(self, btn):
        btn.setStyleSheet("""
            TransparentToolButton {
                border-radius: 6px;
                border: none;
                background-color: transparent;
                color: white;
            }
            TransparentToolButton:hover {
                background-color: rgba(255, 255, 255, 0.1);
            }
            TransparentToolButton:pressed {
                background-color: rgba(255, 255, 255, 0.2);
            }
        """)

    def eventFilter(self, obj, event):
        if obj == self.page_info_widget:
            if event.type() == QEvent.Type.MouseButtonPress:
                if isinstance(event, QMouseEvent) and event.button() == Qt.MouseButton.LeftButton:
                    self.dragging = False
                    self.press_time = time.monotonic()
                    self.drag_start_global = event.globalPosition().toPoint()
                    self.window_start_pos = self.pos()
                return False
            elif event.type() == QEvent.Type.MouseMove:
                if isinstance(event, QMouseEvent) and event.buttons() & Qt.MouseButton.LeftButton:
                    if self.press_time > 0 and not self.dragging:
                        dt = (time.monotonic() - self.press_time) * 1000.0
                        if dt >= self.long_press_threshold_ms:
                            delta = event.globalPosition().toPoint() - self.drag_start_global
                            if delta.manhattanLength() > 3:
                                self.dragging = True
                    if self.dragging:
                        delta = event.globalPosition().toPoint() - self.drag_start_global
                        try:
                            from PyQt6.QtWidgets import QApplication
                            screen = QApplication.primaryScreen().geometry()
                        except Exception:
                            screen = QRect(0, 0, 1920, 1080)
                        nav_h = self.height()
                        margin = 20
                        min_y = margin
                        max_y = max(margin, screen.height() - nav_h - margin)
                        new_y = self.window_start_pos.y() + delta.y()
                        if new_y < min_y:
                            new_y = min_y
                        if new_y > max_y:
                            new_y = max_y
                        nav_w = self.width()
                        if self.is_right:
                            x = screen.width() - nav_w - margin
                        else:
                            x = margin
                        self.setGeometry(x, new_y, nav_w, nav_h)
                        return True
                return False
            elif event.type() == QEvent.Type.MouseButtonRelease:
                if self.dragging:
                    self.dragging = False
                    self.press_time = 0.0
                    self.snap_to_anchor()
                    return True
                else:
                    self.press_time = 0.0
                    if isinstance(event, QMouseEvent) and event.button() == Qt.MouseButton.LeftButton:
                        play_click_sound()
                        self.show_slide_selector()
                        return True
                    return False
            elif event.type() == QEvent.Type.MouseButtonDblClick:
                return super().eventFilter(obj, event)
        return super().eventFilter(obj, event)

    def update_orientation(self):
        if self.anchor == "middle":
            self.inner_layout.setDirection(QBoxLayout.Direction.TopToBottom)
            self.inner_layout.setContentsMargins(2, 8, 2, 8)
            self.inner_layout.setSpacing(12)
            self.inner_layout.setAlignment(Qt.AlignmentFlag.AlignCenter)
            self.container.setMinimumHeight(140)
            self.container.setMinimumWidth(56)
            self.info_layout.setContentsMargins(0, 0, 0, 0)
            self.btn_prev.setIcon(IconFactory.draw_arrow("#ffffff", "up"))
            self.btn_next.setIcon(IconFactory.draw_arrow("#ffffff", "down"))
            self.lbl_page_num.setStyleSheet(self.page_num_style_small)
            self.lbl_page_text.setStyleSheet(self.page_text_style_small)
            self.line1.setFrameShape(QFrame.Shape.HLine)
            self.line2.setFrameShape(QFrame.Shape.HLine)
            self.line1.show()
            self.line2.show()
        else:
            self.inner_layout.setDirection(QBoxLayout.Direction.LeftToRight)
            self.inner_layout.setContentsMargins(8, 6, 8, 6)
            self.inner_layout.setSpacing(15)
            self.inner_layout.setAlignment(Qt.AlignmentFlag.AlignCenter)
            self.container.setMinimumHeight(self.base_min_height)
            self.container.setMinimumWidth(self.base_min_width)
            self.info_layout.setContentsMargins(10, 0, 10, 0)
            self.btn_prev.setIcon(QIcon(icon_path("Previous.svg")))
            self.btn_next.setIcon(QIcon(icon_path("Next.svg")))
            self.lbl_page_num.setStyleSheet(self.page_num_style_large)
            self.lbl_page_text.setStyleSheet(self.page_text_style_large)
            self.line1.setFrameShape(QFrame.Shape.VLine)
            self.line2.setFrameShape(QFrame.Shape.VLine)
            self.line1.show()
            self.line2.show()
        
        # Force layout update and resize
        self.container.adjustSize()
        self.adjustSize()

    def snap_to_anchor(self):
        try:
            from PyQt6.QtWidgets import QApplication
            screen = QApplication.primaryScreen().geometry()
        except Exception:
            screen = QRect(0, 0, 1920, 1080)
        margin = 20
        nav_w = self.width()
        nav_h = self.height()
        top_y = margin
        bottom_y = max(margin, screen.height() - nav_h - margin)
        middle_y = max(margin, (screen.height() - nav_h) // 2)
        current_center = self.y() + nav_h // 2
        top_center = top_y + nav_h // 2
        mid_center = middle_y + nav_h // 2
        bottom_center = bottom_y + nav_h // 2
        options = [
            (abs(current_center - top_center), "top", top_y),
            (abs(current_center - mid_center), "middle", middle_y),
            (abs(current_center - bottom_center), "bottom", bottom_y),
        ]
        options.sort(key=lambda x: x[0])
        _, anchor, target_y = options[0]
        parent = self.parent()
        if parent is not None and hasattr(parent, "sync_nav_position"):
            try:
                parent.sync_nav_position(anchor, target_y)
                return
            except Exception:
                pass
        self.anchor = anchor
        if self.is_right:
            x = screen.width() - nav_w - margin
        else:
            x = margin
        self.setGeometry(x, target_y, nav_w, nav_h)
        self.update_orientation()

    def show_slide_selector(self):
        if not self.ppt_app:
            return
        view = SlideSelectorFlyout(self.ppt_app)
        view.slide_selected.connect(self.request_slide_jump.emit)
        flyout = AcrylicFlyout(view, self.window())
        flyout.exec(self.page_info_widget.mapToGlobal(self.page_info_widget.rect().bottomLeft()), FlyoutAnimationType.PULL_UP)

    def update_page(self, current, total):
        self.lbl_page_num.setText(f"{current}/{total}")

    def apply_settings(self):
        self.btn_prev.setToolTip("上一页")
        self.btn_next.setToolTip("下一页")
        self.lbl_page_text.setText("页码")
        self.style_nav_btn(self.btn_prev)
        self.style_nav_btn(self.btn_next)


class BoardPlaceholderWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("板中板")
        self.setWindowFlags(Qt.WindowType.FramelessWindowHint | Qt.WindowType.WindowStaysOnTopHint | Qt.WindowType.Tool)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        from PyQt6.QtWidgets import QApplication
        screen = QApplication.primaryScreen().geometry()
        self.setGeometry(screen)


class ToolBarWidget(QWidget):
    request_spotlight = pyqtSignal()
    request_pen_color = pyqtSignal(int)
    request_clear_ink = pyqtSignal()
    request_exit = pyqtSignal()
    request_board = pyqtSignal()

    def __init__(self):
        super().__init__()
        self.was_checked = False
        self.setWindowFlags(Qt.WindowType.FramelessWindowHint | Qt.WindowType.WindowStaysOnTopHint | Qt.WindowType.Tool)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        layout = QHBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        self.container = QWidget()
        self.container.setObjectName("Container")
        self.container.setAttribute(Qt.WidgetAttribute.WA_StyledBackground, True)
        self.container.setStyleSheet("""
            QWidget#Container {
                background-color: rgba(30, 30, 30, 240);
                border: 1px solid rgba(255, 255, 255, 0.1);
                border-bottom: 1px solid rgba(255, 255, 255, 0.1);
                border-radius: 12px;
            }
        """)
        container_layout = QHBoxLayout(self.container)
        container_layout.setContentsMargins(8, 6, 8, 6)
        container_layout.setSpacing(12)
        self.container.setMinimumHeight(56)
        self.group = QButtonGroup(self)
        self.group.setExclusive(True)
        self.btn_arrow = self.create_tool_btn("选择", QIcon(icon_path("Mouse.svg")))
        self.btn_pen = self.create_tool_btn("笔", QIcon(icon_path("Pen.svg")))
        self.btn_eraser = self.create_tool_btn("橡皮", QIcon(icon_path("Eraser.svg")))
        self.btn_clear = self.create_action_btn("一键清除", QIcon(icon_path("Clear.svg")))
        self.btn_clear.clicked.connect(self.request_clear_ink.emit)
        self.group.addButton(self.btn_arrow)
        self.group.addButton(self.btn_pen)
        self.group.addButton(self.btn_eraser)
        self.btn_board = self.create_action_btn("板中板", QIcon(icon_path("board-in-board.svg")))
        self.btn_board.clicked.connect(self.show_board_placeholder)
        self.btn_spotlight = self.create_action_btn("聚焦", QIcon(icon_path("Select.svg")))
        self.btn_spotlight.clicked.connect(self.request_spotlight.emit)
        self.btn_exit = self.create_action_btn("结束放映", QIcon(icon_path("Minimaze.svg")))
        self.btn_exit.clicked.connect(self.request_exit.emit)
        self.btn_exit.setStyleSheet("""
            TransparentToolButton {
                border-radius: 6px;
                border: none;
                background-color: transparent;
                color: white;
            }
            TransparentToolButton:hover {
                background-color: rgba(255, 50, 50, 0.3);
            }
            TransparentToolButton:pressed {
                background-color: rgba(255, 50, 50, 0.5);
            }
        """)
        container_layout.addWidget(self.btn_arrow)
        container_layout.addWidget(self.btn_pen)
        container_layout.addWidget(self.btn_eraser)
        container_layout.addWidget(self.btn_clear)
        line1 = QFrame()
        line1.setFrameShape(QFrame.Shape.VLine)
        line1.setStyleSheet("color: rgba(255, 255, 255, 0.2);")
        container_layout.addWidget(line1)
        container_layout.addWidget(self.btn_board)
        container_layout.addWidget(self.btn_spotlight)
        line2 = QFrame()
        line2.setFrameShape(QFrame.Shape.VLine)
        line2.setStyleSheet("color: rgba(255, 255, 255, 0.2);")
        container_layout.addWidget(line2)
        container_layout.addWidget(self.btn_exit)
        layout.addWidget(self.container)
        self.setLayout(layout)
        self.btn_arrow.setChecked(True)
        self.btn_pen.installEventFilter(self)
        self.btn_eraser.installEventFilter(self)

    def eventFilter(self, obj, event):
        if event.type() == QEvent.Type.MouseButtonPress:
            if obj in [self.btn_pen, self.btn_eraser]:
                self.was_checked = obj.isChecked()
        elif event.type() == QEvent.Type.MouseButtonRelease:
            if obj == self.btn_pen and self.was_checked and self.btn_pen.isChecked():
                self.show_pen_settings()
            elif obj == self.btn_eraser and self.was_checked and self.btn_eraser.isChecked():
                self.show_eraser_settings()
            self.was_checked = False
        return super().eventFilter(obj, event)

    def show_pen_settings(self):
        view = PenSettingsFlyout()
        view.color_selected.connect(self.request_pen_color.emit)
        flyout = AcrylicFlyout(view, self.window())
        flyout.exec(self.btn_pen.mapToGlobal(self.btn_pen.rect().bottomLeft()), FlyoutAnimationType.PULL_UP)

    def show_eraser_settings(self):
        view = EraserSettingsFlyout()
        view.clear_all_clicked.connect(self.request_clear_ink.emit)
        flyout = AcrylicFlyout(view, self.window())
        flyout.exec(self.btn_eraser.mapToGlobal(self.btn_eraser.rect().bottomLeft()), FlyoutAnimationType.PULL_UP)

    def show_board_placeholder(self):
        host = BoardPlaceholderWindow()
        host.show()
        host.raise_()
        host.activateWindow()
        box = MessageBox("板中板", "板中板功能正在开发中！", host)
        box.yesButton.setText("知道了")
        box.cancelButton.hide()
        box.buttonLayout.insertStretch(1)
        box.exec()
        host.close()

    def apply_settings(self):
        self.btn_arrow.setToolTip("选择")
        self.btn_pen.setToolTip("笔")
        self.btn_eraser.setToolTip("橡皮")
        self.btn_clear.setToolTip("一键清除")
        self.btn_spotlight.setToolTip("聚焦")
        self.btn_exit.setToolTip("结束放映")
        self.style_tool_btn(self.btn_arrow)
        self.style_tool_btn(self.btn_pen)
        self.style_tool_btn(self.btn_eraser)
        self.style_action_btn(self.btn_clear)
        self.style_action_btn(self.btn_spotlight)
        self.style_action_btn(self.btn_exit)

    def create_tool_btn(self, text, icon):
        btn = TransparentToolButton(parent=self)
        btn.setIcon(icon)
        btn.setFixedSize(40, 40)
        btn.setIconSize(QSize(20, 20))
        btn.setCheckable(True)
        btn.setToolTip(text)
        btn.installEventFilter(ToolTipFilter(btn, 1000, ToolTipPosition.TOP))
        btn.clicked.connect(play_click_sound)
        self.style_tool_btn(btn)
        return btn

    def create_action_btn(self, text, icon):
        btn = TransparentToolButton(parent=self)
        btn.setIcon(icon)
        btn.setFixedSize(40, 40)
        btn.setIconSize(QSize(20, 20))
        btn.setToolTip(text)
        btn.installEventFilter(ToolTipFilter(btn, 1000, ToolTipPosition.TOP))
        btn.clicked.connect(play_click_sound)
        self.style_action_btn(btn)
        return btn

    def style_tool_btn(self, btn):
        btn.setStyleSheet("""
            TransparentToolButton {
                border-radius: 6px;
                border: none;
                background-color: transparent;
                color: white;
                margin-bottom: 2px;
            }
            TransparentToolButton:hover {
                background-color: rgba(255, 255, 255, 0.1);
            }
            TransparentToolButton:checked {
                background-color: rgba(255, 255, 255, 0.1);
                color: white;
                border-bottom: 3px solid #00cc7a;
                border-bottom-left-radius: 2px;
                border-bottom-right-radius: 2px;
            }
            TransparentToolButton:checked:hover {
                background-color: rgba(255, 255, 255, 0.15);
            }
        """)

    def style_action_btn(self, btn):
        btn.setStyleSheet("""
            TransparentToolButton {
                border-radius: 6px;
                border: none;
                background-color: transparent;
                color: white;
            }
            TransparentToolButton:hover {
                background-color: rgba(255, 255, 255, 0.1);
            }
            TransparentToolButton:pressed {
                background-color: rgba(255, 255, 255, 0.2);
            }
        """)
