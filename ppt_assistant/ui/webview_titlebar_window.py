"""
带自绘标题栏的 WebView 窗口示例
"""

import os
import sys
import json
from pathlib import Path

from PySide6.QtWidgets import QApplication, QVBoxLayout, QWidget
from PySide6.QtCore import Qt, QUrl, QObject, Slot, QSize, QTimer
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtWebChannel import QWebChannel
from PySide6.QtGui import QScreen

from ppt_assistant.ui.titlebar_manager import FramelessWindowWithTitlebar, TitlebarBridge


class WebViewTitlebarWindow(FramelessWindowWithTitlebar):
    """
    带自绘标题栏和WebView的无框架窗口
    标题栏占用空间，Web内容区在下方
    """

    def __init__(self, content_url=None, title="Kazuha", titlebar_height=32, parent=None):
        """
        初始化窗口

        Args:
            content_url: 内容页面的URL（相对于应用根目录）
            title: 窗口标题
            titlebar_height: 标题栏高度
            parent: 父窗口
        """
        super().__init__(parent, titlebar_height)

        self.setWindowTitle(title)
        self.setMinimumSize(QSize(400, 300))
        self.resize(QSize(800, 600))

        # 创建主布局
        layout = QVBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)

        # 创建 WebView
        self.webview = QWebEngineView()
        self.setup_webview()

        # 添加到布局
        layout.addWidget(self.webview)

        # 设置到中央界面
        central_widget = QWidget()
        central_widget.setLayout(layout)
        self.setCentralWidget(central_widget)

        # 加载内容
        if content_url:
            self.load_content(content_url)

        # 确保 hwnd 更新
        QTimer.singleShot(100, self._update_hwnd)

    def _update_hwnd(self):
        """更新 Windows 窗口句柄"""
        if sys.platform == "win32":
            self._hwnd = int(self.winId())

    def setup_webview(self):
        """配置 WebView"""
        # 创建 WebChannel 用于 JS 通信
        channel = QWebChannel()

        # 创建标题栏桥接对象
        titlebar_bridge = TitlebarBridge(self)

        # 注册到通道
        channel.registerObject("titlebarBridge", titlebar_bridge)

        # 设置页面选项
        profile = self.webview.page().profile()
        # 可以在这里配置缓存、cookies 等

        # 设置通道
        self.webview.page().setWebChannel(channel)

    def load_content(self, url):
        """加载内容"""
        if url.startswith("http://") or url.startswith("https://"):
            self.webview.load(QUrl(url))
        else:
            # 相对路径，转换为本地文件 URL
            file_path = Path(__file__).parent / url
            if file_path.exists():
                self.webview.load(QUrl.fromLocalFile(str(file_path)))
            else:
                # 如果不存在，加载默认的标题栏 HTML
                self._load_builtin_titlebar()

    def _load_builtin_titlebar(self):
        """加载内置的标题栏 HTML"""
        titlebar_html_path = Path(__file__).parent / "titlebar.html"
        if titlebar_html_path.exists():
            self.webview.load(QUrl.fromLocalFile(str(titlebar_html_path)))
        else:
            # 降级处理：创建一个简单的 HTML
            self.load_html(self._create_fallback_html())

    def load_html(self, html_content):
        """直接加载 HTML 内容"""
        self.webview.setHtml(html_content)

    def _create_fallback_html(self):
        """创建备用 HTML（如果内置 HTML 不存在）"""
        return """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                    font-family: system-ui, -apple-system, sans-serif;
                    background: #f3f3f3;
                    color: #333;
                }
                .titlebar {
                    height: 32px;
                    background: #fafafa;
                    border-bottom: 1px solid #ddd;
                    display: flex;
                    align-items: center;
                    padding: 0 12px;
                    -webkit-app-region: drag;
                    overflow: hidden;
                }
                .titlebar-title { flex: 1; font-size: 13px; font-weight: 500; }
                .titlebar-controls { display: flex; gap: 4px; -webkit-app-region: no-drag; }
                button {
                    width: 36px; height: 32px; border: none; background: transparent;
                    cursor: pointer; font-size: 16px;
                }
                button:hover { background: rgba(0,0,0,0.06); }
                .content { padding: 20px; }
            </style>
        </head>
        <body>
            <div class="titlebar">
                <div class="titlebar-title" id="title">窗口标题</div>
                <div class="titlebar-controls">
                    <button onclick="window.titlebarBridge && window.titlebarBridge.minimize()">−</button>
                    <button onclick="window.titlebarBridge && window.titlebarBridge.maximize()">□</button>
                    <button onclick="window.titlebarBridge && window.titlebarBridge.close()">✕</button>
                </div>
            </div>
            <div class="content">
                <h2>欢迎使用带自绘标题栏的窗口</h2>
                <p>这个窗口支持：</p>
                <ul>
                    <li>✓ 自绘标题栏（带 Segoe MDL2 图标）</li>
                    <li>✓ 最小化、最大化、关闭功能</li>
                    <li>✓ 标题栏拖动窗口</li>
                    <li>✓ Windows 11 Snap 支持</li>
                    <li>✓ 亮/暗主题支持</li>
                </ul>
            </div>
            <script>
                // 获取标题
                document.getElementById('title').textContent =
                    document.querySelector('title')?.textContent || '窗口';
            </script>
        </body>
        </html>
        """

    def setCentralWidget(self, widget):
        """设置中央控件（替代默认行为）"""
        # 移除旧的中央控件
        if hasattr(self, '_central_widget') and self._central_widget:
            self._central_widget.setParent(None)

        # 设置新的中央控件
        if hasattr(self, '_layout'):
            # 替换原有的中央控件
            old_widget = self._layout.itemAt(0)
            if old_widget:
                old_widget.widget().setParent(None)
            self._layout.insertWidget(0, widget, 1)
        else:
            # 第一次设置
            self._central_widget = widget
            if not hasattr(self, '_layout'):
                layout = QVBoxLayout()
                layout.setContentsMargins(0, 0, 0, 0)
                layout.setSpacing(0)
                layout.addWidget(widget)
                self._layout = layout
                self.setLayout(layout)

    def set_titlebar_theme(self, theme):
        """设置标题栏主题（light 或 dark）"""
        js_code = f"""
            (function() {{
                const theme = '{theme}';
                document.body.setAttribute('data-theme', theme);
            }})();
        """
        self.webview.page().runJavaScript(js_code)

    def get_titlebar_height(self):
        """获取标题栏高度"""
        return self.titlebar_height

    def inject_titlebar_script(self, js_code):
        """在页面中注入脚本（在标题栏脚本之后）"""
        self.webview.page().runJavaScript(js_code)


class SimpleDialogWindow(WebViewTitlebarWindow):
    """简单对话框窗口示例"""

    def __init__(self, title="对话框", content_html="", parent=None):
        """
        初始化简单对话框

        Args:
            title: 窗口标题
            content_html: 内容 HTML
            parent: 父窗口
        """
        super().__init__(title=title, titlebar_height=32, parent=parent)

        self.setWindowModality(Qt.ApplicationModal)

        if content_html:
            self.load_html(content_html)


# ============================================================
# 测试和使用示例
# ============================================================

if __name__ == "__main__":
    app = QApplication(sys.argv)

    # 示例1：带自绘标题栏的窗口
    window = WebViewTitlebarWindow(title="Kazuha - PPT 助手")

    window.show()

    sys.exit(app.exec())
