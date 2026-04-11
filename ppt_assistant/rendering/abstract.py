"""
渲染后端抽象接口
定义统一的渲染API，支持DirectX12和OpenGL
"""

from abc import ABC, abstractmethod
from typing import Tuple, Optional, List
import numpy as np


class RenderBackend(ABC):
    """渲染后端抽象接口"""
    
    @abstractmethod
    def initialize(self, hwnd: int, width: int, height: int) -> bool:
        """初始化渲染设备
        
        Args:
            hwnd: 窗口句柄
            width: 窗口宽度
            height: 窗口高度
            
        Returns:
            初始化是否成功
        """
        pass
    
    @abstractmethod
    def shutdown(self) -> None:
        """清理资源，释放GPU内存"""
        pass
    
    @abstractmethod
    def begin_frame(self) -> None:
        """开始渲染一帧"""
        pass
    
    @abstractmethod
    def end_frame(self) -> None:
        """结束渲染一帧，提交命令进行渲染"""
        pass
    
    @abstractmethod
    def clear(self, color: Tuple[float, float, float, float]) -> None:
        """清屏
        
        Args:
            color: RGBA颜色 (0.0-1.0)
        """
        pass
    
    @abstractmethod
    def draw_rect(self, x: int, y: int, w: int, h: int, 
                  color: Tuple[float, float, float, float]) -> None:
        """绘制矩形
        
        Args:
            x, y: 位置
            w, h: 宽高
            color: RGBA颜色
        """
        pass
    
    @abstractmethod
    def draw_text(self, text: str, x: int, y: int, 
                  color: Tuple[float, float, float, float], 
                  size: int = 12, font: str = "Arial") -> None:
        """绘制文字
        
        Args:
            text: 文本内容
            x, y: 位置
            color: RGBA颜色
            size: 字号
            font: 字体名称
        """
        pass
    
    @abstractmethod
    def draw_stroke(self, points: np.ndarray, 
                    color: Tuple[float, float, float, float],
                    width: float = 2.0, 
                    pressure: Optional[np.ndarray] = None) -> None:
        """绘制笔画（高性能GPU加速，用于Overlay）
        
        Args:
            points: 笔画点数组，shape (N, 2) 或 (N, 3)，坐标为像素
            color: RGBA颜色
            width: 笔宽
            pressure: 可选的压力数据，shape (N,)，影响笔宽
        """
        pass
    
    @abstractmethod
    def resize(self, width: int, height: int) -> None:
        """重新调整渲染目标大小"""
        pass

    @property
    @abstractmethod
    def fps(self) -> float:
        """获取当前FPS"""
        pass
    
    @property
    @abstractmethod
    def gpu_name(self) -> str:
        """获取GPU名称"""
        pass
    
    @property
    @abstractmethod
    def api_version(self) -> str:
        """获取API版本（DirectX 12/OpenGL 4.6等）"""
        pass
