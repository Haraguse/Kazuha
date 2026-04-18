"""
DirectX/OpenGL渲染管线与PySide6应用的集成示例
展示如何在现有应用中集成新的渲染系统
"""

from typing import Optional
from ppt_assistant.rendering.integration_layer import RenderingPipeline
from ppt_assistant.rendering.config import RenderConfig, RenderAPI


class RenderedApplication:
    """
    使用DirectX/OpenGL渲染的应用程序包装类
    provides a template for integrating the new rendering pipeline into your app
    """

    def __init__(self, app_name: str = "Luminalium"):
        self.app_name = app_name
        self.rendering_pipeline: Optional[RenderingPipeline] = None
        self.hwnd: Optional[int] = None
        self.width = 1920
        self.height = 1080

    def initialize_rendering(
        self,
        hwnd: int,
        width: int = 1920,
        height: int = 1080,
        force_api: Optional[RenderAPI] = None,
    ) -> bool:
        """
        初始化渲染管线

        Args:
            hwnd: 窗口句柄
            width: 窗口宽度
            height: 窗口高度
            force_api: 强制使用的API (None=自动选择)

        Returns:
            初始化是否成功
        """
        try:
            config = RenderConfig(
                api=force_api or RenderAPI.AUTO, width=width, height=height
            )

            self.rendering_pipeline = RenderingPipeline(config)

            if not self.rendering_pipeline.initialize(hwnd, width, height):
                print(f"[{self.app_name}] 渲染管线初始化失败")
                return False

            self.hwnd = hwnd
            self.width = width
            self.height = height

            print(
                f"[{self.app_name}] 渲染管线已初始化: {self.rendering_pipeline.backend_name}"
            )
            return True

        except Exception as e:
            print(f"[{self.app_name}] 初始化异常: {e}")
            return False

    def render_frame(self):
        """渲染一帧"""
        if self.rendering_pipeline:
            return self.rendering_pipeline.render_frame()
        return None

    def handle_resize(self, width: int, height: int):
        """处理窗口resize事件"""
        if self.rendering_pipeline:
            if self.rendering_pipeline.resize(width, height):
                self.width = width
                self.height = height
                print(f"[{self.app_name}] 窗口已调整为 {width}x{height}")

    def get_performance_stats(self):
        """获取性能统计"""
        if self.rendering_pipeline:
            return self.rendering_pipeline.get_stats()
        return {}

    def shutdown(self):
        """关闭应用"""
        if self.rendering_pipeline:
            self.rendering_pipeline.shutdown()
            print(f"[{self.app_name}] 渲染管线已关闭")


def integration_example():
    """
    集成示例代码
    展示如何在一个真实应用中使用渲染管线
    """

    # 1. 创建应用实例
    app = RenderedApplication("Kazuha - Windows DirectX Edition")

    # 2. 在主窗口创建后初始化渲染
    # hwnd = your_qt_window.winId()  # 从PySide6窗口获取HWND
    # if not app.initialize_rendering(hwnd, 1920, 1080):
    #     return False

    # 3. 在主渲染循环中调用
    # while app_running:
    #     frame_stats = app.render_frame()
    #     if frame_stats:
    #         # 可以使用frame_stats.fps, frame_stats.gpu_memory_mb等
    #         update_performance_display(frame_stats)
    #
    #     # 处理窗口事件
    #     for event in event_queue:
    #         if event.type == RESIZE:
    #             app.handle_resize(event.width, event.height)

    # 4. 应用退出时关闭
    # app.shutdown()

    print("✓ 集成示例已生成")
    print("参考notes详见文件注释")


# 实际的main.py集成注入点
def setup_directx_rendering_for_kazuha():
    """
    Kazuha主应用程序的DirectX集成点
    应该在main.py中的PySide6应用初始化后调用
    """

    # 伪代码示例（实际应该在main.py中实现）

    code_template = """
    # 在main.py中添加:
    
    from ppt_assistant.rendering.integration_layer import RenderingPipeline
    from ppt_assistant.rendering.config import RenderConfig, RenderAPI
    
    class KazuhaMainWindow(QMainWindow):
        def __init__(self):
            super().__init__()
            self.rendering_pipeline = None
            
        def setup_rendering(self):
            '''在showEvent()中调用'''
            hwnd = int(self.winId())
            config = RenderConfig(
                api=RenderAPI.AUTO,
                width=self.width(),
                height=self.height(),
                vsync_mode=VSyncMode.ADAPTIVE
            )
            self.rendering_pipeline = RenderingPipeline(config)
            
            if self.rendering_pipeline.initialize(hwnd, self.width(), self.height()):
                print("✓ DirectX rendering initialized")
                return True
            return False
        
        def on_render_frame(self):
            '''在主渲染循环中调用（比如timer或paint事件）'''
            if self.rendering_pipeline:
                stats = self.rendering_pipeline.render_frame()
                # 根据需要使用stats
                
        def resizeEvent(self, event):
            '''处理窗口resize'''
            super().resizeEvent(event)
            if self.rendering_pipeline:
                self.rendering_pipeline.resize(event.size().width(), event.size().height())
        
        def closeEvent(self, event):
            '''关闭应用'''
            if self.rendering_pipeline:
                self.rendering_pipeline.shutdown()
            super().closeEvent(event)
    """

    print("DirectX集成模板:")
    print(code_template)


if __name__ == "__main__":
    integration_example()
    setup_directx_rendering_for_kazuha()
