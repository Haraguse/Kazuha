#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
自绘标题栏演示程序
运行此脚本查看完整的标题栏功能演示

使用方法:
    python demo_titlebar_window.py
"""

import sys
import os
from pathlib import Path

# 添加项目根目录到Python路径
ROOT_DIR = Path(__file__).parent.parent.parent
sys.path.insert(0, str(ROOT_DIR))

from PySide6.QtWidgets import QApplication
from PySide6.QtCore import QUrl, QTimer
from PySide6.QtGui import QIcon

from ppt_assistant.ui.webview_titlebar_window import WebViewTitlebarWindow


class DemoWindow(WebViewTitlebarWindow):
    """演示窗口"""

    def __init__(self):
        """初始化演示窗口"""
        super().__init__(
            title="Kazuha - 自绘标题栏演示",
            titlebar_height=32
        )

        self.setWindowTitle("Kazuha - PPT 助手")
        self.move(100, 100)

        # 加载演示页面
        self.load_demo_page()

        # 设置初始主题
        QTimer.singleShot(500, self._on_page_loaded)

    def load_demo_page(self):
        """加载演示页面"""
        demo_html_path = Path(__file__).parent / "titlebar_demo.html"

        if demo_html_path.exists():
            print(f"✓ 加载演示页面: {demo_html_path}")
            self.webview.load(QUrl.fromLocalFile(str(demo_html_path)))
        else:
            print(f"✗ 演示页面未找到: {demo_html_path}")
            self._load_fallback()

    def _load_fallback(self):
        """加载备用页面"""
        fallback_html = """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
            <meta charset="UTF-8">
            <title>自绘标题栏演示</title>
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                    font-family: system-ui, -apple-system, sans-serif;
                    background: #f5f5f5;
                    color: #333;
                    padding: 40px;
                }
                h1 { color: #3275F5; margin-bottom: 20px; }
                .feature {
                    background: white;
                    padding: 16px;
                    margin: 12px 0;
                    border-radius: 8px;
                    border-left: 3px solid #3275F5;
                }
                .feature h3 { margin-bottom: 8px; }
                .feature p { color: #666; line-height: 1.6; }
                .button-group { display: flex; gap: 8px; margin-top: 20px; flex-wrap: wrap; }
                button {
                    padding: 8px 16px;
                    background: #3275F5;
                    color: white;
                    border: none;
                    border-radius: 6px;
                    cursor: pointer;
                    font-size: 13px;
                    font-weight: 500;
                }
                button:hover { opacity: 0.9; }
                .code {
                    background: #f0f0f0;
                    padding: 12px;
                    border-radius: 4px;
                    font-family: monospace;
                    font-size: 12px;
                    overflow-x: auto;
                    margin-top: 8px;
                }
            </style>
        </head>
        <body>
            <h1>🎉 自绘标题栏演示</h1>

            <div class="feature">
                <h3>✨ 功能概览</h3>
                <p>此窗口演示了完整的自绘标题栏功能：</p>
                <ul style="margin-left: 20px; margin-top: 8px;">
                    <li>自绘标题栏（占用空间，压缩内容区）</li>
                    <li>Segoe MDL2 字体图标（最小化、最大化、关闭）</li>
                    <li>获取并显示窗口标题</li>
                    <li>标题栏拖动窗口</li>
                    <li>双击标题栏切换最大化</li>
                    <li>Windows 11 Snap 支持</li>
                </ul>
            </div>

            <div class="feature">
                <h3>🖱️ 尝试以下操作</h3>
                <div class="button-group">
                    <button onclick="alert('点击标题栏右侧的最小化按钮，或使用此演示页面上的按钮')">最小化窗口</button>
                    <button onclick="alert('点击标题栏右侧的最大化按钮切换窗口大小')">最大化/还原</button>
                    <button onclick="alert('点击标题栏右侧的关闭按钮关闭窗口')">关闭窗口</button>
                </div>
            </div>

            <div class="feature">
                <h3>🎨 标题栏设计</h3>
                <p>标题栏位于窗口顶部，包含：</p>
                <ul style="margin-left: 20px; margin-top: 8px;">
                    <li>左侧：应用图标和窗口标题</li>
                    <li>右侧：最小化、最大化、关闭按钮</li>
                    <li>背景：半透明毛玻璃效果</li>
                </ul>
            </div>

            <div class="feature">
                <h3>📝 使用示例</h3>
                <p>在 HTML 中使用标题栏 Bridge：</p>
                <div class="code">// 通过 QWebChannel 调用 Python 函数
window.titlebarBridge.minimize();
window.titlebarBridge.maximize();
window.titlebarBridge.close();
window.titlebarBridge.get_window_state();
window.titlebarBridge.get_window_title();
window.titlebarBridge.set_window_title("新标题");</div>
            </div>

            <div class="feature">
                <h3>🔧 技术细节</h3>
                <ul style="margin-left: 20px; margin-top: 8px;">
                    <li>基于 PySide6 (Qt6) 开发</li>
                    <li>无框架窗口 (FramelessWindowHint)</li>
                    <li>通过 Windows API 实现窗口控制</li>
                    <li>WebChannel 实现 Python <-> JavaScript 通信</li>
                    <li>Segoe MDL2 Assets 字体提供专业图标</li>
                </ul>
            </div>

            <div class="feature">
                <h3>✅ 文件结构</h3>
                <div class="code">ppt_assistant/ui/
├── titlebar_manager.py          # 核心窗口管理
├── titlebar.html                # 标题栏 HTML/CSS/JS
├── titlebar_demo.html           # 完整演示页面
├── webview_titlebar_window.py   # 集成的 WebView 窗口
└── demo_titlebar_window.py      # 此演示程序</div>
            </div>
        </body>
        </html>
        """
        self.load_html(fallback_html)

    def _on_page_loaded(self):
        """页面加载完成后的处理"""
        # 这里可以进行一些初始化
        print("✓ 页面已加载完成")


def main():
    """主入口函数"""
    print("=" * 60)
    print("自绘标题栏窗口演示")
    print("=" * 60)

    app = QApplication(sys.argv)

    # 创建窗口
    print("\n正在创建窗口...")
    window = DemoWindow()

    # 显示窗口
    print("✓ 窗口已创建")
    print("\n功能演示：")
    print("  • 点击标题栏拖动窗口")
    print("  • 双击标题栏切换最大化")
    print("  • 使用按钮控制窗口（最小化、最大化、关闭）")
    print("  • 将窗口拖至屏幕边缘测试 Windows 11 Snap")
    print("=" * 60 + "\n")

    window.show()

    sys.exit(app.exec())


if __name__ == "__main__":
    main()
