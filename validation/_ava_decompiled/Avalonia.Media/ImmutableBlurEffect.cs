using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Media;

public class ImmutableBlurEffect : IBlurEffect, IEffect, IImmutableEffect, IEquatable<IEffect>
{
	public double Radius { get; }

	static ImmutableBlurEffect()
	{
		EffectAnimator.EnsureRegistered();
	}

	public ImmutableBlurEffect(double radius)
	{
		Radius = radius;
	}

	public bool Equals(IEffect? other)
	{
		if (other is IBlurEffect blurEffect)
		{
			return blurEffect.Radius == Radius;
		}
		return false;
	}
}
