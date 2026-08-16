using System;
using Avalonia.Media;

namespace Avalonia.Animation.Animators;

/// <summary>
/// Animator that interpolates <see cref="T:Avalonia.Media.Color" /> through 
/// gamma sRGB color space for better visual result.
/// </summary>
internal class ColorAnimator : Animator<Color>
{
	/// <summary>
	/// Opto-electronic conversion function for the sRGB color space.
	/// Takes a gamma-encoded sRGB value and converts it to a linear sRGB value.
	/// </summary>
	private static double OECF_sRGB(double linear)
	{
		if (!(linear <= 0.0031308))
		{
			return Math.Pow(linear, 5.0 / 12.0) * 1.055 - 0.055;
		}
		return linear * 12.92;
	}

	/// <summary>
	/// Electro-optical conversion function for the sRGB color space.
	/// Takes a linear sRGB value and converts it to a gamma-encoded sRGB value.
	/// </summary>
	private static double EOCF_sRGB(double srgb)
	{
		if (!(srgb <= 0.04045))
		{
			return Math.Pow((srgb + 0.055) / 1.055, 2.4);
		}
		return srgb / 12.92;
	}

	public override Color Interpolate(double progress, Color oldValue, Color newValue)
	{
		return InterpolateCore(progress, oldValue, newValue);
	}

	internal static Color InterpolateCore(double progress, Color oldValue, Color newValue)
	{
		double num = (double)(int)oldValue.A / 255.0;
		double srgb = (double)(int)oldValue.R / 255.0;
		double srgb2 = (double)(int)oldValue.G / 255.0;
		double srgb3 = (double)(int)oldValue.B / 255.0;
		double num2 = (double)(int)newValue.A / 255.0;
		double srgb4 = (double)(int)newValue.R / 255.0;
		double srgb5 = (double)(int)newValue.G / 255.0;
		double srgb6 = (double)(int)newValue.B / 255.0;
		srgb = EOCF_sRGB(srgb);
		srgb2 = EOCF_sRGB(srgb2);
		srgb3 = EOCF_sRGB(srgb3);
		srgb4 = EOCF_sRGB(srgb4);
		srgb5 = EOCF_sRGB(srgb5);
		srgb6 = EOCF_sRGB(srgb6);
		double num3 = num + progress * (num2 - num);
		double linear = srgb + progress * (srgb4 - srgb);
		double linear2 = srgb2 + progress * (srgb5 - srgb2);
		double linear3 = srgb3 + progress * (srgb6 - srgb3);
		double a = num3 * 255.0;
		linear = OECF_sRGB(linear) * 255.0;
		linear2 = OECF_sRGB(linear2) * 255.0;
		return new Color(b: (byte)Math.Round(OECF_sRGB(linear3) * 255.0), a: (byte)Math.Round(a), r: (byte)Math.Round(linear), g: (byte)Math.Round(linear2));
	}
}
