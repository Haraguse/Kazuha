namespace Avalonia.Metadata;

/// <summary>
/// Represents the kind of scope from which a data type can be inherited. Used in resolving target for AvaloniaProperty.
/// </summary>
public enum InheritDataTypeFromScopeKind
{
	/// <summary>
	/// Indicates that the data type should be inherited from a style.
	/// </summary>
	Style = 1,
	/// <summary>
	/// Indicates that the data type should be inherited from a control template.
	/// </summary>
	ControlTemplate
}
