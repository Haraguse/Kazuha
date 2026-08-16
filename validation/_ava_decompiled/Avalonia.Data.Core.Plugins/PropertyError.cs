using System;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// An <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" /> that represents an error.
/// </summary>
internal class PropertyError : IPropertyAccessor, IDisposable
{
	private readonly BindingNotification _error;

	/// <inheritdoc />
	public Type? PropertyType => null;

	/// <inheritdoc />
	public object? Value => _error;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Core.Plugins.PropertyError" /> class.
	/// </summary>
	/// <param name="error">The error to report.</param>
	public PropertyError(BindingNotification error)
	{
		_error = error;
	}

	/// <inheritdoc />
	public void Dispose()
	{
	}

	/// <inheritdoc />
	public bool SetValue(object? value, BindingPriority priority)
	{
		return false;
	}

	public void Subscribe(Action<object> listener)
	{
		listener(_error);
	}

	public void Unsubscribe()
	{
	}
}
