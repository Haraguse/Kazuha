"""
DirectX 12 Shader compilation and management.
Handles HLSL shader compilation and pipeline state creation.
"""

import ctypes
from dataclasses import dataclass
from typing import Dict, Optional, Tuple
import struct

try:
    import comtypes
    from comtypes import HRESULT
    from comtypes.gen import DXC
except ImportError:
    DXC = None


@dataclass
class ShaderSource:
    """Container for shader source code."""
    vertex: str
    pixel: str
    compute: Optional[str] = None


class ShaderCompiler:
    """Compile HLSL shaders to bytecode."""
    
    # Default HLSL shader library
    SHADER_SOURCES = {
        'basic': ShaderSource(
            vertex="""
cbuffer TransformBuffer : register(b0) {
    float4x4 projection;
    float4x4 view;
    float4x4 world;
};

struct VS_INPUT {
    float3 position : POSITION;
    float4 color : COLOR;
};

struct PS_INPUT {
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

PS_INPUT main(VS_INPUT input) {
    PS_INPUT output;
    float4 pos = float4(input.position, 1.0f);
    pos = mul(pos, world);
    pos = mul(pos, view);
    pos = mul(pos, projection);
    output.position = pos;
    output.color = input.color;
    return output;
}
            """,
            pixel="""
struct PS_INPUT {
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

float4 main(PS_INPUT input) : SV_TARGET {
    return input.color;
}
            """
        ),
        'text': ShaderSource(
            vertex="""
cbuffer TransformBuffer : register(b0) {
    float4x4 projection;
};

struct VS_INPUT {
    float2 position : POSITION;
    float2 texCoord : TEXCOORD0;
    float4 color : COLOR;
};

struct PS_INPUT {
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD0;
    float4 color : COLOR;
};

PS_INPUT main(VS_INPUT input) {
    PS_INPUT output;
    float4 pos = float4(input.position, 0.0f, 1.0f);
    output.position = mul(pos, projection);
    output.texCoord = input.texCoord;
    output.color = input.color;
    return output;
}
            """,
            pixel="""
Texture2D fontTexture : register(t0);
SamplerState samplerState : register(s0);

struct PS_INPUT {
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD0;
    float4 color : COLOR;
};

float4 main(PS_INPUT input) : SV_TARGET {
    float alpha = fontTexture.Sample(samplerState, input.texCoord).r;
    return float4(input.color.rgb, input.color.a * alpha);
}
            """
        ),
        'path': ShaderSource(
            vertex="""
cbuffer TransformBuffer : register(b0) {
    float4x4 projection;
};

struct VS_INPUT {
    float2 position : POSITION;
    float4 color : COLOR;
};

struct PS_INPUT {
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

PS_INPUT main(VS_INPUT input) {
    PS_INPUT output;
    float4 pos = float4(input.position, 0.0f, 1.0f);
    output.position = mul(pos, projection);
    output.color = input.color;
    return output;
}
            """,
            pixel="""
struct PS_INPUT {
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

float4 main(PS_INPUT input) : SV_TARGET {
    return input.color;
}
            """
        ),
    }
    
    def __init__(self):
        """Initialize shader compiler."""
        self.compiled_cache: Dict[str, bytes] = {}
        self._try_load_compiler()
    
    def _try_load_compiler(self):
        """Attempt to load DirectX Shader Compiler."""
        if DXC is None:
            self.has_dxc = False
            return
        
        try:
            # Try to load DXC library
            self.has_dxc = True
        except Exception:
            self.has_dxc = False
    
    def compile_shader(self, source: str, entry_point: str, 
                      target: str, shader_name: str = "shader") -> bytes:
        """
        Compile HLSL shader to bytecode.
        
        Args:
            source: HLSL source code
            entry_point: Entry point function name (e.g., "main")
            target: Shader target (e.g., "ps_6_0", "vs_6_0")
            shader_name: Name for caching
            
        Returns:
            Compiled shader bytecode
        """
        cache_key = f"{shader_name}_{entry_point}_{target}"
        if cache_key in self.compiled_cache:
            return self.compiled_cache[cache_key]
        
        # Fallback: return dummy bytecode for testing
        # In production, use actual DXC or FXC compiler
        bytecode = self._generate_dummy_bytecode(len(source))
        self.compiled_cache[cache_key] = bytecode
        return bytecode
    
    @staticmethod
    def _generate_dummy_bytecode(size: int) -> bytes:
        """Generate dummy shader bytecode for testing."""
        # Minimal DXBC header
        header = b'DXBC'
        header += struct.pack('<I', 0)  # Hash
        header += struct.pack('<I', 1)  # Version
        header += struct.pack('<I', size)  # Size
        header += struct.pack('<I', 0)  # Chunk count (simplified)
        header += b'\x00' * (size - len(header))
        return header[:size] if len(header) > size else header
    
    def get_shader(self, name: str) -> Optional[ShaderSource]:
        """Get shader source by name."""
        return self.SHADER_SOURCES.get(name)


class PipelineStateObject:
    """Represents a DirectX 12 Pipeline State Object (PSO)."""
    
    def __init__(self, name: str, vertex_bytecode: bytes, 
                 pixel_bytecode: bytes):
        """
        Initialize PSO.
        
        Args:
            name: PSO name
            vertex_bytecode: Compiled vertex shader
            pixel_bytecode: Compiled pixel shader
        """
        self.name = name
        self.vertex_bytecode = vertex_bytecode
        self.pixel_bytecode = pixel_bytecode
        self.pso_object = None  # Will be set by device
    
    def get_name(self) -> str:
        """Get PSO name."""
        return self.name


class ShaderManager:
    """Manage shader compilation and caching."""
    
    def __init__(self):
        """Initialize shader manager."""
        self.compiler = ShaderCompiler()
        self.pso_cache: Dict[str, PipelineStateObject] = {}
    
    def create_pso(self, name: str, vs_name: str, 
                  ps_name: str) -> Optional[PipelineStateObject]:
        """
        Create a Pipeline State Object.
        
        Args:
            name: PSO name
            vs_name: Vertex shader name
            ps_name: Pixel shader name
            
        Returns:
            PipelineStateObject or None
        """
        if name in self.pso_cache:
            return self.pso_cache[name]
        
        vs_source = self.compiler.get_shader(vs_name)
        ps_source = self.compiler.get_shader(ps_name)
        
        if not vs_source or not ps_source:
            return None
        
        # Compile shaders
        vs_bytecode = self.compiler.compile_shader(
            vs_source.vertex, "main", "vs_6_0", vs_name
        )
        ps_bytecode = self.compiler.compile_shader(
            ps_source.pixel, "main", "ps_6_0", ps_name
        )
        
        pso = PipelineStateObject(name, vs_bytecode, ps_bytecode)
        self.pso_cache[name] = pso
        return pso
    
    def get_pso(self, name: str) -> Optional[PipelineStateObject]:
        """Get PSO by name."""
        return self.pso_cache.get(name)
    
    def get_all_psos(self) -> Dict[str, PipelineStateObject]:
        """Get all cached PSOs."""
        return self.pso_cache.copy()
