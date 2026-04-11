"""
渲染调试工具
性能分析、内存追踪、GPU状态监控
"""

import time
from collections import deque
from typing import Optional, Dict
from dataclasses import dataclass
import statistics


@dataclass
class FrameStats:
    """单帧统计"""
    frame_id: int
    timestamp: float
    duration_ms: float
    gpu_duration_ms: float
    vertex_count: int
    draw_calls: int
    
    @property
    def fps(self) -> float:
        return 1000.0 / self.duration_ms if self.duration_ms > 0 else 0


class PerformanceMonitor:
    """性能监视器"""
    
    def __init__(self, window_size: int = 60):
        """
        Args:
            window_size: 统计窗口大小（帧数）
        """
        self.frame_stats: deque = deque(maxlen=window_size)
        self.frame_times: deque = deque(maxlen=window_size)
        self.current_frame = 0
        self.frame_start_time = 0
        
    def frame_begin(self) -> None:
        """标记帧开始"""
        self.frame_start_time = time.perf_counter()
    
    def frame_end(self, gpu_duration_ms: float = 0) -> Optional[FrameStats]:
        """标记帧结束
        
        Args:
            gpu_duration_ms: GPU处理时间（如可得）
            
        Returns:
            框架统计信息
        """
        elapsed = (time.perf_counter() - self.frame_start_time) * 1000
        self.frame_times.append(elapsed)
        
        stats = FrameStats(
            frame_id=self.current_frame,
            timestamp=time.time(),
            duration_ms=elapsed,
            gpu_duration_ms=gpu_duration_ms,
            vertex_count=0,
            draw_calls=0
        )
        self.frame_stats.append(stats)
        self.current_frame += 1
        
        return stats
    
    @property
    def avg_fps(self) -> float:
        """平均FPS"""
        if not self.frame_times or len(self.frame_times) < 2:
            return 0
        avg_time = statistics.mean(self.frame_times)
        return 1000.0 / avg_time if avg_time > 0 else 0
    
    @property
    def min_fps(self) -> float:
        """最小FPS"""
        if not self.frame_times:
            return 0
        max_time = max(self.frame_times)
        return 1000.0 / max_time if max_time > 0 else 0
    
    @property
    def max_fps(self) -> float:
        """最大FPS"""
        if not self.frame_times:
            return 0
        min_time = min(self.frame_times)
        return 1000.0 / min_time if min_time > 0 else 0
    
    @property
    def p95_fps(self) -> float:
        """P95 FPS（95百分位数）"""
        if len(self.frame_times) < 2:
            return 0
        sorted_times = sorted(self.frame_times)
        idx = int(len(sorted_times) * 0.05)  # 最慢5%
        slowest_time = sorted_times[idx]
        return 1000.0 / slowest_time if slowest_time > 0 else 0
    
    def get_summary(self) -> str:
        """获取性能摘要"""
        return (
            f"FPS: {self.avg_fps:.1f} avg (min {self.min_fps:.1f}, "
            f"max {self.max_fps:.1f}, p95 {self.p95_fps:.1f})"
        )


class GPUMemoryTracker:
    """GPU内存追踪"""
    
    def __init__(self):
        self.allocations: Dict[str, int] = {}
        self.total_allocated = 0
    
    def track_allocation(self, name: str, size_bytes: int) -> None:
        """记录内存分配"""
        self.allocations[name] = size_bytes
        self.total_allocated += size_bytes
    
    def track_deallocation(self, name: str) -> None:
        """记录内存释放"""
        if name in self.allocations:
            self.total_allocated -= self.allocations[name]
            del self.allocations[name]
    
    @property
    def summary(self) -> str:
        """内存使用摘要"""
        mb = self.total_allocated / 1024 / 1024
        return f"GPU Memory: {mb:.2f} MB ({len(self.allocations)} allocations)"
