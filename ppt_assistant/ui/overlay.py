
import os
import sys
import math
import json
import importlib.util
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtCore import QObject, Slot, Signal, Qt, QUrl, QTimer, QRect, QPoint, QEvent
from PySide6.QtGui import QColor, QRegion, QGuiApplication, QIcon
from ppt_assistant.core.config import cfg
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.icon_helper import get_file_icon_base64

PLUGIN_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "plugins", "builtins")

class OverlayBridge(QObject):
    def __init__(self, overlay):
        super().__init__()
        self._overlay = overlay

    @Slot()
    def requestInitState(self):
        if self._overlay.monitor:
            pass
        self._overlay.update_theme()
        self._overlay.update_config()

    @Slot(int, int, int)
    def setPenColor(self, r, g, b):
        self._overlay.request_pen_color.emit(r, g, b)

    @Slot(str)
    def setTool(self, tool_name):
        if tool_name == 'select' or tool_name == 'arrow':
            self._overlay.request_ptr_arrow.emit()
        elif tool_name == 'pen':
            self._overlay.request_ptr_pen.emit()
        elif tool_name == 'eraser':
            self._overlay.request_ptr_eraser.emit()

    @Slot()
    def prevPage(self):
        self._overlay.request_prev.emit()

    @Slot()
    def nextPage(self):
        self._overlay.request_next.emit()

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
            os.startfile(path)
        except Exception as e:
            print(f"Failed to launch app {path}: {e}")

    @Slot('QVariantList')
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
        try:
            self._overlay.nudge_size()
        except Exception:
            pass

class OverlayWindow(QWebEngineView):
    request_next = Signal()
    request_prev = Signal()
    request_clear = Signal()
    request_end = Signal()
    request_ptr_arrow = Signal()
    request_ptr_pen = Signal()
    request_ptr_eraser = Signal()
    request_pen_color = Signal(int, int, int)
    
    def __init__(self):
        super().__init__()
        
        self.setWindowFlags(Qt.FramelessWindowHint | Qt.WindowDoesNotAcceptFocus | Qt.Tool | Qt.WindowStaysOnTopHint)
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setAttribute(Qt.WA_NoSystemBackground)
        
        self.page().setBackgroundColor(Qt.transparent)
        
        self.channel = QWebChannel()
        self.bridge = OverlayBridge(self)
        self.channel.registerObject("bridge", self.bridge)
        self.page().setWebChannel(self.channel)
        
        html_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overlay.html")
        url = QUrl.fromLocalFile(html_path)
        self.load(url)
        
        self.monitor = None
        self.plugins = []
        self._icon_cache = {}
        self._is_light = False
        self._slideshow_hwnd = 0
        self._protected_view = False
        self._presentation_readonly = False
        self._active_on_slideshow = False
        
        self.load_plugins()
        self.bind_config_signals()
        
        screen = QGuiApplication.primaryScreen()
        if screen:
            self.setGeometry(screen.geometry())
            
        icon = load_app_icon()
        if not icon.isNull():
            self.setWindowIcon(icon)
    
    def nudge_size(self):
        try:
            w = self.width()
            h = self.height()
            self.resize(w + 1, h)
            QTimer.singleShot(0, lambda: self.resize(w, h))
        except Exception:
            pass

    def set_monitor(self, monitor):
        self.monitor = monitor
        self.monitor.slide_changed.connect(self.on_slide_changed)
        
    def on_slide_changed(self, current, total):
        script = f"updatePageInfo({current}, {total});"
        self.page().runJavaScript(script)

    def update_page_info(self, current, total):
        try:
            current = int(current)
            total = int(total)
        except Exception:
            return
        script = f"updatePageInfo({current}, {total});"
        self.page().runJavaScript(script)

    def update_mask(self, rects_data):
        region = QRegion()
        for r in rects_data:
            x = math.floor(r['x'])
            y = math.floor(r['y'])
            w = math.ceil(r['x'] + r['width']) - x
            h = math.ceil(r['y'] + r['height']) - y
            
            rect = QRect(x - 1, y - 1, w + 2, h + 2)
            region += rect
            
        if not region.isEmpty():
            self.setMask(region)
        else:
            if self.isVisible():
                 pass
            pass

    def update_theme(self):
        mode = cfg.themeMode.value
        is_light = False
        if str(mode).lower() == "light":
            is_light = True
        elif str(mode).lower() == "dark":
            is_light = False
        else:
            from qfluentwidgets import isDarkTheme
            is_light = not isDarkTheme()
            
        self._is_light = is_light
        
        from qfluentwidgets import themeColor
        t_color = themeColor()
        color_str = t_color.name()
        
        js = f"setTheme({'false' if is_light else 'true'}, '{color_str}');"
        self.page().runJavaScript(js)

    def update_config(self):
        try:
            # Check if C++ object is still valid
            if not self.page():
                return
        except RuntimeError:
            return

        trans_map = {
            "select": "选择",
            "pen": "画笔",
            "eraser": "橡皮",
            "clear": "清屏",
            "spotlight": "聚光灯",
            "board_in_board": "板中板",
            "timer": "计时器",
            "end": "结束放映",
            "apps": "更多"
        }
        
        apps_list = []
        if hasattr(cfg, 'quickLaunchApps'):
            for app in cfg.quickLaunchApps.value:
                path = ""
                name = ""
                
                if isinstance(app, str):
                    path = app
                    name = os.path.basename(app)
                    if name.lower().endswith('.exe'):
                        name = name[:-4]
                elif isinstance(app, dict):
                    path = app.get("path", "")
                    name = app.get("name", "")
                
                if not path:
                    continue
                    
                # Get icon (cached)
                icon_data = None
                if path in self._icon_cache:
                    icon_data = self._icon_cache[path]
                else:
                    icon_data = get_file_icon_base64(path)
                    if icon_data:
                        self._icon_cache[path] = icon_data
                        
                apps_list.append({
                    "name": name, 
                    "path": path,
                    "icon": icon_data
                })

        config_data = {
            "showToolbarText": cfg.showToolbarText.value,
            "toolbarOrder": cfg.toolbarOrder.value,
            "showClear": cfg.showClear.value,
            "clearMode": cfg.clearMode.value,
            "showSpotlight": cfg.showSpotlight.value,
            "showBoardInBoard": cfg.showBoardInBoard.value,
            "showTimer": cfg.showTimer.value,
            "texts": trans_map,
            "apps": apps_list
        }
        
        js = f"if(window.updateConfig) window.updateConfig({json.dumps(config_data)});"
        try:
            self.page().runJavaScript(js)
        except RuntimeError:
            pass

    def load_plugins(self):
        if not os.path.exists(PLUGIN_DIR):
            return
        for entry in os.listdir(PLUGIN_DIR):
            plugin_dir = os.path.join(PLUGIN_DIR, entry)
            if not os.path.isdir(plugin_dir):
                continue
            manifest_path = os.path.join(plugin_dir, "manifest.json")
            if not os.path.exists(manifest_path):
                continue
            try:
                with open(manifest_path, "r", encoding="utf-8") as f:
                    manifest = json.load(f)
                entry_point = manifest.get("entry")
                if not entry_point:
                    continue
                module_name, class_name = entry_point.rsplit(".", 1)
                module_path = os.path.join(plugin_dir, module_name + ".py")
                if not os.path.exists(module_path):
                    continue
                spec = importlib.util.spec_from_file_location(f"plugins.builtins.{entry}.{module_name}", module_path)
                module = importlib.util.module_from_spec(spec)
                spec.loader.exec_module(module)
                plugin_cls = getattr(module, class_name, None)
                if plugin_cls is None:
                    continue
                plugin_instance = plugin_cls()
                plugin_instance.manifest = manifest
                if hasattr(plugin_instance, "set_context"):
                    plugin_instance.set_context(self)
                self.plugins.append(plugin_instance)
                print(f"Loaded plugin: {plugin_instance.get_name()}")
            except Exception as e:
                print(f"Failed to load plugin {entry}: {e}")

    def execute_plugin(self, name):
        for plugin in self.plugins:
            if hasattr(plugin, "get_name") and plugin.get_name() == name:
                if hasattr(plugin, "execute"):
                    plugin.execute()
                return

    def bind_config_signals(self):
        cfg.toolbarOrder.valueChanged.connect(lambda *_: self.update_config())
        cfg.quickLaunchApps.valueChanged.connect(lambda *_: self.update_config())
        cfg.showToolbarText.valueChanged.connect(lambda *_: self.update_config())
        cfg.showClear.valueChanged.connect(lambda *_: self.update_config())
        cfg.clearMode.valueChanged.connect(lambda *_: self.update_config())
        cfg.showSpotlight.valueChanged.connect(lambda *_: self.update_config())
        cfg.showBoardInBoard.valueChanged.connect(lambda *_: self.update_config())
        cfg.showTimer.valueChanged.connect(lambda *_: self.update_config())

    def showEvent(self, event):
        super().showEvent(event)
        self.page().setBackgroundColor(Qt.transparent)
        self.update_theme()

    def set_active_on_slideshow(self, active: bool, animate: bool = True):
        self._active_on_slideshow = bool(active)
        if self._active_on_slideshow:
            try:
                self.show()
                self.raise_()
            except Exception:
                pass
        else:
            try:
                self.hide()
            except Exception:
                pass

    def on_slideshow_start_cleanup(self):
        pass

    def on_slideshow_end_cleanup(self):
        pass
        
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
        pass
        
    def update_geometry(self, rect, screen):
        if screen:
            self.setGeometry(screen.geometry())
        elif rect:
            self.setGeometry(rect)

