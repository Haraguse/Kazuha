using System;
using Avalonia.Utilities;

namespace Avalonia.Input.Navigation;

internal static class XYFocusAlgorithms
{
	internal class XYFocusManifolds
	{
		public (double Left, double Right) VManifold { get; set; }

		public (double Top, double Bottom) HManifold { get; set; }

		public XYFocusManifolds()
		{
			Reset();
		}

		public void Reset()
		{
			VManifold = (Left: -1.0, Right: -1.0);
			HManifold = (Top: -1.0, Bottom: -1.0);
		}
	}

	private const double InShadowThreshold = 0.25;

	private const double InShadowThresholdForSecondaryAxis = 0.02;

	private const double ConeAngle = Math.PI / 4.0;

	private const double PrimaryAxisDistanceWeight = 15.0;

	private const double SecondaryAxisDistanceWeight = 1.0;

	private const double PercentInManifoldShadowWeight = 10000.0;

	private const double PercentInShadowWeight = 50.0;

	public static double GetScoreProximity(NavigationDirection direction, Rect bounds, Rect candidateBounds, double maxDistance, bool considerSecondaryAxis)
	{
		double result = 0.0;
		double num = CalculatePrimaryAxisDistance(direction, bounds, candidateBounds);
		double num2 = CalculateSecondaryAxisDistance(direction, bounds, candidateBounds);
		if (num >= 0.0)
		{
			(double, double) referenceManifold;
			(double, double) potentialManifold;
			if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
			{
				referenceManifold = (bounds.Top, bounds.Bottom);
				potentialManifold = (candidateBounds.Top, candidateBounds.Bottom);
			}
			else
			{
				referenceManifold = (bounds.Left, bounds.Right);
				potentialManifold = (candidateBounds.Left, candidateBounds.Right);
			}
			if (!considerSecondaryAxis || CalculatePercentInShadow(referenceManifold, potentialManifold) != 0.0)
			{
				num2 = 0.0;
			}
			result = maxDistance - (num + num2);
		}
		return result;
	}

	public static double GetScoreProjection(NavigationDirection direction, Rect bounds, Rect candidateBounds, XYFocusManifolds manifolds, double maxDistance)
	{
		double result = 0.0;
		double percentInManifoldShadow = 0.0;
		(double, double) referenceManifold;
		(double, double) referenceManifold2;
		(double, double) potentialManifold;
		if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
		{
			referenceManifold = (bounds.Top, bounds.Bottom);
			referenceManifold2 = manifolds.HManifold;
			potentialManifold = (candidateBounds.Top, candidateBounds.Bottom);
		}
		else
		{
			referenceManifold = (bounds.Left, bounds.Right);
			referenceManifold2 = manifolds.VManifold;
			potentialManifold = (candidateBounds.Left, candidateBounds.Right);
		}
		double num = CalculatePrimaryAxisDistance(direction, bounds, candidateBounds);
		double num2 = CalculateSecondaryAxisDistance(direction, bounds, candidateBounds);
		if (num >= 0.0)
		{
			double num3 = CalculatePercentInShadow(referenceManifold, potentialManifold);
			if (num3 >= 0.02)
			{
				percentInManifoldShadow = CalculatePercentInShadow(referenceManifold2, potentialManifold);
				num2 = maxDistance;
			}
			num = maxDistance - num;
			num2 = maxDistance - num2;
			if (num3 >= 0.25)
			{
				num3 = 1.0;
				num *= 2.0;
			}
			result = CalculateScore(num3, num, num2, percentInManifoldShadow);
		}
		return result;
	}

	public static void UpdateManifolds(NavigationDirection direction, Rect bounds, Rect newFocusBounds, XYFocusManifolds manifolds)
	{
		(double Left, double Right) vManifold = manifolds.VManifold;
		(double, double) tuple = manifolds.HManifold;
		(double, double) tuple2 = vManifold;
		if (tuple2.Item2 < 0.0)
		{
			tuple2 = (bounds.Left, bounds.Right);
		}
		if (tuple.Item2 < 0.0)
		{
			tuple = (bounds.Top, bounds.Bottom);
		}
		switch (direction)
		{
		case NavigationDirection.Left:
		case NavigationDirection.Right:
			tuple = (Math.Max(Math.Max(newFocusBounds.Top, bounds.Top), tuple.Item1), Math.Min(Math.Min(newFocusBounds.Bottom, bounds.Bottom), tuple.Item2));
			if (tuple.Item2 <= tuple.Item1)
			{
				tuple = (newFocusBounds.Top, newFocusBounds.Bottom);
			}
			tuple2 = (newFocusBounds.Left, newFocusBounds.Right);
			break;
		case NavigationDirection.Up:
		case NavigationDirection.Down:
			tuple2 = (Math.Max(Math.Max(newFocusBounds.Left, bounds.Left), tuple2.Item1), Math.Min(Math.Min(newFocusBounds.Right, bounds.Right), tuple2.Item2));
			if (tuple2.Item2 <= tuple2.Item1)
			{
				tuple2 = (newFocusBounds.Left, newFocusBounds.Right);
			}
			tuple = (newFocusBounds.Top, newFocusBounds.Bottom);
			break;
		}
		(double, double) vManifold2 = tuple2;
		(double, double) hManifold = tuple;
		manifolds.VManifold = vManifold2;
		manifolds.HManifold = hManifold;
	}

	private static double CalculateScore(double percentInShadow, double primaryAxisDistance, double secondaryAxisDistance, double percentInManifoldShadow)
	{
		return percentInShadow * 50.0 + primaryAxisDistance * 15.0 + secondaryAxisDistance * 1.0 + percentInManifoldShadow * 10000.0;
	}

	public static bool ShouldCandidateBeConsideredForRanking(Rect bounds, Rect candidateBounds, double maxDistance, NavigationDirection direction, Rect exclusionRect, bool ignoreCone)
	{
		if (candidateBounds.IsEmpty() || candidateBounds.Contains(bounds) || exclusionRect.Intersects(candidateBounds) || exclusionRect.Contains(candidateBounds))
		{
			return false;
		}
		if (ignoreCone || direction == NavigationDirection.Down || direction == NavigationDirection.Up)
		{
			return true;
		}
		Vector vector = new Vector(0.0, (float)bounds.Top);
		Vector vector2 = new Vector(0.0, (float)bounds.Bottom);
		Vector[] array = new Vector[4] { candidateBounds.TopLeft, candidateBounds.BottomLeft, candidateBounds.BottomRight, candidateBounds.TopRight };
		maxDistance *= 2.0;
		Span<Vector> span = stackalloc Vector[4];
		switch (direction)
		{
		case NavigationDirection.Left:
		{
			vector = new Vector(bounds.Left - 1.0, vector.Y);
			vector2 = new Vector(bounds.Left - 1.0, vector2.Y);
			double num2 = Math.PI;
			Vector[] array3 = new Vector[2]
			{
				new Vector(vector.X + maxDistance * Math.Cos(num2 + Math.PI / 4.0), vector.Y + maxDistance * Math.Sin(num2 + Math.PI / 4.0)),
				new Vector(vector2.X + maxDistance * Math.Cos(num2 - Math.PI / 4.0), vector2.Y + maxDistance * Math.Sin(num2 - Math.PI / 4.0))
			};
			span[0] = vector;
			span[1] = array3[0];
			span[2] = array3[1];
			span[3] = vector2;
			break;
		}
		case NavigationDirection.Right:
		{
			vector = new Vector(bounds.Right + 1.0, vector.Y);
			vector2 = new Vector(bounds.Right + 1.0, vector2.Y);
			double num = 0.0;
			Vector[] array2 = new Vector[2]
			{
				new Vector(vector.X + maxDistance * Math.Cos(num + Math.PI / 4.0), vector.Y + maxDistance * Math.Sin(num + Math.PI / 4.0)),
				new Vector(vector2.X + maxDistance * Math.Cos(num - Math.PI / 4.0), vector2.Y + maxDistance * Math.Sin(num - Math.PI / 4.0))
			};
			span[0] = vector2;
			span[1] = array2[0];
			span[2] = array2[1];
			span[3] = vector;
			break;
		}
		}
		if (!MathUtilities.DoPolygonsIntersect(4u, span, 4u, array) && !MathUtilities.IsEntirelyContained(4u, array, 4u, span))
		{
			return MathUtilities.IsEntirelyContained(4u, span, 4u, array);
		}
		return true;
	}

	private static double CalculatePrimaryAxisDistance(NavigationDirection direction, Rect bounds, Rect candidateBounds)
	{
		double result = -1.0;
		bool flag = bounds.Intersects(candidateBounds);
		if (bounds == candidateBounds)
		{
			return -1.0;
		}
		if (direction == NavigationDirection.Left && (candidateBounds.Right <= bounds.Left || (flag && candidateBounds.Left <= bounds.Left)))
		{
			result = Math.Abs(bounds.Left - candidateBounds.Right);
		}
		else if (direction == NavigationDirection.Right && (candidateBounds.Left >= bounds.Right || (flag && candidateBounds.Right >= bounds.Right)))
		{
			result = Math.Abs(candidateBounds.Left - bounds.Right);
		}
		else if (direction == NavigationDirection.Up && (candidateBounds.Bottom <= bounds.Top || (flag && candidateBounds.Top <= bounds.Top)))
		{
			result = Math.Abs(bounds.Top - candidateBounds.Bottom);
		}
		else if (direction == NavigationDirection.Down && (candidateBounds.Top >= bounds.Bottom || (flag && candidateBounds.Bottom >= bounds.Bottom)))
		{
			result = Math.Abs(candidateBounds.Top - bounds.Bottom);
		}
		return result;
	}

	private static double CalculateSecondaryAxisDistance(NavigationDirection direction, Rect bounds, Rect candidateBounds)
	{
		if (direction == NavigationDirection.Left || direction == NavigationDirection.Right)
		{
			return (candidateBounds.Top < bounds.Top) ? Math.Abs(bounds.Top - candidateBounds.Bottom) : Math.Abs(candidateBounds.Top - bounds.Bottom);
		}
		return (candidateBounds.Left < bounds.Left) ? Math.Abs(bounds.Left - candidateBounds.Right) : Math.Abs(candidateBounds.Left - bounds.Right);
	}

	/// Calculates the percentage of the potential element that is in the shadow of the reference element.
	/// In other words, this method calculates percentage overlap of two elements ranges (top+bottom or left+right).
	private static double CalculatePercentInShadow((double first, double second) referenceManifold, (double first, double second) potentialManifold)
	{
		if (referenceManifold.first > potentialManifold.second || referenceManifold.second <= potentialManifold.first)
		{
			return 0.0;
		}
		double value = Math.Min(referenceManifold.second, potentialManifold.second) - Math.Max(referenceManifold.first, potentialManifold.first);
		value = Math.Abs(value);
		double num = Math.Abs(potentialManifold.second - potentialManifold.first);
		double num2 = Math.Abs(referenceManifold.second - referenceManifold.first);
		if (num2 >= num)
		{
			num2 = num;
		}
		double result = 1.0;
		if (num2 != 0.0)
		{
			result = Math.Min(value / num2, 1.0);
		}
		return result;
	}
}
