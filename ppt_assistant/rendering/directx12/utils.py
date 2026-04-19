"""DirectX 12 工具函数"""

from typing import Tuple


def make_color(
    r: float, g: float, b: float, a: float = 1.0
) -> Tuple[float, float, float, float]:
    """创建RGBA颜色元组"""
    return (max(0, min(1, r)), max(0, min(1, g)), max(0, min(1, b)), max(0, min(1, a)))


def hresult_to_string(hr: int) -> str:
    """将HRESULT错误码转换为可读字符串"""
    errors = {
        0x80004001: "E_NOTIMPL",
        0x80000001: "E_NOTIMPL",
        0x80000002: "E_NOTIMPL",
        0x80000003: "E_INVALID_ARG",
        0x80000004: "E_NO_INTERFACE",
        0x80000005: "E_POINTER",
        0x80000006: "E_HANDLE",
        0x80000007: "E_ABORT",
        0x80000008: "E_FAIL",
        0x80000009: "E_ACCESSDENIED",
        0x80004005: "E_FAIL",
        0x8007007E: "DXGI_ERROR_NOT_FOUND",
    }
    return errors.get(hr, f"Unknown (0x{hr:X})")


class ComPtr:
    """COM指针包装器（用于自动引用计数）"""

    def __init__(self, ptr=None):
        self._ptr = ptr

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.release()

    def release(self):
        if self._ptr:
            # 调用Release方法
            try:
                self._ptr.Release()
            except:
                pass
            self._ptr = None

    def __del__(self):
        self.release()

    def get(self):
        return self._ptr

    def __getattr__(self, name):
        if self._ptr:
            return getattr(self._ptr, name)
        raise RuntimeError("COM对象已释放")
