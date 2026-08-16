using System;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Animations;

internal class ColorInterpolator : IInterpolator<Color>
{
	public static ColorInterpolator Instance { get; } = new ColorInterpolator();

	private static byte Lerp(float a, float b, float p)
	{
		return (byte)Math.Max(0f, Math.Min(255f, p * (b - a) + a));
	}

	public static Color LerpRGB(Color to, Color from, float progress)
	{
		return new Color(Lerp((int)to.A, (int)from.A, progress), Lerp((int)to.R, (int)from.R, progress), Lerp((int)to.G, (int)from.G, progress), Lerp((int)to.B, (int)from.B, progress));
	}

	public Color Interpolate(Color from, Color to, float progress)
	{
		return LerpRGB(from, to, progress);
	}
}
