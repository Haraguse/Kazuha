using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of the <see cref="T:Avalonia.Rendering.Composition.CompositionTarget" />
/// That's the place where we update visual transforms, track dirty rects and actually do rendering
/// </summary>
internal class ServerCompositionTarget : ServerObject, IDisposable
{
	private readonly ServerCompositor _compositor;

	private readonly Func<IEnumerable<IPlatformRenderSurface>> _surfaces;

	private CompositionTargetOverlays _overlays;

	private static long s_nextId = 1L;

	private IRenderTarget? _renderTarget;

	private PixelSize _layerSize;

	private IDrawingContextLayerImpl? _layer;

	private bool _updateRequested;

	private bool _redrawRequested;

	private bool _fullRedrawRequested;

	private bool _disposed;

	private readonly HashSet<ServerCompositionVisual> _attachedVisuals = new HashSet<ServerCompositionVisual>();

	private ServerCompositionVisual? _root;

	internal static readonly CompositionProperty<ServerCompositionVisual?> s_IdOfRootProperty = CompositionProperty.Register<ServerCompositionTarget, ServerCompositionVisual>("Root", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._root, delegate(SimpleServerObject obj, ServerCompositionVisual? v)
	{
		((ServerCompositionTarget)obj)._root = v;
	}, null);

	private bool _isEnabled;

	internal static readonly CompositionProperty<bool> s_IdOfIsEnabledProperty = CompositionProperty.Register<ServerCompositionTarget, bool>("IsEnabled", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._isEnabled, delegate(SimpleServerObject obj, bool v)
	{
		((ServerCompositionTarget)obj)._isEnabled = v;
	}, (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._isEnabled);

	private RendererDebugOverlays _debugOverlays;

	internal static readonly CompositionProperty<RendererDebugOverlays> s_IdOfDebugOverlaysProperty = CompositionProperty.Register<ServerCompositionTarget, RendererDebugOverlays>("DebugOverlays", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._debugOverlays, delegate(SimpleServerObject obj, RendererDebugOverlays v)
	{
		((ServerCompositionTarget)obj)._debugOverlays = v;
	}, null);

	private LayoutPassTiming _lastLayoutPassTiming;

	internal static readonly CompositionProperty<LayoutPassTiming> s_IdOfLastLayoutPassTimingProperty = CompositionProperty.Register<ServerCompositionTarget, LayoutPassTiming>("LastLayoutPassTiming", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._lastLayoutPassTiming, delegate(SimpleServerObject obj, LayoutPassTiming v)
	{
		((ServerCompositionTarget)obj)._lastLayoutPassTiming = v;
	}, null);

	private double _scaling;

	internal static readonly CompositionProperty<double> s_IdOfScalingProperty = CompositionProperty.Register<ServerCompositionTarget, double>("Scaling", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._scaling, delegate(SimpleServerObject obj, double v)
	{
		((ServerCompositionTarget)obj)._scaling = v;
	}, (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._scaling);

	private Size _size;

	internal static readonly CompositionProperty<Size> s_IdOfSizeProperty = CompositionProperty.Register<ServerCompositionTarget, Size>("Size", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._size, delegate(SimpleServerObject obj, Size v)
	{
		((ServerCompositionTarget)obj)._size = v;
	}, null);

	private CompositionTransparencyLevel _transparencyLevel;

	internal static readonly CompositionProperty<CompositionTransparencyLevel> s_IdOfTransparencyLevelProperty = CompositionProperty.Register<ServerCompositionTarget, CompositionTransparencyLevel>("TransparencyLevel", (SimpleServerObject obj) => ((ServerCompositionTarget)obj)._transparencyLevel, delegate(SimpleServerObject obj, CompositionTransparencyLevel v)
	{
		((ServerCompositionTarget)obj)._transparencyLevel = v;
	}, null);

	public IDirtyRectTracker DirtyRects { get; }

	public long Id { get; }

	public ulong Revision { get; private set; }

	public ICompositionTargetDebugEvents? DebugEvents { get; set; }

	public int RenderedVisuals { get; set; }

	public int VisitedVisuals { get; set; }

	internal PixelSize PixelSize => PixelSize.FromSizeCeiling(Size, Scaling);

	/// <summary>
	/// Returns true if the target is enabled and has pending work but its render target was not ready.
	/// </summary>
	internal bool IsWaitingForReadyRenderTarget { get; private set; }

	/// <summary>
	/// Returns true if the target's render target is waiting for a render loop wakeup
	/// (i.e. the platform will call Wakeup() when ready, no need to keep polling).
	/// </summary>
	internal bool IsWaitingForRenderLoopWakeup { get; private set; }

	public ServerCompositionVisual? Root
	{
		get
		{
			return _root;
		}
		set
		{
			bool flag = false;
			if (_root != value)
			{
				flag = true;
			}
			SetValue(s_IdOfRootProperty, ref _root, value);
		}
	}

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			bool flag = false;
			if (_isEnabled != value)
			{
				flag = true;
			}
			SetValue(s_IdOfIsEnabledProperty, ref _isEnabled, value);
			if (flag)
			{
				OnIsEnabledChanged();
			}
		}
	}

	public RendererDebugOverlays DebugOverlays
	{
		get
		{
			return _debugOverlays;
		}
		set
		{
			bool flag = false;
			if (_debugOverlays != value)
			{
				flag = true;
			}
			SetValue(s_IdOfDebugOverlaysProperty, ref _debugOverlays, value);
			if (flag)
			{
				OnDebugOverlaysChanged();
			}
		}
	}

	public LayoutPassTiming LastLayoutPassTiming
	{
		get
		{
			return _lastLayoutPassTiming;
		}
		set
		{
			bool flag = false;
			if (_lastLayoutPassTiming != value)
			{
				flag = true;
			}
			SetValue(s_IdOfLastLayoutPassTimingProperty, ref _lastLayoutPassTiming, value);
			if (flag)
			{
				OnLastLayoutPassTimingChanged();
			}
		}
	}

	public double Scaling
	{
		get
		{
			return _scaling;
		}
		set
		{
			bool flag = false;
			if (_scaling != value)
			{
				flag = true;
			}
			SetValue(s_IdOfScalingProperty, ref _scaling, value);
		}
	}

	public Size Size
	{
		get
		{
			return _size;
		}
		set
		{
			bool flag = false;
			if (_size != value)
			{
				flag = true;
			}
			SetValue(s_IdOfSizeProperty, ref _size, value);
		}
	}

	public CompositionTransparencyLevel TransparencyLevel
	{
		get
		{
			return _transparencyLevel;
		}
		set
		{
			bool flag = false;
			if (_transparencyLevel != value)
			{
				flag = true;
			}
			SetValue(s_IdOfTransparencyLevelProperty, ref _transparencyLevel, value);
		}
	}

	public ServerCompositionTarget(ServerCompositor compositor, Func<IEnumerable<IPlatformRenderSurface>> surfaces)
		: base(compositor)
	{
		_compositor = compositor;
		_surfaces = surfaces;
		_overlays = new CompositionTargetOverlays(this);
		IPlatformRenderInterface service = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
		if (service != null && service.SupportsRegions && compositor.Options.UseRegionDirtyRectClipping == true)
		{
			int num = compositor.Options.MaxDirtyRects ?? 8;
			IDirtyRectTracker dirtyRectTracker2;
			if (num > 0)
			{
				IDirtyRectTracker dirtyRectTracker = new MultiDirtyRectTracker(service, num, compositor.Options.DirtyRectMergeEagerness ?? 1000.0);
				dirtyRectTracker2 = dirtyRectTracker;
			}
			else
			{
				IDirtyRectTracker dirtyRectTracker = new RegionDirtyRectTracker(service);
				dirtyRectTracker2 = dirtyRectTracker;
			}
			DirtyRects = dirtyRectTracker2;
		}
		if (DirtyRects == null)
		{
			DirtyRects = new SingleDirtyRectTracker();
		}
		Id = Interlocked.Increment(ref s_nextId);
	}

	public void Update(TimeSpan diagnosticsCompositorGlobalUpdateElapsedTime = default(TimeSpan))
	{
		if (_disposed)
		{
			base.Compositor.RemoveCompositionTarget(this);
		}
		else
		{
			if (Root == null)
			{
				return;
			}
			_overlays.RecordGlobalCompositorUpdateTime(diagnosticsCompositorGlobalUpdateElapsedTime);
			_overlays.MarkUpdateCallStart();
			using (Diagnostic.BeginCompositorUpdatePass())
			{
				Matrix transform = Matrix.CreateScale(Scaling, Scaling);
				IDirtyRectCollector dirtyRectCollector;
				if (DebugEvents == null)
				{
					IDirtyRectCollector dirtyRects = DirtyRects;
					dirtyRectCollector = dirtyRects;
				}
				else
				{
					IDirtyRectCollector dirtyRects = new DebugEventsDirtyRectCollectorProxy(DirtyRects, DebugEvents);
					dirtyRectCollector = dirtyRects;
				}
				IDirtyRectCollector tracker = dirtyRectCollector;
				Root.UpdateRoot(tracker, transform, new LtrbRect(0.0, 0.0, PixelSize.Width, PixelSize.Height));
				_updateRequested = false;
				_overlays.MarkUpdateCallEnd();
			}
		}
	}

	public void Render()
	{
		IsWaitingForReadyRenderTarget = false;
		IsWaitingForRenderLoopWakeup = false;
		if (_disposed || Root == null)
		{
			return;
		}
		IRenderTarget? renderTarget = _renderTarget;
		if (renderTarget != null && renderTarget.PlatformRenderTargetState.IsCorrupted)
		{
			_layer?.Dispose();
			_layer = null;
			_renderTarget.Dispose();
			_renderTarget = null;
			_redrawRequested = true;
		}
		try
		{
			if (_renderTarget == null)
			{
				if (!_compositor.IsReadyToCreateRenderTarget(_surfaces()))
				{
					IsWaitingForReadyRenderTarget = IsEnabled;
					return;
				}
				_renderTarget = _compositor.CreateRenderTarget(_surfaces());
			}
		}
		catch (RenderTargetNotReadyException)
		{
			IsWaitingForReadyRenderTarget = IsEnabled;
			return;
		}
		catch (RenderTargetCorruptedException)
		{
			return;
		}
		if (DirtyRects.IsEmpty && !_redrawRequested && !_updateRequested)
		{
			return;
		}
		_redrawRequested |= !DirtyRects.IsEmpty;
		if (!_redrawRequested)
		{
			return;
		}
		if (!_renderTarget.PlatformRenderTargetState.IsReady)
		{
			IsWaitingForReadyRenderTarget = IsEnabled;
			IsWaitingForRenderLoopWakeup = IsEnabled && _renderTarget.PlatformRenderTargetState.WillWakeUpRenderLoopWhenReady;
			return;
		}
		bool flag = _overlays.RequireLayer || !_renderTarget.Properties.RetainsPreviousFrameContents || !_renderTarget.Properties.IsSuitableForDirectRendering;
		IDrawingContextImpl drawingContextImpl;
		RenderTargetDrawingContextProperties properties;
		try
		{
			drawingContextImpl = _renderTarget.CreateDrawingContext(new IRenderTarget.RenderTargetSceneInfo(PixelSize, Scaling, Size, TransparencyLevel), out properties);
		}
		catch (RenderTargetNotReadyException)
		{
			IsWaitingForReadyRenderTarget = IsEnabled;
			return;
		}
		catch (RenderTargetCorruptedException)
		{
			return;
		}
		using (drawingContextImpl)
		{
			using (Diagnostic.BeginCompositorRenderPass())
			{
				bool flag2 = false;
				if (flag && (PixelSize != _layerSize || _layer == null || _layer.IsCorrupted))
				{
					_layer?.Dispose();
					_layer = null;
					_layer = drawingContextImpl.CreateLayer(PixelSize);
					_layerSize = PixelSize;
					flag2 = true;
				}
				else if (!flag)
				{
					_layer?.Dispose();
					_layer = null;
				}
				if (_fullRedrawRequested || (!flag && !properties.PreviousFrameIsRetained))
				{
					_fullRedrawRequested = false;
					flag2 = true;
				}
				LtrbRect ltrbRect = new LtrbRect(0.0, 0.0, PixelSize.Width, PixelSize.Height);
				if (flag2)
				{
					DirtyRects.Initialize(ltrbRect);
					DirtyRects.AddRect(ltrbRect);
				}
				if (!DirtyRects.IsEmpty)
				{
					DirtyRects.FinalizeFrame(ltrbRect);
					if (_layer != null)
					{
						using (IDrawingContextImpl context = _layer.CreateDrawingContext())
						{
							RenderRootToContextWithClip(context, Root);
						}
						drawingContextImpl.Clear(Colors.Transparent);
						drawingContextImpl.Transform = Matrix.Identity;
						if (_layer.CanBlit)
						{
							_layer.Blit(drawingContextImpl);
						}
						else
						{
							Rect rect = new PixelRect(default(PixelPoint), PixelSize).ToRect(1.0);
							drawingContextImpl.DrawBitmap(_layer, 1.0, rect, rect);
						}
						_overlays.Draw(drawingContextImpl, hasLayer: true);
					}
					else
					{
						RenderRootToContextWithClip(drawingContextImpl, Root);
						_overlays.Draw(drawingContextImpl, hasLayer: false);
					}
				}
				RenderedVisuals = 0;
				VisitedVisuals = 0;
				_redrawRequested = false;
				DirtyRects.Initialize(ltrbRect);
			}
		}
	}

	private void RenderRootToContextWithClip(IDrawingContextImpl context, ServerCompositionVisual root)
	{
		bool valueOrDefault = base.Compositor.Options.UseSaveLayerRootClip == true;
		using (DirtyRects.BeginDraw(context))
		{
			context.Clear(Colors.Transparent);
			if (valueOrDefault)
			{
				context.PushLayer(DirtyRects.CombinedRect.ToRect());
			}
			context.Transform = Matrix.CreateScale(Scaling, Scaling);
			(VisitedVisuals, RenderedVisuals) = root.Render(context, new LtrbRect(0.0, 0.0, PixelSize.Width, PixelSize.Height), DirtyRects);
			if (DebugEvents != null)
			{
				DebugEvents.RenderedVisuals = RenderedVisuals;
				DebugEvents.VisitedVisuals = VisitedVisuals;
			}
			if (valueOrDefault)
			{
				context.PopLayer();
			}
		}
	}

	public void RequestUpdate()
	{
		_updateRequested = true;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			ResetRenderTarget();
			_compositor.RemoveCompositionTarget(this);
		}
	}

	public void ResetRenderTarget()
	{
		if (_layer == null && _renderTarget == null)
		{
			return;
		}
		try
		{
			using (_compositor.RenderInterface.EnsureCurrent())
			{
				if (_layer != null)
				{
					_layer.Dispose();
					_layer = null;
				}
				_renderTarget?.Dispose();
				_renderTarget = null;
			}
		}
		catch (Exception propertyValue)
		{
			Logger.TryGet(LogEventLevel.Error, "Visual")?.Log(this, "Unable to make the render interface current: {Error}", propertyValue);
			_layer = null;
			_renderTarget = null;
		}
	}

	public void AddVisual(ServerCompositionVisual visual)
	{
		if (_attachedVisuals.Add(visual) && IsEnabled)
		{
			visual.Activate();
		}
	}

	public void RemoveVisual(ServerCompositionVisual visual)
	{
		if (_attachedVisuals.Remove(visual) && IsEnabled)
		{
			visual.Deactivate();
		}
	}

	public void RequestFullRedraw()
	{
		_redrawRequested = true;
	}

	private void DeserializeChangesExtra(BatchStreamReader c)
	{
		_redrawRequested = true;
		_fullRedrawRequested = true;
	}

	private void OnIsEnabledChanged()
	{
		if (IsEnabled)
		{
			_compositor.AddCompositionTarget(this);
			{
				foreach (ServerCompositionVisual attachedVisual in _attachedVisuals)
				{
					attachedVisual.Activate();
				}
				return;
			}
		}
		_compositor.RemoveCompositionTarget(this);
		foreach (ServerCompositionVisual attachedVisual2 in _attachedVisuals)
		{
			attachedVisual2.Deactivate();
		}
	}

	private void OnDebugOverlaysChanged()
	{
		_fullRedrawRequested = true;
		_overlays.OnChanged(DebugOverlays);
	}

	private void OnLastLayoutPassTimingChanged()
	{
		_overlays.OnLastLayoutPassTimingChanged(LastLayoutPassTiming);
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		DeserializeChangesExtra(reader);
		CompositionTargetChangedFields num = reader.Read<CompositionTargetChangedFields>();
		if ((num & CompositionTargetChangedFields.Root) == CompositionTargetChangedFields.Root)
		{
			Root = reader.ReadObject<ServerCompositionVisual>();
		}
		if ((num & CompositionTargetChangedFields.IsEnabled) == CompositionTargetChangedFields.IsEnabled)
		{
			IsEnabled = reader.Read<bool>();
		}
		if ((num & CompositionTargetChangedFields.DebugOverlays) == CompositionTargetChangedFields.DebugOverlays)
		{
			DebugOverlays = reader.Read<RendererDebugOverlays>();
		}
		if ((num & CompositionTargetChangedFields.LastLayoutPassTiming) == CompositionTargetChangedFields.LastLayoutPassTiming)
		{
			LastLayoutPassTiming = reader.Read<LayoutPassTiming>();
		}
		if ((num & CompositionTargetChangedFields.Scaling) == CompositionTargetChangedFields.Scaling)
		{
			Scaling = reader.Read<double>();
		}
		if ((num & CompositionTargetChangedFields.Size) == CompositionTargetChangedFields.Size)
		{
			Size = reader.Read<Size>();
		}
		if ((num & CompositionTargetChangedFields.TransparencyLevel) == CompositionTargetChangedFields.TransparencyLevel)
		{
			TransparencyLevel = reader.Read<CompositionTransparencyLevel>();
		}
	}

	public override CompositionProperty? GetCompositionProperty(string name)
	{
		if (name == "IsEnabled")
		{
			return s_IdOfIsEnabledProperty;
		}
		if (name == "Scaling")
		{
			return s_IdOfScalingProperty;
		}
		return base.GetCompositionProperty(name);
	}
}
