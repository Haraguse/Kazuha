namespace Avalonia.Media.Transformation;

internal static class InterpolationUtilities
{
	public static double InterpolateScalars(double from, double to, double progress)
	{
		return from * (1.0 - progress) + to * progress;
	}

	public static Vector InterpolateVectors(Vector from, Vector to, double progress)
	{
		double x = InterpolateScalars(from.X, to.X, progress);
		double y = InterpolateScalars(from.Y, to.Y, progress);
		return new Vector(x, y);
	}

	public static Matrix ComposeTransform(Matrix.Decomposed decomposed)
	{
		return Matrix.Identity.Prepend(Matrix.CreateTranslation(decomposed.Translate)).Prepend(Matrix.CreateRotation(decomposed.Angle)).Prepend(Matrix.CreateSkew(decomposed.Skew.X, decomposed.Skew.Y))
			.Prepend(Matrix.CreateScale(decomposed.Scale));
	}

	public static Matrix.Decomposed InterpolateDecomposedTransforms(ref Matrix.Decomposed from, ref Matrix.Decomposed to, double progress)
	{
		return new Matrix.Decomposed
		{
			Translate = InterpolateVectors(from.Translate, to.Translate, progress),
			Scale = InterpolateVectors(from.Scale, to.Scale, progress),
			Skew = InterpolateVectors(from.Skew, to.Skew, progress),
			Angle = InterpolateScalars(from.Angle, to.Angle, progress)
		};
	}
}
