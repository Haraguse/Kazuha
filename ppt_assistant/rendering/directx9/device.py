"""
Direct3D 9 设备初始化与管理
核心：GPU设备、渲染状态、交换链、内存管理
支持软件顶点处理（VM环境兼容）
"""

from typing import Optional, Tuple
from ctypes import windll
import numpy as np
import time

from ..abstract import RenderBackend
from ..config import RenderConfig
from ..debug import PerformanceMonitor, GPUMemoryTracker

try:
    import comtypes
    from comtypes.gen import d3d9
    
    COMTYPES_AVAILABLE = True
except ImportError:
    COMTYPES_AVAILABLE = False

if not COMTYPES_AVAILABLE:
    print("⚠️  comtypes库未安装，Direct3D 9后端不可用")
    print("   运行: pip install comtypes")


class DirectX9Backend(RenderBackend):
    """Direct3D 9 渲染后端
    
    特点：
    - 兼容性强（支持Windows XP及以上）
    - 支持软件顶点处理（VM环境）
    - 简化API相比D3D12
    - 固定功能渲染管线
    """
    
    def __init__(self, config: RenderConfig = None):
        if not COMTYPES_AVAILABLE:
            raise RuntimeError("comtypes库不可用")
        
        self.config = config or RenderConfig()
        
        # D3D9对象
        self.d3d9: Optional[object] = None
        self.device: Optional[d3d9.IDirect3DDevice9] = None
        self.swap_chain: Optional[d3d9.IDirect3DSwapChain9] = None
        
        # 状态
        self._initialized = False
        self._frame_count = 0
        self._gpu_name = "Unknown"
        self._use_software_vp = False  # 虚拟机环境标志
        
        # 监控
        self.perf_monitor = PerformanceMonitor()
        self.memory_tracker = GPUMemoryTracker()
    
    def initialize(self, hwnd: int, width: int, height: int) -> bool:
        """初始化Direct3D9设备
        
        步骤：
        1. 创建D3D9对象
        2. 枚举显示适配器
        3. 创建设备（支持硬件和软件处理）
        4. 设置基本渲染状态
        5. 创建交换链
        
        Args:
            hwnd: 窗口句柄
            width: 宽度
            height: 高度
        
        Returns:
            成功True，失败False
        """
        try:
            self.config.width = width
            self.config.height = height
            
            print("\n[Direct3D9] 初始化中...")
            print(f"  分辨率: {width}x{height}")
            print(f"  目标FPS: {self.config.target_fps}")
            
            # 1. 创建D3D9对象
            try:
                self.d3d9 = d3d9.Direct3DCreate9(d3d9.D3D_SDK_VERSION)
            except Exception as e:
                print(f"[✗] D3D9对象创建失败: {e}")
                return False
            
            print("[✓] D3D9对象创建成功")
            
            # 2. 选择适配器
            adapter_idx = self._select_adapter()
            if adapter_idx < 0:
                print("[✗] 没有找到合适的显示适配器")
                return False
            
            # 3. 获取设备能力
            caps = self._get_device_caps(adapter_idx)
            if not caps:
                return False
            
            # 4. 创建设备
            try:
                device_created = self._create_device(adapter_idx, hwnd)
                if not device_created:
                    return False
            except Exception as e:
                print(f"[✗] 设备创建失败: {e}")
                # 尝试软件处理模式
                try:
                    print("[!] 尝试软件顶点处理模式...")
                    self._use_software_vp = True
                    device_created = self._create_device(adapter_idx, hwnd, software_vp=True)
                    if not device_created:
                        return False
                    print("[✓] 软件处理模式初始化成功（检测到虚拟机环境）")
                except Exception as e2:
                    print(f"[✗] 软件处理模式也失败: {e2}")
                    return False
            
            if self.config.debug:
                print(f"[✓] D3D9设备创建成功 (GPU: {self._gpu_name})")
            
            # 5. 设置渲染状态
            self._set_render_states()
            print("[✓] 渲染状态设置成功")
            
            self._initialized = True
            print("\n[✓] Direct3D9初始化完成！")
            print(f"  GPU模式: {'软件处理' if self._use_software_vp else '硬件加速'}")
            print(f"  {self.perf_monitor.get_summary()}")
            return True
        
        except Exception as e:
            print(f"[✗] Direct3D9初始化失败: {e}")
            if self.config.debug:
                import traceback
                traceback.print_exc()
            return False
    
    def _select_adapter(self) -> int:
        """选择显示适配器
        
        Returns:
            适配器索引，失败返回-1
        """
        try:
            adapter_count = self.d3d9.GetAdapterCount()
            if adapter_count == 0:
                print("[✗] 没有找到显示适配器")
                return -1
            
            print(f"  找到 {adapter_count} 个显示适配器")
            
            # 选择第一个适配器（通常是主显示器）
            adapter_idx = 0
            identifier = self.d3d9.GetAdapterIdentifier(adapter_idx, 0)
            
            self._gpu_name = identifier.Description if identifier else "Unknown GPU"
            print(f"  使用适配器: {self._gpu_name}")
            
            return adapter_idx
        
        except Exception as e:
            print(f"[!] 适配器选择失败: {e}")
            return -1
    
    def _get_device_caps(self, adapter_idx: int) -> Optional[object]:
        """获取设备能力
        
        Args:
            adapter_idx: 适配器索引
        
        Returns:
            设备能力对象或None
        """
        try:
            caps = d3d9.D3DCAPS9()
            self.d3d9.GetDeviceCaps(adapter_idx, d3d9.D3DDEVTYPE_HAL, caps)
            return caps
        except Exception as e:
            print(f"[!] 获取设备能力失败: {e}")
            return None
    
    def _create_device(self, adapter_idx: int, hwnd: int, software_vp: bool = False) -> bool:
        """创建D3D9设备
        
        Args:
            adapter_idx: 适配器索引
            hwnd: 窗口句柄
            software_vp: 是否使用软件顶点处理
        
        Returns:
            成功True，失败False
        """
        try:
            # 设置顶点处理标志
            vp_flag = d3d9.D3DCREATE_SOFTWARE_VERTEXPROCESSING if software_vp else d3d9.D3DCREATE_HARDWARE_VERTEXPROCESSING
            
            # 演示参数
            present_params = d3d9.D3DPRESENT_PARAMETERS()
            present_params.Windowed = True
            present_params.SwapEffect = d3d9.D3DSWAPEFFECT_DISCARD
            present_params.hDeviceWindow = hwnd
            present_params.BackBufferWidth = self.config.width
            present_params.BackBufferHeight = self.config.height
            present_params.BackBufferFormat = d3d9.D3DFMT_A8R8G8B8
            present_params.BackBufferCount = 1
            present_params.MultiSampleType = d3d9.D3DMULTISAMPLE_NONE
            present_params.MultiSampleQuality = 0
            present_params.EnableAutoDepthStencil = True
            present_params.AutoDepthStencilFormat = d3d9.D3DFMT_D24S8
            present_params.PresentationInterval = d3d9.D3DPRESENT_INTERVAL_DEFAULT
            
            # 创建设备
            self.device = self.d3d9.CreateDevice(
                adapter_idx,
                d3d9.D3DDEVTYPE_HAL,
                hwnd,
                vp_flag,
                present_params
            )
            
            return True
        
        except Exception as e:
            print(f"[!] 设备创建失败: {e}")
            return False
    
    def _set_render_states(self) -> None:
        """设置基本渲染状态"""
        try:
            if not self.device:
                return
            
            # 启用深度测试
            self.device.SetRenderState(d3d9.D3DRS_ZENABLE, d3d9.D3DZB_TRUE)
            
            # 设置照光
            self.device.SetRenderState(d3d9.D3DRS_LIGHTING, False)
            
            # 设置着色模式
            self.device.SetRenderState(d3d9.D3DRS_SHADEMODE, d3d9.D3DSHADE_GOURAUD)
            
            # 设置采样模式
            self.device.SetSamplerState(0, d3d9.D3DSAMP_MAGFILTER, d3d9.D3DTEXF_LINEAR)
            self.device.SetSamplerState(0, d3d9.D3DSAMP_MINFILTER, d3d9.D3DTEXF_LINEAR)
            
        except Exception as e:
            print(f"[!] 渲染状态设置失败: {e}")
    
    def shutdown(self) -> None:
        """清理所有资源"""
        if not self._initialized:
            return
        
        try:
            if self.device:
                self.device.Release()
                self.device = None
            
            if self.d3d9:
                self.d3d9.Release()
                self.d3d9 = None
            
            print("[✓] Direct3D9资源已清理")
        
        except Exception as e:
            print(f"[!] 清理过程出错: {e}")
        
        finally:
            self._initialized = False
    
    def begin_frame(self) -> None:
        """开始新的渲染帧"""
        self.perf_monitor.frame_begin()
        
        if not self._initialized or not self.device:
            return
        
        try:
            self.device.BeginScene()
        except Exception as e:
            print(f"[!] 开始帧失败: {e}")
    
    def end_frame(self) -> None:
        """结束帧，提交渲染命令"""
        if not self._initialized or not self.device:
            return
        
        try:
            self.device.EndScene()
            self.device.Present(None, None, None, None)
            
            self._frame_count += 1
            self.perf_monitor.frame_end()
        
        except Exception as e:
            print(f"[!] 帧提交失败: {e}")
    
    def clear(self, color: Tuple[float, float, float, float]) -> None:
        """清屏为指定颜色
        
        Args:
            color: RGBA颜色 (0.0-1.0)
        """
        if not self._initialized or not self.device:
            return
        
        try:
            # 转换RGBA浮点数到DWORD
            r, g, b, a = [int(c * 255) for c in color]
            d3d_color = (a << 24) | (r << 16) | (g << 8) | b
            
            self.device.Clear(
                0,
                None,
                d3d9.D3DCLEAR_TARGET | d3d9.D3DCLEAR_ZBUFFER,
                d3d_color,
                1.0,
                0
            )
        
        except Exception as e:
            print(f"[!] 清屏失败: {e}")
    
    def draw_rect(
        self, x: int, y: int, w: int, h: int, color: Tuple[float, float, float, float]
    ) -> None:
        """绘制矩形
        
        Args:
            x, y: 位置
            w, h: 宽高
            color: RGBA颜色
        """
        # TODO: 实现矩形绘制（需要定义顶点缓冲区）
        pass
    
    def draw_text(
        self,
        text: str,
        x: int,
        y: int,
        color: Tuple[float, float, float, float],
        size: int = 12,
        font: str = "Arial",
    ) -> None:
        """绘制文字
        
        Args:
            text: 文本内容
            x, y: 位置
            color: RGBA颜色
            size: 字号
            font: 字体名称
        """
        # TODO: 实现文字绘制（需要DirectDraw或D3D文本集成）
        pass
    
    def draw_stroke(
        self,
        points: np.ndarray,
        color: Tuple[float, float, float, float],
        width: float = 2.0,
        pressure: Optional[np.ndarray] = None,
    ) -> None:
        """GPU加速笔画绘制
        
        Args:
            points: 笔画点数组，shape (N, 2) 或 (N, 3)
            color: RGBA颜色
            width: 笔宽
            pressure: 可选的压力数据
        """
        # TODO: 实现笔画渲染
        pass
    
    def resize(self, width: int, height: int) -> None:
        """调整渲染目标大小
        
        Args:
            width: 新宽度
            height: 新高度
        """
        if not self._initialized or not self.device:
            return
        
        try:
            self.config.width = width
            self.config.height = height
            
            # 重新创建演示参数
            present_params = d3d9.D3DPRESENT_PARAMETERS()
            present_params.Windowed = True
            present_params.SwapEffect = d3d9.D3DSWAPEFFECT_DISCARD
            present_params.BackBufferWidth = width
            present_params.BackBufferHeight = height
            present_params.BackBufferFormat = d3d9.D3DFMT_A8R8G8B8
            present_params.EnableAutoDepthStencil = True
            present_params.AutoDepthStencilFormat = d3d9.D3DFMT_D24S8
            
            # 重置设备
            self.device.Reset(present_params)
            
            print(f"[✓] 调整渲染大小到 {width}x{height}")
        
        except Exception as e:
            print(f"[!] 调整大小失败: {e}")
    
    @property
    def fps(self) -> float:
        """获取当前FPS"""
        return self.perf_monitor.avg_fps
    
    @property
    def gpu_name(self) -> str:
        """获取GPU名称"""
        return self._gpu_name
    
    @property
    def api_version(self) -> str:
        """获取API版本"""
        return "Direct3D 9.0c" + (" (Software VP)" if self._use_software_vp else " (Hardware)")
