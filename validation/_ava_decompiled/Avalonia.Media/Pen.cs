using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Describes how a stroke is drawn.
/// </summary>
public sealed class Pen : AvaloniaObject, IPen, ICompositionRenderResource<IPen>, ICompositionRenderResource, ICompositorSerializable
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Pen.Brush" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> BrushProperty = AvaloniaProperty.Register<Pen, IBrush>("Brush");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Pen.Thickness" /> property.
	/// </summary>
	public static readonly StyledProperty<double> ThicknessProperty = AvaloniaProperty.Register<Pen, double>("Thickness", 1.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Pen.DashStyle" /> property.
	/// </summary>
	public static readonly StyledProperty<IDashStyle?> DashStyleProperty = AvaloniaProperty.Register<Pen, IDashStyle>("DashStyle");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Pen.LineCap" /> property.
	/// </summary>
	public static readonly StyledProperty<PenLineCap> LineCapProperty = AvaloniaProperty.Register<Pen, PenLineCap>("LineCap", PenLineCap.Flat);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Pen.LineJoin" /> property.
	/// </summary>
	public static readonly StyledProperty<PenLineJoin> LineJoinProperty = AvaloniaProperty.Register<Pen, PenLineJoin>("LineJoin", PenLineJoin.Bevel);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Pen.MiterLimit" /> property.
	/// </summary>
	public static readonly StyledProperty<double> MiterLimitProperty = AvaloniaProperty.Register<Pen, double>("MiterLimit", 10.0);

	private DashStyle? _subscribedToDashes;

	private TargetWeakEventSubscriber<Pen, EventArgs>? _weakSubscriber;

	private static readonly WeakEvent<DashStyle, EventArgs> InvalidatedWeakEvent = WeakEvent.Register(delegate(DashStyle s, EventHandler h)
	{
		s.Invalidated += h;
	}, delegate(DashStyle s, EventHandler h)
	{
		s.Invalidated -= h;
	});

	private CompositorResourceHolder<ServerCompositionSimplePen> _resource;

	/// <summary>
	/// Gets or sets the brush used to draw the stroke.
	/// </summary>
	public IBrush? Brush
	{
		get
		{
			return GetValue(BrushProperty);
		}
		set
		{
			SetValue(BrushProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the stroke thickness.
	/// </summary>
	public double Thickness
	{
		get
		{
			return GetValue(ThicknessProperty);
		}
		set
		{
			SetValue(ThicknessProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the style of dashed lines drawn with a <see cref="T:Avalonia.Media.Pen" /> object.
	/// </summary>
	public IDashStyle? DashStyle
	{
		get
		{
			return GetValue(DashStyleProperty);
		}
		set
		{
			SetValue(DashStyleProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the type of shape to use on both ends of a line.
	/// </summary>
	public PenLineCap LineCap
	{
		get
		{
			return GetValue(LineCapProperty);
		}
		set
		{
			SetValue(LineCapProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the join style for the ends of two consecutive lines drawn with this
	/// <see cref="T:Avalonia.Media.Pen" />.
	/// </summary>
	public PenLineJoin LineJoin
	{
		get
		{
			return GetValue(LineJoinProperty);
		}
		set
		{
			SetValue(LineJoinProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the limit of the thickness of the join on a mitered corner.
	/// </summary>
	public double MiterLimit
	{
		get
		{
			return GetValue(MiterLimitProperty);
		}
		set
		{
			SetValue(MiterLimitProperty, value);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Pen" /> class.
	/// </summary>
	public Pen()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Pen" /> class.
	/// </summary>
	/// <param name="color">The stroke color.</param>
	/// <param name="thickness">The stroke thickness.</param>
	/// <param name="dashStyle">The dash style.</param>
	/// <param name="lineCap">Specifies the type of graphic shape to use on both ends of a line.</param>
	/// <param name="lineJoin">The line join.</param>
	/// <param name="miterLimit">The miter limit.</param>
	public Pen(uint color, double thickness = 1.0, IDashStyle? dashStyle = null, PenLineCap lineCap = PenLineCap.Flat, PenLineJoin lineJoin = PenLineJoin.Miter, double miterLimit = 10.0)
		: this(new SolidColorBrush(color), thickness, dashStyle, lineCap, lineJoin, miterLimit)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Pen" /> class.
	/// </summary>
	/// <param name="brush">The brush used to draw.</param>
	/// <param name="thickness">The stroke thickness.</param>
	/// <param name="dashStyle">The dash style.</param>
	/// <param name="lineCap">The line cap.</param>
	/// <param name="lineJoin">The line join.</param>
	/// <param name="miterLimit">The miter limit.</param>
	public Pen(IBrush? brush, double thickness = 1.0, IDashStyle? dashStyle = null, PenLineCap lineCap = PenLineCap.Flat, PenLineJoin lineJoin = PenLineJoin.Miter, double miterLimit = 10.0)
	{
		Brush = brush;
		Thickness = thickness;
		LineCap = lineCap;
		LineJoin = lineJoin;
		MiterLimit = miterLimit;
		DashStyle = dashStyle;
	}

	/// <summary>
	/// Creates an immutable clone of the brush.
	/// </summary>
	/// <returns>The immutable clone.</returns>
	public ImmutablePen ToImmutable()
	{
		return new ImmutablePen(Brush?.ToImmutable(), Thickness, DashStyle?.ToImmutable(), LineCap, LineJoin, MiterLimit);
	}

	/// <summary>
	/// Smart reuse and update pen properties.
	/// </summary>
	/// <param name="pen">Old pen to modify.</param>
	/// <param name="brush">The brush used to draw.</param>
	/// <param name="thickness">The stroke thickness.</param>
	/// <param name="strokeDashArray">The stroke dask array.</param>
	/// <param name="strokeDaskOffset">The stroke dask offset.</param>
	/// <param name="lineCap">The line cap.</param>
	/// <param name="lineJoin">The line join.</param>
	/// <param name="miterLimit">The miter limit.</param>
	/// <returns>If a new instance was created and visual invalidation required.</returns>
	internal static bool TryModifyOrCreate(ref IPen? pen, IBrush? brush, double thickness, IList<double>? strokeDashArray = null, double strokeDaskOffset = 0.0, PenLineCap lineCap = PenLineCap.Flat, PenLineJoin lineJoin = PenLineJoin.Miter, double miterLimit = 10.0)
	{
		IPen pen2 = pen;
		if (brush == null)
		{
			pen = null;
			return pen2 != null;
		}
		IDashStyle dashStyle = null;
		if (strokeDashArray != null && strokeDashArray.Count > 0)
		{
			IDashStyle dashStyle3;
			if (!(strokeDashArray is INotifyCollectionChanged))
			{
				IDashStyle dashStyle2 = new ImmutableDashStyle(strokeDashArray, strokeDaskOffset);
				dashStyle3 = dashStyle2;
			}
			else
			{
				IDashStyle dashStyle2 = new DashStyle(strokeDashArray, strokeDaskOffset);
				dashStyle3 = dashStyle2;
			}
			dashStyle = dashStyle3;
		}
		IImmutableBrush immutableBrush = brush as IImmutableBrush;
		bool flag = immutableBrush != null;
		if (flag)
		{
			bool flag2 = ((dashStyle == null || dashStyle is ImmutableDashStyle) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			pen = new ImmutablePen(immutableBrush, thickness, (ImmutableDashStyle)dashStyle, lineCap, lineJoin, miterLimit);
			return true;
		}
		Pen pen3 = (pen2 as Pen) ?? new Pen();
		pen3.Brush = brush;
		pen3.Thickness = thickness;
		pen3.LineCap = lineCap;
		pen3.LineJoin = lineJoin;
		pen3.DashStyle = dashStyle;
		pen3.MiterLimit = miterLimit;
		pen = pen3;
		return !object.Equals(pen2, pen);
	}

	private void RegisterForSerialization()
	{
		_resource.RegisterForInvalidationOnAllCompositors(this);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		RegisterForSerialization();
		if (change.Property == BrushProperty)
		{
			_resource.ProcessPropertyChangeNotification(change);
		}
		if (change.Property == DashStyleProperty)
		{
			UpdateDashStyleSubscription();
		}
		base.OnPropertyChanged(change);
	}

	private void UpdateDashStyleSubscription()
	{
		DashStyle dashStyle = (_resource.IsAttached ? (DashStyle as DashStyle) : null);
		if (_subscribedToDashes == dashStyle)
		{
			return;
		}
		if (_subscribedToDashes != null && _weakSubscriber != null)
		{
			InvalidatedWeakEvent.Unsubscribe(_subscribedToDashes, _weakSubscriber);
			_subscribedToDashes = null;
		}
		if (dashStyle == null)
		{
			return;
		}
		if (_weakSubscriber == null)
		{
			_weakSubscriber = new TargetWeakEventSubscriber<Pen, EventArgs>(this, delegate(Pen target, object? _, WeakEvent ev, EventArgs _)
			{
				if (ev == InvalidatedWeakEvent)
				{
					target.RegisterForSerialization();
				}
			});
		}
		InvalidatedWeakEvent.Subscribe(dashStyle, _weakSubscriber);
		_subscribedToDashes = dashStyle;
	}

	IPen ICompositionRenderResource<IPen>.GetForCompositor(Compositor c)
	{
		return _resource.GetForCompositor(c);
	}

	void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
	{
		if (_resource.CreateOrAddRef(c, this, out ServerCompositionSimplePen _, (Compositor compositor) => new ServerCompositionSimplePen(compositor.Server)))
		{
			(Brush as ICompositionRenderResource)?.AddRefOnCompositor(c);
			UpdateDashStyleSubscription();
		}
	}

	void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
	{
		if (_resource.Release(c))
		{
			(Brush as ICompositionRenderResource)?.ReleaseOnCompositor(c);
			UpdateDashStyleSubscription();
		}
	}

	SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c)
	{
		return _resource.TryGetForCompositor(c);
	}

	void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		ServerCompositionSimplePen.SerializeAllChanges(writer, Brush.GetServer(c), DashStyle?.ToImmutable(), LineCap, LineJoin, MiterLimit, Thickness);
	}
}
