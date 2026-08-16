using System;
using System.Buffers;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal class RenderDataStream : IDisposable
{
	internal struct BoundsScope
	{
		public Rect? SavedBounds;

		public bool IsTransform;

		public Matrix Matrix;

		public Thickness EffectPadding;
	}

	internal struct BoundsVisitor : IRenderDataVisitor<BoundsScope>
	{
		public Rect? Current;

		public bool StopVisiting => false;

		public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
		{
			if (serverPen != null)
			{
				Current = Rect.Union(Current, LineBoundsHelper.CalculateBounds(p1, p2, serverPen));
			}
		}

		public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect, BoxShadows boxShadows)
		{
			Rect value = boxShadows.TransformBounds(rect.Rect).Inflate((serverPen?.Thickness ?? 0.0) / 2.0);
			Current = Rect.Union(Current, value);
		}

		public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
		{
			Current = Rect.Union(Current, rect.Inflate(serverPen?.Thickness ?? 0.0));
		}

		public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
		{
			Current = Rect.Union(Current, geometry?.GetRenderBounds(serverPen) ?? default(Rect));
		}

		public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
		{
			Current = Rect.Union(Current, (glyphRun?.Item?.Bounds).GetValueOrDefault());
		}

		public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
		{
			Current = Rect.Union(Current, destRect);
		}

		public void OnDrawCustom(ICustomDrawOperation? operation)
		{
			Current = Rect.Union(Current, operation?.Bounds);
		}

		private BoundsScope EnterChildScope(bool isTransform = false, Matrix matrix = default(Matrix), Thickness effectPadding = default(Thickness))
		{
			BoundsScope result = new BoundsScope
			{
				SavedBounds = Current,
				IsTransform = isTransform,
				Matrix = matrix,
				EffectPadding = effectPadding
			};
			Current = null;
			return result;
		}

		public BoundsScope OnPushClip(RoundedRect clip)
		{
			return EnterChildScope();
		}

		public BoundsScope OnPushGeometryClip(IGeometryImpl? geometry)
		{
			return EnterChildScope();
		}

		public BoundsScope OnPushOpacity(double opacity)
		{
			return EnterChildScope();
		}

		public BoundsScope OnPushOpacityMask(IBrush? brush, Rect bounds)
		{
			return EnterChildScope();
		}

		public BoundsScope OnPushTransform(Matrix matrix)
		{
			return EnterChildScope(isTransform: true, matrix);
		}

		public BoundsScope OnPushRenderOptions(RenderOptions options)
		{
			return EnterChildScope();
		}

		public BoundsScope OnPushTextOptions(TextOptions options)
		{
			return EnterChildScope();
		}

		public BoundsScope OnPushEffect(IEffect? effect, Rect bounds)
		{
			Thickness effectOutputPadding = effect.GetEffectOutputPadding();
			return EnterChildScope(isTransform: false, default(Matrix), effectOutputPadding);
		}

		public void OnPop(in BoundsScope scope)
		{
			Rect? rect = Current;
			if (scope.IsTransform)
			{
				rect = rect?.TransformToAABB(scope.Matrix);
			}
			else if (rect.HasValue && !scope.EffectPadding.Equals(default(Thickness)))
			{
				rect = rect.Value.Inflate(scope.EffectPadding);
			}
			Current = Rect.Union(scope.SavedBounds, rect);
		}

		void IRenderDataVisitor<BoundsScope>.OnPop(in BoundsScope scope)
		{
			OnPop(in scope);
		}
	}

	internal struct HitTestScope
	{
		public bool SavedLive;

		public bool RestorePoint;

		public Point SavedPoint;
	}

	internal struct HitTestVisitor : IRenderDataVisitor<HitTestScope>
	{
		public bool HitFound;

		public Point Current;

		public bool Live;

		public bool StopVisiting { get; private set; }

		public HitTestVisitor(Point point)
		{
			StopVisiting = false;
			HitFound = false;
			Current = point;
			Live = true;
		}

		private void Hit()
		{
			HitFound = true;
			StopVisiting = true;
		}

		public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
		{
			if (Live && HitTestLine(clientPen, p1, p2, Current))
			{
				Hit();
			}
		}

		public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect, BoxShadows boxShadows)
		{
			if (Live && HitTestRectangle(serverBrush, clientPen, rect, Current))
			{
				Hit();
			}
		}

		public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
		{
			if (Live && HitTestEllipse(serverBrush, clientPen, rect, Current))
			{
				Hit();
			}
		}

		public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
		{
			if (Live && geometry != null && ((serverBrush != null && geometry.FillContains(Current)) || (clientPen != null && geometry.StrokeContains(clientPen, Current))))
			{
				Hit();
			}
		}

		public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
		{
			if (Live && glyphRun != null && glyphRun.Item.Bounds.ContainsExclusive(Current))
			{
				Hit();
			}
		}

		public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
		{
			if (Live && destRect.Contains(Current))
			{
				Hit();
			}
		}

		public void OnDrawCustom(ICustomDrawOperation? operation)
		{
			if (Live && operation != null && operation.HitTest(Current))
			{
				Hit();
			}
		}

		public HitTestScope OnPushClip(RoundedRect clip)
		{
			HitTestScope result = new HitTestScope
			{
				SavedLive = Live
			};
			if (Live && !clip.Rect.Contains(Current))
			{
				Live = false;
			}
			return result;
		}

		public HitTestScope OnPushGeometryClip(IGeometryImpl? geometry)
		{
			HitTestScope result = new HitTestScope
			{
				SavedLive = Live
			};
			if (Live && geometry != null && !geometry.FillContains(Current))
			{
				Live = false;
			}
			return result;
		}

		public HitTestScope OnPushOpacity(double opacity)
		{
			return new HitTestScope
			{
				SavedLive = Live
			};
		}

		public HitTestScope OnPushOpacityMask(IBrush? brush, Rect bounds)
		{
			return new HitTestScope
			{
				SavedLive = Live
			};
		}

		public HitTestScope OnPushTransform(Matrix matrix)
		{
			HitTestScope result = new HitTestScope
			{
				SavedLive = Live
			};
			if (Live)
			{
				if (matrix.TryInvert(out var inverted))
				{
					result.RestorePoint = true;
					result.SavedPoint = Current;
					Current = Current.Transform(inverted);
				}
				else
				{
					Live = false;
				}
			}
			return result;
		}

		public HitTestScope OnPushRenderOptions(RenderOptions options)
		{
			return new HitTestScope
			{
				SavedLive = Live
			};
		}

		public HitTestScope OnPushTextOptions(TextOptions options)
		{
			return new HitTestScope
			{
				SavedLive = Live
			};
		}

		public HitTestScope OnPushEffect(IEffect? effect, Rect bounds)
		{
			return new HitTestScope
			{
				SavedLive = Live
			};
		}

		public void OnPop(in HitTestScope scope)
		{
			Live = scope.SavedLive;
			if (scope.RestorePoint)
			{
				Current = scope.SavedPoint;
			}
		}

		void IRenderDataVisitor<HitTestScope>.OnPop(in HitTestScope scope)
		{
			OnPop(in scope);
		}
	}

	internal struct ReplayScope
	{
		public RenderDataOpcode Kind;

		public bool Active;

		public Matrix SavedTransform;
	}

	internal struct ReplayVisitor(IDrawingContextImpl context) : IRenderDataVisitor<ReplayScope>
	{
		private readonly IDrawingContextImpl _context = context;

		public bool StopVisiting => false;

		public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
		{
			_context.DrawLine(serverPen, p1, p2);
		}

		public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect, BoxShadows boxShadows)
		{
			_context.DrawRectangle(serverBrush, serverPen, rect, boxShadows);
		}

		public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
		{
			_context.DrawEllipse(serverBrush, serverPen, rect);
		}

		public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
		{
			if (geometry != null)
			{
				_context.DrawGeometry(serverBrush, serverPen, geometry);
			}
		}

		public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
		{
			if (glyphRun != null)
			{
				_context.DrawGlyphRun(serverBrush, glyphRun.Item);
			}
		}

		public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
		{
			if (bitmap != null)
			{
				_context.DrawBitmap(bitmap.Item, opacity, sourceRect, destRect);
			}
		}

		public void OnDrawCustom(ICustomDrawOperation? operation)
		{
			operation?.Render(new ImmediateDrawingContext(_context, ownsImpl: false));
		}

		public ReplayScope OnPushClip(RoundedRect clip)
		{
			_context.PushClip(clip);
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushClip,
				Active = true
			};
		}

		public ReplayScope OnPushGeometryClip(IGeometryImpl? geometry)
		{
			if (geometry != null)
			{
				_context.PushGeometryClip(geometry);
			}
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushGeometryClip,
				Active = (geometry != null)
			};
		}

		public ReplayScope OnPushOpacity(double opacity)
		{
			if (opacity != 1.0)
			{
				_context.PushOpacity(opacity, null);
			}
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushOpacity,
				Active = (opacity != 1.0)
			};
		}

		public ReplayScope OnPushOpacityMask(IBrush? brush, Rect bounds)
		{
			if (brush != null)
			{
				_context.PushOpacityMask(brush, bounds);
			}
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushOpacityMask,
				Active = (brush != null)
			};
		}

		public ReplayScope OnPushTransform(Matrix matrix)
		{
			Matrix transform = _context.Transform;
			_context.Transform = matrix * transform;
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushTransform,
				Active = true,
				SavedTransform = transform
			};
		}

		public ReplayScope OnPushRenderOptions(RenderOptions options)
		{
			_context.PushRenderOptions(options);
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushRenderOptions,
				Active = true
			};
		}

		public ReplayScope OnPushTextOptions(TextOptions options)
		{
			_context.PushTextOptions(options);
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushTextOptions,
				Active = true
			};
		}

		public ReplayScope OnPushEffect(IEffect? effect, Rect bounds)
		{
			bool active = false;
			if (effect != null && _context is IDrawingContextImplWithEffects drawingContextImplWithEffects)
			{
				drawingContextImplWithEffects.PushEffect(bounds, effect);
				active = true;
			}
			return new ReplayScope
			{
				Kind = RenderDataOpcode.PushEffect,
				Active = active
			};
		}

		public void OnPop(in ReplayScope scope)
		{
			if (scope.Active)
			{
				switch (scope.Kind)
				{
				case RenderDataOpcode.PushClip:
					_context.PopClip();
					break;
				case RenderDataOpcode.PushGeometryClip:
					_context.PopGeometryClip();
					break;
				case RenderDataOpcode.PushOpacity:
					_context.PopOpacity();
					break;
				case RenderDataOpcode.PushOpacityMask:
					_context.PopOpacityMask();
					break;
				case RenderDataOpcode.PushTransform:
					_context.Transform = scope.SavedTransform;
					break;
				case RenderDataOpcode.PushRenderOptions:
					_context.PopRenderOptions();
					break;
				case RenderDataOpcode.PushTextOptions:
					_context.PopTextOptions();
					break;
				case RenderDataOpcode.PushEffect:
					((IDrawingContextImplWithEffects)_context).PopEffect();
					break;
				}
			}
		}

		void IRenderDataVisitor<ReplayScope>.OnPop(in ReplayScope scope)
		{
			OnPop(in scope);
		}
	}

	private RenderDataWriter _writer;

	private RenderDataResources _resources;

	private int _depth;

	private int _maxDepth;

	private const int MaxStackScopeDepth = 64;

	public ReadOnlySpan<byte> Opcodes => _writer.Written;

	public int OpcodeLength => _writer.Length;

	public int Depth => _depth;

	public int ResourceCount => _resources.Count;

	public Rect? CalculateBounds()
	{
		BoundsVisitor visitor = default(BoundsVisitor);
		Visit<BoundsVisitor, BoundsScope>(ref visitor);
		return visitor.Current;
	}

	public object? GetResource(int handle)
	{
		return _resources[handle];
	}

	public void Rewind(int length, int depth)
	{
		_writer.Rewind(length);
		_depth = depth;
	}

	private void EnterScope()
	{
		_depth++;
		if (_depth > _maxDepth)
		{
			_maxDepth = _depth;
		}
	}

	public void DisposeResources()
	{
		for (int i = 0; i < _resources.Count; i++)
		{
			object obj = _resources[i];
			if (!(obj is IRef<IBitmapImpl> obj2))
			{
				if (!(obj is IRef<IGlyphRunImpl> obj3))
				{
					if (obj is ICustomDrawOperation customDrawOperation)
					{
						customDrawOperation.Dispose();
					}
				}
				else
				{
					obj3.Dispose();
				}
			}
			else
			{
				obj2.Dispose();
			}
		}
	}

	public void DrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
	{
		_writer.WritePayload(new DrawLinePayload
		{
			ServerPen = _resources.Intern(serverPen),
			ClientPen = _resources.Intern(clientPen),
			P1 = p1,
			P2 = p2
		});
	}

	public void DrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect, BoxShadows boxShadows)
	{
		_writer.WritePayload(new DrawRectanglePayload
		{
			ServerBrush = _resources.Intern(serverBrush),
			ServerPen = _resources.Intern(serverPen),
			ClientPen = _resources.Intern(clientPen),
			Rect = rect,
			BoxShadowCount = boxShadows.Count
		});
		for (int i = 0; i < boxShadows.Count; i++)
		{
			_writer.Write(boxShadows[i]);
		}
	}

	public void DrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
	{
		_writer.WritePayload(new DrawEllipsePayload
		{
			ServerBrush = _resources.Intern(serverBrush),
			ServerPen = _resources.Intern(serverPen),
			ClientPen = _resources.Intern(clientPen),
			Rect = rect
		});
	}

	public void DrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IRenderDataGeometry? geometry)
	{
		_writer.WritePayload(new DrawGeometryPayload
		{
			ServerBrush = _resources.Intern(serverBrush),
			ServerPen = _resources.Intern(serverPen),
			ClientPen = _resources.Intern(clientPen),
			Geometry = _resources.Intern(geometry)
		});
	}

	public void DrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
	{
		_writer.WritePayload(new DrawGlyphRunPayload
		{
			ServerBrush = _resources.Intern(serverBrush),
			GlyphRun = _resources.Intern(glyphRun)
		});
	}

	public void DrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
	{
		_writer.WritePayload(new DrawBitmapPayload
		{
			Bitmap = _resources.Intern(bitmap),
			Opacity = opacity,
			SourceRect = sourceRect,
			DestRect = destRect
		});
	}

	public void DrawCustom(ICustomDrawOperation? operation)
	{
		_writer.WritePayload(new DrawCustomPayload
		{
			Operation = _resources.Intern(operation)
		});
	}

	public void PushClip(RoundedRect clip)
	{
		_writer.WritePayload(new PushClipPayload
		{
			Clip = clip
		});
		EnterScope();
	}

	public void PushGeometryClip(IRenderDataGeometry? geometry)
	{
		_writer.WritePayload(new PushGeometryClipPayload
		{
			Geometry = _resources.Intern(geometry)
		});
		EnterScope();
	}

	public void PushOpacity(double opacity)
	{
		_writer.WritePayload(new PushOpacityPayload
		{
			Opacity = opacity
		});
		EnterScope();
	}

	public void PushOpacityMask(IBrush? serverBrush, Rect bounds)
	{
		_writer.WritePayload(new PushOpacityMaskPayload
		{
			Brush = _resources.Intern(serverBrush),
			Bounds = bounds
		});
		EnterScope();
	}

	public void PushTransform(Matrix matrix)
	{
		_writer.WritePayload(new PushTransformPayload
		{
			Matrix = matrix
		});
		EnterScope();
	}

	public void PushRenderOptions(RenderOptions renderOptions)
	{
		_writer.WritePayload(new PushRenderOptionsPayload
		{
			Options = renderOptions
		});
		EnterScope();
	}

	public void PushTextOptions(TextOptions textOptions)
	{
		_writer.WritePayload(new PushTextOptionsPayload
		{
			Options = textOptions
		});
		EnterScope();
	}

	public void PushEffect(IImmutableEffect? effect, Rect bounds)
	{
		_writer.WritePayload(new PushEffectPayload
		{
			Effect = _resources.Intern(effect),
			Bounds = bounds
		});
		EnterScope();
	}

	public void Pop()
	{
		_writer.WriteOpcode(RenderDataOpcode.Pop);
		_depth--;
	}

	public void SerializeTo(BatchStreamWriter writer)
	{
		ReadOnlySpan<byte> written = _writer.Written;
		writer.Write(_maxDepth);
		writer.Write(_resources.Count);
		for (int i = 0; i < _resources.Count; i++)
		{
			writer.WriteObject(_resources[i]);
		}
		writer.Write(written.Length);
		writer.Write(written);
	}

	public void DeserializeFrom(BatchStreamReader reader)
	{
		_maxDepth = reader.Read<int>();
		int num = reader.Read<int>();
		for (int i = 0; i < num; i++)
		{
			_resources.AppendDeserialized(reader.ReadObject());
		}
		int num2 = reader.Read<int>();
		if (num2 > 0)
		{
			reader.Read(_writer.Reserve(num2));
		}
	}

	public void Dispose()
	{
		_writer.Dispose();
		_resources.Dispose();
	}

	public bool HitTest(Point point)
	{
		HitTestVisitor visitor = new HitTestVisitor(point);
		Visit<HitTestVisitor, HitTestScope>(ref visitor);
		return visitor.HitFound;
	}

	private static bool HitTestLine(IPen? clientPen, Point p1, Point p2, Point p)
	{
		if (clientPen == null)
		{
			return false;
		}
		double num = clientPen.Thickness / 2.0;
		double num2 = Math.Min(p1.X, p2.X) - num;
		double num3 = Math.Max(p1.X, p2.X) + num;
		double num4 = Math.Min(p1.Y, p2.Y) - num;
		double num5 = Math.Max(p1.Y, p2.Y) + num;
		if (p.X < num2 || p.X > num3 || p.Y < num4 || p.Y > num5)
		{
			return false;
		}
		Vector b = p - p1;
		if (Vector.Dot(p2 - p1, b) < 0.0)
		{
			return b.Length <= num;
		}
		Vector b2 = p - p2;
		if (Vector.Dot(p1 - p2, b2) < 0.0)
		{
			return b2.Length <= num;
		}
		double num6 = p2.X - p1.X;
		double num7 = p2.Y - p1.Y;
		return Math.Abs((num6 * (p.Y - p1.Y) - num7 * (p.X - p1.X)) / Math.Sqrt(num6 * num6 + num7 * num7)) <= num;
	}

	private static bool HitTestRectangle(IBrush? serverBrush, IPen? clientPen, RoundedRect rect, Point p)
	{
		double num = ((clientPen != null) ? (clientPen.Thickness / 2.0) : 0.0);
		if (rect.IsRounded)
		{
			if (rect.Inflate(num, num).ContainsExclusive(p))
			{
				if (serverBrush != null)
				{
					return true;
				}
				return !rect.Deflate(num, num).ContainsExclusive(p);
			}
		}
		else if (rect.Rect.Inflate(num).ContainsExclusive(p))
		{
			if (serverBrush != null)
			{
				return true;
			}
			return !rect.Rect.Deflate(num).ContainsExclusive(p);
		}
		return false;
	}

	private static bool HitTestEllipse(IBrush? serverBrush, IPen? clientPen, Rect rect, Point p)
	{
		Point center = rect.Center;
		double num = clientPen?.Thickness ?? 0.0;
		double num2 = rect.Width / 2.0 + num / 2.0;
		double num3 = rect.Height / 2.0 + num / 2.0;
		double num4 = p.X - center.X;
		double num5 = p.Y - center.Y;
		if (Math.Abs(num4) > num2 || Math.Abs(num5) > num3)
		{
			return false;
		}
		if (serverBrush != null)
		{
			return EllipseContains(num4, num5, num2, num3);
		}
		if (num > 0.0)
		{
			bool num6 = EllipseContains(num4, num5, num2, num3);
			num2 = rect.Width / 2.0 - num / 2.0;
			num3 = rect.Height / 2.0 - num / 2.0;
			bool flag = EllipseContains(num4, num5, num2, num3);
			if (num6)
			{
				return !flag;
			}
			return false;
		}
		return false;
	}

	private static bool EllipseContains(double dx, double dy, double radiusX, double radiusY)
	{
		double num = radiusX * radiusX;
		double num2 = radiusY * radiusY;
		return num2 * dx * dx + num * dy * dy <= num * num2;
	}

	public void Replay(IDrawingContextImpl context)
	{
		ReplayVisitor visitor = new ReplayVisitor(context);
		Visit<ReplayVisitor, ReplayScope>(ref visitor);
	}

	public void Visit<TVisitor, TScope>(ref TVisitor visitor) where TVisitor : struct, IRenderDataVisitor<TScope> where TScope : unmanaged
	{
		RenderDataReader reader = new RenderDataReader(_writer.Written);
		TScope[] array = null;
		Span<TScope> span = ((_maxDepth == 0) ? default(Span<TScope>) : ((_maxDepth > 64) ? ((Span<TScope>)(array = ArrayPool<TScope>.Shared.Rent(_maxDepth))) : stackalloc TScope[_maxDepth]));
		int num = 0;
		try
		{
			while (!visitor.StopVisiting && !reader.IsAtEnd)
			{
				switch (reader.Peek<RenderDataOpcode>())
				{
				case RenderDataOpcode.DrawLine:
				{
					DrawLinePayload drawLinePayload = reader.ReadPayload<DrawLinePayload>();
					visitor.OnDrawLine((IPen)_resources[drawLinePayload.ServerPen], (IPen)_resources[drawLinePayload.ClientPen], drawLinePayload.P1, drawLinePayload.P2);
					break;
				}
				case RenderDataOpcode.DrawRectangle:
				{
					DrawRectanglePayload drawRectanglePayload = reader.ReadPayload<DrawRectanglePayload>();
					BoxShadows boxShadows = ReadBoxShadows(ref reader, drawRectanglePayload.BoxShadowCount);
					visitor.OnDrawRectangle((IBrush)_resources[drawRectanglePayload.ServerBrush], (IPen)_resources[drawRectanglePayload.ServerPen], (IPen)_resources[drawRectanglePayload.ClientPen], drawRectanglePayload.Rect, boxShadows);
					break;
				}
				case RenderDataOpcode.DrawEllipse:
				{
					DrawEllipsePayload drawEllipsePayload = reader.ReadPayload<DrawEllipsePayload>();
					visitor.OnDrawEllipse((IBrush)_resources[drawEllipsePayload.ServerBrush], (IPen)_resources[drawEllipsePayload.ServerPen], (IPen)_resources[drawEllipsePayload.ClientPen], drawEllipsePayload.Rect);
					break;
				}
				case RenderDataOpcode.DrawGeometry:
				{
					DrawGeometryPayload drawGeometryPayload = reader.ReadPayload<DrawGeometryPayload>();
					visitor.OnDrawGeometry((IBrush)_resources[drawGeometryPayload.ServerBrush], (IPen)_resources[drawGeometryPayload.ServerPen], (IPen)_resources[drawGeometryPayload.ClientPen], GetGeometryImpl(_resources[drawGeometryPayload.Geometry]));
					break;
				}
				case RenderDataOpcode.DrawGlyphRun:
				{
					DrawGlyphRunPayload drawGlyphRunPayload = reader.ReadPayload<DrawGlyphRunPayload>();
					visitor.OnDrawGlyphRun((IBrush)_resources[drawGlyphRunPayload.ServerBrush], (IRef<IGlyphRunImpl>)_resources[drawGlyphRunPayload.GlyphRun]);
					break;
				}
				case RenderDataOpcode.DrawBitmap:
				{
					DrawBitmapPayload drawBitmapPayload = reader.ReadPayload<DrawBitmapPayload>();
					visitor.OnDrawBitmap((IRef<IBitmapImpl>)_resources[drawBitmapPayload.Bitmap], drawBitmapPayload.Opacity, drawBitmapPayload.SourceRect, drawBitmapPayload.DestRect);
					break;
				}
				case RenderDataOpcode.DrawCustom:
				{
					DrawCustomPayload drawCustomPayload = reader.ReadPayload<DrawCustomPayload>();
					visitor.OnDrawCustom((ICustomDrawOperation)_resources[drawCustomPayload.Operation]);
					break;
				}
				case RenderDataOpcode.PushClip:
				{
					PushClipPayload pushClipPayload = reader.ReadPayload<PushClipPayload>();
					span[num++] = visitor.OnPushClip(pushClipPayload.Clip);
					break;
				}
				case RenderDataOpcode.PushGeometryClip:
				{
					PushGeometryClipPayload pushGeometryClipPayload = reader.ReadPayload<PushGeometryClipPayload>();
					span[num++] = visitor.OnPushGeometryClip(GetGeometryImpl(_resources[pushGeometryClipPayload.Geometry]));
					break;
				}
				case RenderDataOpcode.PushOpacity:
				{
					PushOpacityPayload pushOpacityPayload = reader.ReadPayload<PushOpacityPayload>();
					span[num++] = visitor.OnPushOpacity(pushOpacityPayload.Opacity);
					break;
				}
				case RenderDataOpcode.PushOpacityMask:
				{
					PushOpacityMaskPayload pushOpacityMaskPayload = reader.ReadPayload<PushOpacityMaskPayload>();
					span[num++] = visitor.OnPushOpacityMask((IBrush)_resources[pushOpacityMaskPayload.Brush], pushOpacityMaskPayload.Bounds);
					break;
				}
				case RenderDataOpcode.PushTransform:
				{
					PushTransformPayload pushTransformPayload = reader.ReadPayload<PushTransformPayload>();
					span[num++] = visitor.OnPushTransform(pushTransformPayload.Matrix);
					break;
				}
				case RenderDataOpcode.PushRenderOptions:
				{
					PushRenderOptionsPayload pushRenderOptionsPayload = reader.ReadPayload<PushRenderOptionsPayload>();
					span[num++] = visitor.OnPushRenderOptions(pushRenderOptionsPayload.Options);
					break;
				}
				case RenderDataOpcode.PushTextOptions:
				{
					PushTextOptionsPayload pushTextOptionsPayload = reader.ReadPayload<PushTextOptionsPayload>();
					span[num++] = visitor.OnPushTextOptions(pushTextOptionsPayload.Options);
					break;
				}
				case RenderDataOpcode.PushEffect:
				{
					PushEffectPayload pushEffectPayload = reader.ReadPayload<PushEffectPayload>();
					span[num++] = visitor.OnPushEffect((IEffect)_resources[pushEffectPayload.Effect], pushEffectPayload.Bounds);
					break;
				}
				case RenderDataOpcode.Pop:
					reader.Read<RenderDataOpcode>();
					visitor.OnPop(in span[--num]);
					break;
				}
			}
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<TScope>.Shared.Return(array);
			}
		}
	}

	private static BoxShadows ReadBoxShadows(ref RenderDataReader reader, int count)
	{
		if (count == 0)
		{
			return default(BoxShadows);
		}
		BoxShadow boxShadow = reader.Read<BoxShadow>();
		if (count == 1)
		{
			return new BoxShadows(boxShadow);
		}
		BoxShadow[] array = new BoxShadow[count - 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = reader.Read<BoxShadow>();
		}
		return new BoxShadows(boxShadow, array);
	}

	private static IGeometryImpl? GetGeometryImpl(object? resource)
	{
		return ((IRenderDataGeometry)resource)?.GeometryImpl;
	}
}
