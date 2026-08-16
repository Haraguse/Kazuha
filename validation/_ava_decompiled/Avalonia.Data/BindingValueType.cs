using System;

namespace Avalonia.Data;

/// <summary>
/// Describes the type of a <see cref="T:Avalonia.Data.BindingValue`1" />.
/// </summary>
[Flags]
public enum BindingValueType
{
	/// <summary>
	/// An unset value: the target property will revert to its unbound state until a new
	/// binding value is produced.
	/// </summary>
	UnsetValue = 0,
	/// <summary>
	/// Do nothing: the binding value will be ignored.
	/// </summary>
	DoNothing = 1,
	/// <summary>
	/// A simple value.
	/// </summary>
	Value = 0x102,
	/// <summary>
	/// A binding error, such as a missing source property.
	/// </summary>
	BindingError = 0x203,
	/// <summary>
	/// A data validation error.
	/// </summary>
	DataValidationError = 0x204,
	/// <summary>
	/// A binding error with a fallback value.
	/// </summary>
	BindingErrorWithFallback = 0x303,
	/// <summary>
	/// A data validation error with a fallback value.
	/// </summary>
	DataValidationErrorWithFallback = 0x304,
	TypeMask = 0xFF,
	HasValue = 0x100,
	HasError = 0x200
}
