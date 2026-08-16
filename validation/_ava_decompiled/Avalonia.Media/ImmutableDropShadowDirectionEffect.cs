using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Media;

public class ImmutableDropShadowDirectionEffect : IDirectionDropShadowEffect, IDropShadowEffect, IEffect, IImmutableEffect, IEquatable<IEffect>
{
	public double OffsetX => Math.Cos(Direction * Math.PI / 180.0) * ShadowDepth;

	public double OffsetY => Math.Sin(Direction * Math.PI / 180.0) * ShadowDepth;

	public double Direction { get; }

	public double ShadowDepth { get; }

	public double BlurRadius { get; }

	public Color Color { get; }

	public double Opacity { get; }

	static ImmutableDropShadowDirectionEffect()
	{
		EffectAnimator.EnsureRegistered();
	}

	public ImmutableDropShadowDirectionEffect(double direction, double shadowDepth, double blurRadius, Color color, double opacity)
	{
		Direction = direction;
		ShadowDepth = shadowDepth;
		BlurRadius = blurRadius;
		Color = color;
		Opacity = opacity;
	}

	public bool Equals(IEffect? other)
	{
		if (other is IDropShadowEffect dropShadowEffect && dropShadowEffect.OffsetX == OffsetX && dropShadowEffect.OffsetY == OffsetY && dropShadowEffect.BlurRadius == BlurRadius && dropShadowEffect.Color == Color)
		{
			return dropShadowEffect.Opacity == Opacity;
		}
		return false;
	}
}
