using System;

namespace Avalonia.Media;

/// <summary>
/// This class is compatible with WPF's DropShadowEffect and provides Direction and ShadowDepth properties instead of OffsetX/OffsetY
/// </summary>
public sealed class DropShadowDirectionEffect : DropShadowEffectBase, IDirectionDropShadowEffect, IDropShadowEffect, IEffect, IMutableEffect
{
	public static readonly StyledProperty<double> ShadowDepthProperty;

	public static readonly StyledProperty<double> DirectionProperty;

	public double ShadowDepth
	{
		get
		{
			return GetValue(ShadowDepthProperty);
		}
		set
		{
			SetValue(ShadowDepthProperty, value);
		}
	}

	public double Direction
	{
		get
		{
			return GetValue(DirectionProperty);
		}
		set
		{
			SetValue(DirectionProperty, value);
		}
	}

	public double OffsetX => Math.Cos(Direction * Math.PI / 180.0) * ShadowDepth;

	public double OffsetY => Math.Sin(Direction * Math.PI / 180.0) * ShadowDepth;

	static DropShadowDirectionEffect()
	{
		ShadowDepthProperty = AvaloniaProperty.Register<DropShadowDirectionEffect, double>("ShadowDepth", 5.0);
		DirectionProperty = AvaloniaProperty.Register<DropShadowDirectionEffect, double>("Direction", 315.0);
		Effect.AffectsRender<DropShadowDirectionEffect>(new AvaloniaProperty[2] { ShadowDepthProperty, DirectionProperty });
	}

	public IImmutableEffect ToImmutable()
	{
		return new ImmutableDropShadowDirectionEffect(OffsetX, OffsetY, base.BlurRadius, base.Color, base.Opacity);
	}
}
