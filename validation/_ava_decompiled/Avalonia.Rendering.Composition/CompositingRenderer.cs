using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections.Pooled;
using Avalonia.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A renderer that utilizes <see cref="T:Avalonia.Rendering.Composition.Compositor" /> to render the visual tree 
/// </summary>
internal class CompositingRenderer : IRendererWithCompositor, IRenderer, IDisposable, IHitTester
{
	private readonly IPresentationSource _root;

	private readonly Compositor _compositor;

	private readonly RenderDataDrawingContext _recorder;

	private readonly HashSet<Visual> _dirty = new HashSet<Visual>();

	private readonly HashSet<Visual> _recalculateChildren = new HashSet<Visual>();

	private readonly Action _update;

	private bool _queuedUpdate;

	private bool _queuedSceneInvalidation;

	private bool _updating;

	private bool _isDisposed;

	internal CompositionTarget CompositionTarget { get; }

	/// <inheritdoc />
	public RendererDiagnostics Diagnostics { get; }

	/// <inheritdoc />
	public Compositor Compositor => _compositor;

	public bool IsDisposed => _isDisposed;

	/// <inheritdoc />
	public event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

	/// <summary>
	/// Initializes a new instance of <see cref="T:Avalonia.Rendering.Composition.CompositingRenderer" />
	/// </summary>
	/// <param name="root">The render root using this renderer.</param>
	/// <param name="compositor">The associated compositors.</param>
	/// <param name="surfaces">
	/// A function returning the list of native platform's surfaces that can be consumed by rendering subsystems.
	/// </param>
	public CompositingRenderer(IPresentationSource root, Compositor compositor, Func<IEnumerable<IPlatformRenderSurface>> surfaces)
	{
		_root = root;
		_compositor = compositor;
		_recorder = new RenderDataDrawingContext(compositor);
		CompositionTarget = compositor.CreateCompositionTarget(surfaces);
		_update = Update;
		Diagnostics = new RendererDiagnostics();
		Diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
	}

	private void OnDiagnosticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		string propertyName = e.PropertyName;
		if (!(propertyName == "DebugOverlays"))
		{
			if (propertyName == "LastLayoutPassTiming")
			{
				CompositionTarget.LastLayoutPassTiming = Diagnostics.LastLayoutPassTiming;
			}
		}
		else
		{
			CompositionTarget.DebugOverlays = Diagnostics.DebugOverlays;
		}
	}

	private void QueueUpdate()
	{
		if (!_queuedUpdate)
		{
			_queuedUpdate = true;
			_compositor.RequestCompositionUpdate(_update);
		}
	}

	/// <inheritdoc />
	public void AddDirty(Visual visual)
	{
		if (!_isDisposed)
		{
			if (_updating)
			{
				throw new InvalidOperationException("Visual was invalidated during the render pass");
			}
			_dirty.Add(visual);
			QueueUpdate();
		}
	}

	/// <inheritdoc />
	public IEnumerable<Visual> HitTest(Point p, Visual? root, Func<Visual, bool>? filter)
	{
		using (Diagnostic.PerformingHitTest())
		{
			CompositionVisual root2 = null;
			if (root != null)
			{
				if (root.CompositionVisual == null)
				{
					yield break;
				}
				root2 = root.CompositionVisual;
			}
			Func<CompositionVisual, bool> filter2 = null;
			if (filter != null)
			{
				filter2 = (CompositionVisual v) => !(v is CompositionDrawListVisual compositionDrawListVisual2) || filter(compositionDrawListVisual2.Visual);
			}
			using PooledList<CompositionVisual> res = CompositionTarget.TryHitTest(p, root2, filter2);
			if (res == null)
			{
				yield break;
			}
			foreach (CompositionVisual item in res)
			{
				if (item is CompositionDrawListVisual compositionDrawListVisual && (filter == null || filter(compositionDrawListVisual.Visual)))
				{
					yield return compositionDrawListVisual.Visual;
				}
			}
		}
	}

	/// <inheritdoc />
	public Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool>? filter)
	{
		using (Diagnostic.PerformingHitTest())
		{
			if (root.CompositionVisual == null)
			{
				return null;
			}
			Func<CompositionVisual, bool> filter2 = ((filter == null) ? null : ((Func<CompositionVisual, bool>)((CompositionVisual v) => !(v is CompositionDrawListVisual compositionDrawListVisual2) || filter(compositionDrawListVisual2.Visual))));
			return (CompositionTarget.TryHitTestFirst(p, root.CompositionVisual, filter2, (CompositionVisual v) => v is CompositionDrawListVisual) is CompositionDrawListVisual compositionDrawListVisual) ? compositionDrawListVisual.Visual : null;
		}
	}

	/// <inheritdoc />
	public void RecalculateChildren(Visual visual)
	{
		if (!_isDisposed)
		{
			if (_updating)
			{
				throw new InvalidOperationException("Visual was invalidated during the render pass");
			}
			_recalculateChildren.Add(visual);
			QueueUpdate();
		}
	}

	private void UpdateCore()
	{
		_queuedUpdate = false;
		foreach (Visual item in _dirty)
		{
			CompositionDrawListVisual compositionVisual = item.CompositionVisual;
			if (compositionVisual != null)
			{
				item.SynchronizeCompositionProperties();
				try
				{
					item.Render(_recorder);
					compositionVisual.DrawList = _recorder.GetRenderResults();
				}
				finally
				{
					_recorder.Reset();
				}
				item.SynchronizeCompositionChildVisuals();
			}
		}
		foreach (Visual recalculateChild in _recalculateChildren)
		{
			if (!_dirty.Contains(recalculateChild))
			{
				recalculateChild.SynchronizeCompositionChildVisuals();
			}
		}
		_dirty.Clear();
		_recalculateChildren.Clear();
		CompositionTarget.Size = _root.ClientSize;
		CompositionTarget.Scaling = _root.RenderScaling;
		CompositionBatch compositionBatch = _compositor.RequestCompositionBatchCommitAsync();
		if (_queuedSceneInvalidation)
		{
			return;
		}
		_queuedSceneInvalidation = true;
		compositionBatch.Rendered.ContinueWith(delegate
		{
			Dispatcher.UIThread.Post(delegate
			{
				_queuedSceneInvalidation = false;
				SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs(new Rect(_root.ClientSize)));
			}, DispatcherPriority.Input);
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}

	public void TriggerSceneInvalidatedForUnitTests(Rect rect)
	{
		SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs(rect));
	}

	private void Update()
	{
		if (_updating)
		{
			return;
		}
		if (!CompositionTarget.IsEnabled)
		{
			_queuedUpdate = false;
			return;
		}
		_updating = true;
		try
		{
			using (Diagnostic.BeginLayoutRenderPass())
			{
				UpdateCore();
			}
		}
		finally
		{
			_updating = false;
		}
	}

	/// <inheritdoc />
	public void Resized(Size size)
	{
	}

	/// <inheritdoc />
	public void Paint(Rect rect)
	{
		Paint(rect, catchExceptions: true);
	}

	public void Paint(Rect rect, bool catchExceptions)
	{
		if (!_isDisposed)
		{
			QueueUpdate();
			CompositionTarget.RequestRedraw();
			MediaContext.Instance.ImmediateRenderRequested(CompositionTarget, catchExceptions);
		}
	}

	/// <inheritdoc />
	public void Start()
	{
		if (!_isDisposed)
		{
			CompositionTarget.IsEnabled = true;
			if (_dirty.Count > 0 || _recalculateChildren.Count > 0)
			{
				QueueUpdate();
			}
		}
	}

	/// <inheritdoc />
	public void Stop()
	{
		CompositionTarget.IsEnabled = false;
	}

	/// <inheritdoc />
	public ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType)
	{
		return Compositor.TryGetRenderInterfaceFeature(featureType);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			_dirty.Clear();
			_recalculateChildren.Clear();
			SceneInvalidated = null;
			Stop();
			MediaContext.Instance.SyncDisposeCompositionTarget(CompositionTarget);
		}
	}
}
