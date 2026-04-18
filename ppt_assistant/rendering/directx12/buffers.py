"""
DirectX 12 Buffer management (vertex, index, constant).
Handles GPU resource creation and memory management.
"""

from dataclasses import dataclass
from enum import Enum
from typing import Optional, List
import struct


class BufferType(Enum):
    """Buffer type enum."""

    VERTEX = 0
    INDEX = 1
    CONSTANT = 2
    STAGING = 3


@dataclass
class Vertex:
    """Simple vertex structure."""

    x: float
    y: float
    z: float
    r: float
    g: float
    b: float
    a: float

    def to_bytes(self) -> bytes:
        """Convert to bytes."""
        return struct.pack(
            "<7f", self.x, self.y, self.z, self.r, self.g, self.b, self.a
        )

    @staticmethod
    def size() -> int:
        """Get vertex size in bytes."""
        return 28  # 7 floats * 4 bytes


@dataclass
class Vertex2D:
    """2D vertex for UI rendering."""

    x: float
    y: float
    z: float = 0.0
    r: float = 1.0
    g: float = 1.0
    b: float = 1.0
    a: float = 1.0
    u: float = 0.0
    v: float = 0.0

    def to_bytes(self) -> bytes:
        """Convert to bytes."""
        return struct.pack(
            "<9f",
            self.x,
            self.y,
            self.z,
            self.r,
            self.g,
            self.b,
            self.a,
            self.u,
            self.v,
        )

    @staticmethod
    def size() -> int:
        """Get vertex size in bytes."""
        return 36  # 9 floats * 4 bytes


class GPUBuffer:
    """GPU buffer wrapper."""

    def __init__(
        self,
        name: str,
        buffer_type: BufferType,
        size: int,
        data: Optional[bytes] = None,
    ):
        """
        Initialize GPU buffer.

        Args:
            name: Buffer name
            buffer_type: Type of buffer
            size: Buffer size in bytes
            data: Initial data
        """
        self.name = name
        self.buffer_type = buffer_type
        self.size = size
        self.data = data
        self.resource = None
        self.gpu_virtual_address = 0

    def get_name(self) -> str:
        """Get buffer name."""
        return self.name

    def get_size(self) -> int:
        """Get buffer size."""
        return self.size

    def get_type(self) -> BufferType:
        """Get buffer type."""
        return self.buffer_type


class VertexBuffer(GPUBuffer):
    """Vertex buffer."""

    def __init__(self, name: str, vertices: List[Vertex], stride: int = 28):
        """
        Initialize vertex buffer.

        Args:
            name: Buffer name
            vertices: List of vertices
            stride: Vertex stride in bytes
        """
        self.vertices = vertices
        self.stride = stride
        self.count = len(vertices)

        # Convert vertices to bytes
        data = b"".join(v.to_bytes() for v in vertices)
        super().__init__(name, BufferType.VERTEX, len(data), data)

        self.view = None  # Set by device

    def get_vertex_count(self) -> int:
        """Get vertex count."""
        return self.count

    def get_stride(self) -> int:
        """Get vertex stride."""
        return self.stride


class Vertex2DBuffer(VertexBuffer):
    """2D vertex buffer for UI."""

    def __init__(self, name: str, vertices: List[Vertex2D]):
        """
        Initialize 2D vertex buffer.

        Args:
            name: Buffer name
            vertices: List of 2D vertices
        """
        # Convert to base vertices but track as 2D
        self.vertices_2d = vertices
        self.stride = 36  # Vertex2D size
        self.count = len(vertices)

        # Convert vertices to bytes
        data = b"".join(v.to_bytes() for v in vertices)
        GPUBuffer.__init__(self, name, BufferType.VERTEX, len(data), data)

        self.view = None


class IndexBuffer(GPUBuffer):
    """Index buffer."""

    def __init__(self, name: str, indices: List[int]):
        """
        Initialize index buffer.

        Args:
            name: Buffer name
            indices: List of indices
        """
        self.indices = indices
        self.count = len(indices)

        # Convert indices to bytes (uint32)
        data = struct.pack(f"<{len(indices)}I", *indices)
        super().__init__(name, BufferType.INDEX, len(data), data)

        self.view = None
        self.format = "R32_UINT"

    def get_index_count(self) -> int:
        """Get index count."""
        return self.count

    def get_format(self) -> str:
        """Get index format."""
        return self.format


class ConstantBuffer(GPUBuffer):
    """Constant buffer for shader data."""

    def __init__(self, name: str, size: int, data: Optional[bytes] = None):
        """
        Initialize constant buffer.

        Args:
            name: Buffer name
            size: Buffer size (must be 256-byte aligned)
            data: Initial data
        """
        # Align to 256 bytes
        aligned_size = ((size + 255) // 256) * 256
        super().__init__(name, BufferType.CONSTANT, aligned_size, data)
        self.view = None

    def update_data(self, data: bytes) -> bool:
        """Update buffer data."""
        if len(data) > self.size:
            return False
        self.data = data
        return True


class BufferPool:
    """Manage and cache GPU buffers."""

    def __init__(self, max_buffers: int = 1000):
        """
        Initialize buffer pool.

        Args:
            max_buffers: Maximum number of buffers
        """
        self.buffers = {}
        self.max_buffers = max_buffers

    def create_vertex_buffer(
        self, name: str, vertices: List[Vertex]
    ) -> Optional[VertexBuffer]:
        """Create vertex buffer."""
        if name in self.buffers:
            return self.buffers[name]

        if len(self.buffers) >= self.max_buffers:
            return None

        vb = VertexBuffer(name, vertices)
        self.buffers[name] = vb
        return vb

    def create_index_buffer(
        self, name: str, indices: List[int]
    ) -> Optional[IndexBuffer]:
        """Create index buffer."""
        if name in self.buffers:
            return self.buffers[name]

        if len(self.buffers) >= self.max_buffers:
            return None

        ib = IndexBuffer(name, indices)
        self.buffers[name] = ib
        return ib

    def create_constant_buffer(
        self, name: str, size: int, data: Optional[bytes] = None
    ) -> Optional[ConstantBuffer]:
        """Create constant buffer."""
        if name in self.buffers:
            return self.buffers[name]

        if len(self.buffers) >= self.max_buffers:
            return None

        cb = ConstantBuffer(name, size, data)
        self.buffers[name] = cb
        return cb

    def get_buffer(self, name: str) -> Optional[GPUBuffer]:
        """Get buffer by name."""
        return self.buffers.get(name)

    def remove_buffer(self, name: str) -> bool:
        """Remove buffer."""
        if name in self.buffers:
            del self.buffers[name]
            return True
        return False

    def clear(self):
        """Clear all buffers."""
        self.buffers.clear()

    def get_buffer_count(self) -> int:
        """Get total buffer count."""
        return len(self.buffers)
