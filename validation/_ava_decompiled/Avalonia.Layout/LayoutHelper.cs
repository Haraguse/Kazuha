using System;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Layout;

/// <summary>
/// Provides helper methods needed for layout.
/// </summary>
public static class LayoutHelper
{
	/// <summary>
	/// Epsilon value used for certain layout calculations.
	/// Based on the value in WPF LayoutDoubleUtil.
	/// </summary>
	public static double LayoutEpsilon { get; } = 1.53E-06;

	/// <summary>
	/// Calculates a control's size based on its <see cref="P:Avalonia.Layout.Layoutable.Width" />,
	/// <see cref="P:Avalonia.Layout.Layoutable.Height" />, <see cref="P:Avalonia.Layout.Layoutable.MinWidth" />,
	/// <see cref="P:Avalonia.Layout.Layoutable.MaxWidth" />, <see cref="P:Avalonia.Layout.Layoutable.MinHeight" /> and
	/// <see cref="P:Avalonia.Layout.Layoutable.MaxHeight" />.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <param name="constraints">The space available for the control.</param>
	/// <returns>The control's size.</returns>
	public static Size ApplyLayoutConstraints(Layoutable control, Size constraints)
	{
		return ApplyLayoutConstraints(new MinMax(control), constraints);
	}

	internal static Size ApplyLayoutConstraints(MinMax minMax, Size constraints)
	{
		return new Size(MathUtilities.Clamp(constraints.Width, minMax.MinWidth, minMax.MaxWidth), MathUtilities.Clamp(constraints.Height, minMax.MinHeight, minMax.MaxHeight));
	}

	public static Size MeasureChild(Layoutable? control, Size availableSize, Thickness padding, Thickness borderThickness)
	{
		if (IsParentLayoutRounded(control, out var scale))
		{
			padding = RoundLayoutThickness(padding, scale);
			borderThickness = RoundLayoutThickness(borderThickness, scale);
		}
		if (control != null)
		{
			control.Measure(availableSize.Deflate(padding + borderThickness));
			return control.DesiredSize.Inflate(padding + borderThickness);
		}
		return default(Size).Inflate(padding + borderThickness);
	}

	public static Size MeasureChild(Layoutable? control, Size availableSize, Thickness padding)
	{
		if (IsParentLayoutRounded(control, out var scale))
		{
			padding = RoundLayoutThickness(padding, scale);
		}
		if (control != null)
		{
			control.Measure(availableSize.Deflate(padding));
			return control.DesiredSize.Inflate(padding);
		}
		return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
	}

	public static Size ArrangeChild(Layoutable? child, Size availableSize, Thickness padding, Thickness borderThickness)
	{
		if (IsParentLayoutRounded(child, out var scale))
		{
			padding = RoundLayoutThickness(padding, scale);
			borderThickness = RoundLayoutThickness(borderThickness, scale);
		}
		return ArrangeChildInternal(child, availableSize, padding + borderThickness);
	}

	public static Size ArrangeChild(Layoutable? child, Size availableSize, Thickness padding)
	{
		if (IsParentLayoutRounded(child, out var scale))
		{
			padding = RoundLayoutThickness(padding, scale);
		}
		return ArrangeChildInternal(child, availableSize, padding);
	}

	private static Size ArrangeChildInternal(Layoutable? child, Size availableSize, Thickness padding)
	{
		child?.Arrange(new Rect(availableSize).Deflate(padding));
		return availableSize;
	}

	private static bool IsParentLayoutRounded(Layoutable? child, out double scale)
	{
		if (!(child?.GetVisualParent() is Layoutable { UseLayoutRounding: not false } layoutable))
		{
			scale = 1.0;
			return false;
		}
		scale = GetLayoutScale(layoutable);
		return true;
	}

	/// <summary>
	/// Invalidates measure for given control and all visual children recursively.
	/// </summary>
	public static void InvalidateSelfAndChildrenMeasure(Layoutable control)
	{
		if (control != null)
		{
			InnerInvalidateMeasure(control);
		}
		static void InnerInvalidateMeasure(Visual target)
		{
			if (target is Layoutable layoutable)
			{
				layoutable.InvalidateMeasure();
			}
			IAvaloniaList<Visual> visualChildren = target.VisualChildren;
			int count = visualChildren.Count;
			for (int i = 0; i < count; i++)
			{
				InnerInvalidateMeasure(visualChildren[i]);
			}
		}
	}

	/// <summary>
	/// Obtains layout scale of the given control.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <exception cref="T:System.Exception">Thrown when control has no root or returned layout scaling is invalid.</exception>
	public static double GetLayoutScale(Layoutable control)
	{
		return control.GetLayoutRoot()?.LayoutScaling ?? 1.0;
	}

	/// <summary>
	/// Rounds a size to integer values for layout purposes, compensating for high DPI screen
	/// coordinates by rounding the size up to the nearest pixel.
	/// </summary>
	/// <param name="size">Input size.</param>
	/// <param name="dpiScale">The DPI scale.</param>
	/// <returns>Value of size that will be rounded under screen DPI.</returns>
	/// <remarks>
	/// This is a layout helper method. It takes DPI into account and also does not return
	/// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper
	/// associated with the UseLayoutRounding property and should not be used as a general rounding
	/// utility.
	/// </remarks>
	public static Size RoundLayoutSizeUp(Size size, double dpiScale)
	{
		if (dpiScale != 1.0)
		{
			return new Size(Math.Ceiling(RoundTo8Digits(size.Width) * dpiScale) / dpiScale, Math.Ceiling(RoundTo8Digits(size.Height) * dpiScale) / dpiScale);
		}
		return new Size(Math.Ceiling(size.Width), Math.Ceiling(size.Height));
	}

	/// <summary>
	/// Rounds a thickness to integer values for layout purposes, compensating for high DPI screen
	/// coordinates.
	/// </summary>
	/// <param name="thickness">Input thickness.</param>
	/// <param name="dpiScale">The DPI scale.</param>
	/// <returns>Value of thickness that will be rounded under screen DPI.</returns>
	/// <remarks>
	/// This is a layout helper method. It takes DPI into account and also does not return
	/// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper
	/// associated with the UseLayoutRounding property and should not be used as a general rounding
	/// utility.
	/// </remarks>
	public static Thickness RoundLayoutThickness(Thickness thickness, double dpiScale)
	{
		if (dpiScale != 1.0)
		{
			return new Thickness(Math.Round(thickness.Left * dpiScale) / dpiScale, Math.Round(thickness.Top * dpiScale) / dpiScale, Math.Round(thickness.Right * dpiScale) / dpiScale, Math.Round(thickness.Bottom * dpiScale) / dpiScale);
		}
		return new Thickness(Math.Round(thickness.Left), Math.Round(thickness.Top), Math.Round(thickness.Right), Math.Round(thickness.Bottom));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point RoundLayoutPoint(Point point, double dpiScale)
	{
		if (dpiScale != 1.0)
		{
			return new Point(Math.Round(point.X * dpiScale) / dpiScale, Math.Round(point.Y * dpiScale) / dpiScale);
		}
		return new Point(Math.Round(point.X), Math.Round(point.Y));
	}

	/// <summary>
	/// Calculates the value to be used for layout rounding at high DPI by rounding the value
	/// up or down to the nearest pixel.
	/// </summary>
	/// <param name="value">Input value to be rounded.</param>
	/// <param name="dpiScale">Ratio of screen's DPI to layout DPI</param>
	/// <returns>Adjusted value that will produce layout rounding on screen at high dpi.</returns>
	/// <remarks>
	/// This is a layout helper method. It takes DPI into account and also does not return
	/// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper
	/// associated with the UseLayoutRounding property and should not be used as a general rounding
	/// utility.
	/// </remarks>
	public static double RoundLayoutValue(double value, double dpiScale)
	{
		if (dpiScale != 1.0)
		{
			return Math.Round(value * dpiScale) / dpiScale;
		}
		return Math.Round(value);
	}

	/// <summary>
	/// Calculates the value to be used for layout rounding at high DPI by rounding the value up
	/// to the nearest pixel.
	/// </summary>
	/// <param name="value">Input value to be rounded.</param>
	/// <param name="dpiScale">Ratio of screen's DPI to layout DPI</param>
	/// <returns>Adjusted value that will produce layout rounding on screen at high dpi.</returns>
	/// <remarks>
	/// This is a layout helper method. It takes DPI into account and also does not return
	/// the rounded value if it is unacceptable for layout, e.g. Infinity or NaN. It's a helper
	/// associated with the UseLayoutRounding property and should not be used as a general rounding
	/// utility.
	/// </remarks>
	public static double RoundLayoutValueUp(double value, double dpiScale)
	{
		if (dpiScale != 1.0)
		{
			return Math.Ceiling(RoundTo8Digits(value) * dpiScale) / dpiScale;
		}
		return Math.Ceiling(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double RoundTo8Digits(double value)
	{
		return Math.Round(value, 8, MidpointRounding.ToZero);
	}

	internal static double ValidateScaling(double scaling)
	{
		if (MathUtilities.IsNegativeOrNonFinite(scaling) || MathUtilities.IsZero(scaling))
		{
			throw new InvalidOperationException($"Invalid render scaling value {scaling}");
		}
		if (MathUtilities.IsOne(scaling))
		{
			return 1.0;
		}
		return scaling;
	}
}
