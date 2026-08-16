using System;
using System.Threading;

namespace Avalonia.Threading;

/// <summary>
///     Representation of Dispatcher frame.
/// </summary>
public class DispatcherFrame
{
	private readonly bool _exitWhenRequested;

	private bool _continue;

	private bool _isRunning;

	private CancellationTokenSource? _cancellationTokenSource;

	public Dispatcher Dispatcher { get; }

	/// <summary>
	///     Indicates that this dispatcher frame should exit.
	/// </summary>
	public bool Continue
	{
		get
		{
			bool flag = _continue;
			if (flag && _exitWhenRequested)
			{
				Dispatcher dispatcher = Dispatcher;
				if (dispatcher.ExitAllFramesRequested || dispatcher.HasShutdownStarted)
				{
					flag = false;
				}
			}
			return flag;
		}
		set
		{
			lock (Dispatcher.InstanceLock)
			{
				_continue = value;
				if (!_continue)
				{
					_cancellationTokenSource?.Cancel();
				}
			}
		}
	}

	/// <summary>
	///     Constructs a new instance of the DispatcherFrame class.
	/// </summary>
	public DispatcherFrame()
		: this(exitWhenRequested: true)
	{
	}

	/// <summary>
	///     Constructs a new instance of the DispatcherFrame class.
	/// </summary>
	/// <param name="exitWhenRequested">
	///     Indicates whether or not this frame will exit when all frames
	///     are requested to exit.
	///     <p />
	///     Dispatcher frames typically break down into two categories:
	///     1) Long running, general purpose frames, that exit only when
	///        told to.  These frames should exit when requested.
	///     2) Short running, very specific frames that exit themselves
	///        when an important criteria is met.  These frames may
	///        consider not exiting when requested in favor of waiting
	///        for their important criteria to be met.  These frames
	///        should have a timeout associated with them.
	/// </param>
	public DispatcherFrame(bool exitWhenRequested)
		: this(Avalonia.Threading.Dispatcher.UIThread, exitWhenRequested)
	{
		Dispatcher.VerifyAccess();
	}

	internal DispatcherFrame(Dispatcher dispatcher, bool exitWhenRequested)
	{
		Dispatcher = dispatcher;
		_exitWhenRequested = exitWhenRequested;
		_continue = true;
	}

	internal void Run(IControlledDispatcherImpl impl)
	{
		Dispatcher.VerifyAccess();
		while (true)
		{
			lock (Dispatcher.InstanceLock)
			{
				if (!Continue)
				{
					break;
				}
				if (_isRunning)
				{
					throw new InvalidOperationException("This frame is already running");
				}
				_cancellationTokenSource = new CancellationTokenSource();
				_isRunning = true;
			}
			try
			{
				Dispatcher.RequestProcessing();
				impl.RunLoop(_cancellationTokenSource.Token);
			}
			finally
			{
				lock (Dispatcher.InstanceLock)
				{
					_isRunning = false;
					_cancellationTokenSource?.Cancel();
					_cancellationTokenSource?.Dispose();
					_cancellationTokenSource = null;
				}
			}
		}
	}

	internal void MaybeExitOnDispatcherRequest()
	{
		if (_exitWhenRequested)
		{
			_cancellationTokenSource?.Cancel();
		}
	}
}
