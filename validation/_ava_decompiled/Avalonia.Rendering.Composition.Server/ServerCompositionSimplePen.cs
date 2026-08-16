using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimplePen : SimpleServerRenderResource, IPen
{
	private IBrush? _brush;

	internal static readonly CompositionProperty<IBrush?> s_IdOfBrushProperty = CompositionProperty.Register<ServerCompositionSimplePen, IBrush>("Brush", (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._brush, delegate(SimpleServerObject obj, IBrush? v)
	{
		((ServerCompositionSimplePen)obj)._brush = v;
	}, null);

	private ImmutableDashStyle? _dashStyle;

	internal static readonly CompositionProperty<ImmutableDashStyle?> s_IdOfDashStyleProperty = CompositionProperty.Register<ServerCompositionSimplePen, ImmutableDashStyle>("DashStyle", (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._dashStyle, delegate(SimpleServerObject obj, ImmutableDashStyle? v)
	{
		((ServerCompositionSimplePen)obj)._dashStyle = v;
	}, null);

	private PenLineCap _lineCap;

	internal static readonly CompositionProperty<PenLineCap> s_IdOfLineCapProperty = CompositionProperty.Register<ServerCompositionSimplePen, PenLineCap>("LineCap", (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._lineCap, delegate(SimpleServerObject obj, PenLineCap v)
	{
		((ServerCompositionSimplePen)obj)._lineCap = v;
	}, null);

	private PenLineJoin _lineJoin;

	internal static readonly CompositionProperty<PenLineJoin> s_IdOfLineJoinProperty = CompositionProperty.Register<ServerCompositionSimplePen, PenLineJoin>("LineJoin", (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._lineJoin, delegate(SimpleServerObject obj, PenLineJoin v)
	{
		((ServerCompositionSimplePen)obj)._lineJoin = v;
	}, null);

	private double _miterLimit;

	internal static readonly CompositionProperty<double> s_IdOfMiterLimitProperty = CompositionProperty.Register<ServerCompositionSimplePen, double>("MiterLimit", (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._miterLimit, delegate(SimpleServerObject obj, double v)
	{
		((ServerCompositionSimplePen)obj)._miterLimit = v;
	}, (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._miterLimit);

	private double _thickness;

	internal static readonly CompositionProperty<double> s_IdOfThicknessProperty = CompositionProperty.Register<ServerCompositionSimplePen, double>("Thickness", (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._thickness, delegate(SimpleServerObject obj, double v)
	{
		((ServerCompositionSimplePen)obj)._thickness = v;
	}, (SimpleServerObject obj) => ((ServerCompositionSimplePen)obj)._thickness);

	IDashStyle? IPen.DashStyle => DashStyle;

	public IBrush? Brush
	{
		get
		{
			return _brush;
		}
		set
		{
			bool flag = false;
			if (_brush != value)
			{
				flag = true;
			}
			SetValue(s_IdOfBrushProperty, ref _brush, value);
		}
	}

	public ImmutableDashStyle? DashStyle
	{
		get
		{
			return _dashStyle;
		}
		set
		{
			bool flag = false;
			if (_dashStyle != value)
			{
				flag = true;
			}
			SetValue(s_IdOfDashStyleProperty, ref _dashStyle, value);
		}
	}

	public PenLineCap LineCap
	{
		get
		{
			return _lineCap;
		}
		set
		{
			bool flag = false;
			if (_lineCap != value)
			{
				flag = true;
			}
			SetValue(s_IdOfLineCapProperty, ref _lineCap, value);
		}
	}

	public PenLineJoin LineJoin
	{
		get
		{
			return _lineJoin;
		}
		set
		{
			bool flag = false;
			if (_lineJoin != value)
			{
				flag = true;
			}
			SetValue(s_IdOfLineJoinProperty, ref _lineJoin, value);
		}
	}

	public double MiterLimit
	{
		get
		{
			return _miterLimit;
		}
		set
		{
			bool flag = false;
			if (_miterLimit != value)
			{
				flag = true;
			}
			SetValue(s_IdOfMiterLimitProperty, ref _miterLimit, value);
		}
	}

	public double Thickness
	{
		get
		{
			return _thickness;
		}
		set
		{
			bool flag = false;
			if (_thickness != value)
			{
				flag = true;
			}
			SetValue(s_IdOfThicknessProperty, ref _thickness, value);
		}
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		RemoveObserversFromProperty(ref _brush);
		_brush = null;
		base.Dispose();
	}

	internal ServerCompositionSimplePen(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionSimplePenChangedFields num = reader.Read<CompositionSimplePenChangedFields>();
		if ((num & CompositionSimplePenChangedFields.Brush) == CompositionSimplePenChangedFields.Brush)
		{
			Brush = reader.ReadObject<IBrush>();
		}
		if ((num & CompositionSimplePenChangedFields.DashStyle) == CompositionSimplePenChangedFields.DashStyle)
		{
			DashStyle = reader.ReadObject<ImmutableDashStyle>();
		}
		if ((num & CompositionSimplePenChangedFields.LineCap) == CompositionSimplePenChangedFields.LineCap)
		{
			LineCap = reader.Read<PenLineCap>();
		}
		if ((num & CompositionSimplePenChangedFields.LineJoin) == CompositionSimplePenChangedFields.LineJoin)
		{
			LineJoin = reader.Read<PenLineJoin>();
		}
		if ((num & CompositionSimplePenChangedFields.MiterLimit) == CompositionSimplePenChangedFields.MiterLimit)
		{
			MiterLimit = reader.Read<double>();
		}
		if ((num & CompositionSimplePenChangedFields.Thickness) == CompositionSimplePenChangedFields.Thickness)
		{
			Thickness = reader.Read<double>();
		}
	}

	internal static void SerializeAllChanges(BatchStreamWriter writer, IBrush? brush, ImmutableDashStyle? dashStyle, PenLineCap lineCap, PenLineJoin lineJoin, double miterLimit, double thickness)
	{
		writer.Write(CompositionSimplePenChangedFields.Brush | CompositionSimplePenChangedFields.DashStyle | CompositionSimplePenChangedFields.LineCap | CompositionSimplePenChangedFields.LineJoin | CompositionSimplePenChangedFields.MiterLimit | CompositionSimplePenChangedFields.Thickness);
		writer.WriteObject(brush);
		writer.WriteObject(dashStyle);
		writer.Write(lineCap);
		writer.Write(lineJoin);
		writer.Write(miterLimit);
		writer.Write(thickness);
	}
}
