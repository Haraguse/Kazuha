using System;

namespace Avalonia.Media;

public static class EffectExtensions
{
	private static double AdjustPaddingRadius(double radius)
	{
		if (radius <= 0.0)
		{
			return 0.0;
		}
		return Math.Ceiling(radius) + 1.0;
	}

	internal static Thickness GetEffectOutputPadding(this IEffect? effect)
	{
		if (effect == null)
		{
			return default(Thickness);
		}
		if (effect is IBlurEffect blurEffect)
		{
			return new Thickness(AdjustPaddingRadius(blurEffect.Radius));
		}
		if (effect is IDropShadowEffect dropShadowEffect)
		{
			double num = AdjustPaddingRadius(dropShadowEffect.BlurRadius);
			Rect rect = new Rect(0.0 - num, 0.0 - num, num * 2.0, num * 2.0).Translate(new Vector(dropShadowEffect.OffsetX, dropShadowEffect.OffsetY));
			return new Thickness(Math.Max(0.0, 0.0 - rect.X), Math.Max(0.0, 0.0 - rect.Y), Math.Max(0.0, rect.Right), Math.Max(0.0, rect.Bottom));
		}
		throw new ArgumentException("Unknown effect type: " + effect.GetType());
	}

	/// <summary>
	/// Converts a effect to an immutable effect.
	/// </summary>
	/// <param name="effect">The effect.</param>
	/// <returns>
	/// The result of calling <see cref="M:Avalonia.Media.IMutableEffect.ToImmutable" /> if the effect is mutable,
	/// otherwise <paramref name="effect" />.
	/// </returns>
	public static IImmutableEffect ToImmutable(this IEffect effect)
	{
		if (effect == null)
		{
			throw new ArgumentNullException("effect");
		}
		return (effect as IMutableEffect)?.ToImmutable() ?? ((IImmutableEffect)effect);
	}

	internal static bool EffectEquals(this IImmutableEffect? immutable, IEffect? right)
	{
		if (immutable == null && right == null)
		{
			return true;
		}
		if (immutable != null && right != null)
		{
			return immutable.Equals(right);
		}
		return false;
	}
}
