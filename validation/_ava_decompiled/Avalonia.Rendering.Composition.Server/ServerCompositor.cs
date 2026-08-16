using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of the <see cref="T:Avalonia.Rendering.Composition.Compositor" />.
/// 1) manages deserialization of changes received from the UI thread
/// 2) triggers animation ticks
/// 3) asks composition targets to render themselves
/// </summary>
internal class ServerCompositor : IRenderLoopTask
{
	private readonly IRenderLoop _renderLoop;

	private readonly Queue<CompositionBatch> _batches = new Queue<CompositionBatch>();

	private readonly Queue<Action> _receivedJobQueue = new Queue<Action>();

	private readonly Queue<Action> _receivedPostTargetJobQueue = new Queue<Action>();

	private readonly List<ServerCompositionTarget> _activeTargets = new List<ServerCompositionTarget>();

	internal BatchStreamObjectPool<object?> BatchObjectPool;

	internal BatchStreamMemoryPool BatchMemoryPool;

	private readonly object _lock = new object();

	private Thread? _safeThread;

	private bool _uiThreadIsInsideRender;

	internal static readonly object RenderThreadDisposeStartMarker = new object();

	internal static readonly object RenderThreadJobsStartMarker = new object();

	internal static readonly object RenderThreadJobsEndMarker = new object();

	internal static readonly object RenderThreadPostTargetJobsStartMarker = new object();

	internal static readonly object RenderThreadPostTargetJobsEndMarker = new object();

	private int _ticksSinceLastCommit;

	private const int CommitGraceTicks = 10;

	private readonly List<CompositionBatch> _reusableToNotifyProcessedList = new List<CompositionBatch>();

	private readonly List<CompositionBatch> _reusableToNotifyRenderedList = new List<CompositionBatch>();

	private readonly Queue<IServerRenderResource> _renderResourcesInvalidationQueue = new Queue<IServerRenderResource>();

	private readonly HashSet<IServerRenderResource> _renderResourcesInvalidationSet = new HashSet<IServerRenderResource>();

	private readonly Queue<ServerCompositionVisual> _visualOwnPropertiesRecomputePass = new Queue<ServerCompositionVisual>();

	private readonly Queue<ServerCompositionVisual> _visualReadbackUpdatePassQueue = new Queue<ServerCompositionVisual>();

	private readonly Queue<ServerCompositionVisual> _adornerUpdateQueue = new Queue<ServerCompositionVisual>();

	private IReadOnlyDictionary<Type, object>? _renderInterfaceFeatureCache;

	private readonly object _renderInterfaceFeaturesUserApiLock = new object();

	public long LastBatchId { get; private set; }

	public Stopwatch Clock { get; } = Stopwatch.StartNew();

	public TimeSpan ServerNow { get; private set; }

	public CompositorPools Pools { get; } = new CompositorPools();

	public PlatformRenderInterfaceContextManager RenderInterface { get; }

	public CompositionOptions Options { get; }

	public ServerCompositorAnimations Animations { get; }

	public ReadbackIndices Readback { get; } = new ReadbackIndices();

	public ServerCompositor(IRenderLoop renderLoop, IPlatformGraphics? platformGraphics, CompositionOptions options, BatchStreamObjectPool<object?> batchObjectPool, BatchStreamMemoryPool batchMemoryPool)
	{
		Options = options;
		Animations = new ServerCompositorAnimations();
		_renderLoop = renderLoop;
		RenderInterface = new PlatformRenderInterfaceContextManager(platformGraphics);
		RenderInterface.ContextDisposed += RT_OnContextDisposed;
		RenderInterface.ContextCreated += RT_OnContextCreated;
		BatchObjectPool = batchObjectPool;
		BatchMemoryPool = batchMemoryPool;
		_renderLoop.Add(this);
	}

	public void EnqueueBatch(CompositionBatch batch)
	{
		lock (_batches)
		{
			_batches.Enqueue(batch);
		}
		_renderLoop.Wakeup();
	}

	internal void UpdateServerTime()
	{
		ServerNow = Clock.Elapsed;
	}

	private void ApplyPendingBatches()
	{
		bool flag = false;
		while (true)
		{
			CompositionBatch compositionBatch;
			lock (_batches)
			{
				if (_batches.Count == 0)
				{
					break;
				}
				compositionBatch = _batches.Dequeue();
				goto IL_003d;
			}
			IL_003d:
			using (BatchStreamReader batchStreamReader = new BatchStreamReader(compositionBatch.Changes, BatchMemoryPool, BatchObjectPool))
			{
				while (!batchStreamReader.IsObjectEof)
				{
					object obj = batchStreamReader.ReadObject();
					if (obj == RenderThreadJobsStartMarker)
					{
						ReadServerJobs(batchStreamReader, _receivedJobQueue, RenderThreadJobsEndMarker);
					}
					else if (obj == RenderThreadPostTargetJobsStartMarker)
					{
						ReadServerJobs(batchStreamReader, _receivedPostTargetJobQueue, RenderThreadPostTargetJobsEndMarker);
					}
					else if (obj == RenderThreadDisposeStartMarker)
					{
						ReadDisposeJobs(batchStreamReader);
					}
					else
					{
						((SimpleServerObject)obj).DeserializeChanges(batchStreamReader, compositionBatch);
					}
				}
			}
			_reusableToNotifyProcessedList.Add(compositionBatch);
			LastBatchId = compositionBatch.SequenceId;
			flag = true;
		}
		if (flag)
		{
			_ticksSinceLastCommit = 0;
		}
		else if (_ticksSinceLastCommit < int.MaxValue)
		{
			_ticksSinceLastCommit++;
		}
	}

	private void ReadServerJobs(BatchStreamReader reader, Queue<Action> queue, object endMarker)
	{
		object obj;
		while ((obj = reader.ReadObject()) != endMarker)
		{
			queue.Enqueue((Action)obj);
		}
	}

	private void ReadDisposeJobs(BatchStreamReader reader)
	{
		for (int num = reader.Read<int>(); num > 0; num--)
		{
			(reader.ReadObject() as IDisposable)?.Dispose();
		}
	}

	private void ExecuteServerJobs(Queue<Action> queue)
	{
		while (queue.Count > 0)
		{
			try
			{
				queue.Dequeue()();
			}
			catch
			{
			}
		}
	}

	private void NotifyBatchesProcessed()
	{
		foreach (CompositionBatch reusableToNotifyProcessed in _reusableToNotifyProcessedList)
		{
			reusableToNotifyProcessed.NotifyProcessed();
		}
		foreach (CompositionBatch reusableToNotifyProcessed2 in _reusableToNotifyProcessedList)
		{
			_reusableToNotifyRenderedList.Add(reusableToNotifyProcessed2);
		}
		_reusableToNotifyProcessedList.Clear();
	}

	private void NotifyBatchesRendered()
	{
		foreach (CompositionBatch reusableToNotifyRendered in _reusableToNotifyRenderedList)
		{
			reusableToNotifyRendered.NotifyRendered();
		}
		_reusableToNotifyRenderedList.Clear();
	}

	bool IRenderLoopTask.Render()
	{
		return ExecuteRender(catchExceptions: true);
	}

	public void Render(bool catchExceptions)
	{
		ExecuteRender(catchExceptions);
	}

	private bool ExecuteRender(bool catchExceptions)
	{
		if (Dispatcher.UIThread.CheckAccess())
		{
			if (_uiThreadIsInsideRender)
			{
				throw new InvalidOperationException("Reentrancy is not supported");
			}
			_uiThreadIsInsideRender = true;
			try
			{
				using (Dispatcher.UIThread.DisableProcessing())
				{
					return RenderReentrancySafe(catchExceptions);
				}
			}
			finally
			{
				_uiThreadIsInsideRender = false;
			}
		}
		return RenderReentrancySafe(catchExceptions);
	}

	private bool RenderReentrancySafe(bool catchExceptions)
	{
		lock (_lock)
		{
			try
			{
				try
				{
					_safeThread = Thread.CurrentThread;
					return RenderCore(catchExceptions);
				}
				finally
				{
					NotifyBatchesRendered();
				}
			}
			finally
			{
				_safeThread = null;
			}
		}
	}

	private TimeSpan ExecuteGlobalPasses()
	{
		long timestamp = Stopwatch.GetTimestamp();
		ApplyPendingBatches();
		NotifyBatchesProcessed();
		Animations.Process();
		ApplyEnqueuedRenderResourceChangesPass();
		VisualOwnPropertiesUpdatePass();
		AdornerUpdatePass();
		return Stopwatch.GetElapsedTime(timestamp);
	}

	private bool RenderCore(bool catchExceptions)
	{
		UpdateServerTime();
		TimeSpan diagnosticsCompositorGlobalUpdateElapsedTime = ExecuteGlobalPasses();
		try
		{
			if (!RenderInterface.IsReady)
			{
				return true;
			}
			RenderInterface.EnsureValidBackendContext();
			ExecuteServerJobs(_receivedJobQueue);
			foreach (ServerCompositionTarget activeTarget in _activeTargets)
			{
				activeTarget.Update(diagnosticsCompositorGlobalUpdateElapsedTime);
				activeTarget.Render();
			}
			VisualReadbackUpdatePass();
			ExecuteServerJobs(_receivedPostTargetJobQueue);
		}
		catch (Exception ex) when (RT_OnContextLostExceptionFilterObserver(ex) & catchExceptions)
		{
			Logger.TryGet(LogEventLevel.Error, "Visual")?.Log(this, "Exception when rendering: {Error}", ex);
		}
		if (Animations.NeedNextTick || _ticksSinceLastCommit < 10)
		{
			return true;
		}
		foreach (ServerCompositionTarget activeTarget2 in _activeTargets)
		{
			if (activeTarget2.IsWaitingForReadyRenderTarget && !activeTarget2.IsWaitingForRenderLoopWakeup)
			{
				return true;
			}
		}
		return false;
	}

	public void AddCompositionTarget(ServerCompositionTarget target)
	{
		_activeTargets.Add(target);
	}

	public void RemoveCompositionTarget(ServerCompositionTarget target)
	{
		_activeTargets.Remove(target);
	}

	public IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
	{
		using (RenderInterface.EnsureCurrent())
		{
			return RenderInterface.CreateRenderTarget(surfaces);
		}
	}

	public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
	{
		return RenderInterface.IsReadyToCreateRenderTarget(surfaces);
	}

	public bool CheckAccess()
	{
		return _safeThread == Thread.CurrentThread;
	}

	public void VerifyAccess()
	{
		if (!CheckAccess())
		{
			throw new InvalidOperationException("This object can be only accessed under compositor lock");
		}
	}

	private void ApplyEnqueuedRenderResourceChangesPass()
	{
		IServerRenderResource result;
		while (_renderResourcesInvalidationQueue.TryDequeue(out result))
		{
			result.QueuedInvalidate();
		}
		_renderResourcesInvalidationSet.Clear();
	}

	public void EnqueueRenderResourceForInvalidation(IServerRenderResource resource)
	{
		if (_renderResourcesInvalidationSet.Add(resource))
		{
			_renderResourcesInvalidationQueue.Enqueue(resource);
		}
	}

	private void VisualOwnPropertiesUpdatePass()
	{
		ServerCompositionVisual result;
		while (_visualOwnPropertiesRecomputePass.TryDequeue(out result))
		{
			result.RecomputeOwnProperties();
		}
	}

	public void EnqueueVisualForOwnPropertiesUpdatePass(ServerCompositionVisual visual)
	{
		_visualOwnPropertiesRecomputePass.Enqueue(visual);
	}

	private void VisualReadbackUpdatePass()
	{
		if (_visualReadbackUpdatePassQueue.Count == 0)
		{
			return;
		}
		Readback.BeginWrite();
		try
		{
			ulong readRevision = Readback.ReadRevision;
			ulong writeRevision = Readback.WriteRevision;
			ServerCompositionVisual result;
			while (_visualReadbackUpdatePassQueue.TryDequeue(out result))
			{
				result.UpdateReadback(writeRevision, readRevision);
			}
		}
		finally
		{
			Readback.EndWrite();
		}
	}

	public void EnqueueVisualForReadbackUpdatePass(ServerCompositionVisual visual)
	{
		_visualReadbackUpdatePassQueue.Enqueue(visual);
	}

	public void EnqueueAdornerUpdate(ServerCompositionVisual visual)
	{
		_adornerUpdateQueue.Enqueue(visual);
	}

	private void AdornerUpdatePass()
	{
		while (_adornerUpdateQueue.Count > 0)
		{
			_adornerUpdateQueue.Dequeue().UpdateAdorner();
		}
	}

	private void RT_OnContextCreated(IPlatformRenderInterfaceContext context)
	{
		lock (_renderInterfaceFeaturesUserApiLock)
		{
			_renderInterfaceFeatureCache = null;
			_renderInterfaceFeatureCache = context.PublicFeatures.ToDictionary<KeyValuePair<Type, object>, Type, object>((KeyValuePair<Type, object> x) => x.Key, (KeyValuePair<Type, object> x) => x.Value);
		}
	}

	private bool RT_OnContextLostExceptionFilterObserver(Exception e)
	{
		if (e is PlatformGraphicsContextLostException)
		{
			lock (_renderInterfaceFeaturesUserApiLock)
			{
				_renderInterfaceFeatureCache = null;
			}
		}
		return false;
	}

	private void RT_OnContextDisposed()
	{
		lock (_renderInterfaceFeaturesUserApiLock)
		{
			_renderInterfaceFeatureCache = null;
		}
	}

	public IReadOnlyDictionary<Type, object>? AT_TryGetCachedRenderInterfaceFeatures()
	{
		lock (_renderInterfaceFeaturesUserApiLock)
		{
			return _renderInterfaceFeatureCache;
		}
	}

	public IReadOnlyDictionary<Type, object> RT_GetRenderInterfaceFeatures()
	{
		lock (_renderInterfaceFeaturesUserApiLock)
		{
			return _renderInterfaceFeatureCache ?? (_renderInterfaceFeatureCache = RenderInterface.Value.PublicFeatures);
		}
	}

	public IBitmapImpl CreateCompositionVisualSnapshot(ServerCompositionVisual visual, double scaling, bool renderChildren)
	{
		using (RenderInterface.EnsureCurrent())
		{
			PixelSize pixelSize = PixelSize.FromSize(new Size(visual.Size.X, visual.Size.Y), scaling);
			Matrix transform = Matrix.CreateScale(scaling, scaling);
			visual.CombinedTransformMatrix.Invert();
			IDrawingContextLayerImpl drawingContextLayerImpl = null;
			try
			{
				drawingContextLayerImpl = RenderInterface.Value.CreateOffscreenRenderTarget(pixelSize, new Vector(scaling, scaling), enableTextAntialiasing: true);
				using (IDrawingContextImpl drawingContextImpl = drawingContextLayerImpl.CreateDrawingContext())
				{
					drawingContextImpl.Transform = transform;
					visual.Render(drawingContextImpl, LtrbRect.Infinite, null, renderChildren);
				}
				if (drawingContextLayerImpl is IDrawingContextLayerWithRenderContextAffinityImpl { HasRenderContextAffinity: not false } drawingContextLayerWithRenderContextAffinityImpl)
				{
					return drawingContextLayerWithRenderContextAffinityImpl.CreateNonAffinedSnapshot();
				}
				IDrawingContextLayerImpl result = drawingContextLayerImpl;
				drawingContextLayerImpl = null;
				return result;
			}
			finally
			{
				drawingContextLayerImpl?.Dispose();
			}
		}
	}

	public void ResetAllGpuResources()
	{
		foreach (ServerCompositionTarget activeTarget in _activeTargets)
		{
			activeTarget.ResetRenderTarget();
		}
		RenderInterface.Reset();
	}

	public void InvalidateAllCompositionTargets()
	{
		foreach (ServerCompositionTarget activeTarget in _activeTargets)
		{
			activeTarget.RequestFullRedraw();
		}
	}
}
