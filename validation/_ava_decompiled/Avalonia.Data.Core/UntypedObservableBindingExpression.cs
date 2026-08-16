using System;

namespace Avalonia.Data.Core;

internal class UntypedObservableBindingExpression : UntypedBindingExpressionBase, IObserver<object?>
{
	private readonly IObservable<object?> _observable;

	private IDisposable? _subscription;

	public override string Description => "Observable";

	public UntypedObservableBindingExpression(IObservable<object?> observable, BindingPriority priority)
		: base(priority)
	{
		_observable = observable;
	}

	protected override void StartCore()
	{
		_subscription = _observable.Subscribe(this);
	}

	protected override void StopCore()
	{
		_subscription?.Dispose();
		_subscription = null;
	}

	void IObserver<object?>.OnCompleted()
	{
	}

	void IObserver<object?>.OnError(Exception error)
	{
	}

	void IObserver<object?>.OnNext(object? value)
	{
		if (value is BindingNotification bindingNotification)
		{
			object value2 = bindingNotification.Value;
			BindingError error = ((bindingNotification.Error != null) ? new BindingError(bindingNotification.Error, bindingNotification.ErrorType) : null);
			PublishValue(value2, error);
		}
		else
		{
			PublishValue(value);
		}
	}
}
