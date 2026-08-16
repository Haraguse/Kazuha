using System;
using Avalonia.Media;

namespace Avalonia;

/// <summary>
/// Extensions for <see cref="T:Avalonia.AvaloniaProperty" />.
/// </summary>
internal static class AvaloniaPropertyExtensions
{
	/// <summary>
	/// Checks if values of given property can affect rendering (via <see cref="T:Avalonia.Media.IAffectsRender" />).
	/// </summary>
	/// <param name="property">Property to check.</param>
	public static bool CanValueAffectRender(this AvaloniaProperty property)
	{
		Type propertyType = property.PropertyType;
		return !propertyType.IsSealed || typeof(IAffectsRender).IsAssignableFrom(propertyType);
	}
}
