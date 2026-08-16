using Avalonia.Metadata;

namespace Avalonia;

/// <summary>
/// Untyped interface to <see cref="T:Avalonia.StyledPropertyMetadata`1" />
/// </summary>
[NotClientImplementable]
public interface IStyledPropertyMetadata
{
	/// <summary>
	/// Gets the default value for the property.
	/// </summary>
	object? DefaultValue { get; }
}
