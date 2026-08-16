using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Media;

public sealed class DrawingGroup : Drawing
{
	private sealed class DrawingGroupDrawingContext : DrawingContext
	{
		private readonly DrawingGroup _drawingGroup;

		private readonly IPlatformRenderInterface _platformRenderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

		private bool _disposed;

		private Drawing? _rootDrawing;

		private DrawingGroup? _currentDrawingGroup;

		private Stack<DrawingGroup?>? _previousDrawingGroupStack;

		public DrawingGroupDrawingContext(DrawingGroup drawingGroup)
		{
			_drawingGroup = drawingGroup;
		}

		protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect)
		{
			if (brush != null || pen != null)
			{
				IGeometryImpl geometryImpl = _platformRenderInterface.CreateEllipseGeometry(rect);
				AddNewGeometryDrawing(brush, pen, new PlatformGeometry(geometryImpl));
			}
		}

		protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
		{
			if (brush != null || pen != null)
			{
				AddNewGeometryDrawing(brush, pen, new PlatformGeometry(geometry));
			}
		}

		public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun)
		{
			if (foreground != null)
			{
				GlyphRunDrawing newDrawing = new GlyphRunDrawing
				{
					Foreground = foreground,
					GlyphRun = glyphRun
				};
				AddDrawing(newDrawing);
			}
		}

		protected override void PushClipCore(RoundedRect rect)
		{
			throw new NotImplementedException();
		}

		protected override void PushClipCore(Rect rect)
		{
			PushNewDrawingGroup().ClipGeometry = new RectangleGeometry(rect);
		}

		protected override void PushGeometryClipCore(Geometry clip)
		{
			PushNewDrawingGroup().ClipGeometry = clip;
		}

		protected override void PushOpacityCore(double opacity)
		{
			PushNewDrawingGroup().Opacity = opacity;
		}

		protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
		{
			PushNewDrawingGroup().OpacityMask = mask;
		}

		internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect)
		{
			throw new NotImplementedException();
		}

		protected override void DrawLineCore(IPen pen, Point p1, Point p2)
		{
			IGeometryImpl geometryImpl = _platformRenderInterface.CreateLineGeometry(p1, p2);
			AddNewGeometryDrawing(null, pen, new PlatformGeometry(geometryImpl));
		}

		protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default(BoxShadows))
		{
			IGeometryImpl geometryImpl = _platformRenderInterface.CreateRectangleGeometry(rrect.Rect);
			AddNewGeometryDrawing(brush, pen, new PlatformGeometry(geometryImpl));
		}

		public override void Custom(ICustomDrawOperation custom)
		{
			throw new NotSupportedException();
		}

		protected override void DisposeCore()
		{
			if (_disposed)
			{
				return;
			}
			if (_previousDrawingGroupStack != null)
			{
				int count = _previousDrawingGroupStack.Count;
				for (int i = 0; i < count; i++)
				{
					Pop();
				}
			}
			DrawingCollection drawingCollection;
			if (_currentDrawingGroup != null)
			{
				drawingCollection = _currentDrawingGroup.Children;
			}
			else
			{
				drawingCollection = new DrawingCollection();
				if (_rootDrawing != null)
				{
					drawingCollection.Add(_rootDrawing);
				}
			}
			_drawingGroup.Children = drawingCollection;
			_disposed = true;
		}

		/// <summary>
		/// Pop
		/// </summary>
		private void Pop()
		{
			if (_previousDrawingGroupStack == null || _previousDrawingGroupStack.Count == 0)
			{
				throw new InvalidOperationException("DrawingGroupStack count missmatch.");
			}
			_currentDrawingGroup = _previousDrawingGroupStack.Pop();
		}

		/// <summary>
		///     PushTransform -
		///     Push a Transform which will apply to all drawing operations until the corresponding
		///     Pop.
		/// </summary>
		/// <param name="matrix"> The transform to push. </param>
		protected override void PushTransformCore(Matrix matrix)
		{
			PushNewDrawingGroup().Transform = new MatrixTransform(matrix);
		}

		protected override void PushRenderOptionsCore(RenderOptions renderOptions)
		{
			PushNewDrawingGroup().RenderOptions = renderOptions;
		}

		protected override void PushTextOptionsCore(TextOptions textOptions)
		{
			PushNewDrawingGroup().TextOptions = textOptions;
		}

		/// <inheritdoc />
		protected override void PushEffectCore(IEffect effect, Rect bounds)
		{
			DrawingGroup drawingGroup = PushNewDrawingGroup();
			drawingGroup.Effect = effect;
			drawingGroup.EffectBounds = bounds;
		}

		protected override void PopClipCore()
		{
			Pop();
		}

		protected override void PopGeometryClipCore()
		{
			Pop();
		}

		protected override void PopOpacityCore()
		{
			Pop();
		}

		protected override void PopOpacityMaskCore()
		{
			Pop();
		}

		protected override void PopTransformCore()
		{
			Pop();
		}

		protected override void PopRenderOptionsCore()
		{
			Pop();
		}

		protected override void PopTextOptionsCore()
		{
			Pop();
		}

		/// <inheritdoc />
		protected override void PopEffectCore()
		{
			Pop();
		}

		/// <summary>
		/// Creates a new DrawingGroup for a Push* call by setting the
		/// _currentDrawingGroup to a newly instantiated DrawingGroup,
		/// and saving the previous _currentDrawingGroup value on the
		/// _previousDrawingGroupStack.
		/// </summary>
		private DrawingGroup PushNewDrawingGroup()
		{
			DrawingGroup drawingGroup = new DrawingGroup();
			AddDrawing(drawingGroup);
			if (_previousDrawingGroupStack == null)
			{
				_previousDrawingGroupStack = new Stack<DrawingGroup>(2);
			}
			_previousDrawingGroupStack.Push(_currentDrawingGroup);
			_currentDrawingGroup = drawingGroup;
			return drawingGroup;
		}

		/// <summary>
		/// Contains the functionality common to GeometryDrawing operations of
		/// instantiating the GeometryDrawing, setting it's Freezable state,
		/// and Adding it to the Drawing Graph.
		/// </summary>
		private void AddNewGeometryDrawing(IBrush? brush, IPen? pen, Geometry? geometry)
		{
			if (geometry == null)
			{
				throw new ArgumentNullException("geometry");
			}
			GeometryDrawing newDrawing = new GeometryDrawing
			{
				Brush = brush,
				Pen = pen,
				Geometry = geometry
			};
			AddDrawing(newDrawing);
		}

		/// <summary>
		/// Adds a new Drawing to the DrawingGraph.
		///
		/// This method avoids creating a DrawingGroup for the common case
		/// where only a single child exists in the root DrawingGroup.
		/// </summary>
		private void AddDrawing(Drawing newDrawing)
		{
			if (newDrawing == null)
			{
				throw new ArgumentNullException("newDrawing");
			}
			if (_rootDrawing == null)
			{
				if (_currentDrawingGroup != null)
				{
					throw new NotSupportedException("When a DrawingGroup is set, it should be made the root if a root drawing didnt exist.");
				}
				_rootDrawing = newDrawing;
			}
			else if (_currentDrawingGroup == null)
			{
				_currentDrawingGroup = new DrawingGroup();
				_currentDrawingGroup.Children.Add(_rootDrawing);
				_currentDrawingGroup.Children.Add(newDrawing);
				_rootDrawing = _currentDrawingGroup;
			}
			else
			{
				_currentDrawingGroup.Children.Add(newDrawing);
			}
		}
	}

	public static readonly StyledProperty<double> OpacityProperty = AvaloniaProperty.Register<DrawingGroup, double>("Opacity", 1.0);

	public static readonly StyledProperty<Transform?> TransformProperty = AvaloniaProperty.Register<DrawingGroup, Transform>("Transform");

	public static readonly StyledProperty<Geometry?> ClipGeometryProperty = AvaloniaProperty.Register<DrawingGroup, Geometry>("ClipGeometry");

	public static readonly StyledProperty<IBrush?> OpacityMaskProperty = AvaloniaProperty.Register<DrawingGroup, IBrush>("OpacityMask");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.DrawingGroup.Effect" /> property.
	/// </summary>
	public static readonly StyledProperty<IEffect?> EffectProperty = AvaloniaProperty.Register<DrawingGroup, IEffect>("Effect");

	public static readonly DirectProperty<DrawingGroup, DrawingCollection> ChildrenProperty = AvaloniaProperty.RegisterDirect("Children", (DrawingGroup o) => o.Children, delegate(DrawingGroup o, DrawingCollection v)
	{
		o.Children = v;
	});

	private DrawingCollection _children = new DrawingCollection();

	public double Opacity
	{
		get
		{
			return GetValue(OpacityProperty);
		}
		set
		{
			SetValue(OpacityProperty, value);
		}
	}

	public Transform? Transform
	{
		get
		{
			return GetValue(TransformProperty);
		}
		set
		{
			SetValue(TransformProperty, value);
		}
	}

	public Geometry? ClipGeometry
	{
		get
		{
			return GetValue(ClipGeometryProperty);
		}
		set
		{
			SetValue(ClipGeometryProperty, value);
		}
	}

	public IBrush? OpacityMask
	{
		get
		{
			return GetValue(OpacityMaskProperty);
		}
		set
		{
			SetValue(OpacityMaskProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the effect to apply to the drawing group.
	/// </summary>
	public IEffect? Effect
	{
		get
		{
			return GetValue(EffectProperty);
		}
		set
		{
			SetValue(EffectProperty, value);
		}
	}

	internal RenderOptions? RenderOptions { get; set; }

	internal TextOptions? TextOptions { get; set; }

	internal Rect? EffectBounds { get; set; }

	/// <summary>
	/// Gets or sets the collection that contains the child geometries.
	/// </summary>
	[Content]
	public DrawingCollection Children
	{
		get
		{
			return _children;
		}
		set
		{
			if (_children == value)
			{
				return;
			}
			_children.CollectionChanged -= ChildrenCollectionChanged;
			foreach (Drawing child in _children)
			{
				child.Invalidated -= ChildInvalidated;
			}
			SetAndRaise(ChildrenProperty, ref _children, value);
			_children.CollectionChanged += ChildrenCollectionChanged;
			foreach (Drawing child2 in _children)
			{
				child2.Invalidated += ChildInvalidated;
			}
		}
	}

	public DrawingGroup()
	{
		_children.CollectionChanged += ChildrenCollectionChanged;
	}

	public DrawingContext Open()
	{
		return new DrawingGroupDrawingContext(this);
	}

	private void ChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Reset)
		{
			throw new NotSupportedException();
		}
		if (e.OldItems != null)
		{
			foreach (Drawing oldItem in e.OldItems)
			{
				oldItem.Invalidated -= ChildInvalidated;
			}
		}
		if (e.NewItems != null)
		{
			foreach (Drawing newItem in e.NewItems)
			{
				newItem.Invalidated += ChildInvalidated;
			}
		}
		RaiseInvalidated();
	}

	private void ChildInvalidated(object? sender, EventArgs e)
	{
		RaiseInvalidated();
	}

	internal override void DrawCore(DrawingContext context)
	{
		Rect rect = EffectBounds.GetValueOrDefault();
		if (!EffectBounds.HasValue)
		{
			foreach (Drawing child in Children)
			{
				rect = rect.Union(child.GetBounds());
			}
		}
		Rect bounds = ((Effect != null) ? rect.Inflate(Effect.GetEffectOutputPadding()) : rect);
		using (context.PushTransform(Transform?.Value ?? Matrix.Identity))
		{
			using (context.PushOpacity(Opacity))
			{
				using ((ClipGeometry != null) ? context.PushGeometryClip(ClipGeometry) : default(DrawingContext.PushedState))
				{
					using ((OpacityMask != null) ? context.PushOpacityMask(OpacityMask, bounds) : default(DrawingContext.PushedState))
					{
						using (RenderOptions.HasValue ? context.PushRenderOptions(RenderOptions.Value) : default(DrawingContext.PushedState))
						{
							using (TextOptions.HasValue ? context.PushTextOptions(TextOptions.Value) : default(DrawingContext.PushedState))
							{
								using ((Effect != null) ? context.PushEffect(Effect, rect) : default(DrawingContext.PushedState))
								{
									foreach (Drawing child2 in Children)
									{
										child2.Draw(context);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	public override Rect GetBounds()
	{
		Rect result = default(Rect);
		foreach (Drawing child in Children)
		{
			result = result.Union(child.GetBounds());
		}
		if (Transform != null)
		{
			result = result.TransformToAABB(Transform.Value);
		}
		return result;
	}
}
