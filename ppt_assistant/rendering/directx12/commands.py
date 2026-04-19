"""
DirectX 12 Command recording and execution.
Handles command list recording and GPU command buffer management.
"""

from dataclasses import dataclass
from enum import Enum
from typing import List, Optional, Tuple


class CommandType(Enum):
    """Type of GPU command."""

    CLEAR = 0
    DRAW = 1
    DRAW_INDEXED = 2
    SET_PIPELINE = 3
    SET_VERTEX_BUFFER = 4
    SET_INDEX_BUFFER = 5
    SET_CONSTANT_BUFFER = 6
    SET_SCISSOR = 7
    SET_VIEWPORT = 8
    COPY_RESOURCE = 9


@dataclass
class DrawCommand:
    """GPU draw command."""

    type: CommandType
    vertex_count: int = 0
    index_count: int = 0
    start_vertex: int = 0
    start_index: int = 0
    start_instance: int = 0
    instance_count: int = 1
    pso_name: Optional[str] = None


@dataclass
class ClearCommand:
    """Clear command."""

    type: CommandType  # CLEAR
    color: Tuple[float, float, float, float]
    depth: float = 1.0


@dataclass
class SetBufferCommand:
    """Set buffer command."""

    type: CommandType  # SET_*_BUFFER
    buffer_name: str
    slot: int = 0


@dataclass
class Viewport:
    """Viewport definition."""

    x: float
    y: float
    width: float
    height: float
    min_depth: float = 0.0
    max_depth: float = 1.0

    def contains_point(self, px: float, py: float) -> bool:
        """Check if point is in viewport."""
        return (
            self.x <= px < self.x + self.width and self.y <= py < self.y + self.height
        )


@dataclass
class Scissor:
    """Scissor rectangle."""

    left: int
    top: int
    right: int
    bottom: int

    @property
    def width(self) -> int:
        """Get width."""
        return self.right - self.left

    @property
    def height(self) -> int:
        """Get height."""
        return self.bottom - self.top


class CommandContext:
    """Command recording context."""

    def __init__(self, name: str, max_commands: int = 10000):
        """
        Initialize command context.

        Args:
            name: Context name
            max_commands: Maximum commands to record
        """
        self.name = name
        self.commands: List = []
        self.max_commands = max_commands
        self.is_recording = False
        self.current_pso: Optional[str] = None
        self.current_scissor: Optional[Scissor] = None
        self.current_viewport: Optional[Viewport] = None

    def begin_recording(self) -> bool:
        """Begin recording commands."""
        if self.is_recording:
            return False
        self.is_recording = True
        self.commands.clear()
        return True

    def end_recording(self) -> bool:
        """End recording and return command count."""
        if not self.is_recording:
            return False
        self.is_recording = False
        return True

    def clear(
        self, color: Tuple[float, float, float, float], depth: float = 1.0
    ) -> bool:
        """Record clear command."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        cmd = ClearCommand(CommandType.CLEAR, color, depth)
        self.commands.append(cmd)
        return True

    def set_pipeline_state(self, pso_name: str) -> bool:
        """Set pipeline state object."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        self.current_pso = pso_name
        self.commands.append((CommandType.SET_PIPELINE, pso_name))
        return True

    def set_vertex_buffer(self, buffer_name: str, slot: int = 0) -> bool:
        """Set vertex buffer."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        cmd = SetBufferCommand(CommandType.SET_VERTEX_BUFFER, buffer_name, slot)
        self.commands.append(cmd)
        return True

    def set_index_buffer(self, buffer_name: str) -> bool:
        """Set index buffer."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        cmd = SetBufferCommand(CommandType.SET_INDEX_BUFFER, buffer_name)
        self.commands.append(cmd)
        return True

    def set_constant_buffer(self, buffer_name: str, slot: int = 0) -> bool:
        """Set constant buffer."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        cmd = SetBufferCommand(CommandType.SET_CONSTANT_BUFFER, buffer_name, slot)
        self.commands.append(cmd)
        return True

    def set_viewport(self, x: float, y: float, width: float, height: float) -> bool:
        """Set viewport."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        self.current_viewport = Viewport(x, y, width, height)
        self.commands.append((CommandType.SET_VIEWPORT, self.current_viewport))
        return True

    def set_scissor(self, left: int, top: int, right: int, bottom: int) -> bool:
        """Set scissor rectangle."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        self.current_scissor = Scissor(left, top, right, bottom)
        self.commands.append((CommandType.SET_SCISSOR, self.current_scissor))
        return True

    def draw(
        self, vertex_count: int, start_vertex: int = 0, instance_count: int = 1
    ) -> bool:
        """Record draw command."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        cmd = DrawCommand(
            CommandType.DRAW,
            vertex_count=vertex_count,
            start_vertex=start_vertex,
            instance_count=instance_count,
            pso_name=self.current_pso,
        )
        self.commands.append(cmd)
        return True

    def draw_indexed(
        self,
        index_count: int,
        start_index: int = 0,
        start_vertex: int = 0,
        instance_count: int = 1,
    ) -> bool:
        """Record indexed draw command."""
        if not self.is_recording or len(self.commands) >= self.max_commands:
            return False

        cmd = DrawCommand(
            CommandType.DRAW_INDEXED,
            index_count=index_count,
            start_index=start_index,
            start_vertex=start_vertex,
            instance_count=instance_count,
            pso_name=self.current_pso,
        )
        self.commands.append(cmd)
        return True

    def get_command_count(self) -> int:
        """Get recorded command count."""
        return len(self.commands)

    def get_commands(self) -> List:
        """Get all recorded commands."""
        return self.commands.copy()

    def reset(self):
        """Reset context."""
        self.commands.clear()
        self.is_recording = False
        self.current_pso = None
        self.current_scissor = None
        self.current_viewport = None


class CommandQueue:
    """GPU command queue wrapper."""

    def __init__(self, name: str):
        """
        Initialize command queue.

        Args:
            name: Queue name
        """
        self.name = name
        self.queue = None  # Set by device
        self.submitted_count = 0

    def submit(self, context: CommandContext) -> bool:
        """Submit command list for execution."""
        if context.get_command_count() == 0:
            return False

        self.submitted_count += 1
        return True

    def wait_idle(self) -> bool:
        """Wait for queue to idle."""
        return True

    def get_submitted_count(self) -> int:
        """Get command lists submitted."""
        return self.submitted_count


class CommandList:
    """GPU command list."""

    def __init__(self, name: str, is_direct: bool = True):
        """
        Initialize command list.

        Args:
            name: Command list name
            is_direct: True for direct queue, False for copy queue
        """
        self.name = name
        self.is_direct = is_direct
        self.is_open = False

    def reset(self) -> bool:
        """Reset command list."""
        self.is_open = False
        return True

    def close(self) -> bool:
        """Close command list."""
        if not self.is_open:
            return False
        self.is_open = False
        return True
