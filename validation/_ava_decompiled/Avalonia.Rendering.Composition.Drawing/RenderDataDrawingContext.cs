using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class RenderDataDrawingContext : DrawingContext
{
	private struct PushEntry
	{
		public bool Emitted;

		public int PositionBefore;

		public int PositionAfter;

		public int DepthBefore;
	}

	private readonly Compositor? _compositor;

	private RenderDataStream? _stream;

	private CompositionRenderData? _renderData;

	private HashSet<object>? _resourcesHashSet;

	private Stack<PushEntry>? _pushStack;

	private static readonly ThreadSafeObjectPool<HashSet<object>> s_hashSetPool = new ThreadSafeObjectPool<HashSet<object>>();

	private static readonly ThreadSafeObjectPool<Stack<PushEntry>> s_pushStackPool = new ThreadSafeObjectPool<Stack<PushEntry>>();

	private RenderDataStream Stream => _stream ?? (_stream = new RenderDataStream());

	private CompositionRenderData RenderData => _renderData ?? (_renderData = new CompositionRenderData(_compositor, Stream));

	public RenderDataDrawingContext(Compositor? compositor)
	{
		_compositor = compositor;
	}

	private void AddResource(object? resource)
	{
		if (_compositor != null && resource != null && !(resource is IImmutableBrush) && !(resource is ImmutablePen) && !(resource is ImmutableTransform))
		{
			if (!(resource is ICompositionRenderResource compositionRenderResource))
			{
				throw new InvalidOperationException(resource.GetType().FullName + " can not be used with this DrawingContext");
			}
			if (_resourcesHashSet == null)
			{
				_resourcesHashSet = s_hashSetPool.Get();
			}
			if (_resourcesHashSet.Add(compositionRenderResource))
			{
				compositionRenderResource.AddRefOnCompositor(_compositor);
				RenderData.AddResource(compositionRenderResource);
			}
		}
	}

	private void PushedScope(int positionBefore)
	{
		(_pushStack ?? (_pushStack = s_pushStackPool.Get())).Push(new PushEntry
		{
			Emitted = true,
			PositionBefore = positionBefore,
			PositionAfter = Stream.OpcodeLength,
			DepthBefore = Stream.Depth - 1
		});
	}

	private void PushedNoOpScope()
	{
		(_pushStack ?? (_pushStack = s_pushStackPool.Get())).Push(new PushEntry
		{
			Emitted = false
		});
	}

	private void PopCore()
	{
		PushEntry pushEntry = _pushStack.Pop();
		if (pushEntry.Emitted)
		{
			if (Stream.OpcodeLength == pushEntry.PositionAfter)
			{
				Stream.Rewind(pushEntry.PositionBefore, pushEntry.DepthBefore);
			}
			else
			{
				Stream.Pop();
			}
		}
	}

	protected override void DrawLineCore(IPen? pen, Point p1, Point p2)
	{
		if (pen != null)
		{
			AddResource(pen);
			Stream.DrawLine(pen.GetServer(_compositor), pen, p1, p2);
		}
	}

	protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
	{
		if (brush != null || pen != null)
		{
			AddResource(brush);
			AddResource(pen);
			Stream.DrawGeometry(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, geometry);
		}
	}

	protected override void DrawGeometryCore(IBrush? brush, IPen? pen, Geometry geometry)
	{
		if (brush != null || pen != null)
		{
			AddResource(brush);
			AddResource(pen);
			AddResource(geometry);
			Stream.DrawGeometry(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, geometry.GetServer(_compositor));
		}
	}

	protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default(BoxShadows))
	{
		if (!rrect.IsEmpty() && (brush != null || pen != null || !(boxShadows == default(BoxShadows))))
		{
			AddResource(brush);
			AddResource(pen);
			Stream.DrawRectangle(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, rrect, boxShadows);
		}
	}

	protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
	{
		if (!rect.IsEmpty() && (brush != null || pen != null))
		{
			AddResource(brush);
			AddResource(pen);
			Stream.DrawEllipse(brush.GetServer(_compositor), pen.GetServer(_compositor), pen, rect);
		}
	}

	public override void Custom(ICustomDrawOperation custom)
	{
		Stream.DrawCustom(custom);
	}

	public override void DrawGlyphRun(IBrush? foreground, GlyphRun? glyphRun)
	{
		if (foreground != null && glyphRun != null)
		{
			AddResource(foreground);
			Stream.DrawGlyphRun(foreground.GetServer(_compositor), glyphRun.PlatformImpl.Clone());
		}
	}

	internal override void DrawBitmap(IRef<IBitmapImpl>? source, double opacity, Rect sourceRect, Rect destRect)
	{
		if (source != null && !sourceRect.IsEmpty() && !destRect.IsEmpty())
		{
			Stream.DrawBitmap(source.Clone(), opacity, sourceRect, destRect);
		}
	}

	protected override void PushClipCore(RoundedRect rect)
	{
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushClip(rect);
		PushedScope(opcodeLength);
	}

	protected override void PushClipCore(Rect rect)
	{
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushClip(new RoundedRect(rect));
		PushedScope(opcodeLength);
	}

	protected override void PushGeometryClipCore(Geometry? clip)
	{
		if (clip == null)
		{
			PushedNoOpScope();
			return;
		}
		AddResource(clip);
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushGeometryClip(clip.GetServer(_compositor));
		PushedScope(opcodeLength);
	}

	protected override void PushOpacityCore(double opacity)
	{
		if (opacity == 1.0)
		{
			PushedNoOpScope();
			return;
		}
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushOpacity(opacity);
		PushedScope(opcodeLength);
	}

	protected override void PushOpacityMaskCore(IBrush? mask, Rect bounds)
	{
		if (mask == null)
		{
			PushedNoOpScope();
			return;
		}
		AddResource(mask);
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushOpacityMask(mask.GetServer(_compositor), bounds);
		PushedScope(opcodeLength);
	}

	protected override void PushTransformCore(Matrix matrix)
	{
		if (matrix.IsIdentity)
		{
			PushedNoOpScope();
			return;
		}
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushTransform(matrix);
		PushedScope(opcodeLength);
	}

	protected override void PushRenderOptionsCore(RenderOptions renderOptions)
	{
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushRenderOptions(renderOptions);
		PushedScope(opcodeLength);
	}

	protected override void PushTextOptionsCore(TextOptions textOptions)
	{
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushTextOptions(textOptions);
		PushedScope(opcodeLength);
	}

	protected override void PushEffectCore(IEffect effect, Rect bounds)
	{
		int opcodeLength = Stream.OpcodeLength;
		Stream.PushEffect(effect.ToImmutable(), bounds.Inflate(effect.GetEffectOutputPadding()));
		PushedScope(opcodeLength);
	}

	protected override void PopClipCore()
	{
		PopCore();
	}

	protected override void PopGeometryClipCore()
	{
		PopCore();
	}

	protected override void PopOpacityCore()
	{
		PopCore();
	}

	protected override void PopOpacityMaskCore()
	{
		PopCore();
	}

	protected override void PopTransformCore()
	{
		PopCore();
	}

	protected override void PopRenderOptionsCore()
	{
		PopCore();
	}

	protected override void PopTextOptionsCore()
	{
		PopCore();
	}

	protected override void PopEffectCore()
	{
		PopCore();
	}

	private void FlushStack()
	{
		while (true)
		{
			Stack<PushEntry> pushStack = _pushStack;
			if (pushStack != null && pushStack.Count > 0)
			{
				PopCore();
				continue;
			}
			break;
		}
	}

	public CompositionRenderData? GetRenderResults()
	{
		FlushStack();
		CompositionRenderData compositionRenderData = _renderData;
		if (compositionRenderData == null)
		{
			RenderDataStream stream = _stream;
			if (stream == null || stream.OpcodeLength <= 0)
			{
				_stream?.Dispose();
				_stream = null;
				return null;
			}
			compositionRenderData = new CompositionRenderData(_compositor, _stream);
		}
		_renderData = null;
		_stream = null;
		_resourcesHashSet?.Clear();
		_compositor.RegisterForSerialization(compositionRenderData);
		return compositionRenderData;
	}

	public ImmediateRenderDataSceneBrushContent? GetImmediateSceneBrushContent(ITileBrush brush, Rect? rect, bool useScalableRasterization)
	{
		FlushStack();
		RenderDataStream stream = _stream;
		if (stream == null || stream.OpcodeLength <= 0)
		{
			_stream?.Dispose();
			_stream = null;
			return null;
		}
		RenderDataStream stream2 = _stream;
		_stream = null;
		return new ImmediateRenderDataSceneBrushContent(brush, stream2, rect, useScalableRasterization);
	}

	public void Reset()
	{
		if (_renderData != null)
		{
			_renderData.Dispose();
			_renderData = null;
		}
		else
		{
			_stream?.Dispose();
		}
		_stream = null;
		_pushStack?.Clear();
		_resourcesHashSet?.Clear();
	}

	protected override void DisposeCore()
	{
		Reset();
		if (_resourcesHashSet != null)
		{
			s_hashSetPool.ReturnAndSetNull(ref _resourcesHashSet);
		}
		if (_pushStack != null)
		{
			s_pushStackPool.ReturnAndSetNull(ref _pushStack);
		}
	}
}
