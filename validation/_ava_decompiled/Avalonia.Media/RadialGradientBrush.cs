using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// Paints an area with a radial gradient.
/// </summary>
public sealed class RadialGradientBrush : GradientBrush, IRadialGradientBrush, IGradientBrush, IBrush
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RadialGradientBrush.Center" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativePoint> CenterProperty = AvaloniaProperty.Register<RadialGradientBrush, RelativePoint>("Center", RelativePoint.Center);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RadialGradientBrush.GradientOrigin" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativePoint> GradientOriginProperty = AvaloniaProperty.Register<RadialGradientBrush, RelativePoint>("GradientOrigin", RelativePoint.Center);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RadialGradientBrush.RadiusX" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativeScalar> RadiusXProperty = AvaloniaProperty.Register<RadialGradientBrush, RelativeScalar>("RadiusX", RelativeScalar.Middle);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RadialGradientBrush.RadiusX" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativeScalar> RadiusYProperty = AvaloniaProperty.Register<RadialGradientBrush, RelativeScalar>("RadiusY", RelativeScalar.Middle);

	/// <summary>
	/// Gets or sets the start point for the gradient.
	/// </summary>
	public RelativePoint Center
	{
		get
		{
			return GetValue(CenterProperty);
		}
		set
		{
			SetValue(CenterProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the location of the two-dimensional focal point that defines the beginning
	/// of the gradient.
	/// </summary>
	public RelativePoint GradientOrigin
	{
		get
		{
			return GetValue(GradientOriginProperty);
		}
		set
		{
			SetValue(GradientOriginProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the horizontal radius of the outermost circle of the radial
	/// gradient.
	/// </summary>
	public RelativeScalar RadiusX
	{
		get
		{
			return GetValue(RadiusXProperty);
		}
		set
		{
			SetValue(RadiusXProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the vertical radius of the outermost circle of the radial
	/// gradient.
	/// </summary>
	public RelativeScalar RadiusY
	{
		get
		{
			return GetValue(RadiusYProperty);
		}
		set
		{
			SetValue(RadiusYProperty, value);
		}
	}

	internal override Func<Compositor, ServerCompositionSimpleBrush> Factory => (Compositor c) => new ServerCompositionSimpleRadialGradientBrush(c.Server);

	/// <inheritdoc />
	public override IImmutableBrush ToImmutable()
	{
		return new ImmutableRadialGradientBrush(this);
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		ServerCompositionSimpleRadialGradientBrush.SerializeAllChanges(writer, Center, GradientOrigin, RadiusX, RadiusY);
	}
}
