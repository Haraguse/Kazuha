using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Media.Immutable;

namespace Avalonia.Media;

/// <summary>
/// A collection of <see cref="T:Avalonia.Media.GradientStop" />s.
/// </summary>
public class GradientStops : AvaloniaList<GradientStop>
{
	public GradientStops()
	{
		base.ResetBehavior = ResetBehavior.Remove;
	}

	public IReadOnlyList<ImmutableGradientStop> ToImmutable()
	{
		int count = base.Count;
		ImmutableGradientStop[] array = new ImmutableGradientStop[count];
		for (int i = 0; i < count; i++)
		{
			GradientStop gradientStop = base[i];
			array[i] = new ImmutableGradientStop(gradientStop.Offset, gradientStop.Color);
		}
		return array;
	}
}
