using System;

namespace Avalonia.Platform;

/// <summary>
/// Represents a platform-specific handle.
/// </summary>
public class PlatformHandle : IPlatformHandle, IEquatable<PlatformHandle>
{
	/// <summary>
	/// Gets the handle.
	/// </summary>
	public nint Handle { get; }

	/// <summary>
	/// Gets an optional string that describes what <see cref="P:Avalonia.Platform.PlatformHandle.Handle" /> represents.
	/// </summary>
	public string? HandleDescriptor { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Platform.PlatformHandle" /> class.
	/// </summary>
	/// <param name="handle">The handle.</param>
	/// <param name="descriptor">
	/// An optional string that describes what <paramref name="handle" /> represents.
	/// </param>
	public PlatformHandle(nint handle, string? descriptor)
	{
		Handle = handle;
		HandleDescriptor = descriptor;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"PlatformHandle {{ {HandleDescriptor} = {Handle} }}";
	}

	/// <inheritdoc />
	public bool Equals(PlatformHandle? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		if (Handle == other.Handle)
		{
			return HandleDescriptor == other.HandleDescriptor;
		}
		return false;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((PlatformHandle)obj);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return (Handle, HandleDescriptor).GetHashCode();
	}

	public static bool operator ==(PlatformHandle? left, PlatformHandle? right)
	{
		return object.Equals(left, right);
	}

	public static bool operator !=(PlatformHandle? left, PlatformHandle? right)
	{
		return !object.Equals(left, right);
	}
}
