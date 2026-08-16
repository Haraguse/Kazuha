using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// The Compositor class manages communication between UI-thread and render-thread parts of the composition engine.
/// It also serves as a factory to create UI-thread parts of various composition objects 
/// </summary>
public class Compositor
{
	private readonly ServerCompositor _server;

	private CompositionBatch? _nextCommit;

	private readonly BatchStreamObjectPool<object?> _batchObjectPool;

	private readonly BatchStreamMemoryPool _batchMemoryPool;

	private readonly Queue<ICompositorSerializable> _objectSerializationQueue = new Queue<ICompositorSerializable>();

	private readonly HashSet<ICompositorSerializable> _objectSerializationHashSet = new HashSet<ICompositorSerializable>();

	private Queue<Action> _invokeBeforeCommitWrite = new Queue<Action>();

	private Queue<Action> _invokeBeforeCommitRead = new Queue<Action>();

	private readonly HashSet<IDisposable> _disposeOnNextBatch = new HashSet<IDisposable>();

	private CompositionBatch? _pendingBatch;

	private readonly object _pendingBatchLock = new object();

	private readonly List<Action> _pendingServerCompositorJobs = new List<Action>();

	private readonly List<Action> _pendingServerCompositorPostTargetJobs = new List<Action>();

	private readonly Action _triggerCommitRequested;

	internal IRenderLoop Loop { get; }

	internal bool UseUiThreadForSynchronousCommits { get; }

	internal ServerCompositor Server => _server;

	internal IEasing DefaultEasing { get; }

	internal Dispatcher Dispatcher { get; }

	internal event Action? AfterCommit;

	/// <summary>
	/// Creates a new compositor on a specified render loop that would use a particular GPU
	/// </summary>
	[PrivateApi]
	public Compositor(IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits = false)
		: this(AvaloniaLocator.Current.GetRequiredService<IRenderLoop>(), gpu, useUiThreadForSynchronousCommits)
	{
	}

	internal Compositor(IRenderLoop loop, IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits = false)
		: this(loop, gpu, useUiThreadForSynchronousCommits, MediaContext.Instance, reclaimBuffersImmediately: false, Avalonia.Threading.Dispatcher.UIThread)
	{
	}

	internal Compositor(IRenderLoop loop, IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits, ICompositorScheduler scheduler, bool reclaimBuffersImmediately, Dispatcher dispatcher, CompositionOptions? options = null)
	{
		Compositor compositor = this;
		if (options == null)
		{
			options = AvaloniaLocator.Current.GetService<CompositionOptions>() ?? new CompositionOptions();
		}
		Loop = loop;
		UseUiThreadForSynchronousCommits = useUiThreadForSynchronousCommits;
		Dispatcher = dispatcher;
		_batchMemoryPool = new BatchStreamMemoryPool(reclaimBuffersImmediately);
		_batchObjectPool = new BatchStreamObjectPool<object>(reclaimBuffersImmediately);
		_server = new ServerCompositor(loop, gpu, options, _batchObjectPool, _batchMemoryPool);
		_triggerCommitRequested = delegate
		{
			scheduler.CommitRequested(compositor);
		};
		DefaultEasing = new SplineEasing(new KeySpline(0.25, 0.1, 0.25, 1.0));
	}

	/// <summary>
	/// Requests pending changes in the composition objects to be serialized and sent to the render thread
	/// </summary>
	/// <returns>A task that completes when sent changes are applied on the render thread</returns>
	public Task RequestCommitAsync()
	{
		return RequestCompositionBatchCommitAsync().Processed;
	}

	/// <summary>
	/// Requests pending changes in the composition objects to be serialized and sent to the render thread
	/// </summary>
	/// <returns>A CompositionBatch object that provides batch lifetime information</returns>
	public CompositionBatch RequestCompositionBatchCommitAsync()
	{
		Dispatcher.VerifyAccess();
		if (_nextCommit == null)
		{
			using (NonPumpingLockHelper.Use())
			{
				_nextCommit = new CompositionBatch();
				CompositionBatch pendingBatch = _pendingBatch;
				if (pendingBatch != null)
				{
					pendingBatch.Processed.ContinueWith(delegate
					{
						Dispatcher.Post(_triggerCommitRequested, DispatcherPriority.Send);
					}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				}
				else
				{
					_triggerCommitRequested();
				}
			}
		}
		return _nextCommit;
	}

	internal CompositionBatch Commit()
	{
		try
		{
			return CommitCore();
		}
		finally
		{
			if (_invokeBeforeCommitWrite.Count > 0)
			{
				RequestCommitAsync();
			}
			AfterCommit?.Invoke();
		}
	}

	private CompositionBatch CommitCore()
	{
		Dispatcher.VerifyAccess();
		using (NonPumpingLockHelper.Use())
		{
			CompositionBatch result = _nextCommit ?? (_nextCommit = new CompositionBatch());
			Queue<Action> invokeBeforeCommitWrite = _invokeBeforeCommitWrite;
			Queue<Action> invokeBeforeCommitRead = _invokeBeforeCommitRead;
			_invokeBeforeCommitRead = invokeBeforeCommitWrite;
			_invokeBeforeCommitWrite = invokeBeforeCommitRead;
			while (_invokeBeforeCommitRead.Count > 0)
			{
				_invokeBeforeCommitRead.Dequeue()();
			}
			using (BatchStreamWriter batchStreamWriter = new BatchStreamWriter(_nextCommit.Changes, _batchMemoryPool, _batchObjectPool))
			{
				ICompositorSerializable result2;
				while (_objectSerializationQueue.TryDequeue(out result2))
				{
					SimpleServerObject simpleServerObject = result2.TryGetServer(this);
					if (simpleServerObject != null)
					{
						batchStreamWriter.WriteObject(simpleServerObject);
						result2.SerializeChanges(this, batchStreamWriter);
					}
				}
				_objectSerializationHashSet.Clear();
				if (_disposeOnNextBatch.Count != 0)
				{
					batchStreamWriter.WriteObject(ServerCompositor.RenderThreadDisposeStartMarker);
					batchStreamWriter.Write(_disposeOnNextBatch.Count);
					foreach (IDisposable item in _disposeOnNextBatch)
					{
						batchStreamWriter.WriteObject(item);
					}
					_disposeOnNextBatch.Clear();
				}
				SerializeServerJobs(batchStreamWriter, _pendingServerCompositorJobs, ServerCompositor.RenderThreadJobsStartMarker, ServerCompositor.RenderThreadJobsEndMarker);
				SerializeServerJobs(batchStreamWriter, _pendingServerCompositorPostTargetJobs, ServerCompositor.RenderThreadPostTargetJobsStartMarker, ServerCompositor.RenderThreadPostTargetJobsEndMarker);
			}
			_nextCommit.CommittedAt = Server.Clock.Elapsed;
			_server.EnqueueBatch(_nextCommit);
			lock (_pendingBatchLock)
			{
				_pendingBatch = _nextCommit;
				_pendingBatch.Processed.ContinueWith(delegate(Task t)
				{
					lock (_pendingBatchLock)
					{
						if (_pendingBatch?.Processed == t)
						{
							_pendingBatch = null;
						}
					}
				}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				_nextCommit = null;
				return result;
			}
		}
		static void SerializeServerJobs(BatchStreamWriter writer, List<Action> list, object startMarker, object endMarker)
		{
			if (list.Count > 0)
			{
				writer.WriteObject(startMarker);
				foreach (Action item2 in list)
				{
					writer.WriteObject(item2);
				}
				writer.WriteObject(endMarker);
			}
			list.Clear();
		}
	}

	/// <summary>
	/// This method submits a composition with a single dispose command outside the normal
	/// commit cycle. This is currently used for disposing CompositionTargets since we need to do that ASAP
	/// and without affecting the not yet completed composition batch
	/// </summary>
	internal CompositionBatch OobDispose(CompositionObject obj)
	{
		using (NonPumpingLockHelper.Use())
		{
			obj.Dispose();
			CompositionBatch compositionBatch = new CompositionBatch();
			using (BatchStreamWriter batchStreamWriter = new BatchStreamWriter(compositionBatch.Changes, _batchMemoryPool, _batchObjectPool))
			{
				batchStreamWriter.WriteObject(ServerCompositor.RenderThreadDisposeStartMarker);
				batchStreamWriter.Write(1);
				batchStreamWriter.WriteObject(obj.Server);
			}
			compositionBatch.CommittedAt = Server.Clock.Elapsed;
			_server.EnqueueBatch(compositionBatch);
			return compositionBatch;
		}
	}

	internal void RegisterForSerialization(ICompositorSerializable compositionObject)
	{
		Dispatcher.VerifyAccess();
		if (_objectSerializationHashSet.Add(compositionObject))
		{
			_objectSerializationQueue.Enqueue(compositionObject);
		}
		RequestCommitAsync();
	}

	internal void DisposeOnNextBatch(SimpleServerObject obj)
	{
		if (obj is IDisposable item && _disposeOnNextBatch.Add(item))
		{
			RequestCommitAsync();
		}
	}

	/// <summary>
	/// Enqueues a callback to be called before the next scheduled commit.
	/// If there is no scheduled commit it automatically schedules one
	/// This is useful for updating your composition tree objects after binding
	/// and layout passes have completed
	/// </summary>
	public void RequestCompositionUpdate(Action action)
	{
		Dispatcher.VerifyAccess();
		_invokeBeforeCommitWrite.Enqueue(action);
		RequestCommitAsync();
	}

	internal void PostServerJob(Action job, bool postTarget = false)
	{
		Dispatcher.VerifyAccess();
		(postTarget ? _pendingServerCompositorPostTargetJobs : _pendingServerCompositorJobs).Add(job);
		RequestCommitAsync();
	}

	internal Task InvokeServerJobAsync(Action job, bool postTarget = false)
	{
		return InvokeServerJobAsync(delegate
		{
			job();
			return (object?)null;
		}, postTarget);
	}

	internal Task<T> InvokeServerJobAsync<T>(Func<T> job, bool postTarget = false)
	{
		TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		PostServerJob(delegate
		{
			try
			{
				tcs.SetResult(job());
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
		}, postTarget);
		return tcs.Task;
	}

	internal ValueTask<IReadOnlyDictionary<Type, object>> GetRenderInterfacePublicFeatures()
	{
		IReadOnlyDictionary<Type, object> readOnlyDictionary = Server.AT_TryGetCachedRenderInterfaceFeatures();
		if (readOnlyDictionary != null)
		{
			return new ValueTask<IReadOnlyDictionary<Type, object>>(readOnlyDictionary);
		}
		if (!Loop.RunsInBackground)
		{
			return new ValueTask<IReadOnlyDictionary<Type, object>>(Server.RT_GetRenderInterfaceFeatures());
		}
		return new ValueTask<IReadOnlyDictionary<Type, object>>(InvokeServerJobAsync((Func<IReadOnlyDictionary<Type, object>>)Server.RT_GetRenderInterfaceFeatures, false));
	}

	/// <summary>
	/// Attempts to query for a feature from the platform render interface
	/// </summary>
	public async ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType)
	{
		(await GetRenderInterfacePublicFeatures().ConfigureAwait(continueOnCapturedContext: false)).TryGetValue(featureType, out var value);
		return value;
	}

	public async Task<Bitmap> CreateCompositionVisualSnapshot(CompositionVisual visual, double scaling)
	{
		if (visual.Compositor != this)
		{
			throw new InvalidOperationException();
		}
		if (visual.Root == null)
		{
			throw new InvalidOperationException();
		}
		return new Bitmap(RefCountable.Create(await InvokeServerJobAsync(() => _server.CreateCompositionVisualSnapshot(visual.Server, scaling, renderChildren: true), postTarget: true)));
	}

	/// <summary>
	/// Attempts to query for GPU interop feature from the platform render interface
	/// </summary>
	/// <returns></returns>
	public async ValueTask<ICompositionGpuInterop?> TryGetCompositionGpuInterop()
	{
		IExternalObjectsRenderInterfaceContextFeature externalObjectsRenderInterfaceContextFeature = (IExternalObjectsRenderInterfaceContextFeature)(await TryGetRenderInterfaceFeature(typeof(IExternalObjectsRenderInterfaceContextFeature)).ConfigureAwait(continueOnCapturedContext: false));
		if (externalObjectsRenderInterfaceContextFeature == null)
		{
			return null;
		}
		return new CompositionInterop(this, externalObjectsRenderInterfaceContextFeature);
	}

	internal bool UnitTestIsRegisteredForSerialization(ICompositorSerializable serializable)
	{
		return _objectSerializationHashSet.Contains(serializable);
	}

	/// <summary>
	/// Attempts to get the Compositor instance that will be used by default for new TopLevels
	/// created by the current platform backend.
	///
	/// This won't work for every single platform backend and backend settings, e. g. with web we'll need to have
	/// separate Compositor instances per output HTML canvas since they don't share OpenGL state.
	/// Another case where default compositor won't be available is our planned multithreaded rendering mode
	/// where each window would get its own Compositor instance
	///
	/// This method is still useful for obtaining GPU device LUID to speed up initialization, but you should
	/// always check if default Compositor matches one used by our control once it gets attached to a TopLevel
	/// </summary>
	/// <returns></returns>
	public static Compositor? TryGetDefaultCompositor()
	{
		return AvaloniaLocator.Current.GetService<Compositor>();
	}

	/// <summary>
	/// Creates a new CompositionTarget
	/// </summary>
	/// <param name="surfaces">A factory method to create IRenderTarget to be called from the render thread</param>
	/// <returns></returns>
	internal CompositionTarget CreateCompositionTarget(Func<IEnumerable<IPlatformRenderSurface>> surfaces)
	{
		return new CompositionTarget(this, new ServerCompositionTarget(_server, surfaces));
	}

	public CompositionContainerVisual CreateContainerVisual()
	{
		return new CompositionContainerVisual(this, new ServerCompositionContainerVisual(_server));
	}

	public ExpressionAnimation CreateExpressionAnimation()
	{
		return new ExpressionAnimation(this);
	}

	public ExpressionAnimation CreateExpressionAnimation(string expression)
	{
		return new ExpressionAnimation(this)
		{
			Expression = expression
		};
	}

	public ImplicitAnimationCollection CreateImplicitAnimationCollection()
	{
		return new ImplicitAnimationCollection(this);
	}

	public CompositionAnimationGroup CreateAnimationGroup()
	{
		return new CompositionAnimationGroup(this);
	}

	public CompositionSolidColorVisual CreateSolidColorVisual()
	{
		return new CompositionSolidColorVisual(this, new ServerCompositionSolidColorVisual(Server));
	}

	public CompositionCustomVisual CreateCustomVisual(CompositionCustomVisualHandler handler)
	{
		return new CompositionCustomVisual(this, handler);
	}

	public CompositionSurfaceVisual CreateSurfaceVisual()
	{
		return new CompositionSurfaceVisual(this, new ServerCompositionSurfaceVisual(_server));
	}

	public CompositionDrawingSurface CreateDrawingSurface()
	{
		return new CompositionDrawingSurface(this);
	}

	public ScalarKeyFrameAnimation CreateScalarKeyFrameAnimation()
	{
		return new ScalarKeyFrameAnimation(this);
	}

	public DoubleKeyFrameAnimation CreateDoubleKeyFrameAnimation()
	{
		return new DoubleKeyFrameAnimation(this);
	}

	public BooleanKeyFrameAnimation CreateBooleanKeyFrameAnimation()
	{
		return new BooleanKeyFrameAnimation(this);
	}

	public ColorKeyFrameAnimation CreateColorKeyFrameAnimation()
	{
		return new ColorKeyFrameAnimation(this);
	}

	public VectorKeyFrameAnimation CreateVectorKeyFrameAnimation()
	{
		return new VectorKeyFrameAnimation(this);
	}

	public Vector2KeyFrameAnimation CreateVector2KeyFrameAnimation()
	{
		return new Vector2KeyFrameAnimation(this);
	}

	public Vector3KeyFrameAnimation CreateVector3KeyFrameAnimation()
	{
		return new Vector3KeyFrameAnimation(this);
	}

	public Vector3DKeyFrameAnimation CreateVector3DKeyFrameAnimation()
	{
		return new Vector3DKeyFrameAnimation(this);
	}

	public Vector4KeyFrameAnimation CreateVector4KeyFrameAnimation()
	{
		return new Vector4KeyFrameAnimation(this);
	}

	public QuaternionKeyFrameAnimation CreateQuaternionKeyFrameAnimation()
	{
		return new QuaternionKeyFrameAnimation(this);
	}
}
