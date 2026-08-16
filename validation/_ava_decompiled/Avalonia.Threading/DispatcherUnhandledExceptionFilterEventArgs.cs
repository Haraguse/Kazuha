using System;

namespace Avalonia.Threading;

/// <summary>
/// Represents the method that will handle the <see cref="E:Avalonia.Threading.Dispatcher.UnhandledExceptionFilter" /> event.
/// </summary>
public sealed class DispatcherUnhandledExceptionFilterEventArgs : DispatcherEventArgs
{
	private Exception? _exception;

	private bool _requestCatch;

	/// <summary>
	/// Gets the exception that was raised when executing code by way of the dispatcher.
	/// </summary>
	public Exception Exception => _exception;

	/// <summary>
	/// Gets or sets whether the exception should be caught and the event handlers called..
	/// </summary>
	/// <remarks>
	///     A filter handler can set this property to false to request that
	///     the exception not be caught, to avoid the callstack getting
	///     unwound up to the Dispatcher.
	///     <p />
	///     A previous handler in the event multicast might have already set this 
	///     property to false, signalling that the exception will not be caught.
	///     We let the "don't catch" behavior override all others because
	///     it most likely means a debugging scenario.
	/// </remarks>
	public bool RequestCatch
	{
		get
		{
			return _requestCatch;
		}
		set
		{
			if (!value)
			{
				_requestCatch = value;
			}
		}
	}

	internal DispatcherUnhandledExceptionFilterEventArgs(Dispatcher dispatcher)
		: base(dispatcher)
	{
	}

	internal void Initialize(Exception exception, bool requestCatch)
	{
		_exception = exception;
		_requestCatch = requestCatch;
	}
}
