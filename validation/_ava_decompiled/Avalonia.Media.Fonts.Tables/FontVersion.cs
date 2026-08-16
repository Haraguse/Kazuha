using System.Diagnostics;

namespace Avalonia.Media.Fonts.Tables;

/// <summary>
/// Represents a Version16Dot16 value from OpenType font tables.
/// The high 16 bits represent the major version, and the low 16 bits represent the minor version as a fraction.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
internal readonly struct FontVersion
{
	/// <summary>
	/// Gets the major version number.
	/// </summary>
	public ushort Major { get; }

	/// <summary>
	/// Gets the minor version number (as a fraction of 65536).
	/// </summary>
	public ushort Minor { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Fonts.Tables.FontVersion" /> struct from raw Version16Dot16 value.
	/// </summary>
	/// <param name="value">The 32-bit Version16Dot16 value.</param>
	public FontVersion(uint value)
	{
		Major = (ushort)(value >> 16);
		Minor = (ushort)(value & 0xFFFF);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Fonts.Tables.FontVersion" /> struct from major and minor components.
	/// </summary>
	/// <param name="major">The major version number.</param>
	/// <param name="minor">The minor version number (as a fraction of 65536).</param>
	public FontVersion(ushort major, ushort minor)
	{
		Major = major;
		Minor = minor;
	}

	/// <summary>
	/// Converts the version to a floating-point representation.
	/// </summary>
	public float ToFloat()
	{
		return (float)(int)Major + (float)(int)Minor / 65536f;
	}

	/// <summary>
	/// Returns the raw 32-bit Version16Dot16 value.
	/// </summary>
	public uint ToUInt32()
	{
		return (uint)((Major << 16) | Minor);
	}

	public override string ToString()
	{
		if (Minor == 0)
		{
			return Major.ToString();
		}
		if (Minor == 32768)
		{
			return $"{Major}.5";
		}
		return ToFloat().ToString("F6").TrimEnd('0').TrimEnd('.');
	}

	public static implicit operator float(FontVersion version)
	{
		return version.ToFloat();
	}

	public static bool operator ==(FontVersion left, FontVersion right)
	{
		if (left.Major == right.Major)
		{
			return left.Minor == right.Minor;
		}
		return false;
	}

	public static bool operator !=(FontVersion left, FontVersion right)
	{
		return !(left == right);
	}

	public override bool Equals(object? obj)
	{
		if (obj is FontVersion fontVersion)
		{
			return this == fontVersion;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Major << 16) | Minor;
	}
}
