namespace Avalonia.Data;

/// <summary>
/// Defines the types of binding errors for a <see cref="T:Avalonia.Data.BindingNotification" />.
/// </summary>
public enum BindingErrorType
{
	/// <summary>
	/// There was no error.
	/// </summary>
	None,
	/// <summary>
	/// There was a binding error.
	/// </summary>
	Error,
	/// <summary>
	/// There was a data validation error.
	/// </summary>
	DataValidationError
}
