namespace Avalonia.Media;

public sealed class DropShadowEffect : DropShadowEffectBase, IDropShadowEffect, IEffect, IMutableEffect
{
	public static readonly StyledProperty<double> OffsetXProperty;

	public static readonly StyledProperty<double> OffsetYProperty;

	public double OffsetX
	{
		get
		{
			return GetValue(OffsetXProperty);
		}
		set
		{
			SetValue(OffsetXProperty, value);
		}
	}

	public double OffsetY
	{
		get
		{
			return GetValue(OffsetYProperty);
		}
		set
		{
			SetValue(OffsetYProperty, value);
		}
	}

	static DropShadowEffect()
	{
		OffsetXProperty = AvaloniaProperty.Register<DropShadowEffect, double>("OffsetX", 3.5355);
		OffsetYProperty = AvaloniaProperty.Register<DropShadowEffect, double>("OffsetY", 3.5355);
		Effect.AffectsRender<DropShadowEffect>(new AvaloniaProperty[2] { OffsetXProperty, OffsetYProperty });
	}

	public IImmutableEffect ToImmutable()
	{
		return new ImmutableDropShadowEffect(OffsetX, OffsetY, base.BlurRadius, base.Color, base.Opacity);
	}
}
