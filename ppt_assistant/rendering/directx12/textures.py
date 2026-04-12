"""
DirectX 12 Texture management.
Handles texture creation, loading, and caching.
"""

from dataclasses import dataclass
from enum import Enum
from typing import Optional, Dict, Tuple
import io


class TextureFormat(Enum):
    """Texture format enum."""
    RGBA8 = 0
    SRGBA8 = 1
    R8 = 2
    RG16F = 3
    RGBA16F = 4
    RGBA32F = 5


@dataclass
class TextureDesc:
    """Texture description."""
    width: int
    height: int
    format: TextureFormat = TextureFormat.RGBA8
    mip_levels: int = 1
    is_render_target: bool = False
    
    @property
    def pixel_size(self) -> int:
        """Get bytes per pixel based on format."""
        format_sizes = {
            TextureFormat.RGBA8: 4,
            TextureFormat.SRGBA8: 4,
            TextureFormat.R8: 1,
            TextureFormat.RG16F: 4,
            TextureFormat.RGBA16F: 8,
            TextureFormat.RGBA32F: 16,
        }
        return format_sizes.get(self.format, 4)


class GPUTexture:
    """GPU texture wrapper."""
    
    def __init__(self, name: str, desc: TextureDesc, 
                 data: Optional[bytes] = None):
        """
        Initialize GPU texture.
        
        Args:
            name: Texture name
            desc: Texture description
            data: Initial texture data
        """
        self.name = name
        self.desc = desc
        self.data = data
        self.resource = None
        self.shader_resource_view = None
        self.render_target_view = None
    
    def get_name(self) -> str:
        """Get texture name."""
        return self.name
    
    def get_width(self) -> int:
        """Get texture width."""
        return self.desc.width
    
    def get_height(self) -> int:
        """Get texture height."""
        return self.desc.height
    
    def get_format(self) -> TextureFormat:
        """Get texture format."""
        return self.desc.format


class Texture2D(GPUTexture):
    """2D texture."""
    
    @staticmethod
    def create_color_texture(width: int, height: int, 
                            color: Tuple[int, int, int, int]) -> 'Texture2D':
        """
        Create solid color texture.
        
        Args:
            width: Texture width
            height: Texture height
            color: RGBA color tuple
            
        Returns:
            Texture2D instance
        """
        desc = TextureDesc(width, height, TextureFormat.RGBA8)
        
        # Create solid color data
        r, g, b, a = color
        pixel = bytes([r, g, b, a])
        data = pixel * (width * height)
        
        return Texture2D(f"color_{width}x{height}_{color}", desc, data)
    
    @staticmethod
    def create_gradient_texture(width: int, height: int) -> 'Texture2D':
        """
        Create gradient texture.
        
        Args:
            width: Texture width
            height: Texture height
            
        Returns:
            Texture2D instance
        """
        desc = TextureDesc(width, height, TextureFormat.RGBA8)
        
        # Create gradient data
        data = bytearray()
        for y in range(height):
            for x in range(width):
                r = int(255 * x / width)
                g = int(255 * y / height)
                b = 128
                a = 255
                data.extend([r, g, b, a])
        
        return Texture2D(f"gradient_{width}x{height}", desc, bytes(data))
    
    @staticmethod
    def create_checkerboard_texture(width: int, height: int, 
                                    square_size: int = 8) -> 'Texture2D':
        """
        Create checkerboard texture.
        
        Args:
            width: Texture width
            height: Texture height
            square_size: Size of each square
            
        Returns:
            Texture2D instance
        """
        desc = TextureDesc(width, height, TextureFormat.RGBA8)
        
        # Create checkerboard data
        data = bytearray()
        for y in range(height):
            for x in range(width):
                is_white = ((x // square_size) + (y // square_size)) % 2 == 0
                gray = 255 if is_white else 128
                data.extend([gray, gray, gray, 255])
        
        return Texture2D(f"checkerboard_{width}x{height}", desc, bytes(data))


class FontTexture:
    """Font/glyph texture atlas."""
    
    def __init__(self, width: int, height: int):
        """
        Initialize font texture.
        
        Args:
            width: Atlas width
            height: Atlas height
        """
        self.width = width
        self.height = height
        self.desc = TextureDesc(width, height, TextureFormat.R8)
        
        # Create blank atlas
        data = bytes([0] * (width * height))
        self.texture = GPUTexture(f"font_atlas_{width}x{height}", 
                                 self.desc, data)
        self.glyphs: Dict[int, Tuple[int, int, int, int]] = {}  # char -> (x, y, w, h)
    
    def add_glyph(self, char_code: int, x: int, y: int, 
                  width: int, height: int) -> bool:
        """
        Add glyph to atlas.
        
        Args:
            char_code: Character code
            x: X position in atlas
            y: Y position in atlas
            width: Glyph width
            height: Glyph height
            
        Returns:
            Success flag
        """
        if x + width > self.width or y + height > self.height:
            return False
        
        self.glyphs[char_code] = (x, y, width, height)
        return True
    
    def get_glyph(self, char_code: int) -> Optional[Tuple[int, int, int, int]]:
        """Get glyph position in atlas."""
        return self.glyphs.get(char_code)
    
    def get_texture(self) -> GPUTexture:
        """Get underlying texture."""
        return self.texture


class TexturePool:
    """Manage and cache GPU textures."""
    
    def __init__(self, max_textures: int = 500):
        """
        Initialize texture pool.
        
        Args:
            max_textures: Maximum number of textures
        """
        self.textures: Dict[str, GPUTexture] = {}
        self.font_atlases: Dict[str, FontTexture] = {}
        self.max_textures = max_textures
    
    def create_texture_2d(self, name: str, width: int, height: int,
                         data: Optional[bytes] = None,
                         format: TextureFormat = TextureFormat.RGBA8) -> Optional[Texture2D]:
        """Create 2D texture."""
        if name in self.textures:
            return self.textures[name]
        
        if len(self.textures) >= self.max_textures:
            return None
        
        desc = TextureDesc(width, height, format)
        tex = Texture2D(name, desc, data)
        self.textures[name] = tex
        return tex
    
    def create_font_atlas(self, name: str, width: int = 512,
                         height: int = 512) -> Optional[FontTexture]:
        """Create font atlas."""
        if name in self.font_atlases:
            return self.font_atlases[name]
        
        if len(self.font_atlases) >= 10:  # Limit font atlases
            return None
        
        atlas = FontTexture(width, height)
        self.font_atlases[name] = atlas
        return atlas
    
    def get_texture(self, name: str) -> Optional[GPUTexture]:
        """Get texture by name."""
        return self.textures.get(name)
    
    def get_font_atlas(self, name: str) -> Optional[FontTexture]:
        """Get font atlas by name."""
        return self.font_atlases.get(name)
    
    def remove_texture(self, name: str) -> bool:
        """Remove texture."""
        if name in self.textures:
            del self.textures[name]
            return True
        return False
    
    def remove_font_atlas(self, name: str) -> bool:
        """Remove font atlas."""
        if name in self.font_atlases:
            del self.font_atlases[name]
            return True
        return False
    
    def clear(self):
        """Clear all textures."""
        self.textures.clear()
        self.font_atlases.clear()
    
    def get_texture_count(self) -> int:
        """Get total texture count."""
        return len(self.textures)
    
    def get_total_memory_usage(self) -> int:
        """Get estimated total memory usage in bytes."""
        total = 0
        for tex in self.textures.values():
            total += tex.desc.width * tex.desc.height * tex.desc.pixel_size
        for atlas in self.font_atlases.values():
            total += atlas.width * atlas.height
        return total
