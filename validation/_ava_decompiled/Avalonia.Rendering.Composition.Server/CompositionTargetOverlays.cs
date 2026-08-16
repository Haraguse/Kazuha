using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositionTargetOverlays
{
	private FpsCounter? _fpsCounter;

	private FrameTimeGraph? _renderTimeGraph;

	private FrameTimeGraph? _compositorUpdateTimeGraph;

	private FrameTimeGraph? _updateTimeGraph;

	private FrameTimeGraph? _layoutTimeGraph;

	private Rect? _oldFpsCounterRect;

	private long _updateStarted;

	private readonly ServerCompositionTarget _target;

	[CompilerGenerated]
	private DiagnosticTextRenderer? _003CDiagnosticTextRenderer_003Ek__BackingField;

	private RendererDebugOverlays DebugOverlays { get; set; }

	private FpsCounter? FpsCounter
	{
		get
		{
			FpsCounter? fpsCounter = _fpsCounter;
			if (fpsCounter == null)
			{
				DiagnosticTextRenderer diagnosticTextRenderer = DiagnosticTextRenderer;
				fpsCounter = (_fpsCounter = ((diagnosticTextRenderer != null) ? new FpsCounter(diagnosticTextRenderer) : null));
			}
			return fpsCounter;
		}
	}

	private FrameTimeGraph? LayoutTimeGraph => _layoutTimeGraph ?? (_layoutTimeGraph = CreateTimeGraph("Layout"));

	private FrameTimeGraph? RenderTimeGraph => _renderTimeGraph ?? (_renderTimeGraph = CreateTimeGraph("Render"));

	private FrameTimeGraph? CompositorUpdateTimeGraph => _compositorUpdateTimeGraph ?? (_compositorUpdateTimeGraph = CreateTimeGraph("GUpdate"));

	private FrameTimeGraph? UpdateTimeGraph => _updateTimeGraph ?? (_updateTimeGraph = CreateTimeGraph("TUpdate"));

	private DiagnosticTextRenderer? DiagnosticTextRenderer
	{
		get
		{
			if (_003CDiagnosticTextRenderer_003Ek__BackingField == null)
			{
				if (AvaloniaLocator.Current.GetService<IFontManagerImpl>() == null)
				{
					return null;
				}
				_003CDiagnosticTextRenderer_003Ek__BackingField = new DiagnosticTextRenderer(Typeface.Default.GlyphTypeface, 12.0);
			}
			return _003CDiagnosticTextRenderer_003Ek__BackingField;
		}
	}

	public bool RequireLayer => DebugOverlays.HasAnyFlag(RendererDebugOverlays.DirtyRects);

	private bool CaptureTiming => (DebugOverlays & RendererDebugOverlays.RenderTimeGraph) != 0;

	public CompositionTargetOverlays(ServerCompositionTarget target)
	{
		_target = target;
	}

	private FrameTimeGraph? CreateTimeGraph(string title)
	{
		DiagnosticTextRenderer diagnosticTextRenderer = DiagnosticTextRenderer;
		if (diagnosticTextRenderer == null)
		{
			return null;
		}
		return new FrameTimeGraph(360, new Size(360.0, 64.0), 16.666666666666668, title, diagnosticTextRenderer);
	}

	public void OnChanged(RendererDebugOverlays debugOverlays)
	{
		DebugOverlays = debugOverlays;
		_oldFpsCounterRect = null;
		if ((DebugOverlays & RendererDebugOverlays.Fps) == 0)
		{
			_fpsCounter?.Reset();
		}
		if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) == 0)
		{
			_layoutTimeGraph?.Reset();
		}
		if ((DebugOverlays & RendererDebugOverlays.RenderTimeGraph) == 0)
		{
			_renderTimeGraph?.Reset();
			_compositorUpdateTimeGraph?.Reset();
			_updateTimeGraph?.Reset();
		}
	}

	public void Draw(IDrawingContextImpl targetContext, bool hasLayer)
	{
		if (DebugOverlays != RendererDebugOverlays.None)
		{
			if (CaptureTiming)
			{
				TimeSpan elapsedTime = StopwatchHelper.GetElapsedTime(_updateStarted);
				RenderTimeGraph?.AddFrameValue(elapsedTime.TotalMilliseconds);
			}
			if (DebugOverlays.HasFlag(RendererDebugOverlays.DirtyRects))
			{
				_target.DirtyRects.Visualize(targetContext);
			}
			targetContext.Transform = Matrix.CreateScale(_target.Scaling, _target.Scaling);
			using ImmediateDrawingContext targetContext2 = new ImmediateDrawingContext(targetContext, ownsImpl: false);
			DrawOverlays(targetContext2, hasLayer, _target.Size);
		}
	}

	public void MarkUpdateCallStart()
	{
		if (CaptureTiming)
		{
			_updateStarted = (CaptureTiming ? Stopwatch.GetTimestamp() : 0);
		}
	}

	public void MarkUpdateCallEnd()
	{
		if (CaptureTiming)
		{
			UpdateTimeGraph?.AddFrameValue(StopwatchHelper.GetElapsedTime(_updateStarted).TotalMilliseconds);
		}
	}

	public void RecordGlobalCompositorUpdateTime(TimeSpan elapsed)
	{
		if (CaptureTiming)
		{
			CompositorUpdateTimeGraph?.AddFrameValue(elapsed.TotalMilliseconds);
		}
	}

	private void DrawOverlays(ImmediateDrawingContext targetContext, bool hasLayer, Size logicalSize)
	{
		if (DebugOverlays.HasFlag(RendererDebugOverlays.Fps))
		{
			string text = ByteSizeHelper.ToString((ulong)((_target.Compositor.BatchMemoryPool.CurrentUsage + _target.Compositor.BatchMemoryPool.CurrentPool) * _target.Compositor.BatchMemoryPool.BufferSize), separate: false);
			string text2 = ByteSizeHelper.ToString((ulong)((_target.Compositor.BatchObjectPool.CurrentUsage + _target.Compositor.BatchObjectPool.CurrentPool) * _target.Compositor.BatchObjectPool.ArraySize * IntPtr.Size), separate: false);
			_oldFpsCounterRect = FpsCounter?.RenderFps(targetContext, FormattableString.Invariant($"M:{text2} / N:{text} V:{_target.VisitedVisuals:0000} R:{_target.RenderedVisuals:0000}"), hasLayer, _oldFpsCounterRect);
		}
		double top = 0.0;
		if (DebugOverlays.HasFlag(RendererDebugOverlays.LayoutTimeGraph))
		{
			DrawTimeGraph(LayoutTimeGraph);
		}
		if (DebugOverlays.HasFlag(RendererDebugOverlays.RenderTimeGraph))
		{
			DrawTimeGraph(RenderTimeGraph);
			DrawTimeGraph(CompositorUpdateTimeGraph);
			DrawTimeGraph(UpdateTimeGraph);
		}
		void DrawTimeGraph(FrameTimeGraph? graph)
		{
			if (graph != null)
			{
				double num = logicalSize.Width - graph.Size.Width - 8.0;
				top += 8.0;
				if (!hasLayer)
				{
					targetContext.FillRectangle(Brushes.White, new Rect(num, top, graph.Size.Width, graph.Size.Height));
				}
				using (targetContext.PushSetTransform(Matrix.CreateTranslation(num, top)))
				{
					graph.Render(targetContext);
				}
				top += graph.Size.Height;
			}
		}
	}

	public void OnLastLayoutPassTimingChanged(LayoutPassTiming lastLayoutPassTiming)
	{
		if ((DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != RendererDebugOverlays.None)
		{
			LayoutTimeGraph?.AddFrameValue(lastLayoutPassTiming.Elapsed.TotalMilliseconds);
		}
	}
}
