using System;

namespace Avalonia.Threading;

/// <summary>
/// Provides data for the <see cref="E:Avalonia.Threading.Dispatcher.UnhandledException" /> event.
/// </summary>
public sealed class DispatcherUnhandledExceptionEventArgs : DispatcherEventArgs
{
	private Exception _exception;

	private bool _handled;

	/// <summary>
	/// Gets the exception that was raised when executing code by way of the dispatcher.
	/// </summary>
	public Exception Exception => _exception;

	/// <summary>
	/// Gets or sets whether the exception event has been handled.
	/// </summary>
	public bool Handled
	{
		get
		{
			return _handled;
		}
		set
		{
			if (value)
			{
				_handled = value;
			}
		}
	}

	internal DispatcherUnhandledExceptionEventArgs(Dispatcher dispatcher)
		: base(dispatcher)
	{
		_exception = null;
	}

	internal void Initialize(Exception exception, bool handled)
	{
		_exception = exception;
		_handled = handled;
	}
}
