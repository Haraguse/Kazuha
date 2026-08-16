namespace Avalonia.Platform;

/// <summary>
/// Represents a platform-specific handle.
/// </summary>
public interface IPlatformHandle
{
	/// <summary>
	/// Gets the handle.
	/// </summary>
	nint Handle { get; }

	/// <summary>
	/// Gets an optional string that describes what <see cref="P:Avalonia.Platform.IPlatformHandle.Handle" /> represents.
	/// </summary>
	string? HandleDescriptor { get; }
}
