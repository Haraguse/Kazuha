namespace Avalonia;

/// <summary>
/// Defines the reference point units of an <see cref="T:Avalonia.RelativePoint" /> or 
/// <see cref="T:Avalonia.RelativeRect" />.
/// </summary>
public enum RelativeUnit
{
	/// <summary>
	/// The point is expressed as a fraction of the containing element's size.
	/// </summary>
	Relative,
	/// <summary>
	/// The point is absolute (i.e. in pixels).
	/// </summary>
	Absolute
}
