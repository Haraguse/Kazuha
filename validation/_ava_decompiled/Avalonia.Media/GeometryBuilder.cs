using System;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Contains internal helpers used to build and draw various geometries.
/// </summary>
internal class GeometryBuilder
{
	/// <summary>
	/// Represents the keypoints of a rounded rectangle.
	/// These keypoints can be shared between methods and turned into geometry.
	/// </summary>
	/// <remarks>
	/// A rounded rectangle is the base geometric shape used when drawing borders.
	/// It is a superset of a simple rectangle (which has corner radii set to zero).
	/// These keypoints can be combined together to produce geometries for both background
	/// and border elements.
	/// </remarks>
	internal struct RoundedRectKeypoints
	{
		/// <summary>
		/// Gets the topmost point in the left line segment of the rectangle.
		/// </summary>
		public Point LeftTop { get; set; }

		/// <summary>
		/// Gets the leftmost point in the top line segment of the rectangle.
		/// </summary>
		public Point TopLeft { get; set; }

		/// <summary>
		/// Gets the rightmost point in the top line segment of the rectangle.
		/// </summary>
		public Point TopRight { get; set; }

		/// <summary>
		/// Gets the topmost point in the right line segment of the rectangle.
		/// </summary>
		public Point RightTop { get; set; }

		/// <summary>
		/// Gets the bottommost point in the right line segment of the rectangle.
		/// </summary>
		public Point RightBottom { get; set; }

		/// <summary>
		/// Gets the rightmost point in the bottom line segment of the rectangle.
		/// </summary>
		public Point BottomRight { get; set; }

		/// <summary>
		/// Gets the leftmost point in the bottom line segment of the rectangle.
		/// </summary>
		public Point BottomLeft { get; set; }

		/// <summary>
		/// Gets the bottommost point in the left line segment of the rectangle.
		/// </summary>
		public Point LeftBottom { get; set; }

		/// <summary>
		/// Gets a value indicating whether the rounded rectangle is actually rounded on
		/// any corner. If false the key points represent a simple rectangle.
		/// </summary>
		public bool IsRounded
		{
			get
			{
				if (!(TopLeft != LeftTop) && !(TopRight != RightTop) && !(BottomLeft != LeftBottom))
				{
					return BottomRight != RightBottom;
				}
				return true;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Avalonia.Media.GeometryBuilder.RoundedRectKeypoints" /> struct.
		/// </summary>
		public RoundedRectKeypoints()
		{
			LeftTop = default(Point);
			TopLeft = default(Point);
			TopRight = default(Point);
			RightTop = default(Point);
			RightBottom = default(Point);
			BottomRight = default(Point);
			BottomLeft = default(Point);
			LeftBottom = default(Point);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Avalonia.Media.GeometryBuilder.RoundedRectKeypoints" /> struct.
		/// </summary>
		/// <param name="roundedRect">An existing <see cref="T:Avalonia.RoundedRect" /> to initialize keypoints with.</param>
		public RoundedRectKeypoints(RoundedRect roundedRect)
		{
			LeftTop = new Point(roundedRect.Rect.TopLeft.X, roundedRect.Rect.TopLeft.Y + roundedRect.RadiiTopLeft.Y);
			TopLeft = new Point(roundedRect.Rect.TopLeft.X + roundedRect.RadiiTopLeft.X, roundedRect.Rect.TopLeft.Y);
			TopRight = new Point(roundedRect.Rect.TopRight.X - roundedRect.RadiiTopRight.X, roundedRect.Rect.TopRight.Y);
			RightTop = new Point(roundedRect.Rect.TopRight.X, roundedRect.Rect.TopRight.Y + roundedRect.RadiiTopRight.Y);
			RightBottom = new Point(roundedRect.Rect.BottomRight.X, roundedRect.Rect.BottomRight.Y - roundedRect.RadiiBottomRight.Y);
			BottomRight = new Point(roundedRect.Rect.BottomRight.X - roundedRect.RadiiBottomRight.X, roundedRect.Rect.BottomRight.Y);
			BottomLeft = new Point(roundedRect.Rect.BottomLeft.X + roundedRect.RadiiBottomLeft.X, roundedRect.Rect.BottomLeft.Y);
			LeftBottom = new Point(roundedRect.Rect.BottomLeft.X, roundedRect.Rect.BottomRight.Y - roundedRect.RadiiBottomLeft.Y);
		}

		/// <summary>
		/// Converts the keypoints into a simple rectangle (with no corners).
		/// This is equivalent to the outer rectangle with zero corner radii.
		/// </summary>
		/// <remarks>
		/// Warning: This will force the keypoints into a simple rectangle without
		/// any rounded corners. Use <see cref="P:Avalonia.Media.GeometryBuilder.RoundedRectKeypoints.IsRounded" /> to determine if corner
		/// information is otherwise available.
		/// </remarks>
		/// <returns>A new rectangle representing the keypoints.</returns>
		public Rect ToRect()
		{
			return new Rect(new Point(LeftTop.X, TopLeft.Y), new Point(RightBottom.X, BottomRight.Y));
		}

		/// <summary>
		/// Converts the keypoints into a rounded rectangle with elliptical corner radii.
		/// </summary>
		/// <remarks>
		/// Elliptical corner radius (represented by <see cref="T:Avalonia.Vector" />) is more powerful
		/// than circular corner radius (represented by a <see cref="T:Avalonia.CornerRadius" />).
		/// Elliptical is a superset of circular.
		/// </remarks>
		/// <returns>A new rounded rectangle representing the keypoints.</returns>
		public RoundedRect ToRoundedRect()
		{
			return new RoundedRect(ToRect(), new Vector(TopLeft.X - LeftTop.X, LeftTop.Y - TopLeft.Y), new Vector(RightTop.X - TopRight.X, RightTop.Y - TopRight.Y), new Vector(RightBottom.X - BottomRight.X, BottomRight.Y - RightBottom.Y), new Vector(BottomLeft.X - LeftBottom.X, BottomLeft.Y - LeftBottom.Y));
		}
	}

	private const double PiOver2 = 1.57079633;

	private const double Epsilon = 1.53E-06;

	/// <summary>
	/// Draws a new rounded rectangle within the given geometry context.
	/// Warning: The caller must manage and dispose the <see cref="T:Avalonia.Media.StreamGeometryContext" /> externally.
	/// </summary>
	/// <remarks>
	/// WinUI: https://github.com/microsoft/microsoft-ui-xaml/blob/93742a178db8f625ba9299f62c21f656e0b195ad/dxaml/xcp/core/core/elements/geometry.cpp#L1072-L1079
	/// </remarks>
	/// <param name="context">The geometry context to draw into.</param>
	/// <param name="keypoints">The rounded rectangle keypoints defining the rectangle to draw.</param>
	public static void DrawRoundedCornersRectangle(StreamGeometryContext context, ref RoundedRectKeypoints keypoints)
	{
		context.BeginFigure(keypoints.TopLeft);
		context.LineTo(keypoints.TopRight);
		double num = keypoints.RightTop.X - keypoints.TopRight.X;
		double num2 = keypoints.TopRight.Y - keypoints.RightTop.Y;
		num = ((num > 0.0) ? num : (0.0 - num));
		num2 = ((num2 > 0.0) ? num2 : (0.0 - num2));
		context.ArcTo(keypoints.RightTop, new Size(num, num2), 0.0, isLargeArc: false, SweepDirection.Clockwise);
		context.LineTo(keypoints.RightBottom);
		num = keypoints.RightBottom.X - keypoints.BottomRight.X;
		num2 = keypoints.BottomRight.Y - keypoints.RightBottom.Y;
		num = ((num > 0.0) ? num : (0.0 - num));
		num2 = ((num2 > 0.0) ? num2 : (0.0 - num2));
		if (num != 0.0 || num2 != 0.0)
		{
			context.ArcTo(keypoints.BottomRight, new Size(num, num2), 0.0, isLargeArc: false, SweepDirection.Clockwise);
		}
		context.LineTo(keypoints.BottomLeft);
		num = keypoints.BottomLeft.X - keypoints.LeftBottom.X;
		num2 = keypoints.BottomLeft.Y - keypoints.LeftBottom.Y;
		num = ((num > 0.0) ? num : (0.0 - num));
		num2 = ((num2 > 0.0) ? num2 : (0.0 - num2));
		if (num != 0.0 || num2 != 0.0)
		{
			context.ArcTo(keypoints.LeftBottom, new Size(num, num2), 0.0, isLargeArc: false, SweepDirection.Clockwise);
		}
		context.LineTo(keypoints.LeftTop);
		num = keypoints.TopLeft.X - keypoints.LeftTop.X;
		num2 = keypoints.TopLeft.Y - keypoints.LeftTop.Y;
		num = ((num > 0.0) ? num : (0.0 - num));
		num2 = ((num2 > 0.0) ? num2 : (0.0 - num2));
		if (num != 0.0 || num2 != 0.0)
		{
			context.ArcTo(keypoints.TopLeft, new Size(num, num2), 0.0, isLargeArc: false, SweepDirection.Clockwise);
		}
		context.EndFigure(isClosed: true);
	}

	/// <summary>
	/// Draws a new rounded rectangle within the given geometry context.
	/// Warning: The caller must manage and dispose the <see cref="T:Avalonia.Media.StreamGeometryContext" /> externally.
	/// </summary>
	/// <param name="context">The geometry context to draw into.</param>
	/// <param name="rect">The existing rectangle dimensions without corner radii.</param>
	/// <param name="radiusX">The radius on the X-axis used to round the corners of the rectangle.</param>
	/// <param name="radiusY">The radius on the Y-axis used to round the corners of the rectangle.</param>
	public static void DrawRoundedCornersRectangle(StreamGeometryContext context, Rect rect, double radiusX, double radiusY)
	{
		Size size = new Size(radiusX, radiusY);
		context.BeginFigure(new Point(rect.Left + radiusX, rect.Top));
		context.LineTo(new Point(rect.Right - radiusX, rect.Top));
		context.ArcTo(new Point(rect.Right, rect.Top + radiusY), size, 1.57079633, isLargeArc: false, SweepDirection.Clockwise);
		context.LineTo(new Point(rect.Right, rect.Bottom - radiusY));
		context.ArcTo(new Point(rect.Right - radiusX, rect.Bottom), size, 1.57079633, isLargeArc: false, SweepDirection.Clockwise);
		context.LineTo(new Point(rect.Left + radiusX, rect.Bottom));
		context.ArcTo(new Point(rect.Left, rect.Bottom - radiusY), size, 1.57079633, isLargeArc: false, SweepDirection.Clockwise);
		context.LineTo(new Point(rect.Left, rect.Top + radiusY));
		context.ArcTo(new Point(rect.Left + radiusX, rect.Top), size, 1.57079633, isLargeArc: false, SweepDirection.Clockwise);
		context.EndFigure(isClosed: true);
	}

	/// <summary>
	/// Calculates the keypoints of a rounded rectangle based on the algorithm in WinUI.
	/// These keypoints may then be drawn or transformed into other types.
	/// </summary>
	/// <param name="outerBounds">The outer bounds of the rounded rectangle.
	/// This should be the overall bounds and size of the shape/control without any
	/// corner radii or border thickness adjustments.</param>
	/// <param name="borderThickness">The unadjusted border thickness of the rounded rectangle.</param>
	/// <param name="cornerRadius">The unadjusted corner radii of the rounded rectangle.
	/// The corner radius is defined to be the middle of the border stroke (center of the border).</param>
	/// <param name="sizing">The sizing mode used to calculate the final rounded rectangle size.</param>
	/// <returns>New rounded rectangle keypoints.</returns>
	public static RoundedRectKeypoints CalculateRoundedCornersRectangleWinUI(Rect outerBounds, Thickness borderThickness, CornerRadius cornerRadius, BackgroundSizing sizing)
	{
		Rect rect = outerBounds;
		bool flag;
		switch (sizing)
		{
		case BackgroundSizing.InnerBorderEdge:
			rect = outerBounds.Deflate(borderThickness);
			flag = false;
			break;
		case BackgroundSizing.OuterBorderEdge:
			flag = true;
			break;
		default:
			rect = outerBounds.Deflate(borderThickness * 0.5);
			flag = false;
			break;
		}
		double num;
		double num2;
		double num3;
		double num4;
		if (borderThickness != default(Thickness))
		{
			num = 0.5 * borderThickness.Left;
			num2 = 0.5 * borderThickness.Right;
			num3 = 0.5 * borderThickness.Top;
			num4 = 0.5 * borderThickness.Bottom;
		}
		else
		{
			num = 0.0;
			num2 = 0.0;
			num3 = 0.0;
			num4 = 0.0;
		}
		double num5;
		double num6;
		double num7;
		double num8;
		double num9;
		double num10;
		double num11;
		double num12;
		if (flag)
		{
			if (MathUtilities.AreClose(cornerRadius.TopLeft, 0.0, 1.53E-06))
			{
				num5 = 0.0;
				num6 = 0.0;
			}
			else
			{
				num5 = cornerRadius.TopLeft + num;
				num6 = cornerRadius.TopLeft + num3;
			}
			if (MathUtilities.AreClose(cornerRadius.TopRight, 0.0, 1.53E-06))
			{
				num7 = 0.0;
				num8 = 0.0;
			}
			else
			{
				num7 = cornerRadius.TopRight + num3;
				num8 = cornerRadius.TopRight + num2;
			}
			if (MathUtilities.AreClose(cornerRadius.BottomRight, 0.0, 1.53E-06))
			{
				num9 = 0.0;
				num10 = 0.0;
			}
			else
			{
				num9 = cornerRadius.BottomRight + num2;
				num10 = cornerRadius.BottomRight + num4;
			}
			if (MathUtilities.AreClose(cornerRadius.BottomLeft, 0.0, 1.53E-06))
			{
				num11 = 0.0;
				num12 = 0.0;
			}
			else
			{
				num11 = cornerRadius.BottomLeft + num4;
				num12 = cornerRadius.BottomLeft + num;
			}
		}
		else
		{
			num5 = Math.Max(0.0, cornerRadius.TopLeft - num);
			num6 = Math.Max(0.0, cornerRadius.TopLeft - num3);
			num7 = Math.Max(0.0, cornerRadius.TopRight - num3);
			num8 = Math.Max(0.0, cornerRadius.TopRight - num2);
			num9 = Math.Max(0.0, cornerRadius.BottomRight - num2);
			num10 = Math.Max(0.0, cornerRadius.BottomRight - num4);
			num11 = Math.Max(0.0, cornerRadius.BottomLeft - num4);
			num12 = Math.Max(0.0, cornerRadius.BottomLeft - num);
		}
		double num13 = num5;
		double num14 = 0.0;
		double num15 = rect.Width - num8;
		double num16 = 0.0;
		double width = rect.Width;
		double num17 = num7;
		double width2 = rect.Width;
		double num18 = rect.Height - num10;
		double num19 = rect.Width - num9;
		double height = rect.Height;
		double num20 = num12;
		double height2 = rect.Height;
		double num21 = 0.0;
		double num22 = rect.Height - num11;
		double num23 = 0.0;
		double num24 = num6;
		if (num13 > num15)
		{
			num15 = (num13 = num5 / (num5 + num8) * rect.Width);
		}
		if (num17 > num18)
		{
			num18 = (num17 = num7 / (num7 + num10) * rect.Height);
		}
		if (num19 < num20)
		{
			num20 = (num19 = num12 / (num12 + num9) * rect.Width);
		}
		if (num22 < num24)
		{
			num24 = (num22 = num6 / (num6 + num11) * rect.Height);
		}
		RoundedRectKeypoints result = new RoundedRectKeypoints();
		result.TopLeft = new Point(rect.X + num13, rect.Y + num14);
		result.TopRight = new Point(rect.X + num15, rect.Y + num16);
		result.RightTop = new Point(rect.X + width, rect.Y + num17);
		result.RightBottom = new Point(rect.X + width2, rect.Y + num18);
		result.BottomRight = new Point(rect.X + num19, rect.Y + height);
		result.BottomLeft = new Point(rect.X + num20, rect.Y + height2);
		result.LeftBottom = new Point(rect.X + num21, rect.Y + num22);
		result.LeftTop = new Point(rect.X + num23, rect.Y + num24);
		return result;
	}
}
