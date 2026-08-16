namespace Avalonia.Media;

public sealed class BlurEffect : Effect, IBlurEffect, IEffect, IMutableEffect
{
	public static readonly StyledProperty<double> RadiusProperty;

	public double Radius
	{
		get
		{
			return GetValue(RadiusProperty);
		}
		set
		{
			SetValue(RadiusProperty, value);
		}
	}

	static BlurEffect()
	{
		RadiusProperty = AvaloniaProperty.Register<BlurEffect, double>("Radius", 5.0);
		Effect.AffectsRender<BlurEffect>(new AvaloniaProperty[1] { RadiusProperty });
	}

	public IImmutableEffect ToImmutable()
	{
		return new ImmutableBlurEffect(Radius);
	}
}
