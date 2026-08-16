using Avalonia;

namespace FluentAvalonia.UI.Controls;

internal static class OrientationBasedMeasuresExt
{
	public static double Major(this IOrientationBasedMeasures m, Size size)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Size)(ref size)).Width;
		}
		return ((Size)(ref size)).Height;
	}

	public static double Minor(this IOrientationBasedMeasures m, Size size)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Size)(ref size)).Height;
		}
		return ((Size)(ref size)).Width;
	}

	public static double MajorSize(this IOrientationBasedMeasures m, Rect rect)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Rect)(ref rect)).Width;
		}
		return ((Rect)(ref rect)).Height;
	}

	public static void SetMajorSize(this IOrientationBasedMeasures m, ref Rect rect, double value)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect = ((Rect)(ref rect)).WithHeight(value);
		}
		else
		{
			rect = ((Rect)(ref rect)).WithWidth(value);
		}
	}

	public static double MinorSize(this IOrientationBasedMeasures m, Rect rect)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Rect)(ref rect)).Height;
		}
		return ((Rect)(ref rect)).Width;
	}

	public static void SetMinorSize(this IOrientationBasedMeasures m, ref Rect rect, double value)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect = ((Rect)(ref rect)).WithWidth(value);
		}
		else
		{
			rect = ((Rect)(ref rect)).WithHeight(value);
		}
	}

	public static double MajorStart(this IOrientationBasedMeasures m, Rect rect)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Rect)(ref rect)).X;
		}
		return ((Rect)(ref rect)).Y;
	}

	public static double MajorEnd(this IOrientationBasedMeasures m, Rect rect)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Rect)(ref rect)).X + ((Rect)(ref rect)).Width;
		}
		return ((Rect)(ref rect)).Y + ((Rect)(ref rect)).Height;
	}

	public static double MinorStart(this IOrientationBasedMeasures m, Rect rect)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Rect)(ref rect)).Y;
		}
		return ((Rect)(ref rect)).X;
	}

	public static void SetMinorStart(this IOrientationBasedMeasures m, ref Rect rect, double value)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect = ((Rect)(ref rect)).WithX(value);
		}
		else
		{
			rect = ((Rect)(ref rect)).WithY(value);
		}
	}

	public static void SetMajorStart(this IOrientationBasedMeasures m, ref Rect rect, double value)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect = ((Rect)(ref rect)).WithY(value);
		}
		else
		{
			rect = ((Rect)(ref rect)).WithX(value);
		}
	}

	public static double MinorEnd(this IOrientationBasedMeasures m, Rect rect)
	{
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return ((Rect)(ref rect)).Y + ((Rect)(ref rect)).Height;
		}
		return ((Rect)(ref rect)).X + ((Rect)(ref rect)).Width;
	}

	public static Rect MinorMajorRect(this IOrientationBasedMeasures m, double minor, double major, double minorSize, double majorSize)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return new Rect(major, minor, majorSize, minorSize);
		}
		return new Rect(minor, major, minorSize, majorSize);
	}

	public static Point MinorMajorPoint(this IOrientationBasedMeasures m, double minor, double major)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return new Point(major, minor);
		}
		return new Point(minor, major);
	}

	public static Size MinorMajorSize(this IOrientationBasedMeasures m, double minor, double major)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (m.ScrollOrientation != ScrollOrientation.Vertical)
		{
			return new Size(major, minor);
		}
		return new Size(minor, major);
	}
}
