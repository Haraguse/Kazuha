"""
应用级集成层 - DirectX/OpenGL渲染管线与主应用的接口
提供简化的API供PySide6应用使用
"""

from typing import Optional, Tuple, Callable, Any, Dict
from dataclasses import dataclass
import threading
import time

from .manager import RenderManager
from .config import RenderConfig, RenderAPI, VSyncMode
from .debug import PerformanceMonitor, GPUMemoryTracker


@dataclass
class FrameStats:
    """单帧统计数据"""

    frame_number: int
    fps: float
    gpu_memory_mb: float
    render_time_ms: float
    present_time_ms: float
    total_time_ms: float


class RenderingPipeline:
    """
    高级渲染管线 - 为PySide6应用提供统一接口
    管理DirectX/OpenGL后端，处理多线程同步
    """

    def __init__(self, config: Optional[RenderConfig] = None):
        self.config = config or RenderConfig(api=RenderAPI.AUTO)
        self.manager = None
        self.monitor = PerformanceMonitor()
        self.memory_tracker = GPUMemoryTracker()
        self._lock = threading.RLock()
        self._running = False
        self._frame_callbacks: Dict[str, Callable] = {}
        self._current_frame = 0

    def initialize(self, hwnd: int, width: int, height: int) -> bool:
        """初始化渲染管线"""
        with self._lock:
            try:
                self.manager = RenderManager(self.config)
                if not self.manager.initialize(hwnd, width, height):
                    return False

                self._running = True
                self.monitor.reset()
                return True
            except Exception as e:
                print(f"[RenderPipeline] 初始化失败: {e}")
                return False

    def shutdown(self):
        """关闭渲染管线"""
        with self._lock:
            self._running = False
            if self.manager:
                self.manager.shutdown()
                self.manager = None

    def render_frame(self) -> FrameStats:
        """
        渲染单帧

        Returns:
            FrameStats: 帧统计信息
        """
        if not self._running or not self.manager:
            return FrameStats(0, 0, 0, 0, 0, 0)

        with self._lock:
            start_time = time.perf_counter()
            self.monitor.frame_begin()

            try:
                # 调用帧开始回调
                if "on_frame_begin" in self._frame_callbacks:
                    self._frame_callbacks["on_frame_begin"]()

                # 开始渲染
                if not self.manager.begin_frame():
                    return FrameStats(self._current_frame, 0, 0, 0, 0, 0)

                render_start = time.perf_counter()

                # 调用场景渲染回调
                if "on_render" in self._frame_callbacks:
                    self._frame_callbacks["on_render"](self.manager.backend)

                render_time = (time.perf_counter() - render_start) * 1000

                # 结束渲染
                present_start = time.perf_counter()
                if not self.manager.end_frame():
                    return FrameStats(self._current_frame, 0, 0, 0, 0, 0)

                present_time = (time.perf_counter() - present_start) * 1000

                # 调用帧结束回调
                if "on_frame_end" in self._frame_callbacks:
                    self._frame_callbacks["on_frame_end"]()

                self.monitor.frame_end()

                total_time = (time.perf_counter() - start_time) * 1000
                gpu_mem = self.memory_tracker.get_total_allocated_mb()

                stats = FrameStats(
                    frame_number=self._current_frame,
                    fps=self.monitor.avg_fps,
                    gpu_memory_mb=gpu_mem,
                    render_time_ms=render_time,
                    present_time_ms=present_time,
                    total_time_ms=total_time,
                )

                self._current_frame += 1
                return stats

            except Exception as e:
                print(f"[RenderPipeline] 渲染错误: {e}")
                return FrameStats(self._current_frame, 0, 0, 0, 0, 0)

    def resize(self, width: int, height: int) -> bool:
        """调整渲染目标大小"""
        if not self._running or not self.manager:
            return False

        with self._lock:
            return self.manager.resize(width, height)

    def register_callback(self, event: str, callback: Callable) -> None:
        """
        注册渲染事件回调

        支持的事件:
        - on_frame_begin: 帧开始前
        - on_render: 场景渲染 (接收backend作为参数)
        - on_frame_end: 帧结束后
        """
        with self._lock:
            self._frame_callbacks[event] = callback

    def unregister_callback(self, event: str) -> None:
        """注销回调"""
        with self._lock:
            self._frame_callbacks.pop(event, None)

    def get_stats(self) -> Dict[str, Any]:
        """获取性能统计信息"""
        with self._lock:
            if not self.manager:
                return {}

            return {
                "backend_api": str(self.manager.backend.api_name),
                "gpu_name": self.manager.backend.gpu_name,
                "api_version": self.manager.backend.api_version,
                "current_fps": self.monitor.avg_fps,
                "min_fps": self.monitor.min_fps,
                "max_fps": self.monitor.max_fps,
                "p95_fps": self.monitor.p95_fps,
                "frame_count": self._current_frame,
                "gpu_memory_mb": self.memory_tracker.get_total_allocated_mb(),
            }

    @property
    def backend_name(self) -> str:
        """获取后端API名称"""
        if self.manager and self.manager.backend:
            return self.manager.backend.api_name
        return "None"

    @property
    def is_running(self) -> bool:
        """检查渲染管线是否正在运行"""
        return self._running

    @property
    def avg_fps(self) -> float:
        """获取平均FPS"""
        return self.monitor.avg_fps


class RenderingContext:
    """
    便捷的渲染上下文 - 简化应用集成

    示例:
        ctx = RenderingContext(hwnd=my_hwnd, width=1920, height=1080)
        with ctx.frame():
            ctx.draw_rect(100, 100, 200, 200, (255, 0, 0, 255))
            ctx.draw_text(150, 150, "Hello", font_name="Arial", font_size=24)
    """

    def __init__(
        self,
        hwnd: int,
        width: int,
        height: int,
        api: RenderAPI = RenderAPI.AUTO,
        vsync: bool = True,
    ):
        config = RenderConfig(
            api=api,
            width=width,
            height=height,
            vsync_mode=VSyncMode.ADAPTIVE if vsync else VSyncMode.DISABLED,
        )
        self.pipeline = RenderingPipeline(config)
        self.hwnd = hwnd
        self.width = width
        self.height = height
        self._initialized = False
        self._frame_current = None

    def __enter__(self):
        """上下文管理器入口"""
        if not self._initialized:
            if not self.pipeline.initialize(self.hwnd, self.width, self.height):
                raise RuntimeError("Failed to initialize rendering pipeline")
            self._initialized = True
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """上下文管理器退出"""
        self.shutdown()

    def frame(self):
        """帧上下文管理器"""
        return _FrameContext(self)

    def render_frame(self) -> FrameStats:
        """渲染单帧"""
        return self.pipeline.render_frame()

    def draw_rect(
        self,
        x: float,
        y: float,
        width: float,
        height: float,
        color: Tuple[int, int, int, int],
        filled: bool = True,
    ) -> None:
        """绘制矩形"""
        if self._frame_current and hasattr(self._frame_current, "draw_rect"):
            self._frame_current.draw_rect(x, y, width, height, color, filled)

    def draw_text(
        self,
        x: float,
        y: float,
        text: str,
        font_name: str = "Arial",
        font_size: int = 14,
        color: Tuple[int, int, int, int] = (255, 255, 255, 255),
    ) -> None:
        """绘制文本"""
        if self._frame_current and hasattr(self._frame_current, "draw_text"):
            self._frame_current.draw_text(x, y, text, font_name, font_size, color)

    def draw_stroke(
        self, points: list, width: float, color: Tuple[int, int, int, int]
    ) -> None:
        """绘制笔画"""
        if self._frame_current and hasattr(self._frame_current, "draw_stroke"):
            self._frame_current.draw_stroke(points, width, color)

    def resize(self, width: int, height: int) -> None:
        """调整窗口大小"""
        if self.pipeline.resize(width, height):
            self.width = width
            self.height = height

    def get_stats(self) -> Dict[str, Any]:
        """获取统计信息"""
        return self.pipeline.get_stats()

    def shutdown(self):
        """关闭上下文"""
        if self._initialized:
            self.pipeline.shutdown()
            self._initialized = False


class _FrameContext:
    """内部帧上下文"""

    def __init__(self, ctx: RenderingContext):
        self.ctx = ctx

    def __enter__(self):
        if self.ctx.pipeline.manager:
            self.ctx.pipeline.manager.begin_frame()
            self.ctx._frame_current = self.ctx.pipeline.manager.backend
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        if self.ctx.pipeline.manager:
            self.ctx.pipeline.manager.end_frame()
            self.ctx._frame_current = None


# 便捷函数 - 全局渲染上下文
_global_pipeline: Optional[RenderingPipeline] = None


def init_rendering(
    hwnd: int, width: int, height: int, api: RenderAPI = RenderAPI.AUTO
) -> bool:
    """初始化全局渲染管线"""
    global _global_pipeline
    _global_pipeline = RenderingPipeline(
        RenderConfig(api=api, width=width, height=height)
    )
    return _global_pipeline.initialize(hwnd, width, height)


def shutdown_rendering():
    """关闭全局渲染管线"""
    global _global_pipeline
    if _global_pipeline:
        _global_pipeline.shutdown()
        _global_pipeline = None


def render_frame() -> FrameStats:
    """渲染全局帧"""
    global _global_pipeline
    if _global_pipeline:
        return _global_pipeline.render_frame()
    return FrameStats(0, 0, 0, 0, 0, 0)


def get_rendering_stats() -> Dict[str, Any]:
    """获取全局渲染统计"""
    global _global_pipeline
    if _global_pipeline:
        return _global_pipeline.get_stats()
    return {}
