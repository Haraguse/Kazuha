using System;

namespace Avalonia.Data.Core.Plugins;

/// <summary>
/// Base class for data validators.
/// </summary>
/// <remarks>
/// Data validators are <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" />s that are returned from an 
/// <see cref="T:Avalonia.Data.Core.Plugins.IDataValidationPlugin" />. They wrap an inner <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" />
/// and convert any values received from the inner property accessor into
/// <see cref="T:Avalonia.Data.BindingNotification" />s.
/// </remarks>
internal abstract class DataValidationBase : PropertyAccessorBase, IObserver<object?>
{
	private readonly IPropertyAccessor _inner;

	/// <inheritdoc />
	public override Type? PropertyType => _inner.PropertyType;

	/// <inheritdoc />
	public override object? Value => _inner.Value;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Core.Plugins.DataValidationBase" /> class.
	/// </summary>
	/// <param name="inner">The inner property accessor.</param>
	protected DataValidationBase(IPropertyAccessor inner)
	{
		_inner = inner;
	}

	/// <inheritdoc />
	public override bool SetValue(object? value, BindingPriority priority)
	{
		return _inner.SetValue(value, priority);
	}

	/// <summary>
	/// Should never be called: the inner <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" /> should never notify
	/// completion.
	/// </summary>
	void IObserver<object?>.OnCompleted()
	{
	}

	/// <summary>
	/// Should never be called: the inner <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" /> should never notify
	/// an error.
	/// </summary>
	void IObserver<object?>.OnError(Exception error)
	{
	}

	/// <summary>
	/// Called when the inner <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" /> notifies with a new value.
	/// </summary>
	/// <param name="value">The value.</param>
	void IObserver<object?>.OnNext(object? value)
	{
		InnerValueChanged(value);
	}

	/// <summary>
	/// Begins listening to the inner <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" />.
	/// </summary>
	protected override void SubscribeCore()
	{
		_inner.Subscribe(InnerValueChanged);
	}

	/// <inheritdoc />
	protected override void UnsubscribeCore()
	{
		_inner.Dispose();
	}

	/// <summary>
	/// Called when the inner <see cref="T:Avalonia.Data.Core.Plugins.IPropertyAccessor" /> notifies with a new value.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <remarks>
	/// Notifies the observer that the value has changed. The value will be wrapped in a
	/// <see cref="T:Avalonia.Data.BindingNotification" /> if it is not already a binding notification.
	/// </remarks>
	protected virtual void InnerValueChanged(object? value)
	{
		BindingNotification value2 = (value as BindingNotification) ?? new BindingNotification(value);
		PublishValue(value2);
	}
}
