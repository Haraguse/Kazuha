using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

internal sealed class SendOrPostCallbackDispatcherOperation : DispatcherOperation
{
	private readonly object? _arg;

	internal SendOrPostCallbackDispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, SendOrPostCallback callback, object? arg, bool throwOnUiThread)
		: base(dispatcher, priority, throwOnUiThread)
	{
		Callback = callback;
		_arg = arg;
	}

	protected override void InvokeCore()
	{
		try
		{
			((SendOrPostCallback)Callback)(_arg);
			lock (base.Dispatcher.InstanceLock)
			{
				base.Status = DispatcherOperationStatus.Completed;
				if (TaskSource is TaskCompletionSource<object> taskCompletionSource)
				{
					taskCompletionSource.SetResult(null);
				}
			}
		}
		catch (Exception ex)
		{
			lock (base.Dispatcher.InstanceLock)
			{
				GetTaskCore();
				base.Status = DispatcherOperationStatus.Completed;
				if (TaskSource is TaskCompletionSource<object> taskCompletionSource2)
				{
					taskCompletionSource2.SetException(ex);
				}
			}
			if (ThrowOnUiThread && !base.Dispatcher.TryCatchWhen(ex))
			{
				throw;
			}
		}
	}
}
