using System;

namespace Avalonia.Styling.Activators;

/// <summary>
/// Base class implementation of <see cref="T:Avalonia.Styling.Activators.IStyleActivator" />.
/// </summary>
internal abstract class StyleActivatorBase : IStyleActivator, IDisposable
{
	private IStyleActivatorSink? _sink;

	private bool? _value;

	public bool IsSubscribed => _sink != null;

	public bool GetIsActive()
	{
		bool flag = EvaluateIsActive();
		bool valueOrDefault = _value == true;
		if (!_value.HasValue)
		{
			valueOrDefault = flag;
			_value = valueOrDefault;
		}
		return flag;
	}

	public void Subscribe(IStyleActivatorSink sink)
	{
		if (_sink == null)
		{
			Initialize();
			_sink = sink;
			return;
		}
		throw new AvaloniaInternalException("StyleActivator is already subscribed.");
	}

	public void Unsubscribe(IStyleActivatorSink sink)
	{
		if (_sink != null)
		{
			if (_sink != sink)
			{
				throw new AvaloniaInternalException("StyleActivatorSink is not subscribed.");
			}
			_sink = null;
			Deinitialize();
		}
	}

	public void Dispose()
	{
		_sink = null;
		Deinitialize();
	}

	/// <summary>
	/// Evaluates the activation state.
	/// </summary>
	/// <remarks>
	/// This method should read directly from its inputs and not rely on any subscriptions to
	/// fire in order to be up-to-date.
	/// </remarks>
	protected abstract bool EvaluateIsActive();

	/// <summary>
	/// Called from a derived class when the activation state should be re-evaluated and the 
	/// subscriber notified of any change.
	/// </summary>
	/// <returns>
	/// The evaluated active state;
	/// </returns>
	protected bool ReevaluateIsActive()
	{
		bool isActive = GetIsActive();
		if (isActive != _value)
		{
			_value = isActive;
			_sink?.OnNext(isActive);
		}
		return isActive;
	}

	/// <summary>
	/// Called in response to a <see cref="M:Avalonia.Styling.Activators.StyleActivatorBase.Subscribe(Avalonia.Styling.Activators.IStyleActivatorSink)" /> to allow the
	/// derived class to set up any necessary subscriptions.
	/// </summary>
	protected abstract void Initialize();

	/// <summary>
	/// Called in response to an <see cref="M:Avalonia.Styling.Activators.StyleActivatorBase.Unsubscribe(Avalonia.Styling.Activators.IStyleActivatorSink)" /> or
	/// <see cref="M:Avalonia.Styling.Activators.StyleActivatorBase.Dispose" /> to allow the derived class to dispose any active subscriptions.
	/// </summary>
	protected abstract void Deinitialize();
}
