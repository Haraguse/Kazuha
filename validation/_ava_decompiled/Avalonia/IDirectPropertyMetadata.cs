using Avalonia.Metadata;

namespace Avalonia;

/// <summary>
/// Untyped interface to <see cref="T:Avalonia.DirectPropertyMetadata`1" />
/// </summary>
[NotClientImplementable]
public interface IDirectPropertyMetadata
{
	/// <summary>
	/// Gets the to use when the property is set to <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
	/// </summary>
	object? UnsetValue { get; }

	/// <summary>
	/// Gets a value indicating whether the property is interested in data validation.
	/// </summary>
	bool? EnableDataValidation { get; }
}
