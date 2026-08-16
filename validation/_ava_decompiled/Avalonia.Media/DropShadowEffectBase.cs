namespace Avalonia.Media;

public abstract class DropShadowEffectBase : Effect
{
	public static readonly StyledProperty<double> BlurRadiusProperty;

	public static readonly StyledProperty<Color> ColorProperty;

	public static readonly StyledProperty<double> OpacityProperty;

	public double BlurRadius
	{
		get
		{
			return GetValue(BlurRadiusProperty);
		}
		set
		{
			SetValue(BlurRadiusProperty, value);
		}
	}

	public Color Color
	{
		get
		{
			return GetValue(ColorProperty);
		}
		set
		{
			SetValue(ColorProperty, value);
		}
	}

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

	static DropShadowEffectBase()
	{
		BlurRadiusProperty = AvaloniaProperty.Register<DropShadowEffectBase, double>("BlurRadius", 5.0);
		ColorProperty = AvaloniaProperty.Register<DropShadowEffectBase, Color>("Color", Colors.Black);
		OpacityProperty = AvaloniaProperty.Register<DropShadowEffectBase, double>("Opacity", 1.0);
		Effect.AffectsRender<DropShadowEffectBase>(new AvaloniaProperty[3] { BlurRadiusProperty, ColorProperty, OpacityProperty });
	}
}
