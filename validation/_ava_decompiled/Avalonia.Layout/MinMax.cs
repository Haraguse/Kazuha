using System.Runtime.CompilerServices;

namespace Avalonia.Layout;

internal struct MinMax
{
	public double MinWidth;

	public double MaxWidth;

	public double MinHeight;

	public double MaxHeight;

	public MinMax(Layoutable e)
	{
		(MinWidth, MaxWidth) = CalcMinMax(e.Width, e.MinWidth, e.MaxWidth);
		(MinHeight, MaxHeight) = CalcMinMax(e.Height, e.MinHeight, e.MaxHeight);
	}

	private static (double Min, double Max) CalcMinMax(double value, double min, double max)
	{
		double value2;
		double value3;
		if (double.IsNaN(value))
		{
			value2 = 0.0;
			value3 = double.PositiveInfinity;
		}
		else
		{
			value2 = (value3 = value);
		}
		max = ClampUnchecked(value3, min, max);
		min = ClampUnchecked(value2, min, max);
		return (Min: min, Max: max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double ClampUnchecked(double value, double min, double max)
	{
		if (value > max)
		{
			value = max;
		}
		if (value < min)
		{
			value = min;
		}
		return value;
	}
}
