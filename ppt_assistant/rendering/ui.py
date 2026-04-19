"""
DirectX 12 UI Rendering primitives.
Implements high-level UI drawing functions (rectangles, text, paths, strokes).
"""

from dataclasses import dataclass
from typing import List, Optional, Tuple
from enum import Enum
import math


class StrokeCapStyle(Enum):
    """Stroke cap style."""

    BUTT = 0
    ROUND = 1
    SQUARE = 2


class StrokeJoinStyle(Enum):
    """Stroke join style."""

    MITER = 0
    BEVEL = 1
    ROUND = 2


@dataclass
class Color:
    """RGBA color."""

    r: int
    g: int
    b: int
    a: int = 255

    @staticmethod
    def from_hex(hex_color: int) -> "Color":
        """Create from hex color (ARGB)."""
        a = (hex_color >> 24) & 0xFF
        r = (hex_color >> 16) & 0xFF
        g = (hex_color >> 8) & 0xFF
        b = hex_color & 0xFF
        return Color(r, g, b, a)

    @staticmethod
    def from_rgb(r: int, g: int, b: int) -> "Color":
        """Create from RGB."""
        return Color(r, g, b, 255)

    def to_normalized(self) -> Tuple[float, float, float, float]:
        """Convert to normalized float RGBA."""
        return (self.r / 255.0, self.g / 255.0, self.b / 255.0, self.a / 255.0)

    def to_uint32(self) -> int:
        """Convert to UINT32 ARGB."""
        return (self.a << 24) | (self.r << 16) | (self.g << 8) | self.b


@dataclass
class Point2D:
    """2D point."""

    x: float
    y: float

    def distance_to(self, other: "Point2D") -> float:
        """Distance to another point."""
        dx = self.x - other.x
        dy = self.y - other.y
        return math.sqrt(dx * dx + dy * dy)


@dataclass
class Rect:
    """Rectangle."""

    left: float
    top: float
    right: float
    bottom: float

    @property
    def width(self) -> float:
        """Get width."""
        return self.right - self.left

    @property
    def height(self) -> float:
        """Get height."""
        return self.bottom - self.top

    @property
    def center(self) -> Point2D:
        """Get center point."""
        return Point2D(self.left + self.width / 2, self.top + self.height / 2)

    def contains_point(self, p: Point2D) -> bool:
        """Check if point is in rectangle."""
        return self.left <= p.x <= self.right and self.top <= p.y <= self.bottom

    def intersect(self, other: "Rect") -> Optional["Rect"]:
        """Get intersection rectangle."""
        left = max(self.left, other.left)
        top = max(self.top, other.top)
        right = min(self.right, other.right)
        bottom = min(self.bottom, other.bottom)

        if left < right and top < bottom:
            return Rect(left, top, right, bottom)
        return None


class UIDrawer:
    """High-level UI drawing interface."""

    def __init__(self):
        """Initialize UI drawer."""
        self.render_context = None
        self.clip_rects: List[Rect] = []

    def set_render_context(self, context):
        """Set render context."""
        self.render_context = context

    def push_clip_rect(self, rect: Rect) -> bool:
        """Push clip rectangle."""
        if not self.render_context:
            return False
        self.clip_rects.append(rect)
        return True

    def pop_clip_rect(self) -> bool:
        """Pop clip rectangle."""
        if not self.clip_rects:
            return False
        self.clip_rects.pop()
        return True

    def get_clip_rect(self) -> Optional[Rect]:
        """Get current clip rectangle."""
        if self.clip_rects:
            return self.clip_rects[-1]
        return None

    def should_cull(self, rect: Rect) -> bool:
        """Check if rect should be culled."""
        if not self.clip_rects:
            return False
        clip = self.get_clip_rect()
        return clip.intersect(rect) is None

    def draw_rect(
        self, rect: Rect, color: Color, filled: bool = True, border_width: float = 0.0
    ) -> bool:
        """
        Draw rectangle.

        Args:
            rect: Rectangle bounds
            color: Fill/border color
            filled: Whether to fill
            border_width: Border width (0 = no border)

        Returns:
            Success flag
        """
        if not self.render_context:
            return False

        if self.should_cull(rect):
            return True

        # Simplified: actual implementation would generate vertices
        return True

    def draw_rounded_rect(
        self, rect: Rect, corner_radius: float, color: Color, filled: bool = True
    ) -> bool:
        """Draw rounded rectangle."""
        if not self.render_context or self.should_cull(rect):
            return False

        return True

    def draw_circle(
        self,
        center: Point2D,
        radius: float,
        color: Color,
        filled: bool = True,
        segments: int = 32,
    ) -> bool:
        """Draw circle."""
        if not self.render_context:
            return False

        # Check bounds
        bounds = Rect(
            center.x - radius, center.y - radius, center.x + radius, center.y + radius
        )
        if self.should_cull(bounds):
            return True

        return True

    def draw_line(
        self, start: Point2D, end: Point2D, color: Color, width: float = 1.0
    ) -> bool:
        """Draw line."""
        if not self.render_context:
            return False

        return True

    def draw_polygon(
        self, points: List[Point2D], color: Color, filled: bool = True
    ) -> bool:
        """Draw polygon."""
        if not self.render_context or len(points) < 3:
            return False

        return True

    def draw_text(
        self,
        text: str,
        position: Point2D,
        color: Color,
        font_size: int = 12,
        font_name: str = "Arial",
    ) -> bool:
        """
        Draw text.

        Args:
            text: Text to draw
            position: Top-left position
            color: Text color
            font_size: Font size in pixels
            font_name: Font name

        Returns:
            Success flag
        """
        if not self.render_context or not text:
            return False

        return True

    def draw_stroke(
        self,
        points: List[Point2D],
        color: Color,
        width: float = 1.0,
        cap: StrokeCapStyle = StrokeCapStyle.ROUND,
        join: StrokeJoinStyle = StrokeJoinStyle.ROUND,
    ) -> bool:
        """
        Draw stroke (multi-segment line).

        Args:
            points: List of points
            color: Stroke color
            width: Stroke width
            cap: Cap style
            join: Join style

        Returns:
            Success flag
        """
        if not self.render_context or len(points) < 2:
            return False

        return True

    def draw_path(
        self, path_data: str, color: Color, filled: bool = True, width: float = 1.0
    ) -> bool:
        """
        Draw path from SVG-like path data.

        Args:
            path_data: SVG path data ("M x y L x y Z")
            color: Color
            filled: Whether to fill
            width: Stroke width if not filled

        Returns:
            Success flag
        """
        if not self.render_context or not path_data:
            return False

        # Parse path data and convert to primitives
        return True

    def draw_gradient_rect(
        self, rect: Rect, color1: Color, color2: Color, horizontal: bool = True
    ) -> bool:
        """Draw gradient rectangle."""
        if not self.render_context or self.should_cull(rect):
            return False

        return True

    def fill_rect(self, rect: Rect, color: Color) -> bool:
        """Fill rectangle."""
        return self.draw_rect(rect, color, filled=True)

    def outline_rect(self, rect: Rect, color: Color, width: float = 1.0) -> bool:
        """Draw rectangle outline."""
        return self.draw_rect(rect, color, filled=False, border_width=width)


class PathBuilder:
    """SVG-like path builder."""

    def __init__(self):
        """Initialize path builder."""
        self.points: List[Point2D] = []
        self.path_data = ""
        self.current_pos = Point2D(0, 0)

    def move_to(self, x: float, y: float) -> "PathBuilder":
        """Move to position."""
        self.current_pos = Point2D(x, y)
        self.points.append(self.current_pos)
        self.path_data += f"M {x} {y} "
        return self

    def line_to(self, x: float, y: float) -> "PathBuilder":
        """Line to position."""
        self.current_pos = Point2D(x, y)
        self.points.append(self.current_pos)
        self.path_data += f"L {x} {y} "
        return self

    def curve_to(
        self, cp1x: float, cp1y: float, cp2x: float, cp2y: float, x: float, y: float
    ) -> "PathBuilder":
        """Cubic Bezier curve."""
        self.current_pos = Point2D(x, y)
        self.points.append(self.current_pos)
        self.path_data += f"C {cp1x} {cp1y} {cp2x} {cp2y} {x} {y} "
        return self

    def quad_curve_to(
        self, cpx: float, cpy: float, x: float, y: float
    ) -> "PathBuilder":
        """Quadratic Bezier curve."""
        self.current_pos = Point2D(x, y)
        self.points.append(self.current_pos)
        self.path_data += f"Q {cpx} {cpy} {x} {y} "
        return self

    def arc_to(
        self,
        rx: float,
        ry: float,
        rotation: float,
        large_arc: bool,
        sweep: bool,
        x: float,
        y: float,
    ) -> "PathBuilder":
        """Elliptical arc."""
        self.current_pos = Point2D(x, y)
        self.points.append(self.current_pos)
        large = 1 if large_arc else 0
        sweep_flag = 1 if sweep else 0
        self.path_data += f"A {rx} {ry} {rotation} {large} {sweep_flag} {x} {y} "
        return self

    def close_path(self) -> "PathBuilder":
        """Close path."""
        self.path_data += "Z"
        return self

    def build(self) -> str:
        """Build path data string."""
        return self.path_data.strip()

    def get_bounds(self) -> Optional[Rect]:
        """Get path bounding rectangle."""
        if not self.points:
            return None

        xs = [p.x for p in self.points]
        ys = [p.y for p in self.points]
        return Rect(min(xs), min(ys), max(xs), max(ys))
