using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// Represents a group of serialized changes from the UI thread to be atomically applied at the render thread
/// </summary>
public sealed class CompositionBatch
{
	private static long _nextSequenceId = 1L;

	private static readonly ConcurrentBag<BatchStreamData> _pool = new ConcurrentBag<BatchStreamData>();

	private readonly TaskCompletionSource<int> _acceptedTcs = new TaskCompletionSource<int>();

	private readonly TaskCompletionSource<int> _renderedTcs = new TaskCompletionSource<int>();

	internal long SequenceId { get; }

	internal BatchStreamData Changes { get; private set; }

	internal TimeSpan CommittedAt { get; set; }

	/// <summary>
	/// Indicates that batch got deserialized on the render thread and will soon be rendered.
	/// It's generally a good time to start producing the next one
	/// </summary>
	/// <remarks>
	/// To allow timing-sensitive code to receive the notification in time, the TaskCompletionSource
	/// is configured to invoke continuations  _synchronously_, so your `await` could happen from the render loop
	/// if it happens to run on the UI thread.
	/// It's recommended to use Dispatcher.AwaitOnPriority when consuming from the UI thread 
	/// </remarks>
	public Task Processed => _acceptedTcs.Task;

	/// <summary>
	/// Indicates that batch got rendered on the render thread.
	/// It's generally a good time to start producing the next one
	/// </summary>
	/// <remarks>
	/// To allow timing-sensitive code to receive the notification in time, the TaskCompletionSource
	/// is configured to invoke continuations  _synchronously_, so your `await` could happen from the render loop
	/// if it happens to run on the UI thread.
	/// It's recommended to use Dispatcher.AwaitOnPriority when consuming from the UI thread 
	/// </remarks>
	public Task Rendered => _renderedTcs.Task;

	internal CompositionBatch()
	{
		SequenceId = Interlocked.Increment(ref _nextSequenceId);
		if (!_pool.TryTake(out BatchStreamData result))
		{
			result = new BatchStreamData();
		}
		Changes = result;
	}

	internal void NotifyProcessed()
	{
		_pool.Add(Changes);
		Changes = null;
		_acceptedTcs.TrySetResult(0);
	}

	internal void NotifyRendered()
	{
		_renderedTcs.TrySetResult(0);
	}
}
