using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSimpleRadialGradientBrush : ServerCompositionSimpleGradientBrush, IRadialGradientBrush, IGradientBrush, IBrush
{
	private RelativePoint _center;

	internal static readonly CompositionProperty<RelativePoint> s_IdOfCenterProperty = CompositionProperty.Register<ServerCompositionSimpleRadialGradientBrush, RelativePoint>("Center", (SimpleServerObject obj) => ((ServerCompositionSimpleRadialGradientBrush)obj)._center, delegate(SimpleServerObject obj, RelativePoint v)
	{
		((ServerCompositionSimpleRadialGradientBrush)obj)._center = v;
	}, null);

	private RelativePoint _gradientOrigin;

	internal static readonly CompositionProperty<RelativePoint> s_IdOfGradientOriginProperty = CompositionProperty.Register<ServerCompositionSimpleRadialGradientBrush, RelativePoint>("GradientOrigin", (SimpleServerObject obj) => ((ServerCompositionSimpleRadialGradientBrush)obj)._gradientOrigin, delegate(SimpleServerObject obj, RelativePoint v)
	{
		((ServerCompositionSimpleRadialGradientBrush)obj)._gradientOrigin = v;
	}, null);

	private RelativeScalar _radiusX;

	internal static readonly CompositionProperty<RelativeScalar> s_IdOfRadiusXProperty = CompositionProperty.Register<ServerCompositionSimpleRadialGradientBrush, RelativeScalar>("RadiusX", (SimpleServerObject obj) => ((ServerCompositionSimpleRadialGradientBrush)obj)._radiusX, delegate(SimpleServerObject obj, RelativeScalar v)
	{
		((ServerCompositionSimpleRadialGradientBrush)obj)._radiusX = v;
	}, null);

	private RelativeScalar _radiusY;

	internal static readonly CompositionProperty<RelativeScalar> s_IdOfRadiusYProperty = CompositionProperty.Register<ServerCompositionSimpleRadialGradientBrush, RelativeScalar>("RadiusY", (SimpleServerObject obj) => ((ServerCompositionSimpleRadialGradientBrush)obj)._radiusY, delegate(SimpleServerObject obj, RelativeScalar v)
	{
		((ServerCompositionSimpleRadialGradientBrush)obj)._radiusY = v;
	}, null);

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

	public RelativePoint GradientOrigin
	{
		get
		{
			return _gradientOrigin;
		}
		set
		{
			bool flag = false;
			if (_gradientOrigin != value)
			{
				flag = true;
			}
			SetValue(s_IdOfGradientOriginProperty, ref _gradientOrigin, value);
		}
	}

	public RelativeScalar RadiusX
	{
		get
		{
			return _radiusX;
		}
		set
		{
			bool flag = false;
			if (_radiusX != value)
			{
				flag = true;
			}
			SetValue(s_IdOfRadiusXProperty, ref _radiusX, value);
		}
	}

	public RelativeScalar RadiusY
	{
		get
		{
			return _radiusY;
		}
		set
		{
			bool flag = false;
			if (_radiusY != value)
			{
				flag = true;
			}
			SetValue(s_IdOfRadiusYProperty, ref _radiusY, value);
		}
	}

	internal ServerCompositionSimpleRadialGradientBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionSimpleRadialGradientBrushChangedFields num = reader.Read<CompositionSimpleRadialGradientBrushChangedFields>();
		if ((num & CompositionSimpleRadialGradientBrushChangedFields.Center) == CompositionSimpleRadialGradientBrushChangedFields.Center)
		{
			Center = reader.Read<RelativePoint>();
		}
		if ((num & CompositionSimpleRadialGradientBrushChangedFields.GradientOrigin) == CompositionSimpleRadialGradientBrushChangedFields.GradientOrigin)
		{
			GradientOrigin = reader.Read<RelativePoint>();
		}
		if ((num & CompositionSimpleRadialGradientBrushChangedFields.RadiusX) == CompositionSimpleRadialGradientBrushChangedFields.RadiusX)
		{
			RadiusX = reader.Read<RelativeScalar>();
		}
		if ((num & CompositionSimpleRadialGradientBrushChangedFields.RadiusY) == CompositionSimpleRadialGradientBrushChangedFields.RadiusY)
		{
			RadiusY = reader.Read<RelativeScalar>();
		}
	}

	internal static void SerializeAllChanges(BatchStreamWriter writer, RelativePoint center, RelativePoint gradientOrigin, RelativeScalar radiusX, RelativeScalar radiusY)
	{
		writer.Write(CompositionSimpleRadialGradientBrushChangedFields.Center | CompositionSimpleRadialGradientBrushChangedFields.GradientOrigin | CompositionSimpleRadialGradientBrushChangedFields.RadiusX | CompositionSimpleRadialGradientBrushChangedFields.RadiusY);
		writer.Write(center);
		writer.Write(gradientOrigin);
		writer.Write(radiusX);
		writer.Write(radiusY);
	}
}
