using Avalonia.Media;

namespace Avalonia.Animation.Animators;

internal class DropShadowEffectAnimator : EffectAnimatorBase<IDropShadowEffect>
{
	private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

	protected override IDropShadowEffect Interpolate(double progress, IDropShadowEffect oldValue, IDropShadowEffect newValue)
	{
		double blurRadius = s_doubleAnimator.Interpolate(progress, oldValue.BlurRadius, newValue.BlurRadius);
		Color color = ColorAnimator.InterpolateCore(progress, oldValue.Color, newValue.Color);
		double opacity = s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity);
		if (oldValue is IDirectionDropShadowEffect directionDropShadowEffect && newValue is IDirectionDropShadowEffect directionDropShadowEffect2)
		{
			return new ImmutableDropShadowDirectionEffect(s_doubleAnimator.Interpolate(progress, directionDropShadowEffect.Direction, directionDropShadowEffect2.Direction), s_doubleAnimator.Interpolate(progress, directionDropShadowEffect.ShadowDepth, directionDropShadowEffect2.ShadowDepth), blurRadius, color, opacity);
		}
		return new ImmutableDropShadowEffect(s_doubleAnimator.Interpolate(progress, oldValue.OffsetX, newValue.OffsetX), s_doubleAnimator.Interpolate(progress, oldValue.OffsetY, newValue.OffsetY), blurRadius, color, opacity);
	}
}
