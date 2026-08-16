using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace Avalonia.Platform;

/// <summary>
/// Information about current system color values, including information about dark mode and accent colors.
/// </summary>
public record PlatformColorValues
{
	private static Color DefaultAccent => new Color(byte.MaxValue, 0, 120, 215);

	/// <summary>
	/// System theme variant or mode.
	/// </summary>
	public PlatformThemeVariant ThemeVariant { get; init; }

	/// <summary>
	/// System high contrast preference.
	/// </summary>
	public ColorContrastPreference ContrastPreference { get; init; }

	/// <summary>
	/// Primary system accent color.
	/// </summary>
	public Color AccentColor1 { get; init; }

	/// <summary>
	/// Secondary system accent color. On some platforms can return the same value as <see cref="P:Avalonia.Platform.PlatformColorValues.AccentColor1" />.
	/// </summary>
	public Color AccentColor2
	{
		get
		{
			if (!(_accentColor2 != default(Color)))
			{
				return AccentColor1;
			}
			return _accentColor2;
		}
		init
		{
			_accentColor2 = value;
		}
	}

	/// <summary>
	/// Tertiary system accent color. On some platforms can return the same value as <see cref="P:Avalonia.Platform.PlatformColorValues.AccentColor1" />.
	/// </summary>
	public Color AccentColor3
	{
		get
		{
			if (!(_accentColor3 != default(Color)))
			{
				return AccentColor1;
			}
			return _accentColor3;
		}
		init
		{
			_accentColor3 = value;
		}
	}

	private Color _accentColor2;

	private Color _accentColor3;

	public PlatformColorValues()
	{
		AccentColor1 = DefaultAccent;
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return ((((EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(_accentColor2)) * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(_accentColor3)) * -1521134295 + EqualityComparer<PlatformThemeVariant>.Default.GetHashCode(ThemeVariant)) * -1521134295 + EqualityComparer<ColorContrastPreference>.Default.GetHashCode(ContrastPreference)) * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(AccentColor1);
	}

	[CompilerGenerated]
	public virtual bool Equals(PlatformColorValues? other)
	{
		if ((object)this != other)
		{
			if ((object)other != null && EqualityContract == other.EqualityContract && EqualityComparer<Color>.Default.Equals(_accentColor2, other._accentColor2) && EqualityComparer<Color>.Default.Equals(_accentColor3, other._accentColor3) && EqualityComparer<PlatformThemeVariant>.Default.Equals(ThemeVariant, other.ThemeVariant) && EqualityComparer<ColorContrastPreference>.Default.Equals(ContrastPreference, other.ContrastPreference))
			{
				return EqualityComparer<Color>.Default.Equals(AccentColor1, other.AccentColor1);
			}
			return false;
		}
		return true;
	}

	[CompilerGenerated]
	protected PlatformColorValues(PlatformColorValues original)
	{
		_accentColor2 = original._accentColor2;
		_accentColor3 = original._accentColor3;
		ThemeVariant = original.ThemeVariant;
		ContrastPreference = original.ContrastPreference;
		AccentColor1 = original.AccentColor1;
	}
}
