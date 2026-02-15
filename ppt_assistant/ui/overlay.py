
import os
import sys
import math
import json
import importlib.util
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtCore import QObject, Slot, Signal, Qt, QUrl, QTimer, QRect, QPoint, QEvent, QByteArray
from PySide6.QtGui import QColor, QRegion, QGuiApplication, QIcon
from PySide6.QtQuick import QQuickView
from PySide6.QtQml import QQmlComponent
from ppt_assistant.core.config import cfg
from ppt_assistant.core.app_icon import load_app_icon
from ppt_assistant.core.icon_helper import get_file_icon_base64
import psutil
import asyncio
import threading
import subprocess
try:
    from winsdk.windows.media.control import GlobalSystemMediaTransportControlsSessionManager
    WINSDK_AVAILABLE = True
except ImportError:
    WINSDK_AVAILABLE = False

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

    @Slot(bool)
    def inkPromptResult(self, keep):
        self._overlay.ink_prompt_result.emit(bool(keep))

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

class InkPromptBridge(QObject):
    result = Signal(bool)

    @Slot()
    def keep(self):
        self.result.emit(True)

    @Slot()
    def discard(self):
        self.result.emit(False)

class OverlayWindow(QWebEngineView):
    request_next = Signal()
    request_prev = Signal()
    request_clear = Signal()
    request_end = Signal()
    request_ptr_arrow = Signal()
    request_ptr_pen = Signal()
    request_ptr_eraser = Signal()
    request_pen_color = Signal(int, int, int)
    ink_prompt_result = Signal(bool)
    
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
        self._ink_prompt_view = None
        self._ink_prompt_bridge = None
        
        self._smtc_info = {"status": "", "title": ""}
        self._smtc_thread = None
        self._stop_smtc = False
        self._start_smtc_thread()

        self.load_plugins()
        self.bind_config_signals()
        
        self.status_timer = QTimer(self)
        self.status_timer.timeout.connect(self._update_system_status)
        self.status_timer.start(2000)
        
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
        if monitor and hasattr(monitor, "set_overlay"):
            monitor.set_overlay(self)
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
        if self._ink_prompt_view:
            self._apply_ink_prompt_context(self._ink_prompt_view.rootContext())

    def _get_media_info_from_powershell(self):
        script = r'''
$ErrorActionPreference="SilentlyContinue"
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$manager=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync().GetAwaiter().GetResult()
$session=$manager.GetCurrentSession()
if ($session -eq $null) { @{status=""; title=""} | ConvertTo-Json -Compress; exit }
$props=$session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult()
$statusValue=[int]$session.GetPlaybackInfo().PlaybackStatus
$title=$props.Title
$artist=$props.Artist
$display=$title
if ($artist) { $display="$title - $artist" }
$state="Stopped"
if ($statusValue -eq 4) { $state="Playing" } elseif ($statusValue -eq 5) { $state="Paused" }
@{status=$state; title=$display} | ConvertTo-Json -Compress
'''
        try:
            result = subprocess.run(
                ["powershell", "-NoProfile", "-Command", script],
                capture_output=True,
                text=True,
                timeout=1.5
            )
            raw = (result.stdout or "").strip()
            if raw:
                data = json.loads(raw)
                if isinstance(data, dict):
                    return {
                        "status": data.get("status", "") or "",
                        "title": data.get("title", "") or ""
                    }
        except Exception:
            pass
        return {"status": "", "title": ""}

    def _start_smtc_thread(self):
        def smtc_loop():
            loop = None
            manager = None

            if WINSDK_AVAILABLE:
                loop = asyncio.new_event_loop()
                asyncio.set_event_loop(loop)

                async def init_manager():
                    return await GlobalSystemMediaTransportControlsSessionManager.request_async()

                try:
                    manager = loop.run_until_complete(init_manager())
                except Exception:
                    manager = None

                async def get_media_info():
                    if not manager:
                        return {"status": "", "title": ""}
                    try:
                        session = None
                        try:
                            sessions = await manager.get_sessions_async()
                        except Exception:
                            sessions = None
                        if sessions:
                            for s in sessions:
                                try:
                                    info = s.get_playback_info()
                                    if info and info.playback_status == 4:
                                        session = s
                                        break
                                except Exception:
                                    pass
                        if not session:
                            try:
                                session = manager.get_current_session()
                            except Exception:
                                session = None
                        if session:
                            info = await session.try_get_media_properties_async()
                            title = info.title if info else ""
                            artist = info.artist if info else ""
                            status = session.get_playback_info().playback_status

                            display_text = title
                            if artist:
                                display_text = f"{title} - {artist}"

                            status_str = "Stopped"
                            if status == 4:
                                status_str = "Playing"
                            elif status == 5:
                                status_str = "Paused"

                            return {"status": status_str, "title": display_text}
                    except Exception:
                        pass
                    return {"status": "", "title": ""}

            while not self._stop_smtc:
                try:
                    if WINSDK_AVAILABLE and loop:
                        info = loop.run_until_complete(get_media_info())
                    else:
                        info = self._get_media_info_from_powershell()
                    self._smtc_info = info
                except Exception:
                    pass
                for _ in range(20):
                    if self._stop_smtc:
                        break
                    import time
                    time.sleep(0.1)

            if loop:
                try:
                    loop.close()
                except Exception:
                    pass

        self._smtc_thread = threading.Thread(target=smtc_loop, daemon=True)
        self._smtc_thread.start()

    def _update_system_status(self):
        try:
            # Battery
            battery = psutil.sensors_battery()
            is_desktop = False
            battery_percent = 100
            battery_charging = False
            
            if battery:
                battery_percent = int(battery.percent)
                battery_charging = battery.power_plugged
            else:
                is_desktop = True
                
            # Network
            net_stats = psutil.net_if_stats()
            network_online = False
            # Check for any active interface (excluding loopback)
            for iface, stats in net_stats.items():
                if stats.isup and 'loopback' not in iface.lower():
                    network_online = True
                    break
            
            # Volume (Placeholder for now as pycaw/comtypes might not be present)
            volume = -1
            
            # SMTC
            smtc_status = self._smtc_info.get("status", "")
            smtc_title = self._smtc_info.get("title", "")
            
            data = {
                "is_desktop": is_desktop,
                "battery_percent": battery_percent,
                "battery_charging": battery_charging,
                "network_online": network_online,
                "volume": volume,
                "smtc_status": smtc_status,
                "smtc_title": smtc_title
            }
            
            js = f"if(window.updateSystemStatus) window.updateSystemStatus({json.dumps(data)});"
            self.page().runJavaScript(js)
        except Exception as e:
            print(f"Status update error: {e}")

    def _on_status_bar_visibility_changed(self, visible):
        js = f"if(window.toggleStatusBar) window.toggleStatusBar({'true' if visible else 'false'});"
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
            "showStatusBar": cfg.showStatusBar.value,
            "showToolbarText": cfg.showToolbarText.value,
            "toolbarOrder": cfg.toolbarOrder.value,
            "toolbarPosition": cfg.toolbarPosition.value,
            "showClear": cfg.showClear.value,
            "clearMode": cfg.clearMode.value,
            "showSpotlight": cfg.showSpotlight.value,
            "showBoardInBoard": cfg.showBoardInBoard.value,
            "showTimer": cfg.showTimer.value,
            "scale": cfg.scale.value,
            "safeArea": cfg.safeArea.value,
            "popWindowScale": cfg.popWindowScale.value,
            "texts": trans_map,
            "apps": apps_list
        }
        
        js = f"if(window.updateConfig) window.updateConfig({json.dumps(config_data)});"
        try:
            self.page().runJavaScript(js)
        except RuntimeError:
            pass

    def show_ink_prompt(self):
        try:
            self._ensure_ink_prompt_view()
            if self._ink_prompt_view:
                self._ink_prompt_view.show()
                self._ink_prompt_view.raise_()
        except Exception:
            pass

    def _ensure_ink_prompt_view(self):
        if not self._ink_prompt_view:
            view = QQuickView()
            view.setColor(Qt.transparent)
            view.setFlags(Qt.FramelessWindowHint | Qt.Tool | Qt.WindowStaysOnTopHint)
            view.setResizeMode(QQuickView.SizeRootObjectToView)
            view.setModality(Qt.ApplicationModal)

            bridge = InkPromptBridge()
            bridge.result.connect(self._on_ink_prompt_result)

            ctx = view.rootContext()
            ctx.setContextProperty("inkBridge", bridge)
            self._ink_prompt_view = view
            self._ink_prompt_bridge = bridge
            self._apply_ink_prompt_context(ctx)

            qml = """
import QtQuick 2.15
import QtQuick.Controls 2.15

Item {
    id: root
    width: screenWidth
    height: screenHeight

    Rectangle {
        anchors.fill: parent
        color: maskColor
    }

    MouseArea {
        anchors.fill: parent
    }

    Rectangle {
        id: card
        width: Math.min(parent.width * 0.6, 460)
        height: content.height + 48
        color: dialogBg
        radius: 12
        border.color: dialogBorder
        border.width: 1
        anchors.centerIn: parent

        Column {
            id: content
            spacing: 20
            width: parent.width - 48
            anchors.centerIn: parent

            Text {
                text: inkTitle
                font.pixelSize: 17
                font.bold: true
                color: titleColor
                width: parent.width
                wrapMode: Text.Wrap
            }

            Text {
                text: inkText
                font.pixelSize: 15
                font.weight: Font.Normal
                color: bodyColor
                width: parent.width
                wrapMode: Text.Wrap
                lineHeight: 1.4
            }

            Item {
                width: parent.width
                height: 4
            }

            Row {
                spacing: 12
                layoutDirection: Qt.RightToLeft
                width: parent.width

                Rectangle {
                    width: 88
                    height: 34
                    radius: 17
                    color: primaryBg
                    border.color: primaryBorder
                    border.width: 1
                    
                    Text {
                        anchors.centerIn: parent
                        text: inkKeep
                        font.pixelSize: 14
                        font.bold: true
                        color: primaryText
                    }
                    MouseArea {
                        anchors.fill: parent
                        cursorShape: Qt.PointingHandCursor
                        onClicked: inkBridge.keep()
                    }
                }

                Rectangle {
                    width: 88
                    height: 34
                    radius: 17
                    color: btnBg
                    border.color: btnBorder
                    border.width: 1
                    
                    Text {
                        anchors.centerIn: parent
                        text: inkDiscard
                        font.pixelSize: 14
                        font.bold: true
                        color: btnText
                    }
                    MouseArea {
                        anchors.fill: parent
                        cursorShape: Qt.PointingHandCursor
                        onClicked: inkBridge.discard()
                    }
                }
            }
        }
    }
}
"""

            component = QQmlComponent(view.engine())
            component.setData(QByteArray(qml.encode("utf-8")), QUrl())
            root = component.create()
            view.setContent(QUrl(), component, root)

        screen = self.screen() or QGuiApplication.primaryScreen()
        if screen:
            self._ink_prompt_view.setGeometry(screen.geometry())
            self._apply_ink_prompt_context(self._ink_prompt_view.rootContext())

    def _apply_ink_prompt_context(self, ctx):
        texts = self._get_ink_prompt_texts()
        palette = self._get_ink_prompt_palette()
        ctx.setContextProperty("inkTitle", texts["title"])
        ctx.setContextProperty("inkText", texts["text"])
        ctx.setContextProperty("inkKeep", texts["keep"])
        ctx.setContextProperty("inkDiscard", texts["discard"])
        ctx.setContextProperty("maskColor", palette["mask"])
        ctx.setContextProperty("dialogBg", palette["bg"])
        ctx.setContextProperty("dialogBorder", palette["border"])
        ctx.setContextProperty("titleColor", palette["title"])
        ctx.setContextProperty("bodyColor", palette["body"])
        ctx.setContextProperty("btnBg", palette["btn_bg"])
        ctx.setContextProperty("btnBorder", palette["btn_border"])
        ctx.setContextProperty("btnText", palette["btn_text"])
        ctx.setContextProperty("primaryBg", palette["primary_bg"])
        ctx.setContextProperty("primaryBorder", palette["primary_border"])
        ctx.setContextProperty("primaryText", palette["primary_text"])
        if self._ink_prompt_view:
            size = self._ink_prompt_view.size()
            ctx.setContextProperty("screenWidth", size.width())
            ctx.setContextProperty("screenHeight", size.height())

    def _get_ink_prompt_texts(self):
        return {
            "title": "是否保留墨迹注释？",
            "text": "检测到放映期间添加了墨迹注释，是否保留到幻灯片中？",
            "keep": "保留",
            "discard": "不保留"
        }

    def _get_ink_prompt_palette(self):
        from qfluentwidgets import themeColor
        accent = themeColor().name()
        if self._is_light:
            return {
                "mask": "rgba(0, 0, 0, 1.0)",
                "bg": "#ffffff",
                "border": "rgba(0, 0, 0, 0.05)",
                "title": "#191919",
                "body": "#191919",
                "btn_bg": "transparent",
                "btn_border": "rgba(0, 0, 0, 0.05)",
                "btn_text": "#666666",
                "primary_bg": "transparent",
                "primary_border": accent,
                "primary_text": accent
            }
        return {
            "mask": "rgba(0, 0, 0, 1.0)",
            "bg": "#2b2b2b",
            "border": "rgba(255, 255, 255, 0.08)",
            "title": "#E5E5E5",
            "body": "#E5E5E5",
            "btn_bg": "transparent",
            "btn_border": "rgba(255, 255, 255, 0.08)",
            "btn_text": "#909090",
            "primary_bg": "transparent",
            "primary_border": accent,
            "primary_text": accent
        }

    def _on_ink_prompt_result(self, keep):
        if self._ink_prompt_view:
            try:
                self._ink_prompt_view.hide()
            except Exception:
                pass
        self.ink_prompt_result.emit(bool(keep))

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
        cfg.toolbarPosition.valueChanged.connect(lambda *_: self.update_config())
        cfg.quickLaunchApps.valueChanged.connect(lambda *_: self.update_config())
        cfg.showToolbarText.valueChanged.connect(lambda *_: self.update_config())
        cfg.showClear.valueChanged.connect(lambda *_: self.update_config())
        cfg.clearMode.valueChanged.connect(lambda *_: self.update_config())
        cfg.showSpotlight.valueChanged.connect(lambda *_: self.update_config())
        cfg.showBoardInBoard.valueChanged.connect(lambda *_: self.update_config())
        cfg.showTimer.valueChanged.connect(lambda *_: self.update_config())
        cfg.scale.valueChanged.connect(lambda *_: self.update_config())
        cfg.safeArea.valueChanged.connect(lambda *_: self.update_config())
        cfg.popWindowScale.valueChanged.connect(lambda *_: self.update_config())

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
        self._stop_smtc = True
        if self._smtc_thread:
            self._smtc_thread.join(timeout=1.0)
        
    def update_geometry(self, rect, screen):
        if screen:
            self.setGeometry(screen.geometry())
        elif rect:
            self.setGeometry(rect)
