using Avalonia.Media;

namespace Avalonia.Animation.Animators;

internal class BlurEffectAnimator : EffectAnimatorBase<IBlurEffect>
{
	private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

	protected override IBlurEffect Interpolate(double progress, IBlurEffect oldValue, IBlurEffect newValue)
	{
		return new ImmutableBlurEffect(s_doubleAnimator.Interpolate(progress, oldValue.Radius, newValue.Radius));
	}
}
