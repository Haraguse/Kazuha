using System.Diagnostics;

namespace Avalonia.Media.TextFormatting.Unicode;

/// <summary>
/// Information about a potential line break position
/// </summary>
[DebuggerDisplay("{PositionMeasure}/{PositionWrap} @ {Required}")]
public readonly record struct LineBreak
{
	/// <summary>
	/// The break position, before any trailing whitespace
	/// </summary>
	/// <remarks>
	/// This doesn't include trailing whitespace
	/// </remarks>
	public int PositionMeasure { get; }

	/// <summary>
	/// The break position, after any trailing whitespace
	/// </summary>
	/// <remarks>
	/// This includes trailing whitespace
	/// </remarks>
	public int PositionWrap { get; }

	/// <summary>
	/// True if there should be a forced line break here
	/// </summary>
	public bool Required { get; }

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="positionMeasure">The code point index to measure to</param>
	/// <param name="positionWrap">The code point index to actually break the line at</param>
	/// <param name="required">True if this is a required line break; otherwise false</param>
	public LineBreak(int positionMeasure, int positionWrap, bool required = false)
	{
		PositionMeasure = positionMeasure;
		PositionWrap = positionWrap;
		Required = required;
	}
}
