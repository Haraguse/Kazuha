using System;
using Avalonia.Media;

namespace Avalonia.Rendering.SceneGraph;

internal static class LineBoundsHelper
{
	private static double CalculateAngle(Point p1, Point p2)
	{
		double x = p2.X - p1.X;
		return Math.Atan2(p2.Y - p1.Y, x);
	}

	internal static double CalculateOppSide(double angle, double hyp)
	{
		return Math.Sin(angle) * hyp;
	}

	internal static double CalculateAdjSide(double angle, double hyp)
	{
		return Math.Cos(angle) * hyp;
	}

	private static (Point p1, Point p2) TranslatePointsAlongTangent(Point p1, Point p2, double angle, double distance)
	{
		double num = CalculateOppSide(angle, distance);
		double num2 = CalculateAdjSide(angle, distance);
		Point point = new Point(p1.X + num, p1.Y - num2);
		Point point2 = new Point(p1.X - num, p1.Y + num2);
		Point point3 = new Point(p2.X + num, p2.Y - num2);
		Point point4 = new Point(p2.X - num, p2.Y + num2);
		double x = Math.Min(point.X, Math.Min(point2.X, Math.Min(point3.X, point4.X)));
		double y = Math.Min(point.Y, Math.Min(point2.Y, Math.Min(point3.Y, point4.Y)));
		double x2 = Math.Max(point.X, Math.Max(point2.X, Math.Max(point3.X, point4.X)));
		return new ValueTuple<Point, Point>(item2: new Point(x2, Math.Max(point.Y, Math.Max(point2.Y, Math.Max(point3.Y, point4.Y)))), item1: new Point(x, y));
	}

	private static Rect CalculateBounds(Point p1, Point p2, double thickness, double angleToCorner)
	{
		(Point, Point) tuple = TranslatePointsAlongTangent(p1, p2, angleToCorner, thickness / 2.0);
		return new Rect(tuple.Item1, tuple.Item2);
	}

	public static Rect CalculateBounds(Point p1, Point p2, IPen p)
	{
		double num = CalculateAngle(p1, p2);
		if (p.LineCap != PenLineCap.Flat)
		{
			(Point, Point) tuple = TranslatePointsAlongTangent(p1, p2, num - Math.PI / 2.0, p.Thickness / 2.0);
			return CalculateBounds(tuple.Item1, tuple.Item2, p.Thickness, num);
		}
		return CalculateBounds(p1, p2, p.Thickness, num);
	}
}
