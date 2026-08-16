namespace Avalonia;

/// <summary>
/// Class representing the <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
/// </summary>
public sealed class UnsetValueType
{
	internal UnsetValueType()
	{
	}

	/// <summary>
	/// Returns the string representation of the <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
	/// </summary>
	/// <returns>The string "(unset)".</returns>
	public override string ToString()
	{
		return "(unset)";
	}
}
