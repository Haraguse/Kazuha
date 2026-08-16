using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Media;

public class ImmutableDropShadowEffect : IDropShadowEffect, IEffect, IImmutableEffect, IEquatable<IEffect>
{
	public double OffsetX { get; }

	public double OffsetY { get; }

	public double BlurRadius { get; }

	public Color Color { get; }

	public double Opacity { get; }

	static ImmutableDropShadowEffect()
	{
		EffectAnimator.EnsureRegistered();
	}

	public ImmutableDropShadowEffect(double offsetX, double offsetY, double blurRadius, Color color, double opacity)
	{
		OffsetX = offsetX;
		OffsetY = offsetY;
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
