"""
渲染后端工厂与管理
用于自动选择最优后端和统一接口
"""

from typing import Optional
from .abstract import RenderBackend
from .config import RenderAPI, RenderConfig
from .directx12.device import DirectX12Backend
from .opengl.context import OpenGLBackend


class RenderBackendFactory:
    """渲染后端工厂"""

    @staticmethod
    def create(config: Optional[RenderConfig] = None) -> RenderBackend:
        """创建最优的渲染后端

        优先级：
        1. DirectX 12 (Windows性能最优)
        2. OpenGL 4.6 (跨平台备选)

        Args:
            config: 渲染配置

        Returns:
            RenderBackend实例

        Raises:
            RuntimeError: 没有可用的后端
        """
        config = config or RenderConfig()

        # 明确指定API
        if config.api == RenderAPI.DIRECTX12:
            return DirectX12Backend(config)
        elif config.api == RenderAPI.OPENGL:
            return OpenGLBackend(config)

        # 自动选择 (AUTO模式)
        # Windows优先DirectX
        try:
            backend = DirectX12Backend(config)
            print("[✓] 已选择DirectX 12后端")
            return backend
        except Exception:
            pass

        # 降级到OpenGL
        try:
            backend = OpenGLBackend(config)
            print("[✓] 已选择OpenGL后端")
            return backend
        except Exception:
            pass

        raise RuntimeError(
            "没有可用的渲染后端请安装 pip install comtypes 用于DirectX或 pip install PyOpenGL"
        )


class RenderManager:
    """统一的渲染管理器

    用途：
    - 管理渲染生命周期
    - 提供统一接口
    - 性能监控
    """

    def __init__(self, config: Optional[RenderConfig] = None):
        self.config = config or RenderConfig()
        self.backend: Optional[RenderBackend] = None

    def initialize(self, hwnd: int, width: int, height: int) -> bool:
        """初始化渲染系统

        Args:
            hwnd: 窗口句柄
            width: 宽度
            height: 高度

        Returns:
            初始化是否成功
        """
        try:
            self.backend = RenderBackendFactory.create(self.config)
            success = self.backend.initialize(hwnd, width, height)

            if success:
                print("\n[✓] 渲染系统初始化完成")
                print(f"  后端: {self.backend.api_version}")
                print(f"  GPU: {self.backend.gpu_name}")

            return success
        except Exception as e:
            print(f"[✗] 渲染系统初始化失败: {e}")
            return False

    def shutdown(self) -> None:
        """关闭渲染系统"""
        if self.backend:
            self.backend.shutdown()
            self.backend = None

    def begin_frame(self) -> None:
        """开始新帧"""
        if self.backend:
            self.backend.begin_frame()

    def end_frame(self) -> None:
        """结束帧"""
        if self.backend:
            self.backend.end_frame()

    @property
    def fps(self) -> float:
        """获取当前FPS"""
        return self.backend.fps if self.backend else 0

    @property
    def performance_summary(self) -> str:
        """获取性能摘要"""
        if not self.backend:
            return "未初始化"
        return self.backend.perf_monitor.get_summary()

    # ============ High-Level UI Drawing ============

    def draw_rect(
        self,
        x: float,
        y: float,
        width: float,
        height: float,
        color_tuple: tuple,
        filled: bool = True,
    ) -> bool:
        """Draw rectangle.

        Args:
            x: Position X
            y: Position Y
            width: Rectangle width
            height: Rectangle height
            color_tuple: RGBA color (r, g, b, a) 0-1.0
            filled: Whether to fill

        Returns:
            Success flag
        """
        if not self.backend or not hasattr(self.backend, "draw_rect"):
            return False
        return self.backend.draw_rect(x, y, width, height, color_tuple, filled)

    def draw_circle(
        self,
        cx: float,
        cy: float,
        radius: float,
        color_tuple: tuple,
        filled: bool = True,
        segments: int = 32,
    ) -> bool:
        """Draw circle."""
        if not self.backend or not hasattr(self.backend, "draw_circle"):
            return False
        return self.backend.draw_circle(cx, cy, radius, color_tuple, filled, segments)

    def draw_line(
        self,
        x1: float,
        y1: float,
        x2: float,
        y2: float,
        color_tuple: tuple,
        width: float = 1.0,
    ) -> bool:
        """Draw line."""
        if not self.backend or not hasattr(self.backend, "draw_line"):
            return False
        return self.backend.draw_line(x1, y1, x2, y2, color_tuple, width)

    def draw_text(
        self,
        text: str,
        x: float,
        y: float,
        color_tuple: tuple,
        font_size: int = 12,
        font_name: str = "Arial",
    ) -> bool:
        """Draw text."""
        if not self.backend or not hasattr(self.backend, "draw_text"):
            return False
        return self.backend.draw_text(text, x, y, color_tuple, font_size, font_name)

    def draw_stroke(self, points, color_tuple: tuple, width: float = 1.0) -> bool:
        """Draw stroke (pen path)."""
        if not self.backend or not hasattr(self.backend, "draw_stroke"):
            return False
        return self.backend.draw_stroke(points, color_tuple, width)

    def set_viewport(self, x: float, y: float, width: float, height: float) -> bool:
        """Set viewport."""
        if not self.backend or not hasattr(self.backend, "set_viewport"):
            return False
        return self.backend.set_viewport(x, y, width, height)

    def set_scissor_rect(self, left: int, top: int, right: int, bottom: int) -> bool:
        """Set scissor rectangle."""
        if not self.backend or not hasattr(self.backend, "set_scissor_rect"):
            return False
        return self.backend.set_scissor_rect(left, top, right, bottom)

    def flush(self) -> bool:
        """Flush GPU commands."""
        if not self.backend or not hasattr(self.backend, "flush"):
            return False
        return self.backend.flush()

    def get_device_info(self) -> dict:
        """Get device information."""
        if not self.backend:
            return {}

        info = {
            "gpu_name": getattr(self.backend, "gpu_name", "Unknown"),
            "api_version": getattr(self.backend, "api_version", "Unknown"),
        }

        if hasattr(self.backend, "get_stats"):
            stats = self.backend.get_stats()
            info.update(
                {
                    "draw_calls": stats.draw_calls,
                    "gpu_memory_used": stats.gpu_memory_used,
                    "buffer_count": stats.buffer_count,
                    "texture_count": stats.texture_count,
                }
            )

        return info
