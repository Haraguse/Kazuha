"""
渲染配置管理
"""

from enum import Enum
from dataclasses import dataclass


class RenderAPI(Enum):
    """支持的渲染API"""

    DIRECTX12 = "directx12"
    OPENGL = "opengl"
    AUTO = "auto"  # 自动选择最佳


class VSyncMode(Enum):
    """垂直同步模式"""

    OFF = 0  # 无同步，无撕裂风险
    ON = 1  # 60Hz垂直同步
    ADAPTIVE = 2  # 自适应VSync（推荐）


@dataclass
class RenderConfig:
    """渲染配置"""

    # API选择
    api: RenderAPI = RenderAPI.AUTO

    # 显示设置
    width: int = 1280
    height: int = 720
    vsync: VSyncMode = VSyncMode.ADAPTIVE
    target_fps: int = 144

    # 性能优化
    multi_sampling: int = 1  # MSAA倍数 (1,2,4,8)
    max_frame_latency: int = 1  # 最大帧延迟

    # 调试
    debug: bool = False
    enable_gpu_validation: bool = False
    profile_fps: bool = True

    # Overlay特有
    overlay_precision: str = "high"  # low/medium/high

    def __post_init__(self):
        """验证配置"""
        if self.width <= 0 or self.height <= 0:
            raise ValueError("分辨率必须 > 0")
        if self.multi_sampling not in [1, 2, 4, 8]:
            raise ValueError("MSAA只支持 1,2,4,8")
        if self.target_fps <= 0 or self.target_fps > 240:
            raise ValueError("目标FPS必须在 1-240之间")
