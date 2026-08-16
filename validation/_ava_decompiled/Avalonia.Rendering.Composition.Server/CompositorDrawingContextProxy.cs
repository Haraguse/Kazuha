using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositorDrawingContextProxy : IDrawingContextImpl, IDisposable, IDrawingContextWithAcrylicLikeSupport, IDrawingContextImplWithEffects
{
	private enum PendingCommandType
	{
		SetTransform,
		PushClip,
		PushOpacity,
		PushOpacityMask,
		PushGeometryClip,
		PushRenderOptions,
		PushTextOptions,
		PushEffect
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct PendingCommandObjectUnion
	{
		[FieldOffset(0)]
		public IEffect? Effect;

		[FieldOffset(0)]
		public IBrush? Mask;

		[FieldOffset(0)]
		public IGeometryImpl? Clip;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct PendingCommandDataUnion
	{
		[FieldOffset(0)]
		public double Opacity;

		[FieldOffset(8)]
		public Rect? NullableOpacityRect;

		[FieldOffset(0)]
		public Matrix Transform;

		[FieldOffset(0)]
		public RenderOptions RenderOptions;

		[FieldOffset(0)]
		public TextOptions TextOptions;

		[FieldOffset(0)]
		public bool IsRoundRect;

		[FieldOffset(4)]
		public RoundedRect RoundRect;

		[FieldOffset(4)]
		public Rect NormalRect;

		[FieldOffset(0)]
		public Rect? EffectClipRect;
	}

	private struct PendingCommand
	{
		public PendingCommandType Type;

		public PendingCommandObjectUnion ObjectUnion;

		public PendingCommandDataUnion DataUnion;
	}

	private readonly IDrawingContextImpl _impl;

	private static readonly ThreadSafeObjectPool<Stack<Matrix>> s_transformStackPool = new ThreadSafeObjectPool<Stack<Matrix>>();

	private Stack<Matrix>? _transformStack = s_transformStackPool.Get();

	private Matrix _reportedTransform = Matrix.Identity;

	private Matrix _effectiveTransform = Matrix.Identity;

	private PooledList<PendingCommand> _commands = new PooledList<PendingCommand>();

	private bool _autoFlush;

	public Matrix? PostTransform { get; set; }

	public Matrix Transform
	{
		get
		{
			return _reportedTransform;
		}
		set
		{
			_reportedTransform = value;
			SetTransform(value);
		}
	}

	public bool AutoFlush
	{
		get
		{
			return _autoFlush;
		}
		set
		{
			_autoFlush = value;
			if (value)
			{
				Flush();
			}
		}
	}

	public CompositorDrawingContextProxy(IDrawingContextImpl impl)
	{
		_impl = impl;
	}

	public void Dispose()
	{
		Flush();
		_commands.Dispose();
		if (_transformStack != null)
		{
			_transformStack.Clear();
		}
		s_transformStackPool.ReturnAndSetNull(ref _transformStack);
	}

	private void SetImplTransform(Matrix m)
	{
		_effectiveTransform = m;
		if (PostTransform.HasValue)
		{
			m *= PostTransform.Value;
		}
		_impl.Transform = m;
	}

	private void SaveTransform()
	{
		_transformStack.Push(_effectiveTransform);
	}

	private void RestoreTransform()
	{
		_reportedTransform = (_effectiveTransform = _transformStack.Pop());
	}

	public void Clear(Color color)
	{
		Flush();
		_impl.Clear(color);
	}

	public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
	{
		Flush();
		_impl.DrawBitmap(source, opacity, sourceRect, destRect);
	}

	public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
	{
		Flush();
		_impl.DrawBitmap(source, opacityMask, opacityMaskRect, destRect);
	}

	public void DrawLine(IPen? pen, Point p1, Point p2)
	{
		Flush();
		_impl.DrawLine(pen, p1, p2);
	}

	public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
	{
		Flush();
		_impl.DrawGeometry(brush, pen, geometry);
	}

	public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default(BoxShadows))
	{
		Flush();
		_impl.DrawRectangle(brush, pen, rect, boxShadows);
	}

	public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region)
	{
		Flush();
		_impl.DrawRegion(brush, pen, region);
	}

	public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
	{
		Flush();
		_impl.DrawEllipse(brush, pen, rect);
	}

	public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
	{
		Flush();
		_impl.DrawGlyphRun(foreground, glyphRun);
	}

	public IDrawingContextLayerImpl CreateLayer(PixelSize size)
	{
		return _impl.CreateLayer(size);
	}

	public void PushClip(Rect clip)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushClip,
			DataUnion = 
			{
				NormalRect = clip
			}
		});
	}

	public void PushClip(RoundedRect clip)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushClip,
			DataUnion = 
			{
				IsRoundRect = true,
				RoundRect = clip
			}
		});
	}

	public void PushClip(IPlatformRenderInterfaceRegion region)
	{
		Flush();
		_impl.PushClip(region);
	}

	public void PopClip()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushClip))
		{
			_impl.PopClip();
			RestoreTransform();
		}
	}

	public void PushLayer(Rect bounds)
	{
		Flush();
		_impl.PushLayer(bounds);
	}

	public void PopLayer()
	{
		Flush();
		_impl.PopLayer();
	}

	public void PushOpacity(double opacity, Rect? bounds)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushOpacity,
			DataUnion = 
			{
				Opacity = opacity,
				NullableOpacityRect = bounds
			}
		});
	}

	public void PopOpacity()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushOpacity))
		{
			_impl.PopOpacity();
			RestoreTransform();
		}
	}

	public void PushOpacityMask(IBrush mask, Rect bounds)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushOpacityMask,
			DataUnion = 
			{
				NormalRect = bounds
			},
			ObjectUnion = 
			{
				Mask = mask
			}
		});
	}

	public void PopOpacityMask()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushOpacityMask))
		{
			_impl.PopOpacityMask();
			RestoreTransform();
		}
	}

	public void PushGeometryClip(IGeometryImpl clip)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushGeometryClip,
			ObjectUnion = 
			{
				Clip = clip
			}
		});
	}

	public void PopGeometryClip()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushGeometryClip))
		{
			_impl.PopGeometryClip();
			RestoreTransform();
		}
	}

	public void PushRenderOptions(RenderOptions renderOptions)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushRenderOptions,
			DataUnion = 
			{
				RenderOptions = renderOptions
			}
		});
	}

	public void PushTextOptions(TextOptions textOptions)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushTextOptions,
			DataUnion = 
			{
				TextOptions = textOptions
			}
		});
	}

	public void PopRenderOptions()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushRenderOptions))
		{
			_impl.PopRenderOptions();
			RestoreTransform();
		}
	}

	public void PopTextOptions()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushTextOptions))
		{
			_impl.PopTextOptions();
			RestoreTransform();
		}
	}

	public object? GetFeature(Type t)
	{
		Flush();
		return _impl.GetFeature(t);
	}

	public void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect)
	{
		Flush();
		if (_impl is IDrawingContextWithAcrylicLikeSupport drawingContextWithAcrylicLikeSupport)
		{
			drawingContextWithAcrylicLikeSupport.DrawRectangle(material, rect);
		}
		else
		{
			_impl.DrawRectangle(new ImmutableSolidColorBrush(material.FallbackColor), null, rect);
		}
	}

	public void PushEffect(Rect? clipRect, IEffect effect)
	{
		AddCommand(new PendingCommand
		{
			Type = PendingCommandType.PushEffect,
			ObjectUnion = 
			{
				Effect = effect
			},
			DataUnion = 
			{
				EffectClipRect = clipRect
			}
		});
	}

	public void PopEffect()
	{
		if (!TryDiscardOrFlush(PendingCommandType.PushEffect))
		{
			if (_impl is IDrawingContextImplWithEffects drawingContextImplWithEffects)
			{
				drawingContextImplWithEffects.PopEffect();
			}
			RestoreTransform();
		}
	}

	public void SetTransform(Matrix m)
	{
		if (_autoFlush)
		{
			SetImplTransform(m);
			return;
		}
		PendingCommand pendingCommand = new PendingCommand
		{
			Type = PendingCommandType.SetTransform,
			DataUnion = 
			{
				Transform = m
			}
		};
		if (_commands.Count > 0 && _commands[_commands.Count - 1].Type == PendingCommandType.SetTransform)
		{
			_commands[_commands.Count - 1] = pendingCommand;
		}
		else
		{
			_commands.Add(pendingCommand);
		}
	}

	private bool TryDiscardOrFlush(PendingCommandType type)
	{
		for (int num = _commands.Count - 1; num >= 0; num--)
		{
			if (_commands[num].Type != PendingCommandType.SetTransform)
			{
				if (_commands[num].Type != type)
				{
					break;
				}
				_commands.RemoveRange(num, _commands.Count - num);
				return true;
			}
		}
		Flush();
		return false;
	}

	private void AddCommand(PendingCommand command)
	{
		if (_autoFlush)
		{
			ExecCommand(ref command);
		}
		else
		{
			_commands.Add(command);
		}
	}

	private void ExecCommand(ref PendingCommand cmd)
	{
		if (cmd.Type == PendingCommandType.SetTransform)
		{
			SetImplTransform(cmd.DataUnion.Transform);
			return;
		}
		SaveTransform();
		if (cmd.Type == PendingCommandType.PushOpacity)
		{
			_impl.PushOpacity(cmd.DataUnion.Opacity, cmd.DataUnion.NullableOpacityRect);
		}
		else if (cmd.Type == PendingCommandType.PushOpacityMask)
		{
			_impl.PushOpacityMask(cmd.ObjectUnion.Mask, cmd.DataUnion.NormalRect);
		}
		else if (cmd.Type == PendingCommandType.PushClip)
		{
			if (cmd.DataUnion.IsRoundRect)
			{
				_impl.PushClip(cmd.DataUnion.RoundRect);
			}
			else
			{
				_impl.PushClip(cmd.DataUnion.NormalRect);
			}
		}
		else if (cmd.Type == PendingCommandType.PushGeometryClip)
		{
			_impl.PushGeometryClip(cmd.ObjectUnion.Clip);
		}
		else if (cmd.Type == PendingCommandType.PushEffect)
		{
			if (_impl is IDrawingContextImplWithEffects drawingContextImplWithEffects)
			{
				drawingContextImplWithEffects.PushEffect(cmd.DataUnion.EffectClipRect, cmd.ObjectUnion.Effect);
			}
		}
		else if (cmd.Type == PendingCommandType.PushRenderOptions)
		{
			_impl.PushRenderOptions(cmd.DataUnion.RenderOptions);
		}
		else if (cmd.Type == PendingCommandType.PushTextOptions)
		{
			_impl.PushTextOptions(cmd.DataUnion.TextOptions);
		}
	}

	public void Flush()
	{
		Span<PendingCommand> span = _commands.AsSpan();
		for (int i = 0; i < span.Length; i++)
		{
			ExecCommand(ref span[i]);
		}
		_commands.Clear();
	}
}
