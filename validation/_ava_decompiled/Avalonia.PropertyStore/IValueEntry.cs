using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore;

/// <summary>
/// Represents an untyped value entry in a <see cref="T:Avalonia.PropertyStore.ValueFrame" />.
/// </summary>
internal interface IValueEntry
{
	/// <summary>
	/// Gets the property that this value applies to.
	/// </summary>
	AvaloniaProperty Property { get; }

	/// <summary>
	/// Checks whether the entry has a value, starting the entry if necessary.
	/// </summary>
	bool HasValue();

	/// <summary>
	/// Gets the value associated with the entry.
	/// </summary>
	/// <exception cref="T:Avalonia.AvaloniaInternalException">
	/// The entry has no value.
	/// </exception>
	object? GetValue();

	/// <summary>
	/// Gets the data validation state if supported.
	/// </summary>
	/// <param name="state">The binding validation state.</param>
	/// <param name="error">The current binding error, if any.</param>
	/// <returns>
	/// True if the entry supports data validation, otherwise false.
	/// </returns>
	bool GetDataValidationState(out BindingValueType state, out Exception? error);

	/// <summary>
	/// Called when the value entry is removed from the value store.
	/// </summary>
	void Unsubscribe();
}
/// <summary>
/// Represents a typed value entry in a <see cref="T:Avalonia.PropertyStore.ValueFrame" />.
/// </summary>
internal interface IValueEntry<T> : IValueEntry
{
	/// <summary>
	/// Gets the value associated with the entry.
	/// </summary>
	/// <exception cref="T:Avalonia.AvaloniaInternalException">
	/// The entry has no value.
	/// </exception>
	new T GetValue();
}
