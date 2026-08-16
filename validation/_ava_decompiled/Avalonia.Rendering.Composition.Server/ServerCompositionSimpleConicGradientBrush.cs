using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimpleConicGradientBrush : ServerCompositionSimpleGradientBrush, IConicGradientBrush, IGradientBrush, IBrush
{
	private double _angle;

	internal static readonly CompositionProperty<double> s_IdOfAngleProperty = CompositionProperty.Register<ServerCompositionSimpleConicGradientBrush, double>("Angle", (SimpleServerObject obj) => ((ServerCompositionSimpleConicGradientBrush)obj)._angle, delegate(SimpleServerObject obj, double v)
	{
		((ServerCompositionSimpleConicGradientBrush)obj)._angle = v;
	}, (SimpleServerObject obj) => ((ServerCompositionSimpleConicGradientBrush)obj)._angle);

	private RelativePoint _center;

	internal static readonly CompositionProperty<RelativePoint> s_IdOfCenterProperty = CompositionProperty.Register<ServerCompositionSimpleConicGradientBrush, RelativePoint>("Center", (SimpleServerObject obj) => ((ServerCompositionSimpleConicGradientBrush)obj)._center, delegate(SimpleServerObject obj, RelativePoint v)
	{
		((ServerCompositionSimpleConicGradientBrush)obj)._center = v;
	}, null);

	public double Angle
	{
		get
		{
			return _angle;
		}
		set
		{
			bool flag = false;
			if (_angle != value)
			{
				flag = true;
			}
			SetValue(s_IdOfAngleProperty, ref _angle, value);
		}
	}

	public RelativePoint Center
	{
		get
		{
			return _center;
		}
		set
		{
			bool flag = false;
			if (_center != value)
			{
				flag = true;
			}
			SetValue(s_IdOfCenterProperty, ref _center, value);
		}
	}

	internal ServerCompositionSimpleConicGradientBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionSimpleConicGradientBrushChangedFields num = reader.Read<CompositionSimpleConicGradientBrushChangedFields>();
		if ((num & CompositionSimpleConicGradientBrushChangedFields.Angle) == CompositionSimpleConicGradientBrushChangedFields.Angle)
		{
			Angle = reader.Read<double>();
		}
		if ((num & CompositionSimpleConicGradientBrushChangedFields.Center) == CompositionSimpleConicGradientBrushChangedFields.Center)
		{
			Center = reader.Read<RelativePoint>();
		}
	}

	internal static void SerializeAllChanges(BatchStreamWriter writer, double angle, RelativePoint center)
	{
		writer.Write(CompositionSimpleConicGradientBrushChangedFields.Angle | CompositionSimpleConicGradientBrushChangedFields.Center);
		writer.Write(angle);
		writer.Write(center);
	}
}
