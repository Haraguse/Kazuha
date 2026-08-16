using Avalonia.Data;

namespace Avalonia.Diagnostics;

/// <summary>
/// Holds diagnostic-related information about the value of an <see cref="T:Avalonia.AvaloniaProperty" />
/// on an <see cref="T:Avalonia.AvaloniaObject" />.
/// </summary>
public sealed class AvaloniaPropertyValue
{
	/// <summary>
	/// Gets the property.
	/// </summary>
	public AvaloniaProperty Property { get; }

	/// <summary>
	/// Gets the current property value.
	/// </summary>
	public object? Value { get; }

	/// <summary>
	/// Gets the priority of the current value.
	/// </summary>
	public BindingPriority Priority { get; }

	/// <summary>
	/// Gets a diagnostic string.
	/// </summary>
	public string? Diagnostic { get; }

	/// <summary>
	/// Gets a value indicating whether the <see cref="P:Avalonia.Diagnostics.AvaloniaPropertyValue.Value" /> was overridden by a call to 
	/// <see cref="M:Avalonia.AvaloniaObject.SetCurrentValue``1(Avalonia.StyledProperty{``0},``0)" />.
	/// </summary>
	public bool IsOverriddenCurrentValue { get; }

	internal AvaloniaPropertyValue(AvaloniaProperty property, object? value, BindingPriority priority, string? diagnostic, bool isOverriddenCurrentValue)
	{
		Property = property;
		Value = value;
		Priority = priority;
		Diagnostic = diagnostic;
		IsOverriddenCurrentValue = isOverriddenCurrentValue;
	}
}
