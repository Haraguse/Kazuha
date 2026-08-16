namespace Avalonia.Platform;

public record struct PlatformGraphicsExternalImageProperties
{
	public int Width { get; set; }

	public int Height { get; set; }

	public PlatformGraphicsExternalImageFormat Format { get; set; }

	public ulong MemorySize { get; set; }

	public ulong MemoryOffset { get; set; }

	public bool TopLeftOrigin { get; set; }
}
