using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media;

/// <summary>
/// Represents a text decoration, which is a visual ornamentation that is added to text (such as an underline).
/// </summary>
public class TextDecoration : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.Location" /> property.
	/// </summary>
	public static readonly StyledProperty<TextDecorationLocation> LocationProperty = AvaloniaProperty.Register<TextDecoration, TextDecorationLocation>("Location", TextDecorationLocation.Underline);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.Stroke" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<TextDecoration, IBrush>("Stroke");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeThicknessUnit" /> property.
	/// </summary>
	public static readonly StyledProperty<TextDecorationUnit> StrokeThicknessUnitProperty = AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>("StrokeThicknessUnit", TextDecorationUnit.FontRecommended);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeDashArray" /> property.
	/// </summary>
	public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty = AvaloniaProperty.Register<TextDecoration, AvaloniaList<double>>("StrokeDashArray");

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeDashOffset" /> property.
	/// </summary>
	public static readonly StyledProperty<double> StrokeDashOffsetProperty = AvaloniaProperty.Register<TextDecoration, double>("StrokeDashOffset", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeThickness" /> property.
	/// </summary>
	public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<TextDecoration, double>("StrokeThickness", 1.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeLineCap" /> property.
	/// </summary>
	public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty = AvaloniaProperty.Register<TextDecoration, PenLineCap>("StrokeLineCap", PenLineCap.Flat);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeOffset" /> property.
	/// </summary>
	public static readonly StyledProperty<double> StrokeOffsetProperty = AvaloniaProperty.Register<TextDecoration, double>("StrokeOffset", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TextDecoration.StrokeOffsetUnit" /> property.
	/// </summary>
	public static readonly StyledProperty<TextDecorationUnit> StrokeOffsetUnitProperty = AvaloniaProperty.Register<TextDecoration, TextDecorationUnit>("StrokeOffsetUnit", TextDecorationUnit.FontRecommended);

	/// <summary>
	/// Gets or sets the location.
	/// </summary>
	/// <value>
	/// The location.
	/// </value>
	public TextDecorationLocation Location
	{
		get
		{
			return GetValue(LocationProperty);
		}
		set
		{
			SetValue(LocationProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.IBrush" /> that specifies how the <see cref="T:Avalonia.Media.TextDecoration" /> is painted.
	/// </summary>
	public IBrush? Stroke
	{
		get
		{
			return GetValue(StrokeProperty);
		}
		set
		{
			SetValue(StrokeProperty, value);
		}
	}

	/// <summary>
	/// Gets the units in which the thickness of the <see cref="T:Avalonia.Media.TextDecoration" /> is expressed.
	/// </summary>
	public TextDecorationUnit StrokeThicknessUnit
	{
		get
		{
			return GetValue(StrokeThicknessUnitProperty);
		}
		set
		{
			SetValue(StrokeThicknessUnitProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a collection of <see cref="T:System.Double" /> values that indicate the pattern of dashes and gaps
	/// that is used to draw the <see cref="T:Avalonia.Media.TextDecoration" />.
	/// </summary>
	public AvaloniaList<double>? StrokeDashArray
	{
		get
		{
			return GetValue(StrokeDashArrayProperty);
		}
		set
		{
			SetValue(StrokeDashArrayProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
	/// </summary>
	public double StrokeDashOffset
	{
		get
		{
			return GetValue(StrokeDashOffsetProperty);
		}
		set
		{
			SetValue(StrokeDashOffsetProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the thickness of the <see cref="T:Avalonia.Media.TextDecoration" />.
	/// </summary>
	public double StrokeThickness
	{
		get
		{
			return GetValue(StrokeThicknessProperty);
		}
		set
		{
			SetValue(StrokeThicknessProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a <see cref="T:Avalonia.Media.PenLineCap" /> enumeration value that describes the shape at the ends of a line.
	/// </summary>
	public PenLineCap StrokeLineCap
	{
		get
		{
			return GetValue(StrokeLineCapProperty);
		}
		set
		{
			SetValue(StrokeLineCapProperty, value);
		}
	}

	/// <summary>
	/// The stroke's offset.
	/// </summary>
	/// <value>
	/// The pen offset.
	/// </value>
	public double StrokeOffset
	{
		get
		{
			return GetValue(StrokeOffsetProperty);
		}
		set
		{
			SetValue(StrokeOffsetProperty, value);
		}
	}

	/// <summary>
	/// Gets the units in which the <see cref="P:Avalonia.Media.TextDecoration.StrokeOffset" /> value is expressed.
	/// </summary>
	public TextDecorationUnit StrokeOffsetUnit
	{
		get
		{
			return GetValue(StrokeOffsetUnitProperty);
		}
		set
		{
			SetValue(StrokeOffsetUnitProperty, value);
		}
	}

	/// <summary>
	/// Draws the <see cref="T:Avalonia.Media.TextDecoration" /> at given origin.
	/// </summary>
	/// <param name="drawingContext">The drawing context.</param>
	/// <param name="glyphRun">The decorated run.</param>
	/// <param name="textMetrics">The font metrics of the decorated run.</param>
	/// <param name="defaultBrush">The default brush that is used to draw the decoration.</param>
	internal void Draw(DrawingContext drawingContext, GlyphRun glyphRun, TextMetrics textMetrics, IBrush defaultBrush)
	{
		Point baselineOrigin = glyphRun.BaselineOrigin;
		double num = StrokeThickness;
		switch (StrokeThicknessUnit)
		{
		case TextDecorationUnit.FontRecommended:
			switch (Location)
			{
			case TextDecorationLocation.Underline:
				num = textMetrics.UnderlineThickness;
				break;
			case TextDecorationLocation.Strikethrough:
				num = textMetrics.StrikethroughThickness;
				break;
			}
			break;
		case TextDecorationUnit.FontRenderingEmSize:
			num = textMetrics.FontRenderingEmSize * num;
			break;
		}
		Point point = baselineOrigin;
		switch (Location)
		{
		case TextDecorationLocation.Overline:
			point += new Point(0.0, textMetrics.Ascent);
			break;
		case TextDecorationLocation.Strikethrough:
			point += new Point(0.0, textMetrics.StrikethroughPosition);
			break;
		case TextDecorationLocation.Underline:
			point += new Point(0.0, textMetrics.UnderlinePosition);
			break;
		}
		switch (StrokeOffsetUnit)
		{
		case TextDecorationUnit.FontRenderingEmSize:
			point += new Point(0.0, StrokeOffset * textMetrics.FontRenderingEmSize);
			break;
		case TextDecorationUnit.Pixel:
			point += new Point(0.0, StrokeOffset);
			break;
		}
		Pen pen = new Pen(Stroke ?? defaultBrush, num, new DashStyle(StrokeDashArray, StrokeDashOffset), StrokeLineCap);
		if (Location != TextDecorationLocation.Strikethrough)
		{
			double num2 = glyphRun.BaselineOrigin.Y - point.Y;
			IReadOnlyList<float> intersections = glyphRun.GetIntersections((float)(num * 0.5 - num2), (float)(num * 1.5 - num2));
			if (intersections.Count > 0)
			{
				double num3 = baselineOrigin.X;
				double num4 = num3 + glyphRun.Bounds.Width;
				double num5 = num3;
				List<double> list = new List<double>();
				for (int i = 0; i < intersections.Count; i += 2)
				{
					double num6 = (double)intersections[i] - num;
					num5 = (double)intersections[i + 1] + num;
					if (num6 > num3 && num3 + textMetrics.FontRenderingEmSize / 12.0 < num6)
					{
						list.Add(num3);
						list.Add(num6);
					}
					num3 = num5;
				}
				if (num5 < num4)
				{
					list.Add(num5);
					list.Add(num4);
				}
				for (int j = 0; j < list.Count; j += 2)
				{
					Point p = new Point(list[j], point.Y);
					Point p2 = new Point(list[j + 1], point.Y);
					drawingContext.DrawLine(pen, p, p2);
				}
				return;
			}
		}
		Point point2 = point;
		Point p3 = point2 + new Point(glyphRun.Metrics.Width, 0.0);
		drawingContext.DrawLine(pen, point2, p3);
	}
}
