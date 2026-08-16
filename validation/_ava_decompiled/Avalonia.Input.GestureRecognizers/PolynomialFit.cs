namespace Avalonia.Input.GestureRecognizers;

/// An nth degree polynomial fit to a dataset.
internal sealed class PolynomialFit
{
	/// The polynomial coefficients of the fit.
	public double[] Coefficients { get; }

	/// An indicator of the quality of the fit.
	///
	/// Larger values indicate greater quality.
	public double Confidence { get; set; }

	/// Creates a polynomial fit of the given degree.
	///
	/// There are n + 1 coefficients in a fit of degree n.
	internal PolynomialFit(int degree)
	{
		Coefficients = new double[degree + 1];
	}
}
