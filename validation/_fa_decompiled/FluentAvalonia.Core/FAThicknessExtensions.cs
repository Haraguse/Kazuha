using Avalonia;

namespace FluentAvalonia.Core;

/// <summary>
/// Extension methods for an Avaloina <see cref="T:Avalonia.Thickness" />
/// </summary>
public static class FAThicknessExtensions
{
	/// <summary>
	/// Retreives the total vertical thickness (top + bottom)
	/// </summary>
	public static double Vertical(this Thickness t)
	{
		return ((Thickness)(ref t)).Top + ((Thickness)(ref t)).Bottom;
	}

	/// <summary>
	/// Retreives the total horizontal thickness (left + right)
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public static double Horizontal(this Thickness t)
	{
		return ((Thickness)(ref t)).Left + ((Thickness)(ref t)).Right;
	}
}
