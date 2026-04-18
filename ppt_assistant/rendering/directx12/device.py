"""
DirectX 12 设备初始化与管理
核心：GPU设备、命令队列、交换链、内存管理
"""

from typing import Optional, Tuple
from ctypes import windll
import numpy as np

from ..abstract import RenderBackend
from ..config import RenderConfig
from ..debug import PerformanceMonitor, GPUMemoryTracker

try:
    import comtypes
    from comtypes.gen import d3d12, dxgi

    COMTYPES_AVAILABLE = True
except ImportError:
    COMTYPES_AVAILABLE = False

if not COMTYPES_AVAILABLE:
    print("⚠️  comtypes库未安装，DirectX12后端不可用")
    print("   运行: pip install comtypes")


class DirectX12Backend(RenderBackend):
    """DirectX 12 渲染后端

    特点：
    - 低级API直接控制GPU
    - 显式资源管理
    - 多线程友好的命令列表
    - 极低CPU开销
    """

    def __init__(self, config: RenderConfig = None):
        if not COMTYPES_AVAILABLE:
            raise RuntimeError("comtypes库不可用")

        self.config = config or RenderConfig()

        # D3D12对象
        self.device: Optional[d3d12.ID3D12Device] = None
        self.command_queue: Optional[d3d12.ID3D12CommandQueue] = None
        self.swap_chain: Optional[dxgi.IDXGISwapChain3] = None
        self.command_allocator: Optional[d3d12.ID3D12CommandAllocator] = None
        self.command_list: Optional[d3d12.ID3D12GraphicsCommandList] = None
        self.frame_fence = None
        self.fence_event = None

        # 状态
        self._initialized = False
        self._frame_count = 0
        self._gpu_name = "Unknown"

        # 监控
        self.perf_monitor = PerformanceMonitor()
        self.memory_tracker = GPUMemoryTracker()

    def initialize(self, hwnd: int, width: int, height: int) -> bool:
        """初始化DirectX12设备

        步骤：
        1. 启用调试层（可选）
        2. 创建DXGI工厂和适配器
        3. 创建D3D12设备
        4. 创建命令队列
        5. 创建交换链和RTV堆
        6. 创建围栏用于GPU同步

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

            print("\n[DirectX12] 初始化中...")
            print(f"  分辨率: {width}x{height}")
            print(f"  目标FPS: {self.config.target_fps}")

            # 1. 启用调试层
            if self.config.debug:
                self._enable_debug_layer()

            # 2. 创建DXGI工厂
            factory = dxgi.CreateDXGIFactory1()
            print("[✓] DXGI工厂创建成功")

            # 3. 选择GPU适配器
            adapter = self._select_gpu(factory)
            if not adapter:
                print("[✗] 没有找到DirectX 12兼容的GPU")
                return False

            # 4. 创建D3D12设备
            self.device = d3d12.D3D12CreateDevice(adapter, d3d12.D3D_FEATURE_LEVEL_12_1)

            if self.config.debug:
                try:
                    info_queue = self.device.QueryInterface(d3d12.ID3D12InfoQueue)
                    info_queue.SetBreakOnSeverity(
                        d3d12.D3D12_MESSAGE_SEVERITY_ERROR, True
                    )
                except:
                    pass

            print(f"[✓] D3D12设备创建成功 (GPU: {self._gpu_name})")

            # 5. 创建命令队列
            self._create_command_queue()
            print("[✓] 命令队列创建成功")

            # 6. 创建交换链
            self._create_swap_chain(factory, hwnd)
            print("[✓] 交换链创建成功")

            # 7. 创建命令分配器和列表
            self.command_allocator = self.device.CreateCommandAllocator(
                d3d12.D3D12_COMMAND_LIST_TYPE_DIRECT
            )
            self.command_list = self.device.CreateCommandList(
                0,
                d3d12.D3D12_COMMAND_LIST_TYPE_DIRECT,
                self.command_allocator,
                None,  # PSO先设未设置
            )
            self.command_list.Close()
            print("[✓] 命令列表创建成功")

            # 8. 创建围栏用于GPU-CPU同步
            self.frame_fence = self.device.CreateFence(0, d3d12.D3D12_FENCE_FLAG_NONE)
            self.fence_event = windll.kernel32.CreateEventW(None, False, False, None)
            print("[✓] 同步围栏创建成功")

            self._initialized = True
            print("\n[✓] DirectX12初始化完成！")
            print(f"  {self.perf_monitor.get_summary()}")
            return True

        except Exception as e:
            print(f"[✗] DirectX12初始化失败: {e}")
            if self.config.debug:
                import traceback

                traceback.print_exc()
            return False

    def _enable_debug_layer(self) -> None:
        """启用Direct3D调试层"""
        try:
            debug = d3d12.D3D12GetDebugInterface(d3d12.ID3D12Debug)
            debug.EnableDebugLayer()
            print("[✓] GPU验证层已启用")
        except Exception as e:
            print(f"[!] 调试层启用失败（发布版本正常）: {e}")

    def _select_gpu(self, factory) -> Optional[object]:
        """选择最佳GPU（优先独显）

        Returns:
            IDXGIAdapter1对象或None
        """
        best_adapter = None
        best_vram = 0
        adapter_count = 0

        i = 0
        while True:
            try:
                adapter = factory.EnumAdapters1(i)
                desc = adapter.GetDesc1()

                # 计算总显存
                vram = (
                    desc.SharedSystemMemory
                    + desc.DedicatedSystemMemory
                    + desc.DedicatedVideoMemory
                ) / (1024 * 1024 * 1024)

                adapter_name = desc.Description
                print(f"  GPU {i}: {adapter_name} ({vram:.1f} GB)")

                # 优先选择独显（DedicatedVideoMemory > 0）
                if desc.DedicatedVideoMemory > 0 and vram > best_vram:
                    best_vram = vram
                    best_adapter = adapter
                    self._gpu_name = adapter_name

                adapter_count += 1
                i += 1
            except:
                break

        if best_adapter is None and adapter_count > 0:
            # 没有独显，选择第一个适配器
            try:
                best_adapter = factory.EnumAdapters1(0)
                desc = best_adapter.GetDesc1()
                self._gpu_name = desc.Description
            except:
                pass

        return best_adapter

    def _create_command_queue(self) -> None:
        """创建命令队列"""
        desc = d3d12.D3D12_COMMAND_QUEUE_DESC()
        desc.Type = d3d12.D3D12_COMMAND_LIST_TYPE_DIRECT
        desc.Priority = d3d12.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL
        desc.Flags = d3d12.D3D12_COMMAND_QUEUE_FLAG_NONE

        self.command_queue = self.device.CreateCommandQueue(desc)

    def _create_swap_chain(self, factory, hwnd: int) -> None:
        """创建交换链"""
        desc = dxgi.DXGI_SWAP_CHAIN_DESC()
        desc.BufferDesc.Width = self.config.width
        desc.BufferDesc.Height = self.config.height
        desc.BufferDesc.Format = dxgi.DXGI_FORMAT_R8G8B8A8_UNORM
        desc.BufferDesc.RefreshRate.Numerator = self.config.target_fps
        desc.BufferDesc.RefreshRate.Denominator = 1

        desc.SampleDesc.Count = self.config.multi_sampling
        desc.SampleDesc.Quality = 0

        desc.BufferCount = 2
        desc.BufferUsage = dxgi.DXGI_USAGE_RENDER_TARGET_OUTPUT
        desc.OutputWindow = hwnd
        desc.Windowed = True

        # 交换效果：FLIP_DISCARD性能最好，但需要DXGI 1.4
        desc.SwapEffect = dxgi.DXGI_SWAP_EFFECT_FLIP_DISCARD
        desc.Flags = dxgi.DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH

        swap_chain = factory.CreateSwapChain(self.command_queue, desc)
        self.swap_chain = swap_chain.QueryInterface(dxgi.IDXGISwapChain3)

        # 禁用全屏Alt+Enter
        factory.MakeWindowAssociation(hwnd, dxgi.DXGI_MWA_NO_ALT_ENTER)

    def shutdown(self) -> None:
        """清理所有资源"""
        if not self._initialized:
            return

        try:
            # GPU等待（确保所有任务完成）
            if self.command_queue:
                self.command_queue.Signal(self.frame_fence, 1)
                if self.fence_event:
                    windll.kernel32.WaitForSingleObject(self.fence_event, 5000)

            # 交换链
            if self.swap_chain:
                self.swap_chain.SetFullscreenState(False, None)

            # COM对象会自动释放
            print("[✓] DirectX12资源已清理")

        except Exception as e:
            print(f"[!] 清理过程出错: {e}")

        finally:
            self._initialized = False

    def begin_frame(self) -> None:
        """开始新的渲染帧"""
        self.perf_monitor.frame_begin()

    def end_frame(self) -> None:
        """结束帧，提交渲染命令"""
        if not self._initialized:
            return

        try:
            # 关闭命令列表
            self.command_list.Close()

            # 提交命令列表到队列
            self.command_queue.ExecuteCommandLists(1, [self.command_list])

            # 呈现交换链
            self.swap_chain.Present(
                1 if self.config.vsync else 0,  # 同步间隔
                0,  # 标志
            )

            # 更新性能数据
            self._frame_count += 1
            self.perf_monitor.frame_end()

        except Exception as e:
            print(f"[!] 帧提交失败: {e}")

    def clear(self, color: Tuple[float, float, float, float]) -> None:
        """清屏为指定颜色"""
        # TODO: 实现清屏命令
        pass

    def draw_rect(
        self, x: int, y: int, w: int, h: int, color: Tuple[float, float, float, float]
    ) -> None:
        """绘制矩形"""
        # TODO: 实现矩形绘制
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
        """绘制文字"""
        # TODO: 实现文字绘制（需要DirectWrite集成）
        pass

    def draw_stroke(
        self,
        points: np.ndarray,
        color: Tuple[float, float, float, float],
        width: float = 2.0,
        pressure: Optional[np.ndarray] = None,
    ) -> None:
        """GPU加速笔画绘制（Overlay优化）"""
        # TODO: 实现GPU笔画渲染
        pass

    def resize(self, width: int, height: int) -> None:
        """调整渲染目标大小"""
        if not self._initialized:
            return

        try:
            self.config.width = width
            self.config.height = height
            # TODO: 调整交换链大小
            print(f"[✓] 调整渲染大小到 {width}x{height}")
        except Exception as e:
            print(f"[!] 调整大小失败: {e}")

    @property
    def fps(self) -> float:
        return self.perf_monitor.avg_fps

    @property
    def gpu_name(self) -> str:
        return self._gpu_name

    @property
    def api_version(self) -> str:
        return "DirectX 12.1"
