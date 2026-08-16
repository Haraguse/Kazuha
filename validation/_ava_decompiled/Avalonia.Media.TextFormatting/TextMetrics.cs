namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A metric that holds information about text specific measurements.
/// </summary>
public readonly record struct TextMetrics
{
	/// <summary>
	/// Em size of font used to format and display text
	/// </summary>
	public double FontRenderingEmSize { get; }

	/// <summary>
	/// Gets the distance from the top to the baseline of the line of text.
	/// </summary>
	public double Baseline { get; }

	/// <summary>
	/// Gets the recommended distance above the baseline.
	/// </summary>
	public double Ascent { get; }

	/// <summary>
	/// Gets the recommended distance under the baseline.
	/// </summary>
	public double Descent { get; }

	/// <summary>
	/// Gets the recommended additional space between two lines of text.
	/// </summary>
	public double LineGap { get; }

	/// <summary>
	/// Gets the estimated line height.
	/// </summary>
	public double LineHeight { get; }

	/// <summary>
	/// Gets a value that indicates the thickness of the underline.
	/// </summary>
	public double UnderlineThickness { get; }

	/// <summary>
	/// Gets a value that indicates the distance of the underline from the baseline.
	/// </summary>
	public double UnderlinePosition { get; }

	/// <summary>
	/// Gets a value that indicates the thickness of the underline.
	/// </summary>
	public double StrikethroughThickness { get; }

	/// <summary>
	/// Gets a value that indicates the distance of the strikethrough from the baseline.
	/// </summary>
	public double StrikethroughPosition { get; }

	public TextMetrics(GlyphTypeface glyphTypeface, double fontRenderingEmSize)
	{
		FontMetrics metrics = glyphTypeface.Metrics;
		double num = fontRenderingEmSize / (double)(int)metrics.DesignEmHeight;
		FontRenderingEmSize = fontRenderingEmSize;
		Ascent = (double)metrics.Ascent * num;
		Descent = (double)metrics.Descent * num;
		LineGap = (double)metrics.LineGap * num;
		Baseline = 0.0 - Ascent + LineGap * 0.5;
		LineHeight = Descent - Ascent + LineGap;
		UnderlineThickness = (double)metrics.UnderlineThickness * num;
		UnderlinePosition = (double)metrics.UnderlinePosition * num;
		StrikethroughThickness = (double)metrics.StrikethroughThickness * num;
		StrikethroughPosition = (double)metrics.StrikethroughPosition * num;
	}
}
